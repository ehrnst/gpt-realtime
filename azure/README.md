# Azure Deployment

This directory contains Azure deployment templates and configurations for the GPT Realtime Voice Assistant.

## Deployment Options

### Option 1: Azure Web Apps

Deploy both frontend and backend as Azure Web Apps.

### Option 2: Azure Container Instances

Deploy containerized applications to ACI.

### Option 3: Azure Kubernetes Service (AKS)

Deploy to a Kubernetes cluster for scalability.

## Environment Variables

- OpenAI__ApiKey: Your OpenAI API key
- OpenAI__BaseUrl: Optional Azure OpenAI endpoint
- OpenAI__Model: Model name
- OpenAI__Voice: Voice selection

## Security

Use Azure Key Vault for secrets management.
