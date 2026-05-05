using Microsoft.AspNetCore.Mvc;
using Frameworks4.Models;
using Frameworks4.Services;

namespace Frameworks4.Controllers;

[ApiController]
[Route("api/booking")]
public class BookingController : ControllerBase
{
    private readonly BookingService _booking;
    private readonly MetricsService _metrics;
    private readonly ILogger<BookingController> _logger;

    public BookingController(BookingService booking, MetricsService metrics, ILogger<BookingController> logger)
    {
        _booking = booking;
        _metrics = metrics;
        _logger  = logger;
    }

    /// <summary>Отправить событие в процесс бронирования</summary>
    [HttpPost("{processKey}/event")]
    public IActionResult SendEvent(string processKey, [FromBody] EventRequest request)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString()
            : request.CorrelationId;

        if (!Enum.TryParse<BookingEvent>(request.Event, ignoreCase: true, out var evt))
        {
            return BadRequest(new
            {
                Error         = $"Unknown event: '{request.Event}'",
                ValidEvents   = Enum.GetNames<BookingEvent>(),
                CorrelationId = correlationId
            });
        }

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? Guid.NewGuid().ToString()
            : request.IdempotencyKey;

        var (state, message) = _booking.ProcessEvent(processKey, evt, idempotencyKey, correlationId);

        return Ok(new EventResponse
        {
            ProcessKey    = processKey,
            State         = state.ToString(),
            Message       = message,
            CorrelationId = correlationId
        });
    }

    /// <summary>Получить текущее состояние процесса</summary>
    [HttpGet("{processKey}")]
    public IActionResult GetState(string processKey)
    {
        var state = _booking.GetState(processKey);
        if (state is null)
            return NotFound(new { Error = "Process not found", ProcessKey = processKey });

        return Ok(new { ProcessKey = processKey, State = state.ToString() });
    }
}
