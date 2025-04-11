namespace IntelligenceHub.Client.Interfaces
{
    /// <summary>
    /// A client for executing tools at external endpoints.
    /// </summary>
    public interface IToolClient
    {
        /// <summary>
        /// Calls a tool at the specified endpoint.
        /// </summary>
        /// <param name="toolName">The name of the tool being executed.</param>
        /// <param name="toolArgs">The arguments to be used as the request body for tool execution.</param>
        /// <param name="endpoint">The endpoint at which the tool will be executed.</param>
        /// <param name="httpMethod">The type of http method to execute at the endpoint.</param>
        /// <param name="key">A base64 password if the API endpoint requires one.</param>
        /// <returns>The response message returned from the endpoint.</returns>
        Task<HttpResponseMessage> CallFunction(string toolName, string toolArgs, string endpoint, string? httpMethod = "Post", string? key = null);
    }
}
