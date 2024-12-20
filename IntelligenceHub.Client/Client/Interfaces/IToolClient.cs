namespace IntelligenceHub.Client.Interfaces
{
    public interface IToolClient
    {
        Task<HttpResponseMessage> CallFunction(string toolName, string toolArgs, string endpoint, string? httpMethod = "Post", string? key = null);
    }
}
