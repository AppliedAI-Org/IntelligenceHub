using Newtonsoft.Json;

namespace IntelligenceHub.API.DTOs.Tools
{
    public class Property
    {
        [JsonIgnore]
        public int? ToolId { get; set; }
        public string type { get; set; }
        public string? description { get; set; }
    }
}
