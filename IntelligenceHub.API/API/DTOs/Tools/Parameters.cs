
namespace IntelligenceHub.API.DTOs.Tools
{
    public class Parameters
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, Property> Properties { get; set; } = new Dictionary<string, Property>();
        public string[]? Required { get; set; } = new string[] { };
    }
}
