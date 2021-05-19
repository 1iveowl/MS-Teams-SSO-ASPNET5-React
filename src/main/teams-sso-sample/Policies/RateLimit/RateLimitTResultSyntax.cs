using System;
using teams_sso_sample.Policies.RateLimit;

namespace Polly
{
    /// <summary>
    /// Fluent API for defining a Rate Limit policy <see cref="Policy{TResult}"/>. 
    /// </summary>
    public static class RateLimitTResultSyntax
    {
        public static AsyncRateLimitPolicy<TResult> RateLimitPolity<TResult>(
            this PolicyBuilder<TResult> policyBuilder,
            int maxLimit, 
            TimeSpan windowSize, 
            int bufferSize)
        {            
            var rateLimitController = new AsyncRateLimitController<TResult>(
                maxLimit,
                windowSize,
                bufferSize);

            return new AsyncRateLimitPolicy<TResult>(
                policyBuilder,
                rateLimitController);
        }
    }
}
