# Azure Function-Based AI Orchestrator Solution

This repository contains an Azure Function solution for orchestrating AI services to process and analyze data using Azure AI Services. The system processes data stored in Azure Blob Storage, interacts with Azure Cognitive Services, and integrates with Azure Cosmos DB and Azure AI Search for storage and retrieval.

## Features

- **Data Processing**: Uses Azure Cognitive Services to analyze and process data.
- **Storage Integration**: Interacts with Azure Blob Storage for data storage and retrieval.
- **Database Integration**: Uses Azure Cosmos DB for structured data storage.
- **Search Integration**: Uploads processed data to Azure AI Search for efficient indexing and retrieval.

## Components

1. **Azure Functions**
   - **HTTP Trigger Function**: Manually triggered for specific processing tasks.

2. **Services**
   - **BlobStorageService**: Manages interactions with Azure Blob Storage.
   - **CosmosDbService**: Manages interactions with Azure Cosmos DB.
   - **CognitiveSearchService**: Manages interactions with Azure AI Search.
   - **OpenIAServices**: Interacts with Azure OpenAI for advanced AI processing.

3. **Contracts**
   - **ProcessResponse**: Defines the structure of the response from processing functions.
   - **RequestBlobBody**: Defines the structure of the request body for blob processing.

## How It Works

###	OpenAI Chat Completion:
•	Initiates a chat completion request to OpenAI using the InitialTemplateSystem and the user's query from requestBody.
•	The response from OpenAI is stored in the query variable.
###	OpenAI Embedding:
•	Then requests an embedding for the user's query from OpenAI.
•	The embedding is used to perform a search using the ICognitiveSearchService.
###	Cognitive Search:
•	The search service uses the query and embedding to find relevant documents.
•	The content of the found document is stored in the documentContent variable.
###	Retrieve Conversation History:
•	Retrieves the conversation history from Cosmos DB using the ICosmosDbService.
•	If no conversation is found, a default message is set. Otherwise, the chat history is constructed from the retrieved conversation.
###	Final Prompt Construction:
•	The final prompt for OpenAI is constructed by replacing placeholders in FinalTemplateUser with the user's query, chat history, and document content.
###	Final OpenAI Chat Completion:
•	Another chat completion request is made to OpenAI using the FinalTemplateSystem and the constructed final prompt.
•	The response is deserialized into a JsonElement.
###	Update Conversation:
•	The conversation is updated with the new chats (user's query and system's response).
•	If the conversation does not exist, it is created. Otherwise, the existing conversation is updated.

## Prerequisites

- **Azure Account**: An active Azure subscription.
- **Azure Resources**:
  - Azure Blob Storage
  - Azure Cosmos DB
  - Azure Cognitive Services
  - Azure AI Search
  - Azure OpenAI Service
  - Azure Key Vault for secure secret management
- **Development Environment**:
  - .NET 8 SDK
  - Visual Studio 2022

## Getting Started

### 1. Clone the Repository

git clone https://github.com/nelsonApraez/aiorchestrator.git

### 2. Install Dependencies

Ensure you have the .NET 8 SDK installed. Then, restore dependencies:

dotnet restore

### 3. Set Up Azure Resources

#### Azure Blob Storage

- Create a storage account with a container named `container-docs`.
- Inside `container-docs`, create folders for different stages of processing (e.g., `unprocessed-docs`, `processed-docs`).

#### Azure Cognitive Services

- Create an AI Services Multi-Service Account for document analysis and text analytics.

#### Azure OpenAI Service

- Set up the embedding model for vector representation of document content.

#### Azure AI Search

- Create an index to store enriched document chunks.

#### Azure Key Vault

- Store necessary API keys and connection strings securely.

### 4. Environment Variables and Secrets Configuration

Configure the following environment variables in the Azure Function App Settings:

| Variable Name | Description |
|---------------|-------------|
AZURE_AI_SEARCH_ENDPOINT |	The endpoint URL for Azure AI Search.
AZURE_AI_SEARCH_INDEX_NAME |	The index name for Azure AI Search.
AZURE_AI_SEARCH_KEY |	The primary key for Azure AI Search.
AZURE_STORAGE_CONTAINER |	The name of the Azure Blob Storage container.
AZURE_STORAGE_EXPIRY_MINUTES |	The expiry time in minutes for Azure Storage operations.
AZURE_STORAGE_FOLDER_TO_DOWNLOAD |	The folder in Azure Storage to download processed documents.
AZURE_STORAGE_FOLDER_TO_UPLOAD |	The folder in Azure Storage to upload unprocessed documents.
AZURE_STORAGE_KEY |	The connection string or key for Azure Storage.
AZURE_WEBJOBS_STORAGE |	The connection string for Azure WebJobs Storage.
COSMOSDB_AMOUNT_HISTORICAL_CHAT_TO_RECOVER |	The number of historical chats to recover from Cosmos DB.
COSMOSDB_CONTAINER_NAME |	The container name in Cosmos DB for conversations.
COSMOSDB_DATABASE_NAME |	The database name for Cosmos DB.
AZURE_COSMOSDB_ENDPOINT |	The endpoint URL for Azure Cosmos DB.
AZURE_COSMOSDB_KEY |	The primary key for Azure Cosmos DB.
KEYVAULT_URI |	The Azure Key Vault URI.
OPENAI_ENDPOINT |	The endpoint URL for Azure OpenAI Service.
OPENAI_KEY |	The primary key for Azure OpenAI Service.
OPENAI_MAX_TOKENS |	The maximum number of tokens for Azure OpenAI Service.
OPENAI_MODEL |	The model name for Azure OpenAI Service.
OPENAI_MODEL_EMBEDDING |	The model used for generating embeddings in Azure OpenAI Service.
PROMPT_FINAL_TEMPLATE_SYSTEM |	The final system prompt template for the AI assistant.
PROMPT_FINAL_TEMPLATE_USER |	The final user prompt template for generating AI assistant responses.
PROMPT_INITIAL_TEMPLATE_SYSTEM |	The initial system prompt template for generating search queries for follow-up questions.
SECURITY_KEY |	The security key for function orchestration token authentication.


Store the following secrets in Azure Key Vault:

| Secret Name | Description |
|-------------|-------------|
dev-kvs-project-genai-aisearch-primary-key |	Primary key for Azure AI Search.
dev-kvs-project-genai-aiservices-primary-key |	Primary key for Azure Cognitive Services.
dev-kvs-project-genai-comos-primary-key |	Primary key for Azure Cosmos DB.
dev-kvs-project-genai-funct-orch-token-auth |	Token for authenticating function orchestration.
dev-kvs-project-genai-openai-primary-key |	Primary API key for Azure OpenAI Service.
dev-kvs-project-genai-st-connection-string |	Connection string for Azure Blob Storage.

### 5. Deploy the Azure Function

#### Option 1: Azure Functions Core Tools

func azure functionapp publish <your-function-app-name>

#### Option 2: Azure CLI

az functionapp deployment source config-zip 
--resource-group <your-resource-group> 
--name <your-function-app-name> 
--src <path-to-your-zip-file>

#### Option 3: Visual Studio

1. Open the Azure Functions project in Visual Studio.
2. Right-click on the project in Solution Explorer and select Publish.
3. Choose Azure as the target and select Azure Function App.
4. Sign in to your Azure account and select the correct Subscription.
5. Choose an existing Function App or create a new one.
6. Click Next, review the settings, and click Finish.
7. Click Publish to deploy the function.

### 6. Test the Solution

1. Upload a file using the FileUpload endpoint and wait for it to be processed.
2. Ask questions via FunctionAIOrchestrator endpoint, about the document you previously uploaded

## Contributing

Contributions are welcome! Please submit a pull request or open an issue for discussion.

