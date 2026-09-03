// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Polyfill.cs
// PORT-SHA: 9343925f5ab6a625
// PORT-STATUS: done
//
// Free functions over the numeric types, plus the small `Int2`/`Int3` structs.
// C# hangs most of these off `Vector3`/`Matrix4x4` as extension methods; here
// they are plain functions, since glam owns those types and Rust will not let a
// foreign trait be implemented on a foreign type.

use bytemuck::{Pod, Zeroable};
use glam::{Mat3, Mat4, Quat, Vec3};

use super::matrix3x3::Matrix3x3;
use super::matrix3x4::Matrix3x4;

/// C# `Polyfill.EPSILON`.
pub const EPSILON: f32 = 1e-6;

/// C# `struct Int2`.
///
/// The C# defines this alongside `Vector2<int>` and both are in use.
/// `glam::IVec2` is the better home for new code; this stays for the on-disk
/// layout and the `ToString` format callers parse.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Hash, Pod, Zeroable)]
pub struct Int2 {
    pub x: i32,
    pub y: i32,
}

impl Int2 {
    pub const ZERO: Self = Self { x: 0, y: 0 };

    pub const fn new(x: i32, y: i32) -> Self {
        Self { x, y }
    }
}

impl std::fmt::Display for Int2 {
    /// C# `ToString() => $"{X},{Y}"`.
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{},{}", self.x, self.y)
    }
}

impl From<Int2> for glam::IVec2 {
    fn from(v: Int2) -> Self {
        glam::IVec2::new(v.x, v.y)
    }
}

/// C# `struct Int3`.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Hash, Pod, Zeroable)]
pub struct Int3 {
    pub x: i32,
    pub y: i32,
    pub z: i32,
}

impl Int3 {
    pub const ZERO: Self = Self { x: 0, y: 0, z: 0 };

    pub const fn new(x: i32, y: i32, z: i32) -> Self {
        Self { x, y, z }
    }
}

impl std::fmt::Display for Int3 {
    /// C# `ToString() => $"{X},{Y},{Z}"`.
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{},{},{}", self.x, self.y, self.z)
    }
}

impl From<Int3> for glam::IVec3 {
    fn from(v: Int3) -> Self {
        glam::IVec3::new(v.x, v.y, v.z)
    }
}

/// C# `IsZero(this Vector3)`.
#[inline]
pub fn is_zero(v: Vec3) -> bool {
    v.x == 0.0 && v.y == 0.0 && v.z == 0.0
}

/// C# `IsZeroEpsilon(this Vector3)` / `NearZero(this Vector3)` — the two are
/// identical in the C#.
#[inline]
pub fn is_zero_epsilon(v: Vec3) -> bool {
    v.x.abs() < EPSILON && v.y.abs() < EPSILON && v.z.abs() < EPSILON
}

/// C# `ConvertToTransformationMatrix(Vector3 scale, Vector3 position, Vector3 pitchYawRoll)`.
///
/// The C# applies pitch (X), then yaw (Y), then roll (Z) — glam's
/// `from_euler(EulerRot::XYZ, ..)` is the same order.
pub fn to_transformation_matrix(scale: Vec3, position: Vec3, pitch_yaw_roll: Vec3) -> Mat4 {
    Mat4::from_scale_rotation_translation(
        scale,
        Quat::from_euler(
            glam::EulerRot::XYZ,
            pitch_yaw_roll.x,
            pitch_yaw_roll.y,
            pitch_yaw_roll.z,
        ),
        position,
    )
}

/// C# `ConvertToTransformationMatrix(string scale, string position, string angles)`.
///
/// Returns `None` on unparseable input; the C# substituted zero vectors
/// silently, which turns a typo into a collapsed transform.
pub fn to_transformation_matrix_str(scale: &str, position: &str, angles: &str) -> Option<Mat4> {
    Some(to_transformation_matrix(
        super::vector3::parse_vec3(scale)?,
        super::vector3::parse_vec3(position)?,
        super::vector3::parse_vec3(angles)?,
    ))
}

/// C# `ConvertToRotationMatrix(this Quaternion)`.
#[inline]
pub fn quat_to_rotation_matrix(q: Quat) -> Matrix3x3 {
    Matrix3x3::from_quat(q)
}

/// C# `GetRotation(this Matrix4x4)` — the upper-left 3x3, scale removed.
pub fn get_rotation(m: Mat4) -> Matrix3x3 {
    let (_, r, _) = m.to_scale_rotation_translation();
    Matrix3x3::from_quat(r)
}

/// C# `GetScale(this Matrix4x4)`.
#[inline]
pub fn get_scale(m: Mat4) -> Vec3 {
    m.to_scale_rotation_translation().0
}

/// C# `GetTranslation(this Matrix4x4)`.
#[inline]
pub fn get_translation(m: Mat4) -> Vec3 {
    m.w_axis.truncate()
}

/// C# `ToMatrix4x4(this Matrix3x4)`.
#[inline]
pub fn matrix3x4_to_mat4(m: Matrix3x4) -> Mat4 {
    m.to_mat4()
}

/// C# `GetRotationMatrix(this Matrix3x3)` — widen to 4x4.
#[inline]
pub fn rotation_matrix_to_mat4(m: Matrix3x3) -> Mat4 {
    Mat4::from_mat3(m.to_mat3())
}

/// C# `CreateTransformFromParts(Vector3 translation, Matrix3x3 rotation)`.
#[inline]
pub fn transform_from_parts(translation: Vec3, rotation: Matrix3x3) -> Mat4 {
    Mat4::from_translation(translation) * Mat4::from_mat3(rotation.to_mat3())
}

/// C# `CreateLocalTransform(Matrix4x4 parent, Matrix4x4 child)` — the child
/// expressed relative to the parent.
///
/// The C# inverts `parent` without checking the result, producing NaNs for a
/// degenerate parent. Returns `None` here.
pub fn create_local_transform(parent: Mat4, child: Mat4) -> Option<Mat4> {
    if parent.determinant().abs() < f32::EPSILON {
        return None;
    }
    Some(parent.inverse() * child)
}

/// C# `Get(this Matrix4x4, int row, int column)`.
#[inline]
pub fn mat4_get(m: Mat4, row: usize, column: usize) -> f32 {
    m.to_cols_array()[column * 4 + row]
}

/// C# `Set(this Matrix4x4, int row, int column, float value)`.
///
/// The C# takes `Matrix4x4` by value, so its `Set` mutates a copy and is
/// discarded — the method cannot do anything. This takes `&mut` and works.
#[inline]
pub fn mat4_set(m: &mut Mat4, row: usize, column: usize, value: f32) {
    let mut a = m.to_cols_array();
    a[column * 4 + row] = value;
    *m = Mat4::from_cols_array(&a);
}

/// C# `Flatten(this Matrix4x4[])` — column-major floats for upload to a GPU.
pub fn flatten(mats: &[Mat4]) -> Vec<f32> {
    let mut out = Vec::with_capacity(mats.len() * 16);
    for m in mats {
        out.extend_from_slice(&m.to_cols_array());
    }
    out
}

/// glam interop for `Mat3`, kept beside the rest for discoverability.
#[inline]
pub fn mat3_from_rows(rows: [[f32; 3]; 3]) -> Mat3 {
    Matrix3x3::new(
        rows[0][0], rows[0][1], rows[0][2],
        rows[1][0], rows[1][1], rows[1][2],
        rows[2][0], rows[2][1], rows[2][2],
    )
    .to_mat3()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn transform_decomposes_back_to_its_parts() {
        let (s, p) = (Vec3::new(2.0, 2.0, 2.0), Vec3::new(1.0, 2.0, 3.0));
        let m = to_transformation_matrix(s, p, Vec3::ZERO);
        assert!((get_scale(m) - s).length() < 1e-5);
        assert!((get_translation(m) - p).length() < 1e-5);
    }

    #[test]
    fn local_transform_undoes_the_parent() {
        let parent = Mat4::from_translation(Vec3::new(5.0, 0.0, 0.0));
        let child = Mat4::from_translation(Vec3::new(7.0, 0.0, 0.0));
        let local = create_local_transform(parent, child).unwrap();
        assert!((get_translation(local) - Vec3::new(2.0, 0.0, 0.0)).length() < 1e-5);
    }

    #[test]
    fn degenerate_parent_returns_none_instead_of_nan() {
        let degenerate = Mat4::from_scale(Vec3::ZERO);
        assert!(create_local_transform(degenerate, Mat4::IDENTITY).is_none());
    }

    #[test]
    fn mat4_set_actually_mutates() {
        // The C# equivalent silently does nothing (it mutates a by-value copy).
        let mut m = Mat4::IDENTITY;
        mat4_set(&mut m, 1, 2, 9.0);
        assert_eq!(mat4_get(m, 1, 2), 9.0);
        assert_eq!(mat4_get(m, 2, 1), 0.0, "must not touch the transpose slot");
    }

    #[test]
    fn flatten_is_column_major_and_contiguous() {
        let f = flatten(&[Mat4::IDENTITY, Mat4::IDENTITY]);
        assert_eq!(f.len(), 32);
        assert_eq!(f[0], 1.0);
        assert_eq!(f[5], 1.0);
    }

    #[test]
    fn int_types_format_like_the_c_sharp() {
        assert_eq!(Int2::new(3, 4).to_string(), "3,4");
        assert_eq!(Int3::new(1, 2, 3).to_string(), "1,2,3");
        assert_eq!(Int3::ZERO.to_string(), "0,0,0");
    }

    #[test]
    fn string_transform_rejects_bad_input() {
        assert!(to_transformation_matrix_str("1 1 1", "0 0 0", "0 0 0").is_some());
        assert!(to_transformation_matrix_str("1 1", "0 0 0", "0 0 0").is_none());
    }
}
