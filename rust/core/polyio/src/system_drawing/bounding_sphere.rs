// PORT-SOURCE: Core/OpenStack.PolyIO/System.Drawing/BoundingSphere.cs
// PORT-SHA: bb5941bfb1e54da2
// PORT-STATUS: done
//
// 26 live lines, 35 commented. As with BoundingBox, all the geometry is
// commented out in the C#; the live type is centre + radius with equality and
// formatting.

use bytemuck::{Pod, Zeroable};
use glam::Vec3;

/// C# `struct BoundingSphere`.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Pod, Zeroable)]
pub struct BoundingSphere {
    pub center: Vec3,
    pub radius: f32,
}

impl Default for BoundingSphere {
    fn default() -> Self {
        Self { center: Vec3::ZERO, radius: 0.0 }
    }
}

impl BoundingSphere {
    pub const fn new(center: Vec3, radius: f32) -> Self {
        Self { center, radius }
    }

    /// C# `Contains(Vector3)` (commented out there).
    #[inline]
    pub fn contains(&self, p: Vec3) -> bool {
        self.center.distance_squared(p) <= self.radius * self.radius
    }

    /// C# `Intersects(BoundingSphere)` (commented out there).
    #[inline]
    pub fn intersects(&self, o: &Self) -> bool {
        let r = self.radius + o.radius;
        self.center.distance_squared(o.center) <= r * r
    }
}

impl std::fmt::Display for BoundingSphere {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{{Center:{} Radius:{}}}", self.center, self.radius)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn contains_includes_the_surface() {
        let s = BoundingSphere::new(Vec3::ZERO, 2.0);
        assert!(s.contains(Vec3::new(2.0, 0.0, 0.0)));
        assert!(!s.contains(Vec3::new(2.01, 0.0, 0.0)));
    }

    #[test]
    fn touching_spheres_intersect() {
        let a = BoundingSphere::new(Vec3::ZERO, 1.0);
        let b = BoundingSphere::new(Vec3::new(2.0, 0.0, 0.0), 1.0);
        let c = BoundingSphere::new(Vec3::new(2.5, 0.0, 0.0), 1.0);
        assert!(a.intersects(&b));
        assert!(!a.intersects(&c));
    }
}
