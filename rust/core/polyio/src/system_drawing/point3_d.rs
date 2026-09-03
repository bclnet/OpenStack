// PORT-SOURCE: Core/OpenStack.PolyIO/System.Drawing/Point3D.cs
// PORT-SHA: 22ea33d158adde89
// PORT-STATUS: done
//
// 24 live lines, 37 commented. An integer 3D point — the `System.Drawing.Point`
// family extended to three axes. `glam::IVec3` covers the arithmetic; this
// keeps the C# name and `ToString` format for the on-disk/logging contract.

use bytemuck::{Pod, Zeroable};
use glam::IVec3;

/// C# `struct Point3D`.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Hash, Pod, Zeroable)]
pub struct Point3D {
    pub x: i32,
    pub y: i32,
    pub z: i32,
}

impl Point3D {
    pub const EMPTY: Self = Self { x: 0, y: 0, z: 0 };

    pub const fn new(x: i32, y: i32, z: i32) -> Self {
        Self { x, y, z }
    }

    #[inline]
    pub fn is_empty(&self) -> bool {
        *self == Self::EMPTY
    }
}

impl From<Point3D> for IVec3 {
    fn from(p: Point3D) -> Self {
        IVec3::new(p.x, p.y, p.z)
    }
}

impl From<IVec3> for Point3D {
    fn from(v: IVec3) -> Self {
        Self::new(v.x, v.y, v.z)
    }
}

impl std::ops::Add for Point3D {
    type Output = Self;
    fn add(self, o: Self) -> Self {
        Self::new(self.x + o.x, self.y + o.y, self.z + o.z)
    }
}

impl std::ops::Sub for Point3D {
    type Output = Self;
    fn sub(self, o: Self) -> Self {
        Self::new(self.x - o.x, self.y - o.y, self.z - o.z)
    }
}

impl std::fmt::Display for Point3D {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{{X={},Y={},Z={}}}", self.x, self.y, self.z)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn arithmetic_is_componentwise() {
        let a = Point3D::new(1, 2, 3);
        let b = Point3D::new(10, 20, 30);
        assert_eq!(a + b, Point3D::new(11, 22, 33));
        assert_eq!(b - a, Point3D::new(9, 18, 27));
    }

    #[test]
    fn converts_to_and_from_glam() {
        let p = Point3D::new(4, 5, 6);
        assert_eq!(Point3D::from(IVec3::from(p)), p);
    }

    #[test]
    fn empty_is_the_origin() {
        assert!(Point3D::EMPTY.is_empty());
        assert!(!Point3D::new(0, 0, 1).is_empty());
    }
}
