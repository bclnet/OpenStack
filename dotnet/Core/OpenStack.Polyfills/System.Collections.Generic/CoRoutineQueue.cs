using System.Collections.Generic;
using System.Diagnostics;
using static System.Diagnostics.Debug;

namespace System.Collections;

/// <summary>
/// Distributes work (the execution of coroutines) over several frames to avoid freezes by soft-limiting execution time.
/// </summary>
public class CoroutineQueue {
    List<IEnumerator> _tasks = [];
    Stopwatch _watch = new();

    /// <summary>
    /// Adds a task coroutine and returns it.
    /// </summary>
    public IEnumerator Add(IEnumerator task) { _tasks.Add(task); return task; }

    public void Cancel(IEnumerator task) => _tasks.Remove(task);

    public void Clear() => _tasks.Clear();

    public void Run(float desiredWorkTime) {
        Assert(desiredWorkTime >= 0);
        if (_tasks.Count == 0) return;
        _watch.Reset(); _watch.Start();
        do {
            // try to execute an iteration of a task. remove the task if it's execution has completed.
            if (!_tasks[0].MoveNext()) _tasks.RemoveAt(0);
        } while (_tasks.Count > 0 && _watch.Elapsed.TotalSeconds < desiredWorkTime);
        _watch.Stop();
    }

    public void WaitFor(IEnumerator task) { Assert(_tasks.Contains(task)); while (task.MoveNext()) { } _tasks.Remove(task); }

    public void WaitForAll() { foreach (var task in _tasks) while (task.MoveNext()) { } _tasks.Clear(); }
}
