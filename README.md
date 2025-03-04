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
Provide examples and instructions on how to use the project.

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