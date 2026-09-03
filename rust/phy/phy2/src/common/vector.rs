// PORT-SOURCE: Phy/OpenStack.Phy2/Common/Vector.cs
// PORT-SHA: 27b4e5d5d168747f
// PORT-STATUS: done
//
// C# `static class Vec` — two epsilon-aware vector helpers.

use glam::Vec3;

use crate::physics_globals::EPSILON;

/// C# `NormalizeCheckSmall(ref Vector3 v)`.
///
/// Normalises in place and returns **true when the vector was too small to
/// normalise** — an inverted-sounding return the C# uses as "is degenerate".
/// The name says "check small" and the value means "was small"; kept, since
/// call sites branch on it.
///
/// When it returns true, `v` is left untouched (the C# does the same), so a
/// caller that ignores the return keeps an unnormalised vector.
pub fn normalize_check_small(v: &mut Vec3) -> bool {
    let dist = v.length();
    if dist < EPSILON {
        return true;
    }
    *v *= 1.0 / dist;
    false
}

/// C# `IsZero(Vector3 v)` — componentwise, against [`EPSILON`].
///
/// Note this is a *box* test, not a length test: `(EPSILON*0.9, EPSILON*0.9,
/// EPSILON*0.9)` counts as zero even though its length exceeds `EPSILON`. That
/// is what the C# does and what `normalize_check_small` does *not*, so the two
/// disagree near the threshold.
#[inline]
pub fn is_zero(v: Vec3) -> bool {
    v.x.abs() < EPSILON && v.y.abs() < EPSILON && v.z.abs() < EPSILON
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn normalises_a_normal_vector() {
        let mut v = Vec3::new(0.0, 3.0, 4.0);
        assert!(!normalize_check_small(&mut v));
        assert!((v.length() - 1.0).abs() < 1e-6);
    }

    #[test]
    fn small_vectors_report_true_and_are_left_alone() {
        let mut v = Vec3::new(1e-9, 0.0, 0.0);
        let before = v;
        assert!(normalize_check_small(&mut v));
        assert_eq!(v, before, "C# leaves the vector untouched");
    }

    #[test]
    fn is_zero_is_a_box_test_not_a_length_test() {
        // Documents the disagreement between the two helpers near threshold.
        let v = Vec3::splat(EPSILON * 0.9);
        assert!(is_zero(v), "inside the epsilon box");
        assert!(v.length() > EPSILON, "but longer than epsilon");
        let mut w = v;
        assert!(!normalize_check_small(&mut w), "so this one normalises it");
    }

    #[test]
    fn exact_zero_is_zero() {
        assert!(is_zero(Vec3::ZERO));
        assert!(!is_zero(Vec3::new(1.0, 0.0, 0.0)));
    }
}
