// PORT-SOURCE: Core/OpenStack.PolyIO/System.Drawing/Ray.cs
// PORT-SHA: 00d4fe073ecc8aea
// PORT-STATUS: done
//
// 22 live lines, 35 commented. Every `Intersects` overload is commented out in
// the C#; what remains is position + direction.
//
// NOTE: the C# never normalises `Direction`, and nothing enforces it. Any
// distance computed against such a ray is scaled by the direction's length.
// `new_normalized` is provided for callers that want the usual invariant.

use bytemuck::{Pod, Zeroable};
use glam::Vec3;

/// C# `struct Ray`.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Pod, Zeroable)]
pub struct Ray {
    /// C# `Position`.
    pub position: Vec3,
    /// C# `Direction` — not normalised by the C#.
    pub direction: Vec3,
}

impl Default for Ray {
    fn default() -> Self {
        Self { position: Vec3::ZERO, direction: Vec3::Z }
    }
}

impl Ray {
    /// C# `Ray(Vector3 position, Vector3 direction)` — direction as given.
    pub const fn new(position: Vec3, direction: Vec3) -> Self {
        Self { position, direction }
    }

    /// As above, but with the direction normalised.
    pub fn new_normalized(position: Vec3, direction: Vec3) -> Self {
        Self { position, direction: direction.normalize_or_zero() }
    }

    /// The point at `t` along the ray.
    #[inline]
    pub fn at(&self, t: f32) -> Vec3 {
        self.position + self.direction * t
    }
}

impl std::fmt::Display for Ray {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{{Position:{} Direction:{}}}", self.position, self.direction)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn at_walks_along_the_direction() {
        let r = Ray::new(Vec3::ZERO, Vec3::X);
        assert_eq!(r.at(3.0), Vec3::new(3.0, 0.0, 0.0));
    }

    #[test]
    fn unnormalised_direction_scales_distance() {
        // Documents the C# behaviour rather than hiding it.
        let r = Ray::new(Vec3::ZERO, Vec3::new(2.0, 0.0, 0.0));
        assert_eq!(r.at(1.0), Vec3::new(2.0, 0.0, 0.0));
        assert_eq!(Ray::new_normalized(Vec3::ZERO, Vec3::new(2.0, 0.0, 0.0)).at(1.0), Vec3::X);
    }
}
