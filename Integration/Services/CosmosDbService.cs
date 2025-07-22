using Integration.Interfaces;
using Integration.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;


namespace Integration.Services
{
    public class CosmosDbService : ICosmosDbService
    {

        private readonly Container _container;
        private readonly Container _containerDocumentHistory;

        public CosmosDbService(IConfiguration configuration)
        {
            var accountEndpoint = configuration["CosmosDb:Endpoint"]
                ?? throw new InvalidOperationException("Missing CosmosDb:Endpoint in configuration.");

            var databaseName = configuration["CosmosDb:DatabaseName"]
                ?? throw new InvalidOperationException("Missing CosmosDb:DatabaseName in configuration.");

            var containerName = configuration["CosmosDb:ContainerName"]
                ?? throw new InvalidOperationException("Missing CosmosDb:ContainerName in configuration.");

            var secretName = configuration["CosmosDb:KeyName"];

            var accountKey = !string.IsNullOrWhiteSpace(secretName)
                ? configuration[secretName] ?? configuration["CosmosDb:Key1"]
                : configuration["CosmosDb:Key1"]
                ?? throw new InvalidOperationException("Missing CosmosDb:Key1 in configuration.");


            var cosmosClient = new CosmosClient(accountEndpoint, accountKey);
            _container = cosmosClient.GetContainer(databaseName, containerName);
            _containerDocumentHistory = cosmosClient.GetContainer(databaseName, typeof(DocumentHistory).Name.ToLower());

        }

        public async Task CreateConversationAsync(Conversation conversation)
        {
            conversation.DateCreate = DateTime.UtcNow;
            conversation.DateModify = DateTime.UtcNow;
            conversation.id = Guid.NewGuid().ToString();

            await _container.CreateItemAsync(conversation, new PartitionKey(conversation.SessionId));
        }

        public async Task UpdateChatsAsync(string sessionId, string personId, List<Chat> chats)
        {
            var conversation = await GetConversationAsync(sessionId, personId, int.MaxValue);
            if (conversation == null)
                throw new Exception("Conversation not found.");

            conversation.Chats.AddRange(chats);
            conversation.DateModify = DateTime.UtcNow;

            await _container.UpsertItemAsync(conversation, new PartitionKey(sessionId));
        }

        public async Task<List<DocumentHistory>> GetLastDocumentHistory()
        {

            var query = _containerDocumentHistory.GetItemLinqQueryable<DocumentHistory>(
                allowSynchronousQueryExecution: true
            )
            .Take(50);

            var documentHistories = query.ToList();


            foreach (var doc in documentHistories)
            {
                if (DateTime.TryParse(doc.StartTimestamp, out var start))
                    doc.StartTimestamp = start.AddHours(-5).ToString("M/d/yyyy h:mm:ss tt");

                if (DateTime.TryParse(doc.StateTimestamp, out var state))
                    doc.StateTimestamp = state.AddHours(-5).ToString("M/d/yyyy h:mm:ss tt");
            }

            return documentHistories
                .OrderByDescending(x => x.StateTimestamp)
                .ToList();

        }

        public async Task<Conversation?> GetConversationAsync(string sessionId, string personId, int chatCount)

        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.SessionId = @sessionId AND c.PersonId = @personId")
                    .WithParameter("@sessionId", sessionId)
                    .WithParameter("@personId", personId);

                var iterator = _container.GetItemQueryIterator<Conversation>(query);
                var conversations = new List<Conversation>();

                while (iterator.HasMoreResults)
                {
                    try
                    {
                        var response = await iterator.ReadNextAsync();
                        conversations.AddRange(response);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Log the exception or handle gracefully
                        Console.WriteLine("No items found in the container.");
                        return null; // Or return a default Conversation object if needed
                    }
                }

                var conversation = conversations.FirstOrDefault();
                if (conversation != null && conversation.Chats.Count > chatCount)
                {
                    conversation.Chats = conversation.Chats
                        .OrderByDescending(c => c.Date)
                        .Take(chatCount)
                        .ToList();
                }

                return conversation;
            }
            catch (CosmosException ex)
            {
                // Handle Cosmos DB exceptions
                Console.WriteLine($"Cosmos DB error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

    }
}
