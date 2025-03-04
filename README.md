# The Intelligence Hub 
## A Powerful API Wrapper for AGI Services Designed to Simplify Consuming Common AI Services such as Chat Completions, RAG Databases, Image Generation, and More.

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
The main goal of this project is to enable rapid setup and development for AI-powered applications. It provides a structured approach to organizing code, data, and documentation, and includes tools for data preprocessing, model training, and deployment. The project is flexible and extensible, allowing easy addition of new features.

Key capabilities include:
- Simplified requests to AGI clients
- Saving and loading conversation history
- Tool execution (returning arguments or executing against external APIs)
- RAG database creation and utilization
- Basic load balancing and retry policies
- A front-end template application for interacting with the API

AI clients are resolved using a custom client factory, allowing new clients to be added easily by implementing the IAGIClient interface. This design also supports image generation from various clients, decoupling the image generation client from the API client.

For more information, please refer to the '[Features](#features)' section below.

### Features
1. **Saving agentic chat profiles**: Simplify client requests, including configurations related to prompting and model settings such as temperature, top-p, system messages, and more.
2. **Chat completions**: Includes streaming via a SignalR web socket or through SSE.
3. **Automatic load balancing**: Retry logic and other resiliency considerations.
4. **Logging**: Via Application Insights.
5. **RAG database creation**: Document ingestion and utilization across AGI service providers. Currently, only Azure AI is supported, but an interface is provided for easy extensibility.
6. **Mixing and matching models**: Image generation models and chat completion models with any AGI provider, including custom models deployed in Azure AI Studio.
7. **Tool execution**: Either returning the tool arguments to the client or executing them against an external API and returning the result of that request.
8. **Conversation history**: Saving and loading.
9. **Support for multiple AGI providers**: Current providers include Azure AI, OpenAI, and Anthropic.
10. **Model recursion**: Use multiple models in a single conversation, dialogues between models, or passing off complex processes to more specialized agent profiles and models.
11. **Front-end application**: For interacting with the API and as a template for building custom applications.
12. **Testing utilities**: Including unit tests, stress tests, and an AI competency testing module.

### Setup - TO DO
1. Prerequisites: List any prerequisites needed to run the project.
2. Installation: Step-by-step instructions on how to install the project.

### Usage - TO DO
Provide examples and instructions on how to use the project.

### API Reference - TO DO
List and describe the available API endpoints, including request and response formats.

### Contributing
Contributions are welcome! Please follow the steps below to contribute to the project:
1. (Optional) If you would like to ensure your changes will be merged, please open an issue to determine if your desired changes align with the project's goals, or choose from an existing issue. Otherwise, feel free to skip this step.
2. Fork the project, create a new branch, and make your changes. 
3. Ensure that any changes include relevant unit tests and IntelliSense documentation, or update existing tests as needed.
4. Push your changes to your fork, and raise a pull request.

If for whatever reason we miss your request, please feel free to notify us on the pull request itself, at the email provided in the 'Contact' section, or any other method you have at your disposal.

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