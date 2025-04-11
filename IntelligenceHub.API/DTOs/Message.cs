
using Newtonsoft.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    public class Message
    {
        public int Id { get; set; }
        public Role? Role { get; set; }
        [JsonIgnore]
        public string User { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Base64Image { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
