using Microsoft.Extensions.Configuration;
using Integration.Interfaces;
using cms_genai_rag_aiorchestrator.Interfaces;
using cms_genai_rag_aiorchestrator.Utilities;



namespace cms_genai_rag_aiorchestrator
{

    public class BlobProcess : IBlobProcess
    {
        private readonly IConfiguration _configuration;
        private readonly IBlobStorageService _blobStorageService;

        public BlobProcess(IConfiguration configuration, IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
            _configuration = configuration;

        }

        public async Task<string> GenerateReadSasTokenAsync(string blobName)
        {

            string FolderToDownLoad = _configuration["AzureStorage:FolderToDownLoad"] ?? throw new InvalidOperationException("Missing Azure Storage FolderToDownLoad in configuration"); 
            string ExpiryMinutes = _configuration["AzureStorage:ExpiryMinutes"] ?? throw new InvalidOperationException("Missing Azure Storage ExpiryMinutes in configuration"); 


            return await _blobStorageService.GenerateReadSasTokenAsync(FolderToDownLoad, blobName, Convert.ToInt32(ExpiryMinutes));
        }

        public async Task UploadFileAsync(string fileName, Stream fileStream)
        {

            string FolderToUpLoad = _configuration["AzureStorage:FolderToUpLoad"] ?? throw new InvalidOperationException("Missing Azure Storage FolderToUpLoad in configuration");
            await _blobStorageService.UploadFileAsync(FolderToUpLoad, cms_genai_rag_aiorchestrator.Utilities.Utilities.NormalizeFileName(fileName), fileStream);

        }
    }
}
