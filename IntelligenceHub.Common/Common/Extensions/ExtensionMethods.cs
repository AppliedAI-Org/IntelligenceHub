using OpenAI.Chat;
using System.Text;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Common.Extensions
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts a string to a FinishReason enum value.
        /// </summary>
        /// <param name="finishReason">The finish reason to convert.</param>
        /// <returns>The converted finish reason.</returns>
        public static FinishReason? ConvertStringToFinishReason(this string finishReason)
        {
            finishReason = finishReason.ToLower();
            if (finishReason == ChatFinishReason.Stop.ToString().ToLower()) return FinishReason.Stop;
            if (finishReason == ChatFinishReason.Length.ToString().ToLower()) return FinishReason.Length;
            if (finishReason == ChatFinishReason.ToolCalls.ToString().ToLower()) return FinishReason.ToolCalls;
            if (finishReason == ChatFinishReason.FunctionCall.ToString().ToLower()) return FinishReason.ToolCalls;
            if (finishReason == ChatFinishReason.ContentFilter.ToString().ToLower()) return FinishReason.ContentFilter;
            return null;
        }

        /// <summary>
        /// Converts a string to a Role enum value.
        /// </summary>
        /// <param name="role">The role to convert.</param>
        /// <returns>The converted role.</returns>
        public static Role? ConvertStringToRole(this string role)
        {
            role = role.ToLower();
            if (role == ChatMessageRole.Assistant.ToString().ToLower()) return Role.Assistant;
            else if (role == ChatMessageRole.User.ToString().ToLower()) return Role.User;
            else if (role == ChatMessageRole.Tool.ToString().ToLower()) return Role.Tool;
            else if (role == ChatMessageRole.Function.ToString().ToLower()) return Role.Tool;
            else if (role == ChatMessageRole.System.ToString().ToLower()) return Role.System;
            return null;
        }

        /// <summary>
        /// Converts a string to a QueryType enum value.
        /// </summary>
        /// <param name="queryType">The query type to convert.</param>
        /// <returns>The converted query type.</returns>
        public static QueryType? ConvertStringToQueryType(this string queryType)
        {
            if (queryType == QueryType.Simple.ToString()) return QueryType.Simple;
            if (queryType == QueryType.Semantic.ToString()) return QueryType.Semantic;
            if (queryType == QueryType.Full.ToString()) return QueryType.Full;
            return null;
        }

        /// <summary>
        /// Converts a string to a SearchInterpolation enum value.
        /// </summary>
        /// <param name="interpolation">The interpolation to convert.</param>
        /// <returns>The converted SearchInterpolation.</returns>
        public static SearchInterpolation? ConvertStringToSearchInterpolation(this string interpolation)
        {
            if (interpolation == SearchInterpolation.Linear.ToString()) return SearchInterpolation.Linear;
            if (interpolation == SearchInterpolation.Constant.ToString()) return SearchInterpolation.Constant;
            if (interpolation == SearchInterpolation.Quadratic.ToString()) return SearchInterpolation.Quadratic;
            if (interpolation == SearchInterpolation.Logarithmic.ToString()) return SearchInterpolation.Quadratic;
            return null;
        }

        /// <summary>
        /// Converts a string to a SearchAggregation enum value.
        /// </summary>
        /// <param name="aggregation">The aggregation to convert.</param>
        /// <returns>The converted search aggregation.</returns>
        public static SearchAggregation? ConvertStringToSearchAggregation(this string aggregation)
        {
            if (aggregation == SearchAggregation.Average.ToString()) return SearchAggregation.Average;
            if (aggregation == SearchAggregation.Sum.ToString()) return SearchAggregation.Sum;
            if (aggregation == SearchAggregation.Maximum.ToString()) return SearchAggregation.Maximum;
            if (aggregation == SearchAggregation.Minimum.ToString()) return SearchAggregation.Minimum;
            if (aggregation == SearchAggregation.FirstMatching.ToString()) return SearchAggregation.FirstMatching;
            return null;
        }

        /// <summary>
        /// Converts a string to a AGIServiceHosts enum value.
        /// </summary>
        /// <param name="strings">The list of strings to convert.</param>
        /// <returns>A comma delimited string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided string is null.</exception>
        public static string ToCommaSeparatedString(this IEnumerable<string> strings)
        {
            if (strings is null) throw new ArgumentNullException(nameof(strings));
            StringBuilder result = new StringBuilder(); // Use StringBuilder for efficient string concatenation
            foreach (string str in strings) result.Append(str + ",");
            if (result.Length > 0) result.Length--;
            return result.ToString();
        }

        /// <summary>
        /// Converts a comma delimited string to a list of strings.
        /// </summary>
        /// <param name="commaSeparatedString">The commad delimited list of strings.</param>
        /// <returns>An array of strings.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the targeted string is null.</exception>
        public static string[] ToStringArray(this string commaSeparatedString)
        {
            if (commaSeparatedString is null) throw new ArgumentNullException(nameof(commaSeparatedString));
            if (commaSeparatedString == "") return []; // return an empty collection for 0 entries

            string[] result = commaSeparatedString.Split(',');
            for (int i = 0; i < result.Length; i++) result[i] = result[i].Trim(); // Trim each element to remove leading and trailing whitespaces
            return result;
        }

        /// <summary>
        /// Converts a string to a AGIServiceHosts enum value.
        /// </summary>
        /// <param name="hostString">The host name to convert.</param>
        /// <returns>The converted AGIServicesHosts enum.</returns>
        /// <exception cref="ArgumentException">Thrown if the provided string is null.</exception>
        public static AGIServiceHosts? ConvertToServiceHost(this string hostString)
        {
            if (hostString == null) throw new ArgumentException(nameof(hostString));

            hostString = hostString.ToLower();
            if (hostString == AGIServiceHosts.OpenAI.ToString().ToLower()) return AGIServiceHosts.OpenAI;
            else if (hostString == AGIServiceHosts.Azure.ToString().ToLower()) return AGIServiceHosts.Azure;
            else if (hostString == AGIServiceHosts.Anthropic.ToString().ToLower()) return AGIServiceHosts.Anthropic;
            return null;
        }
    }
}
