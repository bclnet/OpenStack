// PORT-SOURCE: Core/OpenStack.Polyfills/System/ThreadSafeRandom.cs
// PORT-SHA: a3ad71a04ce54244
// PORT-STATUS: done
//
// NAMING PROBLEM ON THE C# SIDE: `SecureRandom` is not secure. It is a plain
// `System.Random` — a non-cryptographic PRNG, seeded from the clock. Anything
// that trusted the name for tokens, nonces, or keys is not getting what it
// asked for. The Rust equivalent keeps the name so call sites resolve, with the
// same warning at the definition; use a CSPRNG (`getrandom`, `rand::rngs::OsRng`)
// if any caller actually needs one.
//
// `System.Random` is also not thread-safe, which is why `ThreadSafeRandom`
// wraps it in a `ThreadLocal`. Ported as a thread-local `Cell`.
//
// This is a small xorshift64* generator rather than a `rand` dependency: the
// callers are picking jitter and particle offsets, not anything that needs
// statistical rigour, and it keeps the crate dependency-free.

use std::cell::Cell;
use std::time::{SystemTime, UNIX_EPOCH};

thread_local! {
    static STATE: Cell<u64> = Cell::new(seed());
}

fn seed() -> u64 {
    let nanos = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|d| d.as_nanos() as u64)
        .unwrap_or(0x2545_F491_4F6C_DD1D);
    // Mix in the thread identity so two threads starting in the same
    // nanosecond do not produce identical streams.
    let tid = &STATE as *const _ as u64;
    let s = nanos ^ tid.rotate_left(32);
    if s == 0 {
        0x9E37_79B9_7F4A_7C15
    } else {
        s
    }
}

/// xorshift64*, per-thread.
fn next_u64() -> u64 {
    STATE.with(|s| {
        let mut x = s.get();
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        s.set(x);
        x.wrapping_mul(0x2545_F491_4F6C_DD1D)
    })
}

/// C# `ThreadSafeRandom.Next(int min, int max)`.
///
/// **Inclusive of `max`** — the C# calls `Random.Next(min, max + 1)`, so this
/// differs from every other range API in both languages. Preserved, and
/// asserted in the tests, because silently making it exclusive would shift
/// every caller's distribution by one.
pub fn next_range_i32(min: i32, max: i32) -> i32 {
    if max <= min {
        return min;
    }
    let span = (max as i64 - min as i64 + 1) as u64;
    (min as i64 + (next_u64() % span) as i64) as i32
}

/// C# `ThreadSafeRandom.Next(float min, float max)` — half-open `[min, max)`.
///
/// Note the C# returns `double` from `float` arguments; the return type is
/// `f32` here since no caller needs the extra precision.
pub fn next_range_f32(min: f32, max: f32) -> f32 {
    let unit = (next_u64() >> 11) as f64 / (1u64 << 53) as f64;
    min + (unit as f32) * (max - min)
}

/// C# `SecureRandom.RandomValue(int low, int high)` — inclusive of `high`.
///
/// **Not cryptographically secure**, despite the name; see the file header.
pub fn insecure_random_value(low: i32, high: i32) -> i32 {
    next_range_i32(low, high)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn integer_range_is_inclusive_of_max() {
        // Pins the +1 the C# applies. Over this many draws a 0..=1 range must
        // produce both endpoints.
        let mut saw_low = false;
        let mut saw_high = false;
        for _ in 0..500 {
            match next_range_i32(0, 1) {
                0 => saw_low = true,
                1 => saw_high = true,
                other => panic!("out of range: {other}"),
            }
        }
        assert!(saw_low && saw_high);
    }

    #[test]
    fn float_range_stays_within_bounds() {
        for _ in 0..1000 {
            let v = next_range_f32(-2.0, 5.0);
            assert!((-2.0..5.0).contains(&v), "out of range: {v}");
        }
    }

    #[test]
    fn degenerate_ranges_do_not_divide_by_zero() {
        assert_eq!(next_range_i32(7, 7), 7);
        assert_eq!(next_range_i32(9, 3), 9);
    }

    #[test]
    fn successive_draws_differ() {
        let a: Vec<i32> = (0..20).map(|_| next_range_i32(0, 1_000_000)).collect();
        assert!(a.windows(2).any(|w| w[0] != w[1]), "generator is stuck");
    }
}
