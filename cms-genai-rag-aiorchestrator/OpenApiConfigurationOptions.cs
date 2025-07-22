using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace cms_genai_rag_aiorchestrator
{
    public class OpenApiConfigurationOptions : IOpenApiConfigurationOptions
    {
        public OpenApiInfo Info { get; set; } =
          new OpenApiInfo
          {
              Title = "CMS RAG AI Orchestrator",
              Version = "1.0",

              Description = "This API allows you to orchestrate artificial intelligence services and manage files with blob services.",
              Contact = new OpenApiContact()
              {
                  Name = "Development Team",

                  Email = "dev@example.com"
              }
          };


        public List<OpenApiServer> Servers { get; set; } = [];


        public OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;

        public bool IncludeRequestingHostName { get; set; } = false;
        public bool ForceHttp { get; set; } = false;
        public bool ForceHttps { get; set; } = true;

        public List<IDocumentFilter> DocumentFilters { get; set; } = [];

        public bool ExcludeRequestingHost { get; set; } = false;
        public IOpenApiHttpTriggerAuthorization? Security { get; set; } 

    }
}
