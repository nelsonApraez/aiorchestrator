
using Integration.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using cms_genai_rag_aiorchestrator.Contracts;
using cms_genai_rag_aiorchestrator.Interfaces;
using System.Net;


namespace cms_genai_rag_aiorchestrator
{
    public class FunctionAIOrchestrator
    {
        private readonly ILogger<FunctionAIOrchestrator> _logger;
        private readonly IProcess _process;
        private readonly IBlobProcess _blobprocess;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly string Authentication;

        public FunctionAIOrchestrator(IConfiguration configuration, ILogger<FunctionAIOrchestrator> log, IProcess process, IBlobProcess blobprocess, ICosmosDbService cosmosDbService)
        {
            _logger = log;
            _process = process;
            _blobprocess = blobprocess;
            _cosmosDbService = cosmosDbService;

            string secretName = configuration["Security:KeyName"]
                ?? throw new InvalidOperationException("Missing Configuration Security:KeyName in configuration."); 

            Authentication = !string.IsNullOrWhiteSpace(secretName)
                ? configuration[secretName] ?? configuration["Security:Token"]
                : configuration["Security:Token"]
                ?? throw new InvalidOperationException("Missing Security:Token in configuration.");
        }


        /// <summary>
        /// Orchestrates the AI process based on the provided request.
        /// </summary>
        /// <param name="req">The HTTP request triggering the function.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [Function("FunctionAIOrchestrator")]
        [OpenApiOperation(
            operationId: "FunctionAIOrchestrator",
            tags: new[] { "Process" },
            Summary = "Orchestrates the AI process",
            Description = "This function orchestrates the AI process based on the provided request. It validates the request, processes it, and returns the result.",
            Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody("application/json", typeof(RequestBody), Description = "Parameters", Example = typeof(ProcessResponse))]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(ProcessResponse),
            Summary = "The OK response",
            Description = "Returns the result of the AI process.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.BadRequest,
            contentType: "application/json",
            bodyType: typeof(ProcessResponse),
            Summary = "The Warning response",
            Description = "Returns a warning if the request is invalid.")]

        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header)]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string token = req.Headers["Authorization"].ToString();

            if (token != $"Bearer {Authentication}")
            {
                return new UnauthorizedResult();
            }

            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestUser = JsonConvert.DeserializeObject<RequestBody>(requestBody);


            _logger.LogInformation("RequestBody: Conversation_id: ");

            if (requestUser is null || string.IsNullOrEmpty(requestUser.Query))

                return new BadRequestObjectResult("User prompt is empty ");

            if (string.IsNullOrEmpty(requestUser.Conversation_id))
                requestUser.Conversation_id = Guid.NewGuid().ToString();

            ProcessResponse processResponse = new();
            try
            {
                var answerObject = await _process.RunProcessAsync(requestUser);
                var ans = answerObject.GetProperty("answer").GetString() ?? throw new InvalidOperationException("Failed to get answer");
                var thoughts = answerObject.GetProperty("thoughts").GetString() ?? throw new InvalidOperationException("Failed to get thoughts");
                processResponse.Answer = ans;
                processResponse.Thoughts = thoughts;
                processResponse.Current_state = true;
                processResponse.Detail = "Success";
                processResponse.Conversation_id = requestUser.Conversation_id;
            }
            catch (Exception ex)
            {  _logger.LogInformation(ex, "Error: {Message}", ex.Message); processResponse.Current_state = false; processResponse.Detail = "Fail";  processResponse.Error = ex.Message;
                processResponse.Conversation_id = requestUser.Conversation_id; return new ObjectResult(processResponse) { StatusCode = 500 }; }

            return new OkObjectResult(processResponse);
        }


        /// <summary>
        /// Generates a read SAS token for a specified blob.
        /// </summary>
        /// <param name="req">The HTTP request triggering the function.</param>
        /// <returns>HTTP response containing the SAS token.</returns>
        [Function("GenerateReadSasToken")]
        [OpenApiOperation(
            operationId: "GenerateReadSasToken",
            tags: new[] { "Blob" },
            Summary = "Generates a read SAS token",
            Description = "This function generates a read SAS token for a specified blob, allowing temporary access to the blob.",
            Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody("application/json", typeof(RequestBlobBody), Description = "Parameters", Example = typeof(string))]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(string),
            Summary = "The OK response",
            Description = "Returns the generated SAS token.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.BadRequest,
            contentType: "application/json",
            bodyType: typeof(string),
            Summary = "The Warning response",
            Description = "Returns a warning if the request is invalid.")]

        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header)]
        public async Task<IActionResult> GetToken([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string token = req.Headers["Authorization"].ToString();

            if (token != $"Bearer {Authentication}")
            {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestUser = JsonConvert.DeserializeObject<RequestBlobBody>(requestBody);


            _logger.LogInformation("RequestBlobBody: BlobName: ");

            if (requestUser is null ||  string.IsNullOrEmpty(requestUser.BlobName))
                return new BadRequestObjectResult("BlobName is empty ");
            string result = string.Empty;

            try
            {
                result = await _blobprocess.GenerateReadSasTokenAsync(requestUser.BlobName);
            }
            catch (Exception ex)
            { _logger.LogInformation(ex, "Error: {Message}", ex.Message); return new ObjectResult("Internal Server Error") { StatusCode = 500 };  }

            return new OkObjectResult(result);
        }

        /// <summary>
        /// Uploads a file to the blob storage.
        /// </summary>
        /// <param name="req">The HTTP request triggering the function.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [Function("UploadFile")]
        [OpenApiOperation(
            operationId: "UploadFile",
            tags: new[] { "Blob" },
            Summary = "Uploads a file to the blob storage",
            Description = "This function uploads a file to the blob storage. It reads the multipart form data from the request and uploads the file to the specified blob container.",
            Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody("multipart/form-data", typeof(string), Description = "File and parameters")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(string),
            Summary = "File uploaded successfully",
            Description = "Returns a success message upon successful file upload.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.BadRequest,
            contentType: "application/json",
            bodyType: typeof(string),
            Summary = "Invalid request",
            Description = "Returns a warning if the request is invalid.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.InternalServerError,
            contentType: "application/json",
            bodyType: typeof(string),
            Summary = "Internal server error",
            Description = "Returns an error message if there is an internal server error.")]

        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header)]
        public async Task<IActionResult> UploadFile([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string token = req.Headers["Authorization"].ToString();

            if (token != $"Bearer {Authentication}")
            {
                return new UnauthorizedResult();
            }

            try
            {
                // Read multipart form data
                var formCollectionA = await req.ReadFormAsync();

                foreach (var file in formCollectionA.Files)

                {

                    if (file == null || string.IsNullOrEmpty(file.FileName))
                    {
                        return new BadRequestObjectResult("File is required.");
                    }

                    // Open file stream for upload
                    using var fileStream = file.OpenReadStream();
                    await _blobprocess.UploadFileAsync(file.FileName, fileStream);
                }

                return new OkObjectResult(new { message = $"Files uploaded successfully" });
            }
            catch (Exception ex)
            { _logger.LogError(ex, "Exception in UploadFile function");  return new ObjectResult("Internal Server Error") { StatusCode = 500 }; }
        }


        /// <summary>
        /// Retrieves the status of all uploaded files.
        /// </summary>
        /// <param name="req">The HTTP request triggering the function.</param>
        /// <returns>HTTP response containing the status of all uploaded files.</returns>
        [Function("GetAllUploadStatus")]
        [OpenApiOperation(
            operationId: "GetAllUploadStatus",
            tags: new[] { "Blob" },
            Summary = "Retrieves the status of all uploaded files",
            Description = "This function retrieves the status of all uploaded files from the database.",
            Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(string),
            Summary = "Return successfully",
            Description = "Returns the status of all uploaded files.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.BadRequest,
            contentType: "application/json",
            bodyType: typeof(string),
            Summary = "Invalid request",
            Description = "Returns a warning if the request is invalid.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.InternalServerError,
            contentType: "application/json",
            bodyType: typeof(string),
            Summary = "Internal server error",
            Description = "Returns an error message if there is an internal server error.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header)]
        public async Task<IActionResult> GetAllUploadStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            string token = req.Headers["Authorization"].ToString();

            if (token != $"Bearer {Authentication}")
            {
                return new UnauthorizedResult();
            }

            try
            {
                var fileUploads = await _cosmosDbService.GetLastDocumentHistory();
                return new OkObjectResult(fileUploads);
            }
            catch (Exception ex)
            { _logger.LogError(ex, "Exception in Getalluploadstatus function");  return new ObjectResult("Internal Server Error") { StatusCode = 500 }; }
        }

    }
}
