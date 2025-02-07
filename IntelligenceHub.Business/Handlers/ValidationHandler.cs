using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Handlers
{
    public class ValidationHandler : IValidationHandler
    {
        public List<string> _validModels = new List<string>()
        {
            "gpt-4o",
            "gpt-4o-mini",
            "claude-3-5-sonnet-20241022",
            "llama-3.3-70b-versatile",
            "llama-3.1-8b-instant",
            "llama-guard-3-8b",
            "llama3-70b-8192",
            "llama3-8b-8192",
            "mixtral-8x7b-32768",
            "gemma2-9b-it"
        };

        public List<string> _validToolArgTypes = new List<string>()
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

        public ValidationHandler() { }

        public string? ValidateChatRequest(CompletionRequest chatRequest)
        {
            if (chatRequest.ProfileOptions.Name == null) return "A profile name must be included in the request body or route.";
            if (chatRequest == null) return "The chatRequest object must be provided";

            var errorMessage = ValidateBaseDTO(chatRequest.ProfileOptions);
            if (errorMessage != null) return errorMessage;
            return null;
        }

        public string? ValidateAPIProfile(Profile profile)
        {
            // validate reference profiles exist (same with any other values?)
            if (string.IsNullOrWhiteSpace(profile.Name)) return "The 'Name' field is required";
            if (profile.Host == null || string.IsNullOrEmpty(profile.Host.ToString())) return "the 'Host' field is required, and must be set to 'Azure', 'OpenAI', or 'Anthropic'";
            if (profile.Name.ToLower() == "all") return "Profile name 'all' conflicts with the profile/get/all route";

            var errorMessage = ValidateBaseDTO(profile);
            if (errorMessage != null) return errorMessage;
            return null;
        }

        public string? ValidateBaseDTO(Profile profile)
        {
            if (profile.Model != null && _validModels.Contains(profile.Model) == false)
            {
                return "The model name must match and existing AI model";
            }
            if (profile.Frequency_Penalty < -2.0 || profile.Frequency_Penalty > 2.0)
            {
                return "Frequency_Penalty must be a value between -2 and 2";
            }
            if (profile.Presence_Penalty < -2.0 || profile.Presence_Penalty > 2.0)
            {
                return "Presence_Penalty must be a value between -2 and 2";
            }
            if (profile.Temperature < 0 || profile.Temperature > 2)
            {
                return "Temperature must be a value between 0 and 2";
            }
            if (profile.Top_P < 0 || profile.Top_P > 1)
            {
                return "Top_P must be a value between 0 and 1";
            }
            if (profile.Max_Tokens < 1 || profile.Max_Tokens > 1000000) // check this value for Azure
            {
                return "Max_Tokens must be a value between 1 and 1,000,000";
            }
            if (profile.Top_Logprobs < 0 || profile.Top_Logprobs > 5)
            {
                return "Top_Logprobs must be a value between 0 and 5";
            }
            if (profile.Response_Format != null && profile.Response_Format != "text" && profile.Response_Format != GlobalVariables.ResponseFormat.Json.ToString())
            {
                return $"If Response_Type is set, it must either be equal to '{GlobalVariables.ResponseFormat.Text}' or '{GlobalVariables.ResponseFormat.Json}'";
            }

            if (profile.Tools != null)
            {
                foreach (var tool in profile.Tools)
                {
                    var errorMessage = ValidateTool(tool);
                    if (errorMessage != null)
                    {
                        return errorMessage;
                    }
                }
            }
            return null;
        }

        public string? ValidateTool(Tool tool)
        {
            if (tool.Function.Name == null || string.IsNullOrEmpty(tool.Function.Name))
            {
                return "A function name is required for all tools.";
            }
            if (tool.Function.Name.ToLower() == "all")
            {
                return "Profile name 'all' conflicts with the tool/get/all route";
            }
            if (tool.Function.Name.ToLower() == SystemTools.Recurse_ai_dialogue.ToString().ToLower())
            {
                return "The function name 'recurse_ai_dialogue' is reserved.";
            }
            if (tool.Function.Parameters.required != null && tool.Function.Parameters.required.Length > 0)
            {
                foreach (var str in tool.Function.Parameters.required)
                {
                    if (!tool.Function.Parameters.properties.ContainsKey(str))
                    {
                        return $"Required property {str} does not exist in the tool {tool.Function.Name}'s properties list.";
                    }
                }
            }

            if (tool.Function.Parameters.properties != null && tool.Function.Parameters.properties.Count > 0)
            {
                var errorMessage = ValidateProperties(tool.Function.Parameters.properties);
                if (errorMessage != null) return errorMessage;
            }
            return null;
        }

        public string? ValidateProperties(Dictionary<string, Property> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.Value.type == null) return $"The field 'type' for property {prop.Key} is required";
                else if (!_validToolArgTypes.Contains(prop.Value.type)) return $"The 'type' field '{prop.Value.type}' for property {prop.Key} is invalid. Please ensure one of the following types is selected: '{_validToolArgTypes}'";
            }
            return null;
        }
    }
}
