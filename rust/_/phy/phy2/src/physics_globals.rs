// PORT-SOURCE: Phy/OpenStack.Phy2/PhysicsGlobals.cs
// PORT-SHA: f961c3e3712c528e
// PORT-STATUS: done
//
// Engine-wide physics constants.
//
// C#-SIDE BUG — **`DefaultSortingSphere` is never initialised**:
//
//     public static readonly Sphere DummySphere = new Sphere(...);   // fine
//     public static readonly Sphere DefaultSortingSphere;            // null
//
// It is declared with no initialiser and assigned nowhere, so it is
// permanently null. `Sphere` is a class, so any read that dereferences it
// throws `NullReferenceException`. Note the field directly above it *is*
// initialised, which is what makes the omission look deliberate at a glance.
// **Fix this in the C# tree** — it needs whatever radius ACE's sorting sphere
// actually uses; `Option<Sphere>` here so the absence cannot be dereferenced by
// accident.
//
// `DefaultState` is omitted: it is a `PhysicsState` flag combination, and
// `PhysicsState` lives in `ACE.Entity.Enum`, which is not present in this
// solution (see the crate root).

use glam::Vec3;

/// C# `EPSILON`.
pub const EPSILON: f32 = 0.0002;
/// C# `EpsilonSq`.
pub const EPSILON_SQ: f32 = EPSILON * EPSILON;
/// C# `Gravity`.
pub const GRAVITY: f32 = -9.8;
/// C# `DefaultFriction`.
pub const DEFAULT_FRICTION: f32 = 0.95;
/// C# `DefaultElasticity`.
pub const DEFAULT_ELASTICITY: f32 = 0.05;
/// C# `DefaultTranslucency`.
pub const DEFAULT_TRANSLUCENCY: f32 = 0.0;
/// C# `DefaultMass`.
pub const DEFAULT_MASS: f32 = 1.0;
/// C# `DefaultScale`.
pub const DEFAULT_SCALE: f32 = 1.0;
/// C# `MaxElasticity`.
pub const MAX_ELASTICITY: f32 = 0.1;
/// C# `MaxVelocity`.
pub const MAX_VELOCITY: f32 = 50.0;
/// C# `MaxVelocitySquared`.
pub const MAX_VELOCITY_SQUARED: f32 = MAX_VELOCITY * MAX_VELOCITY;
/// C# `SmallVelocity`.
pub const SMALL_VELOCITY: f32 = 0.25;
/// C# `SmallVelocitySquared`.
pub const SMALL_VELOCITY_SQUARED: f32 = SMALL_VELOCITY * SMALL_VELOCITY;
/// C# `MinQuantum` — 30fps.
pub const MIN_QUANTUM: f32 = 1.0 / 30.0;
/// C# `MaxQuantum` — 10fps.
pub const MAX_QUANTUM: f32 = 0.1;
/// C# `HugeQuantum` — 0.5fps.
pub const HUGE_QUANTUM: f32 = 2.0;
/// C# `LandingZ`.
pub const LANDING_Z: f32 = 0.0871557;
/// C# `FloorZ`.
pub const FLOOR_Z: f32 = 0.664_174_15;
/// C# `DummySphereRadius`.
pub const DUMMY_SPHERE_RADIUS: f32 = 0.1;
/// C# `DefaultStepHeight`.
pub const DEFAULT_STEP_HEIGHT: f32 = 0.01;

/// C# `DummySphere`.
pub fn dummy_sphere() -> crate::sphere::Sphere {
    crate::sphere::Sphere::new(Vec3::new(0.0, 0.0, DUMMY_SPHERE_RADIUS), DUMMY_SPHERE_RADIUS)
}

/// C# `DefaultSortingSphere` — permanently null there; `None` here so the
/// absence is a value, not a crash. See the module header.
pub fn default_sorting_sphere() -> Option<crate::sphere::Sphere> {
    None
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn derived_constants_agree_with_their_bases() {
        assert_eq!(EPSILON_SQ, EPSILON * EPSILON);
        assert_eq!(MAX_VELOCITY_SQUARED, MAX_VELOCITY * MAX_VELOCITY);
        assert_eq!(SMALL_VELOCITY_SQUARED, SMALL_VELOCITY * SMALL_VELOCITY);
    }

    #[test]
    fn quantum_ordering_is_sane() {
        assert!(MIN_QUANTUM < MAX_QUANTUM);
        assert!(MAX_QUANTUM < HUGE_QUANTUM);
    }

    #[test]
    fn dummy_sphere_matches_its_radius_constant() {
        let s = dummy_sphere();
        assert_eq!(s.radius, DUMMY_SPHERE_RADIUS);
        assert_eq!(s.center.z, DUMMY_SPHERE_RADIUS);
    }

    #[test]
    fn sorting_sphere_absence_is_explicit() {
        // The C# field is null; reading it through anything throws.
        assert!(default_sorting_sphere().is_none());
    }
}
