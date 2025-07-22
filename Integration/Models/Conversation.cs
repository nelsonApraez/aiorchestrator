namespace Integration.Models
{
    /// <summary>
    /// Represents a conversation record with associated metadata, tags, and chat history.
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// Unique identifier for the conversation.
        /// </summary>

        public string? id { get; set; }


        /// <summary>
        /// Identifier for the conversation thread.
        /// </summary>

        public string? SessionId { get; set; }


        /// <summary>
        /// Identifier for the person involved in the conversation.
        /// </summary>

        public string? PersonId { get; set; }


        /// <summary>
        /// The date and time when the conversation was created.
        /// </summary>
        public DateTime DateCreate { get; set; }

        /// <summary>
        /// The date and time when the conversation was last modified.
        /// </summary>
        public DateTime DateModify { get; set; }

        /// <summary>
        /// Collection of tags associated with the conversation.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Collection of chat messages within the conversation.
        /// </summary>
        public List<Chat> Chats { get; set; } = new List<Chat>();
    }

    /// <summary>
    /// Represents a chat message within a conversation.
    /// </summary>
    public class Chat
    {
        /// <summary>
        /// The content of the chat message.
        /// </summary>

        public string? Text { get; set; }


        /// <summary>
        /// Indicates whether the message is from the user or the system.
        /// </summary>

        public string? Mode { get; set; }


        /// <summary>
        /// The date and time when the message was sent, in UTC.
        /// </summary>
        public DateTime Date { get; set; }
    }
}
