using EveUp.Core.DTOs.App;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/app")]
public class AppController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AppController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Health check
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = typeof(AppController).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        });
    }

    /// <summary>
    /// Retorna versão mínima do app
    /// </summary>
    [HttpGet("min-version")]
    public ActionResult<MinVersionResponse> GetMinVersion()
    {
        return Ok(new MinVersionResponse
        {
            MinVersion = _configuration["App:MinVersion"] ?? "1.0.0",
            StoreUrl = _configuration["App:StoreUrl"]
        });
    }
}
