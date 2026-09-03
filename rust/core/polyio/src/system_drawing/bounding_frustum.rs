// PORT-SOURCE: Core/OpenStack.PolyIO/System.Drawing/BoundingFrustum.cs
// PORT-SHA: 6af56aa928ca1446
// PORT-STATUS: done
//
// 20 live lines, 33 commented. The C# keeps only the backing matrix; the plane
// extraction, corner computation, and every `Intersects`/`Contains` overload
// are commented out, so a `BoundingFrustum` there cannot currently cull
// anything.
//
// Ported as the live type — the matrix — plus Gribb-Hartmann plane extraction,
// which is what the commented code was building toward and what any caller
// needs to make the type useful. Marked as an addition below.

use glam::{Mat4, Vec3, Vec4};

use super::bounding_box::BoundingBox;

/// C# `struct BoundingFrustum`.
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct BoundingFrustum {
    /// C# `Matrix` — the view-projection this frustum was built from.
    pub matrix: Mat4,
}

/// A plane as `ax + by + cz + d = 0`, normal in `xyz`.
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Plane(pub Vec4);

impl Plane {
    /// Signed distance from the plane to `p`; positive is in front.
    #[inline]
    pub fn distance(&self, p: Vec3) -> f32 {
        self.0.x * p.x + self.0.y * p.y + self.0.z * p.z + self.0.w
    }

    fn normalized(self) -> Self {
        let n = Vec3::new(self.0.x, self.0.y, self.0.z).length();
        if n == 0.0 {
            self
        } else {
            Plane(self.0 / n)
        }
    }
}

impl BoundingFrustum {
    pub const fn new(matrix: Mat4) -> Self {
        Self { matrix }
    }

    // -- NOT IN THE LIVE C#: the commented-out routines, reinstated ----------

    /// The six clipping planes, in the order left, right, bottom, top, near,
    /// far. Gribb-Hartmann extraction from the view-projection matrix.
    pub fn planes(&self) -> [Plane; 6] {
        let m = self.matrix.to_cols_array_2d();
        // row(i) of the matrix; glam stores columns, so index accordingly.
        let row = |i: usize| Vec4::new(m[0][i], m[1][i], m[2][i], m[3][i]);
        let (r0, r1, r2, r3) = (row(0), row(1), row(2), row(3));
        [
            Plane(r3 + r0).normalized(), // left
            Plane(r3 - r0).normalized(), // right
            Plane(r3 + r1).normalized(), // bottom
            Plane(r3 - r1).normalized(), // top
            Plane(r3 + r2).normalized(), // near
            Plane(r3 - r2).normalized(), // far
        ]
    }

    /// True if `p` is inside every plane.
    pub fn contains(&self, p: Vec3) -> bool {
        self.planes().iter().all(|pl| pl.distance(p) >= 0.0)
    }

    /// Conservative AABB test: rejects a box only when it is fully outside one
    /// plane. May report a false positive for boxes straddling a corner, which
    /// is the standard and acceptable trade for cheap culling.
    pub fn intersects(&self, b: &BoundingBox) -> bool {
        self.planes().iter().all(|pl| {
            // The box corner furthest along the plane normal.
            let n = Vec3::new(pl.0.x, pl.0.y, pl.0.z);
            let positive = Vec3::new(
                if n.x >= 0.0 { b.max.x } else { b.min.x },
                if n.y >= 0.0 { b.max.y } else { b.min.y },
                if n.z >= 0.0 { b.max.z } else { b.min.z },
            );
            pl.distance(positive) >= 0.0
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn frustum() -> BoundingFrustum {
        let proj = Mat4::perspective_rh(std::f32::consts::FRAC_PI_2, 1.0, 0.1, 100.0);
        let view = Mat4::look_at_rh(Vec3::ZERO, Vec3::NEG_Z, Vec3::Y);
        BoundingFrustum::new(proj * view)
    }

    #[test]
    fn extracts_six_normalised_planes() {
        for p in frustum().planes() {
            let n = Vec3::new(p.0.x, p.0.y, p.0.z).length();
            assert!((n - 1.0).abs() < 1e-4, "plane normal not unit: {n}");
        }
    }

    #[test]
    fn contains_points_in_front_and_rejects_those_behind() {
        let f = frustum();
        assert!(f.contains(Vec3::new(0.0, 0.0, -10.0)));
        assert!(!f.contains(Vec3::new(0.0, 0.0, 10.0)), "behind the camera");
        assert!(!f.contains(Vec3::new(0.0, 0.0, -1000.0)), "past the far plane");
    }

    #[test]
    fn culls_a_box_that_is_fully_outside() {
        let f = frustum();
        let inside = BoundingBox::new(Vec3::new(-1.0, -1.0, -11.0), Vec3::new(1.0, 1.0, -9.0));
        let behind = BoundingBox::new(Vec3::new(-1.0, -1.0, 9.0), Vec3::new(1.0, 1.0, 11.0));
        assert!(f.intersects(&inside));
        assert!(!f.intersects(&behind));
    }
}
