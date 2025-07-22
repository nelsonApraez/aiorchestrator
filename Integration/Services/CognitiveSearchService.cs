using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Integration.Interfaces;
using Integration.Models;
using Microsoft.Extensions.Configuration;

namespace Integration.Services
{
    public class CognitiveSearchService : ICognitiveSearchService
    {
        private readonly SearchClient searchClient;

        // Constructor with IConfiguration to load configuration values
        public CognitiveSearchService(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var endpoint = configuration["AzureAISearch:Endpoint"]
                ?? throw new InvalidOperationException("Missing AzureAISearch:Endpoint in configuration.");

            var secretName = configuration["AzureAISearch:KeyName"];

            var apiKey = !string.IsNullOrWhiteSpace(secretName)
                ? configuration[secretName] ?? configuration["AzureAISearch:Key1"]
                : configuration["AzureAISearch:Key1"]
                ?? throw new InvalidOperationException("Missing AzureAISearch:Key1 in configuration.");

            string indexName = configuration["AzureAISearch:Index"]
                ?? throw new InvalidOperationException("Missing AzureAISearch:Index in configuration.");


            var endpointUri = new Uri(endpoint);
            searchClient = new SearchClient(endpointUri, indexName, new AzureKeyCredential(apiKey));

        }

        // Constructor to unit tests
        public CognitiveSearchService(SearchClient searchClient)
        {
            this.searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        }

        public async Task<string> SearchAsync(
            string? query = null,
            float[]? embedding = null,
            CancellationToken cancellationToken = default)
        {
            if (query is null && embedding is null)
            {
                throw new ArgumentException("Either query or embedding must be provided");
            }

            var top = 3; //TODO: pending definition of value

            SearchOptions searchOptions =
                new SearchOptions
                {
                    Filter = string.Empty,
                    Size = top,
                };

            if (embedding != null)
            {                
                var vectorQuery = new VectorizedQuery(embedding)
                {
                    // if semantic ranker is enabled, we need to set the rank to a large number to get more
                    // candidates for semantic reranking
                    KNearestNeighborsCount = top,
                };
                vectorQuery.Fields.Add("contentVector");
                searchOptions.VectorSearch = new();
                searchOptions.VectorSearch.Queries.Add(vectorQuery);
            }

            // Execute the query
            var searchResultResponse = await searchClient.SearchAsync<SearchDocument>(
                query, searchOptions, cancellationToken);

            if (searchResultResponse.Value is null)
            {
                throw new InvalidOperationException("fail to get search result");
            }

            SearchResults<SearchDocument> searchResult = searchResultResponse.Value;

            // Assemble sources here.
            // Example output for each SearchDocument:
            // 
            //   "@search.score": 11.65396,
            //   "id": "Northwind_Standard_Benefits_Details_pdf-60",
            //   "content": "x-ray, lab, or imaging service, you will likely be responsible for paying a copayment or coinsurance. The exact amount you will be required to pay will depend on the type of service you receive. You can use the Northwind app or website to look up the cost of a particular service before you receive it.\nIn some cases, the Northwind Standard plan may exclude certain diagnostic x-ray, lab, and imaging services. For example, the plan does not cover any services related to cosmetic treatments or procedures. Additionally, the plan does not cover any services for which no diagnosis is provided.\nIt’s important to note that the Northwind Standard plan does not cover any services related to emergency care. This includes diagnostic x-ray, lab, and imaging services that are needed to diagnose an emergency condition. If you have an emergency condition, you will need to seek care at an emergency room or urgent care facility.\nFinally, if you receive diagnostic x-ray, lab, or imaging services from an out-of-network provider, you may be required to pay the full cost of the service. To ensure that you are receiving services from an in-network provider, you can use the Northwind provider search ",
            //   "category": null,
            //   "chunk_file": "Northwind_Standard_Benefits_Details-24.pdf",
            //   "file_name": "Northwind_Standard_Benefits_Details.pdf"
            
            var sb = new List<SupportingContentRecord>();
            foreach (var doc in searchResult.GetResults())
            {

                doc.Document.TryGetValue("chunk_file", out var sourcePageValue); doc.Document.TryGetValue("chunk_uri", out var sourceUriValue); doc.Document.TryGetValue("file_name", out var sourceDocumentValue); doc.Document.TryGetValue("file_uri", out var sourceDocumentUriValue);
                string? contentValue;
                try
                { doc.Document.TryGetValue("content", out var value); contentValue = (string)value;}
                catch (ArgumentNullException){ contentValue = null;}


                if (sourcePageValue is string sourcePage && contentValue is string content &&
                    sourceUriValue is string sourceUri && sourceDocumentValue is string sourceDocument
                    && sourceDocumentUriValue is string sourceDocumentUri)

                { content = content.Replace('\r', ' ').Replace('\n', ' ');sb.Add(new SupportingContentRecord(sourcePage + " Uri: " + sourceUri + " Document: " + sourceDocument + " Uri document: " + sourceDocumentUri, content));}

            }

            string documentContents = string.Empty;
            if (!sb.Any())
            {
                documentContents = "no source available.";
            }

            else{ documentContents = string.Join("\r", sb.Select(x => $"{x.Title}:{x.Content}"));}


            return documentContents;
        }
    }
}

