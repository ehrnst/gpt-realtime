# GPT Realtime Voice Assistant

A full-stack voice assistant application that uses OpenAI's GPT-4 Realtime API to provide voice-based interactions. Built with Angular for the frontend and .NET Core for the backend.

## Architecture

- **Frontend**: Angular 18 application with WebSocket-based real-time audio streaming
- **Backend**: .NET Core 9 Web API with OpenAI SDK integration
- **Security**: Short-lived token authentication (5-minute expiry)
- **Deployment**: Docker containers ready for Azure Web Apps, Azure Functions, and Kubernetes

## Features

- ✅ Real-time voice conversations with GPT-4
- ✅ Secure token-based authentication
- ✅ WebSocket communication for low-latency audio streaming
- ✅ Browser-based audio capture and playback
- ✅ Activity logging and status monitoring
- ✅ Docker containerization for easy deployment
- ✅ Support for both OpenAI and Azure OpenAI endpoints

## Prerequisites

- Node.js 20.x or later
- .NET 9.0 SDK or later
- Docker (optional, for containerized deployment)
- OpenAI API key with access to GPT-4 Realtime API

## Project Structure

```
gpt-realtime/
├── backend/               # .NET Core Web API
│   ├── Controllers/       # API endpoints
│   ├── Models/           # Data models
│   ├── Services/         # Business logic
│   ├── Dockerfile        # Backend container configuration
│   └── appsettings.json  # Configuration
├── frontend/             # Angular application
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/  # UI components
│   │   │   └── services/    # API and WebSocket services
│   │   └── environments/    # Environment configs
│   ├── Dockerfile        # Frontend container configuration
│   └── nginx.conf        # Production web server config
├── docker-compose.yml    # Multi-container orchestration
└── README.md
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/ehrnst/gpt-realtime.git
cd gpt-realtime
```

### 2. Configure API Keys

Create a `.env` file in the root directory (use `.env.example` as template):

```bash
cp .env.example .env
```

Edit `.env` and add your OpenAI API key:

```env
OPENAI_API_KEY=sk-your-api-key-here
OPENAI_BASE_URL=  # Optional: for Azure OpenAI, e.g., https://your-resource.openai.azure.com
OPENAI_MODEL=gpt-4o-realtime-preview-2024-10-01
OPENAI_VOICE=alloy  # Options: alloy, ash, ballad, coral, echo, sage, shimmer, verse, marin, cedar
```

For the backend, also update `backend/appsettings.json` or use environment variables.

### 3. Run with Docker Compose (Recommended)

```bash
docker-compose up --build
```

The application will be available at:
- Frontend: http://localhost:4200
- Backend API: http://localhost:5000

### 4. Run Locally (Development)

#### Backend

```bash
cd backend
dotnet restore
dotnet run
```

The API will start at http://localhost:5000

#### Frontend

```bash
cd frontend
npm install
npm start
```

The Angular app will start at http://localhost:4200

## Usage

1. Open the application in a web browser
2. Click "Connect" to establish a connection and get a session token
3. Click "Start Recording" to begin speaking
4. Speak your message - the AI will respond with voice
5. Click "Stop Recording" when finished
6. View the activity log for conversation history

## API Endpoints

### Backend API

- `GET /api/token` - Generate a short-lived session token
- `GET /api/realtime/ws?token={token}` - WebSocket endpoint for realtime communication

## Configuration

### Backend Configuration

Edit `backend/appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key",
    "BaseUrl": "",  // Optional: Azure OpenAI endpoint
    "Model": "gpt-4o-realtime-preview-2024-10-01",
    "Voice": "alloy"
  }
}
```

### Frontend Configuration

Edit `frontend/src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000'
};
```

## Deployment

### Azure Web Apps

1. **Backend**: Deploy as Azure Web App for .NET
   - Use the provided Dockerfile
   - Set environment variables in Azure Portal

2. **Frontend**: Deploy as Azure Static Web App or Azure Web App
   - Build: `npm run build`
   - Deploy the `dist/frontend` folder

### Azure Functions

The backend can be adapted to run as Azure Functions for serverless deployment.

### Kubernetes

Use the provided Dockerfiles to create container images:

```bash
# Build images
docker build -t gpt-realtime-backend:latest ./backend
docker build -t gpt-realtime-frontend:latest ./frontend

# Push to container registry
docker tag gpt-realtime-backend:latest yourregistry.azurecr.io/gpt-realtime-backend:latest
docker push yourregistry.azurecr.io/gpt-realtime-backend:latest

docker tag gpt-realtime-frontend:latest yourregistry.azurecr.io/gpt-realtime-frontend:latest
docker push yourregistry.azurecr.io/gpt-realtime-frontend:latest
```

Create Kubernetes deployment manifests for both services.

## Security Considerations

- **Token Authentication**: Short-lived tokens (5 minutes) prevent unauthorized access
- **CORS**: Configured to only allow specific origins
- **HTTPS**: Use HTTPS in production
- **API Key Protection**: API keys are only stored on the backend, never exposed to frontend
- **Environment Variables**: Use secure methods to inject secrets in production

## Browser Compatibility

The application requires:
- WebSocket support
- Web Audio API
- MediaDevices API (for microphone access)

Tested on:
- Chrome/Edge 90+
- Firefox 88+
- Safari 14+

## Troubleshooting

### Microphone Access Denied

Ensure the browser has permission to access the microphone. In Chrome:
1. Click the lock icon in the address bar
2. Allow microphone access

### WebSocket Connection Failed

- Check that the backend is running
- Verify the API URL in environment configuration
- Check browser console for detailed error messages

### OpenAI API Errors

- Verify your API key is valid
- Ensure you have access to the Realtime API (beta feature)
- Check your API usage limits

## Development

### Build Backend

```bash
cd backend
dotnet build
```

### Build Frontend

```bash
cd frontend
npm run build
```

### Run Tests

```bash
# Backend tests
cd backend
dotnet test

# Frontend tests
cd frontend
npm test
```

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## Support

For issues and questions, please open an issue on GitHub.