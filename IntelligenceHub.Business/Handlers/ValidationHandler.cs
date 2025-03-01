using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common;
using static IntelligenceHub.Common.GlobalVariables;
using System.Text.RegularExpressions;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using IntelligenceHub.Common.Extensions;

namespace IntelligenceHub.Business.Handlers
{
    /// <summary>
    /// A class that handles validation for incoming DTOs sent to the API.
    /// </summary>
    public class ValidationHandler : IValidationHandler
    {
        #region Profile And Tool Validation

        private readonly string[] _validModels;

        public string[] _validToolArgTypes = new string[]
        {
            // verify these
            "char",
            "string",
            "bool",
            "int",
            "double",
            "float",
            "date",
            "enum",

            // Don't think these would work currently, but can test. May work as string:
            //"array",
            //"object"
        };

        /// <summary>
        /// Default constructor for the ValidationHandler class.
        /// </summary>
        public ValidationHandler(IOptionsMonitor<Settings> settings) 
        {
            _validModels = settings.CurrentValue.ValidAGIModels;
        }

        /// <summary>
        /// Validates the API chat request DTO.
        /// </summary>
        /// <param name="chatRequest">The chat request DTO.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateChatRequest(CompletionRequest chatRequest)
        {
            if (chatRequest.ProfileOptions.Name == null) return "A profile name must be included in the request body or route.";
            if (chatRequest == null) return "The chatRequest object must be provided";

            var errorMessage = ValidateBaseDTO(chatRequest.ProfileOptions);
            if (errorMessage != null) return errorMessage;
            return null;
        }

        /// <summary>
        /// Validates the API profile DTO.
        /// </summary>
        /// <param name="profile">The profile to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateAPIProfile(Profile profile)
        {
            // validate reference profiles exist (same with any other values?)
            if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name == null) return "The 'Name' field is required";
            if (profile.Name.ToLower() == "all") return "Profile name 'all' conflicts with the profile/get/all route";

            var errorMessage = ValidateBaseDTO(profile);
            if (errorMessage != null) return errorMessage;
            return null;
        }

        /// <summary>
        /// Validates the base DTO/Profile DTO for the chat request API and profile API.
        /// </summary>
        /// <param name="profile">The profile to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateBaseDTO(Profile profile)
        {
            if (profile.Model == null) return "The model parameter is required.";
            if (profile.Host == null || profile.Host == AGIServiceHosts.None) return "The host parameter is required.";
            if (!string.IsNullOrEmpty(profile.Model))
            {
                if (profile.Host == AGIServiceHosts.Azure && _validModels.Contains(profile.Model.ToLower()) == false) return $"The provided model name is not supported by Azure. Supported models names include: {_validModels.ToCommaSeparatedString()}";
                if (profile.Host == AGIServiceHosts.OpenAI && ValidOpenAIModels.Contains(profile.Model.ToLower()) == false) return $"The provided model name is not supported by OpenAI. Supported model names include: {ValidOpenAIModels.ToCommaSeparatedString()}";
                if (profile.Host == AGIServiceHosts.Anthropic && ValidAnthropicModels.Contains(profile.Model.ToLower()) == false) return $"The provided model name is not supported by Anthropic. Supported model names include: {ValidAnthropicModels.ToCommaSeparatedString()}";
            }
            if (profile.FrequencyPenalty < -2.0 || profile.FrequencyPenalty > 2.0) return "Frequency_Penalty must be a value between -2 and 2";
            if (profile.PresencePenalty < -2.0 || profile.PresencePenalty > 2.0) return "Presence_Penalty must be a value between -2 and 2";
            if (profile.Temperature < 0 || profile.Temperature > 2) return "Temperature must be a value between 0 and 2";
            if (profile.TopP < 0 || profile.TopP > 1) return "Top_P must be a value between 0 and 1";
            if (profile.MaxTokens < 1 || profile.MaxTokens > 1000000) return "Max_Tokens must be a value between 1 and 1,000,000"; // check this value for Azure and other services
            if (profile.TopLogprobs < 0 || profile.TopLogprobs > 5) return "Top_Logprobs must be a value between 0 and 5";
            if (profile.ResponseFormat != null && profile.ResponseFormat != "text" && profile.ResponseFormat != GlobalVariables.ResponseFormat.Json.ToString()) return $"If Response_Type is set, it must either be equal to '{GlobalVariables.ResponseFormat.Text}' or '{GlobalVariables.ResponseFormat.Json}'";

            if (profile.Tools != null)
            {
                foreach (var tool in profile.Tools)
                {
                    var errorMessage = ValidateTool(tool);
                    if (!string.IsNullOrEmpty(errorMessage)) return errorMessage;
                }
            }
            return null;
        }

        /// <summary>
        /// Validates the tool DTO.
        /// </summary>
        /// <param name="tool">The tool DTO to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateTool(Tool tool)
        {
            if (tool.Function.Name == null || string.IsNullOrEmpty(tool.Function.Name)) return "A function name is required for all tools.";
            if (tool.Function.Name.ToLower() == "all") return "Profile name 'all' conflicts with the tool/get/all route";
            if (tool.Function.Name.ToLower() == SystemTools.Chat_Recursion.ToString().ToLower()) return "The function name 'recurse_ai_dialogue' is reserved.";
            if (tool.Function.Parameters.required != null && tool.Function.Parameters.required.Length > 0)
            {
                foreach (var str in tool.Function.Parameters.required)
                {
                    if (!tool.Function.Parameters.properties.ContainsKey(str)) return $"Required property {str} does not exist in the tool {tool.Function.Name}'s properties list.";
                }
            }

            if (tool.Function.Parameters.properties != null && tool.Function.Parameters.properties.Count > 0)
            {
                var errorMessage = ValidateProperties(tool.Function.Parameters.properties);
                if (errorMessage != null) return errorMessage;
            }
            return null;
        }

        /// <summary>
        /// Validates the properties of a tool.
        /// </summary>
        /// <param name="properties">The properties to validate in a dictionary form where the 
        /// key is the property name, and the value is the property object.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateProperties(Dictionary<string, Property> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.Value.type == null) return $"The field 'type' for property {prop.Key} is required";
                else if (!_validToolArgTypes.Contains(prop.Value.type)) return $"The 'type' field '{prop.Value.type}' for property {prop.Key} is invalid. Please ensure one of the following types is selected: '{_validToolArgTypes}'";
            }
            return null;
        }

        #endregion

        #region RAG Index Validation

        /// <summary>
        /// Validates the index definition DTO for RAG operations.
        /// </summary>
        /// <param name="index">The index definition.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateIndexDefinition(IndexMetadata index)
        {
            // Validate Name
            if (string.IsNullOrWhiteSpace(index.Name)) return "The provided index name is invalid.";

            var includesContentSummarization = index.GenerateKeywords ?? index.GenerateTopic ?? false;
            if (index.GenerationHost == null && includesContentSummarization) return "The GenerationProfile is required if 'GenerateKeywords' or 'GenerateTopic' are set to true.";
            if (index.Name.Length > 255) return "The index name exceeds the maximum allowed length of 255 characters.";
            if (index.IndexingInterval <= TimeSpan.Zero) return "IndexingInterval must be a positive value.";
            if (index.IndexingInterval >= TimeSpan.FromDays(1)) return "The indexing interval must be less than 1 day.";
            if (!string.IsNullOrWhiteSpace(index.EmbeddingModel) && index.EmbeddingModel.Length > 255) return "The EmbeddingModel exceeds the maximum allowed length of 255 characters.";
            if (index.MaxRagAttachments < 0) return "MaxRagAttachments must be a non-negative integer.";
            if (index.ChunkOverlap < 0 || index.ChunkOverlap > 1) return "ChunkOverlap must be between 0 and 1 (inclusive).";

            if (!string.IsNullOrEmpty(index.ScoringProfile?.Name))
            {
                if (string.IsNullOrWhiteSpace(index.ScoringProfile.Name)) return "The ScoringProfile name is required.";
                if (index.ScoringProfile.Name.Length > 255) return "The ScoringProfile name exceeds the maximum allowed length of 255 characters.";
                if (index.ScoringProfile.SearchAggregation == null) return "The Aggregation property provided is invalid.";
                if (index.ScoringProfile.SearchInterpolation == null) return "The Interpolation property provided is invalid.";
                if (index.ScoringProfile.FreshnessBoost < 0) return "FreshnessBoost must be a non-negative value.";
                if (index.ScoringProfile.BoostDurationDays < 0) return "BoostDurationDays must be a non-negative integer.";
                if (index.ScoringProfile.TagBoost < 0) return "TagBoost must be a non-negative value.";

                // Validate Weights
                if (index.ScoringProfile.Weights != null && index.ScoringProfile.Weights.Count > 0)
                {
                    foreach (var weight in index.ScoringProfile.Weights)
                    {
                        if (string.IsNullOrWhiteSpace(weight.Key)) return "All weight keys must be non-empty strings.";
                        if (weight.Value < 0) return $"The weight value for key '{weight.Key}' must be a non-negative number.";
                    }
                }
            }

            // If all validations pass
            return null;
        }

        /// <summary>
        /// Validates the index name for RAG operations.
        /// </summary>
        /// <param name="tableName">The RAG index name that will be used to create an SQL table.</param>
        /// <returns>A bool indicating if validation passed.</returns>
        public bool IsValidIndexName(string tableName)
        {
            // Regular expression to match valid table names (alphanumeric characters and underscores only)

            // change this to mitigate possibility of DOS attacks
            var pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
            var isSuccess = false;

            // Check if the table name matches the pattern and is not a SQL keyword
            if (Regex.IsMatch(tableName, pattern))
            {
                isSuccess = !ContainsSqlKeyword(tableName.ToUpper());
                if (isSuccess) isSuccess = !ContainsAPIKeyword(tableName.ToUpper());
            }
            return isSuccess;
        }

        /// <summary>
        /// Checks if the table name contains a SQL keyword.
        /// </summary>
        /// <param name="tableName">The table name to validate.</param>
        /// <returns>A bool indicating if validation passed.</returns>
        private static bool ContainsSqlKeyword(string tableName)
        {
            // List of common SQL keywords to prevent improper use
            var sqlKeywords = new string[]
            {
                "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TABLE",
                "WHERE", "FROM", "JOIN", "UNION", "ORDER", "GROUP", "HAVING"
            };

            // Check if the table name matches any SQL keyword (case-insensitive)
            foreach (string keyword in sqlKeywords)
            {
                if (string.Equals(tableName, keyword, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the table name contains an API keyword.
        /// </summary>
        /// <param name="tableName">The name of the table to validate.</param>
        /// <returns>A bool indicating if validation passed.</returns>
        private static bool ContainsAPIKeyword(string tableName)
        {
            // List of common SQL keywords to prevent conflicts
            var sqlKeywords = new string[]
            {
                "ALL", "CONFIGURE", "DELETE"
            };

            // Check if the table name matches any SQL keyword (case-insensitive)
            foreach (string keyword in sqlKeywords) if (string.Equals(tableName, keyword, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        #endregion

        #region Rag Document Upsert Request Validation

        /// <summary>
        /// Validates the RAG document upsert request DTO.
        /// </summary>
        /// <param name="request">The document upsert request to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? IsValidRagUpsertRequest(RagUpsertRequest request)
        {
            if (request.Documents == null || request.Documents.Count == 0) return "The request must contain at least one document.";
            foreach (var document in request.Documents)
            {
                var validationResult = ValidateIndexDocument(document);
                if (validationResult != null) return validationResult;
            }

            return null;
        }

        /// <summary>
        /// Validates the document for RAG upsert requests.
        /// </summary>
        /// <param name="document">The document to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        private string? ValidateIndexDocument(IndexDocument document)
        {
            if (string.IsNullOrWhiteSpace(document.Title)) return "Document title cannot be empty.";
            if (string.IsNullOrWhiteSpace(document.Content)) return "Document content cannot be empty.";
            return null;
        }

        #endregion
    }
}
