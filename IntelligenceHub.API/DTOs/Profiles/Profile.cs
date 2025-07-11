using IntelligenceHub.API.DTOs.Tools;
using Newtonsoft.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    /// <summary>
    /// Represents configuration options for an AI client.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// Gets or sets the database identifier of the profile.
        /// </summary>
        [JsonIgnore]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the profile name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the host providing the model implementation.
        /// </summary>
        public AGIServiceHost Host { get; set; }

        /// <summary>
        /// Gets or sets the host used for image generation.
        /// </summary>
        public AGIServiceHost? ImageHost { get; set; }

        /// <summary>
        /// Gets or sets the name of the RAG database associated with this profile.
        /// </summary>
        public string? RagDatabase { get; set; }

        /// <summary>
        /// Gets or sets the frequency penalty applied to token generation.
        /// </summary>
        public float? FrequencyPenalty { get; set; }

        /// <summary>
        /// Gets or sets the presence penalty applied to token generation.
        /// </summary>
        public float? PresencePenalty { get; set; }

        /// <summary>
        /// Gets or sets the temperature setting for randomness.
        /// </summary>
        public float? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the nucleus sampling parameter.
        /// </summary>
        public float? TopP { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate.
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of top log probabilities to include.
        /// </summary>
        public int? TopLogprobs { get; set; }

        /// <summary>
        /// Gets or sets whether token-level log probabilities are included.
        /// </summary>
        public bool? Logprobs { get; set; }

        /// <summary>
        /// Gets or sets the user identifier associated with the request.
        /// </summary>
        public string? User { get; set; }

        /// <summary>
        /// Gets or sets the tool selection strategy.
        /// </summary>
        public string? ToolChoice { get; set; }

        /// <summary>
        /// Gets or sets the expected response format.
        /// </summary>
        public string? ResponseFormat { get; set; }

        /// <summary>
        /// Gets or sets an optional system message to prepend to the conversation.
        /// </summary>
        public string? SystemMessage { get; set; }

        /// <summary>
        /// Gets or sets tokens that will terminate generation when encountered.
        /// </summary>
        public string[]? Stop { get; set; }

        /// <summary>
        /// Gets or sets the list of tools available to the profile.
        /// </summary>
        public List<Tool>? Tools { get; set; }

        /// <summary>
        /// Gets or sets how many historic messages should be sent with requests.
        /// </summary>
        public int? MaxMessageHistory { get; set; }

        /// <summary>
        /// Gets or sets profiles that may be referenced during recursion.
        /// </summary>
        public string[]? ReferenceProfiles { get; set; }
    }
}
