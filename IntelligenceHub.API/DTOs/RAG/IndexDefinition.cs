using Azure.Search.Documents.Indexes;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.RAG
{
    /// <summary>
    /// Represents a document definition used when upserting into an index.
    /// </summary>
    public class IndexDefinition
    {
        /// <summary>
        /// Gets or sets the identifier of the document.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the parent document identifier.
        /// </summary>
        public string Parent_Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the chunk.
        /// </summary>
        public string Chunk_Id { get; set; }

        /// <summary>
        /// Gets or sets the document title.
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// Gets or sets the chunk of content.
        /// </summary>
        public string chunk { get; set; }

        /// <summary>
        /// Gets or sets the topic extracted from the content.
        /// </summary>
        public string topic { get; set; }

        /// <summary>
        /// Gets or sets the keywords extracted from the content.
        /// </summary>
        public string keywords { get; set; }

        /// <summary>
        /// Gets or sets the original content source.
        /// </summary>
        public string source { get; set; }

        /// <summary>
        /// Gets or sets the timestamp for when the document was created.
        /// </summary>
        public DateTimeOffset created { get; set; }

        /// <summary>
        /// Gets or sets the timestamp for when the document was last modified.
        /// </summary>
        public DateTimeOffset modified { get; set; }

        /// <summary>
        /// Gets or sets the vector representation of the title.
        /// </summary>
        public IReadOnlyList<float> TitleVector { get; set; }

        /// <summary>
        /// Gets or sets the vector representation of the content.
        /// </summary>
        public IReadOnlyList<float> ContentVector { get; set; }

        /// <summary>
        /// Gets or sets the vector representation of the topic.
        /// </summary>
        public IReadOnlyList<float> TopicVector { get; set; }

        /// <summary>
        /// Gets or sets the vector representation of the keywords.
        /// </summary>
        public IReadOnlyList<float> KeywordsVector { get; set; }
    }
}
