using System;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;

namespace Syncronex.Api.Features.Webhooks;

public static class ResiliencyPolicies
{
  // 1. Exponential Backoff Retry Policy (e.g., 2s, 4s, 8s)
  public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
  {
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
              Console.WriteLine($"[Polly] Request failed. Waiting {timespan.TotalSeconds}s before retry attempt {retryAttempt}.");
            });
  }

  // 2. Circuit Breaker Policy
  public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
  {
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
            {
              Console.WriteLine($"[Polly] Circuit OPENED for {breakDelay.TotalSeconds} seconds due to continuous failures.");
            },
            onReset: () =>
            {
              Console.WriteLine("[Polly] Circuit RESET (Closed). Normal operations resumed.");
            },
            onHalfOpen: () =>
            {
              Console.WriteLine("[Polly] Circuit HALF-OPEN. Testing if the external service is back online.");
            });
  }
}
