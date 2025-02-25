using IntelligenceHub.Common.Extensions;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.Tools
{
    public class RecursiveChatSystemTool : Tool
    {
        private readonly string _promptResponsePropertyName = "prompt_response";
        private readonly string _respondingModelPropertyName = "responding_ai_model";

        public RecursiveChatSystemTool(List<Profile> referenceProfiles)
        {
            var referenceModels = referenceProfiles.Select(x => x.Name).ToList();
            var referenceModelsString = referenceModels.ToCommaSeparatedString() ?? string.Empty;

            var dialogueHistoryProperty = new Property()
            {
                type = _stringPropertyType,
                description = $"Your response to the original prompt completion in the conversation thread. Valid model names include {referenceModelsString}",
            };

            var recursionProfileNameProperty = new Property()
            {
                type = _stringPropertyType,
                description = "The name of the AI model that you want to respond to your addition to this conversation thread.",
            };

            Function = new Function()
            {
                Name = SystemTools.Chat_Recursion.ToString().ToLower(),
                Description = $"Starts or continues an internal dialogue between yourself, or between other LLM models and configurations. " +
                              $"Below is the name of each model you can call, along with a description of when it should be used. You " +
                              $"should pass this name as the '{_respondingModelPropertyName}' parameter associated with this tool. Only provide names " +
                              $"of tools that exist in the below list, otherwise an error will occur.\n\n",

                Parameters = new Parameters()
                {
                    type = _objectPropertyType,
                    properties = new Dictionary<string, Property>()
                    {
                        { _promptResponsePropertyName, dialogueHistoryProperty },
                        { _respondingModelPropertyName, recursionProfileNameProperty },
                    },
                    required = new string[] { _promptResponsePropertyName, _respondingModelPropertyName },
                }
            };

            foreach (var profile in referenceProfiles) Function.Description += $"Model Name: {profile.Name}, \nModel System Message: {profile.ReferenceDescription}\n\n";
        }
    }
}
