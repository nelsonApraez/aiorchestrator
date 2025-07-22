using Microsoft.Extensions.Configuration;
using Integration.Services;
using Microsoft.Extensions.DependencyInjection;
using Azure.Search.Documents.Models;
using Moq;
using Azure.Search.Documents;
using Azure;


namespace Integration.Tests
{
    public class CognitiveSearchServiceTests
    {
        private readonly ServiceProvider _provider;
        private readonly Mock<SearchClient> _mockSearchClient;

        public CognitiveSearchServiceTests()
        {

            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false)
                .Build();
            services.AddSingleton<IConfiguration>(config);


            _mockSearchClient = new Mock<SearchClient>();
            services.AddSingleton(_mockSearchClient.Object);

            services.AddTransient<CognitiveSearchService>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                return new CognitiveSearchService(configuration);
            });


            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CognitiveSearchService_Esta_Registrado()
        {
            var service = _provider.GetService<CognitiveSearchService>();
            Assert.NotNull(service);
        }

        private IConfiguration GetValidConfiguration(Dictionary<string, string> overrides = null)
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
        public void Constructor_MissingEndpoint_ThrowsInvalidOperationException()
        {
            var config = GetValidConfiguration(new Dictionary<string, string>
            {
                { "AzureAISearch:Endpoint", null }
            });

            Assert.Throws<InvalidOperationException>(() => new CognitiveSearchService(config));
        }

        [Fact]
        public void Constructor_MissingKey1_ThrowsInvalidOperationException()
        {
            var config = GetValidConfiguration(new Dictionary<string, string>
            {
                { "AzureAISearch:Key1", null },
                { "AzureAISearch:KeyName", "" }
            });

            Assert.Throws<InvalidOperationException>(() => new CognitiveSearchService(config));
        }

        [Fact]
        public void Constructor_MissingIndex_ThrowsInvalidOperationException()
        {
            var config = GetValidConfiguration(new Dictionary<string, string>
            {
                { "AzureAISearch:Index", null }
            });

            Assert.Throws<InvalidOperationException>(() => new CognitiveSearchService(config));
        }


        [Fact]
        public async Task SearchAsync_NullQueryAndEmbedding_ThrowsArgumentException()
        {
            var config = GetValidConfiguration();
            var service = new CognitiveSearchService(config);

            await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync(null, null));
        }


        [Fact]
        public async Task SearchAsync_ValidQuery_ReturnsExpectedResult()
        {
            var config = GetValidConfiguration();
            var service = new CognitiveSearchService(_mockSearchClient.Object);

            var mockResponse = new Mock<Response<SearchResults<SearchDocument>>>();
            var mockSearchResults = new Mock<SearchResults<SearchDocument>>();
            mockResponse.Setup(r => r.Value).Returns(mockSearchResults.Object);
            _mockSearchClient.Setup(c => c.SearchAsync<SearchDocument>(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var result = await service.SearchAsync("test query");

            Assert.NotNull(result);
        }

        [Fact]
        public async Task SearchAsync_ValidEmbedding_ReturnsExpectedResult()
        {
            var config = GetValidConfiguration();
            var service = new CognitiveSearchService(_mockSearchClient.Object);

            var mockResponse = new Mock<Response<SearchResults<SearchDocument>>>();
            var mockSearchResults = new Mock<SearchResults<SearchDocument>>();
            mockResponse.Setup(r => r.Value).Returns(mockSearchResults.Object);
            _mockSearchClient.Setup(c => c.SearchAsync<SearchDocument>(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var result = await service.SearchAsync(null, new float[] { 0.1f, 0.2f, 0.3f });

            Assert.NotNull(result);
        }

        [Fact]
        public async Task SearchAsync_SearchClientReturnsNull_ThrowsInvalidOperationException()
        {
            var config = GetValidConfiguration();
            var service = new CognitiveSearchService(_mockSearchClient.Object);

            var mockResponse = new Mock<Response<SearchResults<SearchDocument>>>();
            mockResponse.Setup(r => r.Value).Returns((SearchResults<SearchDocument>)null);
            _mockSearchClient.Setup(c => c.SearchAsync<SearchDocument>(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SearchAsync("test query"));
        }

    }
}
