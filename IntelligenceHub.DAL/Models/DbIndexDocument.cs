using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    /// <summary>
    /// Entity model representing a document stored in a RAG index.
    /// </summary>
    public class DbIndexDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary>
        /// Primary key for the document.
        /// </summary>
        public int Id { get; set; }
        [Required]
        /// <summary>
        /// Gets or sets the title of the document.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        [Required]
        /// <summary>
        /// Gets or sets the content body stored in the index.
        /// </summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Optional topic extracted from the document.
        /// </summary>
        public string? Topic { get; set; }
        /// <summary>
        /// Optional keywords extracted from the document.
        /// </summary>
        public string? Keywords { get; set; }
        [Required]
        /// <summary>
        /// Gets or sets the source identifier for the document.
        /// </summary>
        public string Source { get; set; } = string.Empty;
        [Required]
        /// <summary>
        /// Timestamp indicating when the document was created.
        /// </summary>
        public DateTimeOffset Created { get; set; }
        [Required]
        /// <summary>
        /// Timestamp indicating when the document was last modified.
        /// </summary>
        public DateTimeOffset Modified { get; set; }
    }
}
