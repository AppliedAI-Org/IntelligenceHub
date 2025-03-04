using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// A client for executing tools at external endpoints.
    /// </summary>
    public class ToolClient : IToolClient
    {
        private HttpClient _client { get; set; }

        /// <summary>
        /// Creates a new instance of the ToolClient.
        /// </summary>
        /// <param name="clientFactory">The client factory used to build an HttpClient.</param>
        public ToolClient(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient(GlobalVariables.ClientPolicies.ToolClientPolicy.ToString());
        }

        /// <summary>
        /// Calls a tool at the specified endpoint.
        /// </summary>
        /// <param name="toolName">The name of the tool being executed.</param>
        /// <param name="toolArgs">The arguments to be used as the request body for tool execution.</param>
        /// <param name="endpoint">The endpoint at which the tool will be executed.</param>
        /// <param name="httpMethod">The type of http method to execute at the endpoint.</param>
        /// <param name="key">A base64 password if the API endpoint requires one.</param>
        /// <returns>The response message returned from the endpoint.</returns>
        public async Task<HttpResponseMessage> CallFunction(string toolName, string toolArgs, string endpoint, string? httpMethod = "POST", string? key = null)
        {
            // validate inputs
            if (string.IsNullOrEmpty(endpoint)) return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound) { Content = new StringContent("No endpoint provided") };

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(key)) _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", key);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            var json = string.Empty;
            if (!string.IsNullOrEmpty(toolArgs))
            {
                var tool = new ToolExecutionCall()
                {
                    ToolName = toolName,
                    Arguments = toolArgs,
                };
                json = JsonConvert.SerializeObject(tool, settings);
            }
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                if (string.IsNullOrEmpty(toolArgs))
                {
                    if (httpMethod?.ToUpper() == HttpMethod.Post.ToString()) return await _client.PostAsync(endpoint, null);
                    else if (httpMethod?.ToUpper() == HttpMethod.Put.ToString()) return await _client.PutAsync(endpoint, null);
                    else if (httpMethod?.ToUpper() == HttpMethod.Patch.ToString()) return await _client.PatchAsync(endpoint, null);
                }

                if (httpMethod?.ToUpper() == HttpMethod.Post.ToString()) return await _client.PostAsync(endpoint, body);
                else if (httpMethod?.ToUpper() == HttpMethod.Put.ToString()) return await _client.PutAsync(endpoint, body);
                else if (httpMethod?.ToUpper() == HttpMethod.Patch.ToString()) return await _client.PatchAsync(endpoint, body);
                else if (httpMethod?.ToUpper() == HttpMethod.Get.ToString()) return await _client.GetAsync(endpoint);
                else if (httpMethod?.ToUpper() == HttpMethod.Delete.ToString()) return await _client.DeleteAsync(endpoint);
                else return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            }
            catch (HttpRequestException ex)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest) { ReasonPhrase = ex.Message };
            }
            catch (TaskCanceledException ex)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout) { ReasonPhrase = ex.Message };
            }
        }
    }
}
