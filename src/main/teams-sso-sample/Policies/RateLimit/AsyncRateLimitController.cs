using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace teams_sso_sample.Policies.RateLimit
{
    public class AsyncRateLimitController<TResult> : IAsyncRateLimitController<TResult>
    {
        private int _id;
                        
        private readonly IConnectableObservable<ActionItem<TResult>> _sequencedActionObservable;

        private readonly IObserver<ActionItem<TResult>> _observerIncomingAction;               

        public AsyncRateLimitController(int maxLimit, TimeSpan windowSize, int bufferSize)
        {
            var eventLoopSceduler = new EventLoopScheduler();

            var incomingActionSubject = new ReplaySubject<ActionItem<TResult>>(maxLimit + bufferSize, eventLoopSceduler);
            _observerIncomingAction = incomingActionSubject.AsObserver();
            
            var incomingActionObservable = incomingActionSubject.AsObservable();

            IObservable<(bool hasExceeded, DateTimeOffset nextAvailableTime)> hasExceededLimitObservable = incomingActionObservable
                .Replay(windowSize)
                .Select(a => a.Timestamp)
                .ToList()
                .Select(list => (list.Count > maxLimit, GetNextTimeHonoringRateLimit(list, windowSize)));

            _sequencedActionObservable = incomingActionObservable
                .CombineLatest(hasExceededLimitObservable,
                    (ationItem, hasExceededLimit) => hasExceededLimit.hasExceeded
                        ? (ationItem with { ScheduledFor = hasExceededLimit.nextAvailableTime })
                        : ationItem)
                .Delay(a => Observable.Timer(
                    a.ScheduledFor is not null
                        ? (a.ScheduledFor - DateTimeOffset.UtcNow).Value
                        : TimeSpan.Zero,
                    eventLoopSceduler))
                .Publish(); // Running the delay on the event loop scheduler pauses the pipeline.;            
        }

        public IObservable<TResult> CreateActionObservable(ActionItem<TResult> actionItem)
        {
            var id = _id++;

            var t = Scheduler.CurrentThread;

            var rateLimitObservable = Observable.Create<TResult>(obs =>
            {
                var actionPipelineDisposable = _sequencedActionObservable                    
                    .Where(a => a.Id == id)
                    .Select(a => Observable.FromAsync(_ => RunAction(a), Scheduler.Default))
                    .Concat()
                    .Subscribe(
                        result => obs.OnNext(result),
                        ex => obs.OnError(ex),
                        () => obs.OnCompleted());                

                return actionPipelineDisposable;
            });

            _observerIncomingAction.OnNext(actionItem with { Id = id, Timestamp = DateTimeOffset.UtcNow });

            return rateLimitObservable;
        }

        private static async Task<TResult> RunAction(IActionItem<TResult> actionItem) =>
            await actionItem.Action(actionItem.Context, actionItem.CancellationToken).ConfigureAwait(actionItem.ContinueOnCapturedContext);

        private static DateTimeOffset GetNextTimeHonoringRateLimit(IList<DateTimeOffset> timestampList, TimeSpan windowSize)
        {
            var lastActionInWindow = timestampList[timestampList.Count - 1];
            var firstActionInWindow = timestampList[0];
            var timeUntilLimitNotExceeded = windowSize - (lastActionInWindow - firstActionInWindow);

            return lastActionInWindow + timeUntilLimitNotExceeded;
        }
    }
}
