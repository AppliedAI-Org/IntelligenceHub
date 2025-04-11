using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common;
using IntelligenceHub.Tests.Common.Config;
using IntelligenceHub.Tests.Competency.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace IntelligenceHub.Tests.Competency
{
    class Program
    {
        private static readonly HttpClient _client = new HttpClient();
        private static int _requestCount = 0;
        private static int _successCount = 0;
        private static int _failureCount = 0;
        private static double _avgRequestSeconds;
        private static double _longestRequestSeconds;
        private static string _url = string.Empty;

        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var settings = configuration.GetRequiredSection(nameof(CompetencyTestingSettings)).Get<CompetencyTestingSettings>()
                ?? throw new ArgumentNullException("Appsettings failed to be configured...");

            var intelligenceHubSettings = configuration.GetRequiredSection(nameof(IntelligenceHubSettings)).Get<IntelligenceHubSettings>()
                ?? throw new ArgumentNullException(nameof(IntelligenceHubSettings));

            _url = intelligenceHubSettings.TestingUrl;

            // Load messages for testing
            var completionList = intelligenceHubSettings.Completions;

            // Start testing
            Console.WriteLine("Beginning performance testing...\n");

            if (settings.UseGeneratedCompletions)
            {
                var generatedCompletions = await GenerateCompletionsAsync(settings.GenerativeProfile, settings.GeneratedCompletionsPerBatch);
                foreach (var generatedCompletion in generatedCompletions) await RunPerformanceTest(intelligenceHubSettings.ProfileName, settings.ScoringProfile, settings.TestsPerCompletion, generatedCompletion);
            }
            else
            {
                foreach (var completion in completionList) await RunPerformanceTest(intelligenceHubSettings.ProfileName, settings.ScoringProfile, settings.TestsPerCompletion, completion);
            }

            Console.WriteLine("\nPerformance test completed.\n");
        }

        private static async Task<List<string>> GenerateCompletionsAsync(string generativeProfile, int totalTestRounds)
        {
            var completions = new List<string>();

            for (int i = 0; i < totalTestRounds; i++)
            {
                var request = new CompletionRequest { Messages = new List<Message> { new Message { Content = "Generate a new test completion.", Role = GlobalVariables.Role.User } } };

                var responseString = await PostCompletion(generativeProfile, request);
                var completionData = JsonConvert.DeserializeObject<CompletionResponse>(responseString);
                var generatedContent = completionData?.Messages[0].Content ?? string.Empty;

                if (!string.IsNullOrEmpty(generatedContent)) completions.Add(generatedContent);
            }

            return completions;
        }

        private static async Task RunPerformanceTest(string testingProfile, string scoringProfile, int totalTestRounds, string message)
        {
            for (int i = 0; i < totalTestRounds; i++)
            {
                await SendAndEvaluateRequest(testingProfile, scoringProfile, message);
            }
        }

        private static async Task SendAndEvaluateRequest(string testingProfile, string scoringProfile, string requestMessage)
        {
            Interlocked.Increment(ref _requestCount);
            var request = new CompletionRequest { Messages = new List<Message> { new Message() { Content = requestMessage, Role = IntelligenceHub.Common.GlobalVariables.Role.User } } };

            try
            {
                var sentTime = DateTime.UtcNow;
                var responseString = await PostCompletion(testingProfile, request);

                // Calculate request timing
                var processingTime = DateTime.UtcNow - sentTime;
                UpdateTimingStatistics(processingTime);

                // Handle response
                CompletionResponse completionData = JsonConvert.DeserializeObject<CompletionResponse>(responseString);
                var responseMessage = completionData?.Messages[0].Content;

                if (IsSuccessfulResponse(responseMessage, completionData))
                {
                    Interlocked.Increment(ref _successCount);

                    // Evaluate response with testing evaluator
                    var requestBody = new CompletionRequest() { Messages = new List<Message> { new Message { Content = responseMessage ?? string.Empty, Role = IntelligenceHub.Common.GlobalVariables.Role.User } } };

                    var score = await PostCompletion(scoringProfile, requestBody);
                    await LogResult(requestMessage, responseMessage ?? string.Empty, score);
                }
                else
                {
                    Interlocked.Increment(ref _failureCount);
                    await LogResult(requestMessage, responseMessage ?? string.Empty, "Failed to complete");
                }
            }
            catch
            {
                Interlocked.Increment(ref _failureCount);
            }
        }

        private static async Task<string> PostCompletion(string profile, CompletionRequest request)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_url}/" + profile, requestContent);
            return await response.Content.ReadAsStringAsync();
        }

        private static bool IsSuccessfulResponse(string? responseMessage, CompletionResponse completionData)
        {
            var isFailure = completionData?.FinishReason != null && completionData.FinishReason.HasValue &&
                   completionData.FinishReason.Value == IntelligenceHub.Common.GlobalVariables.FinishReasons.Error;

            if (string.IsNullOrEmpty(responseMessage)) isFailure = true;

            return !isFailure;
        }

        private static async Task LogResult(string request, string response, string score)
        {
            var logLine = $"Score: {score},\n" +
                          $"Timestamp: {DateTime.UtcNow},\n" +
                          $"Request: {request}\n" +
                          $"Response: {response}\n\n";
            Console.WriteLine(logLine);
            await File.AppendAllTextAsync("performance_test_log.txt", logLine);
        }

        private static void UpdateTimingStatistics(TimeSpan processingTime)
        {
            _longestRequestSeconds = Math.Max(_longestRequestSeconds, processingTime.TotalSeconds);
            _avgRequestSeconds = ((_avgRequestSeconds * (_requestCount - 1)) + processingTime.TotalSeconds) / _requestCount;
        }
    }
}
