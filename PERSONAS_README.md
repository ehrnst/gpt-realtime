# Multiple Personas Feature

## Overview

The GPT Realtime Voice Assistant now supports multiple AI personas, allowing users to choose between different assistants with unique personalities, voices, and expertise.

## Available Personas

### 1. Weekend Motivator 🎉
- **Voice**: Alloy
- **Personality**: Enthusiastic, positive, energetic
- **Expertise**: Helping users plan exciting weekends
- **Use Case**: Get suggestions for weekend activities, events, and adventures

### 2. Travel Agent ✈️
- **Voice**: Nova
- **Personality**: Professional, knowledgeable, helpful
- **Expertise**: Travel planning and recommendations
- **Use Case**: Plan trips, get destination advice, find accommodations, and travel tips

## How to Use

1. **Open the Application**: Navigate to the voice assistant application
2. **Select a Persona**: You'll see a persona selector with cards for each available assistant
3. **Click to Choose**: Click on the persona card you want to talk with
4. **Start Conversing**: The assistant will greet you and you can begin your conversation
5. **Switch Personas**: Click "Change Assistant" at any time to select a different persona

## Technical Implementation

### Architecture

The personas feature is implemented following the application's architecture:

- **Backend**: Handles persona configurations (system instructions, voice settings) and session token generation
- **Frontend**: Provides UI for persona selection and passes the selected persona ID to the backend

### API Endpoints

#### GET /api/personas
Returns a list of all available personas.

**Response:**
```json
[
  {
    "id": "weekend-motivator",
    "name": "Weekend Motivator",
    "description": "An enthusiastic coach who helps you make the most of your weekends",
    "voice": "alloy",
    "systemInstructions": "You are a helpful weekend motivator..."
  },
  {
    "id": "travel-agent",
    "name": "Travel Agent",
    "description": "A knowledgeable travel expert who helps you plan amazing trips",
    "voice": "nova",
    "systemInstructions": "You are an experienced travel agent..."
  }
]
```

#### GET /api/token?personaId={personaId}
Returns a session token configured for the specified persona.

**Parameters:**
- `personaId` (optional): The ID of the persona to use. If omitted, uses default settings.

**Example:**
```bash
curl "http://localhost:5000/api/token?personaId=travel-agent"
```

### Configuration

Personas are configured in `appsettings.json`:

```json
{
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
}
```

### Adding New Personas

To add a new persona:

1. **Update Configuration**: Add a new persona object to the `Personas` array in `appsettings.json`
2. **Choose a Voice**: Select from available voices: `alloy`, `echo`, `fable`, `onyx`, `nova`, `shimmer`
3. **Write System Instructions**: Define the personality, expertise, and behavior of the persona
4. **Restart the Application**: The new persona will be available immediately

**Example:**
```json
{
  "Id": "fitness-coach",
  "Name": "Fitness Coach",
  "Description": "A motivating fitness expert who helps you achieve your health goals",
  "Voice": "echo",
  "SystemInstructions": "You are a professional fitness coach. Help users create workout plans, provide nutrition advice, and motivate them to achieve their fitness goals. Be encouraging, knowledgeable, and personalize your advice based on their fitness level."
}
```

## Development

### File Structure

```
backend/
├── Models/
│   ├── Persona.cs                    # Persona data model
│   └── PersonaSettings.cs            # Configuration model
├── Controllers/
│   └── PersonasController.cs         # API endpoints for personas
└── Services/
    └── SpeachService.cs              # Session token generation with persona support

frontend/
├── src/app/
│   ├── services/
│   │   ├── persona.interface.ts      # Persona TypeScript interface
│   │   ├── persona.service.ts        # HTTP service for personas API
│   │   └── realtime.service.ts       # WebRTC connection (persona-aware)
│   ├── persona-selector.component.*  # Persona selection UI
│   └── components/
│       └── voice-assistant/          # Main voice assistant component
```

### Testing

See `TESTING_PERSONAS.md` for comprehensive testing procedures.

**Quick Test:**
```bash
# Terminal 1 - Backend
cd backend
dotnet run

# Terminal 2 - Frontend
cd frontend
npm start

# Open browser to http://localhost:4200
# Select a persona and start talking
```

## User Experience

### Persona Selection Screen

When the application loads, users see an elegant grid of persona cards:

- **Visual Design**: Cards with hover effects and clear selection indicators
- **Information Display**: Name, description, and voice for each persona
- **Selection Feedback**: Selected card is highlighted with blue accent color

### During Conversation

- **Persona Info**: Display of current persona name at the top
- **Change Button**: Easily switch to a different persona
- **Distinct Experience**: Each persona has a unique voice and personality that remains consistent throughout the conversation

### Voice Differences

- **Alloy** (Weekend Motivator): Neutral, balanced tone
- **Nova** (Travel Agent): Warm, friendly tone

Users can clearly distinguish between personas based on both voice characteristics and conversation style.

## Best Practices

### For Users

1. **Choose Based on Need**: Select the persona that matches your current goal
2. **Clear Communication**: Speak clearly and wait for the assistant to finish before responding
3. **Context Setting**: Provide relevant context early in the conversation
4. **Persona Switching**: Feel free to switch personas if your needs change

### For Developers

1. **Consistent Personalities**: Ensure system instructions create coherent, consistent personas
2. **Voice Matching**: Choose voices that match the persona's character
3. **Clear Descriptions**: Write clear, concise descriptions that set user expectations
4. **Testing**: Test each persona thoroughly to ensure quality interactions

## Troubleshooting

### Persona Doesn't Maintain Character

- **Check System Instructions**: Verify the instructions are clear and specific
- **Test with Direct Prompts**: Ask persona-specific questions to verify behavior
- **Review Logs**: Check backend logs for persona selection confirmation

### Wrong Voice Used

- **Verify Configuration**: Check that the correct voice is set in appsettings.json
- **Check Logs**: Backend should log which persona/voice is being used
- **Clear Cache**: Clear browser cache and retry

### Persona Selector Not Showing

- **Check Console**: Look for JavaScript errors in browser console
- **Verify API**: Test GET /api/personas endpoint directly
- **Component Registration**: Ensure PersonaSelectorComponent is properly registered

## Future Enhancements

Potential future improvements:

- **User Preferences**: Remember user's last selected persona
- **Custom Personas**: Allow users to create and save custom personas
- **Persona History**: Track conversations by persona
- **More Personas**: Add specialists like "Code Tutor", "Life Coach", "Language Teacher"
- **Dynamic Loading**: Load personas from a database instead of configuration
- **Persona Analytics**: Track which personas are most popular
- **Multi-language**: Support personas in different languages

## Support

For implementation details, see:
- `PERSONAS_INTEGRATION.md` - Integration guide
- `PERSONAS_CODE_CHANGES.md` - Specific code changes
- `TESTING_PERSONAS.md` - Testing procedures
- `IMPLEMENTATION_STATUS.md` - Current implementation status

For issues or questions, please open a GitHub issue with the label `personas`.
