using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Collections;

/// <summary>
/// Distributes work (the execution of coroutines) over several frames to avoid freezes by soft-limiting execution time.
/// </summary>
public class AsyncCoroutineQueue {
    readonly List<IAsyncEnumerator<object>> Tasks = [];
    readonly Stopwatch Time = new();

    /// <summary>
    /// Adds a task coroutine and returns it.
    /// </summary>
    public IAsyncEnumerator<object> Add(IAsyncEnumerator<object> task) { Tasks.Add(task); return task; }

    public void Cancel(IAsyncEnumerator<object> task) => Tasks.Remove(task);

    public void Clear() => Tasks.Clear();

    public async Task Run(float desiredWorkTime) {
        Debug.Assert(desiredWorkTime >= 0);
        if (Tasks.Count == 0) return;
        Time.Reset(); Time.Start();
        // try to execute an iteration of a task. remove the task if it's execution has completed.
        do if (!await Tasks[0].MoveNextAsync()) Tasks.RemoveAt(0);
        while (Tasks.Count > 0 && Time.Elapsed.TotalSeconds < desiredWorkTime);
        Time.Stop();
    }

    public async Task WaitFor(IAsyncEnumerator<object> task) { Debug.Assert(Tasks.Contains(task)); while (await task.MoveNextAsync()) { } Tasks.Remove(task); }

    public async Task WaitForAll() { foreach (var task in Tasks) while (await task.MoveNextAsync()) { } Tasks.Clear(); }
}
