
namespace IntelligenceHub.API.DTOs
{
    public class CompletionRequest
    {
        public Guid? ConversationId { get; set; }
        public Profile ProfileOptions { get; set; } = new Profile();
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}
