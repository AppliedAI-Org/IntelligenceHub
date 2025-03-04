# The Intelligence Hub - A Powerful API Wrapper for AGI Services Designed to Simplify Consuming Common AI Services such as Chat Completions, RAG Databases, Image Generation, and More.

### Overview
The main goal of this project is to enable rapid setup and development for AI-powered applications. It is designed as a template for AI projects, providing a structured approach to organizing code, data, and documentation. The project includes a set of tools and utilities to assist with common tasks such as data preprocessing, model training, and deployment. It is built to be flexible and extensible, allowing developers to easily add new features and functionality.

In addition to simplifying requests to the currently supported AGI clients, the project also offers capabilities to save and load conversation history, execute tools (either returning the tool arguments to the client or executing them against an external API), create and utilize RAG databases, implement basic load balancing and retry policies, and more. For additional details, please refer to the 'Features' section. This repository also includes a front end application that demonstrates how to interact with the API, and can be used as a template for building custom applications.

AI clients are resolved using a custom client factory, which allows new clients to be added with ease by creating a new class that extends and implements the IAGIClient interface. This design also enables image generation from various clients, decoupling the image generation client from the API client used to generate these images.

Please see the 'Features' section below for more information on the project's capabilities.

### Features
- Feature 1: Saving agentic chat profiles to simplify client requests, including configurations related to prompting and model settings such as temperature, top-p, system messages, and more.
- Feature 2: Chat completions, including streaming via a SignalR web socket or through SSE.
- Feature 3: Automatic load balancing, retry logic, and other resiliency considerations.
- Feature 4: Logging via Application Insights.
- Feature 5: RAG database creation, document ingestion, and utilization across AGI service providers. Currently, only Azure AI is supported, but an interface is provided for easy extensibility.
- Feature 6: Mixing and matching image generation models and chat completion models with any AGI provider, including custom models deployed in Azure AI Studio.
- Feature 7: Tool execution, either returning the tool arguments to the client or executing them against an external API and returning the result of that request.
- Feature 8: Conversation history saving and loading.
- Feature 9: Support for multiple AGI providers. Current providers include Azure AI, OpenAI, and Anthropic.
- Feature 10: Model recursion, allowing for the use of multiple models in a single conversation, dialogues between models, or passing off complex processes to more specialized agent profiles and models.
- Feature 11: Front-end application for interacting with the API and as a template for building custom applications.
- Feature 12: Testing utilities, including unit tests, stress tests, and an AI competency testing module.

### Setup - TO DO
1. Prerequisites: List any prerequisites needed to run the project.
2. Installation: Step-by-step instructions on how to install the project.

### Usage - TO DO
Provide examples and instructions on how to use the project.

### API Reference - TO DO
List and describe the available API endpoints, including request and response formats.

### Contributing
Contributions are welcome! Please follow the steps below to contribute to the project:
- If you would like to ensure your changes will be merged, please open an issue to determine if your desired changes align with the project's goals, or choose from an existing issue. Otherwise, feel free to skip this step.
- Fork the project, create a new branch, and make your changes. Please ensure that any changes include relevant unit tests and IntelliSense documentation, or update existing tests as needed.
- Push your changes to your fork, and raise a pull request.

If for whatever reason we miss your request, please feel free to notify us on the pull request itself, at the email provided in the 'Contact' section, or any other method you have at your disposal.

### Contact
For any questions comments or concerns, please reach out to Applied.AI.Help@gmail.com, or open an issue in the repository.

### License
This project is licensed under the Elastic 2.0 - see the `LICENSE` file for more details.

### Acknowledgements
This project was developed by the Applied AI team, currently consisting of the following members: Jacob J. Thomas

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