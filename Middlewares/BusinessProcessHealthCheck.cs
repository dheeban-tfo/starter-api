using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace starterapi.HealthChecks;

public class BusinessProcessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // This is where you'd check your critical business process
        // For this example, we'll just return healthy
        bool isHealthy = true;

        if (isHealthy)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Business process is functioning normally."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Business process is not functioning."));
    }
}