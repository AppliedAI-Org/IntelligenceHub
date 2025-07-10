namespace IntelligenceHub.API.DTOs
{
    /// <summary>
    /// Represents a request for an AI completion.
    /// </summary>
    public class CompletionRequest
    {
        /// <summary>
        /// Gets or sets an optional conversation identifier allowing message history to be persisted.
        /// </summary>
        public Guid? ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the profile configuration used for the request.
        /// </summary>
        public Profile ProfileOptions { get; set; } = new Profile();

        /// <summary>
        /// Gets or sets the list of chat messages comprising the conversation.
        /// </summary>
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}

