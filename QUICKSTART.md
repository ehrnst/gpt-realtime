# Quick Start Guide

Get your GPT Realtime Voice Assistant up and running in 5 minutes!

## Prerequisites

- Docker and Docker Compose installed
- OpenAI API key with GPT-4 Realtime API access

## Steps

### 1. Clone and Configure

```bash
git clone https://github.com/ehrnst/gpt-realtime.git
cd gpt-realtime
cp .env.example .env
```

### 2. Add Your API Key

Edit `.env` file:
```env
OPENAI_API_KEY=sk-your-actual-api-key-here
```

### 3. Start the Application

```bash
docker-compose up --build
```

Wait for the build to complete (first time takes ~5 minutes).

### 4. Open the Application

Navigate to http://localhost:4200 in your browser.

### 5. Use the Voice Assistant

1. Click **Connect** button
2. Allow microphone access when prompted
3. Click **Start Recording**
4. Speak your question or message
5. Click **Stop Recording**
6. Listen to the AI's voice response!

## Troubleshooting

**Can't hear audio?**
- Check your browser audio settings
- Ensure speakers/headphones are connected
- Check browser console for errors

**Microphone not working?**
- Allow microphone access in browser settings
- Check system microphone permissions

**Connection failed?**
- Verify your API key is correct
- Check Docker logs: `docker-compose logs backend`

## Next Steps

- Read the full [README.md](README.md) for detailed information
- Explore [Azure deployment options](azure/README.md)
- Customize the voice and model settings in backend configuration

## Support

Having issues? Check:
1. Docker containers are running: `docker-compose ps`
2. Backend logs: `docker-compose logs backend`
3. Frontend logs: `docker-compose logs frontend`
4. Browser console (F12)

For more help, open an issue on GitHub.
