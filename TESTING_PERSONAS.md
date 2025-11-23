# Testing the Multiple Personas Feature

This guide provides step-by-step instructions for testing the personas feature after implementation.

## Prerequisites

1. All code changes from `PERSONAS_CODE_CHANGES.md` have been applied
2. Backend and frontend dependencies are installed
3. Azure OpenAI API credentials are configured

## Testing Checklist

### Backend Testing

#### 1. Test Personas API Endpoint

**Start the backend:**
```bash
cd backend
dotnet run
```

**Test GET /api/personas (list all personas):**
```bash
curl http://localhost:5000/api/personas
```

**Expected Response:**
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

✅ **Pass Criteria:** Returns 200 status with both personas

#### 2. Test Token Endpoint with Persona

**Test default token (no persona):**
```bash
curl http://localhost:5000/api/token
```

**Expected Response:**
```json
{
  "clientSecret": "...",
  "expiresAt": "2025-11-23T15:36:57.237Z",
  "realtimeUrl": "wss://swedencentral.realtimeapi-preview.ai.azure.com"
}
```

**Test with weekend-motivator persona:**
```bash
curl "http://localhost:5000/api/token?personaId=weekend-motivator"
```

**Test with travel-agent persona:**
```bash
curl "http://localhost:5000/api/token?personaId=travel-agent"
```

**Test with invalid persona:**
```bash
curl "http://localhost:5000/api/token?personaId=invalid-id"
```

✅ **Pass Criteria:** 
- All requests return 200 status with valid tokens
- Backend logs show persona selection (check console output)
- Invalid persona falls back to default settings (check logs)

#### 3. Verify Backend Logs

Check the backend console output for log messages like:
```
info: backend.Services.SpeachService[0]
      Using persona: Weekend Motivator (ID: weekend-motivator)
```

or for invalid personas:
```
warn: backend.Services.SpeachService[0]
      Persona invalid-id not found, using default settings
```

### Frontend Testing

#### 1. Start the Frontend

```bash
cd frontend
npm install  # if not already done
npm start
```

Navigate to `http://localhost:4200`

#### 2. Test Persona Selector UI

✅ **Visual Checks:**
- [ ] Persona selector is displayed on page load
- [ ] Two persona cards are shown (Weekend Motivator and Travel Agent)
- [ ] Cards show name, description, and voice badge
- [ ] Cards have hover effects
- [ ] Selected card is highlighted

✅ **Interaction Checks:**
- [ ] Click on Weekend Motivator card - it should be selected
- [ ] Click on Travel Agent card - it should be selected
- [ ] Only one card can be selected at a time

#### 3. Test Weekend Motivator Persona

1. **Select Weekend Motivator** from the persona selector
2. **Wait for connection** - should see "Connecting to Weekend Motivator..."
3. **Verify connection** - should see "Connected to Weekend Motivator!"
4. **Wait for greeting** - assistant should speak first with an enthusiastic, motivational tone
5. **Listen to voice** - should be using "alloy" voice (check it sounds like alloy)
6. **Test conversation:**
   - Click "Start Talking"
   - Say: "What should I do this weekend?"
   - Expected: Enthusiastic suggestions for weekend activities
7. **Verify personality:** Assistant should be upbeat, positive, energetic

✅ **Pass Criteria:**
- Connection successful
- Alloy voice is used
- Assistant has motivational personality
- Suggests weekend activities
- Enthusiastic tone

#### 4. Test Travel Agent Persona

1. **Click "Change Assistant"** button
2. **Select Travel Agent** from persona selector
3. **Wait for connection** - should see "Connecting to Travel Agent..."
4. **Verify connection** - should see "Connected to Travel Agent!"
5. **Wait for greeting** - assistant should speak first with a professional, friendly tone
6. **Listen to voice** - should be using "nova" voice (different from alloy)
7. **Test conversation:**
   - Click "Start Talking"
   - Say: "I want to plan a trip to Paris"
   - Expected: Professional travel advice, asks about preferences, budget, dates
8. **Verify personality:** Assistant should be knowledgeable, helpful, travel-focused

✅ **Pass Criteria:**
- Connection successful
- Nova voice is used (noticeably different from alloy)
- Assistant has travel agent personality
- Asks relevant travel questions
- Provides travel recommendations
- Professional and informative tone

#### 5. Test Persona Switching

1. **Start with Weekend Motivator**, have a short conversation
2. **Click "Change Assistant"**
3. **Verify:** Persona selector is shown again
4. **Select Travel Agent**
5. **Verify:** Connection establishes successfully
6. **Start new conversation** - context should be reset
7. **Switch back to Weekend Motivator**
8. **Verify:** Works correctly again

✅ **Pass Criteria:**
- Can switch between personas multiple times
- Each persona maintains its distinct personality
- No errors or connection issues during switching
- Previous conversation context is cleared

### Browser Console Testing

#### 1. Check Network Requests

Open browser DevTools (F12) → Network tab

**Expected Requests:**
1. `GET /api/personas` - Returns list of personas
2. `GET /api/token?personaId=weekend-motivator` - Returns token
3. WebRTC connection to Azure

✅ **Pass Criteria:**
- All requests return 200 status
- No CORS errors
- personaId parameter is correctly sent

#### 2. Check Console Logs

Look for logs like:
```javascript
Using persona: Weekend Motivator (ID: weekend-motivator)
Data channel opened
Received message: {type: 'session.created', ...}
```

✅ **Pass Criteria:**
- No JavaScript errors
- Connection messages appear
- Message events are logged

### Integration Testing

#### 1. Test Complete Flow

**Scenario: Weekend Planning Conversation**
1. Select Weekend Motivator
2. Wait for greeting
3. Have 3-4 turn conversation about weekend plans
4. Verify assistant stays in character throughout
5. Disconnect

**Scenario: Travel Planning Conversation**
1. Select Travel Agent
2. Wait for greeting
3. Have 3-4 turn conversation about trip planning
4. Verify assistant stays in character throughout
5. Disconnect

✅ **Pass Criteria:**
- Both scenarios complete without errors
- Personas maintain distinct personalities
- Voice quality is good
- Audio playback works correctly
- No disconnections or glitches

### Error Handling Testing

#### 1. Test Invalid Persona ID

Manually edit the URL or modify code to send invalid persona ID:
```typescript
await this.realtimeService.connect('invalid-persona-id');
```

✅ **Expected:** Connection still works, falls back to default settings

#### 2. Test Without Persona ID

Call connect without any persona ID:
```typescript
await this.realtimeService.connect();
```

✅ **Expected:** Connection works with default OpenAI settings

#### 3. Test Backend Unavailable

Stop the backend while frontend is running

✅ **Expected:** Appropriate error message shown to user

### Performance Testing

#### 1. Response Time

- Select a persona and measure time to connection
- **Expected:** < 5 seconds

#### 2. Voice Quality

- Test audio playback for both personas
- **Expected:** Clear audio, no distortion or lag

#### 3. Switching Speed

- Time how long it takes to switch personas
- **Expected:** < 3 seconds

### Docker Testing (Optional)

If using Docker:

```bash
docker compose up --build
```

**Test:**
1. Access frontend at `http://localhost:4200`
2. Verify all persona features work
3. Check both personas can be selected and used
4. Verify environment variables are correctly loaded

## Test Results Template

Use this template to document your testing:

```markdown
## Personas Feature Test Results

**Date:** YYYY-MM-DD
**Tester:** Your Name
**Environment:** Local/Docker/Production

### Backend Tests
- [ ] GET /api/personas returns both personas
- [ ] GET /api/token works without personaId
- [ ] GET /api/token works with valid personaId
- [ ] Invalid personaId falls back gracefully
- [ ] Backend logs show persona selection

### Frontend Tests
- [ ] Persona selector displays correctly
- [ ] Persona cards are clickable and selectable
- [ ] Weekend Motivator connects and works
- [ ] Travel Agent connects and works
- [ ] Personas have distinct voices (alloy vs nova)
- [ ] Personas have distinct personalities
- [ ] Change Assistant button works
- [ ] Can switch between personas multiple times

### Integration Tests
- [ ] Complete conversation with Weekend Motivator successful
- [ ] Complete conversation with Travel Agent successful
- [ ] No errors in browser console
- [ ] Network requests successful
- [ ] Audio quality is good

### Issues Found
(List any issues, bugs, or unexpected behavior)

### Screenshots
(Attach screenshots of persona selector, conversations, etc.)
```

## Troubleshooting

### Persona selector doesn't appear
- Check browser console for errors
- Verify PersonaSelectorComponent is registered in app.module.ts
- Check GET /api/personas returns data

### Connection fails with persona
- Check backend logs for errors
- Verify Azure OpenAI credentials
- Check personaId is correctly sent in URL

### Wrong voice used
- Verify persona configuration in appsettings.json
- Check backend logs show correct persona selected
- Clear browser cache and retry

### Persona doesn't maintain character
- Verify systemInstructions in appsettings.json
- Check Azure OpenAI is using correct model
- Test with more explicit prompts

## Success Criteria

All tests should pass with:
✅ Both personas accessible and selectable
✅ Distinct voices for each persona (alloy vs nova)
✅ Distinct personalities maintained throughout conversations
✅ Smooth switching between personas
✅ No errors or connection issues
✅ Good audio quality
✅ Responsive UI with clear visual feedback
