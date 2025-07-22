using System.Reflection;
using Microsoft.Azure.Cosmos;
using Moq;
using Integration.Models;
using Integration.Services;
using cms.UnitTest;


namespace Integration.Tests
{
    public class CosmosDbServiceTests
    {
        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }


        private (CosmosDbService service, Mock<Container> mockContainer, Mock<Container> mockContainerHistory) CreateService()
        {
            var config = TestConfig.Instance;

            var service = new CosmosDbService(config);

            var mockContainer = new Mock<Container>();
            var mockContainerHistory = new Mock<Container>();

            SetPrivateField(service, "_container", mockContainer.Object);
            SetPrivateField(service, "_containerDocumentHistory", mockContainerHistory.Object);

            return (service, mockContainer, mockContainerHistory);
        }

        [Fact]
        public async Task CreateConversationAsync_ShouldSetPropertiesAndCallCreateItemAsync()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            mockContainer
                .Setup(x => x.CreateItemAsync(
                    It.IsAny<Conversation>(),
                    It.IsAny<PartitionKey>(),
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ItemResponse<Conversation>)null);

            var conversation = new Conversation { SessionId = "session1", PersonId = "person1", Chats = new List<Chat>() };

            // Act
            await service.CreateConversationAsync(conversation);

            // Assert
            Assert.False(string.IsNullOrEmpty(conversation.id));
            Assert.NotEqual(default(DateTime), conversation.DateCreate);
            Assert.NotEqual(default(DateTime), conversation.DateModify);
        }

        [Fact]
        public async Task UpdateChatsAsync_ShouldAppendChatsAndCallUpsertItemAsync()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            var conversation = new Conversation
            {
                SessionId = "session1",
                PersonId = "person1",
                Chats = new List<Chat> { new Chat { Date = DateTime.UtcNow.AddMinutes(-10), Text = "Hola" } }
            };

            var feedResponseMock = new Mock<FeedResponse<Conversation>>();
            feedResponseMock.Setup(fr => fr.GetEnumerator())
                .Returns(() => (new List<Conversation> { conversation }).GetEnumerator());

            var feedIteratorMock = new Mock<FeedIterator<Conversation>>();
            feedIteratorMock.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            mockContainer
                .Setup(x => x.GetItemQueryIterator<Conversation>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            mockContainer
                .Setup(x => x.UpsertItemAsync(
                    It.IsAny<Conversation>(),
                    It.IsAny<PartitionKey>(),
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ItemResponse<Conversation>)null);

            var newChats = new List<Chat>
            {
                new Chat { Date = DateTime.UtcNow, Text = "Nuevo mensaje" }
            };

            // Act
            await service.UpdateChatsAsync("session1", "person1", newChats);

            // Assert
            Assert.Equal(2, conversation.Chats.Count);
        }

        [Fact]
        public async Task UpdateChatsAsync_WhenConversationNotFound_ShouldThrowException()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            var emptyFeedIteratorMock = new Mock<FeedIterator<Conversation>>();
            emptyFeedIteratorMock.Setup(x => x.HasMoreResults).Returns(false);

            mockContainer
                .Setup(x => x.GetItemQueryIterator<Conversation>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.IsAny<QueryRequestOptions>()))
                .Returns(emptyFeedIteratorMock.Object);

            var newChats = new List<Chat> { new Chat { Date = DateTime.UtcNow, Text = "Mensaje" } };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateChatsAsync("session1", "person1", newChats));
            Assert.Equal("Conversation not found.", ex.Message);
        }

        [Fact]
        public async Task GetConversationAsync_ShouldReturnConversationWithTrimmedChats()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            var conversation = new Conversation
            {
                SessionId = "session1",
                PersonId = "person1",
                Chats = new List<Chat>
                {
                    new Chat { Date = DateTime.UtcNow.AddMinutes(-30), Text = "Mensaje 1" },
                    new Chat { Date = DateTime.UtcNow.AddMinutes(-20), Text = "Mensaje 2" },
                    new Chat { Date = DateTime.UtcNow.AddMinutes(-10), Text = "Mensaje 3" }
                }
            };

            var feedResponseMock = new Mock<FeedResponse<Conversation>>();
            feedResponseMock.Setup(fr => fr.GetEnumerator())
                .Returns(() => (new List<Conversation> { conversation }).GetEnumerator());

            var feedIteratorMock = new Mock<FeedIterator<Conversation>>();
            feedIteratorMock.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            mockContainer
                .Setup(x => x.GetItemQueryIterator<Conversation>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);


            // Act

            var result = await service.GetConversationAsync("session1", "person1", 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Chats.Count);
            Assert.Equal("Mensaje 3", result.Chats.First().Text);
        }

        [Fact]
        public async Task GetLastDocumentHistory_ShouldReturnOrderedAndAdjustTimestamps()
        {
            
            var (service, _, mockContainerHistory) = CreateService();

            SupportingContentRecord supportingContentRecord = new SupportingContentRecord("","");  

            var testData = new List<DocumentHistory>
                {
                    new DocumentHistory
                    {
                        Id = "1",
                        StartTimestamp = "1/1/2025 10:00:00 AM",
                        StateTimestamp = "1/1/2025 10:00:00 AM",
                        StateDescription = "State 1"
                    },
                    new DocumentHistory
                    {
                        Id = "2",
                        StartTimestamp = "1/2/2025 8:00:00 AM",
                        StateTimestamp = "1/2/2025 8:00:00 AM",
                        StateDescription = "State 1"
                    }
                };

            
            var orderedQueryableData = testData
                .AsQueryable()
                .OrderByDescending(x => x.StateTimestamp);

            mockContainerHistory
                .Setup(x => x.GetItemLinqQueryable<DocumentHistory>(
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>(),
                    null)) // Explicitly passing null for the optional parameter
                .Returns(orderedQueryableData);

            // 5) Mock  FeedResponse
            var mockFeedResponse = new Mock<FeedResponse<DocumentHistory>>();
            mockFeedResponse
                .Setup(fr => fr.GetEnumerator())
                .Returns(() => testData.GetEnumerator());

            // 6) Mock  FeedIterator
            var mockFeedIterator = new Mock<FeedIterator<DocumentHistory>>();
            mockFeedIterator
                .SetupSequence(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFeedResponse.Object) // Primera página con datos
                .ReturnsAsync((FeedResponse<DocumentHistory>)null); // Fin

           
            mockContainerHistory
                .Setup(x => x.GetItemQueryIterator<DocumentHistory>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            // ACT
            var result = await service.GetLastDocumentHistory();

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            
            Assert.Equal("2", result[0].Id);
            Assert.Equal("1", result[1].Id);

           
            // "1/2/2025 8:00:00 AM" - 5h = "1/2/2025 3:00:00 AM"
            var originalState = DateTime.Parse("1/2/2025 8:00:00 AM");
            var expected = originalState.AddHours(-5); // 3:00 AM
            var actual = DateTime.Parse(result[0].StateTimestamp);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GetConversationAsync_WhenNoConversationFound_ReturnsNull()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            
            var feedResponseMock = new Mock<FeedResponse<Conversation>>();
            feedResponseMock.Setup(fr => fr.GetEnumerator())
                .Returns(new List<Conversation>().GetEnumerator()); // Lista vacía

           
            var feedIteratorMock = new Mock<FeedIterator<Conversation>>();
            feedIteratorMock.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            
            mockContainer
                .Setup(x => x.GetItemQueryIterator<Conversation>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act
            var result = await service.GetConversationAsync("session1", "person1", 2);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetConversationAsync_WhenChatsAreFewerThanChatCount_ShouldNotTrimChats()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            var conversation = new Conversation
            {
                SessionId = "session1",
                PersonId = "person1",
                Chats = new List<Chat>
        {
            new Chat { Date = DateTime.UtcNow.AddMinutes(-30), Text = "Mensaje A" },
            new Chat { Date = DateTime.UtcNow.AddMinutes(-20), Text = "Mensaje B" }
        }
            };

            var feedResponseMock = new Mock<FeedResponse<Conversation>>();
            feedResponseMock.Setup(fr => fr.GetEnumerator())
                .Returns(new List<Conversation> { conversation }.GetEnumerator());

            var feedIteratorMock = new Mock<FeedIterator<Conversation>>();
            feedIteratorMock.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            mockContainer
                .Setup(x => x.GetItemQueryIterator<Conversation>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act
            var result = await service.GetConversationAsync("session1", "person1", 5);

            // Assert
            Assert.NotNull(result);
            
            Assert.Equal(2, result.Chats.Count);
            Assert.Contains(result.Chats, c => c.Text == "Mensaje A");
            Assert.Contains(result.Chats, c => c.Text == "Mensaje B");
        }

        [Fact]
        public async Task GetConversationAsync_WhenMultipleConversationsReturned_ReturnsFirstOne()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            var conversation1 = new Conversation
            {
                SessionId = "session1",
                PersonId = "person1",
                Chats = new List<Chat> { new Chat { Date = DateTime.UtcNow, Text = "Conv1-Chat1" } }
            };

            var conversation2 = new Conversation
            {
                SessionId = "session1",
                PersonId = "person1",
                Chats = new List<Chat> { new Chat { Date = DateTime.UtcNow, Text = "Conv2-Chat1" } }
            };

            var feedResponseMock = new Mock<FeedResponse<Conversation>>();
            feedResponseMock.Setup(fr => fr.GetEnumerator())
                .Returns(new List<Conversation> { conversation1, conversation2 }.GetEnumerator());

            var feedIteratorMock = new Mock<FeedIterator<Conversation>>();
            feedIteratorMock.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            mockContainer
                .Setup(x => x.GetItemQueryIterator<Conversation>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act
            var result = await service.GetConversationAsync("session1", "person1", 10);

            // Assert
            Assert.NotNull(result);
            
            Assert.Equal("Conv1-Chat1", result.Chats.First().Text);
        }

        [Fact]
        public async Task GetConversationAsync_WhenCosmosExceptionIsNot404_ShouldThrow()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            var feedIteratorMock = new Mock<FeedIterator<Conversation>>();
            feedIteratorMock.Setup(x => x.HasMoreResults).Returns(true);

            
            feedIteratorMock
                .Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Internal Server Error", System.Net.HttpStatusCode.InternalServerError, 0, "", 0));

            mockContainer
                .Setup(x => x.GetItemQueryIterator<Conversation>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<CosmosException>(() =>
                service.GetConversationAsync("session1", "person1", 2));
        }

        [Fact]
        public async Task GetConversationAsync_WhenCosmosExceptionIs404_ShouldReturnNull()
        {
            // Arrange
            var (service, mockContainer, _) = CreateService();

            var feedIteratorMock = new Mock<FeedIterator<Conversation>>();
            feedIteratorMock.Setup(x => x.HasMoreResults).Returns(true);

            
            feedIteratorMock
                .Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not Found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            mockContainer
                .Setup(x => x.GetItemQueryIterator<Conversation>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act
            var result = await service.GetConversationAsync("session1", "person1", 2);

            // Assert
            Assert.Null(result);
        }

    }
}
