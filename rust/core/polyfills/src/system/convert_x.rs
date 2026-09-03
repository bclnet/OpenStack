// PORT-SOURCE: Core/OpenStack.Polyfills/System/ConvertX.cs
// PORT-SHA: a600019bbb815710
// PORT-STATUS: done
//
// Lenient string parsing. Every C# method uses `TryParse` and returns the
// type's default on failure, so a malformed value is indistinguishable from a
// legitimately-zero one.
//
// Ported with both shapes: `to_*` keeps the C#'s swallow-and-default behaviour
// for call sites that depend on it, and `try_to_*` returns `Option` so new code
// can tell the two apart. Prefer the latter.

use std::time::Duration;

/// C# `ToBoolean(string)`.
pub fn to_bool(value: &str) -> bool {
    try_to_bool(value).unwrap_or(false)
}

/// .NET `bool.TryParse` accepts "true"/"false" in any case, trimmed.
pub fn try_to_bool(value: &str) -> Option<bool> {
    match value.trim().to_ascii_lowercase().as_str() {
        "true" => Some(true),
        "false" => Some(false),
        _ => None,
    }
}

/// C# `ToDouble(string)`.
pub fn to_f64(value: &str) -> f64 {
    try_to_f64(value).unwrap_or(0.0)
}

pub fn try_to_f64(value: &str) -> Option<f64> {
    value.trim().parse().ok()
}

/// C# `ToInt32(string)` — accepts a `0x` prefix for hex, decimal otherwise.
pub fn to_i32(value: &str) -> i32 {
    try_to_i32(value).unwrap_or(0)
}

pub fn try_to_i32(value: &str) -> Option<i32> {
    let v = value.trim();
    // C# checks StartsWith("0x") case-sensitively, so "0X" takes the decimal
    // path and fails. Preserved.
    if let Some(hex) = v.strip_prefix("0x") {
        u32::from_str_radix(hex, 16).ok().map(|u| u as i32)
    } else {
        v.parse().ok()
    }
}

/// C# `ToTimeSpan(string)`.
///
/// .NET's `TimeSpan.TryParse` accepts `[d.]hh:mm:ss[.fffffff]`. Only the
/// common `hh:mm:ss[.fff]` and `d.hh:mm:ss` forms are handled here; anything
/// else returns `None` rather than guessing.
///
/// `Duration` is unsigned, unlike `TimeSpan`; negative inputs return `None`.
pub fn try_to_duration(value: &str) -> Option<Duration> {
    let v = value.trim();
    if v.starts_with('-') {
        return None;
    }
    let (days, rest) = match v.split_once('.') {
        // A leading "d.hh:mm:ss" — the part before '.' is days only if what
        // follows still contains a ':'.
        Some((d, r)) if r.contains(':') => (d.parse::<u64>().ok()?, r),
        _ => (0, v),
    };
    let mut parts = rest.split(':');
    let h: u64 = parts.next()?.parse().ok()?;
    let m: u64 = parts.next()?.parse().ok()?;
    let s: f64 = parts.next()?.parse().ok()?;
    if parts.next().is_some() || m >= 60 || s >= 60.0 {
        return None;
    }
    Some(Duration::from_secs_f64(
        (days * 86400 + h * 3600 + m * 60) as f64 + s,
    ))
}

/// C# `ToTimeSpan(string)` with the default-on-failure behaviour.
pub fn to_duration(value: &str) -> Duration {
    try_to_duration(value).unwrap_or(Duration::ZERO)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parses_decimal_and_hex() {
        assert_eq!(to_i32("42"), 42);
        assert_eq!(to_i32("0xFF"), 255);
        assert_eq!(to_i32("-7"), -7);
    }

    #[test]
    fn uppercase_hex_prefix_fails_like_the_c_sharp() {
        // C# tests StartsWith("0x") case-sensitively, so "0XFF" is parsed as
        // decimal and fails. Documented, not fixed, to keep the trees aligned.
        assert_eq!(to_i32("0XFF"), 0);
        assert!(try_to_i32("0XFF").is_none());
    }

    #[test]
    fn failure_is_distinguishable_from_a_real_zero() {
        assert_eq!(to_i32("garbage"), 0);
        assert!(try_to_i32("garbage").is_none());
        assert_eq!(try_to_i32("0"), Some(0));
    }

    #[test]
    fn booleans_are_case_insensitive() {
        assert!(to_bool("True"));
        assert!(to_bool(" TRUE "));
        assert!(!to_bool("yes"));
    }

    #[test]
    fn durations_parse_the_common_forms() {
        assert_eq!(try_to_duration("01:02:03").unwrap().as_secs(), 3723);
        assert_eq!(try_to_duration("2.00:00:00").unwrap().as_secs(), 172_800);
        assert_eq!(try_to_duration("00:00:01.500").unwrap().as_millis(), 1500);
        assert!(try_to_duration("00:99:00").is_none());
        assert!(try_to_duration("-00:00:01").is_none(), "Duration is unsigned");
    }
}
