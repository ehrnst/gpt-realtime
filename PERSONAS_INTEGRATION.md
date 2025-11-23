# Personas Integration Guide

This document describes how to integrate the multi-persona feature into the existing GPT Realtime Voice Assistant application.

## Overview

The personas feature allows users to choose between different AI assistants (e.g., Weekend Motivator, Travel Agent) with different personalities, voices, and system instructions.

## Architecture

- **Backend**: Stores persona configurations and applies them during session token creation
- **Frontend**: Provides UI for persona selection and passes selected persona ID to backend

## Backend Integration Steps

### 1. Update Program.cs

Add PersonaSettings configuration binding:

```csharp
// Add this after the existing OpenAI settings configuration
builder.Services.Configure<PersonaSettings>(
    builder.Configuration.GetSection("Personas"));
```

### 2. Update appsettings.json

Merge the contents of `appsettings.Personas.json` into `appsettings.json`:

```json
{
  "Logging": { ... },
  "AllowedHosts": "*",
  "OpenAI": { ... },
  "Personas": {
    "Personas": [
      {
        "Id": "weekend-motivator",
        "Name": "Weekend Motivator",
        "Description": "An enthusiastic coach who helps you make the most of your weekends",
        "Voice": "alloy",
        "SystemInstructions": "You are a helpful weekend motivator..."
      },
      {
        "Id": "travel-agent",
        "Name": "Travel Agent",
        "Description": "A knowledgeable travel expert who helps you plan amazing trips",
        "Voice": "nova",
        "SystemInstructions": "You are an experienced travel agent..."
      }
    ]
  }
}
```

### 3. Modify SpeachService.cs

Update the `CreateSessionTokenAsync` method to accept an optional personaId parameter:

```csharp
public class SpeachService
{
    private readonly OpenAISettings _settings;
    private readonly PersonaSettings _personaSettings;
    private readonly ILogger<SpeachService> _logger;
    
    // Add PersonaSettings to constructor
    public SpeachService(
        IOptions<OpenAISettings> settings,
        IOptions<PersonaSettings> personaSettings,
        ILogger<SpeachService> logger)
    {
        _settings = settings.Value;
        _personaSettings = personaSettings.Value;
        _logger = logger;
    }
    
    // Update method signature
    public async Task<SessionToken> CreateSessionTokenAsync(string? personaId = null)
    {
        // Determine which configuration to use
        string voice = _settings.Voice;
        string systemInstructions = _settings.SystemInstructions;
        
        // If personaId is provided, override with persona-specific settings
        if (!string.IsNullOrEmpty(personaId))
        {
            var persona = _personaSettings.Personas
                .FirstOrDefault(p => p.Id == personaId);
                
            if (persona != null)
            {
                voice = persona.Voice;
                systemInstructions = persona.SystemInstructions;
                _logger.LogInformation("Using persona: {PersonaId}", personaId);
            }
            else
            {
                _logger.LogWarning("Persona {PersonaId} not found, using default settings", personaId);
            }
        }
        
        // In CreateSessionConfiguration, use the voice and systemInstructions variables
        var sessionConfig = new
        {
            model = _settings.Model,
            voice = voice,  // Use determined voice
            modalities = new[] { "text", "audio" },
            instructions = systemInstructions,  // Use determined instructions
            // ... rest of config
        };
        
        // ... rest of the method remains the same
    }
}
```

### 4. Modify TokenController.cs

Update the GetToken method to accept a personaId query parameter:

```csharp
[HttpGet]
public async Task<ActionResult<SessionToken>> GetToken([FromQuery] string? personaId = null)
{
    try
    {
        var token = await _speachService.CreateSessionTokenAsync(personaId);
        return Ok(token);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating session token");
        return StatusCode(500, "Failed to create session token");
    }
}
```

## Frontend Integration Steps

### 1. Update app.module.ts (or standalone component if using Angular 18+)

Import and declare the PersonaSelectorComponent:

```typescript
import { PersonaSelectorComponent } from './persona-selector.component';

@NgModule({
  declarations: [
    // ... existing components
    PersonaSelectorComponent
  ],
  // ...
})
```

### 2. Update realtime.service.ts

Modify the connect method to accept and pass personaId:

```typescript
async connect(personaId?: string): Promise<void> {
  try {
    // Fetch session token with personaId
    const tokenUrl = personaId 
      ? `${environment.apiUrl}/api/token?personaId=${encodeURIComponent(personaId)}`
      : `${environment.apiUrl}/api/token`;
      
    const tokenResponse = await fetch(tokenUrl);
    
    if (!tokenResponse.ok) {
      throw new Error(`Failed to get session token: ${tokenResponse.statusText}`);
    }
    
    const sessionToken = await tokenResponse.json();
    
    // ... rest of connection logic remains the same
  } catch (error) {
    console.error('Failed to connect:', error);
    throw error;
  }
}
```

### 3. Update voice-assistant.component.ts

Add persona selection logic:

```typescript
import { Persona } from './services/persona.interface';

export class VoiceAssistantComponent implements OnInit {
  selectedPersona: Persona | null = null;
  showPersonaSelector = true;
  
  // ... existing properties
  
  onPersonaSelected(persona: Persona): void {
    this.selectedPersona = persona;
    this.showPersonaSelector = false;
    this.connect();
  }
  
  async connect(): Promise<void> {
    if (!this.selectedPersona) {
      console.error('No persona selected');
      return;
    }
    
    try {
      this.statusMessage = `Connecting to ${this.selectedPersona.name}...`;
      await this.realtimeService.connect(this.selectedPersona.id);
      this.statusMessage = 'Connected! Start talking...';
    } catch (error) {
      this.statusMessage = 'Connection failed. Please try again.';
      console.error('Connection error:', error);
    }
  }
  
  changePersona(): void {
    this.disconnect();
    this.showPersonaSelector = true;
    this.selectedPersona = null;
  }
}
```

### 4. Update voice-assistant.component.html

Add persona selector UI:

```html
<div class="voice-assistant-container">
  <!-- Show persona selector before connection -->
  <app-persona-selector 
    *ngIf="showPersonaSelector"
    (personaSelected)="onPersonaSelected($event)">
  </app-persona-selector>
  
  <!-- Show voice assistant after persona selection -->
  <div *ngIf="!showPersonaSelector">
    <div class="persona-info">
      <h3>Talking with {{ selectedPersona?.name }}</h3>
      <button (click)="changePersona()" class="change-persona-btn">
        Change Assistant
      </button>
    </div>
    
    <!-- Existing voice assistant UI -->
    <div class="status">{{ statusMessage }}</div>
    <!-- ... rest of existing UI -->
  </div>
</div>
```

## Testing

1. Start the backend: `cd backend && dotnet run`
2. Start the frontend: `cd frontend && npm start`
3. Open http://localhost:4200
4. You should see persona selection UI
5. Select "Weekend Motivator" - should connect with alloy voice and motivational personality
6. Disconnect and select "Travel Agent" - should connect with nova voice and travel expert personality

## Environment Variables

For Docker deployments, you don't need to change environment variables. The personas are configured in appsettings.json and the existing OpenAI credentials will be used for all personas.

## Future Enhancements

- Add more personas via configuration
- Allow dynamic persona creation
- Store user's last selected persona preference
- Add persona-specific conversation history
