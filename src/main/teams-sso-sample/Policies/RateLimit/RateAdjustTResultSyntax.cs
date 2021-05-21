using System;
using teams_sso_sample.Policies.RateLimit;

namespace Polly
{
    /// <summary>
    /// Fluent API for defining a Rate Limit policy <see cref="Policy{TResult}"/>. 
    /// </summary>
    public static class RateAdjustTResultSyntax
    {
        public static AsyncRateAdjustPolicy<TResult> RateLimitPolity<TResult>(
            this PolicyBuilder<TResult> policyBuilder,
            int maxLimit, 
            TimeSpan windowSize, 
            int bufferSize)
        {            
            var rateLimitController = new AsyncRateAdjustController<TResult>(
                maxLimit,
                windowSize,
                bufferSize);

            return new AsyncRateAdjustPolicy<TResult>(
                policyBuilder,
                rateLimitController);
        }
    }
}
