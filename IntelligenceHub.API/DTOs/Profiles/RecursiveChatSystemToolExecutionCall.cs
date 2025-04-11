using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.API.DTOs.Tools
{
    public class RecursiveChatSystemToolExecutionCall
    {
        public string responding_ai_model { get; set; }
        public string prompt_response { get; set; }
    }
}
