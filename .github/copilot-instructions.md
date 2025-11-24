# GPT Realtime Voice Assistant - AI Agent Instructions

## Architecture Overview

This is a full-stack **voice assistant** using OpenAI's GPT-4 Realtime API via **Azure OpenAI WebRTC**. The backend (.NET 9) acts as a **session token provider** that configures realtime sessions with OpenAI, while the frontend (Angular 18) handles **WebRTC peer connections** for audio streaming.

**Critical distinction**: This is NOT a WebSocket proxy architecture. The backend creates session tokens with pre-configured settings, and the frontend establishes direct WebRTC connections to Azure OpenAI's realtime endpoints.

### Component Boundaries

```
Frontend (Angular)        Backend (.NET)              Azure OpenAI
─────────────────         ──────────────              ────────────
HTTP GET /api/token  →    Generate session token  →   POST /realtimeapi/sessions
                                                       Returns client_secret + webrtc_url
Receive token        ←    Return SessionToken
                          (clientSecret, realtimeUrl,
                           expiresAt)

WebRTC SDP Offer     →────────────────────────────→   Azure WebRTC endpoint
                                                       (wss://{region}.realtimeapi-preview.ai.azure.com)
Audio streams        ←────────────────────────────→   Direct connection (no proxy)
```

**Backend responsibilities**: Session configuration (model, voice, system instructions, turn detection, audio formats), token generation, CORS policy.  
**Frontend responsibilities**: WebRTC peer connection setup, microphone capture, audio playback, data channel message handling.

## Development Workflows

### Local Development

```powershell
# Backend (runs on port 5000)
cd backend
dotnet restore
dotnet run

# Frontend (runs on port 4200)  
cd frontend
npm install
npm start
```

### Docker Development (Recommended)

```powershell
docker compose up --build
# Frontend: http://localhost:4200
# Backend: http://localhost:5000
```

**Important**: Docker Compose uses environment variables from `.env` file. Required variables:
- `OPENAI_API_KEY`: Azure OpenAI API key
- `OPENAI_BASE_URL`: Azure OpenAI endpoint (e.g., `https://your-resource.openai.azure.com`)
- `OPENAI_MODEL`: Model name (e.g., `gpt-4o-realtime-preview`)
- `OPENAI_VOICE`: Voice option (`alloy`, `ash`, `ballad`, `coral`, `echo`, `sage`, `shimmer`, `verse`, `marin`, `cedar`)

### Configuration Patterns

**Backend configuration hierarchy** (priority: Environment Variables > appsettings.json):
- `OpenAI__ApiKey`, `OpenAI__BaseUrl`, `OpenAI__Model`, `OpenAI__Voice`, `OpenAI__SystemInstructions`, `OpenAI__Region`
- `appsettings.json` contains defaults and example system instructions
- All session parameters are configured in `TokenService.CreateSessionConfiguration()` - **do not replicate in frontend**

**Frontend environment configuration**:
- `src/environments/environment.ts` (dev): `apiUrl: 'http://localhost:5000'`
- `src/environments/environment.prod.ts` (prod): Update for production backend URL

## Critical Implementation Details

### Session Token Flow (backend/Services/SpeachService.cs)

The `TokenService.CreateSessionTokenAsync()` method:
1. Validates `OpenAISettings` (BaseUrl, ApiKey required)
2. Builds Azure OpenAI session endpoint: `{baseUrl}/realtimeapi/sessions?api-version=2025-04-01-preview`
3. POSTs session configuration with headers: `api-key`, `OpenAI-Beta: realtime=v1`
4. Session config includes: model, voice, modalities, instructions, turn detection (server_vad), audio formats (pcm16)
5. Parses response for `client_secret.value` (Azure nested format) and calculates `expiresAt`
6. Returns `SessionToken` with `ClientSecret`, `ExpiresAt`, `RealtimeUrl` (region-specific WebRTC URL)

**Why this matters**: Session configuration is **fully backend-controlled**. Frontend receives a pre-configured token and connects directly to Azure. Do not add session.update calls in frontend.

### WebRTC Connection (frontend/src/app/services/realtime.service.ts)

The `RealtimeService.connect()` method:
1. Creates `RTCPeerConnection` with STUN server
2. Creates data channel `oai-events` for bidirectional JSON messages
3. Gets microphone access via `navigator.mediaDevices.getUserMedia({ audio: true })`
4. Adds audio track to peer connection (initially disabled/muted)
5. Creates SDP offer and sets local description
6. POSTs offer to `{realtimeUrl}?model=gpt-4o-realtime-preview` with `Authorization: Bearer {clientSecret}` header
7. Sets remote description from response
8. Waits for `session.created` event, then triggers initial greeting with `response.create`

**Audio capture control**:
- `startAudioCapture()`: Enables microphone track (`track.enabled = true`)
- `stopAudioCapture()`: Disables track and sends `input_audio_buffer.commit` + `response.create`

**Message handling**: Data channel messages are parsed as JSON and emitted via RxJS Subject. Component subscribes to handle events like `response.audio.delta`, `input_audio_buffer.speech_started`, etc.

### Turn Detection & Conversation Flow

This app uses **server-side VAD (Voice Activity Detection)**:
- `TurnDetectionConfig`: `type: "server_vad"`, `threshold: 0.5`, `prefixPaddingMs: 300`, `silenceDurationMs: 200`
- OpenAI detects when user stops speaking and automatically commits audio buffer
- Frontend listens for `input_audio_buffer.speech_stopped` to know when to expect response

**Initial greeting pattern** (voice-assistant.component.ts):
1. Connection established → `session.created` event received
2. Frontend sends `response.create` (no input) to trigger assistant greeting
3. Assistant speaks first based on system instructions
4. After `response.done`, frontend enables microphone for user input

### CORS Configuration (backend/Program.cs)

```csharp
policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials();
```

**When deploying**: Update CORS origins to include production frontend URLs.

## Project-Specific Conventions

### Audio Format Standards
- **Input/Output**: PCM16 (16-bit linear PCM at 24kHz sample rate)
- Frontend converts browser audio to PCM16, backend specifies format in session config
- Do not change audio formats without updating both frontend audio processing and backend session config

### Error Handling Patterns
- Backend: Use ILogger for structured logging; throw `InvalidOperationException` with descriptive messages
- Frontend: Subscribe error handlers log to console and update `statusMessage` for user feedback
- WebRTC connection failures: Check `peerConnection.iceConnectionState` and `eventsChannel.readyState`

### Naming Conventions
- Backend: PascalCase for classes/methods, camelCase for local variables
- Frontend: camelCase for properties/methods, kebab-case for file names
- WebSocket message types: Use exact OpenAI event names (e.g., `session.created`, `response.audio.delta`)

### File Structure Patterns
- Backend services: Interface in `Services/ITokenService.cs`, implementation in `Services/TokenService.cs`
- Backend models: Plain POSOs in `Models/` with JSON property attributes
- Frontend components: Component class + HTML + CSS in `components/` subdirectories
- Frontend services: Injectable services in `services/` with RxJS observables

## Common Gotchas

1. **Session configuration location**: Do NOT send `session.update` from frontend. Backend pre-configures everything in token creation.
2. **Azure URL format**: Azure OpenAI base URLs should NOT include `/openai/v1/` suffix - backend adds correct paths.
3. **Client secret parsing**: Azure returns `client_secret.value` (nested), not `client_secret` (flat) - handle both formats.
4. **Microphone timing**: Enable microphone AFTER assistant's initial greeting completes to avoid interrupting it.
5. **Model parameter in WebRTC URL**: Must append `?model=gpt-4o-realtime-preview` to WebRTC URL for Azure.
6. **Docker ports**: Backend exposes 8080 internally but maps to 5000 externally; frontend uses nginx on port 80 mapped to 4200.
7. **System instructions**: Configure in `appsettings.json` or `OpenAI__SystemInstructions` env var - affects assistant behavior.

## Testing & Debugging

- **Backend logs**: `dotnet run` shows ILogger output; check for session creation errors
- **Frontend console**: Open browser DevTools to see realtime message logs and WebRTC state changes
- **Docker logs**: `docker compose logs backend` / `docker compose logs frontend`
- **Audio issues**: Verify browser permissions, check `remoteAudioElement.srcObject` assignment, test with `ontrack` event logging
- **Connection issues**: Inspect network tab for token request (should return 200), check SDP offer POST to WebRTC URL

## Deployment Considerations

- **Azure Web Apps**: Enable WebSocket support in App Service configuration
- **Environment variables**: Use Azure App Settings or Key Vault for API keys
- **HTTPS**: Required in production; update CORS and environment URLs to use https://
- **Region selection**: Set `OpenAI__Region` to match your Azure OpenAI resource (e.g., `swedencentral`, `eastus`)
- **Container registries**: Push Docker images to Azure Container Registry for AKS/ACI deployments

## Key Files to Reference

- `backend/Services/SpeachService.cs` - Session token generation and configuration logic
- `frontend/src/app/services/realtime.service.ts` - WebRTC connection and audio handling
- `backend/Program.cs` - DI configuration, CORS, OpenAI client setup
- `docker-compose.yml` - Multi-container orchestration with environment variables
- `ARCHITECTURE.md` - Detailed system architecture diagrams and data flows
