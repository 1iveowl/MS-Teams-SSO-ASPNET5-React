using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace teams_sso_sample.Policies.RateLimit
{
    public interface IActionItem<TResult>
    {
        int Id { get; }
        Func<Context, CancellationToken, Task<TResult>> Action { get; }
        Context Context { get; }
        CancellationToken CancellationToken { get; }
        bool ContinueOnCapturedContext { get; }
        DateTimeOffset Timestamp { get; }
        DateTimeOffset? ScheduledFor { get; }
    }
}
