// PORT-SOURCE: Core/OpenStack.Polyfills/System.Numerics/NumericsExtensions.cs
// PORT-SHA: 089ffc59c098a9cd
// PORT-STATUS: done
//
// Two unrelated groups in one C# file:
//
//   1. `Matrix3x3` linear algebra (`Inverse`, `Conjugate`, `ConjugateTranspose`,
//      `ConjugateTransposeThisAndMultiply`, `Diagonal`) — each routed through
//      MathNet.Numerics by converting to a heap-allocated `Matrix<float>`,
//      operating, and converting back. For a 3x3 that is enormously more
//      expensive than the closed form. All five are already implemented
//      directly on `Matrix3x3` in `openstack-polyio`, so they are re-exported
//      here rather than duplicated.
//   2. Debug logging of vectors and matrices, ported below.
//
// Note `Inverse` on the polyio type returns `Option` — MathNet throws on a
// singular matrix where the C# `Matrix3x3.Inverse` property returned NaNs.

use glam::{Mat4, Vec3, Vec4};
use openstack_polyio::system_numerics::matrix3x3::Matrix3x3;

use crate::log::info;

/// C# `LogVector3(this Vector3, string label = null)`.
pub fn log_vec3(v: Vec3, label: Option<&str>) {
    info(&format!("*** WriteVector3 *** - {}", label.unwrap_or("")));
    info(&format!("{:.7}  {:.7}  {:.7}", v.x, v.y, v.z));
    info("");
}

/// C# `LogVector4(this Vector4)`.
pub fn log_vec4(v: Vec4) {
    info("=============================================");
    info(&format!(
        "x:{:.7}  y:{:.7}  z:{:.7} w:{:.7}",
        v.x, v.y, v.z, v.w
    ));
}

/// C# `LogMatrix3x3(this Matrix3x3, string label = null)`.
pub fn log_matrix3x3(m: &Matrix3x3, label: Option<&str>) {
    info(&format!("====== {} ===========", label.unwrap_or("")));
    info(&format!("{:.7}  {:.7}  {:.7}", m.m11, m.m12, m.m13));
    info(&format!("{:.7}  {:.7}  {:.7}", m.m21, m.m22, m.m23));
    info(&format!("{:.7}  {:.7}  {:.7}", m.m31, m.m32, m.m33));
}

/// C# `LogMatrix4x4(this Matrix4x4)`.
///
/// The C# prints `M11..M44` in row order; `Mat4` is column-major, so this
/// transposes on the way out to keep the two logs comparable.
pub fn log_matrix4x4(m: &Mat4) {
    let a = m.to_cols_array();
    info("=============================================");
    for row in 0..4 {
        info(&format!(
            "{:.7}  {:.7}  {:.7}  {:.7}",
            a[row], a[4 + row], a[8 + row], a[12 + row]
        ));
    }
    info("");
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn logging_does_not_panic_without_a_label() {
        log_vec3(Vec3::ONE, None);
        log_vec4(Vec4::ONE);
        log_matrix3x3(&Matrix3x3::IDENTITY, Some("id"));
        log_matrix4x4(&Mat4::IDENTITY);
    }
}
