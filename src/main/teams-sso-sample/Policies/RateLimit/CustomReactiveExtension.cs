using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace teams_sso_sample.Policies.RateLimit
{
    public static class CustomReactiveExtension
    {

        public static IObservable<IReadOnlyList<Timestamped<T>>>
            SlidingWindow<T>(this IObservable<Timestamped<T>> self, TimeSpan length) =>
                self.Scan(new LinkedList<Timestamped<T>>(),
                    (ll, newSample) =>
                    {
                        ll.AddLast(newSample);
                        var oldest = newSample.Timestamp - length;
                        while (ll.Count > 0 && ll.First.Value.Timestamp < oldest)
                        {
                            ll.RemoveFirst();
                        }
                        return ll;
                    }).Select(l => l.ToList().AsReadOnly());

        public static IObservable<T> RateAdjust<T>(
              this IObservable<T> source,
              TimeSpan windowSize,
              int maxLimit,
              int bufferSize,
              TimeSpan? padding = default)
        {
            return Observable.Create<T>(observer =>
            {
                if (padding == default)
                {
                    padding = windowSize / (maxLimit * 5);
                }

                var subscription = source.Publish(ps =>
                {
                    LinkedList<DateTimeOffset> linkedTimestampList = default;

                    var i = 0;
                    var coolDownCounter = 0;
                    var comingOffOfQueue = false;
                    var eventLooptScheduler = new EventLoopScheduler();

                    return source
                        .Timestamp()
                        .SlidingWindow(windowSize)
                        .Select(RateEnforce)
                        .Where(entity => entity != Observable.Empty<T>())
                        .SelectMany(entity => entity);

                    IObservable<T> RateEnforce(IReadOnlyList<Timestamped<T>> timestampedWindowList)
                    {
                        if (i >= bufferSize)
                        {
                            return Observable.Empty<T>();
                        }

                        if (timestampedWindowList.Count >= maxLimit || i > 0)
                        {
                            i++;

                            if (i == 1)
                            {
                                linkedTimestampList = new LinkedList<DateTimeOffset>(timestampedWindowList.Select(x => x.Timestamp));
                                linkedTimestampList.RemoveLast();

                                comingOffOfQueue = true;
                            }

                            var nextAcceptableExecutionTime = GetNextTimestampHonoringRateLimit();

                            linkedTimestampList.AddLast(nextAcceptableExecutionTime.Value);
                            linkedTimestampList.RemoveFirst();

                            return Observable.Return(timestampedWindowList.ElementAt(timestampedWindowList.Count - 1).Value)
                                .Delay(nextAcceptableExecutionTime.Value, eventLooptScheduler)
                                .Do(_ => i--);
                        }
                        else
                        {
                            if (comingOffOfQueue)
                            {
                                coolDownCounter = 3;
                                comingOffOfQueue = false;
                            }

                            if (coolDownCounter > 0)
                            {
                                return Observable.Return(timestampedWindowList.ElementAt(timestampedWindowList.Count - 1).Value)
                                .Delay((windowSize / maxLimit), eventLooptScheduler)
                                .Do(x => coolDownCounter--);
                            }

                            return Observable.Return(timestampedWindowList.ElementAt(timestampedWindowList.Count - 1).Value);
                        }
                    }

                    DateTimeOffset? GetNextTimestampHonoringRateLimit()
                    {
                        var lastActionInWindow = linkedTimestampList.Last();
                        var firstActionInWindow = linkedTimestampList.First();
                        var timeUntilLimitNotExceeded = windowSize - (lastActionInWindow - firstActionInWindow);

                        return lastActionInWindow + timeUntilLimitNotExceeded + padding;
                    }
                })
                .Subscribe(observer);

                return subscription;
            });
        }
    }
}
