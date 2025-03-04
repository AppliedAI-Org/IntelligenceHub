
namespace IntelligenceHub.API.DTOs.Tools
{
    public class Parameters
    {
        public string type { get; set; } = "object";
        public Dictionary<string, Property> properties { get; set; } = new Dictionary<string, Property>();
        public string[]? required { get; set; } = new string[] { };
    }
}
