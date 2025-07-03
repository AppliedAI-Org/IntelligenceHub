using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    public class DbIndexDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Content { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public string? Keywords { get; set; }
        [Required]
        public string Source { get; set; } = string.Empty;
        [Required]
        public DateTimeOffset Created { get; set; }
        [Required]
        public DateTimeOffset Modified { get; set; }
    }
}
