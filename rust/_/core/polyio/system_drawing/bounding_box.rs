// PORT-SOURCE: Core/OpenStack.PolyIO/System.Drawing/BoundingBox.cs
// PORT-SHA: 7960ac6d2b667aaa
// PORT-STATUS: done
//
// Axis-aligned bounding box. The C# has 22 live lines and 35 commented ones —
// `Intersects`, `Contains`, `CreateMerged`, and `CreateFromPoints` are all
// commented out, so this is a data type with equality and formatting.
//
// The handful of methods below are the commented-out ones reinstated, because
// they are trivial over `glam` and every caller of a bounding box eventually
// wants them. They are marked so it is clear they are additions, not ports.

use bytemuck::{Pod, Zeroable};
use glam::Vec3;

/// C# `struct BoundingBox`.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Pod, Zeroable)]
pub struct BoundingBox {
    /// C# `Min`.
    pub min: Vec3,
    /// C# `Max`.
    pub max: Vec3,
}

impl Default for BoundingBox {
    fn default() -> Self {
        Self::EMPTY
    }
}

impl BoundingBox {
    /// An inverted box, so the first `encapsulate` sets both corners.
    pub const EMPTY: Self = Self {
        min: Vec3::new(f32::INFINITY, f32::INFINITY, f32::INFINITY),
        max: Vec3::new(f32::NEG_INFINITY, f32::NEG_INFINITY, f32::NEG_INFINITY),
    };

    pub const fn new(min: Vec3, max: Vec3) -> Self {
        Self { min, max }
    }

    #[inline]
    pub fn center(&self) -> Vec3 {
        (self.min + self.max) * 0.5
    }

    #[inline]
    pub fn size(&self) -> Vec3 {
        self.max - self.min
    }

    #[inline]
    pub fn is_valid(&self) -> bool {
        self.min.x <= self.max.x && self.min.y <= self.max.y && self.min.z <= self.max.z
    }

    // -- NOT IN THE LIVE C#: these mirror routines commented out there --------

    /// C# `CreateFromPoints` (commented out).
    pub fn from_points(points: &[Vec3]) -> Self {
        points.iter().fold(Self::EMPTY, |b, &p| b.encapsulate(p))
    }

    /// Grow to include `p`.
    #[inline]
    pub fn encapsulate(self, p: Vec3) -> Self {
        Self { min: self.min.min(p), max: self.max.max(p) }
    }

    /// C# `CreateMerged` (commented out).
    #[inline]
    pub fn merge(self, o: Self) -> Self {
        Self { min: self.min.min(o.min), max: self.max.max(o.max) }
    }

    /// C# `Contains(Vector3)` (commented out).
    #[inline]
    pub fn contains(&self, p: Vec3) -> bool {
        p.cmpge(self.min).all() && p.cmple(self.max).all()
    }

    /// C# `Intersects(BoundingBox)` (commented out). Touching counts as
    /// intersecting, matching the usual AABB convention.
    #[inline]
    pub fn intersects(&self, o: &Self) -> bool {
        self.min.cmple(o.max).all() && self.max.cmpge(o.min).all()
    }
}

impl std::fmt::Display for BoundingBox {
    /// C# `ToString() => $"{{Min:{Min} Max:{Max}}}"`.
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{{Min:{} Max:{}}}", self.min, self.max)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn from_points_bounds_them_all() {
        let b = BoundingBox::from_points(&[
            Vec3::new(1.0, 5.0, -2.0),
            Vec3::new(-3.0, 0.0, 4.0),
        ]);
        assert_eq!(b.min, Vec3::new(-3.0, 0.0, -2.0));
        assert_eq!(b.max, Vec3::new(1.0, 5.0, 4.0));
    }

    #[test]
    fn empty_is_invalid_until_something_is_added() {
        assert!(!BoundingBox::EMPTY.is_valid());
        assert!(BoundingBox::EMPTY.encapsulate(Vec3::ZERO).is_valid());
    }

    #[test]
    fn contains_includes_the_boundary() {
        let b = BoundingBox::new(Vec3::ZERO, Vec3::splat(2.0));
        assert!(b.contains(Vec3::splat(1.0)));
        assert!(b.contains(Vec3::ZERO));
        assert!(!b.contains(Vec3::splat(2.1)));
    }

    #[test]
    fn intersection_is_symmetric() {
        let a = BoundingBox::new(Vec3::ZERO, Vec3::splat(2.0));
        let b = BoundingBox::new(Vec3::splat(1.0), Vec3::splat(3.0));
        let far = BoundingBox::new(Vec3::splat(9.0), Vec3::splat(10.0));
        assert!(a.intersects(&b) && b.intersects(&a));
        assert!(!a.intersects(&far) && !far.intersects(&a));
    }

    #[test]
    fn merge_covers_both() {
        let a = BoundingBox::new(Vec3::ZERO, Vec3::ONE);
        let b = BoundingBox::new(Vec3::splat(5.0), Vec3::splat(6.0));
        let m = a.merge(b);
        assert_eq!(m.min, Vec3::ZERO);
        assert_eq!(m.max, Vec3::splat(6.0));
    }
}
