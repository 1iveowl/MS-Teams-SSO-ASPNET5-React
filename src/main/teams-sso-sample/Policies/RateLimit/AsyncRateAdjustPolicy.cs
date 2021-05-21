using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace teams_sso_sample.Policies.RateLimit
{
    //public class AsyncRateLimitPolicy : AsyncPolicy, IRateLimitPolicy
    //{
    //    private readonly int _maxLimit;
    //    private readonly TimeSpan _windowSize;
    //    private readonly int _bufferSize;              

    //    /// <summary>
    //    /// Constructs a new instance of <see cref="AsyncRateLimitPolicy"/>.
    //    /// </summary>
    //    /// <param name="maxLimit">Max rate limit</param>
    //    /// <param name="windowSize">The time window for the rate limit - e.g. 30 request per second is a window size of one second.</param>
    //    /// <returns><see cref="AsyncRateLimitPolicy"/></returns>
    //    public static AsyncRateLimitPolicy Create(int maxLimit, TimeSpan windowSize, int bufferSize)
    //    {
    //        return new AsyncRateLimitPolicy(maxLimit, windowSize, bufferSize);
    //    }

    //    internal AsyncRateLimitPolicy(PolicyBuilder policyBuilder) : base(policyBuilder)
    //    {
    //    }


    //    internal AsyncRateLimitPolicy(int maxLimit, TimeSpan windowSize, int bufferSize)
    //    {
    //        _maxLimit = maxLimit;
    //        _windowSize = windowSize;
    //        _bufferSize = bufferSize;
    //    }

    //    /// <inheritdoc/>
    //    protected override Task<TResult> ImplementationAsync<TResult>(
    //        Func<Context, CancellationToken, Task<TResult>> action, 
    //        Context context, 
    //        CancellationToken cancellationToken,
    //        bool continueOnCapturedContext)
    //    {
    //        return AsyncRateLimitEngine<TResult>.ImplementationAsync(
    //            action,
    //            context,
    //            cancellationToken,
    //            continueOnCapturedContext,
    //            _maxLimit,
    //            _windowSize,
    //            _bufferSize);
    //    }
    //}

    public class AsyncRateAdjustPolicy<TResult> : AsyncPolicy<TResult>, IRateLimitPolicy<TResult>
    {
        private readonly int _maxLimit;
        private readonly TimeSpan _windowSize;
        private readonly int _bufferSize;

        private readonly IAsyncRateAdjustController<TResult> _rateLimitController;

        /// <summary>
        /// Constructs a new instance of <see cref="AsyncRateAdjustPolicy{TResult}"/>.
        /// </summary>
        /// <param name="maxLimit">Max rate limit</param>
        /// <param name="windowSize">The time window for the rate limit - e.g. 30 request per second is a window size of one second.</param>
        /// <returns><see cref="AsyncRateAdjustPolicy{TResult}"/></returns>
        public static AsyncRateAdjustPolicy<TResult> Create(
            int maxLimit, 
            TimeSpan windowSize, 
            int bufferSize,
            IAsyncRateAdjustController<TResult> rateLimitController)
        {
            return new AsyncRateAdjustPolicy<TResult>(maxLimit, windowSize, bufferSize, rateLimitController);
        }

        internal AsyncRateAdjustPolicy(
            PolicyBuilder<TResult> policyBuilder, 
            IAsyncRateAdjustController<TResult> rateLimitController) : base(policyBuilder)
        {
            _rateLimitController = rateLimitController;
        }

        internal AsyncRateAdjustPolicy(
            int maxLimit, 
            TimeSpan windowSize, 
            int bufferSize,
            IAsyncRateAdjustController<TResult> rateLimitController)
        {
            _maxLimit = maxLimit;
            _windowSize = windowSize;
            _bufferSize = bufferSize;
            _rateLimitController = rateLimitController;
        }

        /// <inheritdoc/>
        protected override Task<TResult> ImplementationAsync(
            Func<Context, CancellationToken, Task<TResult>> action, 
            Context context, 
            CancellationToken cancellationToken,
            bool continueOnCapturedContext)
        {
            return AsyncRateAdjustEngine<TResult>.ImplementationAsync(
                action,
                context,
                cancellationToken,
                continueOnCapturedContext,
                _maxLimit,
                _windowSize,
                _bufferSize,
                _rateLimitController);
        }
    }
}
