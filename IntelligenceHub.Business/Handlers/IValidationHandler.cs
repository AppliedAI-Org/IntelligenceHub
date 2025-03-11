using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.API.DTOs.Tools;

namespace IntelligenceHub.Business.Handlers
{
    /// <summary>
    /// A class that handles validation for incoming DTOs sent to the API.
    /// </summary>
    public interface IValidationHandler
    {
        /// <summary>
        /// Validates the API chat request DTO.
        /// </summary>
        /// <param name="chatRequest">The chat request DTO.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateChatRequest(CompletionRequest chatRequest);

        /// <summary>
        /// Validates the API profile DTO.
        /// </summary>
        /// <param name="profile">The profile to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateAPIProfile(Profile profile);

        /// <summary>
        /// Validates the base DTO/Profile DTO for the chat request API and profile API.
        /// </summary>
        /// <param name="profile">The profile to validate.</param>
        /// /// <param name="messages">Messages if any need to be validated against the host API's parameters.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateProfileOptions(Profile profile, List<Message>? messages = null);

        /// <summary>
        /// Validates the tool DTO.
        /// </summary>
        /// <param name="tool">The tool DTO to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateTool(Tool tool);

        /// <summary>
        /// Validates the properties of a tool.
        /// </summary>
        /// <param name="properties">The properties to validate in a dictionary form where the 
        /// key is the property name, and the value is the property object.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateProperties(Dictionary<string, Property> properties);

        /// <summary>
        /// Validates a list of messages.
        /// </summary>
        /// <param name="messageList">The list of messages to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateMessageList(List<Message> messageList);

        /// <summary>
        /// Validates a single message.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateMessage(Message message);

        /// <summary>
        /// Validates the index definition DTO for RAG operations.
        /// </summary>
        /// <param name="index">The index definition.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? ValidateIndexDefinition(IndexMetadata index);

        /// <summary>
        /// Validates the index name for RAG operations.
        /// </summary>
        /// <param name="tableName">The RAG index name that will be used to create an SQL table.</param>
        /// <returns>A bool indicating if validation passed.</returns>
        public bool IsValidIndexName(string tableName);

        /// <summary>
        /// Validates the RAG document upsert request DTO.
        /// </summary>
        /// <param name="request">The document upsert request to validate.</param>
        /// <returns>An error message string, or null if validation passes.</returns>
        public string? IsValidRagUpsertRequest(RagUpsertRequest documentName);
    }
}
