import os

# Define the path to the IntelligenceHub.Host folder
file_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'IntelligenceHub.Host', '.env')

# Ensure the directory exists
directory = os.path.dirname(file_path)
if not os.path.exists(directory):
    raise FileNotFoundError(f"The directory {directory} does not exist.")

# Initialize a dictionary to store the environment variables
env_variables = {}

# Hardcoded values for logging and allowed hosts
env_variables['Logging__LogLevel__Default'] = 'Information'
env_variables['Logging__LogLevel__Microsoft.AspNetCore'] = 'Warning'
env_variables['Logging__LogLevel__Microsoft.AspNetCore.SignalR'] = 'Information'
env_variables['AllowedHosts'] = '*'
env_variables['Azure__SignalR__Enabled'] = 'true'

# Prompt for user input for each setting
env_variables['Settings__DbConnectionString'] = input("Enter the database connection string: ")
env_variables['AuthSettings__Domain'] = input("Enter the auth domain: ")
env_variables['AuthSettings__Audience'] = input("Enter the auth audience: ")
env_variables['AppInsightSettings__ConnectionString'] = input("Enter the Application Insights connection string: ")

# Initialize lists for AGIClientSettings
agi_client_endpoints = []
agi_client_keys = []

# Prompt for the first AGI client service endpoint and key
agi_client_endpoints.append(input("Enter the AGI client service endpoint: "))
agi_client_keys.append(input("Enter the AGI client service key: "))

# Ask if the user wants to add more AGI client services
while True:
    add_more = input("Would you like to add another AGI client service? (y/n): ").strip().lower()
    if add_more == 'y':
        agi_client_endpoints.append(input("Enter the AGI client service endpoint: "))
        agi_client_keys.append(input("Enter the AGI client service key: "))
    elif add_more == 'n':
        break
    else:
        print("Please enter 'y' for yes or 'n' for no.")

# Add AGI client services to the dictionary
for index, (endpoint, key) in enumerate(zip(agi_client_endpoints, agi_client_keys)):
    env_variables[f'AGIClientSettings__Services__{index}__Endpoint'] = endpoint
    env_variables[f'AGIClientSettings__Services__{index}__Key'] = key

# Continue prompting for other settings
env_variables['AGIClientSettings__SearchServiceCompletionServiceEndpoint'] = input(
    "Enter the search service completion service endpoint: "
)
env_variables['AGIClientSettings__SearchServiceCompletionServiceKey'] = input(
    "Enter the search service completion service key: "
)
env_variables['SearchServiceClientSettings__Endpoint'] = input("Enter the search service client endpoint: ")
env_variables['SearchServiceClientSettings__Key'] = input("Enter the search service client key: ")

print("NOTE: These values may not be required for all environments. Enter blank values to skip.")
env_variables['AzureAd__Instance'] = input("Enter the Azure AD instance: ")
env_variables['AzureAd__Domain'] = input("Enter the Azure AD domain: ")
env_variables['AzureAd__TenantId'] = input("Enter the Azure AD tenant ID: ")
env_variables['AzureAd__ClientId'] = input("Enter the Azure AD client ID: ")
env_variables['AzureAd__CallbackPath'] = input("Enter the Azure AD callback path: ")
env_variables['AzureAd__Scopes'] = input("Enter the Azure AD scopes: ")

# Define the path up one level and then down into the IntelligenceHub.Host folder
file_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'IntelligenceHub.Host', '.env')

# Write the environment variables to a .env file
with open(file_path, 'w') as env_file:
    for key, value in env_variables.items():
        if value.strip():  # Skip empty values
            env_file.write(f"{key}={value}\n")

print("Environment variables have been saved to .env file.")
