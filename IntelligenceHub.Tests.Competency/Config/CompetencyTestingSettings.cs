namespace IntelligenceHub.Tests.Competency.Config
{
    public class CompetencyTestingSettings
    {
        public string ScoringProfile { get; set; }
        public string GenerativeProfile { get; set; }
        public int TestsPerCompletion { get; set; }
        public bool UseGeneratedCompletions { get; set; }
        public int GeneratedCompletionsPerBatch { get; set; }
    }
}
