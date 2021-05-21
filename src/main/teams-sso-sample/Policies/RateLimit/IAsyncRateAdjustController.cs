using System;
using System.Threading.Tasks;

namespace teams_sso_sample.Policies.RateLimit
{
    public interface IAsyncRateAdjustController<TResult>
    {
        Task<TResult> ProcessActionInPipeline(ActionItem<TResult> action);
    }
}
