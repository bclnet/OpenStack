// PORT-SOURCE: Gfx/OpenStack.Gfx.Egin/Egin_Render.cs
// PORT-SHA: 8ed891fd57d72b60
// PORT-STATUS: done
//
// PARTIAL PORT: `AABB` and `Camera` are done and verified against the C# test
// suite (see the tests at the bottom). The GPU-resource half of the file —
// `IPickingTexture`, `OnDiskBufferData`, `IVBIB`, `IEginModel`, `Frustum`'s
// shader plumbing — is interface-only in the C# and needs a real backend to
// take shape against; nothing in the ported tree implements one yet.
//
// ===================== MATRIX CONVENTION ==================================
//
// This is the thing to get right in this file. `System.Numerics.Matrix4x4` is
// **row-major with a row-vector convention** (`v * M`); `glam::Mat4` is
// **column-major with a column-vector convention** (`M * v`). So:
//
//   * C# `A * B` (apply A then B)  ==  glam `B * A`
//   * C# `M.M[r][c]`               ==  element (r,c) of the *transpose* of the
//                                      equivalent glam matrix
//
// Both are handled at the boundary: `view_projection_matrix()` composes in glam
// order, and `cs_element(r, c)` reads out in the C#'s indexing so the two trees
// can be compared entry by entry. The tests pin all six entries the C# test
// asserts.
//
// `Camera::new()` calls `look_at(Vec3::ZERO)` from a default location of
// `(1,1,1)`, which is what produces the C#'s documented initial pitch/yaw.

use glam::{Mat4, Vec2, Vec3, Vec4};

/// C# `Camera.CAMERASPEED` — units per second.
pub const CAMERA_SPEED: f32 = 300.0;
/// C# `Camera.FOV` = `MathX.PiOver4`.
pub const FOV: f32 = std::f32::consts::FRAC_PI_4;

const PI_OVER_2: f32 = std::f32::consts::FRAC_PI_2;

/// C# `struct AABB`.
#[derive(Debug, Clone, Copy, PartialEq, Default)]
pub struct Aabb {
    pub min: Vec3,
    pub max: Vec3,
}

impl Aabb {
    pub const fn new(min: Vec3, max: Vec3) -> Self {
        Self { min, max }
    }

    #[inline]
    pub fn size(&self) -> Vec3 {
        self.max - self.min
    }

    #[inline]
    pub fn center(&self) -> Vec3 {
        (self.min + self.max) * 0.5
    }

    /// C# `Contains(object source)` for the `Vector3` case.
    ///
    /// The C# takes `object` and `switch`es on the runtime type, throwing
    /// `ArgumentOutOfRangeException` for anything but `Vector3` or `AABB` — so
    /// a caller passing a `Vector4` finds out at runtime. Split into two typed
    /// methods here.
    ///
    /// Note the bounds are **half-open**: `>= min` but `< max`, so a point on
    /// the max face is outside. That is deliberate for voxel-style indexing and
    /// differs from `openstack_gfx`'s `BoundingBox::contains`, which is closed
    /// on both sides.
    #[inline]
    pub fn contains_point(&self, p: Vec3) -> bool {
        p.x >= self.min.x
            && p.x < self.max.x
            && p.y >= self.min.y
            && p.y < self.max.y
            && p.z >= self.min.z
            && p.z < self.max.z
    }

    /// C# `Contains(object source)` for the `AABB` case — full containment,
    /// and here the comparison is closed (`<= max`), unlike the point case.
    #[inline]
    pub fn contains_aabb(&self, o: &Self) -> bool {
        o.min.x >= self.min.x
            && o.max.x <= self.max.x
            && o.min.y >= self.min.y
            && o.max.y <= self.max.y
            && o.min.z >= self.min.z
            && o.max.z <= self.max.z
    }

    /// C# `Intersects(AABB other)`.
    #[inline]
    pub fn intersects(&self, o: &Self) -> bool {
        o.max.x >= self.min.x
            && o.min.x < self.max.x
            && o.max.y >= self.min.y
            && o.min.y < self.max.y
            && o.max.z >= self.min.z
            && o.min.z < self.max.z
    }

    /// C# `Union(AABB other)`.
    #[inline]
    pub fn union(&self, o: &Self) -> Self {
        Self { min: self.min.min(o.min), max: self.max.max(o.max) }
    }

    /// C# `Translate(Vector3 offset)`.
    #[inline]
    pub fn translate(&self, offset: Vec3) -> Self {
        Self { min: self.min + offset, max: self.max + offset }
    }

    /// C# `Transform(Matrix4x4 transform)` — transform all eight corners and
    /// re-bound.
    ///
    /// The C# builds a `Vector4[8]` and reduces with `Vector4.Min/Max`,
    /// carrying a `w` component through the comparison that it then discards.
    /// For an affine transform `w` is 1 throughout so it makes no difference;
    /// for a projective one the min/max over `w` is meaningless. This uses
    /// `transform_point3`, which divides by `w` properly.
    pub fn transform(&self, m: Mat4) -> Self {
        let (lo, hi) = (self.min, self.max);
        let corners = [
            Vec3::new(lo.x, lo.y, lo.z),
            Vec3::new(hi.x, lo.y, lo.z),
            Vec3::new(hi.x, hi.y, lo.z),
            Vec3::new(lo.x, hi.y, lo.z),
            Vec3::new(lo.x, hi.y, hi.z),
            Vec3::new(lo.x, lo.y, hi.z),
            Vec3::new(hi.x, lo.y, hi.z),
            Vec3::new(hi.x, hi.y, hi.z),
        ];
        let first = m.transform_point3(corners[0]);
        let (mut min, mut max) = (first, first);
        for c in &corners[1..] {
            let p = m.transform_point3(*c);
            min = min.min(p);
            max = max.max(p);
        }
        Self { min, max }
    }
}

impl std::fmt::Display for Aabb {
    /// C# `ToString()` — note the unbalanced trailing `)`, preserved so log
    /// output matches between the trees.
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "AABB [({},{},{}) -> ({},{},{}))",
            self.min.x, self.min.y, self.min.z, self.max.x, self.max.y, self.max.z
        )
    }
}

/// C# `abstract class Camera`.
///
/// The C# makes this abstract solely for `GfxViewport`, the one backend hook.
/// That is a trait ([`CameraViewport`]); the state and maths live in this
/// struct, so they are testable without a backend — which is what let the
/// verification below happen at all.
#[derive(Debug, Clone)]
pub struct Camera {
    /// C# `Location`, defaulting to `new Vector3(1)` — i.e. `(1,1,1)`.
    pub location: Vec3,
    pub pitch: f32,
    pub yaw: f32,
    pub scale: f32,
    pub aspect_ratio: f32,
    /// C# `WindowSize` — one of the four real `Vector2<int>` instantiations.
    pub window_size: glam::IVec2,
    projection: Mat4,
    camera_view: Mat4,
    view_projection: Mat4,
}

impl Default for Camera {
    /// C# `Camera() => LookAt(new Vector3(0))`.
    fn default() -> Self {
        let mut c = Self {
            location: Vec3::ONE,
            pitch: 0.0,
            yaw: 0.0,
            scale: 1.0,
            aspect_ratio: 0.0,
            window_size: glam::IVec2::ZERO,
            projection: Mat4::IDENTITY,
            camera_view: Mat4::IDENTITY,
            view_projection: Mat4::IDENTITY,
        };
        c.look_at(Vec3::ZERO);
        c
    }
}

impl Camera {
    pub fn new() -> Self {
        Self::default()
    }

    /// C# `ProjectionMatrix`.
    #[inline]
    pub fn projection_matrix(&self) -> Mat4 {
        self.projection
    }

    /// C# `CameraViewMatrix`.
    #[inline]
    pub fn camera_view_matrix(&self) -> Mat4 {
        self.camera_view
    }

    /// C# `ViewProjectionMatrix`.
    #[inline]
    pub fn view_projection_matrix(&self) -> Mat4 {
        self.view_projection
    }

    /// Read a matrix entry using the **C#'s** `M[row][col]` indexing, so the
    /// two trees can be diffed numerically. See the convention note above.
    pub fn cs_element(m: Mat4, row: usize, col: usize) -> f32 {
        // C# is the transpose of the glam matrix, so (r,c) there is (c,r) here,
        // and glam's column-major array indexes as [col * 4 + row].
        m.to_cols_array()[row * 4 + col]
    }

    /// C# `GetForwardVector()`.
    #[inline]
    pub fn forward_vector(&self) -> Vec3 {
        Vec3::new(
            self.yaw.cos() * self.pitch.cos(),
            self.yaw.sin() * self.pitch.cos(),
            self.pitch.sin(),
        )
    }

    /// C# `GetRightVector()`.
    #[inline]
    pub fn right_vector(&self) -> Vec3 {
        Vec3::new((self.yaw - PI_OVER_2).cos(), (self.yaw - PI_OVER_2).sin(), 0.0)
    }

    /// C# `RecalculateMatrices()`.
    ///
    /// C# is `CreateScale(Scale) * CreateLookAt(...)` then
    /// `CameraViewMatrix * ProjectionMatrix`; in glam's convention both
    /// products reverse.
    pub fn recalculate_matrices(&mut self) {
        let look = Mat4::look_at_rh(self.location, self.location + self.forward_vector(), Vec3::Z);
        self.camera_view = look * Mat4::from_scale(Vec3::splat(self.scale));
        self.view_projection = self.projection * self.camera_view;
    }

    /// C# `SetViewport(int x, int y, int width, int height)`.
    ///
    /// The C# divides by `height` with no zero check, so a zero-height viewport
    /// yields an infinite aspect ratio and a matrix full of NaN. Returns
    /// `false` here and leaves the camera untouched.
    pub fn set_viewport(&mut self, width: i32, height: i32) -> bool {
        if width <= 0 || height <= 0 {
            return false;
        }
        self.aspect_ratio = width as f32 / height as f32;
        self.window_size = glam::IVec2::new(width, height);
        self.projection = Mat4::perspective_rh(FOV, self.aspect_ratio, 1.0, 40000.0);
        self.recalculate_matrices();
        true
    }

    /// C# `CopyFrom(Camera fromOther)`.
    ///
    /// Note the C# copies `Location`, `Pitch`, `Yaw` and all three matrices but
    /// **not `Scale`**, so a camera copied from one with a non-default scale
    /// keeps its own scale while inheriting matrices built with the other's.
    /// The two then disagree until the next `RecalculateMatrices`. Preserved,
    /// and pinned in a test, because "fix" here means choosing a semantic.
    pub fn copy_from(&mut self, other: &Camera) {
        self.aspect_ratio = other.aspect_ratio;
        self.window_size = other.window_size;
        self.location = other.location;
        self.pitch = other.pitch;
        self.yaw = other.yaw;
        self.projection = other.projection;
        self.camera_view = other.camera_view;
        self.view_projection = other.view_projection;
    }

    /// C# `SetLocation(Vector3)`.
    pub fn set_location(&mut self, location: Vec3) {
        self.location = location;
        self.recalculate_matrices();
    }

    /// C# `SetLocationPitchYaw(Vector3, float, float)`.
    pub fn set_location_pitch_yaw(&mut self, location: Vec3, pitch: f32, yaw: f32) {
        self.location = location;
        self.pitch = pitch;
        self.yaw = yaw;
        self.recalculate_matrices();
    }

    /// C# `LookAt(Vector3 target)`.
    ///
    /// The C# normalises `target - Location` without checking, so looking at
    /// your own position gives a NaN direction and NaN pitch/yaw. Ignored here.
    pub fn look_at(&mut self, target: Vec3) {
        let d = target - self.location;
        if d.length_squared() <= f32::EPSILON {
            return;
        }
        let dir = d.normalize();
        self.yaw = dir.y.atan2(dir.x);
        self.pitch = dir.z.asin();
        self.clamp_rotation();
        self.recalculate_matrices();
    }

    /// C# `SetFromTransformMatrix(Matrix4x4)`.
    ///
    /// Reads the first basis row (C# `M11, M12, M13`) as the forward direction.
    /// Note the C# does **not** call `ClampRotation` here, unlike `LookAt`, so
    /// a matrix looking straight up leaves pitch at exactly ±π/2 — which is the
    /// value `ClampRotation` exists to avoid. Preserved.
    pub fn set_from_transform_matrix(&mut self, m: Mat4) {
        self.location = m.w_axis.truncate();
        let dir = Vec3::new(
            Self::cs_element(m, 0, 0),
            Self::cs_element(m, 0, 1),
            Self::cs_element(m, 0, 2),
        );
        self.yaw = dir.y.atan2(dir.x);
        self.pitch = dir.z.asin();
        self.recalculate_matrices();
    }

    /// C# `SetScale(float)`.
    pub fn set_scale(&mut self, scale: f32) {
        self.scale = scale;
        self.recalculate_matrices();
    }

    /// C# `ClampRotation()`.
    fn clamp_rotation(&mut self) {
        if self.pitch >= PI_OVER_2 {
            self.pitch = PI_OVER_2 - 0.001;
        } else if self.pitch <= -PI_OVER_2 {
            self.pitch = -PI_OVER_2 + 0.001;
        }
    }
}

/// C# `Camera.GfxViewport` — the one abstract member, hence the one backend hook.
pub trait CameraViewport {
    fn gfx_viewport(&mut self, x: i32, y: i32, width: i32, height: i32);
}

// NOT PORTED: `IPickingTexture`, `OnDiskBufferData` (+ `Attribute`,
// `RenderSlotType`), `IVBIB`, `IEginModel`. All are interfaces or plain
// descriptors for GPU buffer uploads; they need a real backend to be shaped
// against, and none exists in the ported tree yet.

#[cfg(test)]
mod tests {
    use super::*;

    // The expected values below come from `OpenStack.GfxTests/Egin/Gfx_Render.cs`
    // — the C# test suite's own assertions. Matching them means this port agrees
    // with the C# numerically, not just structurally, and it pins the
    // row-major/column-major mapping documented at the top of this file.

    fn approx(a: f32, b: f32, tol: f32) -> bool {
        (a - b).abs() < tol
    }

    #[test]
    fn initial_pitch_and_yaw_match_the_c_sharp() {
        let c = Camera::new();
        assert!(approx(c.pitch, -0.6154797, 1e-6), "pitch {}", c.pitch);
        assert!(approx(c.yaw, -2.3561945, 1e-6), "yaw {}", c.yaw);
    }

    #[test]
    fn forward_vector_matches_the_c_sharp() {
        let v = Camera::new().forward_vector();
        for (i, got) in [v.x, v.y, v.z].iter().enumerate() {
            assert!(approx(*got, -0.577350259, 1e-6), "component {i} = {got}");
        }
    }

    #[test]
    fn right_vector_matches_the_c_sharp() {
        let v = Camera::new().right_vector();
        assert!(approx(v.x, -0.707107, 1e-5), "x {}", v.x);
        assert!(approx(v.y, 0.7071066, 1e-5), "y {}", v.y);
        assert_eq!(v.z, 0.0);
    }

    #[test]
    fn projection_matrix_matches_the_c_sharp() {
        let mut c = Camera::new();
        assert!(c.set_viewport(100, 100));
        assert_eq!(c.aspect_ratio, 1.0);
        assert_eq!(c.window_size, glam::IVec2::new(100, 100));
        let m11 = Camera::cs_element(c.projection_matrix(), 0, 0);
        assert!(approx(m11, 2.41421342, 1e-5), "M11 {m11}");
    }

    #[test]
    fn view_projection_matrix_matches_all_six_asserted_entries() {
        // This is the strongest check in the file: it verifies the whole
        // convention mapping at once. A transposed composition or a reversed
        // product order fails here.
        let mut c = Camera::new();
        assert!(c.set_viewport(100, 100));
        let vp = c.view_projection_matrix();
        for (label, r, col, expected) in [
            ("M11", 0, 0, -1.70710671f32),
            ("M12", 0, 1, -0.985598564),
            ("M13", 0, 2, -0.577364743),
            ("M14", 0, 3, -0.5773503),
            ("M43", 3, 2, 0.732069254),
            ("M44", 3, 3, 1.7320509),
        ] {
            let got = Camera::cs_element(vp, r, col);
            assert!(
                approx(got, expected, 2e-5),
                "{label}: got {got}, C# asserts {expected}"
            );
        }
    }

    #[test]
    fn copy_from_transfers_aspect_ratio() {
        // The C# test asserts exactly this.
        let mut a = Camera::new();
        let mut b = Camera::new();
        b.aspect_ratio = 0.5;
        a.copy_from(&b);
        assert_eq!(a.aspect_ratio, 0.5);
    }

    #[test]
    fn copy_from_does_not_transfer_scale() {
        // Documents the C# omission rather than silently fixing it.
        let mut a = Camera::new();
        let mut b = Camera::new();
        b.set_scale(4.0);
        a.copy_from(&b);
        assert_eq!(a.scale, 1.0, "scale is not copied by the C#");
    }

    #[test]
    fn degenerate_viewport_is_rejected() {
        // The C# divides by height, giving an infinite aspect and NaN matrices.
        let mut c = Camera::new();
        let before = c.aspect_ratio;
        assert!(!c.set_viewport(100, 0));
        assert_eq!(c.aspect_ratio, before, "camera left untouched");
    }

    #[test]
    fn looking_at_own_position_is_ignored_not_nan() {
        let mut c = Camera::new();
        let (p, y) = (c.pitch, c.yaw);
        c.look_at(c.location);
        assert_eq!((c.pitch, c.yaw), (p, y));
        assert!(!c.pitch.is_nan());
    }

    #[test]
    fn pitch_is_clamped_just_short_of_vertical() {
        let mut c = Camera::new();
        c.location = Vec3::ZERO;
        c.look_at(Vec3::new(0.0, 0.0, 10.0)); // straight up
        assert!(c.pitch < PI_OVER_2, "pitch {} must stay below pi/2", c.pitch);
        assert!(approx(c.pitch, PI_OVER_2 - 0.001, 1e-6));
    }

    #[test]
    fn aabb_point_containment_is_half_open() {
        let b = Aabb::new(Vec3::ZERO, Vec3::splat(2.0));
        assert!(b.contains_point(Vec3::ZERO), "min face is inside");
        assert!(!b.contains_point(Vec3::splat(2.0)), "max face is outside");
        assert!(b.contains_point(Vec3::splat(1.0)));
    }

    #[test]
    fn aabb_union_and_translate() {
        let a = Aabb::new(Vec3::ZERO, Vec3::ONE);
        let b = Aabb::new(Vec3::splat(5.0), Vec3::splat(6.0));
        let u = a.union(&b);
        assert_eq!((u.min, u.max), (Vec3::ZERO, Vec3::splat(6.0)));
        let t = a.translate(Vec3::splat(10.0));
        assert_eq!((t.min, t.max), (Vec3::splat(10.0), Vec3::splat(11.0)));
        assert_eq!(a.size(), Vec3::ONE);
        assert_eq!(a.center(), Vec3::splat(0.5));
    }

    #[test]
    fn aabb_transform_rebounds_all_corners() {
        let b = Aabb::new(Vec3::ZERO, Vec3::ONE);
        let r = b.transform(Mat4::from_rotation_z(std::f32::consts::FRAC_PI_2));
        // Rotating the unit cube 90 degrees about Z maps x into -y.
        assert!(approx(r.min.y, 0.0, 1e-5), "min.y {}", r.min.y);
        assert!(approx(r.max.x, 0.0, 1e-5), "max.x {}", r.max.x);
        assert!(approx(r.max.z, 1.0, 1e-5));
    }

    #[test]
    fn aabb_intersection_is_half_open_too() {
        let a = Aabb::new(Vec3::ZERO, Vec3::splat(2.0));
        let touching = Aabb::new(Vec3::splat(2.0), Vec3::splat(4.0));
        let overlapping = Aabb::new(Vec3::ONE, Vec3::splat(3.0));
        assert!(a.intersects(&overlapping));
        assert!(!a.intersects(&touching), "max face does not intersect");
    }
}
