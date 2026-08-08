using Polly;
using Polly.CircuitBreaker;

namespace OrderService.Services;

public static class ResiliencePolicies
{
    // ─────────────────────────────────────────
    // RETRY POLICY
    // Retries 3 times with exponential backoff
    // Wait: 1s → 2s → 4s between retries
    // ─────────────────────────────────────────
    public static IAsyncPolicy GetRetryPolicy(ILogger logger) =>
        Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)), // 2s, 4s, 8s
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s — Reason: {Message}",
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });

    // ─────────────────────────────────────────
    // CIRCUIT BREAKER POLICY
    // Opens circuit after 3 consecutive failures
    // Stays open for 10 seconds (fast fail)
    // Then goes half-open to test recovery
    // ─────────────────────────────────────────
    public static IAsyncPolicy GetCircuitBreakerPolicy(ILogger logger) =>
        Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(10),
                onBreak: (exception, duration) =>
                {
                    logger.LogError(
                        "Circuit OPEN — too many failures. " +
                        "Fast failing for {Duration}s. Reason: {Message}",
                        duration.TotalSeconds, exception.Message);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit CLOSED — service recovered");
                },
                onHalfOpen: () =>
                {
                    logger.LogWarning("Circuit HALF-OPEN — testing recovery");
                });

    // ─────────────────────────────────────────
    // COMBINED POLICY
    // Retry sits OUTSIDE circuit breaker
    // So retry fires first, then circuit breaker
    // tracks consecutive failures
    // ─────────────────────────────────────────
    public static IAsyncPolicy GetCombinedPolicy(ILogger logger) =>
        Policy.WrapAsync(
            GetRetryPolicy(logger),
            GetCircuitBreakerPolicy(logger));
}