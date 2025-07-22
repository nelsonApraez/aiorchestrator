using System.Reflection;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Integration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Integration.Tests
{
    public class BlobStorageServiceTests
    {
        private readonly ServiceProvider _provider;

        public BlobStorageServiceTests()
        {
            // Se ejecuta 1 vez por cada prueba de esta clase
            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false)
                .Build();
            services.AddSingleton<IConfiguration>(config);

            services.AddTransient<BlobStorageService>();

            _provider = services.BuildServiceProvider();
        }

        // Helper method to create a test configuration.
        private IConfiguration GetTestConfiguration(Dictionary<string, string> overrides = null)
        {

            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false);

            if (overrides != null)
            {
                configBuilder.AddInMemoryCollection(overrides);
            }


            return configBuilder.Build();
        }

        [Fact]
        public void BlobStorageService_Esta_Registrado()
        {
            var service = _provider.GetService<BlobStorageService>();
            Assert.NotNull(service);
        }

        // Helper method to set the private _blobServiceClient field using reflection.
        private void SetPrivateBlobServiceClient(BlobStorageService service, BlobServiceClient client)
        {
            var field = typeof(BlobStorageService)
                .GetField("_blobServiceClient", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(service, client);
        }

        [Fact]
        public void Constructor_NullConfiguration_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BlobStorageService(null));
        }

        [Fact]
        public void Constructor_MissingConnectionString_ThrowsInvalidOperationException()
        {
            // Aquí realmente dejas la connection string en null
            var config = GetTestConfiguration(new Dictionary<string, string>
            {
                ["AzureStorage:ConnectionString"] = null
            });

            Assert.Throws<InvalidOperationException>(() => new BlobStorageService(config));
        }

        [Fact]
        public void Constructor_MissingContainer_ThrowsInvalidOperationException()
        {
            // Aquí dejas el container en null
            var config = GetTestConfiguration(new Dictionary<string, string>
            {
                ["AzureStorage:Container"] = null
            });

            Assert.Throws<InvalidOperationException>(() => new BlobStorageService(config));
        }

        [Fact]
        public async Task GenerateReadSasTokenAsync_BlobDoesNotExist_ReturnsErrorMessage()
        {
            // Arrange
            var config = GetTestConfiguration();
            var service = new BlobStorageService(config);

            // Create a mock for BlobClient and set ExistsAsync to return false.
            var blobClientMock = new Mock<BlobClient>();
            blobClientMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, null));

            // Create a mock for BlobContainerClient to return our BlobClient mock.
            var containerClientMock = new Mock<BlobContainerClient>();
            containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            // Create a mock for BlobServiceClient to return our ContainerClient mock.
            var blobServiceClientMock = new Mock<BlobServiceClient>();
            blobServiceClientMock.Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerClientMock.Object);

            // Replace the internal BlobServiceClient with our mock.
            SetPrivateBlobServiceClient(service, blobServiceClientMock.Object);

            // Act
            var result = await service.GenerateReadSasTokenAsync("docs", "file.pdf", 30);

            // Assert
            Assert.Equal("The requested blob does not exist in the specified folder.", result);
        }

        [Fact]
        public async Task GenerateReadSasTokenAsync_BlobExists_ReturnsSasToken()
        {
            // Arrange
            var config = GetTestConfiguration();
            var service = new BlobStorageService(config);

            // Expected URI returned by the GenerateSasUri method.
            var expectedUri = new Uri("http://fake/blob/docs/file.pdf?sas=token");

            // Create a mock for BlobClient, setup ExistsAsync to return true and GenerateSasUri to return expectedUri.
            var blobClientMock = new Mock<BlobClient>();
            blobClientMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, null));
            blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedUri);

            // Create a mock for BlobContainerClient.
            var containerClientMock = new Mock<BlobContainerClient>();
            containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            // Create a mock for BlobServiceClient.
            var blobServiceClientMock = new Mock<BlobServiceClient>();
            blobServiceClientMock.Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerClientMock.Object);

            // Replace the internal BlobServiceClient with our mock.
            SetPrivateBlobServiceClient(service, blobServiceClientMock.Object);

            // Act
            var result = await service.GenerateReadSasTokenAsync("docs", "file.pdf", 30);

            // Assert
            Assert.Equal(expectedUri.ToString(), result);
        }

    }
}
