using Azure.Search.Documents.Indexes;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.RAG
{
    public class IndexDefinition
    {
        public string Id { get; set; }
        public string Parent_Id { get; set; }
        public string Chunk_Id { get; set; }
        public string Title { get; set; }
        public string chunk { get; set; }
        public string Topic { get; set; }
        public string Keywords { get; set; }
        public string Source { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
        public IReadOnlyList<float> TitleVector { get; set; }
        public IReadOnlyList<float> ContentVector { get; set; }
        public IReadOnlyList<float> TopicVector { get; set; }
        public IReadOnlyList<float> KeywordsVector { get; set; }
    }
}
