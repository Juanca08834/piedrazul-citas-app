using Microsoft.AspNetCore.Mvc;

namespace Piedrazul.Api.Controllers;

[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", utc = DateTime.UtcNow });
}
