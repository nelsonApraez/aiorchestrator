using Integration.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;

namespace cms_genai_rag_aiorchestrator.Tests
{
    public class BlobProcessTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IBlobStorageService> _mockBlobStorageService;
        private readonly BlobProcess _blobProcess;

        public BlobProcessTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockBlobStorageService = new Mock<IBlobStorageService>();

            _blobProcess = new BlobProcess(
                _mockConfiguration.Object,
                _mockBlobStorageService.Object
            );
        }

        [Fact]
        public async Task GenerateReadSasTokenAsync_ShouldReturnToken_WhenSuccessful()
        {
            // Arrange
            string blobName = "testBlob";
            string expectedToken = "sasToken";

            _mockConfiguration.Setup(c => c["AzureStorage:FolderToDownLoad"]).Returns("downloadFolder");
            _mockConfiguration.Setup(c => c["AzureStorage:ExpiryMinutes"]).Returns("60");

            _mockBlobStorageService.Setup(s => s.GenerateReadSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(expectedToken);

            // Act
            var result = await _blobProcess.GenerateReadSasTokenAsync(blobName);

            // Assert
            Assert.Equal(expectedToken, result);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldCallUploadFileAsync_WhenSuccessful()
        {
            // Arrange
            string fileName = "testFile.txt";
            using var fileStream = new MemoryStream();

            _mockConfiguration.Setup(c => c["AzureStorage:FolderToUpLoad"]).Returns("uploadFolder");

            // Act
            await _blobProcess.UploadFileAsync(fileName, fileStream);

            // Assert
            _mockBlobStorageService.Verify(s => s.UploadFileAsync("uploadFolder", It.IsAny<string>(), fileStream), Times.Once);
        }

    }
}
