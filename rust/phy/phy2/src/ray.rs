// PORT-SOURCE: Phy/OpenStack.Phy2/Ray.cs
// PORT-SHA: f86e2b59021622ce
// PORT-STATUS: done
//
// C#-SIDE BUG — **the two-argument constructor silently produces a null ray.**
//
//     public Ray(Vector3 startPoint, Vector3 offset) {
//         if (Math.Abs(offset.X - startPoint.X) > EPSILON || ...) {
//             Length = ...; Point = startPoint; Dir = ...;
//         }
//         // no else: Point, Dir and Length keep their default zero values
//     }
//
// When the guard fails, `Point` is left at `(0,0,0)` rather than `startPoint`,
// so a degenerate ray points at the world origin instead of where it was
// created. A caller has no way to tell — every field is a legal `Vector3`.
//
// The guard is also comparing the wrong things: it tests
// `offset - startPoint` against epsilon, but `offset` is a *direction/extent*,
// not a second point. The intended test is surely whether `offset` itself is
// near zero. As written, an offset that happens to sit near `startPoint`
// (e.g. both `(1,1,1)`) is treated as degenerate even though it is a perfectly
// good unit-ish extent, while a genuinely zero offset far from the origin
// passes. Both readings are preserved below so the behaviour can be pinned
// down before it is changed.

use glam::Vec3;

use crate::physics_globals::EPSILON;

/// C# `class Ray`.
#[derive(Debug, Clone, Copy, PartialEq, Default)]
pub struct Ray {
    pub point: Vec3,
    pub dir: Vec3,
    pub length: f32,
}

impl Ray {
    /// C# `Ray(Vector3 point, Vector3 dir, float length)` — fields as given.
    pub const fn new(point: Vec3, dir: Vec3, length: f32) -> Self {
        Self { point, dir, length }
    }

    /// C# `Ray(Vector3 startPoint, Vector3 offset)`, with the degenerate case
    /// reported instead of silently zeroed.
    ///
    /// `None` when `offset` is shorter than [`EPSILON`], so a caller must
    /// decide what a zero-length ray means rather than receiving one aimed at
    /// the origin.
    pub fn from_offset(start_point: Vec3, offset: Vec3) -> Option<Self> {
        let length = offset.length();
        if length < EPSILON {
            return None;
        }
        Some(Self { point: start_point, dir: offset / length, length })
    }

    /// The C#'s literal behaviour, guard condition and all, for pinning down
    /// existing call sites before changing them.
    #[deprecated(note = "mirrors a C#-side bug: returns a ray at the origin for the degenerate case")]
    pub fn from_offset_bug_compat(start_point: Vec3, offset: Vec3) -> Self {
        let d = offset - start_point;
        if d.x.abs() > EPSILON || d.y.abs() > EPSILON || d.z.abs() > EPSILON {
            let length = offset.length();
            Self { point: start_point, dir: offset / length, length }
        } else {
            Self::default() // Point == Vec3::ZERO, not start_point
        }
    }

    /// The point at `t` along the ray.
    #[inline]
    pub fn at(&self, t: f32) -> Vec3 {
        self.point + self.dir * t
    }

    /// The far end of the ray.
    #[inline]
    pub fn end(&self) -> Vec3 {
        self.at(self.length)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn from_offset_normalises_and_keeps_the_start() {
        let r = Ray::from_offset(Vec3::new(5.0, 0.0, 0.0), Vec3::new(0.0, 3.0, 4.0)).unwrap();
        assert_eq!(r.point, Vec3::new(5.0, 0.0, 0.0));
        assert!((r.length - 5.0).abs() < 1e-6);
        assert!((r.dir.length() - 1.0).abs() < 1e-6);
        assert!((r.end() - Vec3::new(5.0, 3.0, 4.0)).length() < 1e-5);
    }

    #[test]
    fn degenerate_offset_is_reported_not_silently_zeroed() {
        assert!(Ray::from_offset(Vec3::new(9.0, 9.0, 9.0), Vec3::ZERO).is_none());
    }

    #[test]
    fn the_c_sharp_loses_the_start_point_on_the_degenerate_path() {
        // Documents the bug: start (1,1,1) with offset (1,1,1) trips the guard,
        // and the result points at the origin instead of at (1,1,1).
        #[allow(deprecated)]
        let r = Ray::from_offset_bug_compat(Vec3::ONE, Vec3::ONE);
        assert_eq!(r.point, Vec3::ZERO, "start point discarded");
        assert_eq!(r.length, 0.0);
    }

    #[test]
    fn the_c_sharp_guard_tests_the_wrong_quantity() {
        // A genuinely zero offset far from the origin *passes* the guard,
        // producing dir = 0/0 = NaN.
        #[allow(deprecated)]
        let r = Ray::from_offset_bug_compat(Vec3::new(10.0, 0.0, 0.0), Vec3::ZERO);
        assert!(r.dir.is_nan(), "0/0 in the normalise");
        // The safe constructor rejects it.
        assert!(Ray::from_offset(Vec3::new(10.0, 0.0, 0.0), Vec3::ZERO).is_none());
    }

    #[test]
    fn at_walks_along_the_direction() {
        let r = Ray::new(Vec3::ZERO, Vec3::X, 10.0);
        assert_eq!(r.at(3.0), Vec3::new(3.0, 0.0, 0.0));
        assert_eq!(r.end(), Vec3::new(10.0, 0.0, 0.0));
    }
}
