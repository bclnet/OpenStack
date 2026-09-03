// PORT-SOURCE: Core/OpenStack.Polyfills/System.Collections.Generic/GenericPoolX.cs
// PORT-SHA: 0539af1ef504d469
// PORT-STATUS: done
//
// An object pool with a factory, an optional reset, and a retain cap.
//
// THREE C#-SIDE BUGS, two of them use-after-release races:
//
//   1. `ActionAsync(Func<T, Task> action)` is
//      `try { action(item); return Task.CompletedTask; } finally { Release(item); }`
//      — it never awaits. The item goes back in the pool while the async work
//      is still using it, so another thread can `Get()` it mid-flight. It also
//      returns `CompletedTask`, so callers cannot await real completion and
//      exceptions are swallowed.
//   2. `FuncAsync` has the same shape: `finally { Release(item); }` runs when
//      the task is *created*, not when it completes.
//   3. `SinglePool<T>.Release` calls `Single.Dispose()` on **every** release,
//      so the second `Get()` hands back a disposed object.
//
// Rust makes 1 and 2 unrepresentable: a `PoolGuard` holds the item and returns
// it on drop, so the borrow cannot outlive the checkout. Async callers hold the
// guard across the await like any other value.

use std::sync::Mutex;

/// C# `IGenericPool<T>` / `GenericPoolX<T>`.
pub struct GenericPool<T> {
    items: Mutex<Vec<T>>,
    factory: Box<dyn Fn() -> T + Send + Sync>,
    reset: Option<Box<dyn Fn(&mut T) + Send + Sync>>,
    retain_in_pool: usize,
}

impl<T> GenericPool<T> {
    /// C# `GenericPoolX(Func<T> factory, Action<T> reset = null, int retainInPool = 10)`.
    pub fn new(factory: impl Fn() -> T + Send + Sync + 'static) -> Self {
        Self {
            items: Mutex::new(Vec::new()),
            factory: Box::new(factory),
            reset: None,
            retain_in_pool: 10,
        }
    }

    pub fn with_reset(mut self, reset: impl Fn(&mut T) + Send + Sync + 'static) -> Self {
        self.reset = Some(Box::new(reset));
        self
    }

    pub fn with_retain(mut self, retain_in_pool: usize) -> Self {
        self.retain_in_pool = retain_in_pool;
        self
    }

    /// C# `Get()` — take from the pool, or build a new one.
    ///
    /// Prefer [`checkout`](Self::checkout); this leaves the caller responsible
    /// for calling `release`, which is the mistake the C# API invited.
    pub fn get(&self) -> T {
        let mut g = self.items.lock().unwrap_or_else(|p| p.into_inner());
        g.pop().unwrap_or_else(|| (self.factory)())
    }

    /// C# `Release(T item)` — keep it if there is room, else drop it.
    pub fn release(&self, mut item: T) {
        let mut g = self.items.lock().unwrap_or_else(|p| p.into_inner());
        if g.len() < self.retain_in_pool {
            if let Some(r) = &self.reset {
                r(&mut item);
            }
            g.push(item);
        }
        // Otherwise `item` drops here — C# called `Dispose()`, which is what
        // `Drop` does automatically.
    }

    /// The safe checkout. Replaces C# `Action`, `Func`, `ActionAsync`, and
    /// `FuncAsync` — all four existed only to pair `Get` with `Release`, and a
    /// guard does that without the caller writing a `finally`.
    ///
    /// Hold the guard across an `.await` for the async cases; the item cannot
    /// return to the pool while it is still borrowed.
    pub fn checkout(&self) -> PoolGuard<'_, T> {
        PoolGuard { pool: self, item: Some(self.get()) }
    }

    /// How many items are currently idle in the pool.
    pub fn idle(&self) -> usize {
        self.items.lock().unwrap_or_else(|p| p.into_inner()).len()
    }
}

/// Returns its item to the pool on drop.
pub struct PoolGuard<'a, T> {
    pool: &'a GenericPool<T>,
    item: Option<T>,
}

impl<T> std::ops::Deref for PoolGuard<'_, T> {
    type Target = T;
    fn deref(&self) -> &T {
        self.item.as_ref().expect("item taken")
    }
}

impl<T> std::ops::DerefMut for PoolGuard<'_, T> {
    fn deref_mut(&mut self) -> &mut T {
        self.item.as_mut().expect("item taken")
    }
}

impl<T> Drop for PoolGuard<'_, T> {
    fn drop(&mut self) {
        if let Some(item) = self.item.take() {
            self.pool.release(item);
        }
    }
}

/// C# `StaticPool<T>` — one shared instance, reset on release, never disposed.
pub struct StaticPool<T> {
    value: Mutex<T>,
}

impl<T> StaticPool<T> {
    pub fn new(value: T) -> Self {
        Self { value: Mutex::new(value) }
    }

    /// Locks for the duration of the closure, since a single shared instance
    /// cannot be handed to two callers at once — a constraint the C# version
    /// ignored entirely.
    pub fn with<R>(&self, f: impl FnOnce(&mut T) -> R) -> R {
        let mut g = self.value.lock().unwrap_or_else(|p| p.into_inner());
        f(&mut g)
    }
}

// NOT PORTED: `SinglePool<T>`. Its `Release` disposes the single instance on
// every call, so the second `Get()` returns a disposed object — the type cannot
// be used more than once as written. `StaticPool` covers the intent.

#[cfg(test)]
mod tests {
    use super::*;
    use std::sync::atomic::{AtomicUsize, Ordering};
    use std::sync::Arc;

    #[test]
    fn reuses_released_items() {
        let made = Arc::new(AtomicUsize::new(0));
        let m = made.clone();
        let pool = GenericPool::new(move || m.fetch_add(1, Ordering::SeqCst));
        {
            let _a = pool.checkout();
        }
        {
            let _b = pool.checkout();
        }
        assert_eq!(made.load(Ordering::SeqCst), 1, "second checkout must reuse");
    }

    #[test]
    fn guard_returns_the_item_on_drop() {
        let pool = GenericPool::new(Vec::<u8>::new);
        assert_eq!(pool.idle(), 0);
        {
            let mut g = pool.checkout();
            g.push(1);
            assert_eq!(pool.idle(), 0, "checked out, so not idle");
        }
        assert_eq!(pool.idle(), 1);
    }

    #[test]
    fn reset_runs_on_release() {
        let pool = GenericPool::new(|| vec![1u8, 2, 3]).with_reset(|v| v.clear());
        {
            let _g = pool.checkout();
        }
        assert!(pool.checkout().is_empty(), "reset should have cleared it");
    }

    #[test]
    fn retain_cap_is_honoured() {
        let pool = GenericPool::new(|| 0u32).with_retain(2);
        pool.release(1);
        pool.release(2);
        pool.release(3);
        assert_eq!(pool.idle(), 2);
    }

    #[test]
    fn two_checkouts_get_distinct_items() {
        let counter = Arc::new(AtomicUsize::new(0));
        let c = counter.clone();
        let pool = GenericPool::new(move || c.fetch_add(1, Ordering::SeqCst));
        let a = pool.checkout();
        let b = pool.checkout();
        assert_ne!(*a, *b, "an item must not be handed out twice");
    }

    #[test]
    fn static_pool_serialises_access() {
        let p = StaticPool::new(0u32);
        p.with(|v| *v += 1);
        p.with(|v| *v += 1);
        assert_eq!(p.with(|v| *v), 2);
    }
}
