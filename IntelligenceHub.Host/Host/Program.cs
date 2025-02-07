using DotNetEnv;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Business.Handlers;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.Extensions.DependencyInjection;

namespace IntelligenceHub.Host
{
    public class Program
    {
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
            builder.Services.Configure<SearchServiceClientSettings>(builder.Configuration.GetRequiredSection(nameof(SearchServiceClientSettings)));

            // Add Services

            // Add EF Core DbContext with Basic Retry Policy
            builder.Services.AddDbContext<IntelligenceHubDbContext>(options =>
                options.UseSqlServer(settings.DbConnectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
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
            builder.Services.AddSingleton<IAGIClient, AzureAIClient>(); // default client
            builder.Services.AddSingleton<OpenAIClient>();
            builder.Services.AddSingleton<AzureAIClient>();
            builder.Services.AddSingleton<AnthropicAIClient>();
            builder.Services.AddSingleton<IToolClient, ToolClient>();
            builder.Services.AddSingleton<IAISearchServiceClient, AISearchServiceClient>();

            // Repositories
            builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
            builder.Services.AddScoped<IToolRepository, ToolRepository>();
            builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
            builder.Services.AddScoped<IProfileToolsAssociativeRepository, ProfileToolsAssociativeRepository>();
            builder.Services.AddScoped<IMessageHistoryRepository, MessageHistoryRepository>();
            builder.Services.AddScoped<IIndexRepository, IndexRepository>();
            builder.Services.AddScoped<IIndexMetaRepository, IndexMetaRepository>();

            // Handlers
            var serviceUrls = new Dictionary<string, string[]>
                {
                    { ClientPolicies.AzureAIClientPolicy.ToString(), agiClientSettings.AzureServices.Select(service => service.Endpoint).ToArray() },
                    { ClientPolicies.OpenAIClientPolicy.ToString(), agiClientSettings.OpenAIServices.Select(service => service.Endpoint).ToArray() },
                    { ClientPolicies.AnthropicAIClientPolicy.ToString(), agiClientSettings.AnthropicServices.Select(service => service.Endpoint).ToArray() }
                };

            builder.Services.AddSingleton(new LoadBalancingSelector(serviceUrls));
            builder.Services.AddSingleton<IValidationHandler, ValidationHandler>();

            #endregion

            #region Configure Client Policies
            // Function Calling Client Policies:

            // Define the ToolClient policy
            builder.Services.AddHttpClient(ClientPolicies.ToolClientPolicy.ToString()).AddPolicyHandler(
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // AGI Completion Policies

            // Define the Completion retry policy
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(5, _ =>
                {
                    // Add random jitter up to 5 seconds.
                    var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 5000));
                    return TimeSpan.FromSeconds(10) + jitter;
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
                        handledEventsAllowedBeforeBreaking: 3,  // Break after 3 consecutive failures.
                        durationOfBreak: TimeSpan.FromSeconds(30), // Open the circuit for 30 seconds.
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
            RegisterClientPolicy(builder.Services, ClientPolicies.AzureAIClientPolicy.ToString(), ClientPolicies.AzureAIClientPolicy.ToString(), agiClientSettings.AzureServices.Count);
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

            #region Authentication

            // Configure Auth
            var authSettings = builder.Configuration.GetRequiredSection(nameof(AuthSettings)).Get<AuthSettings>();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = authSettings.Domain;
                options.Audience = authSettings.Audience;

                // Specify the Role Claim Type if necessary
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "roles"
                };
            });

            // Add role-based authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => (c.Type == "scope" || c.Type == "permissions") && c.Value.Split(' ').Contains("all:admin"))));
            });
            #endregion

            #region Swagger
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Intelligence Hub API",
                    Version = "v1",
                    Description = "An API that simplifies utilizing and designing intelligent systems, particularly with AGI."
                });

                // Define the security scheme for bearer tokens
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' followed by a space and the JWT token."
                });

                // Apply the security scheme globally
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                                {
                                    new OpenApiSecurityScheme
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "Bearer"
                                        }
                                    },
                                    Array.Empty<string>()
                                }
                });
            });
            #endregion

            #region Build App
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseCors(policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://intelligencehub-dev.azurewebsites.net") // Specify allowed origin explicitly
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetIsOriginAllowed((host) => true);
                });
            }
            else
            {
                // configure prod cors policy
            }

            app.UseFileServer();
            app.UseRouting();

            app.UseMiddleware<LoggingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<ChatHub>("/chatstream");

            app.Run();
            #endregion
        }
    }
}