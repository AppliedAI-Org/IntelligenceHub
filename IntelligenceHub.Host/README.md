# Intelligence Hub

## Please note: This readme is currently in need of a major overhaul

### Project Summary: 
This repo consists of a custom API designed to simplify consumption of OpenAI, or a local mixtral model, assuming its been fine tuned to use function calling. Currently the most interesting portion of the project is my implementation of function calling, although I have many more additions planned for the future. You can read more about the API's function calling and future feature roadmap in their respective sections. 

The ultimate goal of the project is to create a significantly simplified way of interacting with popular AI models by providing a way to save model configurations, update those configurations, ingest external data through RAG pipelines and internet queries, and seamlessly interact with a variety of media.

### Instillation and set up:
The easiest way to set up the projcet is to clone the repository into Visual Studio. After cloning the repository, please be sure to add your own API token to the "openAIKey" property in appsettings.json file. Incase you aren't experienced with Visual Studio, you may also need to open the solution file if the project folder was opened.

### Unique Function Calling Implementation
The default configuration for this API is able to call multiple additional functions provided the names are passed as part of the post arguments. After I have the configurations route complete, the post request will look more like a prompt and the name of the preset configuration like in this example below:
```
{
  "prompt": "please turn on my lights for me.",
  "configuration": "default"
}
```

This default endpoint will be used in the instance that configuration is null, or set to "default" like in the case below, and will consist of a series of function names that correspond to the other possible configurations, which can also be called directly. This default layer works by passing the user input to more specialized configurations, which then can use their own fine tuning, RAG databases, or system prompting to extract relevant details and call functions, find and return relevant data, or ask follow up questions if more data is needed. This allows complicated behaviors to be accomplished from a single user prompt sent to a single unified endpoint, vastly simplifying the process of utilizing generative AI models.

You can use the below request targeting the "completion" route to test this out. It should return the names of two of the function names included in the request body, which can either trigger a follow up question from a more specialized AI configuration as described above, or trigger an action from another application.
```
{
  "prompt": "hello, can you please turn on my lights for me? Also, write an email to the land lord asking if the can investigate the flickering bulb in the bedroom.",
  "functionEndpoint": "string",
  "functionNames": {
    "callHomeAutomationModel": "A function that should be used when the user wants to trigger, configure, or inquire about their home automations, such as lighting orchestrations, heating/cooling, etc.",
    "callEmailOrTextWriterModel": "A function that should be used when the user wants to write and send an email or a text message.",
    "callOrderPlacementModel": "A function that should be used when the user wants to place an order online. This can be food, amazon orders, or any other transaction else that can be done online."
  }
}
```

### Feature Roadmap
1. SQL database integration and model configuration routes to create, update, and delete request presets to adjust settings for the completion requests like top p, temperature, RAG settings, function calling settings, and more
2. Handling of follow up completion requests internally
3. Create a Vector Pipeline
4. Internet Search/Scraping capabilities
5. RAG Database configuration and ingestion routes
6. Fine tuning routes
