using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    /// <summary>
    /// Entity model representing a message stored in the database.
    /// </summary>
    [Table("MessageHistory")]
    public class DbMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary>
        /// Primary key for the message entry.
        /// </summary>
        public int Id { get; set; }
        [Required]
        /// <summary>
        /// Conversation identifier to which this message belongs.
        /// </summary>
        public Guid ConversationId { get; set; }
        /// <summary>
        /// Role of the message sender (e.g., system, user, assistant).
        /// </summary>
        public string Role { get; set; } = string.Empty;
        /// <summary>
        /// Optional username of the sender.
        /// </summary>
        public string User { get; set; } = string.Empty;
        /// <summary>
        /// Base64-encoded representation of an image attached to the message.
        /// </summary>
        public string? Base64Image { get; set; }
        /// <summary>
        /// Timestamp of when the message was sent.
        /// </summary>
        public DateTime TimeStamp { get; set; }
        /// <summary>
        /// Text content of the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
