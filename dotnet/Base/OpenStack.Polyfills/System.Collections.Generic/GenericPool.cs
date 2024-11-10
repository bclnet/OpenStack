using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public delegate void GenericPoolAction<T>(Action<T> action);
    public delegate TResult GenericPoolFunc<T, TResult>(Func<T, TResult> action);

    public interface IGenericPool<T>
    {
        T Get();
        void Release(T item);
        void Action(Action<T> action);
        TResult Func<TResult>(Func<T, TResult> action);
        Task ActionAsync(Func<T, Task> action);
        Task<TResult> FuncAsync<TResult>(Func<T, Task<TResult>> action);
    }

    public class GenericPool<T>(Func<T> factory, Action<T> reset = null, int retainInPool = 10) : IGenericPool<T>, IDisposable where T : IDisposable
    {
        readonly ConcurrentBag<T> items = [];
        public readonly Func<T> Factory = factory;
        public readonly int RetainInPool = retainInPool;

        public void Dispose()
        {
            foreach (var item in items) item.Dispose();
        }

        public virtual T Get() => items.TryTake(out var item) ? item : Factory();

        public virtual void Release(T item)
        {
            if (items.Count < RetainInPool) { reset?.Invoke(item); items.Add(item); }
            else item.Dispose();
        }

        public void Action(Action<T> action)
        {
            var item = Get();
            try { action(item); }
            finally { Release(item); }
        }

        public TResult Func<TResult>(Func<T, TResult> action)
        {
            var item = Get();
            try { return action(item); }
            finally { Release(item); }
        }

        public Task ActionAsync(Func<T, Task> action)
        {
            var item = Get();
            try { action(item); return Task.CompletedTask; }
            finally { Release(item); }
        }

        public Task<TResult> FuncAsync<TResult>(Func<T, Task<TResult>> action)
        {
            var item = Get();
            try { return action(item); }
            finally { Release(item); }
        }
    }

    public class SinglePool<T>(T single, Action<T> reset = null) : GenericPool<T>(null, reset) where T : IDisposable
    {
        public readonly T Single = single;
        public override T Get() => Single;
        public override void Release(T item) { Single.Dispose(); }
    }

    public class StaticPool<T>(T @static, Action<T> reset = null) : GenericPool<T>(null, reset) where T : IDisposable
    {
        public readonly T Static = @static;
        public override T Get() => Static;
        public override void Release(T item) { reset?.Invoke(item); }
    }
}