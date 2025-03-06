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
# CompletionController API Reference

The `CompletionController` is responsible for processing chat requests, both as a standard HTTP response and via Server-Sent Events (SSE). This API requires authentication and uses route-based profile naming. The controller leverages business logic for completions and validation to ensure request integrity.

---

## Base URL /Completion

---

## Endpoints

### 1. Chat Request (Standard Response)

#### **HTTP Request** POST /Completion/Chat/{name?}

#### **Description**

This endpoint receives a chat request and returns a standard HTTP response with the chat completion result.

#### **Route Parameters**

- **name** (optional, string):  

  The profile name to be used for constructing the request. If not provided, the name defined within the request body (`ProfileOptions.Name`) will be used.

#### **Request Body**

- **Content-Type:** `application/json`

- **Payload Schema:** [`CompletionRequest`](#completionrequest)

```json

{

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

                              "properties": { "propertyName": { "type": "string", "description": "string (optional)" } },

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

}

#### **Responses**

-   **200 OK**
    -   **Description:** Returns the chat completion response.
    -   **Schema:** [`CompletionResponse`](#completionresponse)
-   **400 Bad Request**
    -   **Description:** Returned if the request payload fails validation.
-   **404 Not Found**
    -   **Description:** Returned if the requested profile or resource is not found.
-   **429 Too Many Requests**
    -   **Description:** Returned if request rate limits are exceeded.
-   **500 Internal Server Error**
    -   **Description:** Returned if an unexpected error occurs on the server.

#### **Example Request**

bash

CopyEdit

`curl -X POST "https://yourapi.com/Completion/Chat/ChatProfile"\
  -H "Authorization: Bearer {token}"\
  -H "Content-Type: application/json"\
  -d '{
        "ConversationId": "d290f1ee-6c54-4b01-90e6-d701748f0851",
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
            "Content": "Hello, how are you?"
          }
        ]
      }'`

* * * * *

### 2\. Chat Request (SSE Streaming)

#### **HTTP Request**

swift

CopyEdit

`POST /Completion/SSE/{name?}`

#### **Description**

This endpoint processes a chat request and returns the result as a stream of Server-Sent Events (SSE). The response headers are set to stream data in real-time.

#### **Route Parameters**

-   **name** (optional, string):\
    The profile name used for constructing the request. If omitted, the name from `ProfileOptions.Name` in the request body will be used.

#### **Request Body**

-   **Content-Type:** `application/json`
-   **Payload Schema:** Same as the standard chat request -- see [CompletionRequest](#completionrequest).

#### **Response**

-   **Content-Type:** `text/event-stream`
-   **Response Format:**\
    The response is streamed in chunks where each chunk is a JSON-serialized object of type [`CompletionStreamChunk`](#completionstreamchunk).

#### **Response Codes**

-   **200 OK**
    -   **Description:** Stream initiated successfully. Each SSE chunk contains a part of the chat completion response.
-   **400 Bad Request**
    -   **Description:** Returned if the request payload fails validation.
-   **404 Not Found**
    -   **Description:** Returned if the requested profile or resource is not found.
-   **429 Too Many Requests**
    -   **Description:** Returned if request rate limits are exceeded.
-   **500 Internal Server Error**
    -   **Description:** Returned if an unexpected error occurs on the server.

#### **Example Request**

bash

CopyEdit

`curl -X POST "https://yourapi.com/Completion/SSE/ChatProfile"\
  -H "Authorization: Bearer {token}"\
  -H "Content-Type: application/json"\
  -N\
  -d '{
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
      }'`

On a successful request, you will receive a continuous stream of SSE messages formatted as follows:

css

CopyEdit

`data: { "chunkProperty": "chunkValue", ... }

data: { "chunkProperty": "chunkValue", ... }`

* * * * *

Data Transfer Objects (DTOs)
----------------------------

### CompletionRequest

Represents the payload for a chat request.

-   **ConversationId**: *Guid (optional)*
-   **ProfileOptions**: *Profile* -- Options for the chat profile.
-   **Messages**: *List<Message>* -- Array of chat messages.\
    **Note:** Only the `Messages` array is required.

### Profile

Contains profile and model configuration details.

-   **Name**: *string (required)* -- Name of the profile.
-   **Model**: *string (required)* -- The model to be used.
-   **Host**: *AGIServiceHosts enum (required)* -- The host service (e.g., OpenAI, Azure, Anthropic).
-   **ImageHost**: *AGIServiceHosts enum (optional)*
-   **RagDatabase**: *string (optional)*
-   **FrequencyPenalty**: *float (optional)*
-   **PresencePenalty**: *float (optional)*
-   **Temperature**: *float (optional)*
-   **TopP**: *float (optional)*
-   **MaxTokens**: *int (optional)*
-   **TopLogprobs**: *int (optional)*
-   **Logprobs**: *bool (optional)*
-   **User**: *string (optional)*
-   **ToolChoice**: *string (optional)*
-   **ResponseFormat**: *string (optional)*
-   **SystemMessage**: *string (optional)*
-   **Stop**: *array of strings (optional)*
-   **Tools**: *List<Tool> (optional)*
-   **MaxMessageHistory**: *int (optional)*
-   **ReferenceProfiles**: *array of strings (optional)*

### Message

Represents an individual chat message.

-   **Role**: *enum (User, Assistant, System, Tool)* -- Role of the sender.
-   **Content**: *string (required)* -- Message text.
-   **Base64Image**: *string (optional)* -- Base64-encoded image string.
-   **TimeStamp**: *DateTime* -- Timestamp in UTC.
-   **User**: *string (ignored in JSON)* -- Internal user field.

### CompletionResponse & CompletionStreamChunk

These types encapsulate the response data for standard and streaming endpoints respectively. Their detailed structures are defined within the business logic layer and are returned on successful completion.

* * * * *

Error Handling
--------------

The API uses standard HTTP status codes to indicate the result of the operation:

-   **400 Bad Request:** Indicates issues with validation. The response body contains an error message detailing the problem.
-   **404 Not Found:** Indicates that the requested profile or resource could not be found.
-   **429 Too Many Requests:** Indicates that the client has sent too many requests in a given period.
-   **500 Internal Server Error:** Indicates an unexpected server error. A generic error message is returned.

* * * * *

Authentication
--------------

All endpoints in the `CompletionController` are secured with the `[Authorize]` attribute. Ensure that you include a valid authentication token in your request headers.

* * * * *

Summary
-------

-   Use **POST** `/Completion/Chat/{name?}` for standard chat completions.
-   Use **POST** `/Completion/SSE/{name?}` to receive a streaming response via SSE.
-   Validate that the `CompletionRequest` object contains a valid `ProfileOptions` and a non-empty `Messages` array.
-   Handle responses according to the status codes provided.

For further details on the data structures and validation rules, please refer to the inline documentation in the source code.

# IntelligenceHub API Reference

This documentation provides an overview of the IntelligenceHub API components used to stream chat completions to clients via SignalR. It includes details on the SignalR hub (`ChatHub`) and all associated Data Transfer Objects (DTOs) used in the chat completion process.

---

## SignalR Hub: ChatHub

### Overview
The `ChatHub` is a SignalR hub designed to stream chat completion responses to connected clients. It is secured with authorization, meaning only authenticated users can access its functionality.

### Constructor

```csharp
public ChatHub(ICompletionLogic completionLogic, IValidationHandler validationHandler)
```

-   **Parameters:**
    -   `completionLogic`: Implements the business logic to process chat completion requests.
    -   `validationHandler`: Validates the request payload to ensure it meets required criteria.

### Methods

#### Send(CompletionRequest completionRequest)

-   **Description:**\
    Sends a chat completion request to the server and streams response chunks back to the client.

-   **Parameters:**

    -   `completionRequest` (`CompletionRequest`): The request object containing conversation details, profile options, and messages.
-   **Returns:**\
    An asynchronous `Task` that streams response chunks to the client using SignalR.

-   **Workflow:**

    1.  **Validation:**\
        The request is validated using `_validationLogic.ValidateChatRequest(completionRequest)`.

        -   If validation fails, an error message is sent to the client with the method `Clients.Caller.SendAsync("broadcastMessage", errorMessage)`.
    2.  **Processing:**\
        If validation passes, the hub calls `_completionLogic.StreamCompletion(completionRequest)` to initiate the chat completion process.

    3.  **Response Streaming:**\
        For each chunk returned from the streaming response:

        -   A status code and an error message (if any) are sent back to the client using `Clients.Caller.SendAsync("broadcastMessage", message)`.
        -   Special cases are handled based on the response status code (e.g., `NotFound`, `TooManyRequests`, `InternalError`).

* * * * *

Data Transfer Objects (DTOs)
----------------------------

### CompletionRequest

-   **Namespace:** `IntelligenceHub.API.DTOs`

-   **Description:**\
    Represents a chat completion request.

-   **Properties:**

    -   `Guid? ConversationId`\
        *Optional identifier for the conversation.*
    -   `Profile ProfileOptions`\
        *Profile configuration options for processing the chat.*
    -   `List<Message> Messages`\
        *List of messages included in the conversation.*

* * * * *

### Message

-   **Namespace:** `IntelligenceHub.API.DTOs`

-   **Description:**\
    Represents an individual chat message.

-   **Properties:**

    -   `Role? Role`\
        *The role of the message sender (e.g., user, assistant).*
    -   `string User` *(Ignored during JSON serialization)*\
        *The username of the sender.*
    -   `string Content`\
        *The text content of the message.*
    -   `string? Base64Image`\
        *Optional Base64-encoded image attached to the message.*
    -   `DateTime TimeStamp`\
        *Timestamp of when the message was created (defaults to `DateTime.UtcNow`).*

* * * * *

### Profile

-   **Namespace:** `IntelligenceHub.API.DTOs`

-   **Description:**\
    Contains configuration settings for a chat profile.

-   **Properties:**

    -   `int Id` *(Ignored during JSON serialization)*\
        *Unique identifier for the profile.*
    -   `string Name`\
        *Name of the profile.*
    -   `string Model`\
        *Specifies the model to be used for processing.*
    -   `AGIServiceHosts Host`\
        *Primary service host for chat completions.*
    -   `AGIServiceHosts? ImageHost`\
        *Optional image processing service host.*
    -   `string? RagDatabase`\
        *Optional database name for Retrieval-Augmented Generation (RAG).*
    -   `float? FrequencyPenalty`\
        *Optional penalty applied to repetitive token usage.*
    -   `float? PresencePenalty`\
        *Optional penalty for token presence.*
    -   `float? Temperature`\
        *Controls randomness in the response.*
    -   `float? TopP`\
        *Probability threshold for token selection.*
    -   `int? MaxTokens`\
        *Maximum token count for responses.*
    -   `int? TopLogprobs`\
        *Specifies the number of highest log probabilities to be returned.*
    -   `bool? Logprobs`\
        *Flag to include log probabilities in the response.*
    -   `string? User`\
        *Optional user identifier.*
    -   `string? ToolChoice`\
        *Specifies an optional tool for processing.*
    -   `string? ResponseFormat`\
        *Defines the desired response format.*
    -   `string? SystemMessage`\
        *Optional system message to provide context.*
    -   `string[]? Stop`\
        *Tokens to indicate when to stop processing.*
    -   `List<Tool>? Tools`\
        *List of tools that may assist in processing the request.*
    -   `int? MaxMessageHistory`\
        *Maximum number of messages to keep in conversation history.*
    -   `string[]? ReferenceProfiles`\
        *Optional reference profile names for additional context.*

* * * * *

### Tool

-   **Namespace:** `IntelligenceHub.API.DTOs.Tools`

-   **Description:**\
    Represents a tool used to assist with constructing system messages and executing additional logic.

-   **Properties:**

    -   `int Id` *(Ignored during JSON serialization)*\
        *Unique identifier for the tool.*
    -   `string Type`\
        *Type of the tool; default is `"function"`.*
    -   `Function Function`\
        *Details about the function the tool represents.*
    -   `string? ExecutionUrl`\
        *URL for executing the tool's function.*
    -   `string? ExecutionMethod`\
        *HTTP method used for execution (e.g., GET, POST).*
    -   `string? ExecutionBase64Key`\
        *Optional Base64 encoded key for secure execution.*

* * * * *

### CompletionStreamChunk

-   **Namespace:** `IntelligenceHub.API.DTOs`

-   **Description:**\
    Represents a single chunk of streamed completion data sent from the server.

-   **Properties:**

    -   `int Id` *(Ignored during JSON serialization)*\
        *Unique identifier for the chunk.*
    -   `string CompletionUpdate`\
        *Contains the text update of the completion.*
    -   `string? Base64Image`\
        *Optional Base64 encoded image included in the update.*
    -   `Role? Role`\
        *Role associated with this chunk (e.g., user, assistant).*
    -   `string? User` *(Ignored during JSON serialization)*\
        *User identifier related to the chunk.*
    -   `FinishReasons? FinishReason`\
        *Indicates the reason why the completion process finished.*
    -   `Dictionary<string, string> ToolCalls`\
        *Collection of results from tool calls made during processing.*
    -   `List<HttpResponseMessage> ToolExecutionResponses`\
        *List of HTTP responses resulting from tool execution.*

* * * * *

Global Enums & Variables
------------------------

-   **APIResponseStatusCodes:**\
    Used to represent various HTTP response statuses, such as:

    -   `NotFound`
    -   `TooManyRequests`
    -   `InternalError`
-   **Role:**\
    Enumerates the role types for messages (e.g., `User`, `Assistant`).

-   **FinishReasons:**\
    Enumerates reasons for terminating the chat completion stream.

* * * * *

Usage Example
-------------

Below is an example of how a client might construct a `CompletionRequest` and invoke the `Send` method on the `ChatHub` via a SignalR connection.

csharp

CopyEdit

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
await hubConnection.InvokeAsync("Send", request);`

* * * * *

Notes
-----

-   **Validation:**\
    The `ChatHub` uses a validation handler to ensure incoming `CompletionRequest` objects are valid. If validation fails, an error is immediately returned to the client.

-   **Streaming:**\
    Completion responses are streamed back to the client in chunks. Each chunk contains a status code and may include error messages if the processing encounters issues.

-   **Security:**\
    The hub is decorated with `[Authorize]`, ensuring that only authenticated users can invoke its methods.

# Message History API Reference

This document details the endpoints provided by the Message History API. The API handles retrieving, updating, creating, and deleting conversations and messages. It validates inputs using a built-in validation mechanism and maps between DTOs and database models.

---

## Endpoints

### 1. Get Conversation History

**Method:** `GET`  
**Endpoint:** `/api/conversations/{id}`  
**Query Parameters:**
- `count` (int, required): The number of messages to retrieve.
- `page` (int, required): The page offset (pagination).

**Description:**  
Retrieves the conversation history for a given conversation ID. The response wraps a list of messages that have been mapped from the database representation to the API model.

**Response:**
- **Success:**  
  - **HTTP Status:** 200 (OK)  
  - **Body:** An `APIResponseWrapper<List<Message>>` containing the list of messages.
- **Error:**  
  - **HTTP Status:** As defined by `APIResponseStatusCodes` (e.g., 404 if not found).

**Example Response:**
```json
{
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


### 2\. Update or Create Conversation

**Method:** `POST`\
**Endpoint:** `/api/conversations/{conversationId}`\
**Request Body:** A JSON array of `Message` objects.

**Description:**\
Validates and updates (or creates) a conversation by adding the provided list of messages.

-   If validation fails (e.g., empty message list, missing user message, or any message-level validation error), the API returns a failure response with a `BadRequest` status.
-   For each valid message, the message is mapped to a database model and stored. Only successfully added messages are returned in the response.

**Response:**

-   **Success:**
    -   **HTTP Status:** 200 (OK)
    -   **Body:** An `APIResponseWrapper<List<Message>>` containing the messages that were successfully added.
-   **Failure:**
    -   **HTTP Status:** 400 (BadRequest)
    -   **Body:** An error message describing the validation failure.

**Example Request:**

json

CopyEdit

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
]`

* * * * *

### 3\. Delete Conversation

**Method:** `DELETE`\
**Endpoint:** `/api/conversations/{id}`

**Description:**\
Deletes the entire conversation identified by the provided ID.

-   If the conversation does not exist, the API returns a failure response with a `NotFound` status.

**Response:**

-   **Success:**
    -   **HTTP Status:** 200 (OK)
    -   **Body:** An `APIResponseWrapper<bool>` with `true` indicating successful deletion.
-   **Failure:**
    -   **HTTP Status:** 404 (NotFound)
    -   **Body:** An error message indicating that no conversation with the provided ID exists.

**Example Response:**

json

CopyEdit

`{
  "status": "Ok",
  "data": true
}`

* * * * *

### 4\. Add Message to Conversation

**Method:** `POST`\
**Endpoint:** `/api/conversations/{conversationId}/message`\
**Request Body:** A JSON representation of a `Message` object.

**Description:**\
Adds a single message to an existing conversation.

-   The provided message is mapped into a database message.
-   The conversation is first validated to exist.
-   If the conversation is not found, a `NotFound` response is returned.

**Response:**

-   **Success:**
    -   **HTTP Status:** 200 (OK)
    -   **Body:** An `APIResponseWrapper<DbMessage>` containing the newly added database message.
-   **Failure:**
    -   **HTTP Status:** 404 (NotFound)
    -   **Body:** An error message indicating that the conversation was not found.

**Example Request:**

json

CopyEdit

`{
  "Role": "User",
  "Content": "Can I update my shipping address?",
  "Base64Image": null,
  "TimeStamp": "2025-03-05T12:45:00Z"
}`

* * * * *

### 5\. Delete Message from Conversation

**Method:** `DELETE`\
**Endpoint:** `/api/conversations/{conversationId}/message/{messageId}`

**Description:**\
Deletes a specific message from the conversation history.

-   The `conversationId` identifies the conversation.
-   The `messageId` identifies the specific message within the conversation.
-   If the conversation or message is not found, a failure response with `NotFound` is returned.

**Response:**

-   **Success:**
    -   **HTTP Status:** 200 (OK)
    -   **Body:** An `APIResponseWrapper<bool>` with `true` if the deletion was successful.
-   **Failure:**
    -   **HTTP Status:** 404 (NotFound)
    -   **Body:** An error message indicating that the conversation or message was not found.

**Example Response:**

json

CopyEdit

`{
  "status": "Ok",
  "data": true
}`

* * * * *

Data Models
-----------

### Message

| Field | Type | Description |
| --- | --- | --- |
| `Role` | `Role` (enum) | The role of the message sender (e.g., User, Assistant, System). |
| `User` | `string` | The username associated with the message (ignored during JSON serialization). |
| `Content` | `string` | The textual content of the message. |
| `Base64Image` | `string` (nullable) | Base64-encoded image content (if applicable). |
| `TimeStamp` | `DateTime` | The UTC timestamp when the message was created. |

### APIResponseWrapper<T>

A generic wrapper used for all responses. Contains:

-   `status`: Indicates the status code (`Ok`, `BadRequest`, `NotFound`, etc.).
-   `data`: The payload of type `T`.
-   Additional error message fields (if any).

* * * * *

Validation
----------

The API employs validation for both single messages and lists of messages:

-   **ValidateMessageList(List<Message> messageList):**

    -   Ensures the list is not null or empty.
    -   Confirms at least one message in the list has a `Role` of `User`.
    -   Iterates over each message to run individual validations.
-   **ValidateMessage(Message message):**

    -   Checks that the message is not null.
    -   Ensures a role is provided.
    -   Requires that either `Content` or `Base64Image` is provided.
    -   Validates maximum length constraints for `User` and `Content`.
    -   Validates image size and proper Base64 encoding if an image is provided.

* * * * *

Notes
-----

-   **Mapping:**\
    The API uses a `DbMappingHandler` to convert between the database message model and the API DTO.

-   **Error Handling:**\
    All endpoints return standardized error messages and status codes as defined in the `GlobalVariables.APIResponseStatusCodes` enum.

-   **Timeouts and Limits:**\
    Image validation uses a timeout mechanism to prevent excessive processing. Maximum image sizes are enforced to ensure performance and security.

This API Reference documentation provides an overview of the functionality available through the Message History API. Adjustments and additional details may be needed based on further implementation specifics or integration with other parts of the IntelligenceHub application.

# RAG Controller API Reference

The **RAG Controller** provides endpoints to manage RAG indexes including creation, configuration, querying, document management, and deletion. All endpoints require authentication with the `ElevatedAuthPolicy`.

---

## Endpoints

### 1. Get RAG Index by Name

**HTTP Method:** `GET`  
**Route:** `/Rag/Index/{index}`  
**Operation ID:** `GetIndexAsync`

#### Description
Retrieves the metadata for a specific RAG index by its name.

#### Parameters
- **index** (path, string)  
  The name of the index.

#### Responses
- **200 OK:** Returns an `IndexMetadata` object.
- **400 Bad Request:** The index name is invalid or missing.
- **404 Not Found:** The specified index was not found.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 2. Get All RAG Indexes

**HTTP Method:** `GET`  
**Route:** `/Rag/Index/All`  
**Operation ID:** `GetAllIndexesAsync`

#### Description
Retrieves metadata for all available RAG indexes.

#### Responses
- **200 OK:** Returns a list of `IndexMetadata` objects.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 3. Create a New RAG Index

**HTTP Method:** `POST`  
**Route:** `/Rag/Index`  
**Operation ID:** `CreateIndexAsync`

#### Description
Creates a new RAG index using the provided index definition.

#### Request Body
- **indexDefinition** (`IndexMetadata`):  
  The definition of the index to create.

#### Responses
- **200 OK:** Returns the created `IndexMetadata` object.
- **400 Bad Request:** The request body is malformed or invalid.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 4. Configure an Existing RAG Index

**HTTP Method:** `POST`  
**Route:** `/Rag/Index/Configure/{index}`  
**Operation ID:** `ConfigureIndexAsync`

#### Description
Updates the configuration of an existing RAG index.

#### Parameters
- **index** (path, string)  
  The name of the index to configure.

#### Request Body
- **indexDefinition** (`IndexMetadata`):  
  The new definition for the index.

#### Responses
- **200 OK:** Returns the updated `IndexMetadata` object.
- **400 Bad Request:** The request body is malformed or invalid.
- **404 Not Found:** The specified index was not found.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 5. Query a RAG Index

**HTTP Method:** `GET`  
**Route:** `/Rag/Index/{index}/Query/{query}`  
**Operation ID:** `QueryIndexAsync`

#### Description
Executes a query against the specified RAG index and returns matching documents.

#### Parameters
- **index** (path, string)  
  The name of the index.
- **query** (path, string)  
  The query string to search the index.

#### Responses
- **200 OK:** Returns a collection of documents (implementation-specific structure).
- **400 Bad Request:** The query parameter is null or empty.
- **404 Not Found:** The specified index was not found.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 6. Run Index Update

**HTTP Method:** `POST`  
**Route:** `/Rag/Index/{index}/Run`  
**Operation ID:** `RunIndexUpdateAsync`

#### Description
Triggers an update for the specified RAG index.

#### Parameters
- **index** (path, string)  
  The name of the index.

#### Responses
- **204 No Content:** The index update was successfully initiated.
- **400 Bad Request:** The index name is invalid.
- **404 Not Found:** The specified index was not found.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 7. Delete a RAG Index

**HTTP Method:** `DELETE`  
**Route:** `/Rag/Index/Delete/{index}`  
**Operation ID:** `DeleteIndexAsync`

#### Description
Deletes the specified RAG index.

#### Parameters
- **index** (path, string)  
  The name of the index to delete.

#### Responses
- **204 No Content:** The index was successfully deleted.
- **400 Bad Request:** The index name is invalid.
- **404 Not Found:** The specified index was not found.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 8. Get All Documents in a RAG Index

**HTTP Method:** `GET`  
**Route:** `/Rag/Index/{index}/Document/{count}/Page/{page}`  
**Operation ID:** `GetIndexDocumentsAsync`

#### Description
Retrieves a paginated list of documents from a specific RAG index.

#### Parameters
- **index** (path, string)  
  The name of the index.
- **count** (path, int)  
  The number of documents to retrieve in the current batch (must be 1 or greater).
- **page** (path, int)  
  The page number to retrieve (must be 1 or greater).

#### Responses
- **200 OK:** Returns a list of `IndexDocument` objects.
- **400 Bad Request:** One or more parameters are invalid.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 9. Get a Specific Document from a RAG Index

**HTTP Method:** `GET`  
**Route:** `/Rag/index/{index}/document/{document}`  
**Operation ID:** `GetDocumentAsync`

#### Description
Retrieves a specific document from the specified RAG index.

#### Parameters
- **index** (path, string)  
  The name of the index.
- **document** (path, string)  
  The title of the document.

#### Responses
- **200 OK:** Returns the requested `IndexDocument` object.
- **400 Bad Request:** The request parameters are invalid.
- **404 Not Found:** The document or index was not found.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 10. Upsert Documents in a RAG Index

**HTTP Method:** `POST`  
**Route:** `/Rag/index/{index}/Document`  
**Operation ID:** `UpsertDocumentAsync`

#### Description
Inserts or updates one or more documents in the specified RAG index.

#### Parameters
- **index** (path, string)  
  The name of the index.

#### Request Body
- **documentUpsertRequest** (`RagUpsertRequest`):  
  Contains an array of `IndexDocument` objects to upsert.  
  **Note:** At least one document must be provided.

#### Responses
- **200 OK:** Returns the upserted documents.
- **400 Bad Request:** The request body is malformed or invalid.
- **404 Not Found:** The specified index was not found.
- **500 Internal Server Error:** An unexpected error occurred.

---

### 11. Delete Documents from a RAG Index

**HTTP Method:** `DELETE`  
**Route:** `/Rag/index/{index}/Document/{commaDelimitedDocNames}`  
**Operation ID:** `DeleteDocumentsAsync`

#### Description
Deletes one or more documents from the specified RAG index.

#### Parameters
- **index** (path, string)  
  The name of the index.
- **commaDelimitedDocNames** (path, string)  
  A comma-separated list of document titles to delete.

#### Responses
- **200 OK:** Returns the number of documents that were deleted.
- **400 Bad Request:** The index name is invalid or no document names were provided.
- **404 Not Found:** One or more of the specified documents were not found.
- **500 Internal Server Error:** An unexpected error occurred.

---

## Data Models

### IndexMetadata

| Property                | Type                      | Description                                                                                       |
|-------------------------|---------------------------|---------------------------------------------------------------------------------------------------|
| `Name`                  | string                    | The name of the index.                                                                            |
| `QueryType`             | QueryType (enum)          | The type of query operation.                                                                      |
| `GenerationHost`        | AGIServiceHosts?          | Host for content generation. Required if topic or keyword generation is enabled.                |
| `IndexingInterval`      | TimeSpan?                 | The interval between index updates; must be positive and less than one day.                      |
| `EmbeddingModel`        | string?                   | Optional embedding model (max length 255 characters).                                             |
| `MaxRagAttachments`     | int?                      | Maximum number of attachments; must be non-negative and cannot exceed 20.                         |
| `ChunkOverlap`          | double?                   | Overlap ratio between document chunks (between 0 and 1 inclusive).                                |
| `GenerateTopic`         | bool?                     | Flag to enable topic generation.                                                                  |
| `GenerateKeywords`      | bool?                     | Flag to enable keywords generation.                                                             |
| `GenerateTitleVector`   | bool?                     | Flag to enable title vector generation.                                                         |
| `GenerateContentVector` | bool?                     | Flag to enable content vector generation.                                                       |
| `GenerateTopicVector`   | bool?                     | Flag to enable topic vector generation.                                                         |
| `GenerateKeywordVector` | bool?                     | Flag to enable keyword vector generation.                                                       |
| `ScoringProfile`        | IndexScoringProfile?      | Scoring profile details.                                                                          |

### IndexScoringProfile

| Property              | Type                         | Description                                                       |
|-----------------------|------------------------------|-------------------------------------------------------------------|
| `Name`                | string                       | Name of the scoring profile.                                      |
| `SearchAggregation`   | SearchAggregation?           | Method used for search aggregation.                               |
| `SearchInterpolation` | SearchInterpolation?         | Method used for search interpolation.                             |
| `FreshnessBoost`      | double                       | Boost value for freshness (non-negative).                         |
| `BoostDurationDays`   | int                          | Duration in days for the boost (non-negative).                    |
| `TagBoost`            | double                       | Boost value for tags (non-negative).                              |
| `Weights`             | Dictionary<string, double>?  | Custom scoring weights. Keys must be non-empty and values non-negative. |

### IndexDocument

| Property   | Type            | Description                                                                    |
|------------|-----------------|--------------------------------------------------------------------------------|
| `Id`       | int             | Unique identifier for the document.                                            |
| `Title`    | string          | Document title (max 255 characters).                                           |
| `Content`  | string          | Document content (max 1,000,000 characters).                                   |
| `Topic`    | string?         | Optional topic (max 255 characters).                                           |
| `Keywords` | string?         | Optional keywords (max 255 characters).                                        |
| `Source`   | string          | Source of the document (max 4000 characters).                                  |
| `Created`  | DateTimeOffset  | Timestamp when the document was created.                                       |
| `Modified` | DateTimeOffset  | Timestamp when the document was last modified.                                 |

### RagUpsertRequest

| Property    | Type                         | Description                                                                   |
|-------------|------------------------------|-------------------------------------------------------------------------------|
| `Documents` | List&lt;IndexDocument&gt;   | Collection of documents to be upserted; must contain at least one document.    |

---

## Validation Rules

### Index Metadata Validation

- **Name:**  
  - Must not be empty or whitespace.  
  - Maximum length is 128 characters.

- **IndexingInterval:**  
  - Must be greater than zero and less than one day.

- **EmbeddingModel:**  
  - If provided, must not exceed 255 characters.

- **MaxRagAttachments:**  
  - Must be a non-negative integer and cannot exceed 20.

- **ChunkOverlap:**  
  - Must be between 0 and 1 (inclusive).

For the **ScoringProfile**:
- **Name:** Must be provided and not exceed 128 characters.
- Other string properties (e.g., `QueryType`, `GenerationHost`): Maximum length is 255 characters.
- **Weights:**  
  - All keys must be non-empty strings and corresponding values must be non-negative.

### Document Validation

- **Title:**  
  - Cannot be empty.  
  - Maximum length is 255 characters.

- **Content:**  
  - Cannot be empty.  
  - Maximum length is 1,000,000 characters.

- **Topic/Keywords:**  
  - If provided, must not exceed 255 characters.

- **Source:**  
  - Maximum length is 4000 characters.

---

## Notes

- **Authentication:** All endpoints require the caller to be authenticated under the `ElevatedAuthPolicy`.
- **Error Handling:** In case of an error, endpoints return an appropriate HTTP status code along with a descriptive error message.
- **Validation:** Input data is validated before any business logic is executed. Invalid requests will result in a `400 Bad Request` with details of the validation failure.

# Profile Controller API Reference

This document details the endpoints available in the **ProfileController**. All endpoints require authorization under the `ElevatedAuthPolicy`.

---

## GET /Profile/get/{name}

**Description:**  
Retrieves a profile by its name.

**Route Parameter:**  
- `name` (string, required): The name of the agent profile.

**Responses:**
- **200 OK**  
  Returns the profile object.
  - **Content-Type:** `application/json`
  - **Response Body:** A profile object.
- **400 Bad Request**  
  Returned if the `name` parameter is null, empty, or whitespace.
- **404 Not Found**  
  Returned if the profile is not found.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `GetProfileAsync`

---

## GET /Profile/get/all/page/{page}/count/{count}

**Description:**  
Retrieves a paginated list of all profiles.

**Route Parameters:**  
- `page` (integer, required): The page number to retrieve. Must be greater than 0.
- `count` (integer, required): The number of profiles to retrieve. Must be greater than 0.

**Responses:**
- **200 OK**  
  Returns a list of profile objects.
  - **Content-Type:** `application/json`
  - **Response Body:** An array of profiles.
- **400 Bad Request**  
  Returned if `page` or `count` are less than 1.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `GetAllProfilesAsync`

---

## POST /Profile/upsert

**Description:**  
Creates a new profile or updates an existing one.

**Request Body:**  
- A JSON object representing the profile definition.
  - **Type:** `Profile`
  - **Example:**
    ```json
    {
      "Name": "exampleProfile",
      "Model": "gpt-3",
      "Host": "OpenAI",
      "ImageHost": "None",
      "RagDatabase": "SampleDB",
      "FrequencyPenalty": 0.5,
      "PresencePenalty": 0.5,
      "Temperature": 1.0,
      "TopP": 0.9,
      "MaxTokens": 100,
      "TopLogprobs": 0,
      "Logprobs": false,
      "User": "user@example.com",
      "ToolChoice": "default",
      "ResponseFormat": "json",
      "SystemMessage": "Welcome to the API",
      "Stop": ["\n"],
      "Tools": [],
      "MaxMessageHistory": 5,
      "ReferenceProfiles": []
    }
    ```

**Responses:**
- **200 OK**  
  Returns the newly created or updated profile.
  - **Content-Type:** `application/json`
  - **Response Body:** The profile object.
- **400 Bad Request**  
  Returned if the provided profile data is invalid.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `UpsertProfileAsync`

---

## POST /Profile/associate/{name}

**Description:**  
Associates an existing profile with one or more tools.

**Route Parameter:**  
- `name` (string, required): The name of the profile to associate.

**Request Body:**  
- A JSON array of tool names to associate with the profile.
  - **Type:** `List<string>`
  - **Example:**
    ```json
    ["tool1", "tool2"]
    ```

**Responses:**
- **200 OK**  
  Returns a list of tool names that are now associated with the profile.
  - **Content-Type:** `application/json`
  - **Response Body:** An array of strings.
- **400 Bad Request**  
  Returned if the `name` is null/empty or if the tools list is null or empty.
- **404 Not Found**  
  Returned if the profile or specified tools are not found.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `AssociateProfileWithToolsAsync`

---

## POST /Profile/dissociate/{name}

**Description:**  
Dissociates a profile from one or more tools.

**Route Parameter:**  
- `name` (string, required): The name of the profile to dissociate.

**Request Body:**  
- A JSON array of tool names to dissociate from the profile.
  - **Type:** `List<string>`
  - **Example:**
    ```json
    ["tool1", "tool2"]
    ```

**Responses:**
- **200 OK**  
  Returns a list of tool names that were successfully dissociated from the profile.
  - **Content-Type:** `application/json`
  - **Response Body:** An array of strings.
- **400 Bad Request**  
  Returned if the `name` is null/empty or if the tools list is null or empty.
- **404 Not Found**  
  Returned if the profile or associations are not found.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `DissociateProfileFromToolsAsync`

---

## DELETE /Profile/delete/{name}

**Description:**  
Deletes a profile by its name.

**Route Parameter:**  
- `name` (string, required): The name of the profile to delete.

**Responses:**
- **204 No Content**  
  Indicates that the profile was successfully deleted.
- **400 Bad Request**  
  Returned if the `name` is null, empty, or whitespace.
- **404 Not Found**  
  Returned if the profile is not found.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `DeleteProfileAsync`

---

# Additional Information

### Profile Object Structure

The `Profile` object contains the following properties:

- **Id** (integer): Internal identifier (ignored in JSON responses).
- **Name** (string): Name of the profile.
- **Model** (string): Model name (e.g., `"gpt-3"`).
- **Host** (enum): The primary host for the AGI service (e.g., `OpenAI`, `Azure`, `Anthropic`).
- **ImageHost** (enum, optional): The host for image generation, if applicable.
- **RagDatabase** (string, optional): Name of the RAG (Retrieval Augmented Generation) database.
- **FrequencyPenalty** (float, optional): Value between -2 and 2.
- **PresencePenalty** (float, optional): Value between -2 and 2.
- **Temperature** (float, optional): Value between 0 and 2.
- **TopP** (float, optional): Value between 0 and 1.
- **MaxTokens** (integer, optional): Minimum value is 1.
- **TopLogprobs** (integer, optional): Value between 0 and 5.
- **Logprobs** (bool, optional): Indicates whether log probabilities are enabled.
- **User** (string, optional): User identifier.
- **ToolChoice** (string, optional): Choice of tool.
- **ResponseFormat** (string, optional): Either `"text"` or `"json"`.
- **SystemMessage** (string, optional): System message for context.
- **Stop** (string array, optional): List of stop sequences.
- **Tools** (list, optional): List of associated tools.
- **MaxMessageHistory** (integer, optional): Maximum number of message history items.
- **ReferenceProfiles** (string array, optional): List of reference profiles (maximum of 3, with each not exceeding 40 characters).

### Validation Rules

When creating or updating a profile, the following validations apply:
- **Name**: Required, must not be `"all"`, and must not exceed 40 characters.
- **Model**: Required and must be supported by the specified `Host`.
- **Host**: Required and cannot be `None`.
- **FrequencyPenalty** and **PresencePenalty**: Must be between -2 and 2.
- **Temperature**: Must be between 0 and 2.
- **TopP**: Must be between 0 and 1.
- **MaxTokens**: Must be at least 1 and, for certain hosts, cannot exceed model-specific context limits.
- **TopLogprobs**: Must be between 0 and 5.
- **ResponseFormat**: If provided, must be either `"text"` or `"json"`.
- **SQL Constraints**: Several string fields (e.g., `Model`, `ResponseFormat`, `User`, `SystemMessage`) have maximum length requirements.
- **Tools**: Each tool in the `Tools` list is validated via its own rules.

Validation may also consider additional parameters such as prompt token counts when applicable.

# Tool Controller API Reference

This document provides an overview of the API endpoints available in the **ToolController**. Each endpoint is described with its HTTP method, URL pattern, parameters, request body (if applicable), and possible responses.

> **Note:** All endpoints require authorization under the `ElevatedAuthPolicy`.

---

## GET /Tool/get/{name}

**Description:**  
Retrieves a tool by its name.

**Route Parameter:**  
- `name` (string, required): The name of the tool.

**Responses:**
- **200 OK**  
  Returns the tool object.
  - **Content-Type:** `application/json`
  - **Response Body:** A single tool object.
- **400 Bad Request**  
  Returned if the `name` parameter is null or empty.
- **404 Not Found**  
  Returned if no tool matching the provided name is found.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `GetToolAsync`

---

## GET /Tool/get/all/page/{page}/count/{count}

**Description:**  
Retrieves a paginated list of all tools.

**Route Parameters:**  
- `page` (integer, required): The page number to retrieve. Must be greater than 0.
- `count` (integer, required): The number of tools per page. Must be greater than 0.

**Responses:**
- **200 OK**  
  Returns a list of tool objects.
  - **Content-Type:** `application/json`
  - **Response Body:** An array of tool objects (can be empty if no tools are found).
- **400 Bad Request**  
  Returned if the `page` or `count` values are less than 1.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `GetAllToolsAsync`

---

## GET /Tool/get/{name}/profiles

**Description:**  
Retrieves the list of profile names that are associated with the specified tool.

**Route Parameter:**  
- `name` (string, required): The name of the tool.

**Responses:**
- **200 OK**  
  Returns a list of profile names.
  - **Content-Type:** `application/json`
  - **Response Body:** An array of strings representing profile names (can be empty).
- **400 Bad Request**  
  Returned if the `name` parameter is null or empty.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `GetToolProfilesAsync`

---

## POST /Tool/upsert

**Description:**  
Creates new tools or updates existing ones.

**Request Body:**  
- A JSON array of tool objects to add or update.
  - **Type:** `List<Tool>`
  - **Example:**
    ```json
    [
      {
        "Type": "function",
        "Function": {
          "Name": "exampleTool",
          "Description": "A sample tool",
          "Parameters": {
            "type": "object",
            "properties": {
              "param1": { "type": "string", "description": "Sample parameter" }
            },
            "required": [ "param1" ]
          }
        },
        "ExecutionUrl": "https://example.com/execute",
        "ExecutionMethod": "POST",
        "ExecutionBase64Key": "base64encodedkey"
      }
    ]
    ```

**Responses:**
- **200 OK**  
  Returns the updated list of tools.
  - **Content-Type:** `application/json`
  - **Response Body:** The array of tool objects that was submitted.
- **400 Bad Request**  
  Returned if the tool list is invalid or if business logic validation fails.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `UpsertToolAsync`

---

## POST /Tool/associate/{name}

**Description:**  
Associates an existing tool with one or more profiles.

**Route Parameter:**  
- `name` (string, required): The name of the tool.

**Request Body:**  
- A JSON array of profile names to be associated with the tool.
  - **Type:** `List<string>`
  - **Example:**
    ```json
    ["profile1", "profile2"]
    ```

**Responses:**
- **200 OK**  
  Returns a list of profile names that are now associated with the tool.
  - **Content-Type:** `application/json`
  - **Response Body:** An array of strings.
- **400 Bad Request**  
  Returned if the `name` parameter is null/empty or if the profiles list is null or empty.
- **404 Not Found**  
  Returned if the tool is not found.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `AssociateToolWithProfilesAsync`

---

## POST /Tool/dissociate/{name}

**Description:**  
Dissociates a tool from one or more profiles.

**Route Parameter:**  
- `name` (string, required): The name of the tool.

**Request Body:**  
- A JSON array of profile names to be dissociated from the tool.
  - **Type:** `List<string>`
  - **Example:**
    ```json
    ["profile1", "profile2"]
    ```

**Responses:**
- **200 OK**  
  Returns a list of profile names that were successfully dissociated.
  - **Content-Type:** `application/json`
  - **Response Body:** An array of strings.
- **400 Bad Request**  
  Returned if the `name` parameter is null/empty or if the profiles list is null or empty.
- **404 Not Found**  
  Returned if the tool or its associations are not found.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `DissociateToolFromProfilesAsync`

---

## DELETE /Tool/delete/{name}

**Description:**  
Deletes a tool by its name.

**Route Parameter:**  
- `name` (string, required): The name of the tool to delete.

**Responses:**
- **204 No Content**  
  Indicates the tool was successfully deleted.
- **400 Bad Request**  
  Returned if the `name` parameter is null or empty.
- **404 Not Found**  
  Returned if the tool to delete is not found.
- **500 Internal Server Error**  
  Returned when an unexpected error occurs.

**Swagger Operation ID:** `DeleteToolAsync`

---

# Additional Information

### Tool Object Structure

The `Tool` object typically contains the following properties:

- **Id** (integer): Internal identifier (ignored in JSON responses).
- **Type** (string): Constant value `"function"`.
- **Function** (object): Contains details about the tool's function.
  - **Name** (string): The function name.
  - **Description** (string, optional): A brief description of the function.
  - **Parameters** (object): Contains:
    - `type` (string): Always `"object"`.
    - `properties` (Dictionary<string, Property>): Dictionary of parameter definitions.
    - `required` (string array): List of required property names.
- **ExecutionUrl** (string, optional): URL used for tool execution.
- **ExecutionMethod** (string, optional): HTTP method used to execute the tool.
- **ExecutionBase64Key** (string, optional): Base64 key for execution purposes.

### Validation Rules

When upserting tools, the following validations apply:
- The tool's function **Name** is required and must not be empty.
- The function **Name** must not exceed 64 characters.
- The function **Description** must not exceed 512 characters.
- The required parameters list must not exceed 255 characters and each required property must exist in the properties dictionary.
- **ExecutionUrl** must not exceed 4000 characters.
- **ExecutionBase64Key** and **ExecutionMethod** must not exceed 255 characters each.
- Certain function names (e.g., `"all"`, reserved names like `"recurse_ai_dialogue"` and `"image_gen"`) are disallowed.

Validation for each property in the tool's parameters includes:
- The property `type` is required and must be one of the valid tool argument types.
- The property `description` must not exceed 200 characters.
- The property name must not exceed 64 characters.

---

This documentation outlines the primary endpoints for managing agent tools within the IntelligenceHub system. For any further details or updates, please refer to the source code or contact the API maintainer.

### API Reference - TO DO
List and describe the available API endpoints, including request and response formats.

### Contributing
Contributions are welcome! Please follow the steps below to contribute to the project:
1. (Optional) If you would like to gaurentee your changes will be merged, please open an issue to determine if your desired changes align with the project's goals, or choose from an existing issue. Otherwise, feel free to skip this step.
2. Fork the project, create a new branch, and make your changes. 
3. Ensure that any changes include relevant unit tests and IntelliSense documentation, or update existing tests/documentation as needed.
4. Push your changes to your fork, and raise a pull request.

If for whatever reason we miss your request, please feel free to notify us on the pull request itself, at the email provided in the [Contact](#contact) section, or any other method you have at your disposal.

### Contact
For any questions comments or concerns, please reach out to Applied.AI.Help@gmail.com, or open an issue in the repository.

### License
This project is licensed under the Elastic 2.0 license - see the `LICENSE` file for more details.

### Acknowledgements
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