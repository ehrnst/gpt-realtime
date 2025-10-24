# Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                          User Browser                            │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │           Angular Frontend (Port 4200)                      │ │
│  │  ┌──────────────┐  ┌─────────────┐  ┌──────────────────┐  │ │
│  │  │  Voice       │  │  Realtime   │  │  Token Service   │  │ │
│  │  │  Assistant   │──│  Service    │──│                  │  │ │
│  │  │  Component   │  │  (WebSocket)│  │  (HTTP Client)   │  │ │
│  │  └──────────────┘  └─────────────┘  └──────────────────┘  │ │
│  │         │                  │                   │            │ │
│  │         │                  │                   │            │ │
│  │    [Microphone]       [WebSocket]          [HTTP]          │ │
│  │         │                  │                   │            │ │
│  └─────────┼──────────────────┼───────────────────┼────────────┘ │
└────────────┼──────────────────┼───────────────────┼──────────────┘
             │                  │                   │
             │                  │                   │
             │      ┌───────────▼───────────────────▼─────────────┐
             │      │    .NET Core Backend (Port 5000)            │
             │      │  ┌──────────────────┐  ┌──────────────────┐ │
             │      │  │ Token Controller │  │ Realtime         │ │
             │      │  │ (GET /api/token) │  │ Controller       │ │
             │      │  └──────────────────┘  │ (WebSocket       │ │
             │      │           │             │  /api/realtime)  │ │
             │      │           │             └──────────────────┘ │
             │      │           │                      │           │
             │      │  ┌────────▼──────────┐  ┌───────▼─────────┐ │
             │      │  │  Token Service    │  │ Realtime Service│ │
             │      │  │  - Generate token │  │ - WebSocket     │ │
             │      │  │  - Validate token │  │   proxy         │ │
             │      │  │  - 5min expiry    │  │ - Audio relay   │ │
             │      │  └───────────────────┘  └─────────────────┘ │
             │      └─────────────────────────────────┼───────────┘
             │                                        │
             │                                        │ WebSocket
             │                                        │ (wss://)
             │                                        │
             │                            ┌───────────▼───────────┐
             │                            │   OpenAI Realtime API │
             └────────────Audio Playback──│   GPT-4 Model         │
                                          │   Voice: Alloy        │
                                          └───────────────────────┘
```

## Component Details

### Frontend (Angular 18)

**Voice Assistant Component**
- User interface for voice interaction
- Button controls (Connect, Disconnect, Start/Stop Recording)
- Status display and activity log
- Manages component lifecycle

**Realtime Service**
- WebSocket connection management
- Audio capture from browser microphone
- Audio playback through Web Audio API
- Message handling and event processing
- PCM16 audio encoding/decoding

**Token Service**
- HTTP client for token requests
- Token caching and management
- Communication with backend API

### Backend (.NET Core 9)

**Token Controller**
- `GET /api/token`: Generates short-lived session tokens
- Returns token with expiration timestamp

**Realtime Controller**
- `GET /api/realtime/ws`: WebSocket endpoint
- Token validation
- WebSocket connection management
- Stream bridging between client and OpenAI

**Token Service**
- Token generation using cryptographic random
- In-memory token storage with expiration
- Automatic cleanup of expired tokens
- Thread-safe operations

**Realtime Service**
- OpenAI WebSocket client management
- Bidirectional message relay
- Session configuration
- Audio data streaming
- Error handling and logging

### Security Layers

```
┌──────────────────────────────────────┐
│  1. CORS Policy                      │
│     - Whitelist origins              │
│     - Allow credentials              │
├──────────────────────────────────────┤
│  2. Token Authentication             │
│     - 5-minute expiry                │
│     - Secure random generation       │
│     - Server-side validation         │
├──────────────────────────────────────┤
│  3. API Key Protection               │
│     - Backend only                   │
│     - Environment variables          │
│     - Never exposed to client        │
├──────────────────────────────────────┤
│  4. WebSocket Security               │
│     - Token validation required      │
│     - Connection timeouts            │
│     - Error handling                 │
└──────────────────────────────────────┘
```

## Data Flow

### Connection Flow

1. **User opens application**
   - Angular app loads in browser
   - UI displays "Connect" button

2. **User clicks Connect**
   - Frontend requests token from backend
   - Backend generates 5-minute token
   - Backend returns token + expiry
   - Frontend receives token

3. **Establish WebSocket connection**
   - Frontend creates WebSocket with token parameter
   - Backend validates token
   - Backend creates WebSocket to OpenAI
   - Backend sends session configuration to OpenAI
   - Connection established

### Audio Flow

1. **User clicks Start Recording**
   - Frontend requests microphone access
   - Browser prompts user for permission
   - Audio context created
   - Audio capture begins

2. **Audio streaming**
   - Microphone captures raw audio
   - Frontend converts to PCM16 format
   - Frontend encodes to base64
   - Frontend sends via WebSocket to backend
   - Backend relays to OpenAI

3. **Response handling**
   - OpenAI processes audio
   - OpenAI sends response audio
   - Backend relays to frontend
   - Frontend decodes base64
   - Frontend converts PCM16 to float32
   - Frontend plays through Web Audio API

## Deployment Architecture

### Docker Deployment

```
docker-compose.yml
├── backend service
│   ├── Build: Dockerfile
│   ├── Port: 5000:8080
│   └── Environment: API keys
└── frontend service
    ├── Build: Dockerfile
    ├── Port: 4200:80
    └── Depends on: backend
```

### Azure Deployment Options

**Option 1: Azure Web Apps**
```
Azure Web App (Backend)
├── .NET Core runtime
├── WebSocket enabled
├── Environment variables
└── App Service Plan (B1+)

Azure Static Web App (Frontend)
├── Static files (dist/)
├── CDN integration
└── Custom domain
```

**Option 2: Azure Container Instances**
```
Container Registry
├── Backend image
└── Frontend image

Container Instances
├── Backend container
└── Frontend container
```

**Option 3: Azure Kubernetes Service**
```
AKS Cluster
├── Backend deployment
│   ├── 2+ replicas
│   └── Service (LoadBalancer)
└── Frontend deployment
    ├── 2+ replicas
    └── Service (LoadBalancer)
```

## Technology Stack

### Frontend
- **Framework**: Angular 18
- **Language**: TypeScript 5.x
- **Build Tool**: Angular CLI
- **HTTP Client**: HttpClient (Angular)
- **WebSocket**: Native WebSocket API
- **Audio**: Web Audio API
- **Server**: Nginx (production)

### Backend
- **Framework**: .NET Core 9
- **Language**: C# 12
- **API Style**: Minimal API
- **WebSocket**: System.Net.WebSockets
- **OpenAI SDK**: OpenAI .NET SDK 2.1.0
- **Logging**: ILogger

### Infrastructure
- **Containerization**: Docker
- **Orchestration**: Docker Compose
- **Cloud**: Azure (Web Apps, AKS, ACI)
- **Web Server**: Nginx (frontend)
- **Reverse Proxy**: Kestrel (backend)

## Scalability Considerations

### Horizontal Scaling
- Backend can scale with load balancer
- Frontend is stateless
- Token service needs shared storage (Redis) for multi-instance

### Vertical Scaling
- Increase container resources
- Adjust Azure App Service tier
- Monitor CPU and memory usage

### Optimization
- WebSocket connection pooling
- Audio buffer optimization
- Token cache with Redis
- CDN for frontend assets

## Monitoring and Observability

### Logging
- Structured logging with ILogger
- Request/response logging
- WebSocket events
- Error tracking

### Metrics
- Token generation rate
- WebSocket connections
- Audio stream duration
- API latency

### Alerts
- High error rates
- Connection failures
- Token expiration issues
- Resource exhaustion

## Future Enhancements

1. **Rate Limiting**: Add per-user rate limits
2. **Persistent Storage**: Use Redis for tokens
3. **User Authentication**: Add OAuth/OIDC
4. **Audio Recording**: Save conversation history
5. **Multi-language**: Support additional languages
6. **Mobile Apps**: Native iOS/Android clients
7. **Advanced Features**: Interruption handling, conversation context

## References

- [OpenAI Realtime API Documentation](https://platform.openai.com/docs/guides/realtime)
- [Angular Documentation](https://angular.io/docs)
- [.NET Core Documentation](https://docs.microsoft.com/dotnet/core/)
- [Web Audio API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API)
- [WebSocket Protocol](https://datatracker.ietf.org/doc/html/rfc6455)
