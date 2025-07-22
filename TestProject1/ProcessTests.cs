using System.Text.Json;
using Integration.Interfaces;
using Integration.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using OpenAI.Chat;
using cms_genai_rag_aiorchestrator.Contracts;
using cms_genai_rag_aiorchestrator;
using RequestBody = cms_genai_rag_aiorchestrator.Contracts.RequestBody;

namespace cms_genai_rag_aiorchestrator.Tests
{
    public class ProcessTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IOpenIAServices> _mockOpenIAServices;
        private readonly Mock<ICosmosDbService> _mockCosmosDbService;
        private readonly Mock<ICognitiveSearchService> _mockCognitiveSearchService;
        private readonly Process _process;

        public ProcessTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockOpenIAServices = new Mock<IOpenIAServices>();
            _mockCosmosDbService = new Mock<ICosmosDbService>();
            _mockCognitiveSearchService = new Mock<ICognitiveSearchService>();

            _process = new Process(
                _mockConfiguration.Object,
                _mockOpenIAServices.Object,
                _mockCosmosDbService.Object,
                _mockCognitiveSearchService.Object
            );
        }

        [Fact]
        public async Task RunProcessAsync_ShouldReturnJsonElement_WhenSuccessful()
        {
            // Arrange
            var requestBody = new RequestBody
            {
                Conversation_id = "conversationId",
                Person_id = "personId",
                Query = "query",
                Tags = new Dictionary<string, string>()
            };

            _mockConfiguration.Setup(c => c["CosmosDb:AmountHistoricalChatToRecover"]).Returns("10");
            _mockConfiguration.Setup(c => c["Prompt:InitialTemplateSystem"]).Returns("InitialTemplateSystem");
            _mockConfiguration.Setup(c => c["Prompt:FinalTemplateSystem"]).Returns("FinalTemplateSystem");
            _mockConfiguration.Setup(c => c["Prompt:FinalTemplateUser"]).Returns("FinalTemplateUser");

            _mockOpenIAServices.Setup(s => s.CreateChatCompletionTextAsync(It.IsAny<IList<ChatMessage>>(), "text", It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"response\": \"query\"}");

            _mockOpenIAServices.Setup(s => s.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<float> { 0.1f, 0.2f });

            _mockCognitiveSearchService.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("documentContent");

            _mockCosmosDbService.Setup(s => s.GetConversationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Conversation { DateCreate = DateTime.Now, DateModify = DateTime.Now, id = Guid.NewGuid().ToString(), 
                    PersonId = Guid.NewGuid().ToString(), SessionId = Guid.NewGuid().ToString(), Tags = new Dictionary<string, string>(), Chats = new List<Chat> { new Chat { Text = "chatText", Mode = "User", Date = DateTime.Now } } });

            _mockCosmosDbService.Setup(s => s.CreateConversationAsync(It.IsAny<Conversation>())).Returns(Task.CompletedTask);
            _mockCosmosDbService.Setup(s => s.UpdateChatsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<Chat>>())).Returns(Task.CompletedTask);

            // Act
            var result = await _process.RunProcessAsync(requestBody);

            // Assert
            Assert.IsType<JsonElement>(result);
        }

        [Fact]
        public async Task RunProcessAsync_ShouldThrowException_WhenConfigurationIsMissing()
        {
            // Arrange
            var requestBody = new RequestBody
            {
                Conversation_id = "conversationId",
                Person_id = "personId",
                Query = "query"
            };

            _mockConfiguration.Setup(c => c["CosmosDb:AmountHistoricalChatToRecover"]).Returns((string?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _process.RunProcessAsync(requestBody));
        }

        [Fact]
        public async Task RunProcessAsync_ShouldThrowException_WhenOpenIAServiceFails()
        {
            // Arrange
            var requestBody = new RequestBody
            {
                Conversation_id = "conversationId",
                Person_id = "personId",
                Query = "query"
            };

            _mockConfiguration.Setup(c => c["CosmosDb:AmountHistoricalChatToRecover"]).Returns("10");
            _mockConfiguration.Setup(c => c["Prompt:InitialTemplateSystem"]).Returns("InitialTemplateSystem");
            _mockConfiguration.Setup(c => c["Prompt:FinalTemplateSystem"]).Returns("FinalTemplateSystem");
            _mockConfiguration.Setup(c => c["Prompt:FinalTemplateUser"]).Returns("FinalTemplateUser");

            _mockOpenIAServices.Setup(s => s.CreateChatCompletionTextAsync(It.IsAny<IList<ChatMessage>>(), "text", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("OpenIA Service Error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _process.RunProcessAsync(requestBody));
        }

    }
}
