using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.API.API.DTOs.Tools
{
    public class ProfileReferenceToolExecutionCall
    {
        public string responding_ai_model { get; set; }
        public string prompt_response { get; set; }
    }
}
