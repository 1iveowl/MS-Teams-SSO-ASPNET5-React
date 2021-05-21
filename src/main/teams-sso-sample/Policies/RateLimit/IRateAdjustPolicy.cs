using Polly;

namespace teams_sso_sample.Policies.RateLimit
{
    /// <summary>
    /// Defines properties common to synchronous and asynchronous ProactiveFoo policies.
    /// </summary>

    public interface IRateAdjustPolicy : IsPolicy
    {
        /* Define properties (if any) or methods (if any) you may want to expose on ProactiveFooPolicy.

             - Perhaps the custom policy takes configuration properties which you want to expose.
             - Perhaps the custom policy exposes methods for manual control.

            ... but it is equally common to have none.
             */
    }

    /// <summary>
    /// Defines properties common to generic, synchronous and asynchronous ProactiveFoo policies.
    /// </summary>
    public interface IRateLimitPolicy<TResult> : IRateAdjustPolicy
    {
        /* Define properties (if any) or methods (if any) you may want to expose on ProactiveFooPolicy<TResult>.

           Typically, IProactiveFooPolicy<TResult> : IProactiveFooPolicy, so you would only expose here any 
           extra properties/methods typed in <TResult> for TResult policies.
         */
    }
}
