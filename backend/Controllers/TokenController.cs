using Microsoft.AspNetCore.Mvc;
using GptRealtime.Api.Services;
using GptRealtime.Api.Models;
using Microsoft.Extensions.Options;

namespace GptRealtime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly OpenAISettings _settings;

    public TokenController(ITokenService tokenService, IOptions<OpenAISettings> settings)
    {
        _tokenService = tokenService;
        _settings = settings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetToken([FromQuery] string? personaId, CancellationToken cancellationToken)
    {
        var token = await _tokenService.CreateSessionTokenAsync(personaId, cancellationToken);
        return Ok(token);
    }

    [HttpGet("personas")]
    public IActionResult GetPersonas()
    {
        return Ok(_settings.Personas);
    }
}
