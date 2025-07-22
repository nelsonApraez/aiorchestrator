namespace cms_genai_rag_aiorchestrator.Interfaces
{
    public interface IBlobProcess
    {
        Task<string> GenerateReadSasTokenAsync(string blobName);
        Task UploadFileAsync(string fileName, Stream fileStream);
    }
}
