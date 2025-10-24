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
    public IActionResult GetToken()
    {
        var token = _tokenService.GenerateToken();
        return Ok(token);
    }
}
