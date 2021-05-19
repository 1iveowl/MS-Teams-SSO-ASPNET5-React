using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace teams_sso_sample.Policies.RateLimit
{
    public class AsyncRateLimitEngine<TResult>
    {
        private record ActionItem
        {
            internal int Id { get; init; }
            internal Func<Context, CancellationToken, Task<TResult>> Action { get; init; }
            internal Context Context { get; init; }
            internal CancellationToken CancellationToken { get; init; }
            internal bool ContinueOnCapturedContext { get; init; }
            internal DateTimeOffset? ScheduledFor { get; init; }
        }
        private static int _id;

        private readonly static Subject<ActionItem> _actionSubject = new();
        private readonly static BehaviorSubject<bool> _isThrottlingSubject = new(false);

        private readonly static IObserver<ActionItem> _actionObserver = _actionSubject.AsObserver();

        private readonly static IObservable<ActionItem> _actionObservable = _actionSubject.AsObservable()
            .Pausable(_isThrottlingObservable, bufferSize: 100)
            .Publish().RefCount();

        private readonly static IObserver<bool> _isThrottlingObserver = _isThrottlingSubject.AsObserver();
        private readonly static IObservable<bool> _isThrottlingObservable = _isThrottlingSubject.AsObservable();

        private static IObservable<(bool hasExceeded, DateTimeOffset nextAvailableTime)> HasExceededLimitObservable(int lenght, TimeSpan windowSize) =>
            _actionObservable.Timestamp()
                .SlidingWindow(windowSize)
                .Select(list => (list.Count > lenght, GetNextTimeNotViolatingTheRateLimit(list, windowSize)))
                .Publish().RefCount();

        public AsyncRateLimitEngine()        {
            
        }

        internal static async Task<TResult> ImplementationAsync(
            Func<Context, CancellationToken, Task<TResult>> action,
            Context context,
            CancellationToken cancellationToken,
            bool continueOnCapturedContext,
            int maxLimit,
            TimeSpan windowSize,
            int bufferSize,
            IAsyncRateLimitController<TResult> rateLimitController) =>
                await rateLimitController.CreateActionObservable(new ActionItem<TResult>
                {
                    Action = action,
                    Context = context,
                    CancellationToken = cancellationToken,
                    ContinueOnCapturedContext = continueOnCapturedContext                  
                }).FirstAsync();


        //Observable.Create<TResult>(obs =>
        //    {
        //        _id++;

        //        var id = _id;

        //        var actionsDisposable = _actionObservable
        //        // Only react to the specific action that was created when ImplementationAsync was called.
        //        .Where(a => a.Id == id) 
        //        .CombineLatest(HasExceededLimitObservable(maxLimit, windowSize),
        //            (action, hasExceededLimit) => hasExceededLimit.hasExceeded 
        //                ? (action with { ScheduledFor = hasExceededLimit.nextAvailableTime }) 
        //                : action)
        //        .Do(a =>
        //        {
        //            if (a.ScheduledFor is not null)
        //            {
        //                /* As soon as we hit the first instance of the rate limit being exceeded
        //                 * the throttling observer is set to true, which pauses all incoming actions
        //                 * and places them on the buffer.
        //                */
        //                _isThrottlingObserver.OnNext(true);

        //                /* The action that triggered the rate limit overrun is decorated with 
        //                 * the next available time when the action can be run without violating
        //                 * the rate limit.
        //                */
        //            }
        //        })
        //        .Delay(a => Observable.Timer(a.ScheduledFor is not null 
        //            ? (a.ScheduledFor - DateTimeOffset.UtcNow).Value 
        //            : TimeSpan.Zero))
        //        .Do(a =>
        //        {
        //            if (a.ScheduledFor is not null)
        //            {                            
        //                _isThrottlingObserver.OnNext(false);
        //            }
        //        })
        //        .Select(a => Observable.FromAsync(_ => RunAction(a)))
        //        .Concat()
        //        .Subscribe(
        //            result => obs.OnNext(result),
        //            ex => obs.OnError(ex),
        //            () => obs.OnCompleted());

        //        _actionObserver.OnNext(new ActionItem
        //        {
        //            Action = action,
        //            Context = context,
        //            CancellationToken = cancellationToken,
        //            ContinueOnCapturedContext = continueOnCapturedContext,
        //            Id = id
        //        });

        //        return actionsDisposable;
        //    }).FirstAsync(); // Complete with the first OnNext.


        private static async Task<TResult> RunAction(ActionItem actionItem) => 
            await actionItem.Action(actionItem.Context, actionItem.CancellationToken).ConfigureAwait(actionItem.ContinueOnCapturedContext);

        private static DateTimeOffset GetNextTimeNotViolatingTheRateLimit<T>(IReadOnlyList<Timestamped<T>> timestampList, TimeSpan windowSize)
        {
            var lastActionInWindow = timestampList[timestampList.Count - 1].Timestamp;
            var firstActionInWindow = timestampList[0].Timestamp;
            var timeUntilLimitNotExceeded = windowSize - (lastActionInWindow - firstActionInWindow);

            return lastActionInWindow + timeUntilLimitNotExceeded;
        }
    }
}
