using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using cms_genai_rag_aiorchestrator;

namespace cms_genai_rag_aiorchestrator.Tests
{
    public class OpenApiConfigurationOptionsTests
    {
        [Fact]
        public void OpenApiConfigurationOptions_DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange
            var options = new OpenApiConfigurationOptions();

            // Act & Assert
            Assert.NotNull(options.Info);
            Assert.Equal("CMS RAG AI Orchestrator", options.Info.Title);
            Assert.Equal("1.0", options.Info.Version);
            Assert.Equal("This API allows you to orchestrate artificial intelligence services and manage files with blob services.", options.Info.Description);
            Assert.NotNull(options.Info.Contact);
            Assert.Equal("Development Team", options.Info.Contact.Name);
            Assert.Equal("dev@example.com", options.Info.Contact.Email);

            Assert.NotNull(options.Servers);
            Assert.Empty(options.Servers);

            Assert.Equal(OpenApiVersionType.V3, options.OpenApiVersion);
            Assert.False(options.IncludeRequestingHostName);
            Assert.False(options.ForceHttp);
            Assert.True(options.ForceHttps);
            Assert.NotNull(options.DocumentFilters);
            Assert.Empty(options.DocumentFilters);
            Assert.False(options.ExcludeRequestingHost);
            Assert.Null(options.Security);
        }
    }
}
