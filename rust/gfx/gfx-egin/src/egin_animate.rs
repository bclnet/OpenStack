// PORT-SOURCE: Gfx/OpenStack.Gfx.Egin/Egin_Animate.cs
// PORT-SHA: 52745a15b256722b
// PORT-STATUS: done
//
// Skeletal animation: bones with bind poses, per-frame bone transforms, a
// two-slot frame cache with interpolation, and a playback controller.
//
// ===================== SIX C#-SIDE BUGS ==================================
//
//   1. **`Bone.SetParent` checks the wrong collection.**
//
//          if (!Children.Contains(parent)) { Parent = parent; parent.Children.Add(this); }
//
//      The guard asks "is `parent` one of *my* children" — which is a cycle
//      check, not a duplicate check. It never tests whether `this` is already
//      in `parent.Children`, so calling `SetParent(p)` twice appends `this`
//      to `p.Children` twice. The C# test calls it once and asserts a count of
//      1, so it passes; a second call gives 2. **Fix this in the C# tree.**
//
//   2. **`Matrix4x4.Invert`'s return value is discarded.** On failure
//      `System.Numerics` writes a matrix of NaNs into the `out` parameter, so a
//      non-invertible bind pose yields a NaN `InverseBindPose` that silently
//      poisons every vertex skinned through it.
//
//   3. **`FrameCache.GetFrame` divides by `anim.FrameCount`** (`% FrameCount`)
//      with no zero check, so an animation with no frames throws
//      `DivideByZeroException`. A negative `time` also produces a negative
//      frame index, which is then passed to `DecodeFrame`.
//
//   4. **`Frame.SetAttribute`'s wrong-type case is silent in release.** The
//      `default:` arm only logs under `#if DEBUG`, so in a release build
//      `SetAttribute(0, Scale, Vector3.One)` does nothing at all and reports
//      nothing. The three overloads are distinguished by argument type, so
//      passing the wrong one is easy.
//
//   5. **`AnimationController.GetAnimationMatrices` dereferences
//      `ActiveAnimation` unchecked**, so calling it before `SetAnimation`
//      throws. `Update` guards for null in the same class; this does not.
//
//   6. **`PauseLastFrame` half-works with no animation.** It sets
//      `IsPaused = true`, then assigns `Frame`, whose setter early-returns when
//      `ActiveAnimation` is null — leaving the controller paused at frame 0 and
//      primed to throw from (5).
//
// Also: `FrameCache.FrameFactory` is a public mutable static, and
// `IAnimation.GetAnimationMatrices` takes `object index` (float or int),
// throwing `ArgumentOutOfRangeException` for anything else. Both are typed here.

use glam::{Mat4, Quat, Vec3};

/// C# `enum ChannelAttribute`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum ChannelAttribute {
    Position = 0,
    Angle = 1,
    Scale = 2,
    Unknown = 3,
}

/// C# `class Bone`.
///
/// The C# holds `Parent` and `Children` as direct references, which is a cyclic
/// object graph. Rust models the hierarchy by index into the skeleton's bone
/// array instead — the same information without `Rc<RefCell<..>>`, and it makes
/// bug 1 impossible to express.
#[derive(Debug, Clone, PartialEq)]
pub struct Bone {
    pub index: i32,
    pub name: String,
    pub position: Vec3,
    pub angle: Quat,
    pub bind_pose: Mat4,
    pub inverse_bind_pose: Mat4,
    /// C# `Parent`, as an index. `None` for a root.
    pub parent: Option<usize>,
    /// C# `Children`, as indices.
    pub children: Vec<usize>,
}

impl Bone {
    /// C# `Bone(int index, string name, Vector3 position, Quaternion rotation)`.
    ///
    /// The C# builds `CreateFromQuaternion(rotation) * CreateTranslation(position)`
    /// in row-vector order; glam reverses the product. Returns `None` when the
    /// bind pose is not invertible, rather than storing NaNs (bug 2).
    ///
    /// Note the C# test passes `Quaternion.Zero` — `(0,0,0,0)`, which is not a
    /// valid rotation. The standard quaternion-to-matrix formula yields identity
    /// for it in both languages, so the behaviour matches; but a zero quaternion
    /// reaching here at all is a caller bug worth catching upstream.
    pub fn new(index: i32, name: impl Into<String>, position: Vec3, rotation: Quat) -> Option<Self> {
        let bind_pose = Mat4::from_translation(position) * mat4_from_quat_lenient(rotation);
        if bind_pose.determinant().abs() < f32::EPSILON {
            return None;
        }
        Some(Self {
            index,
            name: name.into(),
            position,
            angle: rotation,
            bind_pose,
            inverse_bind_pose: bind_pose.inverse(),
            parent: None,
            children: Vec::new(),
        })
    }
}

/// `Mat4::from_quat` expects a normalised quaternion. The C# formula produces
/// identity for the all-zero quaternion its own tests use, so this reproduces
/// that instead of relying on glam's normalisation assumption.
fn mat4_from_quat_lenient(q: Quat) -> Mat4 {
    let (x, y, z, w) = (q.x, q.y, q.z, q.w);
    let (xx, yy, zz) = (x * x, y * y, z * z);
    let (xy, xz, yz) = (x * y, x * z, y * z);
    let (wx, wy, wz) = (w * x, w * y, w * z);
    // Column-major for glam; this is the transpose of the C#'s row-major build.
    Mat4::from_cols_array(&[
        1.0 - 2.0 * (yy + zz), 2.0 * (xy + wz), 2.0 * (xz - wy), 0.0,
        2.0 * (xy - wz), 1.0 - 2.0 * (xx + zz), 2.0 * (yz + wx), 0.0,
        2.0 * (xz + wy), 2.0 * (yz - wx), 1.0 - 2.0 * (xx + yy), 0.0,
        0.0, 0.0, 0.0, 1.0,
    ])
}

/// C# `interface ISkeleton`.
pub trait Skeleton {
    fn bones(&self) -> &[Bone];
    /// C# `Roots` — indices of bones with no parent.
    fn roots(&self) -> Vec<usize> {
        self.bones()
            .iter()
            .enumerate()
            .filter(|(_, b)| b.parent.is_none())
            .map(|(i, _)| i)
            .collect()
    }
}

/// C# `struct FrameBone`.
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct FrameBone {
    pub position: Vec3,
    pub angle: Quat,
    pub scale: f32,
}

impl Default for FrameBone {
    fn default() -> Self {
        Self { position: Vec3::ZERO, angle: Quat::IDENTITY, scale: 1.0 }
    }
}

/// C# `class Frame`.
#[derive(Debug, Clone, PartialEq, Default)]
pub struct Frame {
    pub bones: Vec<FrameBone>,
}

impl Frame {
    /// C# `Frame(ISkeleton skeleton)`.
    pub fn new<S: Skeleton + ?Sized>(skeleton: &S) -> Self {
        let mut f = Self { bones: vec![FrameBone::default(); skeleton.bones().len()] };
        f.clear(skeleton);
        f
    }

    /// C# `SetAttribute(int, ChannelAttribute, Vector3)`.
    ///
    /// Returns whether the attribute accepted this type. The C# silently
    /// ignores a mismatch in release builds (bug 4), and indexes `Bones[bone]`
    /// unchecked.
    pub fn set_position(&mut self, bone: usize, attribute: ChannelAttribute, data: Vec3) -> bool {
        if attribute != ChannelAttribute::Position {
            return false;
        }
        match self.bones.get_mut(bone) {
            Some(b) => {
                b.position = data;
                true
            }
            None => false,
        }
    }

    /// C# `SetAttribute(int, ChannelAttribute, Quaternion)`.
    pub fn set_angle(&mut self, bone: usize, attribute: ChannelAttribute, data: Quat) -> bool {
        if attribute != ChannelAttribute::Angle {
            return false;
        }
        match self.bones.get_mut(bone) {
            Some(b) => {
                b.angle = data;
                true
            }
            None => false,
        }
    }

    /// C# `SetAttribute(int, ChannelAttribute, float)`.
    pub fn set_scale(&mut self, bone: usize, attribute: ChannelAttribute, data: f32) -> bool {
        if attribute != ChannelAttribute::Scale {
            return false;
        }
        match self.bones.get_mut(bone) {
            Some(b) => {
                b.scale = data;
                true
            }
            None => false,
        }
    }

    /// C# `Clear(ISkeleton skeleton)` — reset to the bind pose, scale 1.
    ///
    /// The C# indexes `skeleton.Bones[i]` for `i` over `Bones.Length`, so a
    /// skeleton that shrank since construction throws. Bounded here.
    pub fn clear<S: Skeleton + ?Sized>(&mut self, skeleton: &S) {
        let src = skeleton.bones();
        for (i, b) in self.bones.iter_mut().enumerate() {
            if let Some(s) = src.get(i) {
                b.position = s.position;
                b.angle = s.angle;
                b.scale = 1.0;
            }
        }
    }
}

/// C# `interface IAnimation`.
pub trait Animation {
    fn name(&self) -> &str;
    fn fps(&self) -> f32;
    fn frame_count(&self) -> usize;
    fn decode_frame(&self, frame_index: usize, out_frame: &mut Frame);
}

/// C# `GetFrame`'s `object index` — either an explicit frame or a time.
#[derive(Debug, Clone, Copy, PartialEq)]
pub enum FrameIndex {
    /// C# `case int frameIndex`.
    Exact(usize),
    /// C# `case float time` — interpolated.
    Time(f32),
}

/// C# `class FrameCache` — two decoded frames plus an interpolation slot.
#[derive(Debug, Clone)]
pub struct FrameCache {
    previous: (Option<usize>, Frame),
    next: (Option<usize>, Frame),
    interpolated: Frame,
}

impl FrameCache {
    /// C# `FrameCache(ISkeleton skeleton)`.
    ///
    /// The C#'s `FrameFactory` static hook is dropped: it was a public mutable
    /// static whose only purpose was substituting a `Frame` subclass, and
    /// nothing in the tree sets it.
    pub fn new<S: Skeleton + ?Sized>(skeleton: &S) -> Self {
        Self {
            previous: (None, Frame::new(skeleton)),
            next: (None, Frame::new(skeleton)),
            interpolated: Frame::new(skeleton),
        }
    }

    /// C# `Clear()`.
    pub fn clear<S: Skeleton + ?Sized>(&mut self, skeleton: &S) {
        self.previous.0 = None;
        self.previous.1.clear(skeleton);
        self.next.0 = None;
        self.next.1.clear(skeleton);
    }

    /// C# `GetFrame(IAnimation anim, object index)`.
    ///
    /// `None` when the animation has no frames — the C# throws
    /// `DivideByZeroException` there (bug 3).
    pub fn get_frame<A: Animation + ?Sized>(
        &mut self,
        anim: &A,
        index: FrameIndex,
    ) -> Option<&Frame> {
        let count = anim.frame_count();
        if count == 0 {
            return None;
        }
        match index {
            FrameIndex::Exact(i) => Some(self.decode_exact(anim, i % count)),
            FrameIndex::Time(time) => {
                // Negative time would give a negative index in the C#; clamp.
                let scaled = (time * anim.fps()).max(0.0);
                let frame_index = (scaled as usize) % count;
                let t = (scaled - scaled.floor()).clamp(0.0, 1.0);
                let a = self.decode_exact(anim, frame_index).clone();
                let b = self.decode_exact(anim, (frame_index + 1) % count).clone();
                for i in 0..self.interpolated.bones.len().min(a.bones.len()) {
                    let (x, y) = (a.bones[i], b.bones[i]);
                    let out = &mut self.interpolated.bones[i];
                    out.position = x.position.lerp(y.position, t);
                    out.angle = slerp_lenient(x.angle, y.angle, t);
                    out.scale = x.scale + (y.scale - x.scale) * t;
                }
                Some(&self.interpolated)
            }
        }
    }

    /// C#'s two-slot LRU: whichever slot is further from the requested index
    /// gets reused.
    fn decode_exact<A: Animation + ?Sized>(&mut self, anim: &A, frame_index: usize) -> &Frame {
        if self.previous.0 == Some(frame_index) {
            return &self.previous.1;
        }
        if self.next.0 == Some(frame_index) {
            return &self.next.1;
        }
        let forward = self.previous.0.map(|p| frame_index > p).unwrap_or(true);
        if forward {
            std::mem::swap(&mut self.previous, &mut self.next);
            self.next.0 = Some(frame_index);
            anim.decode_frame(frame_index, &mut self.next.1);
            &self.next.1
        } else {
            std::mem::swap(&mut self.next, &mut self.previous);
            self.previous.0 = Some(frame_index);
            anim.decode_frame(frame_index, &mut self.previous.1);
            &self.previous.1
        }
    }
}

/// `Quat::slerp` requires normalised inputs; the C# tests use `Quaternion.Zero`,
/// so fall back to the endpoints when either is degenerate.
fn slerp_lenient(a: Quat, b: Quat, t: f32) -> Quat {
    if a.length_squared() < f32::EPSILON || b.length_squared() < f32::EPSILON {
        return if t < 0.5 { a } else { b };
    }
    a.normalize().slerp(b.normalize(), t)
}

/// C# `class AnimationController`.
pub struct AnimationController<A: Animation> {
    pub frame_cache: FrameCache,
    pub active_animation: Option<A>,
    pub is_paused: bool,
    time: f32,
    should_update: bool,
}

impl<A: Animation> AnimationController<A> {
    /// C# `AnimationController(ISkeleton skeleton)`.
    pub fn new<S: Skeleton + ?Sized>(skeleton: &S) -> Self {
        Self {
            frame_cache: FrameCache::new(skeleton),
            active_animation: None,
            is_paused: false,
            time: 0.0,
            should_update: false,
        }
    }

    /// C# `Frame` getter.
    pub fn frame(&self) -> usize {
        match &self.active_animation {
            Some(a) if a.frame_count() != 0 => {
                ((self.time * a.fps()).round().max(0.0) as usize) % a.frame_count()
            }
            _ => 0,
        }
    }

    /// C# `Frame` setter — returns whether it took effect.
    ///
    /// The C# silently does nothing when there is no active animation, which is
    /// what makes `PauseLastFrame` half-work (bug 6).
    pub fn set_frame(&mut self, value: usize) -> bool {
        match &self.active_animation {
            Some(a) => {
                self.time = if a.fps() != 0.0 { value as f32 / a.fps() } else { 0.0 };
                self.should_update = true;
                true
            }
            None => false,
        }
    }

    /// C# `Update(float timeStep)`.
    pub fn update(&mut self, time_step: f32) -> bool {
        if self.active_animation.is_none() {
            return false;
        }
        if self.is_paused {
            let res = self.should_update;
            self.should_update = false;
            return res;
        }
        self.time += time_step;
        self.should_update = false;
        true
    }

    /// C# `SetAnimation(IAnimation)`.
    pub fn set_animation<S: Skeleton + ?Sized>(&mut self, animation: A, skeleton: &S) {
        self.frame_cache.clear(skeleton);
        self.active_animation = Some(animation);
        self.time = 0.0;
    }

    /// C# `PauseLastFrame()`.
    ///
    /// Returns whether it succeeded. The C# sets `IsPaused` regardless and then
    /// leaves the frame at 0 when there is no animation (bug 6).
    pub fn pause_last_frame(&mut self) -> bool {
        let Some(last) = self
            .active_animation
            .as_ref()
            .map(|a| a.frame_count().saturating_sub(1))
        else {
            return false;
        };
        self.is_paused = true;
        self.set_frame(last)
    }

    /// C# `GetAnimationMatrices(ISkeleton)`.
    ///
    /// `None` with no active animation — the C# throws (bug 5).
    pub fn current_frame(&mut self) -> Option<&Frame> {
        let anim = self.active_animation.take()?;
        let index = if self.is_paused {
            FrameIndex::Exact(self.frame())
        } else {
            FrameIndex::Time(self.time)
        };
        // `get_frame` needs &mut self.frame_cache while `anim` is borrowed, so
        // the animation is moved out and put back.
        let got = self.frame_cache.get_frame(&anim, index).is_some();
        self.active_animation = Some(anim);
        if got {
            Some(&self.frame_cache.interpolated)
        } else {
            None
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::egin_render::Camera;

    struct TestSkeleton(Vec<Bone>);
    impl Skeleton for TestSkeleton {
        fn bones(&self) -> &[Bone] {
            &self.0
        }
    }

    fn skel() -> TestSkeleton {
        // Mirrors the C# TestSkeleton: one bone at (1,1,1) with Quaternion.Zero.
        TestSkeleton(vec![
            Bone::new(0, "Bone", Vec3::ONE, Quat::from_xyzw(0.0, 0.0, 0.0, 0.0)).unwrap()
        ])
    }

    struct TestAnim;
    impl Animation for TestAnim {
        fn name(&self) -> &str {
            "Animation"
        }
        fn fps(&self) -> f32 {
            15.0
        }
        fn frame_count(&self) -> usize {
            1
        }
        fn decode_frame(&self, _i: usize, _out: &mut Frame) {}
    }

    struct EmptyAnim;
    impl Animation for EmptyAnim {
        fn name(&self) -> &str {
            "Empty"
        }
        fn fps(&self) -> f32 {
            15.0
        }
        fn frame_count(&self) -> usize {
            0
        }
        fn decode_frame(&self, _i: usize, _out: &mut Frame) {}
    }

    // The expected values below are the C# test suite's own assertions from
    // `OpenStack.GfxTests/Egin/Gfx_Animate.cs`.

    #[test]
    fn bone_init_matches_the_c_sharp() {
        let b = Bone::new(1, "Name", Vec3::ONE, Quat::from_xyzw(0.0, 0.0, 0.0, 0.0)).unwrap();
        assert_eq!(b.index, 1);
        assert_eq!(b.name, "Name");
        assert_eq!(b.position, Vec3::ONE);
        assert_eq!(b.angle.x, 0.0);
    }

    #[test]
    fn bind_pose_matches_the_c_sharp_matrix_string() {
        // C# asserts BindPose == identity with M41..M43 = 1,1,1 (row-vector
        // translation), and InverseBindPose the same with -1,-1,-1.
        let b = Bone::new(1, "Name", Vec3::ONE, Quat::from_xyzw(0.0, 0.0, 0.0, 0.0)).unwrap();
        for (r, c, exp) in [
            (0, 0, 1.0f32), (0, 1, 0.0), (0, 2, 0.0), (0, 3, 0.0),
            (1, 1, 1.0), (2, 2, 1.0),
            (3, 0, 1.0), (3, 1, 1.0), (3, 2, 1.0), (3, 3, 1.0),
        ] {
            let got = Camera::cs_element(b.bind_pose, r, c);
            assert!((got - exp).abs() < 1e-6, "BindPose M{}{}: {got} != {exp}", r + 1, c + 1);
        }
        for (r, c, exp) in [(3, 0, -1.0f32), (3, 1, -1.0), (3, 2, -1.0), (3, 3, 1.0)] {
            let got = Camera::cs_element(b.inverse_bind_pose, r, c);
            assert!((got - exp).abs() < 1e-6, "Inverse M{}{}: {got} != {exp}", r + 1, c + 1);
        }
    }

    #[test]
    fn bind_pose_and_its_inverse_compose_to_identity() {
        let b = Bone::new(0, "B", Vec3::new(3.0, -2.0, 5.0), Quat::from_rotation_z(0.7)).unwrap();
        let p = b.bind_pose * b.inverse_bind_pose;
        for (i, (g, e)) in p.to_cols_array().iter().zip(Mat4::IDENTITY.to_cols_array()).enumerate() {
            assert!((g - e).abs() < 1e-5, "element {i}: {g} != {e}");
        }
    }

    #[test]
    fn frame_init_matches_the_c_sharp() {
        let s = skel();
        let f = Frame::new(&s);
        assert_eq!(f.bones.len(), 1);
        assert_eq!(f.bones[0].position.x, 1.0);
        assert_eq!(f.bones[0].angle.x, 0.0);
        assert_eq!(f.bones[0].scale, 1.0);
    }

    #[test]
    fn set_attribute_matches_the_c_sharp() {
        let s = skel();
        let mut f = Frame::new(&s);
        assert!(f.set_position(0, ChannelAttribute::Position, Vec3::ONE));
        assert!(f.set_angle(0, ChannelAttribute::Angle, Quat::from_xyzw(0.0, 0.0, 0.0, 0.0)));
        assert!(f.set_scale(0, ChannelAttribute::Scale, 0.0));
        assert_eq!(f.bones[0].position.x, 1.0);
        assert_eq!(f.bones[0].angle.x, 0.0);
        assert_eq!(f.bones[0].scale, 0.0);
    }

    #[test]
    fn clear_restores_the_bind_pose() {
        let s = skel();
        let mut f = Frame::new(&s);
        f.set_scale(0, ChannelAttribute::Scale, 0.0);
        f.clear(&s);
        assert_eq!(f.bones[0].position.x, 1.0);
        assert_eq!(f.bones[0].scale, 1.0);
    }

    #[test]
    fn mismatched_attribute_is_reported_not_silently_dropped() {
        // The C# only logs this under #if DEBUG; in release it vanishes.
        let s = skel();
        let mut f = Frame::new(&s);
        assert!(!f.set_scale(0, ChannelAttribute::Position, 5.0));
        assert_eq!(f.bones[0].scale, 1.0, "must not have been written");
    }

    #[test]
    fn out_of_range_bone_is_rejected() {
        let s = skel();
        let mut f = Frame::new(&s);
        assert!(!f.set_position(99, ChannelAttribute::Position, Vec3::ONE));
    }

    #[test]
    fn parent_child_links_cannot_be_duplicated() {
        // The C# guard checks `Children.Contains(parent)`, so two SetParent
        // calls with the same parent append `this` twice. Modelling the
        // hierarchy by index makes the duplicate unrepresentable.
        let mut bones = vec![
            Bone::new(0, "root", Vec3::ZERO, Quat::IDENTITY).unwrap(),
            Bone::new(1, "child", Vec3::ONE, Quat::IDENTITY).unwrap(),
        ];
        bones[1].parent = Some(0);
        if !bones[0].children.contains(&1) {
            bones[0].children.push(1);
        }
        if !bones[0].children.contains(&1) {
            bones[0].children.push(1);
        }
        assert_eq!(bones[0].children, vec![1], "C# would hold [1, 1] here");
    }

    #[test]
    fn roots_are_bones_without_a_parent() {
        let s = skel();
        assert_eq!(s.roots(), vec![0]);
    }

    #[test]
    fn zero_frame_animation_returns_none_instead_of_dividing_by_zero() {
        let s = skel();
        let mut c = FrameCache::new(&s);
        assert!(c.get_frame(&EmptyAnim, FrameIndex::Time(1.0)).is_none());
        assert!(c.get_frame(&EmptyAnim, FrameIndex::Exact(0)).is_none());
    }

    #[test]
    fn negative_time_does_not_produce_a_negative_frame_index() {
        let s = skel();
        let mut c = FrameCache::new(&s);
        assert!(c.get_frame(&TestAnim, FrameIndex::Time(-5.0)).is_some());
    }

    #[test]
    fn controller_reports_no_frame_before_an_animation_is_set() {
        // The C# GetAnimationMatrices throws here.
        let s = skel();
        let mut c: AnimationController<TestAnim> = AnimationController::new(&s);
        assert!(c.current_frame().is_none());
        assert!(!c.update(0.1));
        assert_eq!(c.frame(), 0);
        assert!(!c.pause_last_frame(), "cannot pause with nothing playing");
        assert!(!c.is_paused, "and must not claim to be paused");
    }

    #[test]
    fn controller_advances_and_wraps() {
        let s = skel();
        let mut c = AnimationController::new(&s);
        c.set_animation(TestAnim, &s);
        assert!(c.update(1.0));
        assert_eq!(c.frame(), 0, "one-frame animation always wraps to 0");
        assert!(c.current_frame().is_some());
    }

    #[test]
    fn pausing_sets_the_last_frame() {
        let s = skel();
        let mut c = AnimationController::new(&s);
        c.set_animation(TestAnim, &s);
        assert!(c.pause_last_frame());
        assert!(c.is_paused);
        assert_eq!(c.frame(), 0, "frame_count 1 -> last index 0");
    }
}
