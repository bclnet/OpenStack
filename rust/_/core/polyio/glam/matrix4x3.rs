// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Matrix4x3.cs
// PORT-SHA: e54a118e0abe1d35
// PORT-STATUS: done
//
// The transpose of `Matrix3x4`: 4 rows x 3 columns, used where a format stores
// an affine transform row-per-basis-vector with translation last.

use bytemuck::{Pod, Zeroable};
use glam::{Mat4, Vec3};

/// C# `struct Matrix4x3`. Row-major, `m[row][col]`.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Pod, Zeroable)]
pub struct Matrix4x3 {
    pub m11: f32, pub m12: f32, pub m13: f32,
    pub m21: f32, pub m22: f32, pub m23: f32,
    pub m31: f32, pub m32: f32, pub m33: f32,
    pub m41: f32, pub m42: f32, pub m43: f32,
}

impl Default for Matrix4x3 {
    fn default() -> Self {
        Self::IDENTITY
    }
}

impl Matrix4x3 {
    pub const IDENTITY: Self = Self {
        m11: 1.0, m12: 0.0, m13: 0.0,
        m21: 0.0, m22: 1.0, m23: 0.0,
        m31: 0.0, m32: 0.0, m33: 1.0,
        m41: 0.0, m42: 0.0, m43: 0.0,
    };

    /// The trailing row — translation in this layout.
    #[inline]
    pub fn translation(&self) -> Vec3 {
        Vec3::new(self.m41, self.m42, self.m43)
    }

    /// Widen to a full 4x4 by appending a `[0 0 0 1]` column.
    pub fn to_mat4(&self) -> Mat4 {
        Mat4::from_cols_array(&[
            self.m11, self.m12, self.m13, 0.0,
            self.m21, self.m22, self.m23, 0.0,
            self.m31, self.m32, self.m33, 0.0,
            self.m41, self.m42, self.m43, 1.0,
        ])
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn identity_round_trips() {
        assert_eq!(Matrix4x3::IDENTITY.to_mat4(), Mat4::IDENTITY);
    }

    #[test]
    fn trailing_row_is_the_translation() {
        let mut m = Matrix4x3::IDENTITY;
        m.m41 = 5.0;
        m.m42 = 6.0;
        m.m43 = 7.0;
        assert_eq!(m.to_mat4().transform_point3(Vec3::ZERO), m.translation());
    }

    #[test]
    fn layout_is_twelve_contiguous_floats() {
        assert_eq!(std::mem::size_of::<Matrix4x3>(), 48);
    }
}
