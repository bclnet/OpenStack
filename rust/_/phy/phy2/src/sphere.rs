// PORT-SOURCE: Phy/OpenStack.Phy2/Sphere.cs
// PORT-SHA: 709857328362bcd1
// PORT-STATUS: done
//
// PARTIAL PORT. The C# file is 435 live lines, but most of it —
// `Attack`, `CollideWithPoint`, `SphereSphere` and the rest of the collision
// suite — takes or returns types from namespaces that are **not present in
// this solution**: `Quadrant` and `TransitionState` from `ACE.Entity.Enum`,
// `Position` and `ObjCell` from `ACE.Server.Physics.Common`,
// `SpherePath`/`CollisionInfo` from `ACE.Server.Physics.Collision`, and
// `DatLoader.Entity.Sphere` in one constructor.
//
// Porting those would mean inventing signatures for types whose definitions do
// not exist. What is ported here is the geometry that stands alone; see the
// crate root for the full picture.

use glam::Vec3;

/// C# `class Sphere : IEquatable<Sphere>`.
///
/// A struct here: it is two fields with value semantics and no identity. The C#
/// made it a class, which is why `PhysicsGlobals.DefaultSortingSphere` can be
/// null at all.
#[derive(Debug, Clone, Copy, PartialEq, Default)]
pub struct Sphere {
    pub center: Vec3,
    pub radius: f32,
}

impl Sphere {
    /// C# `ThresholdMed`.
    pub const THRESHOLD_MED: f32 = 1.0 / 3.0;
    /// C# `ThresholdHigh`.
    pub const THRESHOLD_HIGH: f32 = 2.0 / 3.0;

    /// C# `Sphere(Vector3 center, float radius)`.
    pub const fn new(center: Vec3, radius: f32) -> Self {
        Self { center, radius }
    }

    /// Whether `point` lies inside or on the surface.
    #[inline]
    pub fn contains(&self, point: Vec3) -> bool {
        self.center.distance_squared(point) <= self.radius * self.radius
    }

    /// Whether two spheres overlap or touch.
    #[inline]
    pub fn intersects(&self, other: &Self) -> bool {
        let r = self.radius + other.radius;
        self.center.distance_squared(other.center) <= r * r
    }
}

// NOT PORTED from Sphere.cs: `Attack`, `CollideWithPoint`, `SphereSphere`,
// `SphereCollide`, `SphereObj`, `SlideSphere`, `StepSphereUp/Down`,
// `CollisionNormal`, and the `DatLoader.Entity.Sphere` constructor. Each
// depends on at least one type from a missing namespace.

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn contains_includes_the_surface() {
        let s = Sphere::new(Vec3::ZERO, 2.0);
        assert!(s.contains(Vec3::new(2.0, 0.0, 0.0)));
        assert!(!s.contains(Vec3::new(2.01, 0.0, 0.0)));
    }

    #[test]
    fn touching_spheres_intersect() {
        let a = Sphere::new(Vec3::ZERO, 1.0);
        let b = Sphere::new(Vec3::new(2.0, 0.0, 0.0), 1.0);
        let c = Sphere::new(Vec3::new(2.5, 0.0, 0.0), 1.0);
        assert!(a.intersects(&b));
        assert!(!a.intersects(&c));
    }

    #[test]
    fn thresholds_are_thirds() {
        assert!((Sphere::THRESHOLD_MED - 1.0 / 3.0).abs() < 1e-7);
        assert!((Sphere::THRESHOLD_HIGH - 2.0 / 3.0).abs() < 1e-7);
    }
}
