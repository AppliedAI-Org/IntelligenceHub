using Newtonsoft.Json;

namespace IntelligenceHub.API.DTOs.Tools
{
    public class Property
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string Type { get; set; }
        public string? Description { get; set; }
    }
}
