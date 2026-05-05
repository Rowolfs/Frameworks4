using System.Collections.Concurrent;
using Frameworks4.Models;

namespace Frameworks4.Services;

public class BookingService
{
    private readonly ConcurrentDictionary<string, BookingProcess> _processes = new();
    private readonly MetricsService _metrics;
    private readonly ILogger<BookingService> _logger;
    private static readonly Random _rng = new();

    // Complete-шаг падает с вероятностью 30%, триггерит компенсацию CheckIn
    private const double CompleteFailureRate = 0.30;

    private static readonly Dictionary<(BookingState, BookingEvent), BookingState> Transitions = new()
    {
        { (BookingState.Idle,      BookingEvent.Reserve),  BookingState.Reserved  },
        { (BookingState.Reserved,  BookingEvent.Confirm),  BookingState.Confirmed },
        { (BookingState.Confirmed, BookingEvent.CheckIn),  BookingState.CheckedIn },
        { (BookingState.CheckedIn, BookingEvent.Complete), BookingState.Completed },
        { (BookingState.Reserved,  BookingEvent.Cancel),   BookingState.Cancelled },
        { (BookingState.Confirmed, BookingEvent.Cancel),   BookingState.Cancelled },
        { (BookingState.CheckedIn, BookingEvent.Cancel),   BookingState.Cancelled },
    };

    public BookingService(MetricsService metrics, ILogger<BookingService> logger)
    {
        _metrics = metrics;
        _logger  = logger;
    }

    public (BookingState State, string Message) ProcessEvent(
        string processKey, BookingEvent evt, string idempotencyKey, string correlationId)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var process = _processes.GetOrAdd(processKey, k => new BookingProcess { ProcessKey = k });

        lock (process)
        {
            // --- Идемпотентность ---
            if (process.ProcessedEvents.Contains(idempotencyKey))
            {
                _metrics.RecordDuplicate();
                _logger.LogInformation(
                    "DUPLICATE event={Event} processKey={ProcessKey} idempotencyKey={IdempKey} correlationId={CorrId}",
                    evt, processKey, idempotencyKey, correlationId);
                return (process.State, "Duplicate event — state unchanged");
            }

            // --- Проверка допустимости перехода ---
            if (!Transitions.TryGetValue((process.State, evt), out var nextState))
            {
                _metrics.RecordFailure();
                _logger.LogWarning(
                    "INVALID_TRANSITION state={State} event={Event} processKey={ProcessKey} correlationId={CorrId}",
                    process.State, evt, processKey, correlationId);
                sw.Stop();
                _metrics.RecordLatency(evt.ToString(), sw.Elapsed.TotalMilliseconds);
                return (process.State, $"Invalid transition: {process.State} + {evt}");
            }

            // --- Симуляция сбоя Complete + компенсация CheckIn ---
            if (evt == BookingEvent.Complete && _rng.NextDouble() < CompleteFailureRate)
            {
                const BookingState compensated = BookingState.Confirmed;
                process.State = compensated;
                process.ProcessedEvents.Add(idempotencyKey);

                _metrics.RecordFailure();
                _metrics.RecordCompensation();
                sw.Stop();
                _metrics.RecordLatency(evt.ToString(), sw.Elapsed.TotalMilliseconds);

                _logger.LogError(
                    "STEP_FAILURE event={Event} processKey={ProcessKey} correlationId={CorrId} — compensation: rolled back CheckIn → {CompState}",
                    evt, processKey, correlationId, compensated);

                return (compensated, $"Step {evt} failed. Compensation: CheckIn rolled back, state → {compensated}");
            }

            // --- Успешный переход ---
            var prevState = process.State;
            process.State = nextState;
            process.ProcessedEvents.Add(idempotencyKey);

            _metrics.RecordSuccess();
            sw.Stop();
            _metrics.RecordLatency(evt.ToString(), sw.Elapsed.TotalMilliseconds);

            _logger.LogInformation(
                "TRANSITION processKey={ProcessKey} {From} --{Event}--> {To} correlationId={CorrId}",
                processKey, prevState, evt, nextState, correlationId);

            return (nextState, $"Transition: {prevState} → {nextState}");
        }
    }

    public BookingState? GetState(string processKey) =>
        _processes.TryGetValue(processKey, out var p) ? p.State : null;

    public int ActiveProcessCount => _processes.Count;
}
