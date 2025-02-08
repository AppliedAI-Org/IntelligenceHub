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

        // used to assist with system message construction
        [JsonIgnore]
        internal readonly string _stringPropertyType = "string";
        [JsonIgnore]
        internal readonly string _objectPropertyType = "object";
    }
}
