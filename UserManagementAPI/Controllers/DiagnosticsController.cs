using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Controllers;

/// <summary>
/// Internal diagnostics. Endpoints are no-ops outside Development.
/// Used to verify middleware behavior end-to-end.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DiagnosticsController : ControllerBase
{
    private readonly IHostEnvironment _env;

    public DiagnosticsController(IHostEnvironment env) => _env = env;

    /// <summary>
    /// Throws an unhandled exception so ErrorHandlingMiddleware can be exercised.
    /// Returns 404 outside Development.
    /// </summary>
    [HttpGet("throw")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ThrowForMiddlewareTest()
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }
        throw new InvalidOperationException(
            "Intentional test exception for ErrorHandlingMiddleware verification.");
    }
}
