
using Newtonsoft.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    /// <summary>
    /// Represents a single chat message exchanged with the AI model.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the identifier for the message in the database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the role of the author of the message.
        /// </summary>
        public Role? Role { get; set; }

        /// <summary>
        /// Gets or sets the username associated with the message.
        /// </summary>
        [JsonIgnore]
        public string User { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the textual content of the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional base64 encoded image attached to the message.
        /// </summary>
        public string? Base64Image { get; set; }

        /// <summary>
        /// Gets or sets the time the message was created.
        /// </summary>
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
