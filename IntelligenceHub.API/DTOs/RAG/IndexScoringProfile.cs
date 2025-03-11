using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.RAG
{
    public class IndexScoringProfile
    {
        public string Name { get; set; } = string.Empty;
        public SearchAggregation? SearchAggregation {  get; set; }
        public SearchInterpolation? SearchInterpolation { get; set; }
        public double FreshnessBoost { get; set; }
        public int BoostDurationDays { get; set; }
        public double TagBoost { get; set; }
        public Dictionary<string, double>? Weights { get; set; } = new Dictionary<string, double>();
    }
}
