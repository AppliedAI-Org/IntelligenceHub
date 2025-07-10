using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.RAG
{
    /// <summary>
    /// Configuration used to weight and boost search results.
    /// </summary>
    public class IndexScoringProfile
    {
        /// <summary>
        /// Gets or sets the name of the scoring profile.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the aggregation method used when combining scores.
        /// </summary>
        public SearchAggregation? SearchAggregation { get; set; }

        /// <summary>
        /// Gets or sets how scores are interpolated.
        /// </summary>
        public SearchInterpolation? SearchInterpolation { get; set; }

        /// <summary>
        /// Gets or sets the boost applied for recent documents.
        /// </summary>
        public double FreshnessBoost { get; set; }

        /// <summary>
        /// Gets or sets the number of days a freshness boost should last.
        /// </summary>
        public int BoostDurationDays { get; set; }

        /// <summary>
        /// Gets or sets the boost applied for tag matches.
        /// </summary>
        public double TagBoost { get; set; }

        /// <summary>
        /// Gets or sets weight values for individual fields.
        /// </summary>
        public Dictionary<string, double>? Weights { get; set; } = new Dictionary<string, double>();
    }
}
