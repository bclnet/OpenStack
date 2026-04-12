using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections;

/// <summary>
/// Distributes work (the execution of coroutines) over several frames to avoid freezes by soft-limiting execution time.
/// </summary>
public class CoroutineQueue {
    readonly List<IEnumerator> Tasks = [];
    readonly Stopwatch Time = new();

    /// <summary>
    /// Adds a task coroutine and returns it.
    /// </summary>
    public IEnumerator Add(IEnumerator task) { Tasks.Add(task); return task; }

    public void Cancel(IEnumerator task) => Tasks.Remove(task);

    public void Clear() => Tasks.Clear();

    public void Run(float desiredWorkTime) {
        Debug.Assert(desiredWorkTime >= 0);
        if (Tasks.Count == 0) return;
        Time.Reset(); Time.Start();
        // try to execute an iteration of a task. remove the task if it's execution has completed.
        do if (!Tasks[0].MoveNext()) Tasks.RemoveAt(0);
        while (Tasks.Count > 0 && Time.Elapsed.TotalSeconds < desiredWorkTime);
        Time.Stop();
    }

    public void WaitFor(IEnumerator task) { Debug.Assert(Tasks.Contains(task)); while (task.MoveNext()) { } Tasks.Remove(task); }

    public void WaitForAll() { foreach (var task in Tasks) while (task.MoveNext()) { } Tasks.Clear(); }
}
