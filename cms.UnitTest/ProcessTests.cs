using System.Text.Json;
using Azure.Security.KeyVault.Secrets;
using Integration.Interfaces;
using Integration.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace cms_genai_rag_aiorchestrator.Tests
{
    public class ProcessTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<SecretClient> _mockSecretClient;
        private readonly Mock<IOpenIAServices> _mockOpenIAServices;
        private readonly Mock<ICosmosDbService> _mockCosmosDbService;
        private readonly Mock<ICognitiveSearchService> _mockCognitiveSearchService;
        private readonly Process _process;

        public ProcessTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockSecretClient = new Mock<SecretClient>();
            _mockOpenIAServices = new Mock<IOpenIAServices>();
            _mockCosmosDbService = new Mock<ICosmosDbService>();
            _mockCognitiveSearchService = new Mock<ICognitiveSearchService>();

            _process = new Process(
                _mockConfiguration.Object,
                _mockSecretClient.Object,
                _mockOpenIAServices.Object,
                _mockCosmosDbService.Object,
                _mockCognitiveSearchService.Object
            );
        }

        [Fact]
        public async Task RunProcessAsync_ShouldReturnJsonElement_WhenSuccessful()
        {
            // Arrange
            var requestBody = new RequestBody
            {
                Conversation_id = "conversationId",
                Person_id = "personId",
                Query = "query"
            };

            _mockConfiguration.Setup(c => c["CosmosDb:AmountHistoricalChatToRecover"]).Returns("10");
            _mockConfiguration.Setup(c => c["Prompt:InitialTemplateSystem"]).Returns("InitialTemplateSystem");
            _mockConfiguration.Setup(c => c["Prompt:FinalTemplateSystem"]).Returns("FinalTemplateSystem");
            _mockConfiguration.Setup(c => c["Prompt:FinalTemplateUser"]).Returns("FinalTemplateUser");

            _mockOpenIAServices.Setup(s => s.CreateChatCompletionTextAsync(It.IsAny<IList<ChatMessage>>(), "text", It.IsAny<CancellationToken>()))
                .ReturnsAsync("query");

            _mockOpenIAServices.Setup(s => s.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<float> { 0.1f, 0.2f });

            _mockCognitiveSearchService.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("documentContent");

            _mockCosmosDbService.Setup(s => s.GetConversationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Conversation { Chats = new List<Chat> { new Chat { Text = "chatText", Mode = "User" } } });

            _mockCosmosDbService.Setup(s => s.CreateConversationAsync(It.IsAny<Conversation>())).Returns(Task.CompletedTask);
            _mockCosmosDbService.Setup(s => s.UpdateChatsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<Chat>>())).Returns(Task.CompletedTask);

            // Act
            var result = await _process.RunProcessAsync(requestBody);

            // Assert
            Assert.IsType<JsonElement>(result);
        }

    }
}
