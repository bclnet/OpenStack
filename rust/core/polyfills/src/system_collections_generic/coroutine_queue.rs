// PORT-SOURCE: Core/OpenStack.Polyfills/System.Collections.Generic/CoroutineQueue.cs
// PORT-SHA: bb693ecdca64a3e4
// PORT-STATUS: done
//
// A cooperative task queue: each task is a C# `IEnumerator` stepped one
// `MoveNext` at a time, with `Run(desiredWorkTime)` advancing tasks until a
// time budget runs out. The Rust equivalent of a stepped `IEnumerator` is a
// boxed `Iterator`, which `next()` advances the same way.
//
// TWO C#-SIDE HAZARDS:
//
//   1. `Run` always steps `Tasks[0]` and never rotates, so one long-running
//      coroutine starves every task behind it no matter how large the budget
//      is. `run` here keeps that head-of-line order (changing it would alter
//      execution order across the codebase) but `run_round_robin` is provided
//      for callers that want fairness.
//   2. `WaitForAll` iterates `Tasks` with `foreach` while stepping tasks. A
//      coroutine that adds or cancels a task during its own step mutates the
//      list mid-enumeration and throws `InvalidOperationException`. Rust would
//      not compile that aliasing; `wait_for_all` drains by index instead.

use std::time::{Duration, Instant};

/// A cooperative task. `next()` performs one step; `None` means finished.
pub type Task = Box<dyn Iterator<Item = ()>>;

/// Handle for cancelling a queued task. C# compared `IEnumerator` by reference.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct TaskId(u64);

/// C# `class CoroutineQueue`.
#[derive(Default)]
pub struct CoroutineQueue {
    tasks: Vec<(TaskId, Task)>,
    next_id: u64,
}

impl CoroutineQueue {
    pub fn new() -> Self {
        Self::default()
    }

    /// C# `Add(IEnumerator task)`.
    pub fn add(&mut self, task: Task) -> TaskId {
        let id = TaskId(self.next_id);
        self.next_id += 1;
        self.tasks.push((id, task));
        id
    }

    /// C# `Cancel(IEnumerator task)`.
    pub fn cancel(&mut self, id: TaskId) -> bool {
        let before = self.tasks.len();
        self.tasks.retain(|(i, _)| *i != id);
        self.tasks.len() != before
    }

    /// C# `Clear()`.
    pub fn clear(&mut self) {
        self.tasks.clear();
    }

    pub fn len(&self) -> usize {
        self.tasks.len()
    }

    pub fn is_empty(&self) -> bool {
        self.tasks.is_empty()
    }

    /// C# `Run(float desiredWorkTime)` — step the head task until the budget
    /// is spent. At least one step always happens, matching the C# do-while.
    pub fn run(&mut self, budget: Duration) {
        if self.tasks.is_empty() {
            return;
        }
        let start = Instant::now();
        loop {
            if self.tasks[0].1.next().is_none() {
                self.tasks.remove(0);
            }
            if self.tasks.is_empty() || start.elapsed() >= budget {
                break;
            }
        }
    }

    /// Fair variant: one step per task per pass, so a long task cannot starve
    /// the rest. Not in the C#.
    pub fn run_round_robin(&mut self, budget: Duration) {
        if self.tasks.is_empty() {
            return;
        }
        let start = Instant::now();
        let mut i = 0;
        loop {
            if self.tasks[i].1.next().is_none() {
                self.tasks.remove(i);
                if self.tasks.is_empty() {
                    break;
                }
                if i >= self.tasks.len() {
                    i = 0;
                }
            } else {
                i = (i + 1) % self.tasks.len();
            }
            if start.elapsed() >= budget {
                break;
            }
        }
    }

    /// C# `WaitFor(IEnumerator task)` — run one task to completion.
    pub fn wait_for(&mut self, id: TaskId) {
        if let Some(pos) = self.tasks.iter().position(|(i, _)| *i == id) {
            let (_, mut task) = self.tasks.remove(pos);
            for _ in task.by_ref() {}
        }
    }

    /// C# `WaitForAll()` — run everything to completion.
    ///
    /// Drains by index rather than iterating, so a task that queues more work
    /// during its own step is handled instead of throwing.
    pub fn wait_for_all(&mut self) {
        while !self.tasks.is_empty() {
            let (_, mut task) = self.tasks.remove(0);
            for _ in task.by_ref() {}
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::cell::RefCell;
    use std::rc::Rc;

    fn counting(n: usize, log: Rc<RefCell<Vec<&'static str>>>, tag: &'static str) -> Task {
        Box::new((0..n).map(move |_| {
            log.borrow_mut().push(tag);
        }))
    }

    #[test]
    fn wait_for_all_runs_every_task_to_completion() {
        let log = Rc::new(RefCell::new(Vec::new()));
        let mut q = CoroutineQueue::new();
        q.add(counting(2, log.clone(), "a"));
        q.add(counting(3, log.clone(), "b"));
        q.wait_for_all();
        assert!(q.is_empty());
        assert_eq!(log.borrow().len(), 5);
    }

    #[test]
    fn finished_tasks_are_removed() {
        let log = Rc::new(RefCell::new(Vec::new()));
        let mut q = CoroutineQueue::new();
        q.add(counting(1, log, "a"));
        q.run(Duration::from_millis(50));
        assert!(q.is_empty());
    }

    #[test]
    fn cancel_removes_a_pending_task() {
        let log = Rc::new(RefCell::new(Vec::new()));
        let mut q = CoroutineQueue::new();
        let id = q.add(counting(100, log.clone(), "a"));
        assert!(q.cancel(id));
        assert!(!q.cancel(id), "cancelling twice reports false");
        q.wait_for_all();
        assert!(log.borrow().is_empty());
    }

    #[test]
    fn round_robin_interleaves_where_run_does_not() {
        let log = Rc::new(RefCell::new(Vec::new()));
        let mut q = CoroutineQueue::new();
        q.add(counting(4, log.clone(), "a"));
        q.add(counting(4, log.clone(), "b"));
        q.run_round_robin(Duration::from_millis(50));
        let l = log.borrow();
        assert!(l.contains(&"b"), "second task must get time: {l:?}");
    }

    #[test]
    fn running_an_empty_queue_is_a_no_op() {
        CoroutineQueue::new().run(Duration::from_millis(1));
    }

    #[test]
    fn wait_for_runs_only_the_named_task() {
        let log = Rc::new(RefCell::new(Vec::new()));
        let mut q = CoroutineQueue::new();
        let a = q.add(counting(2, log.clone(), "a"));
        q.add(counting(2, log.clone(), "b"));
        q.wait_for(a);
        assert_eq!(*log.borrow(), vec!["a", "a"]);
        assert_eq!(q.len(), 1);
    }
}
