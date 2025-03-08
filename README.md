# The Intelligence Hub
### A robust wrapper for popular AI services, designed to streamline AI-powered app development while ensuring reliability and effortless cross-service configuration.
---
## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Setup](#setup)
- [Usage](#usage)
- [API Reference](#api-reference)
- [Contributing](#contributing)
- [Contact](#contact)
- [License](#license)
- [Acknowledgements](#acknowledgements)
---
## Overview
The main goal of this project is to enable rapid setup and development for AI-powered applications. With minor configurations, a payload as slim as the below can be used for powerful AGI operations ranging from standard chat completions, to image generation, tool execution, and RAG powered searches. 

```json
{
    "Messages": [  
        {  
            "Role": "User",   
            "Content": "Generate an image of a dog! | Set an alarm for 7 a.m. please. | What books does John Bookwerm currently have checked out?"
        } 
    ] 
}
```

Key capabilities include:
- Simplified request payloads to AGI clients using pre-configured agent/completion 'profiles'
- Saving and loading conversation history
- Tool execution at external APIs
- RAG database creation and consumption
- Conversation persistence

AI clients are resolved using a custom client factory, allowing new clients to be added easily by implementing the IAGIClient interface. This design also supports image generation from various clients, decoupling the image generation providers from LLM providers within the same agent profile. A similar design could also be used to provide support for multiple RAG services with relative ease.

For more information, please refer to the [Features](#features) section below.

---

## Features
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

---
## Setup

### Setup Instructions

This section outlines the steps required to set up the IntelligenceHub repository for local development. IntelligenceHub is a .NET API wrapper for AI services that relies on several external resources including AI service providers, search functionalities, and additional infrastructure.

Optional infrastructure can be treated modularly unless otherwise noted, but you should be able to find a free tier available for every piece of infrastructure as well, excluding the LLM hosts, although the Azure OpenAI option is at least a 'pay as you go' model, keeping things exceptionally cheap. An Azure subscription is only required for RAG operations, or if you wish to use Azure OpenAI as your LLM host. 

**NOTE:** The application requires several sensative keys and values to populate the configuration. If you intend to use an associated feature, please be sure to have the relevant keys and endpoint data ready when running the `env_setup.py` script at IntelligenceHub\IntelligenceHub\infrastructure\ `env_setup.py`. The only required features are an SQL Server connection string, and the API key for at least one LLM host.

---

#### 1. Environment Setup

- **Ensure the following are installed**:
  - [.NET SDK (version 8.0 or later)](https://dotnet.microsoft.com/download)
  - [Python 3.1 or later](https://www.python.org/downloads/) – for running the appsettings generation script.
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) is recommended for easiest set up, but technically the Git client should suffice.

- **Set Up the Repo**:
    - Open Visual Studio, and select 'clone a repository.'
    - Get the Git repository URL at https://github.com/Jacob-J-Thomas/IntelligenceHub, and use it to clone the repo.

---

#### 2. Create Resource Dependencies

**Note:** The repository includes a Python script, `IntelligenceHub\infrastructure\env_setup.py`, that generates a development configuration file by populating `appsettings.Development.json` from a template. You may find it worthwhile to save the API keys and URLs associated with these resources so that you can easily enter them into the Python script later. 

- **Essential Resources for Basic Completions**:
The below resources are the bare minimum requirements to run the API and get a `200` response from the `\Completion\Chat\{profileName}` endpoint. If your application requires RAG database operations, app insight telemetry collection, or 
  - An SQL Server instance running locally. For setup instructions, follow the [SQL Server Installation Guide](https://docs.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server?view=sql-server-ver15). 
    - **NOTE:** If you intend to perform RAG operations, **consider following the documentation for Azure SQL databases** provided below instead. Otherwise, you should be comfortable configuring your development server to be accessible to Azure AI Search services.
    - Free versions and more can be [found here](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).
    - Please be sure to run the SQL scripts found in the repo at `\IntelligenceHub\IntelligenceHub.DAL\Scripts` against the database after it is created.
  - An API key from one of the supported LLM providers (e.g., OpenAI, Azure OpenAI, or Anthropic). NOTE: Technically only one of the below is required, provided you only use that host.  
    - **OpenAI**: Retrieve your API key from [OpenAI's platform](https://help.openai.com/en/articles/4936850-where-do-i-find-my-openai-api-key).  
    - **Anthropic**: Sign in and create a [new API key here](https://console.anthropic.com/settings/keys). Make sure to save its details.
    - **Azure OpenAI**: Create an Azure OpenAI resource, and be sure to deploy at least one LLM in your Azure OpenAI deployment. You can name this model deployment whatever you want, but be sure to add this name to the ValidAGIModels array when running the `env_setup.py` script. Instructions on deploying this resource can be [found here](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).

- **Azure Resources (Optional)**:  
  You only need to set up these additional resources if you plan to use advanced features such as RAG operations. For basic completions, local configuration with a single LLM provider's API key is sufficient.
  - **Azure SQL Database** – In order to support RAG requests powered by Azure AI Search Services, the database must either be created in Azure, or otherwise be accessible to the Azure AI Search instance. This is required to create the datasource connection used to index the database via automatic change tracking. For set up instructions, follow the [Azure SQL database quickstart guide](https://learn.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart?view=azuresql&tabs=azure-portal#prerequisites).
  - **Azure AI Search Services** – Required only if you intend to use RAG operations. Follow [these instructions](https://learn.microsoft.com/en-us/azure/search/search-create-service-portal) until an AI Search Service resource is created, but feel free to skip any sections after you get the API key. The API is fully capable of setting up and managing AI Search Service indexes with just a couple of API requests.
  - **Application Insights** – Only required for telemetry and performance monitoring. For setup, follow [these instructions for setting up Application Insights for .NET Core](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core). The application is already set up to use the service, so skip any sections that aren't related to getting a connection string.

---

#### 3. Configure Application Settings

1. **Locate the Template and Output Paths**:  
   - **Template**: `IntelligenceHub.Host/appsettings.Template.json`  
   - **Output**: `IntelligenceHub.Host/appsettings.Development.json`

2. **Collect Any Required Secrets and Keys**
    - Only the SQL database string, and one LLM host credential/url combination is required.
    - Refer to the appsettings.Template.json file for a list of all values possible values.

2. **Run the Script**:  
   - Open a terminal in the repository’s base directory.
   - Execute the script (e.g., open the command line, navigate to the file, and run: `python env_setup.py`).
   - **Script Details**:
     - The script is configured to work in a CI/CD pipeline, but this setup is outside of the scope of this guide.
     - The script searches for environment variables before requesting they be populated by the user.
     - Supports multiple entries for service endpoints (e.g., Azure OpenAI Services, OpenAI Services, Anthropic Services).
     - If using Azure OpenAI, be sure to populate `ValidAGIModels` with the names of your deployed models, otherwise leave empty.
     - Be sure to list any intended clients under `ValidOrigins`, which will be used to populate the cors policy's allowed origins. Can be set to `*` for a permissive policy to get a dev environment up and running quickly. 
     - For the `SearchServiceCompletionService` values, provide any of set of credentials you used for one of the completion services, or if you don't intend to use RAG operations, feel free to leave it null. These values are used to generate the Keywords and Topic strings for RAG documents if `GenerateKeywords` or `GenerateTopic` is set to true on the targeted index.
   - Upon completion, the script writes the updated configuration to `appsettings.Development.json`.

---

#### 4. Run the application

1. **Open the solution folder if the repo did not already**
2. **Run the application**
    - If using Visual Studio, click the drop down arrow next to 'run' button, and select 'Configure Startup Projects' from the list. Ensure that IntelligenceHub.Host.csproj is selected as the startup application, and then run the application.
    - Otherwise, start the application using 'Dotnet Run' or your preferred IDE.
3. **Begin Making Requests Using the Swagger Page**
    - Refer to the [next section on usage](#usage) for help making your first request.

---

## Usage

The heart of this API is the concept of an agentic `profile`, which can be thought of as a preset of request parameters and prompting data that can be used to return LLM chat completions, and other responses.  Using profiles, you can easily handle saving system prompts, tool definitions, and model settings, allowing clients to recieve AI content with nothing more than the name of the profile, and a single user message. 

Follow these steps to create a profile and then make a chat request using that profile.

#### 1. Create or Update a Profile

Use the upsert endpoint to create (or update) an agent profile. In this example, we create a profile named **ChatProfile**. Only a Name, Host, and Model are required to create a profile. The rest of the details can be defaulted.

**HTTP Request:**
`POST https://yourapi.com/Profile/upsert`

 **Request Payload:**
```json
{
    "Name": "ChatProfile",
    "Model": "gpt-4o",
    "Host": "OpenAI",
    "Temperature": 0.7,
    "MaxTokens": 150
}
```

**Example using curl:**
`curl -X POST "https://yourapi.com/Profile/upsert"\
     -H "Content-Type: application/json"\
     -d '{
           "Name": "ChatProfile",
           "Model": "gpt-4o",
           "Host": "OpenAI",
           "Temperature": 0.7,
           "MaxTokens": 150
         }'`

If successful, the API will create or update the **ChatProfile** with the provided configuration.

#### 2\. Make a Chat Request

With the profile created, you can now send a chat request using the Chat Completion API.

**HTTP Request:**
`POST https://yourapi.com/Completion/Chat/ChatProfile`

**Request Payload:**
```json
{
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
}
```

**Example using curl:**
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

**Expected Response:**
```json
{
    "Messages": [
        {
            "Role": "Assistant",
            "Content": "I'm good! How can I assist you today?"
        }
    ],
    "ToolCalls": {},
    "ToolExecutionResponses": [],
    "FinishReason": "Stop | Length | ToolCalls | ContentFilter | TooManyRequests | Error"
}
```

### 3\. Integrate RAG Index with Chat Requests

To enable your AI model to answer queries using information stored in a RAG index, follow these steps:

#### 3.1 Create a RAG Index

Use the **Create RAG Index** endpoint to define a new RAG index. For example, to create an index named **MyRagIndex**, send a POST request with the required index metadata.

**HTTP Request:**
`POST https://yourapi.com/Rag/Index`

**Request Payload:**
```json
{
    "Name": "MyRagIndex",
    "GenerationHost": "AzureAI",
    "IndexingInterval": "00:05:00",
    "EmbeddingModel": "ada-text-embedding-002",
    "MaxRagAttachments": 10,
    "ChunkOverlap": 0.2,
    "ScoringProfile": {
        "Name": "DefaultScoring",
        "SearchAggregation": "Sum",
        "SearchInterpolation": "Linear",
        "FreshnessBoost": 1.0,
        "BoostDurationDays": 7,
        "TagBoost": 1.5,
        "Weights": {
            "field1": 0.8,
            "field2": 1.2
        }
    },
    "GenerateKeywords": true,
    "GenerateTopic": false
}
```

**Example using curl:**
`curl -X POST "https://yourapi.com/Rag/Index"\
     -H "Content-Type: application/json"\
     -d '{
           "Name": "MyRagIndex",
           "GenerationHost": "AzureAI",
           "IndexingInterval": "00:05:00",
           "EmbeddingModel": "ada-text-embedding-002",
           "MaxRagAttachments": 10,
           "ChunkOverlap": 0.2,
           "ScoringProfile": {
               "Name": "DefaultScoring",
               "SearchAggregation": "Sum",
               "SearchInterpolation": "Linear",
               "FreshnessBoost": 1.0,
               "BoostDurationDays": 7,
               "TagBoost": 1.5,
               "Weights": {
                   "field1": 0.8,
                   "field2": 1.2
               }
           },
           "GenerateKeywords": true,
           "GenerateTopic": false
         }'`

A successful request will return the index metadata for **MyRagIndex**.

#### 3.2 Add a New Document to the RAG Index

After creating the index, add or update documents using the **Upsert Documents** endpoint.

**HTTP Request:**
`POST https://yourapi.com/Rag/index/MyRagIndex/Document`

**Request Payload:**
```json
{
    "Documents": [
        {
            "Title": "Azure AI Overview",
            "Content": "Detailed information on Azure AI Search Services and its capabilities...",
            "Topic": "Azure",
            "Keywords": "AI, Search, Azure",
            "Source": "Official Documentation"
        }
    ]
}
```

**Example using curl:**
`curl -X POST "https://yourapi.com/Rag/index/MyRagIndex/Document"\
     -H "Content-Type: application/json"\
     -d '{
           "Documents": [
               {
                   "Title": "Azure AI Overview",
                   "Content": "Detailed information on Azure AI Search Services and its capabilities...",
                   "Topic": "Azure",
                   "Keywords": "AI, Search, Azure",
                   "Source": "Official Documentation"
               }
           ]
         }'`

This request will upsert the document into **MyRagIndex**.

#### 3.3 Run an Update on the RAG Index

Once you have added new content, refresh the RAG index by initiating an update run.

**HTTP Request:**
`POST https://yourapi.com/Rag/Index/MyRagIndex/Run`

**Example using curl:**
`curl -X POST "https://yourapi.com/Rag/Index/MyRagIndex/Run"`

A successful update will return a `204 No Content` status, indicating that the index update process has started.

#### 3.4 Make a Chat Request with RAG Database Integration

Finally, to leverage the RAG index during chat interactions, include the `RagDatabase` property in the `ProfileOptions` of your chat request. This tells the AI model to retrieve and use the context from **MyRagIndex**.

**HTTP Request:**
`POST https://yourapi.com/Completion/Chat/ChatProfile`

**Request Payload:**
```json
{
    "ConversationId": "d290f1ee-6c54-4b01-90e6-d701748f0851",
    "ProfileOptions": {
        "Name": "ChatProfile",
        "Model": "gpt-4o",
        "Host": "OpenAI",
        "Temperature": 0.7,
        "MaxTokens": 150,
        "RagDatabase": "MyRagIndex"
    },
    "Messages": [
        {
            "Role": "User",
            "Content": "Can you tell me about Azure AI Services?"
        }
    ]
}
```

**Example using curl:**
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
               "MaxTokens": 150,
               "RagDatabase": "MyRagIndex"
           },
           "Messages": [
               {
                   "Role": "User",
                   "Content": "Can you tell me about Azure AI Services?"
               }
           ]
         }'`

When processed, the AI model will use the documents and context from **MyRagIndex** to provide an informed response to your query. By following these steps, you integrate the RAG database into your chat workflows, allowing the AI model to reference up-to-date and relevant information stored in your custom index.

---

## API Reference 

### Chat Completion API Reference
###### Base URL `/Completion`
&nbsp;
The `CompletionController` handles LLM chat requests (completions) via standard HTTP responses or Server-Sent Events (SSE). For streaming, please see the section on the `ChatHub`.
### Endpoints

#### 1\. Chat Request (Standard Response)

-   **HTTP Request**: `POST /Completion/Chat/{name?}`
-   **Description**: Receives a chat request and returns a standard HTTP response. The only required parameters are the name of the agent profile, and a `Messages` array with at least one Message with its `Role` set to `User`, and a valid `Content` string.
    -   Provide a `ConversationId` if you want to let the API handle conversation persistence, or leave null. 
    -   The `ProfileOptions` object can be used to overwrite any values saved to the agent profile configuration, but is entirely optional so long as the `name` is passed in through the request route parameter. All values conform to their respective 'host' API's, and may be ignored or behave differently depending on the chosen `Host`. Please check the documentation for the `Host`'s API if in doubt. For any discrepencies, please raise an issue in [the GitHub repo](https://github.com/Jacob-J-Thomas/IntelligenceHub/issues).
-   **Route Parameters**:
    -   `Name` (string): The agent profile `Name` for the request. Defaults to `ProfileOptions.Name` in the request body if not provided. Is used to provide the default values for the `ProfileOptions`.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Request Payload**:
        ```json
        {   
            "ConversationId": "A GUID can be provided here to persist conversational data. (optional)",   
            "ProfileOptions": {   
                "Name": "The name of the agent profile to use to populate these options. (optional if the name is provided in the request route)",   
                "Model": "The name of the model. (optional)",   
                "Host": "The name of the host service. Valid values are: Azure | OpenAI | Anthropic. (optional)",   
                "ImageHost": "The service to use for image generation. (optional - defaults to 'Host' if empty)",   
                "RagDatabase": "The name of a RAG database, if one is being used. (optional)",   
                "FrequencyPenalty": "The frequency penalty. (optional)",   
                "PresencePenalty": "The presence penalty. (optional)",   
                "Temperature": "The temperature. (optional)",   
                "TopP": "The TopP. (optional)",   
                "MaxTokens": "The maximum number of tokens to use for the completion. (optional)",   
                "TopLogprobs": "How many log probs to return (optional)",   
                "Logprobs": "Whether or not to return log probabilities (optional)",   
                "User": "A user name to associate with the request. (optional)",   
                "ToolChoice": "A tool to call. Can be used to force image generation, for example. (optional)",   
                "ResponseFormat": "Valid values are: Json | Text (optional)",   
                "SystemMessage": "The system message (optional)",   
                "Stop": ["An array of stop strings", "..."],   
                "Tools": [  
                    {  
                        "Type": "function",   
                        "Function": {   
                            "Name": "The name of the tool.",   
                            "Description": "A description of the tool. (optional)",   
                            "Parameters": {   
                                "type": "object",   
                                "properties": {   
                                    "propertyName": {   
                                        "type": "string",   
                                        "description": "A description of the property. (optional)"   
                                    } 
                                },  
                                "required": ["propertyName"]  
                            } 
                        },  
                        "ExecutionUrl": "If provided, sends the arguments for this tool to the provided URL. (optional)",   
                        "ExecutionMethod": "The kind of method to execute at the Execution URL. Valid values are GET | POST | PUT | PATCH | DELETE (optional)",   
                        "ExecutionBase64Key": "A base64 encoded key that can be used to perform basic authentication at the ExecutionURL. (optional)"   
                    } 
                ],  
                "MaxMessageHistory": "The maximum number of past messages to include based on the data associated with the ConversationId. (optional)",   
                "ReferenceProfiles": ["An array of profile names that will be provided in a system tool, allowing models to continue the conversation before returning a response. (optional)", "..."]  
            },  
            "Messages": [  
                {  
                    "Role": "User | Assistant | System | Tool",   
                    "Content": "The content of the message.",   
                    "Base64Image": "An base64 encoded image string. (optional)",   
                    "TimeStamp": "DateTime (UTC). (optional)"   
                } 
            ] 
        } 
        ```

-   **Responses**:
    -   `200 OK`: Returns the chat completion response. Schema: CompletionResponse
    -   `400 Bad Request`: Request payload fails validation.
    -   `404 Not Found`: Profile or resource not found.
    -   `429 Too Many Requests`: Rate limits exceeded.
    -   `500 Internal Server Error`: Unexpected server error.
    
**Example Request**:
`curl -X POST "https://yourapi.com/Completion/Chat/ChatProfile" \  -H "Authorization: Bearer {token}" \  -H "Content-Type: application/json" \  -d '{  "ConversationId": "d290f1ee-6c54-4b01-90e6-d701748f0851", "ProfileOptions": { "Name": "ChatProfile", "Model": "gpt-4o", "Host": "OpenAI", "Temperature": 0.7, "MaxTokens": 150 }, "Messages": [ { "Role": "User", "Content": "Hello, how are you?" } ] }'  `

**Example Response:**
```json
{
    "Messages": [
        {
            "Role": "Assistant",
            "Content": "I'm good! How can I assist you today?"
        }
    ],
    "ToolCalls": {},
    "ToolExecutionResponses": [],
    "FinishReason": "Stop | Length | ToolCalls | ContentFilter | TooManyRequests | Error"
}
```
&nbsp;
#### 2\. Chat Request (SSE Streaming)

-   **HTTP Request**: `POST /Completion/SSE/{name?}`
-   **Description**: Processes a chat request and returns the result via SSE.
-   **Route Parameters**:
    -   `name` (string): Profile name for the request. Defaults to `ProfileOptions.Name` in the request body if not provided.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload Schema**: Refer to the Completions\Chat\{profileName} request payload - see 1\. Chat Request (Standard Response).
-   **Response**:
    -   **Content-Type**: `text/event-stream`
    -   **Response Format**: Streamed in chunks of `CompletionStreamChunk`.
-   **Response Codes**:
    -   `200 OK`: Stream initiated successfully.
    -   `400 Bad Request`: Request payload fails validation.
    -   `404 Not Found`: Profile or resource not found.
    -   `429 Too Many Requests`: Rate limits exceeded.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`curl -X POST "https://yourapi.com/Completion/SSE/ChatProfile" \  -H "Authorization: Bearer {token}" \  -H "Content-Type: application/json" \  -N \ -d 
'{ "ProfileOptions": { "Name": "ChatProfile", "Model": "gpt-4o", "Host": "OpenAI", "Temperature": 0.7, "MaxTokens": 150 }, "Messages": [ { "Role": "User", "Content": "Hello, stream my response!" } ] }'  `\

**Example SSE Response**:
`data: { "chunkProperty": "chunkValue", ... } data: { "chunkProperty": "chunkValue", ... } `

### Error Handling
The API uses standard HTTP status codes to indicate the result of the operation:

-   `400 Bad Request`: Issues with validation.
-   `404 Not Found`: Profile or resource not found.
-   `429 Too Many Requests`: Rate limits exceeded.
-   `500 Internal Server Error`: Unexpected server error.

### Summary

-   Use **POST** `/Completion/Chat/{name?}` for standard chat completions.
-   Use **POST** `/Completion/SSE/{name?}` to receive a streaming response via SSE.
-   Validate that the `CompletionRequest` object contains a valid `ProfileOptions` and a non-empty `Messages` array.
-   Handle responses according to the status codes provided.

---

&nbsp;
# ChatHub (SignalR Hub)

### Overview
###### Base URL `/ChatStream`
The `ChatHub` is a SignalR hub designed to stream chat completion responses to connected clients. 

### Methods

#### 1. Send(CompletionRequest completionRequest)

-   **Description**: Sends a chat completion request and streams response chunks back to the client.
-   **Parameters**: Refer to the `Completions/Chat/{profileName}` request payload - see 1\. Chat Request (Standard Response).
-   **Returns**: An asynchronous `Task` streaming response chunks using SignalR.
-   **Workflow**:
    1.  **Validation**:
        -   Validates the request.
        -   If validation fails, an error is sent to the client.
    2.  **Processing**:
        -   If validation passes, initiates the chat completion process.
    3.  **Response Streaming**:
        -   Streams response chunks to the client.

### C# Usage Example

```csharp
// Create a new completion request  
var request = new CompletionRequest 
{
    ConversationId = Guid.NewGuid(), 
    ProfileOptions = new Profile  
    { 
        Name = "DefaultProfileName",  
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
await hubConnection.InvokeAsync("Send", request); 
```

### Notes

-   **Validation**: The `ChatHub` validates incoming `CompletionRequest` objects. Validation errors result in immediate responses to the client.
-   **Streaming**: Responses are streamed back to the client in chunks. Each chunk includes a status code and possibly error messages.

---

### Profile API Reference

###### Base URL `/Profile`

The `ProfileController` manages agent profiles. It provides endpoints for retrieving, creating/updating, associating/dissociating tools, and deleting profiles.

### Endpoints

#### 1. Get Profile By Name
- **HTTP Request**: `GET /Profile/get/{name}`
- **Description**: Retrieves the profile associated with the specified `name`.
- **Route Parameters**:
  - `name` (string): The name of the agent profile.
- **Responses**:
  - `200 OK`: Returns the profile object. Schema: `Profile`
  - `400 Bad Request`: Invalid route parameter.
  - `404 Not Found`: Profile not found.
  - `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Profile/get/ChatProfile`

 **Example Response**:
```json
{   
    "Name": "ProfileName",   
    "Model": "gpt4o",   
    "Host": "OpenAI",   
    "ImageHost": "OpenAI",   
    "RagDatabase": "MyRagIndex",   
    "FrequencyPenalty": .5,   
    "PresencePenalty": .5,   
    "Temperature": .5,   
    "TopP": .5,
    "MaxTokens": 4000,
    "TopLogprobs": 2,   
    "Logprobs": true,   
    "User": "Jacob",   
    "ToolChoice": "TestTool",   
    "ResponseFormat": "Text",   
    "SystemMessage": "You are an example assistant.",   
    "Stop": ["string", "another string"],   
    "Tools": [  
        {  
            "Type": "function",   
            "Function": {   
                "Name": "TestTool",   
                "Description": "A tool description for example purposes. This will be sent to example.com",   
                "Parameters": {   
                    "type": "object",   
                    "properties": {   
                        "anExampleProperty": {   
                            "type": "string",   
                            "description": "An example description of the property."   
                        } 
                    },  
                    "required": ["anExampleProperty"]  
                } 
            },  
            "ExecutionUrl": "https://example.com",   
            "ExecutionMethod": "POST",   
            "ExecutionBase64Key": "A base key"   
        } 
    ],  
    "MaxMessageHistory": 20,   
    "ReferenceProfiles": ["ProfileName", "AnotherProfile"]  
}
```

#### 2\. Get All Profiles

-   **HTTP Request**: `GET /Profile/get/all/page/{page}/count/{count}`
-   **Description**: Retrieves a paginated list of profiles.
-   **Route Parameters**:
    -   `page` (integer): The page number to retrieve (must be ≥ 1).
    -   `count` (integer): The number of profiles per page (must be ≥ 1).
-   **Responses**:
    -   `200 OK`: Returns a list of profile objects.
    -   `400 Bad Request`: Invalid pagination parameters.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Profile/get/all/page/1/count/10`

**Example Response**:
```json
[
    {   
        "Name": "ProfileName",   
        "Model": "gpt4o",   
        "Host": "OpenAI",   
        "ImageHost": "OpenAI",   
        "RagDatabase": "MyRagIndex",   
        "FrequencyPenalty": .5,   
        "PresencePenalty": .5,   
        "Temperature": .5,   
        "TopP": .5,
        "MaxTokens": 4000,
        "TopLogprobs": 2,   
        "Logprobs": true,   
        "User": "Jacob",   
        "ToolChoice": "TestTool",   
        "ResponseFormat": "Text",   
        "SystemMessage": "You are an example assistant.",   
        "Stop": ["string", "another string"],   
        "Tools": [  
            {  
                "Type": "function",   
                "Function": {   
                    "Name": "TestTool",   
                    "Description": "A tool description for example purposes. This will be sent to example.com",   
                    "Parameters": {   
                        "type": "object",   
                        "properties": {   
                            "anExampleProperty": {   
                                "type": "string",   
                                "description": "An example description of the property."   
                            } 
                        },  
                        "required": ["anExampleProperty"]  
                    } 
                },  
                "ExecutionUrl": "https://example.com",   
                "ExecutionMethod": "POST",   
                "ExecutionBase64Key": "A base key"   
            } 
        ],  
        "MaxMessageHistory": 20,   
        "ReferenceProfiles": ["ProfileName", "AnotherProfile"]  
    },
    {   
        "Name": "AnotherProfile",   
        "Model": "gpt4o",   
        "Host": "OpenAI",   
        "ImageHost": "OpenAI",   
        "RagDatabase": "MyRagIndex",   
        "FrequencyPenalty": .5,   
        "PresencePenalty": .5,   
        "Temperature": .5,   
        "TopP": .5,
        "MaxTokens": 4000,
        "TopLogprobs": 2,   
        "Logprobs": true,   
        "User": "Jacob",   
        "ToolChoice": "TestTool",   
        "ResponseFormat": "Text",   
        "SystemMessage": "You are an example assistant.",   
        "Stop": ["string", "another string"],   
        "Tools": [  
            {  
                "Type": "function",   
                "Function": {   
                    "Name": "TestTool",   
                    "Description": "A tool description for example purposes. This will be sent to example.com",   
                    "Parameters": {   
                        "type": "object",   
                        "properties": {   
                            "anExampleProperty": {   
                                "type": "string",   
                                "description": "An example description of the property."   
                            } 
                        },  
                        "required": ["anExampleProperty"]  
                    } 
                },  
                "ExecutionUrl": "https://example.com",   
                "ExecutionMethod": "POST",   
                "ExecutionBase64Key": "A base key"   
            } 
        ],  
        "MaxMessageHistory": 20,   
        "ReferenceProfiles": ["ProfileName", "AnotherProfile"]  
    }
]
```

#### 3\. Create or Update Profile

-   **HTTP Request**: `POST /Profile/upsert`
-   **Description**: Creates a new profile or updates an existing one.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: Refer to the `Completions/Chat/{profileName}` request payload - see 1\. Chat Request (Standard Response).

-   **Responses**:
    -   `200 OK`: Returns the newly created or updated profile. Schema: `Profile`
    -   `400 Bad Request`: Request payload fails validation.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`POST https://yourapi.com/Profile/upsert`

See the request payload at `Completions/Chat/{profileName}`.

**Example Response**:
```json
{   
    "Name": "ProfileName",   
    "Model": "gpt4o",   
    "Host": "OpenAI",   
    "ImageHost": "OpenAI",   
    "RagDatabase": "MyRagIndex",   
    "FrequencyPenalty": .5,   
    "PresencePenalty": .5,   
    "Temperature": .5,   
    "TopP": .5,
    "MaxTokens": 4000,
    "TopLogprobs": 2,   
    "Logprobs": true,   
    "User": "Jacob",   
    "ToolChoice": "TestTool",   
    "ResponseFormat": "Text",   
    "SystemMessage": "You are an example assistant.",   
    "Stop": ["string", "another string"],   
    "Tools": [  
        {  
            "Type": "function",   
            "Function": {   
                "Name": "TestTool",   
                "Description": "A tool description for example purposes. This will be sent to example.com",   
                "Parameters": {   
                    "type": "object",   
                    "properties": {   
                        "anExampleProperty": {   
                            "type": "string",   
                            "description": "An example description of the property."   
                        } 
                    },  
                    "required": ["anExampleProperty"]  
                } 
            },  
            "ExecutionUrl": "https://example.com",   
            "ExecutionMethod": "POST",   
            "ExecutionBase64Key": "A base key"   
        } 
    ],  
    "MaxMessageHistory": 20,   
    "ReferenceProfiles": ["ProfileName", "AnotherProfile"]  
}
```

#### 4\. Associate Profile with Tools

-   **HTTP Request**: `POST /Profile/associate/{name}`
-   **Description**: Associates the specified profile with one or more tools available in the database.
-   **Route Parameters**:
    -   `name` (string): The name of the profile.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: An array of tool names.
        `["Tool1", "Tool2"]`

-   **Responses**:
    -   `200 OK`: Returns a list of tools successfully associated with the profile.
    -   `400 Bad Request`: Invalid profile name or tools list.
    -   `404 Not Found`: Profile not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`POST https://yourapi.com/Profile/associate/ChatProfile`

`["Tool1", "Tool2"]`

**Example Response**:
`["Tool1", "Tool2"]`

#### 5\. Dissociate Profile from Tools

-   **HTTP Request**: `POST /Profile/dissociate/{name}`
-   **Description**: Dissociates one or more tools from the specified profile.
-   **Route Parameters**:
    -   `name` (string): The name of the profile.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: An array of tool names.
            `["Tool1", "Tool2"]`

-   **Responses**:
    -   `200 OK`: Returns a list of tools successfully dissociated from the profile.
    -   `400 Bad Request`: Invalid profile name or tools list.
    -   `404 Not Found`: Profile or tools association not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`POST https://yourapi.com/Profile/dissociate/ChatProfile`

`["Tool1", "Tool2"]`

**Example Response**:
`["Tool1", "Tool2"]`

#### 6\. Delete Profile

-   **HTTP Request**: `DELETE /Profile/delete/{name}`
-   **Description**: Deletes the specified profile.
-   **Route Parameters**:
    -   `name` (string): The name of the profile to delete.
-   **Responses**:
    -   `204 No Content`: Profile deleted successfully.
    -   `400 Bad Request`: Invalid profile name.
    -   `404 Not Found`: Profile not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`DELETE https://yourapi.com/Profile/delete/ChatProfile`

**Example Response**: *No content*

---

### Tool API Reference
###### Base URL `/Tool`
&nbsp;
The `ToolController` manages agent tools. It provides endpoints for retrieving individual tools, listing all tools, managing tool-profile associations, creating or updating tools, and deleting tools.

### Endpoints

#### 1. Get Tool By Name
- **HTTP Request**: `GET /Tool/get/{name}`
- **Description**: Retrieves the tool associated with the specified `name`.
- **Route Parameters**:
  - `name` (string): The name of the tool.
- **Responses**:
  - `200 OK`: Returns the tool object. Schema: `Tool`
  - `400 Bad Request`: Invalid tool name.
  - `404 Not Found`: Tool not found.
  - `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Tool/get/ImageProcessor`

 **Example Response**:
```json
{  
    "Type": "function",   
    "Function": {   
        "Name": "TestTool",   
        "Description": "A tool description for example purposes. This will be sent to example.com",   
        "Parameters": {   
            "type": "object",   
            "properties": {   
                "anExampleProperty": {   
                    "type": "string",   
                    "description": "An example description of the property."   
                } 
            },  
            "required": ["anExampleProperty"]  
        } 
    },  
    "ExecutionUrl": "https://example.com",   
    "ExecutionMethod": "POST",   
    "ExecutionBase64Key": "A base key"   
} 
```

#### 2\. Get All Tools

-   **HTTP Request**: `GET /Tool/get/all/page/{page}/count/{count}`
-   **Description**: Retrieves a paginated list of tools.
-   **Route Parameters**:
    -   `page` (integer): The page number to retrieve (must be ≥ 1).
    -   `count` (integer): The number of tools per page (must be ≥ 1).
-   **Responses**:
    -   `200 OK`: Returns a list of tool objects.
    -   `400 Bad Request`: Invalid pagination parameters.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Tool/get/all/page/1/count/10`

**Example Response**:
```json
[
    {  
        "Type": "function",   
        "Function": {   
            "Name": "TestTool",   
            "Description": "A tool description for example purposes. This will be sent to example.com",   
            "Parameters": {   
                "type": "object",   
                "properties": {   
                    "anExampleProperty": {   
                        "type": "string",   
                        "description": "An example description of the property."   
                    } 
                },  
                "required": ["anExampleProperty"]  
            } 
        },  
        "ExecutionUrl": "https://example.com",   
        "ExecutionMethod": "POST",   
        "ExecutionBase64Key": "A base key"   
    },
    {  
        "Type": "function",   
        "Function": {   
            "Name": "TestTool",   
            "Description": "A tool description for example purposes. This will be sent to example.com",   
            "Parameters": {   
                "type": "object",   
                "properties": {   
                    "anExampleProperty": {   
                        "type": "string",   
                        "description": "An example description of the property."   
                    } 
                },  
                "required": ["anExampleProperty"]  
            } 
        },  
        "ExecutionUrl": "https://example.com",   
        "ExecutionMethod": "POST",   
        "ExecutionBase64Key": "A base key"   
    } 
]
```

#### 3\. Get Tool Profiles

-   **HTTP Request**: `GET /Tool/get/{name}/profiles`
-   **Description**: Retrieves a list of profile names associated with the specified tool.
-   **Route Parameters**:
    -   `name` (string): The name of the tool.
-   **Responses**:
    -   `200 OK`: Returns a list of profile names that use the tool.
    -   `400 Bad Request`: Invalid tool name.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Tool/get/ImageProcessor/profiles`

**Example Response**:
`[ "ChatProfile", "ImageAssistant" ]`

#### 4\. Create or Update Tools

-   **HTTP Request**: `POST /Tool/upsert`
-   **Description**: Creates new tool definitions or updates existing ones.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: An array of tool objects.
        ```json
        [
            {  
                "Type": "function",   
                "Function": {   
                    "Name": "TestTool",   
                    "Description": "A tool description for example purposes. This will be sent to example.com",   
                    "Parameters": {   
                        "type": "object",   
                        "properties": {   
                            "anExampleProperty": {   
                                "type": "string",   
                                "description": "An example description of the property."   
                            } 
                        },  
                        "required": ["anExampleProperty"]  
                    } 
                },  
                "ExecutionUrl": "https://example.com",   
                "ExecutionMethod": "POST",   
                "ExecutionBase64Key": "A base key"   
            },
            {  
                "Type": "function",   
                "Function": {   
                    "Name": "TestTool",   
                    "Description": "A tool description for example purposes. This will be sent to example.com",   
                    "Parameters": {   
                        "type": "object",   
                        "properties": {   
                            "anExampleProperty": {   
                                "type": "string",   
                                "description": "An example description of the property."   
                            } 
                        },  
                        "required": ["anExampleProperty"]  
                    } 
                },  
                "ExecutionUrl": "https://example.com",   
                "ExecutionMethod": "POST",   
                "ExecutionBase64Key": "A base key"   
            } 
        ]
        ```

-   **Responses**:
    -   `200 OK`: Returns the updated list of tool objects.
    -   `400 Bad Request`: Request payload fails validation.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`POST https://yourapi.com/Tool/upsert`

with the payload as shown above.

**Example Response**:
```json
[
    {  
        "Type": "function",   
        "Function": {   
            "Name": "TestTool",   
            "Description": "A tool description for example purposes. This will be sent to example.com",   
            "Parameters": {   
                "type": "object",   
                "properties": {   
                    "anExampleProperty": {   
                        "type": "string",   
                        "description": "An example description of the property."   
                    } 
                },  
                "required": ["anExampleProperty"]  
            } 
        },  
        "ExecutionUrl": "https://example.com",   
        "ExecutionMethod": "POST",   
        "ExecutionBase64Key": "A base key"   
    },
    {  
        "Type": "function",   
        "Function": {   
            "Name": "TestTool",   
            "Description": "A tool description for example purposes. This will be sent to example.com",   
            "Parameters": {   
                "type": "object",   
                "properties": {   
                    "anExampleProperty": {   
                        "type": "string",   
                        "description": "An example description of the property."   
                    } 
                },  
                "required": ["anExampleProperty"]  
            } 
        },  
        "ExecutionUrl": "https://example.com",   
        "ExecutionMethod": "POST",   
        "ExecutionBase64Key": "A base key"   
    } 
]
```

#### 5\. Associate Tool with Profiles

-   **HTTP Request**: `POST /Tool/associate/{name}`
-   **Description**: Associates the specified tool with one or more profiles.
-   **Route Parameters**:
    -   `name` (string): The name of the tool.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: An array of profile names.
        `["ChatProfile", "ImageAssistant"]`

-   **Responses**:
    -   `200 OK`: Returns a list of profile names successfully associated with the tool.
    -   `400 Bad Request`: Invalid tool name or profiles list.
    -   `404 Not Found`: Tool or profile not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`POST https://yourapi.com/Tool/associate/ImageProcessor`

with body:
`["ChatProfile", "ImageAssistant"]`

**Example Response**:
`["ChatProfile", "ImageAssistant"]`

#### 6\. Dissociate Tool from Profiles

-   **HTTP Request**: `POST /Tool/dissociate/{name}`
-   **Description**: Dissociates the specified tool from one or more profiles.
-   **Route Parameters**:
    -   `name` (string): The name of the tool.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: An array of profile names.
        `["ChatProfile", "ImageAssistant"]`

-   **Responses**:
    -   `200 OK`: Returns a list of profile names successfully dissociated from the tool.
    -   `400 Bad Request`: Invalid tool name or profiles list.
    -   `404 Not Found`: Tool or association not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`POST https://yourapi.com/Tool/dissociate/ImageProcessor`

with body:
`["ChatProfile", "ImageAssistant"]`

**Example Response**:
`["ChatProfile", "ImageAssistant"]`

#### 7\. Delete Tool

-   **HTTP Request**: `DELETE /Tool/delete/{name}`
-   **Description**: Deletes the specified tool.
-   **Route Parameters**:
    -   `name` (string): The name of the tool to delete.
-   **Responses**:
    -   `204 No Content`: Tool deleted successfully.
    -   `400 Bad Request`: Invalid tool name.
    -   `404 Not Found`: Tool not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`DELETE https://yourapi.com/Tool/delete/ImageProcessor`

**Example Response**: *No content*

---

### Message History API Reference

###### Base URL `/MessageHistory`

The Message History API provides endpoints to manage message histories. You can retrieve messages, update or create a message history, delete an entire message history, add individual messages, or remove specific messages.

### Endpoints

#### 1. Get Message History
- **HTTP Request**: `GET /MessageHistory/{id}`
- **Description**: Retrieves the message history for the specified message history identifier.
- **Route Parameters**:
  - `id` (string): The unique identifier for the message history.
- **Query Parameters**:
  - `count` (integer, required): Number of messages to retrieve.
  - `page` (integer, required): Page offset for pagination.
- **Responses**:
  - `200 OK`: Returns a list of message objects.
  - `400 Bad Request`: If parameters are invalid.
  - `404 Not Found`: If the message history is not found.

**Example Response**:
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
```

#### 2\. Update or Create Message History

-   **HTTP Request**: `POST /MessageHistory/{messageHistoryId}`
-   **Description**: Validates and updates (or creates) a message history by adding the provided list of messages.
-   **Route Parameters**:
    -   `messageHistoryId` (string): The unique identifier for the message history.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: JSON array of `Message` objects.
        ```json
        [
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
        ]
        ```

-   **Responses**:
    -   `200 OK`: Returns the successfully added messages.
    -   `400 Bad Request`: Validation errors.

#### 3\. Delete Message History

-   **HTTP Request**: `DELETE /MessageHistory/{id}`
-   **Description**: Deletes the entire message history identified by the provided ID.
-   **Route Parameters**:
    -   `id` (string): The unique identifier for the message history.
-   **Responses**:
    -   `200 OK`: Indicates successful deletion.
    -   `404 Not Found`: If the message history is not found.

**Example Response**:
```json
{
    "status": "Ok",
    "data": true
}
```

#### 4\. Add Message to Message History

-   **HTTP Request**: `POST /MessageHistory/{messageHistoryId}/message`
-   **Description**: Adds a single message to an existing message history.
-   **Route Parameters**:
    -   `messageHistoryId` (string): The unique identifier for the message history.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: JSON representation of a `Message` object.
        ```json
        {
            "Role": "User",
            "Content": "Can I update my shipping address?",
            "Base64Image": null,
            "TimeStamp": "2025-03-05T12:45:00Z"
        }
        ```

-   **Responses**:
    -   `200 OK`: Returns the newly added message.
    -   `404 Not Found`: If the message history is not found.

#### 5\. Delete Message from Message History

-   **HTTP Request**: `DELETE /MessageHistory/{messageHistoryId}/message/{messageId}`
-   **Description**: Deletes a specific message from the message history.
-   **Route Parameters**:
    -   `messageHistoryId` (string): The unique identifier for the message history.
    -   `messageId` (string): The unique identifier for the message.
-   **Responses**:
    -   `200 OK`: Indicates successful deletion.
    -   `404 Not Found`: If the message history or message is not found.

**Example Response**:
```json
{
    "status": "Ok",
    "data": true
}
```

### RAG Index API Reference
###### Base URL `/Rag`
&nbsp;
The `RagController` manages RAG indexes for Azure AI Search Services. This API allows you to create, configure, query, and manage RAG indexes and their documents. It enforces strict validation rules to ensure that index definitions and document contents conform to both internal requirements and Azure AI Search limits.

> **Note:**  
> When creating or configuring an index, the API validates the provided definition using rules such as:  
> - **Index Name:** Must be non-empty, no longer than 128 characters, and only include alphanumeric characters or underscores (must not match SQL or API keywords).  
> - **GenerationHost Requirement:** If content summarization features (`GenerateKeywords` or `GenerateTopic`) are enabled, a `GenerationHost` must be provided.  
> - **IndexingInterval:** Must be a positive value less than 1 day.  
> - **EmbeddingModel:** Maximum length is 255 characters.  
> - **MaxRagAttachments:** Must be a non-negative integer not exceeding 20.  
> - **ChunkOverlap:** Must be between 0 and 1 (inclusive).  
> - **ScoringProfile (if provided):** Must adhere to length and non-negative numeric constraints.  
>  
> Additionally, document upsert requests are validated to ensure that each document has a non-empty title and content, with limits on content size and field lengths.

---

### Endpoints

#### 1. Get RAG Index
- **HTTP Request**: `GET /Rag/Index/{index}`
- **Description**: Retrieves the metadata for a RAG index identified by its name.
- **Route Parameters**:
  - `index` (string): The name of the RAG index.
- **Responses**:
  - `200 OK`: Returns the index metadata. Schema: `IndexMetadata`
  - `400 Bad Request`: If the index name is invalid.
  - `404 Not Found`: If the index is not found.
  - `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Rag/Index/MyRagIndex`

 **Example Response**:
```json
{
    "Name": "MyRagIndex",
    "GenerationHost": "AzureAI",
    "IndexingInterval": "00:05:00",
    "EmbeddingModel": "ada-text-embedding-002 | text-embedding-3-large",
    "MaxRagAttachments": 10,
    "ChunkOverlap": 0.2,
    "ScoringProfile": {
        "Name": "DefaultScoring",
        "SearchAggregation": "Sum | Average | Minimum | Maximum | FirstMatching",
        "SearchInterpolation": "Linear | Constant | Quadratic | Logarithmic",
        "FreshnessBoost": 1.0,
        "BoostDurationDays": 7,
        "TagBoost": 1.5,
        "Weights": {
            "field1": 0.8,
            "field2": 1.2
        }
    }
}
```

#### 2\. Get All RAG Indexes

-   **HTTP Request**: `GET /Rag/Index/All`
-   **Description**: Retrieves a list of all RAG index metadata.
-   **Responses**:
    -   `200 OK`: Returns a list of indexes. Schema: `IEnumerable<IndexMetadata>`
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Rag/Index/All`

**Example Response**:
```json
[
    {
    "Name": "MyRagIndex",
    "GenerationHost": "AzureAI",
    "IndexingInterval": "00:05:00",
    "EmbeddingModel": "ada-text-embedding-002 | text-embedding-3-large",
    "MaxRagAttachments": 10,
    "ChunkOverlap": 0.2,
    "ScoringProfile": {
        "Name": "DefaultScoring",
        "SearchAggregation": "Sum | Average | Minimum | Maximum | FirstMatching",
        "SearchInterpolation": "Linear | Constant | Quadratic | Logarithmic",
        "FreshnessBoost": 1.0,
        "BoostDurationDays": 7,
        "TagBoost": 1.5,
        "Weights": {
            "field1": 0.8,
            "field2": 1.2
        }
    }
},
    {
    "Name": "AnotherIndex",
    "GenerationHost": "AzureAI",
    "IndexingInterval": "00:05:00",
    "EmbeddingModel": "ada-text-embedding-002 | text-embedding-3-large",
    "MaxRagAttachments": 10,
    "ChunkOverlap": 0.2,
    "ScoringProfile": {
        "Name": "SecondaryScoring",
        "SearchAggregation": "Sum | Average | Minimum | Maximum | FirstMatching",
        "SearchInterpolation": "Linear | Constant | Quadratic | Logarithmic",
        "FreshnessBoost": 1.0,
        "BoostDurationDays": 7,
        "TagBoost": 1.5,
        "Weights": {
            "field1": 0.8,
            "field2": 1.2
        }
    }
}
]
```

#### 3\. Create RAG Index

-   **HTTP Request**: `POST /Rag/Index`
-   **Description**: Creates a new RAG index in Azure AI Search Services using the provided index definition. The definition is validated to ensure compliance with naming rules, indexing intervals, scoring profiles, and other constraints.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: An `IndexMetadata` object.\
        **Validation highlights:**
        -   **Name**: Must be non-empty, ≤ 128 characters, and conform to naming conventions (alphanumeric and underscores only, excluding SQL/API keywords).
        -   **GenerationHost**: Required if `GenerateKeywords` or `GenerateTopic` is set to true.
        -   **IndexingInterval**: Must be positive and less than 1 day.
        -   **EmbeddingModel**: Maximum length is 255 characters.
        -   **MaxRagAttachments**: Must be non-negative and no greater than 20.
        -   **ChunkOverlap**: Must be between 0 and 1 (inclusive).
        -   **ScoringProfile**: If provided, all properties must meet their respective length and non-negative constraints.
-   **Responses**:
    -   `200 OK`: Returns the created index metadata.
    -   `400 Bad Request`: If validation fails or the request payload is malformed.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
```json
{
    "Name": "MyRagIndex",
    "GenerationHost": "AzureAI",
    "IndexingInterval": "00:05:00",
    "EmbeddingModel": "ada-text-embedding-002",
    "MaxRagAttachments": 10,
    "ChunkOverlap": 0.2,
    "ScoringProfile": {
        "Name": "DefaultScoring",
        "SearchAggregation": "Sum | Average | Minimum | Maximum | FirstMatching",
        "SearchInterpolation": "Linear | Constant | Quadratic | Logarithmic",
        "FreshnessBoost": 1.0,
        "BoostDurationDays": 7,
        "TagBoost": 1.5,
        "Weights": {
            "field1": 0.8,
            "field2": 1.2
        }
    },
    "GenerateKeywords": true,
    "GenerateTopic": false
}
```

**Example Response**:
```json
{
    "Name": "MyRagIndex",
    "GenerationHost": "AzureAI",
    "IndexingInterval": "00:05:00",
    "EmbeddingModel": "ada-text-embedding-002",
    "MaxRagAttachments": 10,
    "ChunkOverlap": 0.2,
    "ScoringProfile": {
        "Name": "DefaultScoring",
        "SearchAggregation": "Sum",
        "SearchInterpolation": "Linear",
        "FreshnessBoost": 1.0,
        "BoostDurationDays": 7,
        "TagBoost": 1.5,
        "Weights": {
            "field1": 0.8,
            "field2": 1.2
        }
    },
    "GenerateKeywords": true,
    "GenerateTopic": false
}
```

#### 4\. Configure RAG Index

-   **HTTP Request**: `POST /Rag/Index/Configure/{index}`
-   **Description**: Configures an existing RAG index with a new definition. The updated configuration is validated using the same rules as index creation.
-   **Route Parameters**:
    -   `index` (string): The name of the index to configure.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: Updated `IndexMetadata` object.
-   **Responses**:
    -   `200 OK`: Returns the updated index metadata.
    -   `400 Bad Request`: If validation fails or the request payload is malformed.
    -   `404 Not Found`: If the index is not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
```json
{
    "Name": "MyRagIndex",
    "GenerationHost": "AzureAI",
    "IndexingInterval": "00:05:00",
    "EmbeddingModel": "text-embedding-3-large | ada-text-embedding-002",
    "MaxRagAttachments": 10,
    "ChunkOverlap": 0.2,
    "ScoringProfile": {
        "Name": "DefaultScoring",
        "SearchAggregation": "Sum | Average | Minimum | Maximum | FirstMatching",
        "SearchInterpolation": "Linear | Constant | Quadratic | Logarithmic",
        "FreshnessBoost": 1.0,
        "BoostDurationDays": 7,
        "TagBoost": 1.5,
        "Weights": {
            "field1": 0.8,
            "field2": 1.2
        }
    },
    "GenerateKeywords": true,
    "GenerateTopic": false
}
```

**Example Response**:
```json
{
    "Name": "MyRagIndex",
    "GenerationHost": "AzureAI",
    "IndexingInterval": "00:05:00",
    "EmbeddingModel": "text-embedding-3-large",
    "MaxRagAttachments": 10,
    "ChunkOverlap": 0.2,
    "ScoringProfile": {
        "Name": "DefaultScoring",
        "SearchAggregation": "Sum",
        "SearchInterpolation": "Linear",
        "FreshnessBoost": 1.0,
        "BoostDurationDays": 7,
        "TagBoost": 1.5,
        "Weights": {
            "field1": 0.8,
            "field2": 1.2
        }
    },
    "GenerateKeywords": true,
    "GenerateTopic": false
}
```

#### 5\. Query RAG Index

-   **HTTP Request**: `GET /Rag/Index/{index}/Query/{query}`
-   **Description**: Executes a query against the specified RAG index to retrieve matching documents.
-   **Route Parameters**:
    -   `index` (string): The name of the RAG index.
    -   `query` (string): The search query.
-   **Responses**:
    -   `200 OK`: Returns a collection of documents that match the query.
    -   `400 Bad Request`: If the query is invalid.
    -   `404 Not Found`: If the index is not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Rag/Index/MyRagIndex/Query/azure`

**Example Response**:
```json
[
    {
        "Title": "Azure AI Services",
        "Content": "Details about Azure AI Search Services..."
    },
    {
        "Title": "Getting Started with Azure AI",
        "Content": "An introductory guide..."
    }
]
```

#### 6\. Run RAG Index Update

-   **HTTP Request**: `POST /Rag/Index/{index}/Run`
-   **Description**: Initiates an update run for the specified RAG index to refresh its data.
-   **Route Parameters**:
    -   `index` (string): The name of the RAG index.
-   **Responses**:
    -   `204 No Content`: Index update run initiated successfully.
    -   `400 Bad Request`: If the index name is invalid.
    -   `404 Not Found`: If the index is not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`POST https://yourapi.com/Rag/Index/MyRagIndex/Run`

#### 7\. Delete RAG Index

-   **HTTP Request**: `DELETE /Rag/Index/Delete/{index}`
-   **Description**: Deletes the specified RAG index.
-   **Route Parameters**:
    -   `index` (string): The name of the RAG index to delete.
-   **Responses**:
    -   `204 No Content`: Index deleted successfully.
    -   `400 Bad Request`: If the index name is invalid.
    -   `404 Not Found`: If the index is not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`DELETE https://yourapi.com/Rag/Index/Delete/MyRagIndex`


#### 8\. Get All Documents in a RAG Index

-   **HTTP Request**: `GET /Rag/Index/{index}/Document/{count}/Page/{page}`
-   **Description**: Retrieves a paginated list of documents from the specified RAG index.
-   **Route Parameters**:
    -   `index` (string): The name of the RAG index.
    -   `count` (integer): Number of documents to retrieve in the current batch.
    -   `page` (integer): Page offset (must be 1 or greater).
-   **Responses**:
    -   `200 OK`: Returns a collection of documents. Schema: `IEnumerable<IndexDocument>`
    -   `400 Bad Request`: If pagination parameters or index name are invalid.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Rag/Index/MyRagIndex/Document/10/Page/1`

**Example Response**:
```json
[
    {
        "Title": "Azure AI Services",
        "Content": "Details about Azure AI Search Services..."
    },
    {
        "Title": "Getting Started with Azure AI",
        "Content": "An introductory guide..."
    }
]
```

#### 9\. Get Document from RAG Index

-   **HTTP Request**: `GET /Rag/Index/{index}/document/{document}`
-   **Description**: Retrieves a specific document from a RAG index by its title.
-   **Route Parameters**:
    -   `index` (string): The name of the RAG index.
    -   `document` (string): The title of the document.
-   **Responses**:
    -   `200 OK`: Returns the requested document. Schema: `IndexDocument`
    -   `400 Bad Request`: If parameters are invalid.
    -   `404 Not Found`: If the document is not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`GET https://yourapi.com/Rag/Index/MyRagIndex/document/AzureAIOverview`

**Example Response**:
```json
{
    "Title": "AzureAIOverview",
    "Content": "Comprehensive overview of Azure AI Search Services..."
}
```

#### 10\. Upsert Documents to RAG Index

-   **HTTP Request**: `POST /Rag/index/{index}/Document`
-   **Description**: Upserts (adds or updates) documents in the specified RAG index. The request payload is validated to ensure:
    -   At least one document is provided.
    -   Each document has a non-empty title and content.
    -   Document content does not exceed 1,000,000 characters.
    -   Field length constraints (255 for title/topic/keywords, 4000 for source) are respected.
-   **Route Parameters**:
    -   `index` (string): The name of the RAG index.
-   **Request Body**:
    -   **Content-Type**: `application/json`
    -   **Payload**: A `RagUpsertRequest` containing an array of documents.
        ```json
        {
            "Documents": [
                {
                    "Title": "Azure AI Overview",
                    "Content": "Details about Azure AI Search Services...",
                    "Topic": "Azure",
                    "Keywords": "AI, Search, Azure",
                    "Source": "Official Documentation"
                },
                {
                    "Title": "Getting Started with Azure AI",
                    "Content": "An introductory guide...",
                    "Topic": "Tutorial",
                    "Keywords": "Azure, AI, Beginner",
                    "Source": "Microsoft Learn"
                }
            ]
        }
        ````

-   **Responses**:
    -   `200 OK`: Returns the upserted documents.
    -   `400 Bad Request`: If validation fails or the request payload is malformed.
    -   `404 Not Found`: If the index is not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Response**:
`true`

#### 11\. Delete Documents from RAG Index

-   **HTTP Request**: `DELETE /Rag/index/{index}/Document/{commaDelimitedDocNames}`
-   **Description**: Deletes documents from a RAG index. The `commaDelimitedDocNames` parameter should contain the titles of the documents to be deleted, separated by commas.
-   **Route Parameters**:
    -   `index` (string): The name of the RAG index.
    -   `commaDelimitedDocNames` (string): A comma-delimited string of document titles.
-   **Responses**:
    -   `200 OK`: Returns the number of documents deleted.
    -   `400 Bad Request`: If parameters are invalid.
    -   `404 Not Found`: If one or more documents are not found.
    -   `500 Internal Server Error`: Unexpected server error.

**Example Request**:
`DELETE https://yourapi.com/Rag/index/MyRagIndex/Document/Doc1,Doc2`

**Example Response**:
`2` - an int to validate the appropriate number of documents were deleted.
___

## Contributing
Contributions are welcome! Please follow the steps below to contribute to the project:
1. (Optional) If you would like to gaurentee your changes will be merged, please open an issue to determine if your desired changes align with the project's goals, or choose from an existing issue. Otherwise, feel free to skip this step.
2. Fork the project, create a new branch, and make your changes. 
3. Ensure that any changes include relevant unit tests and IntelliSense documentation, or update existing tests/documentation as needed.
4. Push your changes to your fork, and raise a pull request.

If for whatever reason we miss your request, please feel free to notify us on the pull request itself, at the email provided in the [Contact](#contact) section, or any other method you have at your disposal.

---

## Contact
For any questions comments or concerns, please reach out to Applied.AI.Help@gmail.com, or open an issue in the repository.

---

## License
This project is licensed under the Elastic 2.0 license - see the `LICENSE` file for more details.

---

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