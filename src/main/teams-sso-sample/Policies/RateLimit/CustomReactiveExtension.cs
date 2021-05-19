using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

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

        public static IObservable<T> Pausable<T>(this IObservable<T> source,
            IObservable<bool> pauser,
            int bufferSize = 0) =>
                Observable.Create<T>(o =>
                {
                    var paused = new SerialDisposable();

                    var subscription = source.Publish(ps =>
                    {
                        var values = bufferSize > 0
                            ? new ReplaySubject<T>(bufferSize)
                            : new ReplaySubject<T>();

                        return pauser.StartWith(true)
                            .DistinctUntilChanged()
                            .Select(Switcher)
                            .Switch();

                        IObservable<T> Switcher(bool b)
                        {
                            if (b)
                            {
                                values.Dispose();

                                values = bufferSize > 0
                                    ? new ReplaySubject<T>(bufferSize)
                                    : new ReplaySubject<T>();

                                paused.Disposable = ps.Subscribe(values);

                                return Observable.Empty<T>();
                            }

                            return values.Concat(ps);
                        }
                    })
                        .Subscribe(o);

                    return new CompositeDisposable(subscription, paused);
                });
    }
}
