# Configuration Guide

This document provides guidance on configuring the application for your environment.

## Environment Variables

The following environment variables need to be configured:

### Azure Key Vault
- `KEYVAULT_URI`: Your Azure Key Vault URI (e.g., https://your-keyvault.vault.azure.net/)

### Azure AI Search
- `AISEARCH_ENDPOINT`: Your Azure AI Search endpoint
- `AISEARCH_INDEX`: Your search index name
- `AISEARCH_KEY_NAME`: Key name in Key Vault for AI Search

### Azure OpenAI
- `OPENAI_ENDPOINT`: Your Azure OpenAI endpoint
- `OPENAI_KEY_NAME`: Key name in Key Vault for OpenAI
- `OPENAI_MODEL`: The GPT model to use
- `OPENAI_EMBEDDING_MODEL`: The embedding model to use

### Azure Cosmos DB
- `COSMOS_ENDPOINT`: Your Cosmos DB endpoint
- `COSMOS_DATABASE`: Database name
- `COSMOS_CONTAINER`: Container name
- `COSMOS_KEY_NAME`: Key name in Key Vault for Cosmos DB

### Azure Storage
- `AZURE_STORAGE_CONTAINER`: Blob storage container name
- `AZURE_STORAGE_UPFOLDER`: Folder for unprocessed files
- `AZURE_STORAGE_DOWNFOLDER`: Folder for processed files

### Security
- Configure authentication tokens and keys according to your security requirements

## Local Development

1. Copy `local.settings.json.template` to `local.settings.json`
2. Update the configuration values with your Azure resource information
3. Ensure your development environment has access to the configured Azure resources

## Deployment

Update the `env_settings_dev.json` file with your production environment configuration before deploying.
