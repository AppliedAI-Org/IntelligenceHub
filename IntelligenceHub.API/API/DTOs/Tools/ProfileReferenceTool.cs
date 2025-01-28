using IntelligenceHub.Common.Extensions;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.Tools
{
    public class ProfileReferenceTool : Tool
    {
        public ProfileReferenceTool(List<Profile> referenceProfiles)
        {
            var referenceModels = referenceProfiles.Select(x => x.Name).ToList();
            var referenceModelsString = referenceModels.ToCommaSeparatedString() ?? string.Empty;
            var dialogueHistoryProperty = new Property()
            {
                Type = "string",
                Description = $"Your response to the original prompt completion in the conversation thread. Valid model names include {referenceModelsString}",
            };

            var recursionProfileNameProperty = new Property()
            {
                Type = "string",
                Description = "The name of the AI model that you want to respond to your addition to this conversation thread.",
            };

            Function = new Function()
            {
                Name = SystemTools.Recurse_ai_dialogue.ToString().ToLower(),
                Description = $"Starts or continues an internal dialogue between yourself, or between other LLM models and configurations. " +
                              $"Below is the name of each model you can call, along with a description of when it should be used. You " +
                              $"should pass this name as the 'responding_ai_model' parameter associated with this tool. Only provide names " +
                              $"of tools that exist in the below list, otherwise an error will occur.\n\n",
                Parameters = new Parameters()
                {
                    Type = "object",
                    Properties = new Dictionary<string, Property>()
                    {
                        { "prompt_response", dialogueHistoryProperty },
                        { "responding_ai_model", recursionProfileNameProperty },
                    },
                    Required = new string[] { "prompt_response", "responding_ai_model" },
                }
            };

            foreach (var profile in referenceProfiles) Function.Description += $"Model Name: {profile.Name}, \nModel System Message: {profile.ReferenceDescription}\n\n";
        }
    }
}
