using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace teams_sso_sample.Policies.RateLimit
{
    public class AsyncRateAdjustController<TResult> : IAsyncRateAdjustController<TResult>
    {
        private bool _isRunning;

        private int _id;
                        
        private readonly IConnectableObservable<ActionItem<TResult>> _actionPipelineObservable;

        private readonly IObserver<ActionItem<TResult>> _observerIncomingAction;

        public AsyncRateAdjustController(int maxLimit, TimeSpan windowSize, int bufferSize)
        {
            var incomingActionSubject = new Subject<ActionItem<TResult>>();
            _observerIncomingAction = incomingActionSubject.AsObserver();
            var incomingActionObservable = incomingActionSubject.AsObservable();

            _actionPipelineObservable = incomingActionObservable                
                .RateAdjust(windowSize, maxLimit, bufferSize)
                .Replay(maxLimit + bufferSize);
        }

        public async Task<TResult> ProcessActionInPipeline(ActionItem<TResult> action)
        {
            if (!_isRunning)
            {
                _actionPipelineObservable.Connect();
                _isRunning = true;
            }

            var id = _id++;

            var resultObservable = _actionPipelineObservable
                .Where(a => a.Id == id)
                .Select(a => Observable.FromAsync(_ => RunAction(a), Scheduler.Default))
                .Concat()
                .FirstAsync();

            _observerIncomingAction.OnNext(action with { Id = id });

            return await resultObservable;
        }

        private static async Task<TResult> RunAction(ActionItem<TResult> actionItem) =>
            await actionItem.Action(actionItem.Context, actionItem.CancellationToken).ConfigureAwait(actionItem.ContinueOnCapturedContext);


    }
}
