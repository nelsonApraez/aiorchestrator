using Integration.Models;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Integration.Interfaces;
using System.Text;
using cms_genai_rag_aiorchestrator.Interfaces;
using cms_genai_rag_aiorchestrator.Contracts;

namespace cms_genai_rag_aiorchestrator
{
    public class Process : IProcess
    {
        private readonly IConfiguration _configuration;

        private readonly IOpenIAServices _openIAServices;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ICognitiveSearchService _cognitiveSearchService;


        public Process(IConfiguration configuration, IOpenIAServices openIAServices, ICosmosDbService cosmosDbService, ICognitiveSearchService cognitiveSearchService)
        {
            _configuration = configuration;

            _openIAServices = openIAServices;
            _cosmosDbService = cosmosDbService;
            _cognitiveSearchService = cognitiveSearchService;
        }

        public async Task<JsonElement> RunProcessAsync(RequestBody requestBody)
        {
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                string AmountHistoricalChatToRecover = _configuration["CosmosDb:AmountHistoricalChatToRecover"]
                    ?? throw new InvalidOperationException("Missing CosmosDb:AmountHistoricalChatToRecover in configuration");              
                string InitialTemplateSystem = _configuration["Prompt:InitialTemplateSystem"]
                    ?? throw new InvalidOperationException("Missing Prompt:InitialTemplateSystem in configuration");             
                string FinalTemplateSystem = _configuration["Prompt:FinalTemplateSystem"]
                    ?? throw new InvalidOperationException("Missing Prompt:FinalTemplateSystem in configuration");      
                string FinalTemplateUser = _configuration["Prompt:FinalTemplateUser"]
                    ?? throw new InvalidOperationException("Missing Prompt:FinalTemplateUser in configuration");
                

                Console.WriteLine("start openAI gpt4o first");
                var query = await _openIAServices.CreateChatCompletionTextAsync(
                [
                    SystemChatMessage.CreateSystemMessage(InitialTemplateSystem),
                    SystemChatMessage.CreateUserMessage(requestBody.Query),
                ]
                , "text", cancellationToken);
                Console.WriteLine("end openAI gpt4o first");
                Console.WriteLine("question enhance: " + query);

                //extract embedding 
                Console.WriteLine("start openAI Embedding");
                var responseEmbedding = await _openIAServices.GetEmbeddingAsync(requestBody.Query, cancellationToken);
                Console.WriteLine("end openAI Embedding");
                Console.WriteLine("start SearchService");
                //send to ia search: query + embedding
                string documentContent = await _cognitiveSearchService.SearchAsync(query, responseEmbedding.ToArray(), cancellationToken);
                Console.WriteLine("end SearchService");

                Conversation conversation = await _cosmosDbService.GetConversationAsync(requestBody.Conversation_id, requestBody.Person_id, Convert.ToInt16(AmountHistoricalChatToRecover));

                string chatHistory = string.Empty;

                if (conversation == null)
                    chatHistory = "no chat history available.";
                else
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var chat in conversation.Chats)
                    {
                        stringBuilder.AppendLine(chat.Mode + ": " + chat.Text);
                    }
                    chatHistory = stringBuilder.ToString();
                }


                var finalPrompt = FinalTemplateUser
                        .Replace("{0}", requestBody.Query)
                        .Replace("{1}", chatHistory)
                        .Replace("{2}", documentContent);


                Console.WriteLine("start openAI gpt4o second");
                var finalAnswer = await _openIAServices.CreateChatCompletionTextAsync(
                [
                    SystemChatMessage.CreateSystemMessage(FinalTemplateSystem),
                    SystemChatMessage.CreateUserMessage(finalPrompt),
                ]
                , "text", cancellationToken);
                Console.WriteLine("end openAI gpt4o second");

                var answerJson = finalAnswer ?? throw new InvalidOperationException("Failed to get search query");
                var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);

                // Update conversation
                var newChats = new List<Chat>
                {
                    new Chat { Text = requestBody.Query, Mode = AuthorRole.User.ToString(), Date = DateTime.UtcNow },
                    new Chat { Text = finalAnswer, Mode = AuthorRole.System.ToString(), Date = DateTime.UtcNow }
                };

                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        SessionId = requestBody.Conversation_id,
                        PersonId = requestBody.Person_id,
                        Chats = newChats
                    };
                    await _cosmosDbService.CreateConversationAsync(conversation);
                }
                else
                {
                    await _cosmosDbService.UpdateChatsAsync(requestBody.Conversation_id, requestBody.Person_id, newChats);
                }

                Console.WriteLine("Final answer: " + finalAnswer);

                return answerObject;

            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

    }
}
