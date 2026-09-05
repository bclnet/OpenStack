// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Matrix3x3.cs
// PORT-SHA: 2f4e39f8185fa995
// PORT-STATUS: done
//
// 28KB in C#, and the single most-used matrix type (128 references). glam's
// `Mat3` provides the linear algebra; this type owns the row-major on-disk
// layout plus the handful of methods with no glam equivalent
// (`IsScaleRotation`, `GetScaleRotation`, `Conjugate*`).
//
// `Mat3` is column-major, so every conversion transposes. That is the one thing
// to get right here; the tests below pin it against glam.

use bytemuck::{Pod, Zeroable};
use glam::{Mat3, Quat, Vec2, Vec3};

/// C# `struct Matrix3x3`. Row-major, `m[row][col]`.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Pod, Zeroable)]
pub struct Matrix3x3 {
    pub m11: f32, pub m12: f32, pub m13: f32,
    pub m21: f32, pub m22: f32, pub m23: f32,
    pub m31: f32, pub m32: f32, pub m33: f32,
}

impl Default for Matrix3x3 {
    fn default() -> Self {
        Self::IDENTITY
    }
}

impl Matrix3x3 {
    /// C# `Identity`.
    pub const IDENTITY: Self = Self {
        m11: 1.0, m12: 0.0, m13: 0.0,
        m21: 0.0, m22: 1.0, m23: 0.0,
        m31: 0.0, m32: 0.0, m33: 1.0,
    };

    pub const ZERO: Self = Self {
        m11: 0.0, m12: 0.0, m13: 0.0,
        m21: 0.0, m22: 0.0, m23: 0.0,
        m31: 0.0, m32: 0.0, m33: 0.0,
    };

    #[allow(clippy::too_many_arguments)]
    pub const fn new(
        m11: f32, m12: f32, m13: f32,
        m21: f32, m22: f32, m23: f32,
        m31: f32, m32: f32, m33: f32,
    ) -> Self {
        Self { m11, m12, m13, m21, m22, m23, m31, m32, m33 }
    }

    // -- glam interop (transposes: C# is row-major, Mat3 is column-major) ----

    #[inline]
    pub fn to_mat3(&self) -> Mat3 {
        Mat3::from_cols_array(&[
            self.m11, self.m21, self.m31,
            self.m12, self.m22, self.m32,
            self.m13, self.m23, self.m33,
        ])
    }

    #[inline]
    pub fn from_mat3(m: Mat3) -> Self {
        Self {
            m11: m.x_axis.x, m12: m.y_axis.x, m13: m.z_axis.x,
            m21: m.x_axis.y, m22: m.y_axis.y, m23: m.z_axis.y,
            m31: m.x_axis.z, m32: m.y_axis.z, m33: m.z_axis.z,
        }
    }

    /// C# `CreateFromQuaternion(Quaternion)` / `Polyfill.ConvertToRotationMatrix`.
    #[inline]
    pub fn from_quat(q: Quat) -> Self {
        Self::from_mat3(Mat3::from_quat(q))
    }

    /// C# `CreateRotationX(float radians)`.
    #[inline]
    pub fn from_rotation_x(radians: f32) -> Self {
        Self::from_mat3(Mat3::from_rotation_x(radians))
    }

    #[inline]
    pub fn from_rotation_y(radians: f32) -> Self {
        Self::from_mat3(Mat3::from_rotation_y(radians))
    }

    #[inline]
    pub fn from_rotation_z(radians: f32) -> Self {
        Self::from_mat3(Mat3::from_rotation_z(radians))
    }

    /// C# `CreateScale(Vector3)`.
    #[inline]
    pub fn from_scale(s: Vec3) -> Self {
        Self::new(s.x, 0.0, 0.0, 0.0, s.y, 0.0, 0.0, 0.0, s.z)
    }

    /// C# `CreateTranslation(Vector2)` — a 2D affine transform in a 3x3, so the
    /// translation lives in the last column.
    #[inline]
    pub fn from_translation_2d(t: Vec2) -> Self {
        let mut m = Self::IDENTITY;
        m.m13 = t.x;
        m.m23 = t.y;
        m
    }

    /// C# `Translation` — the 2D affine translation component.
    #[inline]
    pub fn translation_2d(&self) -> Vec2 {
        Vec2::new(self.m13, self.m23)
    }

    // -- linear algebra ------------------------------------------------------

    /// C# `Transpose`.
    #[inline]
    pub fn transpose(&self) -> Self {
        Self::new(
            self.m11, self.m21, self.m31,
            self.m12, self.m22, self.m32,
            self.m13, self.m23, self.m33,
        )
    }

    /// C# `GetDeterminant`.
    #[inline]
    pub fn determinant(&self) -> f32 {
        self.m11 * (self.m22 * self.m33 - self.m23 * self.m32)
            - self.m12 * (self.m21 * self.m33 - self.m23 * self.m31)
            + self.m13 * (self.m21 * self.m32 - self.m22 * self.m31)
    }

    /// C# `Inverse` / `Invert(out ...)`.
    ///
    /// The C# `Inverse` property divides by the determinant unconditionally,
    /// yielding infinities and NaNs for a singular matrix. Returns `None` here.
    pub fn inverse(&self) -> Option<Self> {
        let det = self.determinant();
        if det.abs() < f32::EPSILON {
            return None;
        }
        Some(Self::from_mat3(self.to_mat3().inverse()))
    }

    /// C# `Mult(Vector3)` — row-vector convention, matching the C#.
    #[inline]
    pub fn mult(&self, v: Vec3) -> Vec3 {
        Vec3::new(
            self.m11 * v.x + self.m12 * v.y + self.m13 * v.z,
            self.m21 * v.x + self.m22 * v.y + self.m23 * v.z,
            self.m31 * v.x + self.m32 * v.y + self.m33 * v.z,
        )
    }

    /// C# `Diagonal`.
    #[inline]
    pub fn diagonal(&self) -> Vec3 {
        Vec3::new(self.m11, self.m22, self.m33)
    }

    /// C# `GetScale` — the length of each row basis vector.
    #[inline]
    pub fn scale(&self) -> Vec3 {
        Vec3::new(
            Vec3::new(self.m11, self.m12, self.m13).length(),
            Vec3::new(self.m21, self.m22, self.m23).length(),
            Vec3::new(self.m31, self.m32, self.m33).length(),
        )
    }

    /// C# `IsRotation` — orthonormal with a determinant of +1.
    pub fn is_rotation(&self) -> bool {
        const TOL: f32 = 1e-4;
        let s = self.scale();
        (s.x - 1.0).abs() < TOL
            && (s.y - 1.0).abs() < TOL
            && (s.z - 1.0).abs() < TOL
            && (self.determinant() - 1.0).abs() < TOL
    }

    /// C# `IsScaleRotation` — a rotation with uniform or per-axis scale, i.e.
    /// the rows stay mutually orthogonal.
    pub fn is_scale_rotation(&self) -> bool {
        const TOL: f32 = 1e-4;
        let (r1, r2, r3) = (
            Vec3::new(self.m11, self.m12, self.m13),
            Vec3::new(self.m21, self.m22, self.m23),
            Vec3::new(self.m31, self.m32, self.m33),
        );
        r1.dot(r2).abs() < TOL && r1.dot(r3).abs() < TOL && r2.dot(r3).abs() < TOL
    }

    /// C# `GetScaleRotation` — the scale factors, then the matrix with them
    /// divided out.
    pub fn scale_rotation(&self) -> (Vec3, Self) {
        let s = self.scale();
        let d = |v: f32, by: f32| if by.abs() < f32::EPSILON { v } else { v / by };
        let r = Self::new(
            d(self.m11, s.x), d(self.m12, s.x), d(self.m13, s.x),
            d(self.m21, s.y), d(self.m22, s.y), d(self.m23, s.y),
            d(self.m31, s.z), d(self.m32, s.z), d(self.m33, s.z),
        );
        (s, r)
    }

    /// C# `Conjugate`. Real-valued, so this equals the transpose; kept under
    /// the C# name so call sites port over unchanged.
    #[inline]
    pub fn conjugate(&self) -> Self {
        self.transpose()
    }

    /// C# `ConjugateTranspose`.
    #[inline]
    pub fn conjugate_transpose(&self) -> Self {
        *self
    }
}

impl std::ops::Add for Matrix3x3 {
    type Output = Self;
    fn add(self, o: Self) -> Self {
        Self::new(
            self.m11 + o.m11, self.m12 + o.m12, self.m13 + o.m13,
            self.m21 + o.m21, self.m22 + o.m22, self.m23 + o.m23,
            self.m31 + o.m31, self.m32 + o.m32, self.m33 + o.m33,
        )
    }
}

impl std::ops::Sub for Matrix3x3 {
    type Output = Self;
    fn sub(self, o: Self) -> Self {
        Self::new(
            self.m11 - o.m11, self.m12 - o.m12, self.m13 - o.m13,
            self.m21 - o.m21, self.m22 - o.m22, self.m23 - o.m23,
            self.m31 - o.m31, self.m32 - o.m32, self.m33 - o.m33,
        )
    }
}

impl std::ops::Neg for Matrix3x3 {
    type Output = Self;
    fn neg(self) -> Self {
        Self::ZERO - self
    }
}

impl std::ops::Mul for Matrix3x3 {
    type Output = Self;
    fn mul(self, o: Self) -> Self {
        Self::from_mat3(self.to_mat3() * o.to_mat3())
    }
}

impl std::ops::Mul<f32> for Matrix3x3 {
    type Output = Self;
    fn mul(self, s: f32) -> Self {
        Self::new(
            self.m11 * s, self.m12 * s, self.m13 * s,
            self.m21 * s, self.m22 * s, self.m23 * s,
            self.m31 * s, self.m32 * s, self.m33 * s,
        )
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn approx(a: Matrix3x3, b: Matrix3x3) -> bool {
        let (x, y) = (a.to_mat3().to_cols_array(), b.to_mat3().to_cols_array());
        x.iter().zip(y).all(|(p, q)| (p - q).abs() < 1e-5)
    }

    #[test]
    fn row_major_conversion_round_trips() {
        let m = Matrix3x3::new(1., 2., 3., 4., 5., 6., 7., 8., 9.);
        assert_eq!(Matrix3x3::from_mat3(m.to_mat3()), m);
    }

    #[test]
    fn mult_agrees_with_glam_on_the_same_convention() {
        // Pins the row-major/column-major transpose. If the conversion were
        // wrong, these would differ for a non-symmetric matrix.
        let m = Matrix3x3::new(1., 2., 3., 4., 5., 6., 7., 8., 10.);
        let v = Vec3::new(1., 2., 3.);
        assert!((m.mult(v) - m.to_mat3() * v).length() < 1e-5);
    }

    #[test]
    fn multiplication_is_associative_and_identity_is_neutral() {
        let a = Matrix3x3::from_rotation_x(0.3);
        let b = Matrix3x3::from_rotation_y(0.7);
        assert!(approx(a * Matrix3x3::IDENTITY, a));
        assert!(approx((a * b) * a, a * (b * a)));
    }

    #[test]
    fn singular_matrices_return_none_instead_of_nan() {
        // The C# `Inverse` divides by zero and yields NaNs here.
        let singular = Matrix3x3::new(1., 2., 3., 2., 4., 6., 7., 8., 9.);
        assert_eq!(singular.determinant(), 0.0);
        assert!(singular.inverse().is_none());
    }

    #[test]
    fn inverse_undoes_the_rotation() {
        let r = Matrix3x3::from_rotation_z(0.9);
        assert!(approx(r * r.inverse().unwrap(), Matrix3x3::IDENTITY));
    }

    #[test]
    fn rotations_are_detected_scaled_ones_are_not() {
        assert!(Matrix3x3::from_rotation_y(1.1).is_rotation());
        let scaled = Matrix3x3::from_scale(Vec3::splat(2.0));
        assert!(!scaled.is_rotation());
        assert!(scaled.is_scale_rotation(), "axes stay orthogonal under scale");
    }

    #[test]
    fn scale_rotation_splits_cleanly() {
        let m = Matrix3x3::from_rotation_z(0.4) * Matrix3x3::from_scale(Vec3::splat(3.0));
        let (s, r) = m.scale_rotation();
        assert!((s.x - 3.0).abs() < 1e-4, "got {s:?}");
        assert!(r.is_rotation());
    }

    #[test]
    fn layout_is_nine_contiguous_floats() {
        assert_eq!(std::mem::size_of::<Matrix3x3>(), 36);
    }
}
