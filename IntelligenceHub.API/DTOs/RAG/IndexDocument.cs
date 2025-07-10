namespace IntelligenceHub.API.DTOs.RAG
{
    /// <summary>
    /// Represents a document stored within a RAG index.
    /// </summary>
    public class IndexDocument
    {
        /// <summary>
        /// Gets or sets the document identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the document title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the textual content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional topic extracted from the content.
        /// </summary>
        public string? Topic { get; set; }

        /// <summary>
        /// Gets or sets optional keywords describing the document.
        /// </summary>
        public string? Keywords { get; set; }

        /// <summary>
        /// Gets or sets the original source of the document.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the document was created.
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Gets or sets when the document was last modified.
        /// </summary>
        public DateTimeOffset Modified { get; set; }
    }
}

