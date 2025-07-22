using Azure.Identity;
using Integration.Interfaces;
using Integration.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using cms_genai_rag_aiorchestrator;
using cms_genai_rag_aiorchestrator.Interfaces;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(config =>

    {var builtConfig = config.Build();

        config
            // Load local.settings.json for local development
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            // Load environment variables (takes priority over local.settings.json)
            .AddEnvironmentVariables();

        // Retrieve the Azure Key Vault endpoint from environment variables or configuration
        var keyVaultUri = builtConfig["KeyVault:VaultUri"] ?? Environment.GetEnvironmentVariable("KEYVAULT_URI");

        if (!string.IsNullOrEmpty(keyVaultUri)){var credential = new DefaultAzureCredential(); config.AddAzureKeyVault(new Uri(keyVaultUri), credential); }
    })
    .ConfigureServices((context, services) =>
    { var configuration = context.Configuration;   services.AddSingleton<IConfiguration>(configuration);


        // Register services for dependency injection
        services
            .AddLogging()
            .AddSingleton<ICognitiveSearchService, CognitiveSearchService>()
            .AddSingleton<ICosmosDbService, CosmosDbService>()
            .AddSingleton<IOpenIAServices, OpenIAServices>()
            .AddSingleton<IProcess, Process>()
            .AddSingleton<IBlobProcess, BlobProcess>()
            .AddSingleton<IBlobStorageService, BlobStorageService>()
            .AddSingleton<FunctionAIOrchestrator>();  })       

    .ConfigureOpenApi()
    .Build();

// Run the Azure Function host

await host.RunAsync();

