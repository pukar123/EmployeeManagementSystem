using Microsoft.AspNetCore.Mvc;
using Pukar.Usermanagement.Application.Services.Jwt;

namespace Pukar.Usermanagement.API.Controllers;

[ApiController]
public sealed class JwksController : ControllerBase
{
    private readonly IJwksProvider? _jwks;

    public JwksController(IServiceProvider services)
    {
        _jwks = services.GetService(typeof(IJwksProvider)) as IJwksProvider;
    }

    [HttpGet("/.well-known/jwks.json")]
    [ResponseCache(Duration = 300)]
    public IActionResult GetJwks()
    {
        if (_jwks is null)
            return NotFound();

        return Ok(_jwks.GetJsonWebKeySet());
    }
}
