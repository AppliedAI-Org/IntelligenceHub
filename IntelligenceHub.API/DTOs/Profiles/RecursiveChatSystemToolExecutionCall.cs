using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.API.DTOs.Tools
{
    /// <summary>
    /// Represents the payload sent when invoking the chat recursion system tool.
    /// </summary>
    public class RecursiveChatSystemToolExecutionCall
    {
        /// <summary>
        /// Gets or sets the name of the responding AI model.
        /// </summary>
        public string responding_ai_model { get; set; }

        /// <summary>
        /// Gets or sets the response that should be used as the next prompt.
        /// </summary>
        public string prompt_response { get; set; }
    }
}
