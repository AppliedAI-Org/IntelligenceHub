
namespace IntelligenceHub.API.DTOs.Tools
{
    public class Function
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public Parameters Parameters { get; set; } = new Parameters();
    }
}
