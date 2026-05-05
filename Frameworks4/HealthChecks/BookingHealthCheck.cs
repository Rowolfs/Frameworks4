using Microsoft.Extensions.Diagnostics.HealthChecks;
using Frameworks4.Services;

namespace Frameworks4.HealthChecks;

public class BookingReadinessCheck : IHealthCheck
{
    private readonly MetricsService _metrics;
    private const long FailureThreshold = 10;

    public BookingReadinessCheck(MetricsService metrics) => _metrics = metrics;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        if (_metrics.TotalFailures > FailureThreshold)
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"Too many failures ({_metrics.TotalFailures}), service degraded"));

        return Task.FromResult(HealthCheckResult.Healthy("Service is ready"));
    }
}
