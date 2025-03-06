# The Intelligence Hub 
### A powerful API wrapper for common AGI services, designed to simplify application AI powered app development.

### Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Setup](#setup)
- [Usage](#usage)
- [API Reference](#api-reference)
- [Contributing](#contributing)
- [Contact](#contact)
- [License](#license)
- [Acknowledgements](#acknowledgements)

### Overview
The main goal of this project is to enable rapid setup and development for AI-powered applications.

Key capabilities include:
- Simplified request payloads to AGI clients using pre-configured agent/completion 'profiles'
- Saving and loading conversation history
- Tool execution at external APIs
- RAG database creation and consumption
- Conversation persistence

AI clients are resolved using a custom client factory, allowing new clients to be added easily by implementing the IAGIClient interface. This design also supports image generation from various clients, decoupling the image generation providers from LLM providers within the same agent profile. A similar design could also be used to provide support for multiple RAG services with relative ease.

For more information, please refer to the [Features](#features) section below.

### Features
1. **Saving agentic chat 'profiles'** to simplify client requests, and create preset configurations related to prompting and AI model configs.
2. **Chat completions** including streaming via a SignalR web socket or via server side events, and support for custom models deployed in Azure AI Studio.
3. **RAG database support**, including database creation, document ingestion, and the ability to perform RAG operations across AGI service providers. (Currently, only Azure AI is supported, but an interface is provided for easy extensibility.)
4. **Multimodality and image generation** including the ability to mix and match image generation models accross AGI service providers.
5. **Tool execution** via returning the tool arguments to the client, or executing them against an external API, and returning the resulting http response data.
6. **Conversation history** saving and loading from the database.
7. **Support for multiple AGI providers**, which currently includes Azure AI, OpenAI, and Anthropic.
8. **Chat recursion** allowing LLM models to continue dialogues between themselves and other agent profiles, pass off complex processes to more specialized profiles, and generate additional content that would otherwise surpass the output window.
9. **Resilient design** including automatic load balancing, and retry policies.
10. **Automatic logging** via Application Insights.
11. **Front-end template** to jump start the process of building custom applications to consume the API.
12. **Testing utilities** including unit tests, stress tests, and an AI competency testing module.

### Setup - TO DO
1. Prerequisites: List any prerequisites needed to run the project.
2. Installation: Step-by-step instructions on how to install the project.

### Usage - TO DO


### API Reference 

###CompletionController API Reference
The `CompletionController` handles chat requests via standard HTTP responses and Server-Sent Events (SSE). Authentication is required, and routes are profile-based.

### Base URL `/Completion`

### Endpoints

#### 1\. Chat Request (Standard Response)

-   **HTTP Request**: `POST /Completion/Chat/{name?}`
-   **Description**: Receives a chat request and returns a standard HTTP response.
-   **Route Parameters**:
    -   `name` (optional, string): Profile name for the request. Defaults to `ProfileOptions.Name` in the request body if not provided.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload Schema**: [`CompletionRequest`](https://ai.azure.com/resource/playground?wsid=/subscriptions/1dc97be4-3550-41c0-b2a9-cfdd85ea7713/resourceGroups/AppliedAI/providers/Microsoft.CognitiveServices/accounts/appliedai-eastus2-dev&tid=27dbd7ba-4c05-41cf-af4d-86a141eee738&deploymentId=/subscriptions/1dc97be4-3550-41c0-b2a9-cfdd85ea7713/resourceGroups/AppliedAI/providers/Microsoft.CognitiveServices/accounts/appliedai-eastus2-dev/deployments/gpt-4o#completionrequest)

    `{   
        "ConversationId": "Guid (optional)",   
        "ProfileOptions": {   
            "Name": "string",   
            "Model": "string",   
            "Host": "AGIServiceHosts enum value",   
            "ImageHost": "AGIServiceHosts enum value (optional)",   
            "RagDatabase": "string (optional)",   
            "FrequencyPenalty": "float (optional)",   
            "PresencePenalty": "float (optional)",   
            "Temperature": "float (optional)",   
            "TopP": "float (optional)",   
            "MaxTokens": "int (optional)",   
            "TopLogprobs": "int (optional)",   
            "Logprobs": "bool (optional)",   
            "User": "string (optional)",   
            "ToolChoice": "string (optional)",   
            "ResponseFormat": "string (optional)",   
            "SystemMessage": "string (optional)",   
            "Stop": ["string", "..."],   
            "Tools": [  
                {  
                    "Type": "function",   
                    "Function": {   
                        "Name": "string",   
                        "Description": "string (optional)",   
                        "Parameters": {   
                            "type": "object",   
                            "properties": {   
                                "propertyName": {   
                                    "type": "string",   
                                    "description": "string (optional)"   
                                } 
                            },  
                            "required": ["propertyName"]  
                        } 
                    },  
                    "ExecutionUrl": "string (optional)",   
                    "ExecutionMethod": "string (optional)",   
                    "ExecutionBase64Key": "string (optional)"   
                } 
            ],  
            "MaxMessageHistory": "int (optional)",   
            "ReferenceProfiles": ["string", "..."]  
        },  
        "Messages": [  
            {  
                "Role": "User | Assistant | System | Tool",   
                "Content": "string",   
                "Base64Image": "string (optional)",   
                "TimeStamp": "DateTime (UTC)"   
            } 
        ] 
    } `

-   **Responses**:
    -   `200 OK`: Returns the chat completion response. Schema: [`CompletionResponse`](https://ai.azure.com/resource/playground?wsid=/subscriptions/1dc97be4-3550-41c0-b2a9-cfdd85ea7713/resourceGroups/AppliedAI/providers/Microsoft.CognitiveServices/accounts/appliedai-eastus2-dev&tid=27dbd7ba-4c05-41cf-af4d-86a141eee738&deploymentId=/subscriptions/1dc97be4-3550-41c0-b2a9-cfdd85ea7713/resourceGroups/AppliedAI/providers/Microsoft.CognitiveServices/accounts/appliedai-eastus2-dev/deployments/gpt-4o#completionresponse)
    -   `400 Bad Request`: Request payload fails validation.
    -   `404 Not Found`: Profile or resource not found.
    -   `429 Too Many Requests`: Rate limits exceeded.
    -   `500 Internal Server Error`: Unexpected server error.

        **Example Request**:

`curl -X POST "https://yourapi.com/Completion/Chat/ChatProfile" \  -H "Authorization: Bearer {token}" \  -H "Content-Type: application/json" \  -d '{  "ConversationId": "d290f1ee-6c54-4b01-90e6-d701748f0851", "ProfileOptions": { "Name": "ChatProfile", "Model": "gpt-4o", "Host": "OpenAI", "Temperature": 0.7, "MaxTokens": 150 }, "Messages": [ { "Role": "User", "Content": "Hello, how are you?" } ] }'  `

#### 2\. Chat Request (SSE Streaming)

-   **HTTP Request**: `POST /Completion/SSE/{name?}`
-   **Description**: Processes a chat request and returns the result via SSE.
-   **Route Parameters**:
    -   `name` (optional, string): Profile name for the request. Defaults to `ProfileOptions.Name` in the request body if not provided.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload Schema**: Same as the standard chat request -- see [`CompletionRequest`](https://ai.azure.com/resource/playground?wsid=/subscriptions/1dc97be4-3550-41c0-b2a9-cfdd85ea7713/resourceGroups/AppliedAI/providers/Microsoft.CognitiveServices/accounts/appliedai-eastus2-dev&tid=27dbd7ba-4c05-41cf-af4d-86a141eee738&deploymentId=/subscriptions/1dc97be4-3550-41c0-b2a9-cfdd85ea7713/resourceGroups/AppliedAI/providers/Microsoft.CognitiveServices/accounts/appliedai-eastus2-dev/deployments/gpt-4o#completionrequest).
-   **Response**:
    -   **Content-Type**: `text/event-stream`
    -   **Response Format**: Streamed in chunks of [`CompletionStreamChunk`](https://ai.azure.com/resource/playground?wsid=/subscriptions/1dc97be4-3550-41c0-b2a9-cfdd85ea7713/resourceGroups/AppliedAI/providers/Microsoft.CognitiveServices/accounts/appliedai-eastus2-dev&tid=27dbd7ba-4c05-41cf-af4d-86a141eee738&deploymentId=/subscriptions/1dc97be4-3550-41c0-b2a9-cfdd85ea7713/resourceGroups/AppliedAI/providers/Microsoft.CognitiveServices/accounts/appliedai-eastus2-dev/deployments/gpt-4o#completionstreamchunk).
-   **Response Codes**:
    -   `200 OK`: Stream initiated successfully.
    -   `400 Bad Request`: Request payload fails validation.
    -   `404 Not Found`: Profile or resource not found.
    -   `429 Too Many Requests`: Rate limits exceeded.
    -   `500 Internal Server Error`: Unexpected server error.

        **Example Request**:

`curl -X POST "https://yourapi.com/Completion/SSE/ChatProfile" \  -H "Authorization: Bearer {token}" \  -H "Content-Type: application/json" \  -N \ -d 
'{  
    "ProfileOptions": { 
        "Name": "ChatProfile", 
        "Model": "gpt-4o", 
        "Host": "OpenAI", 
        "Temperature": 0.7, 
        "MaxTokens": 150 
    }, 
    "Messages": [ 
        { 
            "Role": "User", 
            "Content": "Hello, stream my response!" 
        } 
    ] 
}'  `\
**Example SSE Response**:

`data: { "chunkProperty": "chunkValue", ... } data: { "chunkProperty": "chunkValue", ... } `

### Data Transfer Objects (DTOs)

#### CompletionRequest
Represents the payload for a chat request.

-   **ConversationId**: `Guid` (optional)
-   **ProfileOptions**: `Profile` -- Options for the chat profile.
-   **Messages**: `List<Message>` -- Array of chat messages.

#### Profile
Contains profile and model configuration details.

-   **Name**: `string` (required)
-   **Model**: `string` (required)
-   **Host**: `AGIServiceHosts enum` (required)
-   **ImageHost**: `AGIServiceHosts enum` (optional)
-   **RagDatabase**: `string` (optional)
-   **FrequencyPenalty**: `float` (optional)
-   **PresencePenalty**: `float` (optional)
-   **Temperature**: `float` (optional)
-   **TopP**: `float` (optional)
-   **MaxTokens**: `int` (optional)
-   **TopLogprobs**: `int` (optional)
-   **Logprobs**: `bool` (optional)
-   **User**: `string` (optional)
-   **ToolChoice**: `string` (optional)
-   **ResponseFormat**: `string` (optional)
-   **SystemMessage**: `string` (optional)
-   **Stop**: `array of strings` (optional)
-   **Tools**: `List<Tool>` (optional)
-   **MaxMessageHistory**: `int` (optional)
-   **ReferenceProfiles**: `array of strings` (optional)

#### Message
Represents an individual chat message.

-   **Role**: `enum (User, Assistant, System, Tool)` -- Role of the sender.
-   **Content**: `string` (required) -- Message text.
-   **Base64Image**: `string` (optional) -- Base64-encoded image string.
-   **TimeStamp**: `DateTime` (UTC)

#### CompletionResponse & CompletionStreamChunk
These types encapsulate the response data for standard and streaming endpoints respectively.

### Error Handling
The API uses standard HTTP status codes to indicate the result of the operation:

-   `400 Bad Request`: Issues with validation.
-   `404 Not Found`: Profile or resource not found.
-   `429 Too Many Requests`: Rate limits exceeded.
-   `500 Internal Server Error`: Unexpected server error.

### Authentication
All endpoints in the `CompletionController` are secured with the `[Authorize]` attribute. Ensure to include a valid authentication token in your request headers.

### Summary

-   Use **POST** `/Completion/Chat/{name?}` for standard chat completions.
-   Use **POST** `/Completion/SSE/{name?}` to receive a streaming response via SSE.
-   Validate that the `CompletionRequest` object contains a valid `ProfileOptions` and a non-empty `Messages` array.
-   Handle responses according to the status codes provided.

    For further details on data structures and validation rules, refer to the inline documentation in the source code.

* * * * *

IntelligenceHub API Reference

This documentation provides an overview of the IntelligenceHub API components used to stream chat completions to clients via SignalR.

### SignalR Hub: ChatHub

#### Overview
The `ChatHub` is a SignalR hub designed to stream chat completion responses to connected clients. It is secured with authorization.

#### Constructor

`public ChatHub(ICompletionLogic completionLogic, IValidationHandler validationHandler)`

-   **Parameters**:
    -   `completionLogic`: Implements the business logic to process chat completion requests.
    -   `validationHandler`: Validates the request payload.

#### Methods

##### Send(CompletionRequest completionRequest)

-   **Description**: Sends a chat completion request and streams response chunks back to the client.
-   **Parameters**:
    -   `completionRequest` (`CompletionRequest`): The request object.
-   **Returns**: An asynchronous `Task` streaming response chunks using SignalR.
-   **Workflow**:
    1.  **Validation**:
        -   Validates the request.
        -   If validation fails, an error is sent to the client.
    2.  **Processing**:
        -   If validation passes, initiates the chat completion process.
    3.  **Response Streaming**:
        -   Streams response chunks to the client.

### Data Transfer Objects (DTOs)

#### CompletionRequest

-   **Namespace**: `IntelligenceHub.API.DTOs`
-   **Description**: Represents a chat completion request.
-   **Properties**:
    -   `Guid? ConversationId`
    -   `Profile ProfileOptions`
    -   `List<Message> Messages`

#### Message

-   **Namespace**: `IntelligenceHub.API.DTOs`
-   **Description**: Represents an individual chat message.
-   **Properties**:
    -   `Role? Role`
    -   `string User` (Ignored during JSON serialization)
    -   `string Content`
    -   `string? Base64Image`
    -   `DateTime TimeStamp`

#### Profile

-   **Namespace**: `IntelligenceHub.API.DTOs`
-   **Description**: Contains configuration settings for a chat profile.
-   **Properties**:
    -   `int Id` (Ignored during JSON serialization)
    -   `string Name`
    -   `string Model`
    -   `AGIServiceHosts Host`
    -   `AGIServiceHosts? ImageHost`
    -   `string? RagDatabase`
    -   `float? FrequencyPenalty`
    -   `float? PresencePenalty`
    -   `float? Temperature`
    -   `float? TopP`
    -   `int? MaxTokens`
    -   `int? TopLogprobs`
    -   `bool? Logprobs`
    -   `string? User`
    -   `string? ToolChoice`
    -   `string? ResponseFormat`
    -   `string? SystemMessage`
    -   `string[]? Stop`
    -   `List<Tool>? Tools`
    -   `int? MaxMessageHistory`
    -   `string[]? ReferenceProfiles`

#### Tool

-   **Namespace**: `IntelligenceHub.API.DTOs.Tools`
-   **Description**: Represents a tool for system messages and logic execution.
-   **Properties**:
    -   `int Id` (Ignored during JSON serialization)
    -   `string Type`
    -   `Function Function`
    -   `string? ExecutionUrl`
    -   `string? ExecutionMethod`
    -   `string? ExecutionBase64Key`

#### CompletionStreamChunk

-   **Namespace**: `IntelligenceHub.API.DTOs`
-   **Description**: Represents a single chunk of streamed completion data.
-   **Properties**:
    -   `int Id` (Ignored during JSON serialization)
    -   `string CompletionUpdate`
    -   `string? Base64Image`
    -   `Role? Role`
    -   `string? User` (Ignored during JSON serialization)
    -   `FinishReasons? FinishReason`
    -   `Dictionary<string, string> ToolCalls`
    -   `List<HttpResponseMessage> ToolExecutionResponses`

### Global Enums & Variables

-   **APIResponseStatusCodes**: Represents various HTTP response statuses.
    -   `NotFound`
    -   `TooManyRequests`
    -   `InternalError`
-   **Role**: Enumerates message roles (e.g., `User`, `Assistant`).
-   **FinishReasons**: Enumerates reasons for ending the chat completion stream.

### Usage Example

`// Create a new completion request  
var request = new CompletionRequest 
{
    ConversationId = Guid.NewGuid(), 
    ProfileOptions = new Profile  
    { 
        Name = "Default Profile",  
        Model = "gpt-3.5-turbo",  
        Host = AGIServiceHosts.YourPrimaryHost, 
        Temperature = 0.7f,  
        MaxTokens = 150   
    }, 
    Messages = new List<Message>  
    {  
        new Message  
        { 
            Role = Role.User, 
            Content = "Hello, can you assist me with my query?"   
        } 
    } 
};
// Invoke the Send method on the ChatHub using SignalR  
await hubConnection.InvokeAsync("Send", request); `

### Notes

-   **Validation**: The `ChatHub` validates incoming `CompletionRequest` objects. Validation errors result in immediate responses to the client.
-   **Streaming**: Responses are streamed back to the client in chunks. Each chunk includes a status code and possibly error messages.
-   **Security**: The hub is secured with `[Authorize]`, ensuring only authenticated users can invoke methods.

* * * * *

Message History API Reference

### Endpoints

#### 1\. Get Conversation History

-   **Method**: `GET`
-   **Endpoint**: `/api/conversations/{id}`
-   **Query Parameters**:
    -   `count` (int, required): Number of messages to retrieve.
    -   `page` (int, required): Page offset (pagination).
-   **Description**: Retrieves conversation history for a given conversation ID.
-   **Response**:
    -   **200 OK**: Returns a list of messages.
    -   **400 Bad Request**: If parameters are invalid.
    -   **404 Not Found**: If the conversation is not found.

        **Example Response**:

`{   
    "status": "Ok",   
    "data": [  
        {  
            "Role": "User",   
            "Content": "Hello, how can I help you?",   
            "Base64Image": null,   
            "TimeStamp": "2025-03-05T12:34:56Z"   
        }, 
        {  
            "Role": "Assistant",   
            "Content": "I need assistance with my order.",   
            "Base64Image": null,   
            "TimeStamp": "2025-03-05T12:35:10Z"   
        } 
    ] 
} 
`

#### 2\. Update or Create Conversation

-   **Method**: `POST`
-   **Endpoint**: `/api/conversations/{conversationId}`
-   **Request Body**: JSON array of `Message` objects.
-   **Description**: Validates and updates (or creates) a conversation by adding the provided list of messages.
-   **Response**:
    -   **200 OK**: Returns the successfully added messages.
    -   **400 Bad Request**: Validation errors.

        **Example Request**:

`[  
    {  
        "Role": "User",   
        "Content": "What is the status of my order?",   
        "Base64Image": null,   
        "TimeStamp": "2025-03-05T12:40:00Z"   
    }, 
    {  
        "Role": "Assistant",   
        "Content": "Your order is being processed.",   
        "Base64Image": null,   
        "TimeStamp": "2025-03-05T12:41:00Z"   
    } 
] `

#### 3\. Delete Conversation

-   **Method**: `DELETE`
-   **Endpoint**: `/api/conversations/{id}`
-   **Description**: Deletes the entire conversation identified by the provided ID.
-   **Response**:
    -   **200 OK**: Indicates successful deletion.
    -   **404 Not Found**: If the conversation is not found.

        **Example Response**:

`{   "status": "Ok",   "data": true  } `

#### 4\. Add Message to Conversation

-   **Method**: `POST`
-   **Endpoint**: `/api/conversations/{conversationId}/message`
-   **Request Body**: JSON representation of a `Message` object.
-   **Description**: Adds a single message to an existing conversation.
-   **Response**:
    -   **200 OK**: Returns the newly added message.
    -   **404 Not Found**: If the conversation is not found.

        **Example Request**:

`{   
    "Role": "User",   
    "Content": "Can I update my shipping address?",   
    "Base64Image": null,   
    "TimeStamp": "2025-03-05T12:45:00Z"  
} `

#### 5\. Delete Message from Conversation

-   **Method**: `DELETE`
-   **Endpoint**: `/api/conversations/{conversationId}/message/{messageId}`
-   **Description**: Deletes a specific message from the conversation history.
-   **Response**:
    -   **200 OK**: Indicates successful deletion.
    -   **404 Not Found**: If the conversation or message is not found.

        **Example Response**:

`{   "status": "Ok",   "data": true  } `

### Data Models

#### Message

| Field | Type | Description |
| --- | --- | --- |
| `Role` | `Role` (enum) | The role of the message sender (e.g., User, Assistant, System). |
| `User` | `string` | The username associated with the message (ignored during JSON serialization). |
| `Content` | `string` | The textual content of the message. |
| `Base64Image` | `string` (nullable) | Base64-encoded image content (if applicable). |
| `TimeStamp` | `DateTime` | The UTC timestamp when the message was created. |
|   |  |  |

### Validation
The API employs validation for both single messages and lists of messages:

-   **ValidateMessageList(List messageList)**:
    -   Ensures the list is not null or empty.
    -   Confirms at least one message in the list has a `Role` of `User`.
    -   Iterates over each message to run individual validations.
-   **ValidateMessage(Message message)**:
    -   Checks that the message is not null.
    -   Ensures a role is provided.
    -   Requires that either

## Contributing
Contributions are welcome! Please follow the steps below to contribute to the project:
1. (Optional) If you would like to gaurentee your changes will be merged, please open an issue to determine if your desired changes align with the project's goals, or choose from an existing issue. Otherwise, feel free to skip this step.
2. Fork the project, create a new branch, and make your changes. 
3. Ensure that any changes include relevant unit tests and IntelliSense documentation, or update existing tests/documentation as needed.
4. Push your changes to your fork, and raise a pull request.

If for whatever reason we miss your request, please feel free to notify us on the pull request itself, at the email provided in the [Contact](#contact) section, or any other method you have at your disposal.

## Contact
For any questions comments or concerns, please reach out to Applied.AI.Help@gmail.com, or open an issue in the repository.

## License
This project is licensed under the Elastic 2.0 license - see the `LICENSE` file for more details.

## Acknowledgements
This project was developed by the Applied AI team, currently consisting of the following members: 
- [Jacob J. Thomas](https://github.com/jacob-j-thomas)

The project relies on the following third-party packages and libraries:
- **Anthropic.SDK**: [MIT License](https://github.com/tghamm/Anthropic.SDK/blob/main/LICENSE.md)
- **Azure.AI.OpenAI**: [MIT License](https://github.com/Azure/azure-sdk-for-net/blob/main/LICENSE.txt)
- **Azure.Search.Documents**: [MIT License](https://github.com/Azure/azure-sdk-for-net/blob/main/LICENSE.txt)
- **DotNetEnv**: [MIT License](https://github.com/tonerdo/dotnet-env/blob/master/LICENSE)
- **Microsoft.ApplicationInsights.AspNetCore**: [MIT License](https://github.com/microsoft/ApplicationInsights-dotnet/blob/develop/LICENSE)
- **Microsoft.AspNet.Mvc**: [Apache License 2.0](https://github.com/aspnet/AspNetWebStack/blob/main/LICENSE.txt)
- **Microsoft.AspNetCore.Authentication.JwtBearer**: [MIT License](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt)
- **Microsoft.AspNetCore.Mvc.Core**: [MIT License](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt)
- **Microsoft.AspNetCore.SignalR.Common**: [MIT License](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt)
- **Microsoft.AspNetCore.SignalR.Core**: [MIT License](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt)
- **Microsoft.Data.SqlClient**: [MIT License](https://github.com/dotnet/SqlClient/blob/main/LICENSE)
- **Microsoft.EntityFrameworkCore**: [MIT License](https://github.com/dotnet/efcore/blob/main/LICENSE.txt)
- **Microsoft.EntityFrameworkCore.SqlServer**: [MIT License](https://github.com/dotnet/efcore/blob/main/LICENSE.txt)
- **Microsoft.Extensions.Hosting.Abstractions**: [MIT License](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)
- **Microsoft.Extensions.Http**: [MIT License](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)
- **Microsoft.Extensions.Http.Polly**: [MIT License](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt)
- **Microsoft.Extensions.Options**: [MIT License](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)
- **Newtonsoft.Json**: [MIT License](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
- **NSwag.AspNetCore**: [MIT License](https://github.com/RicoSuter/NSwag/blob/master/LICENSE.md)
- **OpenAI**: [MIT License](https://github.com/openai/openai-dotnet/blob/main/LICENSE)
- **Owin.Extensions**: [Apache License 2.0](https://github.com/owin-contrib/owin-hosting/blob/master/LICENSE.txt)
- **Swashbuckle.AspNetCore**: [MIT License](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/LICENSE)
- **Swashbuckle.AspNetCore.Annotations**: [MIT License](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/LICENSE)
- **System.ClientModel**: [MIT License](https://github.com/Azure/azure-sdk-for-net/blob/main/LICENSE.txt)
- **System.Text.Json**: [MIT License](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)

For any dependencies missing from this list, please refer to their respective repositories for licensing information.