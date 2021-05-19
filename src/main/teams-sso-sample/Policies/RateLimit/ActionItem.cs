using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace teams_sso_sample.Policies.RateLimit
{
    public record ActionItem<TResult> : IActionItem<TResult>
    {
        public int Id { get; init; }
        public Func<Context, CancellationToken, Task<TResult>> Action { get; init; }
        public Context Context { get; init; }
        public CancellationToken CancellationToken { get; init; }
        public bool ContinueOnCapturedContext { get; init; }
        public DateTimeOffset? ScheduledFor { get; init; }

        public DateTimeOffset Timestamp { get; init; }
    }
}
