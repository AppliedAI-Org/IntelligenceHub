using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.Host.Config;
using IntelligenceHub.Host.Logging;
using IntelligenceHub.Host.Policies;
using IntelligenceHub.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Host.Swagger;
using IntelligenceHub.Business.Handlers;

namespace IntelligenceHub.Host
{
    /// <summary>
    /// Entry point for the IntelligenceHub web host.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Bootstraps and runs the web application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Add Services and Settings

            var settingsSection = builder.Configuration.GetRequiredSection(nameof(Settings));
            var settings = settingsSection.Get<Settings>();

            var insightSettingsSection = builder.Configuration.GetRequiredSection(nameof(AppInsightSettings));
            var insightSettings = insightSettingsSection.Get<AppInsightSettings>();

            var agiClientSettingsSection = builder.Configuration.GetRequiredSection(nameof(AGIClientSettings));
            var agiClientSettings = agiClientSettingsSection.Get<AGIClientSettings>();

            builder.Services.Configure<Settings>(settingsSection);
            builder.Services.Configure<AppInsightSettings>(insightSettingsSection);
            builder.Services.Configure<AGIClientSettings>(agiClientSettingsSection);
            builder.Services.Configure<AzureSearchServiceClientSettings>(builder.Configuration.GetRequiredSection(nameof(AzureSearchServiceClientSettings)));
            builder.Services.Configure<WeaviateSearchServiceClientSettings>(builder.Configuration.GetSection(nameof(WeaviateSearchServiceClientSettings)));

            // Add Services

            // Add EF Core DbContext with Basic Retry Policy
            builder.Services.AddDbContext<IntelligenceHubDbContext>(options =>
                options.UseSqlServer(settings.DbConnectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: settings.MaxDbRetries,
                        maxRetryDelay: TimeSpan.FromSeconds(settings.MaxDbRetryDelay),
                        errorNumbersToAdd: null // Provide specific codes here if desired
                    );
                })
            );

            // Logic
            builder.Services.AddScoped<ICompletionLogic, CompletionLogic>();
            builder.Services.AddScoped<IMessageHistoryLogic, MessageHistoryLogic>();
            builder.Services.AddScoped<IProfileLogic, ProfileLogic>();
            builder.Services.AddScoped<IRagLogic, RagLogic>();

            // Clients and Client Factory
            builder.Services.AddSingleton<IAGIClientFactory, AGIClientFactory>();
            builder.Services.AddSingleton<IRagClientFactory, RagClientFactory>();
            builder.Services.AddSingleton<IAGIClient, AzureAIClient>();
            builder.Services.AddSingleton<OpenAIClient>();
            builder.Services.AddSingleton<AzureAIClient>();
            builder.Services.AddSingleton<AnthropicAIClient>();
            builder.Services.AddSingleton<IToolClient, ToolClient>();
            builder.Services.AddSingleton<AzureAISearchServiceClient>();
            builder.Services.AddSingleton<WeaviateSearchServiceClient>();

            // Repositories
            builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
            builder.Services.AddScoped<IToolRepository, ToolRepository>();
            builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
            builder.Services.AddScoped<IProfileToolsAssociativeRepository, ProfileToolsAssociativeRepository>();
            builder.Services.AddScoped<IMessageHistoryRepository, MessageHistoryRepository>();
            builder.Services.AddScoped<IIndexRepository, IndexRepository>();
            builder.Services.AddScoped<IIndexMetaRepository, IndexMetaRepository>();

            // Handlers
            var serviceUrls = new Dictionary<string, string[]>();
            if (agiClientSettings?.AzureOpenAIServices != null) serviceUrls.Add(ClientPolicies.AzureAIClientPolicy.ToString(), agiClientSettings.AzureOpenAIServices.Select(service => service.Endpoint).ToArray());
            if (agiClientSettings?.OpenAIServices != null) serviceUrls.Add(ClientPolicies.OpenAIClientPolicy.ToString(), agiClientSettings.OpenAIServices.Select(service => service.Endpoint).ToArray());
            if (agiClientSettings?.AnthropicServices != null) serviceUrls.Add(ClientPolicies.AnthropicAIClientPolicy.ToString(), agiClientSettings.AnthropicServices.Select(service => service.Endpoint).ToArray());

            builder.Services.AddSingleton(new LoadBalancingSelector(serviceUrls));
            builder.Services.AddSingleton<IValidationHandler, ValidationHandler>();
            builder.Services.AddSingleton<IBackgroundTaskQueueHandler, BackgroundTaskQueueHandler>();
            builder.Services.AddHostedService<BackgroundWorker>();

            #endregion

            #region Configure Client Policies

            // Define the ToolClient policy
            builder.Services.AddHttpClient(ClientPolicies.ToolClientPolicy.ToString()).AddPolicyHandler(
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(settings.ToolClientMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(settings.ToolClientInitialRetryDelay, retryAttempt))));

            // Define the Completion retry policy
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(settings.AGIClientMaxRetries, _ =>
                {
                    // Add random jitter
                    var maxJitter = settings.AGIClientMaxJitter * 1000; // convert to milliseconds
                    var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, maxJitter));
                    return TimeSpan.FromSeconds(settings.AGIClientInitialRetryDelay) + jitter;
                });

            // Define the Completion circuit breaker policy if more than one service exists.
            IAsyncPolicy<HttpResponseMessage> policyWrap = retryPolicy;

            // Register the HttpClient with the load balancing handler and appropriate policy for each AI client type. -> Services with more than 1 instance will have a circuit breaker policy.
            void RegisterClientPolicy(IServiceCollection services, string policyName, string serviceName, int serviceCount)
            {
                IAsyncPolicy<HttpResponseMessage> policy = retryPolicy;

                if (serviceCount > 1)
                {
                    var circuitBreakerPolicy = Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: settings.MaxCircuitBreakerFailures,
                        durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreakerBreakDuration),
                        onBreak: (result, breakDelay) =>
                        {
                            Console.WriteLine($"Circuit opened for {breakDelay.TotalSeconds} seconds.");
                        },
                        onReset: () => Console.WriteLine("Circuit closed - normal operation resumed."),
                        onHalfOpen: () => Console.WriteLine("Circuit is half-open - trial requests allowed.")
                    );

                    // Combine the Completion retry and circuit breaker policies.
                    policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
                }

                LoadBalancingSelector.RegisterHttpClientWithPolicy(services, policyName, serviceName, policy);
            }

            // Register policies for each service type
            RegisterClientPolicy(builder.Services, ClientPolicies.AzureAIClientPolicy.ToString(), ClientPolicies.AzureAIClientPolicy.ToString(), agiClientSettings.AzureOpenAIServices.Count);
            RegisterClientPolicy(builder.Services, ClientPolicies.OpenAIClientPolicy.ToString(), ClientPolicies.OpenAIClientPolicy.ToString(), agiClientSettings.OpenAIServices.Count);
            RegisterClientPolicy(builder.Services, ClientPolicies.AnthropicAIClientPolicy.ToString(), ClientPolicies.AnthropicAIClientPolicy.ToString(), agiClientSettings.AnthropicServices.Count);

            #endregion

            #region Json Serialization Settings

            // Configure json serialization for controllers and hubs
            builder.Services.AddSignalR().AddJsonProtocol(options =>
            {
                // Set serialization for global enums utilized in DTOs
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }).AddHubOptions<ChatHub>(options =>
            {
                if (builder.Environment.IsDevelopment()) options.EnableDetailedErrors = true; // enable detailed errors for dev environments
            });
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                // Set serialization for global enums utilized in DTOs
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
            #endregion

            #region Logging

            // Add Logging via Application Insights
            builder.Services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = insightSettings?.ConnectionString;
            });

            builder.Services.AddLogging(options =>
            {
                options.AddApplicationInsights();
            });
            #endregion

            #region Swagger
            builder.Services.AddSwaggerGen(options =>
            {
                // Add Swagger documentation
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Intelligence Hub API",
                    Version = "v1",
                    Description = "An API that simplifies utilizing and designing intelligent systems, particularly with AGI."
                });

                // Used to mark nullable path parameters as such
                options.OperationFilter<NullableRouteParametersOperationFilter>();

                // Enable annotations to set NSwag generated names for client methods
                options.EnableAnnotations();
            });

            #endregion

            #region Build App

            var app = builder.Build();

            app.UseRouting();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseCors(policy =>
                {
                    policy.AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowAnyOrigin();
                });

                // Serve static files in development environment
                app.UseStaticFiles();
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    DefaultFileNames = new List<string> { "index.html" }
                });
            }
            else
            {
                app.UseCors(policy =>
                {
                    policy.AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithOrigins(settings.ValidOrigins);
                });
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<LoggingMiddleware>();

            app.MapControllers();
            app.MapHub<ChatHub>("/chatstream").RequireCors();

            app.Run();
            #endregion
        }
    }
}

