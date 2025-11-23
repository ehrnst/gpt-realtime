// HELPER METHODS TO ADD TO SpeachService.cs
// Add these two methods to the SpeachService class (before the closing brace of the class)

private string GetVoiceForPersona(string? personaId)
{
    if (!string.IsNullOrEmpty(personaId))
    {
        var persona = _personaSettings.Personas?.FirstOrDefault(p => p.Id == personaId);
        if (persona != null)
        {
            _logger.LogInformation("Using voice '{Voice}' for persona: {PersonaName} (ID: {PersonaId})", 
                persona.Voice, persona.Name, personaId);
            return persona.Voice;
        }
        _logger.LogWarning("Persona {PersonaId} not found, using default voice", personaId);
    }
    return _settings.Voice;
}

private string GetInstructionsForPersona(string? personaId)
{
    if (!string.IsNullOrEmpty(personaId))
    {
        var persona = _personaSettings.Personas?.FirstOrDefault(p => p.Id == personaId);
        if (persona != null)
        {
            return persona.SystemInstructions;
        }
    }
    return _settings.SystemInstructions;
}
