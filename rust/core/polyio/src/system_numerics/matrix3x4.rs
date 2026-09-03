// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Matrix3x4.cs
// PORT-SHA: bd42c4edab9657c9
// PORT-STATUS: done
//
// An affine transform stored as 3 rows x 4 columns — rotation in the leading
// 3x3, translation in the last column. glam has `Affine3A`, but its internal
// layout is column-major with SIMD padding, so it is NOT layout-compatible with
// what is on disk. This keeps the explicit row-major field order for
// serialisation and converts to glam at the boundary.

use bytemuck::{Pod, Zeroable};
use glam::{Mat3, Mat4, Quat, Vec3};

/// C# `struct Matrix3x4`. Row-major, `m[row][col]`.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Pod, Zeroable)]
pub struct Matrix3x4 {
    pub m11: f32, pub m12: f32, pub m13: f32, pub m14: f32,
    pub m21: f32, pub m22: f32, pub m23: f32, pub m24: f32,
    pub m31: f32, pub m32: f32, pub m33: f32, pub m34: f32,
}

impl Default for Matrix3x4 {
    fn default() -> Self {
        Self::IDENTITY
    }
}

impl Matrix3x4 {
    pub const IDENTITY: Self = Self {
        m11: 1.0, m12: 0.0, m13: 0.0, m14: 0.0,
        m21: 0.0, m22: 1.0, m23: 0.0, m24: 0.0,
        m31: 0.0, m32: 0.0, m33: 1.0, m34: 0.0,
    };

    /// C# `Translation` getter — the last column.
    #[inline]
    pub fn translation(&self) -> Vec3 {
        Vec3::new(self.m14, self.m24, self.m34)
    }

    /// C# `Translation` setter.
    #[inline]
    pub fn set_translation(&mut self, v: Vec3) {
        self.m14 = v.x;
        self.m24 = v.y;
        self.m34 = v.z;
    }

    /// C# `Rotation` — the leading 3x3 block.
    ///
    /// The C# fields are row-major but `Mat3::from_cols_array` wants columns, so
    /// the transpose here is deliberate, not a slip.
    #[inline]
    pub fn rotation(&self) -> Mat3 {
        Mat3::from_cols_array(&[
            self.m11, self.m21, self.m31,
            self.m12, self.m22, self.m32,
            self.m13, self.m23, self.m33,
        ])
    }

    /// C# `CreateFromQuaternion(Quaternion)`.
    pub fn from_quat(q: Quat) -> Self {
        let m = Mat3::from_quat(q);
        // Mat3 is column-major; read it back out row-wise.
        Self {
            m11: m.x_axis.x, m12: m.y_axis.x, m13: m.z_axis.x, m14: 0.0,
            m21: m.x_axis.y, m22: m.y_axis.y, m23: m.z_axis.y, m24: 0.0,
            m31: m.x_axis.z, m32: m.y_axis.z, m33: m.z_axis.z, m34: 0.0,
        }
    }

    /// C# `Polyfill.ToMatrix4x4(this Matrix3x4)` — append `[0 0 0 1]`.
    pub fn to_mat4(&self) -> Mat4 {
        Mat4::from_cols_array(&[
            self.m11, self.m21, self.m31, 0.0,
            self.m12, self.m22, self.m32, 0.0,
            self.m13, self.m23, self.m33, 0.0,
            self.m14, self.m24, self.m34, 1.0,
        ])
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn identity_survives_the_mat4_conversion() {
        assert_eq!(Matrix3x4::IDENTITY.to_mat4(), Mat4::IDENTITY);
    }

    #[test]
    fn translation_lands_in_the_last_column() {
        let mut m = Matrix3x4::IDENTITY;
        m.set_translation(Vec3::new(1.0, 2.0, 3.0));
        assert_eq!(m.translation(), Vec3::new(1.0, 2.0, 3.0));
        // A Mat4 built from it must translate a point the same way.
        let p = m.to_mat4().transform_point3(Vec3::ZERO);
        assert_eq!(p, Vec3::new(1.0, 2.0, 3.0));
    }

    #[test]
    fn quaternion_rotation_matches_glam() {
        let q = Quat::from_rotation_z(std::f32::consts::FRAC_PI_2);
        let ours = Matrix3x4::from_quat(q).to_mat4().transform_vector3(Vec3::X);
        let theirs = Mat4::from_quat(q).transform_vector3(Vec3::X);
        assert!((ours - theirs).length() < 1e-6, "{ours:?} vs {theirs:?}");
    }

    #[test]
    fn layout_is_twelve_contiguous_floats() {
        // Guards the on-disk contract that makes bytemuck blitting valid.
        assert_eq!(std::mem::size_of::<Matrix3x4>(), 48);
    }
}
