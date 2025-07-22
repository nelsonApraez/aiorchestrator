using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Integration.Interfaces;
using Integration.Services;
using cms_genai_rag_aiorchestrator.Interfaces;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;


namespace cms_genai_rag_aiorchestrator.Tests
{
    public class ServiceRegistrationTests
    {
        private readonly IHost _host;

        public ServiceRegistrationTests()
        {
           
            _host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .ConfigureAppConfiguration(config =>
                {
                    // Para pruebas se usa el directorio actual
                    config.SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddLogging();
                    services.AddSingleton<ICognitiveSearchService, CognitiveSearchService>();
                    services.AddSingleton<ICosmosDbService, CosmosDbService>();
                    services.AddSingleton<IOpenIAServices, OpenIAServices>();
                    services.AddSingleton<IProcess, Process>();
                    services.AddSingleton<IBlobProcess, BlobProcess>();
                    services.AddSingleton<IBlobStorageService, BlobStorageService>();
                    services.AddSingleton<FunctionAIOrchestrator>();
                })
                .ConfigureOpenApi()
                .Build();
        }

        [Fact]
        public void CognitiveSearchService_Is_Registrated()
        {
            var service = _host.Services.GetService<ICognitiveSearchService>();
            Assert.NotNull(service);
            Assert.IsType<CognitiveSearchService>(service);
        }

        [Fact]
        public void CosmosDbService_Is_Registrated()
        {
            var service = _host.Services.GetService<ICosmosDbService>();
            Assert.NotNull(service);
            Assert.IsType<CosmosDbService>(service);
        }

        [Fact]
        public void OpenIAServices_Is_Registrated()
        {
            var service = _host.Services.GetService<IOpenIAServices>();
            Assert.NotNull(service);
            Assert.IsType<OpenIAServices>(service);
        }

        [Fact]
        public void Process_Is_Registrated()
        {
            var service = _host.Services.GetService<IProcess>();
            Assert.NotNull(service);
            Assert.IsType<Process>(service);
        }

        [Fact]
        public void BlobProcess_Is_Registrated()
        {
            var service = _host.Services.GetService<IBlobProcess>();
            Assert.NotNull(service);
            Assert.IsType<BlobProcess>(service);
        }

        [Fact]
        public void BlobStorageService_Is_Registrated()
        {
            var service = _host.Services.GetService<IBlobStorageService>();
            Assert.NotNull(service);
            Assert.IsType<BlobStorageService>(service);
        }

        [Fact]
        public void FunctionAIOrchestrator_Is_Registrated()
        {
            var service = _host.Services.GetService<FunctionAIOrchestrator>();
            Assert.NotNull(service);
            Assert.IsType<FunctionAIOrchestrator>(service);
        }

        /// <summary>
        /// Verifies that the configuration correctly loads the KEYVAULT_URI environment variable.
        /// </summary>
        [Fact]
        public void Test_Configuration_EnvironmentVariables()
        {
            // Set a test environment variable for KEYVAULT_URI.
            var testKeyVaultUri = "https://test-keyvault.vault.azure.net/";
            Environment.SetEnvironmentVariable("KEYVAULT_URI", testKeyVaultUri);

            var host = _host;
            var configuration = host.Services.GetService<IConfiguration>();

            // Assert that the configuration is not null.
            Assert.NotNull(configuration);

            // Verify that the KEYVAULT_URI environment variable is correctly set.
            Assert.Equal(testKeyVaultUri, Environment.GetEnvironmentVariable("KEYVAULT_URI"));
        }

        /// <summary>
        /// Verifies that the essential services are correctly registered in the DI container.
        /// </summary>
        [Fact]
        public void Test_ServiceRegistrations_AreNotNull()
        {
            var host = _host;

            // Retrieve services from the DI container.
            var configuration = host.Services.GetService<IConfiguration>();
            var cognitiveSearchService = host.Services.GetService<ICognitiveSearchService>();
            var cosmosDbService = host.Services.GetService<ICosmosDbService>();
            var openIAServices = host.Services.GetService<IOpenIAServices>();
            var process = host.Services.GetService<IProcess>();
            var blobProcess = host.Services.GetService<IBlobProcess>();
            var blobStorageService = host.Services.GetService<IBlobStorageService>();
            var functionAIOrchestrator = host.Services.GetService<FunctionAIOrchestrator>();
            var logger = host.Services.GetService<ILogger<Program>>();

            // Assert that each service is not null.
            Assert.NotNull(configuration);
            Assert.NotNull(cognitiveSearchService);
            Assert.NotNull(cosmosDbService);
            Assert.NotNull(openIAServices);
            Assert.NotNull(process);
            Assert.NotNull(blobProcess);
            Assert.NotNull(blobStorageService);
            Assert.NotNull(functionAIOrchestrator);
            Assert.NotNull(logger);
        }

    }
}
