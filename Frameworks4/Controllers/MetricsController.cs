using Microsoft.AspNetCore.Mvc;
using Frameworks4.Services;

namespace Frameworks4.Controllers;

[ApiController]
public class MetricsController : ControllerBase
{
    private readonly MetricsService _metrics;
    public MetricsController(MetricsService metrics) => _metrics = metrics;

    /// <summary>Получить счётчики метрик</summary>
    [HttpGet("/metrics")]
    public IActionResult GetMetrics() => Ok(_metrics.GetMetrics());
}
