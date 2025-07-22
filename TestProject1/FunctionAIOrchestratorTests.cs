using Integration.Interfaces;
using Integration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using cms_genai_rag_aiorchestrator.Contracts;
using cms_genai_rag_aiorchestrator.Interfaces;
using System.Text;
using System.Text.Json;

namespace cms_genai_rag_aiorchestrator.Tests
{
    public class FunctionAIOrchestratorTests
    {
        private readonly Mock<ILogger<FunctionAIOrchestrator>> _mockLogger;
        private readonly Mock<IProcess> _mockProcess;
        private readonly Mock<IBlobProcess> _mockBlobProcess;
        private readonly Mock<ICosmosDbService> _mockCosmosDbService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly FunctionAIOrchestrator _functionAIOrchestrator;

        public FunctionAIOrchestratorTests()
        {
            _mockLogger = new Mock<ILogger<FunctionAIOrchestrator>>();
            _mockProcess = new Mock<IProcess>();
            _mockBlobProcess = new Mock<IBlobProcess>();
            _mockCosmosDbService = new Mock<ICosmosDbService>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.Setup(config => config["Security:KeyName"]).Returns("TestKeyName");
            _mockConfiguration.Setup(config => config["TestKeyName"]).Returns("TestToken");

            _functionAIOrchestrator = new FunctionAIOrchestrator(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockProcess.Object,
                _mockBlobProcess.Object,
                _mockCosmosDbService.Object);
        }

        private HttpRequest CreateHttpRequest(string token, string body)
        {
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = token;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            request.Body = stream;
            stream.Position = 0;
            return request;
        }

        [Fact]
        public async Task Run_InvalidAuthorization_ReturnsUnauthorized()
        {
            // Arrange:
            string invalidToken = "Bearer invalid-token";
            var requestBody = JsonConvert.SerializeObject(new RequestBody
            {
                Conversation_id = "123",
                Query = "Test query",
                Person_id = "123"
            });
            var request = CreateHttpRequest(invalidToken, requestBody);

            // Act
            var result = await _functionAIOrchestrator.Run(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Run_EmptyQuery_ReturnsBadRequest()
        {
            // Arrange: 
            string validToken = "Bearer TestToken";
            var requestBody = JsonConvert.SerializeObject(new RequestBody
            {
                Conversation_id = "123",
                Query = "",
                Person_id = "123"
            });
            var request = CreateHttpRequest(validToken, requestBody);

            // Act
            var result = await _functionAIOrchestrator.Run(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User prompt is empty ", badRequestResult.Value);
        }

        [Fact]
        public async Task Run_ValidRequest_ReturnsOkResult()
        {
            // Arrange: 
            string validToken = "Bearer TestToken";
            var requestBodyObj = new RequestBody
            {
                Conversation_id = "123",
                Query = "Test query",
                Person_id = "123"
            };
            var requestBody = JsonConvert.SerializeObject(requestBodyObj);
            var request = CreateHttpRequest(validToken, requestBody);

            
            var jsonResult = "{\"answer\": \"Test Answer\", \"thoughts\": \"Test Thoughts\"}";
            var jsonDoc = JsonDocument.Parse(jsonResult);
            _mockProcess.Setup(p => p.RunProcessAsync(It.IsAny<RequestBody>()))
                        .ReturnsAsync(jsonDoc.RootElement);

            var processResponse = new ProcessResponse
            {
                Answer = "Test Answer",
                Thoughts = "Test Thoughts",
                Current_state = true,
                Detail = "Success",
                Conversation_id = "123",
                Error = "null"

            };

            // Act
            var result = await _functionAIOrchestrator.Run(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ProcessResponse>(okResult.Value);
            Assert.Equal("Test Answer", response.Answer);
            Assert.Equal("Test Thoughts", response.Thoughts);
            Assert.True(response.Current_state);
            Assert.Equal("Success", response.Detail);
            Assert.Equal("123", response.Conversation_id);
        }

        [Fact]
        public async Task Run_ValidRequest_EmptyConversationId_GeneratesNewConversationId()
        {
            // Arrange: 
            string validToken = "Bearer TestToken";
            var requestBodyObj = new RequestBody
            {
                Conversation_id = "",
                Query = "Test query",
                Person_id = "123"
            };
            var requestBody = JsonConvert.SerializeObject(requestBodyObj);
            var request = CreateHttpRequest(validToken, requestBody);

           
            var jsonResult = "{\"answer\": \"Test Answer\", \"thoughts\": \"Test Thoughts\"}";
            var jsonDoc = JsonDocument.Parse(jsonResult);
            _mockProcess.Setup(p => p.RunProcessAsync(It.IsAny<RequestBody>()))
                        .ReturnsAsync(jsonDoc.RootElement);

            // Act
            var result = await _functionAIOrchestrator.Run(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ProcessResponse>(okResult.Value);
            Assert.Equal("Test Answer", response.Answer);
            Assert.Equal("Test Thoughts", response.Thoughts);
            Assert.True(response.Current_state);
            Assert.Equal("Success", response.Detail);
            
            Assert.False(string.IsNullOrEmpty(response.Conversation_id));
        }


        [Fact]
        public async Task Run_InvalidToken_ReturnsUnauthorizedResult()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer InvalidToken";
            var request = context.Request;

            // Act
            var result = await _functionAIOrchestrator.Run(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetToken_InvalidToken_ReturnsUnauthorizedResult()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer InvalidToken";
            var request = context.Request;

            // Act
            var result = await _functionAIOrchestrator.GetToken(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetToken_ValidToken_ReturnsOkObjectResult()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer TestToken";
            var requestBody = new RequestBlobBody
            {
                BlobName = "test_blob_name"
            };
            var json = JsonConvert.SerializeObject(requestBody);
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var request = context.Request;

            _mockBlobProcess.Setup(bp => bp.GenerateReadSasTokenAsync(It.IsAny<string>())).ReturnsAsync("test_sas_token");

            // Act
            var result = await _functionAIOrchestrator.GetToken(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UploadFile_InvalidToken_ReturnsUnauthorizedResult()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer InvalidToken";
            var request = context.Request;

            // Act
            var result = await _functionAIOrchestrator.UploadFile(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UploadFile_ValidToken_ReturnsOkObjectResult()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer TestToken";
            var formFile = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("file_content")), 0, 11, "file", "test.txt");
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection { formFile });
            context.Request.Form = formCollection;
            var request = context.Request;

            // Act
            var result = await _functionAIOrchestrator.UploadFile(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAllUploadStatus_InvalidToken_ReturnsUnauthorizedResult()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer InvalidToken";
            var request = context.Request;

            // Act
            var result = await _functionAIOrchestrator.GetAllUploadStatus(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetAllUploadStatus_ValidToken_ReturnsOkObjectResult()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer TestToken";
            var request = context.Request;

            var documentHistory = new List<DocumentHistory>
            {
                new DocumentHistory { Id = "1", FileName = "test.txt", State = "Uploaded" }
            };
            _mockCosmosDbService.Setup(cds => cds.GetLastDocumentHistory()).ReturnsAsync(documentHistory);

            // Act
            var result = await _functionAIOrchestrator.GetAllUploadStatus(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
    }
}