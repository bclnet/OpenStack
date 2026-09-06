// PORT-SOURCE: Core/OpenStack/Profiler.cs
// PORT-SHA: 1dbe3bc7e7a315d7
// PORT-STATUS: done
//
// A frame profiler: nested named contexts, each keeping a 60-sample rolling
// window of durations.
//
// ===================== SIX C#-SIDE BUGS ===================================
//
//   1. **`LastTime` returns the oldest sample, not the newest.**
//
//          public double LastTime => m_LastTimes[LastIndex % ProfileTimeCount];
//
//      `AddNewHitLength` writes at `LastIndex % N` and *then* increments, so by
//      the time anyone reads `LastTime` the index has moved on — it points at
//      the slot 60 samples back. Construct a `ProfileData` and read `LastTime`
//      and you get 0.0, never the value you just passed in. **Fix this in the
//      C# tree.**
//
//   2. **`ExitContext` throws when no context is open.** `Context[^1]` on an
//      empty list is an `ArgumentOutOfRangeException`. Any mismatched
//      Enter/Exit — an early return, an exception unwinding past an
//      `ExitContext` — takes the process down from inside the profiler.
//
//   3. **`ExitContext` continues after detecting a mismatch.** It logs
//      "context_name does not match current context" and then pops anyway,
//      recording the elapsed time against the wrong context. The diagnostic
//      names the problem and the code proceeds to corrupt the data.
//
//   4. **`ProfileData.Empty` and `TotalTimeData` are built with a null
//      `Context`.** `MatchesContext`, `ToString`, and `GetContext`'s
//      `Context[^1]` all dereference it, so the value `GetContext` returns on a
//      miss throws if the caller does anything with it.
//
//   5. **`AverageTime` always divides by 60**, even with three samples
//      recorded, so every average is understated until the window fills.
//
//   6. **Everything is unsynchronised mutable static state** — `Enabled`,
//      `Context`, `ThisFrameData`, `AllFrameData`. Profiling from more than one
//      thread corrupts the lists. `AllFrameData` also grows without bound, one
//      entry per distinct context path, never cleared.
//
// The port makes the profiler an owned value with `&mut self` methods, so (6) is
// a compile error rather than a data race. A process-wide instance is available
// behind a lock for callers that want the C#'s static feel.

use std::collections::HashMap;
use std::sync::{Mutex, OnceLock};
use std::time::{Duration, Instant};

/// C# `Profiler.ProfileTimeCount`.
pub const PROFILE_TIME_COUNT: usize = 60;

/// C# `Profiler.ProfileData` — a rolling window of durations for one context.
#[derive(Debug, Clone)]
pub struct ProfileData {
    /// C# `Context`, never null here — see bug 4.
    pub context: Vec<String>,
    times: [f64; PROFILE_TIME_COUNT],
    /// Total samples ever added, so `average_time` can divide correctly.
    count: u64,
}

impl ProfileData {
    /// C# `ProfileData(string[] context, double time)`.
    pub fn new(context: Vec<String>, time_ms: f64) -> Self {
        let mut d = Self { context, times: [0.0; PROFILE_TIME_COUNT], count: 0 };
        d.add_hit(time_ms);
        d
    }

    /// C# `ProfileData.Empty`, with an empty context rather than a null one.
    pub fn empty() -> Self {
        Self { context: Vec::new(), times: [0.0; PROFILE_TIME_COUNT], count: 0 }
    }

    /// C# `AddNewHitLength(double time)`.
    pub fn add_hit(&mut self, time_ms: f64) {
        self.times[(self.count as usize) % PROFILE_TIME_COUNT] = time_ms;
        self.count += 1;
    }

    /// C# `LastTime` — the most recent sample.
    ///
    /// Indexes `count - 1`, not `count`; the C# used the post-increment value
    /// and so returned the oldest slot. See bug 1.
    pub fn last_time(&self) -> f64 {
        if self.count == 0 {
            return 0.0;
        }
        self.times[((self.count - 1) as usize) % PROFILE_TIME_COUNT]
    }

    /// C# `TimeInContext` — sum of the window.
    pub fn time_in_context(&self) -> f64 {
        self.times.iter().sum()
    }

    /// C# `AverageTime`.
    ///
    /// Divides by the number of samples actually recorded (capped at the window
    /// size), not unconditionally by 60. See bug 5.
    pub fn average_time(&self) -> f64 {
        let n = (self.count as usize).min(PROFILE_TIME_COUNT);
        if n == 0 {
            return 0.0;
        }
        self.time_in_context() / n as f64
    }

    /// C# `MatchesContext(string[] context)`.
    pub fn matches_context(&self, context: &[String]) -> bool {
        self.context == context
    }

    /// Samples recorded so far. No C# equivalent; needed to make
    /// `average_time` honest.
    pub fn sample_count(&self) -> u64 {
        self.count
    }
}

impl std::fmt::Display for ProfileData {
    /// C# `ToString()` — `a:b:c - 12.3ms`.
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{} - {:.1}ms", self.context.join(":"), self.time_in_context())
    }
}

/// C# `Profiler.ContextAndTick`.
#[derive(Debug, Clone)]
struct ContextAndTick {
    name: String,
    at: Instant,
}

/// C# `Profiler`'s error cases, which it either swallowed or threw on.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum ProfilerError {
    /// C#: `Context[^1]` on an empty list threw. See bug 2.
    NoOpenContext { closing: String },
    /// C#: logged, then popped anyway. See bug 3.
    ContextMismatch { expected: String, got: String },
}

impl std::fmt::Display for ProfilerError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            ProfilerError::NoOpenContext { closing } => {
                write!(f, "exit_context({closing}) with no context open")
            }
            ProfilerError::ContextMismatch { expected, got } => {
                write!(f, "exit_context({got}) but the open context is {expected}")
            }
        }
    }
}

impl std::error::Error for ProfilerError {}

/// C# `static class Profiler`, as an owned value.
pub struct Profiler {
    /// C# `Enabled`.
    pub enabled: bool,
    context: Vec<ContextAndTick>,
    this_frame: Vec<(Vec<String>, f64)>,
    /// C# `AllFrameData`, keyed so lookup is not a linear `MatchesContext` scan.
    all_frames: HashMap<Vec<String>, ProfileData>,
    total: ProfileData,
    frame_start: Option<Instant>,
    last_frame_ms: f64,
}

impl Default for Profiler {
    fn default() -> Self {
        Self {
            enabled: false, // C# `Enabled = false`
            context: Vec::new(),
            this_frame: Vec::new(),
            all_frames: HashMap::new(),
            total: ProfileData::empty(),
            frame_start: None,
            last_frame_ms: 0.0,
        }
    }
}

impl Profiler {
    pub fn new() -> Self {
        Self::default()
    }

    /// C# `LastFrameTimeMS`.
    #[inline]
    pub fn last_frame_ms(&self) -> f64 {
        self.last_frame_ms
    }

    /// C# `TrackedTime`.
    #[inline]
    pub fn tracked_time(&self) -> f64 {
        self.total.time_in_context()
    }

    /// C# `BeginFrame()` — fold the previous frame's samples into the totals.
    pub fn begin_frame(&mut self) {
        if !self.enabled {
            return;
        }
        for (ctx, ms) in self.this_frame.drain(..) {
            match self.all_frames.get_mut(&ctx) {
                Some(d) => d.add_hit(ms),
                None => {
                    self.all_frames.insert(ctx.clone(), ProfileData::new(ctx, ms));
                }
            }
        }
        self.frame_start = Some(Instant::now());
    }

    /// C# `EndFrame()`.
    pub fn end_frame(&mut self) {
        if !self.enabled {
            return;
        }
        // The C# subtracts from `BeginFrameTicks`, which is 0 before the first
        // `BeginFrame` — so the first `EndFrame` reports time since process
        // start. `Option` makes that case explicit.
        let Some(start) = self.frame_start else { return };
        self.last_frame_ms = start.elapsed().as_secs_f64() * 1000.0;
        self.total.add_hit(self.last_frame_ms);
    }

    /// C# `EnterContext(string)`.
    pub fn enter_context(&mut self, name: impl Into<String>) {
        if !self.enabled {
            return;
        }
        self.context.push(ContextAndTick { name: name.into(), at: Instant::now() });
    }

    /// C# `ExitContext(string)`.
    ///
    /// Returns an error instead of throwing on an empty stack, and **does not
    /// record anything** on a mismatch — see bugs 2 and 3. The context stack is
    /// left untouched in both cases, so a caller can recover.
    pub fn exit_context(&mut self, name: &str) -> Result<(), ProfilerError> {
        if !self.enabled {
            return Ok(());
        }
        let Some(top) = self.context.last() else {
            return Err(ProfilerError::NoOpenContext { closing: name.to_string() });
        };
        if top.name != name {
            return Err(ProfilerError::ContextMismatch {
                expected: top.name.clone(),
                got: name.to_string(),
            });
        }
        let elapsed = top.at.elapsed().as_secs_f64() * 1000.0;
        let path: Vec<String> = self.context.iter().map(|c| c.name.clone()).collect();
        self.this_frame.push((path, elapsed));
        self.context.pop();
        Ok(())
    }

    /// C# `InContext(string)`.
    pub fn in_context(&self, name: &str) -> bool {
        self.enabled && self.context.last().map(|c| c.name.as_str()) == Some(name)
    }

    /// C# `GetContext(string)` — the deepest recorded context with this leaf
    /// name. `None` on a miss, where the C# returned a `ProfileData` whose null
    /// `Context` throws on use.
    pub fn get_context(&self, name: &str) -> Option<&ProfileData> {
        if !self.enabled {
            return None;
        }
        self.all_frames
            .values()
            .find(|d| d.context.last().map(String::as_str) == Some(name))
        }

    /// Discard accumulated per-context history. No C# equivalent — its
    /// `AllFrameData` grew for the life of the process (bug 6).
    pub fn clear_history(&mut self) {
        self.all_frames.clear();
        self.this_frame.clear();
    }

    /// Every recorded context, for a report. Sorted so output is stable.
    pub fn contexts(&self) -> Vec<&ProfileData> {
        let mut v: Vec<&ProfileData> = self.all_frames.values().collect();
        v.sort_by(|a, b| a.context.cmp(&b.context));
        v
    }

    /// A scope guard: enters on construction, exits on drop.
    ///
    /// Not in the C#, and it is the fix for bugs 2 and 3 at the source — an
    /// early return or a panic cannot skip the exit, so the stack cannot
    /// desynchronise in the first place.
    pub fn scope<'a>(&'a mut self, name: &str) -> ProfileScope<'a> {
        self.enter_context(name);
        ProfileScope { profiler: self, name: name.to_string() }
    }
}

/// RAII context guard from [`Profiler::scope`].
pub struct ProfileScope<'a> {
    profiler: &'a mut Profiler,
    name: String,
}

impl Drop for ProfileScope<'_> {
    fn drop(&mut self) {
        // A mismatch here is impossible by construction, so the result is safe
        // to discard.
        let _ = self.profiler.exit_context(&self.name);
    }
}

/// Process-wide profiler, for callers porting from the C#'s statics.
pub fn global() -> &'static Mutex<Profiler> {
    static P: OnceLock<Mutex<Profiler>> = OnceLock::new();
    P.get_or_init(|| Mutex::new(Profiler::new()))
}

/// Convenience for `Duration` -> milliseconds, matching the C#'s
/// `ticks * 1000 / Stopwatch.Frequency`.
#[inline]
pub fn to_ms(d: Duration) -> f64 {
    d.as_secs_f64() * 1000.0
}

#[cfg(test)]
mod tests {
    use super::*;

    fn enabled() -> Profiler {
        let mut p = Profiler::new();
        p.enabled = true;
        p
    }

    #[test]
    fn last_time_returns_the_newest_sample() {
        // The C# returns the slot 60 samples back, so this reads 0.0 there.
        let mut d = ProfileData::new(vec!["a".into()], 5.0);
        assert_eq!(d.last_time(), 5.0);
        d.add_hit(9.0);
        assert_eq!(d.last_time(), 9.0);
    }

    #[test]
    fn average_divides_by_samples_recorded_not_by_sixty() {
        let mut d = ProfileData::new(vec!["a".into()], 10.0);
        assert_eq!(d.average_time(), 10.0);
        d.add_hit(20.0);
        assert_eq!(d.average_time(), 15.0);
        // The C# would report 30/60 = 0.5 here.
    }

    #[test]
    fn window_wraps_at_sixty_samples() {
        let mut d = ProfileData::new(vec!["a".into()], 1.0);
        for _ in 0..PROFILE_TIME_COUNT {
            d.add_hit(2.0);
        }
        assert_eq!(d.time_in_context(), 2.0 * PROFILE_TIME_COUNT as f64);
        assert_eq!(d.average_time(), 2.0, "the initial 1.0 has been evicted");
    }

    #[test]
    fn empty_profile_data_is_safe_to_use() {
        // The C#'s Empty has a null Context; ToString and GetContext throw.
        let e = ProfileData::empty();
        assert_eq!(e.last_time(), 0.0);
        assert_eq!(e.average_time(), 0.0);
        assert_eq!(e.to_string(), " - 0.0ms");
        assert!(!e.matches_context(&["a".to_string()]));
    }

    #[test]
    fn exit_without_enter_is_an_error_not_a_panic() {
        let mut p = enabled();
        assert_eq!(
            p.exit_context("nope"),
            Err(ProfilerError::NoOpenContext { closing: "nope".into() })
        );
    }

    #[test]
    fn mismatched_exit_records_nothing_and_leaves_the_stack() {
        // The C# logs and then pops anyway, attributing the time to the wrong
        // context.
        let mut p = enabled();
        p.enter_context("outer");
        let e = p.exit_context("inner").unwrap_err();
        assert!(matches!(e, ProfilerError::ContextMismatch { .. }));
        assert!(p.in_context("outer"), "stack must be untouched");
        p.begin_frame();
        assert!(p.get_context("inner").is_none(), "nothing recorded");
    }

    #[test]
    fn nested_contexts_record_their_full_path() {
        let mut p = enabled();
        p.enter_context("frame");
        p.enter_context("draw");
        p.exit_context("draw").unwrap();
        p.exit_context("frame").unwrap();
        p.begin_frame();
        let d = p.get_context("draw").unwrap();
        assert_eq!(d.context, vec!["frame".to_string(), "draw".to_string()]);
    }

    #[test]
    fn repeated_frames_accumulate_into_one_entry() {
        let mut p = enabled();
        for _ in 0..3 {
            p.enter_context("work");
            p.exit_context("work").unwrap();
            p.begin_frame();
        }
        assert_eq!(p.get_context("work").unwrap().sample_count(), 3);
        assert_eq!(p.contexts().len(), 1, "one context, three samples");
    }

    #[test]
    fn disabled_profiler_records_nothing() {
        let mut p = Profiler::new(); // enabled defaults to false
        p.enter_context("x");
        assert!(p.exit_context("x").is_ok());
        p.begin_frame();
        assert!(p.get_context("x").is_none());
        assert!(!p.in_context("x"));
    }

    #[test]
    fn scope_guard_cannot_desynchronise_the_stack() {
        let mut p = enabled();
        {
            let _g = p.scope("guarded");
        }
        assert!(!p.in_context("guarded"), "drop must have exited");
        p.begin_frame();
        assert!(p.get_context("guarded").is_some());
    }

    #[test]
    fn history_can_be_cleared() {
        let mut p = enabled();
        p.enter_context("a");
        p.exit_context("a").unwrap();
        p.begin_frame();
        assert_eq!(p.contexts().len(), 1);
        p.clear_history();
        assert!(p.contexts().is_empty());
    }

    #[test]
    fn end_frame_before_begin_frame_does_not_report_process_uptime() {
        // The C# subtracts from BeginFrameTicks == 0, reporting time since the
        // Stopwatch started.
        let mut p = enabled();
        p.end_frame();
        assert_eq!(p.last_frame_ms(), 0.0);
    }
}
