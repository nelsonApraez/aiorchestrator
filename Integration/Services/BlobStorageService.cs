using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Integration.Interfaces;

using Microsoft.Extensions.Configuration;


namespace Integration.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;


        public BlobStorageService(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var secretName = configuration["AzureStorage:KeyName"];

            var connectionString = !string.IsNullOrWhiteSpace(secretName)
                ? configuration[secretName] ?? configuration["AzureStorage:ConnectionString"]
                : configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Missing Azure Storage Connection String in configuration");


            var containerName = configuration["AzureStorage:Container"]
                ?? throw new InvalidOperationException("Missing Azure Storage Container in configuration");



            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = containerName;
        }

        /// <summary>
        /// Generates a SAS token with read permission for a document inside a specific folder.
        /// </summary>
        public async Task<string> GenerateReadSasTokenAsync(string folderName, string blobName, int expiryMinutes)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            string blobPath = $"{folderName}/{blobName}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            if (!await blobClient.ExistsAsync())
            {

                return "The requested blob does not exist in the specified folder.";

            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobPath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = blobClient.GenerateSasUri(sasBuilder);
            return sasToken.ToString();
        }

        /// <summary>
        /// Uploads a file to a specific folder inside the container.
        /// </summary>
        public async Task UploadFileAsync(string folderName, string fileName, Stream fileStream)
        { var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);  var blobClient = containerClient.GetBlobClient($"{folderName}/{fileName}"); await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = "application/octet-stream" }); }
    }
}
