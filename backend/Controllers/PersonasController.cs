using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonasController : ControllerBase
{
    private readonly PersonaSettings _personaSettings;
    private readonly ILogger<PersonasController> _logger;

    public PersonasController(
        IOptions<PersonaSettings> personaSettings,
        ILogger<PersonasController> logger)
    {
        _personaSettings = personaSettings.Value;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Persona>> GetPersonas()
    {
        _logger.LogInformation("Retrieving all personas");
        return Ok(_personaSettings.Personas);
    }

    [HttpGet("{id}")]
    public ActionResult<Persona> GetPersona(string id)
    {
        var persona = _personaSettings.Personas.FirstOrDefault(p => p.Id == id);
        
        if (persona == null)
        {
            _logger.LogWarning("Persona with id {PersonaId} not found", id);
            return NotFound($"Persona with id '{id}' not found");
        }

        _logger.LogInformation("Retrieved persona {PersonaId}", id);
        return Ok(persona);
    }
}
