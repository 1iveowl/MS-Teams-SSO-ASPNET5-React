using System;

namespace teams_sso_sample.Policies.RateLimit
{
    public interface IAsyncRateLimitController<TResult>
    {
        //IObservable<IActionItem<TResult>> ActionPipelineObservable { get; }

        IObservable<TResult> CreateActionObservable(ActionItem<TResult> action);
    }
}
