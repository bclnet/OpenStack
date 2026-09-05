// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Matrix2x2.cs
// PORT-SHA: 075ce425754a645d
// PORT-STATUS: done
//
// A 2x2 float matrix. glam's `Mat2` covers the arithmetic, so this is a thin
// on-disk representation plus conversions — `Mat2` is column-major and SIMD-
// aligned, so it is not layout-compatible with the C# field order.
//
// C#-SIDE BUG: `Matrix2x2.Transpose` is declared as returning `Matrix3x3`, not
// `Matrix2x2`. It fills only M11/M12/M21/M22 and leaves M13/M23/M31/M32/M33 at
// zero, so the result is a degenerate 3x3 with a zero determinant — every
// caller that inverts or composes it gets silent garbage. Ported here with the
// correct 2x2 return type; `transpose_as_3x3_bug_compat` preserves the old
// shape for any caller that depends on it. **Fix this in the C# tree.**

use bytemuck::{Pod, Zeroable};
use glam::{Mat2, Vec2};

use super::matrix3x3::Matrix3x3;

/// C# `struct Matrix2x2`. Row-major, `m[row][col]`.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Pod, Zeroable)]
pub struct Matrix2x2 {
    pub m11: f32, pub m12: f32,
    pub m21: f32, pub m22: f32,
}

impl Default for Matrix2x2 {
    fn default() -> Self {
        Self::IDENTITY
    }
}

impl Matrix2x2 {
    /// C# `Identity`.
    pub const IDENTITY: Self = Self { m11: 1.0, m12: 0.0, m21: 0.0, m22: 1.0 };

    pub const fn new(m11: f32, m12: f32, m21: f32, m22: f32) -> Self {
        Self { m11, m12, m21, m22 }
    }

    /// C# `Transpose`, with the return type it should have had.
    #[inline]
    pub fn transpose(&self) -> Self {
        Self { m11: self.m11, m12: self.m21, m21: self.m12, m22: self.m22 }
    }

    /// The C# `Transpose` verbatim, degenerate 3x3 and all.
    ///
    /// Only for call sites that depend on the broken shape. New code wants
    /// [`transpose`](Self::transpose).
    #[deprecated(note = "mirrors a C#-side bug: yields a 3x3 with a zero determinant")]
    pub fn transpose_as_3x3_bug_compat(&self) -> Matrix3x3 {
        let mut m = Matrix3x3::ZERO;
        m.m11 = self.m11;
        m.m12 = self.m21;
        m.m21 = self.m12;
        m.m22 = self.m22;
        m
    }

    /// C# `Mult(Vector2)`.
    #[inline]
    pub fn mult(&self, v: Vec2) -> Vec2 {
        Vec2::new(
            self.m11 * v.x + self.m12 * v.y,
            self.m21 * v.x + self.m22 * v.y,
        )
    }

    /// glam interop. `Mat2` is column-major, so this transposes.
    #[inline]
    pub fn to_mat2(&self) -> Mat2 {
        Mat2::from_cols_array(&[self.m11, self.m21, self.m12, self.m22])
    }

    #[inline]
    pub fn from_mat2(m: Mat2) -> Self {
        Self { m11: m.x_axis.x, m12: m.y_axis.x, m21: m.x_axis.y, m22: m.y_axis.y }
    }

    /// C# `GetDeterminant`.
    #[inline]
    pub fn determinant(&self) -> f32 {
        self.m11 * self.m22 - self.m12 * self.m21
    }
}

impl std::ops::Add for Matrix2x2 {
    type Output = Self;
    fn add(self, o: Self) -> Self {
        Self::new(self.m11 + o.m11, self.m12 + o.m12, self.m21 + o.m21, self.m22 + o.m22)
    }
}

impl std::ops::Sub for Matrix2x2 {
    type Output = Self;
    fn sub(self, o: Self) -> Self {
        Self::new(self.m11 - o.m11, self.m12 - o.m12, self.m21 - o.m21, self.m22 - o.m22)
    }
}

impl std::ops::Neg for Matrix2x2 {
    type Output = Self;
    fn neg(self) -> Self {
        Self::new(-self.m11, -self.m12, -self.m21, -self.m22)
    }
}

impl std::ops::Mul for Matrix2x2 {
    type Output = Self;
    fn mul(self, o: Self) -> Self {
        Self::new(
            self.m11 * o.m11 + self.m12 * o.m21,
            self.m11 * o.m12 + self.m12 * o.m22,
            self.m21 * o.m11 + self.m22 * o.m21,
            self.m21 * o.m12 + self.m22 * o.m22,
        )
    }
}

impl std::ops::Mul<f32> for Matrix2x2 {
    type Output = Self;
    fn mul(self, s: f32) -> Self {
        Self::new(self.m11 * s, self.m12 * s, self.m21 * s, self.m22 * s)
    }
}

impl std::fmt::Display for Matrix2x2 {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{{ {{M11:{} M12:{}}} {{M21:{} M22:{}}} }}", self.m11, self.m12, self.m21, self.m22)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn transpose_swaps_the_off_diagonal() {
        let m = Matrix2x2::new(1.0, 2.0, 3.0, 4.0);
        assert_eq!(m.transpose(), Matrix2x2::new(1.0, 3.0, 2.0, 4.0));
        assert_eq!(m.transpose().transpose(), m);
    }

    #[test]
    fn the_c_sharp_transpose_produces_a_degenerate_matrix() {
        // Documents why the bug matters: the 3x3 it returns is not invertible.
        #[allow(deprecated)]
        let bad = Matrix2x2::new(1.0, 2.0, 3.0, 4.0).transpose_as_3x3_bug_compat();
        assert_eq!(bad.determinant(), 0.0);
    }

    #[test]
    fn multiplication_agrees_with_glam() {
        let a = Matrix2x2::new(1.0, 2.0, 3.0, 4.0);
        let b = Matrix2x2::new(5.0, 6.0, 7.0, 8.0);
        assert_eq!(a * b, Matrix2x2::from_mat2(a.to_mat2() * b.to_mat2()));
    }

    #[test]
    fn identity_is_neutral() {
        let m = Matrix2x2::new(1.0, 2.0, 3.0, 4.0);
        assert_eq!(m * Matrix2x2::IDENTITY, m);
        assert_eq!(m.mult(Vec2::new(1.0, 0.0)), Vec2::new(1.0, 3.0));
    }

    #[test]
    fn determinant_matches_glam() {
        let m = Matrix2x2::new(1.0, 2.0, 3.0, 4.0);
        assert!((m.determinant() - m.to_mat2().determinant()).abs() < 1e-6);
    }
}
