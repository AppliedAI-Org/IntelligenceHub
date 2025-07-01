using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using static IntelligenceHub.Common.GlobalVariables;
using System.Text.RegularExpressions;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using IntelligenceHub.Common.Extensions;
using System.Drawing.Imaging;
using Message = IntelligenceHub.API.DTOs.Message;
using Tool = IntelligenceHub.API.DTOs.Tools.Tool;
using Property = IntelligenceHub.API.DTOs.Tools.Property;

namespace IntelligenceHub.Business.Handlers
{
    /// <summary>
    /// A class that handles validation for incoming DTOs sent to the API.
    /// </summary>
    public class ValidationHandler : IValidationHandler
    {
        private const int MAX_IMAGE_SIZE_BYTES = 20 * 1024 * 1024; // 20MB limit
        private const int VALIDATION_TIMEOUT_MS = 1500; // 1500ms timeout

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

        #region Profile And Tool Validation

        /// <summary>
        /// Validates the API chat request DTO.
        /// </summary>
        /// <param name="chatRequest">The chat request DTO.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateChatRequest(CompletionRequest chatRequest)
        {
            if (chatRequest.ProfileOptions.Name == null) return "A profile name must be included in the request body or route.";
            if (chatRequest == null) return "The chatRequest object must be provided.";

            var profileOptionsErrorMessage = ValidateProfileOptions(chatRequest.ProfileOptions, chatRequest.Messages);
            if (profileOptionsErrorMessage != null) return profileOptionsErrorMessage;

            var messagesErrorMessage = ValidateMessageList(chatRequest.Messages);
            if (messagesErrorMessage != null) return messagesErrorMessage;
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
            if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name == null) return "The 'Name' field is required.";
            if (profile.Name.ToLower() == "all") return "Profile name 'all' conflicts with the profile/get/all route.";

            var errorMessage = ValidateProfileOptions(profile);
            if (errorMessage != null) return errorMessage;
            return null;
        }

        /// <summary>
        /// Validates the base DTO/Profile DTO for the chat request API and profile API.
        /// </summary>
        /// <param name="profile">The profile to validate.</param>
        /// <param name="messages">Messages if any need to be validated against the host API's parameters.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateProfileOptions(Profile profile, List<Message>? messages = null)
        {
            if (profile.Model == null) return "The model parameter is required.";
            if (string.IsNullOrEmpty(profile.Host.ToString()) || profile.Host == AGIServiceHosts.None) return "The host parameter is required.";

            // Validate model names for each host.
            if (!string.IsNullOrEmpty(profile.Model))
            {
                if (profile.Host == AGIServiceHosts.Azure && !_validModels.Contains(profile.Model.ToLower())) return $"The provided model name is not supported by Azure. Supported model names include: {_validModels.ToCommaSeparatedString()}.";
                if (profile.Host == AGIServiceHosts.OpenAI && !ValidOpenAIModelsAndContextLimits.Keys.Contains(profile.Model.ToLower())) return $"The provided model name is not supported by OpenAI. Supported model names include: {ValidOpenAIModelsAndContextLimits.Keys.ToCommaSeparatedString()}.";
                if (profile.Host == AGIServiceHosts.Anthropic && !ValidAnthropicModels.Contains(profile.Model.ToLower())) return $"The provided model name is not supported by Anthropic. Supported model names include: {ValidAnthropicModels.ToCommaSeparatedString()}.";
            }

            // Validate common parameters.
            if (profile.FrequencyPenalty < -2.0 || profile.FrequencyPenalty > 2.0) return "FrequencyPenalty must be a value between -2 and 2.";
            if (profile.PresencePenalty < -2.0 || profile.PresencePenalty > 2.0) return "PresencePenalty must be a value between -2 and 2.";
            if (profile.Temperature < 0 || profile.Temperature > 2) return "Temperature must be a value between 0 and 2.";
            if (profile.TopP < 0 || profile.TopP > 1) return "TopP must be a value between 0 and 1.";
            if (profile.MaxTokens < 1) return "MaxTokens must be at least 1.";
            if (profile.ReferenceProfiles?.Length > 3) return "The 'ReferenceProfiles' field must contain 3 or fewer profiles.";
            if (profile.ReferenceProfiles != null && profile.ReferenceProfiles.Length > 0) foreach (var reference in profile.ReferenceProfiles) if (reference.Length > 40) return "The 'ReferenceProfiles' field exceeds the maximum allowed length of 40 characters.";

            // Validate model-specific parameters.
            if (profile.Host == AGIServiceHosts.OpenAI)
            {
                // Look up the context limit for the specified model.
                if (!ValidOpenAIModelsAndContextLimits.TryGetValue(profile.Model.ToLower(), out int contextLimit)) contextLimit = 4096;
                if (profile.MaxTokens > contextLimit) return $"For OpenAI, Max_Tokens cannot exceed {contextLimit} for the selected model.";

                // Validate total tokens (prompt + maxTokens) against model capacity.
                if (messages != null)
                {
                    int promptTokens = EstimateTokenCount(messages);
                    if (promptTokens + profile.MaxTokens > contextLimit) return $"The combined token count of the prompt ({promptTokens}) and the requested max tokens ({profile.MaxTokens}) exceeds the model's capacity of {contextLimit} tokens.";
                }
            }
            else if (profile.Host == AGIServiceHosts.Anthropic)
            {
                // For Anthropic, use a fixed context limit.
                int contextLimit = 4000; // Recommended limit; update if newer models allow more.
                if (profile.MaxTokens > contextLimit) return "For Anthropic, MaxTokens (max_tokens_to_sample) should not exceed 4000.";

                // Anthropic does not support penalties.
                if (profile.FrequencyPenalty != 0 || profile.PresencePenalty != 0) return "Frequency and Presence penalties are not supported for Anthropic and must be set to 0 or null.";

                if (messages != null)
                {
                    int promptTokens = EstimateTokenCount(messages);
                    if (promptTokens + profile.MaxTokens > contextLimit) return $"The combined token count of the prompt ({promptTokens}) and the requested max tokens ({profile.MaxTokens}) exceeds the Anthropic model's capacity of {contextLimit} tokens.";
                }
            }
            else if (profile.Host == AGIServiceHosts.Azure)
            {
                // Azure endpoints typically do not support returning token-level log probabilities.
                if (profile.TopLogprobs.HasValue && profile.TopLogprobs.Value != 0) return "The Azure endpoint does not support TopLogprobs. Please set TopLogprobs to 0 or leave it unset.";
            }

            if (profile.TopLogprobs < 0 || profile.TopLogprobs > 5) return "Top_Logprobs must be a value between 0 and 5";
            if (profile.ResponseFormat != null && profile.ResponseFormat.ToLower() != ResponseFormat.Text.ToString().ToLower() && profile.ResponseFormat.ToLower() != ResponseFormat.Json.ToString().ToLower()) return $"If ResponseType is set, it must either be equal to '{ResponseFormat.Text}' or '{ResponseFormat.Json}'.";

            // Validate SQL constraints
            if (profile.Name.Length > 40) return "The 'Name' field exceeds the maximum allowed length of 40 characters.";
            if (profile.Model.Length > 255) return "The 'Model' field exceeds the maximum allowed length of 255 characters.";
            if (profile.ResponseFormat != null && profile.ResponseFormat.Length > 255) return "The 'ResponseFormat' field exceeds the maximum allowed length of 255 characters.";
            if (profile.User != null && profile.User.Length > 255) return "The 'User' field exceeds the maximum allowed length of 255 characters.";
            //if (profile.SystemMessage != null && profile.SystemMessage.Length > 2040) return "The 'SystemMessage' field exceeds the maximum allowed length of 2040 characters.";
            if (profile.Stop != null && profile.Stop.ToCommaSeparatedString().Length > 255) return "The 'Stop' field exceeds the maximum allowed length of 255 characters. Please note that strings are added between each entry, adding +1 to each one's character count.";
            if (profile.ReferenceProfiles != null && profile.ReferenceProfiles.Length > 2040) return "The 'ReferenceProfiles' field exceeds the maximum allowed length of 2040 characters.";
            if (profile.Host.ToString().Length > 255) return "The 'Host' field exceeds the maximum allowed length of 255 characters.";
            if (profile.ImageHost != null && profile.ImageHost?.ToString().Length > 255) return "The 'ImageHost' field exceeds the maximum allowed length of 255 characters.";

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
            if (tool.Function.Name.Length > 64) return "The function name exceeds the maximum allowed length of 255 characters.";
            if (tool.Function.Description?.Length > 512) return "The function description exceeds the maximum allowed length of 512 characters.";
            if (tool.Function.Parameters.required?.Length > 255) return "The function parameters exceeds the maximum allowed length of 255 characters.";
            if (tool.ExecutionUrl?.Length > 4000) return "The tool ExecutionUrl exceeds the maximum length of 2000 characters.";
            if (tool.ExecutionBase64Key?.Length > 255) return "The tool ExecutionBase64Key exceeds the maximum length of 255 characters.";
            if (tool.ExecutionMethod?.Length > 255) return "The tool ExecutionMethod exceeds the maximum length of 255 characters.";
            if (tool.Function.Name.ToLower() == "all") return "Profile name 'all' conflicts with the tool/get/all route.";
            if (tool.Function.Name.ToLower() == SystemTools.Chat_Recursion.ToString().ToLower()) return "The function name 'recurse_ai_dialogue' is reserved.";
            if (tool.Function.Name.ToLower() == SystemTools.Image_Gen.ToString().ToLower()) return "The function name 'image_gen' is reserved.";
            if (tool.Function.Parameters.required != null && tool.Function.Parameters.required.Length > 0)
            {
                foreach (var str in tool.Function.Parameters.required) if (!tool.Function.Parameters.properties.ContainsKey(str)) return $"Required property {str} does not exist in the tool {tool.Function.Name}'s properties list.";
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
                if (prop.Value.type == null) return $"The field 'type' for property {prop.Key} is required.";
                else if (!_validToolArgTypes.Contains(prop.Value.type)) return $"The 'type' field '{prop.Value.type}' for property {prop.Key} is invalid. Please ensure one of the following types is selected: '{_validToolArgTypes}'.";
                else if (prop.Value.description?.Length > 200) return "Tool property descriptions cannot exceed 200 characters.";
                else if (prop.Key.Length > 64) return "Tool property names cannot exceed 64 characters.";
            }
            return null;
        }

        /// <summary>
        /// Validates a list of messages.
        /// </summary>
        /// <param name="messageList">The list of messages to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateMessageList(List<Message> messageList)
        {
            if (messageList == null || !messageList.Any()) return "The messages array was null or empty.";
            if (!messageList.Exists(m => m.Role == Role.User)) return "The messages array must contain at least one user message, but contains none.";
            foreach (var message in messageList)
            {
                var errorMessage = ValidateMessage(message);
                if (!string.IsNullOrEmpty(errorMessage)) return errorMessage;
            }
            return null;
        }

        /// <summary>
        /// Validates a single message.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateMessage(Message message)
        {
            if (message == null) return "The message object was null.";
            if (message.Role == null) return "A role must be provided for each message.";
            if (string.IsNullOrWhiteSpace(message.Content) && string.IsNullOrWhiteSpace(message.Base64Image)) return "All messages must contain content or an image.";
            if (message.User.Length > 255) return "The user name exceeds the maximum allowed length of 255 characters.";
            if (message.Content.Length > 32000) return "The message content exceeds the maximum allowed length of 32,000 characters.";
            if (message.Base64Image != null)
            {
                if (message.Base64Image.Length > MAX_IMAGE_SIZE_BYTES) return "The image size exceeds the maximum allowed size of 20MB.";
                if (!IsValidBase64Image(message.Base64Image)) return "The image provided is not valid.";
            }
            return null;
        }

        /// <summary>
        /// Estimates the token count for a list of messages.
        /// </summary>
        /// <param name="messages">The list of messages to estimate the token count for.</param>
        /// <returns>The estimated token count.</returns>
        private int EstimateTokenCount(List<Message> messages)
        {
            int count = 0;
            foreach (var message in messages)
            {
                // This is an approximation. Replace with an actual tokenizer if needed.
                count += message.Content.Split(' ').Length;
            }
            return count;
        }

        /// <summary>
        /// Checks if a base64 string is a valid image.
        /// </summary>
        /// <param name="base64String">The base64 string to validate.</param>
        /// <returns>True if the base64 string is a valid image, otherwise false.</returns>
        private static bool IsValidBase64Image(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String)) return false;

            try
            {
                // Enforce Base64 size limit before decoding
                if (base64String.Length > MAX_IMAGE_SIZE_BYTES * 4 / 3) return false;

                byte[] imageBytes = Convert.FromBase64String(base64String);

                // Enforce byte array size limit
                if (imageBytes.Length > MAX_IMAGE_SIZE_BYTES) return false;

                // Run validation with a timeout to prevent excessive processing
                return Task.Run(() => ValidateImage(imageBytes)).Wait(VALIDATION_TIMEOUT_MS);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates an image byte array.
        /// </summary>
        /// <param name="imageBytes">The image byte array to validate.</param>
        /// <returns>True if the image is valid, otherwise false.</returns>
        private static bool ValidateImage(byte[] imageBytes)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false)) 
                    {
                        return img.RawFormat.Equals(ImageFormat.Jpeg) || img.RawFormat.Equals(ImageFormat.Png) || img.RawFormat.Equals(ImageFormat.Gif) || img.RawFormat.Equals(ImageFormat.Bmp);
                    }
                }
            }
            catch
            {
                return false;
            }
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
            if (index.Name.Length > 128) return "The index name exceeds the maximum allowed length of 128 characters.";
            if (index.IndexingInterval <= TimeSpan.Zero) return "IndexingInterval must be a positive value.";
            if (index.IndexingInterval >= TimeSpan.FromDays(1)) return "The indexing interval must be less than 1 day.";
            if (!string.IsNullOrWhiteSpace(index.EmbeddingModel) && index.EmbeddingModel.Length > 255) return "The EmbeddingModel exceeds the maximum allowed length of 255 characters.";
            if (index.MaxRagAttachments < 0) return "MaxRagAttachments must be a non-negative integer greater than 0.";
            if (index.MaxRagAttachments > 20) return "MaxRagAttachments cannot exceed 20.";
            if (index.ChunkOverlap < 0 || index.ChunkOverlap > 1) return "ChunkOverlap must be between 0 and 1 (inclusive).";
            if (index.EmbeddingModel?.ToLower() != "text-embedding-ada-002" && index.EmbeddingModel?.ToLower() != "text-embedding-3-small" && index.EmbeddingModel?.ToLower() != "text-embedding-3-large") return $"The provided embedding model '{index.EmbeddingModel}' is not supported. Currently, the supported embedding models are limited to text-embedding-3-large, text-embedding-3-small, and text-embedding-ada-002";

            if (!string.IsNullOrEmpty(index.ScoringProfile?.Name))
            {
                if (string.IsNullOrWhiteSpace(index.ScoringProfile.Name)) return "The ScoringProfile name is required.";
                if (index.ScoringProfile.Name.Length > 128) return "The ScoringProfile name exceeds the maximum allowed length of 255 characters.";
                if (index.QueryType.ToString().Length > 255) return "The QueryType exceeds the maximum allowed length of 255 characters.";
                if (index.GenerationHost?.ToString().Length > 255) return "The GenerationHost exceeds the maximum allowed length of 255 characters.";
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
            var pattern = @"^[a-zA-Z_][a-zA-Z0-9_-]*$";
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
            var charLimit = 1000000; // Represents roughly 2mb of chars
            if (string.IsNullOrWhiteSpace(document.Title)) return "Document title cannot be empty.";
            if (string.IsNullOrWhiteSpace(document.Content)) return "Document content cannot be empty.";
            if (document.Content.Length > charLimit) return "Document content exceeds the maximum allowed length of 1,000,000 characters.";
            if (document.Title.Length > 255) return "Document title exceeds the maximum allowed length of 255 characters.";
            if (document?.Topic?.Length > 255) return "Document topic exceeds the maximum allowed length of 255 characters.";
            if (document?.Keywords?.Length > 255) return "Document keywords exceeds the maximum allowed length of 255 characters.";
            if (document?.Source.Length > 4000) return "Document source exceeds the maximum allowed length of 4000 characters.";
            return null;
        }

        #endregion
    }
}
