using System.Text.Json.Serialization;

namespace IntelligenceHub.API.DTOs.Tools
{
    public class Tool
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Type { get; private set; } = "function";
        public Function Function { get; set; } = new Function();
        public string? ExecutionUrl { get; set; }
        public string? ExecutionMethod { get; set; }
        public string? ExecutionBase64Key { get; set; }
    }
}
