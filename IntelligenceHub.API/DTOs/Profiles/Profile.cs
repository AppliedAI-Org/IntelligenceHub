using IntelligenceHub.API.DTOs.Tools;
using Newtonsoft.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    public class Profile
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public AGIServiceHosts Host { get; set; }
        public AGIServiceHosts? ImageHost { get; set; }
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? MaxTokens { get; set; }
        public int? TopLogprobs { get; set; }
        public bool? Logprobs { get; set; }
        public string? User { get; set; }
        public string? ToolChoice { get; set; }
        public string? ResponseFormat { get; set; }
        public string? SystemMessage { get; set; }
        public string[]? Stop { get; set; }
        public List<Tool>? Tools { get; set; }
        public int? MaxMessageHistory { get; set; }
        public string[]? ReferenceProfiles { get; set; }
    }
}
