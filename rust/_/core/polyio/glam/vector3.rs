// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Vector3.cs
// PORT-SHA: 93e2d1fbc55184ee
// PORT-STATUS: done
//
// The C# file is a 27KB generic `Vector3<T> where T : IComparable<T>,
// IEquatable<T>`, with arithmetic dispatched through a
// `Dictionary<char, Func<T,T,T>>` of operators.
//
// It has ZERO instantiations anywhere in the solution. Every one of its ~100
// mentions is a self-reference inside its own definition. Meanwhile the BCL
// `System.Numerics.Vector3` (float) is used 1490 times.
//
// So this file ports to a re-export of `glam::Vec3` — the float vector that is
// actually used — plus `IVec3` for the integer case its sibling files do use.
// The generic machinery is deliberately not reproduced: `T`-dispatch through a
// dictionary of closures is slow in C# and has no reason to exist in Rust,
// where a generic over `num` traits would be monomorphised for free if it were
// ever needed.
//
// If a real `Vector3<T>` instantiation appears on the C# side, revisit — but
// port the concrete instantiation, not the dictionary.

/// C# `System.Numerics.Vector3` (BCL, float) — the type 1490 call sites use.
pub use glam::Vec3;

/// Integer variant, for the `Vector3<int>` shape.
pub use glam::IVec3;

/// Nearness threshold matching C# `Polyfill.EPSILON`.
pub const EPSILON: f32 = 1e-6;

/// C# `Polyfill.IsZero(this Vector3 v)` — exact comparison against zero.
#[inline]
pub fn is_zero(v: Vec3) -> bool {
    v.x == 0.0 && v.y == 0.0 && v.z == 0.0
}

/// C# `Polyfill.IsZeroEpsilon(this Vector3 v)` / `NearZero`.
#[inline]
pub fn is_zero_epsilon(v: Vec3) -> bool {
    v.x.abs() < EPSILON && v.y.abs() < EPSILON && v.z.abs() < EPSILON
}

/// C# `Polyfill.ParseVector3(string input)` — whitespace-separated triple.
///
/// C# returns `Vector3.Zero` on malformed input rather than throwing; this
/// returns `None` so callers can tell "parsed as zero" from "did not parse".
/// Call sites that want the old behaviour use `.unwrap_or(Vec3::ZERO)`.
pub fn parse_vec3(input: &str) -> Option<Vec3> {
    let mut it = input.split_whitespace();
    let x = it.next()?.parse().ok()?;
    let y = it.next()?.parse().ok()?;
    let z = it.next()?.parse().ok()?;
    if it.next().is_some() {
        return None;
    }
    Some(Vec3::new(x, y, z))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parses_a_well_formed_triple() {
        assert_eq!(parse_vec3("1 2.5 -3").unwrap(), Vec3::new(1.0, 2.5, -3.0));
    }

    #[test]
    fn rejects_malformed_input_instead_of_returning_zero() {
        assert!(parse_vec3("1 2").is_none());
        assert!(parse_vec3("1 2 3 4").is_none());
        assert!(parse_vec3("a b c").is_none());
        // Old C# behaviour stays available where callers want it.
        assert_eq!(parse_vec3("bad").unwrap_or(Vec3::ZERO), Vec3::ZERO);
    }

    #[test]
    fn zero_tests_differ_on_tiny_values() {
        let tiny = Vec3::new(1e-9, 0.0, 0.0);
        assert!(!is_zero(tiny));
        assert!(is_zero_epsilon(tiny));
    }
}
