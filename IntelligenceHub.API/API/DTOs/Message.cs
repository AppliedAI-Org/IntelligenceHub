
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    public class Message
    {
        public Role? Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Base64Image { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
