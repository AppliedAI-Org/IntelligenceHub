using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    [Table("MessageHistory")]
    public class DbMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public Guid ConversationId {  get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Base64Image { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
