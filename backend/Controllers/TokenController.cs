using Microsoft.AspNetCore.Mvc;
using GptRealtime.Api.Services;

namespace GptRealtime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokenController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpGet]
    public async Task<IActionResult> GetToken(CancellationToken cancellationToken)
    {
        var token = await _tokenService.CreateSessionTokenAsync(cancellationToken);
        return Ok(token);
    }
}
