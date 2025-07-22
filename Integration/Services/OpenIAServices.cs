using Azure.AI.OpenAI;
using Integration.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Integration.Services
{
    public class OpenIAServices : IOpenIAServices
    {

        private readonly int maxTokens;
        private readonly ChatClient chatClient;
        private readonly EmbeddingClient embeddingClient;

        public OpenIAServices(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var endpoint = configuration["OpenIA:EndPoint"]
                ?? throw new InvalidOperationException("Missing OPENIA:Endpoint in configuration.");

            var secretName = configuration["OpenIA:KeyName"];

            var key = !string.IsNullOrWhiteSpace(secretName)
                ? configuration[secretName] ?? configuration["OpenIA:Key1"]
                : configuration["OpenIA:Key1"]
                ?? throw new InvalidOperationException("Missing OpenIA:Key1 in configuration.");

            string model = configuration["OpenIA:Model"]
                ?? throw new InvalidOperationException("Missing Configuration OpenIA:Model in configuration.");

            string modelEmbedding = configuration["OpenIA:ModelEmbedding"]
                ?? throw new InvalidOperationException("Missing OpenIA:ModelEmbeddingin configuration.");

            maxTokens = Convert.ToInt32(configuration["OpenIA:maxTokens"]);
            var clientIA = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(key));
            embeddingClient = clientIA.GetEmbeddingClient(modelEmbedding);
            chatClient = clientIA.GetChatClient(model);

        }

        /// <summary>
        /// Creates a chat completion response in plain text format based on the provided list of chat messages.
        /// </summary>
        /// <param name="requests">The list of chat messages to be sent to the model.</param>
        /// <param name="modelFormat">The format of the model's response. Defaults to "text".</param>
        /// <returns>A task representing the asynchronous operation, with a string result containing the model's response.</returns>

        public async Task<string> CreateChatCompletionTextAsync(IList<ChatMessage> requests, string modelFormat = "text",
            CancellationToken cancellationToken = default)
        {
            var result = await CreateChatCompletionAsync(requests, modelFormat).ConfigureAwait(false);
            return result.Content[0].Text;}



        /// <summary>
        /// Creates a chat completion response as a full object, including metadata and additional information.
        /// </summary>
        /// <param name="requests">The list of chat messages to be sent to the model.</param>
        /// <param name="modelFormat">The format of the model's response. Defaults to "text".</param>
        /// <returns>A task representing the asynchronous operation, with a ChatCompletion object containing the model's response.</returns>

        private async Task<ChatCompletion> CreateChatCompletionAsync(IList<ChatMessage> requests, string modelFormat,

        CancellationToken cancellationToken = default)
        {
            ChatCompletion completion = await chatClient.CompleteChatAsync(requests,
              new ChatCompletionOptions()
              {
                  Temperature = (float)1,
                  FrequencyPenalty = (float)0,
                  PresencePenalty = (float)0,
                  MaxOutputTokenCount = maxTokens,
                  TopP = (float)1,
                  ResponseFormat = modelFormat != "text" ? ChatResponseFormat.CreateJsonObjectFormat() : ChatResponseFormat.CreateTextFormat()
              }, cancellationToken
            );
            return completion;}

        /// <summary>
        /// Generates an embedding vector for a given input string using the specified embedding model.
        /// </summary>
        /// <param name="input">The input text to generate the embedding for.</param>
        /// <returns>A task representing the asynchronous operation, with a list of floats representing the embedding vector.</returns>
        public async Task<IList<float>> GetEmbeddingAsync(string input,
        CancellationToken cancellationToken = default)

        {

            var response = await embeddingClient.GenerateEmbeddingAsync(input, null, cancellationToken).ConfigureAwait(false);
            // Validate the response and ensure no null values
            if (response?.Value == null)
            {

                return [];

            }

            // Use ToFloats() directly if it's available
            return response.Value.ToFloats().ToArray().ToList();
        }
    }
}
