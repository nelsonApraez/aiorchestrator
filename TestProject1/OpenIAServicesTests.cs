using Moq;
using Microsoft.Extensions.Configuration;
using Integration.Services;
using OpenAI.Chat;
using FluentAssertions;
using System.Reflection;
using OpenAI.Embeddings;
using Azure;
using System.ClientModel;
using cms.UnitTest;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests
{
    public class OpenAIServiceTests
    {
        private readonly OpenIAServices _openAIService;

        private readonly ServiceProvider _provider;

        public OpenAIServiceTests()
        {
            var config = TestConfig.Instance;

            // Instantiate the service using IConfiguration
            _openAIService = new OpenIAServices(
                config
            );

            // Se ejecuta 1 vez por cada prueba de esta clase
            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(config);

            services.AddTransient<OpenIAServices>();

            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void OpenIAServices_Esta_Registrado()
        {
            var service = _provider.GetService<OpenIAServices>();
            Assert.NotNull(service);
        }

        [Fact]
        public void Should_Initialize_Service_Correctly()
        {
            _openAIService.Should().NotBeNull();
        }


        [Fact]
        public void OpenAIService_ShouldThrowInvalidOperationException_WhenMissingConfiguration()
        {
            var invalidConfig = new Mock<IConfiguration>();
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new OpenIAServices(invalidConfig.Object));
            Assert.Contains("Missing", exception.Message);
        }

        [Fact]
        public async Task CreateChatCompletionTextAsync_ShouldThrowException_WhenChatClientIsNotInitialized()
        {
            var chatMessages = new List<ChatMessage>
            {
                CreateFakeChatMessage("Hello")
            };

            Func<Task> act = async () => await _openAIService.CreateChatCompletionTextAsync(chatMessages, It.IsAny<string>());
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        private ChatMessage CreateFakeChatMessage(string content)
        {
            var chatContent = new ChatMessageContent(content);
            return (ChatMessage)Activator.CreateInstance(typeof(ChatMessage), true)!;
        }

        // Helper para inyectar en campos privados mediante reflection.
        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        // Helper para crear la instancia del servicio con configuración "falsa" válida
        // y sobrescribir (por reflection) el embeddingClient con un mock.
        private (OpenIAServices service, Mock<EmbeddingClient> mockEmbeddingClient) CreateService()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"OpenIA:EndPoint", "https://fakeopenai.azure.com"},
                {"OpenIA:Key1", "fakeKey"},
                {"OpenIA:Model", "gpt-4"},
                {"OpenIA:ModelEmbedding", "text-embedding-ada-002"},
                {"OpenIA:maxTokens", "100"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Instanciamos el servicio (se creará internamente un AzureOpenAIClient real,
            // pero con datos "fake").
            var service = new OpenIAServices(config);

            // Creamos el mock para el EmbeddingClient.
            var mockEmbeddingClient = new Mock<EmbeddingClient>();

            // Sobrescribimos el campo privado "embeddingClient" con el mock.
            SetPrivateField(service, "embeddingClient", mockEmbeddingClient.Object);

            return (service, mockEmbeddingClient);
        }

        [Fact]
        public async Task GetEmbeddingAsync_ShouldReturnFloatList_WhenResponseIsValid()
        {
            // Arrange
            var (service, mockEmbeddingClient) = CreateService();

            mockEmbeddingClient
                .Setup(x => x.GenerateEmbeddingAsync(
                    It.IsAny<string>(),
                    It.IsAny<EmbeddingGenerationOptions>(),   
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(It.IsAny<ClientResult<OpenAIEmbedding>>());

            // Act
            var result = await service.GetEmbeddingAsync("Hello world");

            // Assert
            Assert.NotNull(result);
        }

    }
}
