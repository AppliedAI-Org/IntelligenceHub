using Azure.Search.Documents.Indexes;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.RAG
{
    public class IndexDefinition
    {
        public string Id { get; set; }
        public string Parent_Id { get; set; }
        public string Chunk_Id { get; set; }
        public string title { get; set; }
        public string chunk { get; set; }
        public string topic { get; set; }
        public string keywords { get; set; }
        public string source { get; set; }
        public DateTimeOffset created { get; set; }
        public DateTimeOffset modified { get; set; }
        public IReadOnlyList<float> TitleVector { get; set; }
        public IReadOnlyList<float> ContentVector { get; set; }
        public IReadOnlyList<float> TopicVector { get; set; }
        public IReadOnlyList<float> KeywordsVector { get; set; }
    }
}
