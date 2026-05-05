namespace Frameworks4.Services;

public class MetricsService
{
    private long _successfulTransitions;
    private long _failedTransitions;
    private long _duplicateDeliveries;
    private long _compensations;

    private readonly Dictionary<string, (long Count, double TotalMs)> _stepLatencies = new();
    private readonly object _latencyLock = new();

    public void RecordSuccess()    => Interlocked.Increment(ref _successfulTransitions);
    public void RecordFailure()    => Interlocked.Increment(ref _failedTransitions);
    public void RecordDuplicate()  => Interlocked.Increment(ref _duplicateDeliveries);
    public void RecordCompensation() => Interlocked.Increment(ref _compensations);

    public void RecordLatency(string step, double ms)
    {
        lock (_latencyLock)
        {
            if (_stepLatencies.TryGetValue(step, out var existing))
                _stepLatencies[step] = (existing.Count + 1, existing.TotalMs + ms);
            else
                _stepLatencies[step] = (1, ms);
        }
    }

    public long TotalFailures => Interlocked.Read(ref _failedTransitions);

    public object GetMetrics()
    {
        Dictionary<string, double> avgLatencies;
        lock (_latencyLock)
        {
            avgLatencies = _stepLatencies.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Count > 0 ? Math.Round(kv.Value.TotalMs / kv.Value.Count, 2) : 0.0);
        }
        return new
        {
            SuccessfulTransitions = Interlocked.Read(ref _successfulTransitions),
            FailedTransitions     = Interlocked.Read(ref _failedTransitions),
            DuplicateDeliveries   = Interlocked.Read(ref _duplicateDeliveries),
            Compensations         = Interlocked.Read(ref _compensations),
            StepAvgLatencyMs      = avgLatencies
        };
    }
}
