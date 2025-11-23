// REFERENCE IMPLEMENTATION - This file shows how TokenController.cs should look after personas integration
// Compare this with your actual TokenController.cs and apply the necessary changes

using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly SpeachService _speachService;
    private readonly ILogger<TokenController> _logger;

    public TokenController(
        SpeachService speachService,
        ILogger<TokenController> logger)
    {
        _speachService = speachService;
        _logger = logger;
    }

    // UPDATED: Method now accepts optional personaId query parameter
    [HttpGet]
    public async Task<IActionResult> GetToken([FromQuery] string? personaId = null)
    {
        try
        {
            _logger.LogInformation("Token requested for persona: {PersonaId}", personaId ?? "default");
            
            // UPDATED: Pass personaId to CreateSessionTokenAsync
            var token = await _speachService.CreateSessionTokenAsync(personaId);
            
            return Ok(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session token");
            return StatusCode(500, new { error = "Failed to create session token", message = ex.Message });
        }
    }
}
