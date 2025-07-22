using Integration.Models;

namespace Integration.Interfaces
{
    public interface ICosmosDbService
    {
        /// <summary>
        /// Creates a new conversation in the database.
        /// </summary>
        Task CreateConversationAsync(Conversation conversation);

        /// <summary>
        /// Updates the chats for an existing conversation.
        /// </summary>
        Task UpdateChatsAsync(string sessionId, string personId, List<Chat> chats);

        /// <summary>
        /// Retrieves a conversation by SessionId and PersonId, including the last N chats.
        /// </summary>

        Task<Conversation?> GetConversationAsync(string sessionId, string personId, int chatCount);


        /// <summary>
        /// Retrieves the last 50 document history entries.
        /// </summary>
        /// <returns></returns>
        Task<List<DocumentHistory>> GetLastDocumentHistory();

    }
}
