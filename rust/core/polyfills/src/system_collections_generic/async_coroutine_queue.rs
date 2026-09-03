// PORT-SOURCE: Core/OpenStack.Polyfills/System.Collections.Generic/AsyncCoroutineQueue.cs
// PORT-SHA: 6e00223075cee917
// PORT-STATUS: done
//
// The C# is `CoroutineQueue` with `IAsyncEnumerator<object>` in place of
// `IEnumerator` and `await MoveNextAsync()` in place of `MoveNext()`. It
// carries both of that file's hazards verbatim: head-of-line starvation in
// `Run`, and `WaitForAll` mutating `Tasks` while iterating it.
//
// NOT PORTED, deliberately. Rust has no stable `AsyncIterator`/`Stream` in
// `std`, so a faithful port means either picking an async runtime for the whole
// workspace (`futures::Stream`, `tokio`) or hand-rolling a `poll_next` trait —
// a choice that should follow a real async caller, not precede one. There is
// none yet: nothing outside this file references `AsyncCoroutineQueue`.
//
// When one appears, the shape is `CoroutineQueue` with `Task` redefined as
// `Pin<Box<dyn Stream<Item = ()>>>` and `next()` awaited; the queue logic in
// `coroutine_queue.rs` transfers unchanged. Fix the two hazards there at the
// same time rather than reproducing them here.
