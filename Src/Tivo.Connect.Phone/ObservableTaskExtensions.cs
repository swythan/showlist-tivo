using System;
using System.Reactive.Subjects;

namespace Tivo.Connect
{
    public static class ObservableTaskExtensions
    {
        public static AwaitableAsyncSubject<TSource> GetAwaiter<TSource>(this IObservable<TSource> source)
        {
            var s = new AwaitableAsyncSubject<TSource>();
            source.SubscribeSafe(s);
            return s;
        }

        public static AwaitableAsyncSubject<TSource> GetAwaiter<TSource>(this IConnectableObservable<TSource> source)
        {
            var s = new AwaitableAsyncSubject<TSource>();
            source.SubscribeSafe(s);
            source.Connect();
            return s;
        }
    }
}
