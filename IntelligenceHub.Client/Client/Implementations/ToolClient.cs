using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;

namespace IntelligenceHub.Client.Implementations
{
    public class ToolClient : IToolClient
    {
        private HttpClient _client { get; set; }
        public ToolClient(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient(GlobalVariables.ClientPolicies.ToolClientPolicy.ToString());
        }

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
