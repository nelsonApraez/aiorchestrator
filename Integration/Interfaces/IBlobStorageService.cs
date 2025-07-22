namespace Integration.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> GenerateReadSasTokenAsync(string folderName, string blobName, int expiryMinutes);
        Task UploadFileAsync(string folderName, string fileName, Stream fileStream);
    }
}
