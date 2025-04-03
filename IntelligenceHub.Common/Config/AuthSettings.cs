namespace IntelligenceHub.Common.Config
{
    public class AuthSettings
    {
        public string Domain { get; set; }
        public string Audience { get; set; }
        public string BasicUsername { get; set; }
        public string BasicPassword { get; set; }
        public string DefaultClientId { get; set; }
        public string DefaultClientSecret { get; set; }
        public string AdminClientId { get; set; }
        public string AdminClientSecret { get; set; }
    }
}
