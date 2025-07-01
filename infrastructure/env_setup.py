import os
import json
import sys
import re

# This is a python script designed to generate a development appsettings file for the IntelligenceHub.Host project.
#
# Note: This script is designed to first check for the presence of environment variables and use them to populate 
# the appsettings file. If no environment variables are found, the script will prompt the user to enter these
# values. It can also be ran in a production setting to guarantee that all required environment variables are set,
# although additional CI\CD configurations is required to push these variables to a production environment.

base_dir = os.path.dirname(__file__)
template_path = os.path.join(base_dir, '..', 'IntelligenceHub.Host', 'appsettings.Template.json')
output_path = os.path.join(base_dir, '..', 'IntelligenceHub.Host', 'appsettings.Development.json')

# Ensure the template file exists
if not os.path.exists(template_path):
    raise FileNotFoundError(f"The template file {template_path} does not exist.")

# Load the template JSON
with open(template_path, 'r') as template_file:
    appsettings_template = json.load(template_file)

# Enhanced token replacement function
def replace_tokens(obj, replacements):
    # If the object is a list, check if it contains a single token indicating an array replacement.
    if isinstance(obj, list):
        if len(obj) == 1 and isinstance(obj[0], str):
            token_match = re.match(r"^__(.+)_0__$", obj[0])
            if token_match:
                base_token = token_match.group(1)
                if base_token in replacements and isinstance(replacements[base_token], list):
                    return replacements[base_token]
        # Otherwise, process each item individually.
        return [replace_tokens(item, replacements) for item in obj]
    elif isinstance(obj, dict):
        return {key: replace_tokens(value, replacements) for key, value in obj.items()}
    elif isinstance(obj, str) and obj.startswith("__") and obj.endswith("__"):
        token = obj[2:-2]  # Extract token name without the delimiters.
        # If token has a pattern ending with _<number>, check if a list exists in replacements
        token_match = re.match(r"^(.+)_\d+$", token)
        if token_match:
            base_token = token_match.group(1)
            if base_token in replacements and isinstance(replacements[base_token], list):
                return replacements[base_token]
        return replacements.get(token, obj)
    else:
        return obj

# List of required tokens (from environment or manual input)
required_env_vars = [
    "Logging_LogLevel_Default",
    "Logging_LogLevel_Microsoft_AspNetCore",
    "Logging_LogLevel_Microsoft_AspNetCore_SignalR",
    "AllowedHosts",
    "Azure_SignalR_Enabled",
    "Settings_DbConnectionString",
    "AppInsightSettings_ConnectionString",
    "AGIClientSettings_AzureOpenAIServices_0_Endpoint",
    "AGIClientSettings_AzureOpenAIServices_0_Key",
    "AGIClientSettings_OpenAIServices_0_Endpoint",
    "AGIClientSettings_OpenAIServices_0_Key",
    "AGIClientSettings_AnthropicServices_0_Endpoint",
    "AGIClientSettings_AnthropicServices_0_Key",
    "AGIClientSettings_SearchServiceCompletionServiceEndpoint",
    "AGIClientSettings_SearchServiceCompletionServiceKey",
    "SearchServiceClientSettings_Endpoint",
    "SearchServiceClientSettings_Key",
    "Settings_DefaultImageHost"  # token for DefaultImageGenHost
]

# Check the current environment
current_environment = os.getenv("ASPNETCORE_ENVIRONMENT", "Development")

if current_environment.lower() == "production":
    missing_vars = [var for var in required_env_vars if var not in os.environ]
    if missing_vars:
        print(f"Production environment detected but the following environment variables are missing: {', '.join(missing_vars)}. Exiting with failure.")
        sys.exit(1)
    else:
        print("Production environment detected with all required environment variables. Exiting without changes.")
        sys.exit(0)

# Build the replacements dictionary from environment variables
replacements = {}
if os.environ:
    print("Environment variables detected. Using them to populate the appsettings file.")
    for key, value in os.environ.items():
        token = key.replace("__", "_")
        if token in required_env_vars:
            replacements[token] = value
    print("Replacements dictionary populated with environment variables:", replacements)
else:
    print("No environment variables detected. Proceeding with manual input.")

# Prompt the user for any missing required tokens
if len(replacements) != len(required_env_vars):
    print("Enter the values for the following tokens. Leave blank to skip.")
    for token in required_env_vars:
        if token not in replacements:
            user_input = input(f"{token}: ").strip()
            if user_input:
                replacements[token] = user_input

# Prompt for additional services (unchanged from your original approach)
additional_services = {
    "AzureOpenAIServices": [],
    "OpenAIServices": [],
    "AnthropicServices": []
}
for service_type in additional_services.keys():
    while True:
        add_more = input(f"Would you like to add another {service_type[:-1]} service? (y/n): ").strip().lower()
        if add_more == 'y':
            endpoint = input(f"Enter the {service_type[:-1]} service endpoint: ").strip()
            key = input(f"Enter the {service_type[:-1]} service key: ").strip()
            if endpoint and key:
                additional_services[service_type].append({"Endpoint": endpoint, "Key": key})
        elif add_more == 'n':
            break
        else:
            print("Please enter 'y' for yes or 'n' for no.")
for service_type, services in additional_services.items():
    if services:
        replacements[f"AGIClientSettings_{service_type}"] = services

# Prompt for multiple ValidOrigins entries
if "Settings_ValidOrigins" not in replacements:
    valid_origins = []
    print("Enter values for ValidOrigins (leave blank when finished):")
    while True:
        origin = input("  ValidOrigin: ").strip()
        if not origin:
            break
        valid_origins.append(origin)
    replacements["Settings_ValidOrigins"] = valid_origins

# Prompt for multiple ValidAGIModels entries
if "Settings_ValidAGIModels" not in replacements:
    valid_models = []
    print("Enter values for ValidAGIModels (leave blank when finished):")
    while True:
        model = input("  ValidAGIModel: ").strip()
        if not model:
            break
        valid_models.append(model)
    replacements["Settings_ValidAGIModels"] = valid_models

# Replace tokens in the template
updated_appsettings = replace_tokens(appsettings_template, replacements)

# If an existing development file exists, merge in default values that are already provided
if os.path.exists(output_path):
    with open(output_path, 'r') as existing_file:
        existing_appsettings = json.load(existing_file)
    # List of default keys in the Settings section that have literal defaults in the template.
    default_keys = [
        "AGIClientMaxRetries",
        "AGIClientMaxJitter",
        "ToolClientMaxRetries",
        "ToolClientInitialDelay",
        "MaxDbRetries",
        "MaxDbRetryDelay",
        "MaxCircuitBreakerFailures",
        "CircuitBreakerBreakDuration"
    ]
    if "Settings" in updated_appsettings:
        for key in default_keys:
            # If the key already exists in the existing file, preserve its value.
            if key in existing_appsettings.get("Settings", {}):
                updated_appsettings["Settings"][key] = existing_appsettings["Settings"][key]

# Print the updated appsettings for debugging
print("Updated appsettings JSON:", json.dumps(updated_appsettings, indent=4))

# Write the updated JSON to a new file
with open(output_path, 'w') as output_file:
    json.dump(updated_appsettings, output_file, indent=4)

print(f"Development appsettings file has been created at {output_path}.")
