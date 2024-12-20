using OpenAI.Chat;
using System.Reflection.Metadata;
using System.Text;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Common.Extensions
{
    public static class ExtensionMethods
    {

        public static FinishReason? ConvertStringToFinishReason(this string finishReason)
        {
            if (finishReason == ChatFinishReason.Stop.ToString()) return FinishReason.Stop;
            if (finishReason == ChatFinishReason.Length.ToString()) return FinishReason.Length;
            if (finishReason == ChatFinishReason.ToolCalls.ToString()) return FinishReason.ToolCalls;
            if (finishReason == ChatFinishReason.FunctionCall.ToString()) return FinishReason.ToolCalls;
            if (finishReason == ChatFinishReason.ContentFilter.ToString()) return FinishReason.ContentFilter;
            return null;
        }

        public static Role? ConvertStringToRole(this string role)
        {
            if (role == ChatMessageRole.Assistant.ToString()) return Role.Assistant;
            if (role == ChatMessageRole.User.ToString()) return Role.User;
            if (role == ChatMessageRole.Tool.ToString()) return Role.Tool;
            if (role == ChatMessageRole.Function.ToString()) return Role.Tool;
            if (role == ChatMessageRole.System.ToString()) return Role.System;
            return null;
        }

        public static string ToCommaSeparatedString(this IEnumerable<string> strings)
        {
            if (strings is null) throw new ArgumentNullException(nameof(strings));
            StringBuilder result = new StringBuilder(); // Use StringBuilder for efficient string concatenation
            foreach (string str in strings) result.Append(str + ",");
            if (result.Length > 0) result.Length--;
            return result.ToString();
        }

        public static string[] ToStringArray(this string commaSeparatedString)
        {
            if (commaSeparatedString is null) throw new ArgumentNullException(nameof(commaSeparatedString));
            if (commaSeparatedString == "") return []; // return an empty collection for 0 entries

            string[] result = commaSeparatedString.Split(',');
            for (int i = 0; i < result.Length; i++) result[i] = result[i].Trim(); // Trim each element to remove leading and trailing whitespaces
            return result;
        }
    }
}
