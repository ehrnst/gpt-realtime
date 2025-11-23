# Personas Feature - Specific Code Changes

This document provides the exact code changes needed to integrate the personas feature.

## Backend Changes

### File: backend/Services/SpeachService.cs

#### Change 1: Add PersonaSettings field and constructor parameter

**Find the private fields section (near the top of the class):**
```csharp
private readonly OpenAISettings _settings;
private readonly ILogger<SpeachService> _logger;
```

**Add this field:**
```csharp
private readonly PersonaSettings _personaSettings;
```

**Find the constructor:**
```csharp
public SpeachService(
    IOptions<OpenAISettings> settings,
    ILogger<SpeachService> logger)
{
    _settings = settings.Value;
    _logger = logger;
}
```

**Update to:**
```csharp
public SpeachService(
    IOptions<OpenAISettings> settings,
    IOptions<PersonaSettings> personaSettings,
    ILogger<SpeachService> logger)
{
    _settings = settings.Value;
    _personaSettings = personaSettings.Value;
    _logger = logger;
}
```

#### Change 2: Update CreateSessionTokenAsync method signature

**Find:**
```csharp
public async Task<SessionToken> CreateSessionTokenAsync()
```

**Update to:**
```csharp
public async Task<SessionToken> CreateSessionTokenAsync(string? personaId = null)
```

#### Change 3: Add persona resolution logic

**Find the beginning of CreateSessionTokenAsync method (after the signature):**
```csharp
public async Task<SessionToken> CreateSessionTokenAsync(string? personaId = null)
{
    // Existing validation code...
```

**Add this code right after the method starts, before building the session config:**
```csharp
// Determine which voice and instructions to use
string voice = _settings.Voice;
string systemInstructions = _settings.SystemInstructions;

// Override with persona-specific settings if provided
if (!string.IsNullOrEmpty(personaId))
{
    var persona = _personaSettings.Personas?.FirstOrDefault(p => p.Id == personaId);
    if (persona != null)
    {
        voice = persona.Voice;
        systemInstructions = persona.SystemInstructions;
        _logger.LogInformation("Using persona: {PersonaName} (ID: {PersonaId})", persona.Name, personaId);
    }
    else
    {
        _logger.LogWarning("Persona {PersonaId} not found, using default settings", personaId);
    }
}
```

#### Change 4: Use persona variables in session configuration

**Find where the session configuration is built (look for the object that includes "voice" and "instructions"):**
```csharp
var sessionConfig = new
{
    model = _settings.Model,
    voice = _settings.Voice,
    // ...
    instructions = _settings.SystemInstructions,
    // ...
};
```

**Update to use the variables:**
```csharp
var sessionConfig = new
{
    model = _settings.Model,
    voice = voice,  // Use determined voice
    // ...
    instructions = systemInstructions,  // Use determined instructions
    // ...
};
```

### File: backend/Controllers/TokenController.cs

#### Change: Update GetToken method to accept personaId

**Find the GetToken method:**
```csharp
[HttpGet]
public async Task<IActionResult> GetToken()
{
    try
    {
        var token = await _speachService.CreateSessionTokenAsync();
        return Ok(token);
    }
    // ... error handling
}
```

**Update to:**
```csharp
[HttpGet]
public async Task<IActionResult> GetToken([FromQuery] string? personaId = null)
{
    try
    {
        var token = await _speachService.CreateSessionTokenAsync(personaId);
        return Ok(token);
    }
    // ... error handling
}
```

### File: backend/Program.cs

#### Change: Register PersonaSettings

**Find where OpenAI settings are configured (look for something like):**
```csharp
builder.Services.Configure<OpenAISettings>(
    builder.Configuration.GetSection("OpenAI"));
```

**Add right after:**
```csharp
builder.Services.Configure<PersonaSettings>(
    builder.Configuration.GetSection("Personas"));
```

### File: backend/appsettings.json

#### Change: Add Personas configuration

**Add this section to the JSON (at the same level as "OpenAI" and "Logging"):**
```json
"Personas": {
  "Personas": [
    {
      "Id": "weekend-motivator",
      "Name": "Weekend Motivator",
      "Description": "An enthusiastic coach who helps you make the most of your weekends",
      "Voice": "alloy",
      "SystemInstructions": "You are a helpful weekend motivator. Your goal is to inspire and energize users to make the most of their weekends. Be enthusiastic, positive, and suggest fun activities based on the conversation."
    },
    {
      "Id": "travel-agent",
      "Name": "Travel Agent",
      "Description": "A knowledgeable travel expert who helps you plan amazing trips",
      "Voice": "nova",
      "SystemInstructions": "You are an experienced travel agent. Your goal is to help users plan amazing trips. Ask about their preferences, budget, interests, and desired destinations. Provide personalized recommendations for destinations, accommodations, activities, and travel tips. Be informative, friendly, and help them create memorable travel experiences."
    }
  ]
}
```

## Frontend Changes

### File: frontend/src/app/services/realtime.service.ts

#### Change: Update connect method to accept personaId

**Find the connect method signature:**
```typescript
async connect(): Promise<void>
```

**Update to:**
```typescript
async connect(personaId?: string): Promise<void>
```

**Find where the token is fetched (look for fetch or HttpClient call to '/api/token'):**
```typescript
const tokenUrl = `${environment.apiUrl}/api/token`;
const response = await fetch(tokenUrl);
```

**Update to:**
```typescript
const tokenUrl = personaId 
  ? `${environment.apiUrl}/api/token?personaId=${encodeURIComponent(personaId)}`
  : `${environment.apiUrl}/api/token`;
const response = await fetch(tokenUrl);
```

### File: frontend/src/app/app.module.ts (or app.config.ts for standalone)

#### Change: Import and declare PersonaSelectorComponent

**If using NgModule (app.module.ts):**

**Find the imports:**
```typescript
import { VoiceAssistantComponent } from './components/voice-assistant/voice-assistant.component';
```

**Add:**
```typescript
import { PersonaSelectorComponent } from './persona-selector.component';
```

**Find declarations array:**
```typescript
declarations: [
  AppComponent,
  VoiceAssistantComponent,
  // ... other components
],
```

**Add:**
```typescript
declarations: [
  AppComponent,
  VoiceAssistantComponent,
  PersonaSelectorComponent,  // Add this
  // ... other components
],
```

**If using standalone components (Angular 18+), in the component that uses PersonaSelectorComponent:**

**Find imports array:**
```typescript
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    // ... other imports
  ],
  // ...
})
```

**Add:**
```typescript
imports: [
  CommonModule,
  PersonaSelectorComponent,  // Add this
  // ... other imports
],
```

### File: frontend/src/app/components/voice-assistant/voice-assistant.component.ts

#### Change 1: Import Persona interface and add properties

**Add to imports:**
```typescript
import { Persona } from '../../services/persona.interface';
```

**Add properties to the class:**
```typescript
export class VoiceAssistantComponent implements OnInit {
  selectedPersona: Persona | null = null;
  showPersonaSelector = true;
  
  // ... existing properties
```

#### Change 2: Add persona selection handler

**Add this method:**
```typescript
onPersonaSelected(persona: Persona): void {
  this.selectedPersona = persona;
  this.showPersonaSelector = false;
  this.connect();
}
```

#### Change 3: Update connect method

**Find the connect method:**
```typescript
async connect(): Promise<void> {
  try {
    this.statusMessage = 'Connecting...';
    await this.realtimeService.connect();
    // ...
  }
}
```

**Update to:**
```typescript
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
```

#### Change 4: Add changePersona method

**Add this method:**
```typescript
changePersona(): void {
  this.disconnect();
  this.showPersonaSelector = true;
  this.selectedPersona = null;
}
```

### File: frontend/src/app/components/voice-assistant/voice-assistant.component.html

#### Change: Add persona selector UI

**Wrap the existing content and add persona selector:**

**Before:**
```html
<div class="voice-assistant">
  <div class="status">{{ statusMessage }}</div>
  <!-- ... rest of existing UI ... -->
</div>
```

**After:**
```html
<div class="voice-assistant-container">
  <!-- Show persona selector before connection -->
  <app-persona-selector 
    *ngIf="showPersonaSelector"
    (personaSelected)="onPersonaSelected($event)">
  </app-persona-selector>
  
  <!-- Show voice assistant after persona selection -->
  <div *ngIf="!showPersonaSelector" class="voice-assistant">
    <div class="persona-info">
      <h3>Talking with {{ selectedPersona?.name }}</h3>
      <button (click)="changePersona()" class="change-persona-btn">
        Change Assistant
      </button>
    </div>
    
    <div class="status">{{ statusMessage }}</div>
    <!-- ... rest of existing UI ... -->
  </div>
</div>
```

## Summary

These changes implement:
1. Backend: Persona-aware session token creation
2. Backend: API endpoint to list available personas
3. Frontend: UI to select personas
4. Frontend: Passing selected persona to backend

After making these changes, the application will support multiple personas with different voices and personalities.
