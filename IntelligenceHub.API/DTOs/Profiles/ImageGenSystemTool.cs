using IntelligenceHub.Common.Extensions;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.Tools
{
    public class ImageGenSystemTool : Tool
    {
        private readonly string _promptPropertyName = "prompt";

        public ImageGenSystemTool()
        {
            var promptProperty = new Property()
            {
                type = _stringPropertyType,
                description = "A prompt that will be used to generate the image.",
            };

            Function = new Function()
            {
                Name = SystemTools.Image_Gen.ToString().ToLower(),
                Description = $"Generates a picture which will be attached to your response back to the user.",
                Parameters = new Parameters()
                {
                    type = _objectPropertyType,
                    properties = new Dictionary<string, Property>()
                    {
                        { _promptPropertyName, promptProperty },
                    },
                    required = new string[] { _promptPropertyName },
                }
            };
        }
    }
}
