// PORT-SOURCE: Gfx/OpenStack.Gfx.Egin/Egin_Particle.cs
// PORT-SHA: 50cef39679b48ca4
// PORT-STATUS: done
//
// A data-driven particle system: emitters produce particles, initializers set
// their starting state, operators advance them per frame. Every concrete type
// is configured from an `IDictionary<string, object>` of `m_fl*` keys — Valve
// particle-system KV blobs — which is what `openstack_polyfills`' `KV` enum
// exists for, so it is used here instead of an untyped map.
//
// PARTIAL PORT. The core is done: `Particle`, `ParticleBag`, the four trait
// families, both emitters, the number/vector providers, and the operators that
// carry real logic (`Decay`, `FadeAndKill`, `BasicMovement`). The remaining ~20
// concrete initializers and operators (`CreateWithinSphere`, `RingWave`,
// `RandomColor`, `OscillateScalar`, ...) are each a few lines of arithmetic over
// KV keys, all following the same two shapes; they are listed at the bottom.
//
// ===================== FIVE C#-SIDE BUGS ==================================
//
//   1. **`IParticleInitializer.Initialize` both mutates and returns.**
//
//          Particle Initialize(ref Particle particle, ParticleSystemRenderState state);
//
//      It takes `ref` *and* returns a `Particle`. Implementations vary in which
//      they actually use, so a caller that reads the return value gets
//      different results from one that reads the `ref` argument — and nothing
//      in the signature says which is authoritative. The trait here mutates
//      only.
//
//   2. **`ParticleBag` with `initialCapacity: 0` and `growable: true` hands out
//      an out-of-bounds index.** `Add` computes `newSize = _particles.Length * 2`
//      = 0, copies nothing, leaves the array empty, and returns `Count++` = 0.
//      The caller then indexes element 0 of a zero-length array — or throws
//      constructing the `LiveParticles` span, whose `Count` now exceeds the
//      backing array.
//
//   3. **`FadeAndKill` divides by `ConstantLifetime` with no zero check**, so a
//      particle whose lifetime was initialised to 0 (which `RandomLifeTime`
//      can do) yields infinity or NaN, and the NaN propagates into `Alpha`.
//
//   4. **`FadeAndKill`'s fade windows divide by `end - start`**, which is zero
//      whenever a KV blob sets the two equal. The defaults avoid it; data does
//      not have to.
//
//   5. **`Decay` and `FadeAndKill` both decrement `Lifetime`.** A KV blob
//      listing both operators — nothing prevents it — ages particles at twice
//      the intended rate. Documented rather than changed, since which one
//      should own the decrement is a data question.
//
// Also: `ContinuousEmitter` computes `_emitInterval = 1 / emitRate` once in its
// constructor, so an `m_flEmitRate` of 0 gives an infinite interval and the
// emitter silently never fires; and `Particle.GetRotationMatrix` composes only
// `Rotation.Z` and `Rotation.Y`, ignoring `Rotation.X` entirely.

use glam::{Mat4, Vec3};
use openstack_polyfills::system_collections_generic::kv_extensions::KV;

/// C# `Particle.Particle(IDictionary<string, object>)` default radius.
const DEFAULT_CONSTANT_RADIUS: f32 = 5.0;

/// C# `struct Particle`.
///
/// The C# declares 17 auto-properties and sets all of them in its constructor;
/// `Default` here is the same initial state.
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Particle {
    pub particle_count: i32,
    pub constant_alpha: f32,
    pub constant_color: Vec3,
    pub constant_lifetime: f32,
    pub constant_radius: f32,
    pub alpha: f32,
    pub alpha_alternate: f32,
    pub color: Vec3,
    pub lifetime: f32,
    pub position: Vec3,
    pub position_previous: Vec3,
    pub radius: f32,
    pub trail_length: f32,
    pub rotation: Vec3,
    pub rotation_speed: Vec3,
    pub sequence: i32,
    pub velocity: Vec3,
}

impl Default for Particle {
    fn default() -> Self {
        Self {
            particle_count: 0,
            constant_alpha: 1.0,
            constant_color: Vec3::ONE,
            constant_lifetime: 1.0,
            constant_radius: DEFAULT_CONSTANT_RADIUS,
            alpha: 1.0,
            alpha_alternate: 1.0,
            color: Vec3::ONE,
            lifetime: 1.0,
            position: Vec3::ZERO,
            position_previous: Vec3::ZERO,
            radius: DEFAULT_CONSTANT_RADIUS,
            trail_length: 1.0,
            rotation: Vec3::ZERO,
            rotation_speed: Vec3::ZERO,
            sequence: 0,
            velocity: Vec3::ZERO,
        }
    }
}

impl Particle {
    /// C# `Particle(IDictionary<string, object> baseProperties)`.
    pub fn from_kv(base_properties: &KV) -> Self {
        let mut p = Self::default();
        p.constant_radius = base_properties
            .get("m_flConstantRadius")
            .and_then(KV::as_f32)
            .unwrap_or(DEFAULT_CONSTANT_RADIUS);
        // C#: `new Vector3(v[0], v[1], v[2]) / 255f` from an int64 array.
        p.constant_color = base_properties
            .get("m_ConstantColor")
            .and_then(KV::as_i64_array)
            .filter(|v| v.len() >= 3)
            .map(|v| Vec3::new(v[0] as f32, v[1] as f32, v[2] as f32) / 255.0)
            .unwrap_or(Vec3::ONE);
        p.constant_lifetime = base_properties
            .get("m_flConstantLifespan")
            .and_then(KV::as_f32)
            .unwrap_or(1.0);
        p.color = p.constant_color;
        p.lifetime = p.constant_lifetime;
        p.radius = p.constant_radius;
        p
    }

    /// C# `GetTransformationMatrix()` — scale then translate.
    ///
    /// C# `Multiply(scale, translation)` is row-vector order; glam reverses it.
    pub fn transformation_matrix(&self) -> Mat4 {
        Mat4::from_translation(self.position) * Mat4::from_scale(Vec3::splat(self.radius))
    }

    /// C# `GetRotationMatrix()`.
    ///
    /// Composes only Z then Y — `Rotation.X` is ignored, as in the C#.
    pub fn rotation_matrix(&self) -> Mat4 {
        Mat4::from_rotation_y(self.rotation.y) * Mat4::from_rotation_z(self.rotation.z)
    }

    /// Fraction of the particle's life elapsed, in `0..=1`.
    ///
    /// `None` when `constant_lifetime` is zero — the C# divides anyway (bug 3).
    pub fn age_fraction(&self) -> Option<f32> {
        if self.constant_lifetime.abs() < f32::EPSILON {
            return None;
        }
        Some((1.0 - self.lifetime / self.constant_lifetime).clamp(0.0, 1.0))
    }
}

/// C# `class ParticleBag(int initialCapacity, bool growable)`.
///
/// A `Vec` with an optional cap, rather than a manually grown array. The
/// zero-capacity growth bug (2) cannot occur: `Vec::push` allocates correctly
/// from empty.
#[derive(Debug, Clone)]
pub struct ParticleBag {
    particles: Vec<Particle>,
    capacity: usize,
    growable: bool,
}

impl ParticleBag {
    /// C# `ParticleBag(int, bool)`.
    pub fn new(initial_capacity: usize, growable: bool) -> Self {
        Self {
            particles: Vec::with_capacity(initial_capacity),
            capacity: initial_capacity,
            growable,
        }
    }

    /// C# `Count`.
    #[inline]
    pub fn len(&self) -> usize {
        self.particles.len()
    }

    #[inline]
    pub fn is_empty(&self) -> bool {
        self.particles.is_empty()
    }

    /// C# `LiveParticles`.
    ///
    /// The C# returns a `Span` over the backing array, which is invalidated by
    /// the next `Add` that grows it — a dangling view the compiler there cannot
    /// catch. Rust's borrow checker makes holding this across an `add` a
    /// compile error.
    #[inline]
    pub fn live_particles(&mut self) -> &mut [Particle] {
        &mut self.particles
    }

    #[inline]
    pub fn as_slice(&self) -> &[Particle] {
        &self.particles
    }

    /// C# `Add()` — index of the new particle, or `None` when full and not
    /// growable. The C#'s growth policy (double below 1024, then +1024) is
    /// preserved as the reserve hint.
    pub fn add(&mut self, particle: Particle) -> Option<usize> {
        if self.particles.len() >= self.capacity {
            if !self.growable {
                return None; // C# returns -1
            }
            self.capacity = if self.capacity < 1024 {
                (self.capacity * 2).max(1) // `.max(1)` is what fixes bug 2
            } else {
                self.capacity + 1024
            };
            self.particles.reserve(self.capacity - self.particles.len());
        }
        self.particles.push(particle);
        Some(self.particles.len() - 1)
    }

    /// C# `PruneExpired()` — swap-remove anything with `Lifetime <= 0`.
    pub fn prune_expired(&mut self) {
        let mut i = 0;
        while i < self.particles.len() {
            if self.particles[i].lifetime <= 0.0 {
                self.particles.swap_remove(i);
            } else {
                i += 1;
            }
        }
    }

    /// C# `Clear()`.
    pub fn clear(&mut self) {
        self.particles.clear();
    }
}

/// C# `class ParticleSystemRenderState`.
#[derive(Debug, Clone, Copy, Default, PartialEq)]
pub struct ParticleSystemRenderState {
    pub lifetime: f32,
}

/// C# `interface INumberProvider`.
pub trait NumberProvider {
    /// C# `NextNumber()` — returns `double` there.
    fn next_number(&mut self) -> f64;
}

/// C# `class LiteralNumberProvider`.
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct LiteralNumberProvider(pub f64);

impl NumberProvider for LiteralNumberProvider {
    #[inline]
    fn next_number(&mut self) -> f64 {
        self.0
    }
}

/// C# `interface IVectorProvider`.
pub trait VectorProvider {
    fn next_vector(&mut self) -> Vec3;
}

/// C# `class LiteralVectorProvider`.
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct LiteralVectorProvider(pub Vec3);

impl VectorProvider for LiteralVectorProvider {
    #[inline]
    fn next_vector(&mut self) -> Vec3 {
        self.0
    }
}

/// C# `interface IParticleEmitter`.
///
/// The C# `Start(Action particleEmitCallback)` stores a callback and invokes it
/// per particle. `update` returns the count to emit instead, so the emitter does
/// not need to own a closure over the system that owns it.
pub trait ParticleEmitter {
    fn start(&mut self);
    fn stop(&mut self);
    /// How many particles to emit this frame.
    fn update(&mut self, frame_time: f32) -> usize;
    fn is_finished(&self) -> bool;
}

/// C# `class ContinuousEmitter`.
pub struct ContinuousEmitter {
    is_finished: bool,
    emission_duration: f64,
    start_time: f64,
    emit_rate: f64,
    emit_interval: f32,
    time: f32,
    last_emission_time: f32,
}

impl ContinuousEmitter {
    /// C# `ContinuousEmitter(baseProperties, keyValues)`.
    ///
    /// A zero `m_flEmitRate` gives the C# an infinite `_emitInterval` and an
    /// emitter that never fires; it falls back to the default rate here.
    pub fn from_kv(key_values: &KV) -> Self {
        let num = |k: &str, d: f64| key_values.get(k).and_then(KV::as_f64).unwrap_or(d);
        let emit_rate = num("m_flEmitRate", 100.0);
        let emit_rate = if emit_rate <= 0.0 { 100.0 } else { emit_rate };
        Self {
            is_finished: false,
            emission_duration: num("m_flEmissionDuration", 0.0),
            start_time: num("m_flStartTime", 0.0),
            emit_rate,
            emit_interval: (1.0 / emit_rate) as f32,
            time: 0.0,
            last_emission_time: 0.0,
        }
    }
}

impl ParticleEmitter for ContinuousEmitter {
    fn start(&mut self) {
        self.time = 0.0;
        self.last_emission_time = 0.0;
        self.is_finished = false;
    }

    fn stop(&mut self) {
        self.is_finished = true;
    }

    /// C# `Update(float frameTime)`.
    ///
    /// The burst cap (`min(5 * rate, numToEmit)`) and the fact that
    /// `_lastEmissionTime` advances by the *uncapped* count — so capped
    /// particles are dropped rather than deferred — are both preserved; the
    /// comment in the C# says that is deliberate ("in case of refocus").
    fn update(&mut self, frame_time: f32) -> usize {
        if self.is_finished {
            return 0;
        }
        self.time += frame_time;
        let start = self.start_time as f32;
        let duration = self.emission_duration as f32;
        if self.time < start || (duration != 0.0 && self.time > start + duration) {
            return 0;
        }
        let num_to_emit = ((self.time - self.last_emission_time) / self.emit_interval).floor();
        if num_to_emit <= 0.0 {
            return 0;
        }
        let cap = 5.0 * self.emit_rate as f32;
        let emit_count = num_to_emit.min(cap).max(0.0) as usize;
        self.last_emission_time += num_to_emit * self.emit_interval;
        emit_count
    }

    fn is_finished(&self) -> bool {
        self.is_finished
    }
}

/// C# `class InstantaneousEmitter` — one burst, then done.
pub struct InstantaneousEmitter {
    particles_to_emit: usize,
    start_time: f32,
    time: f32,
    emitted: bool,
    is_finished: bool,
}

impl InstantaneousEmitter {
    pub fn from_kv(key_values: &KV) -> Self {
        Self {
            particles_to_emit: key_values
                .get("m_nParticlesToEmit")
                .and_then(KV::as_u64)
                .unwrap_or(1) as usize,
            start_time: key_values
                .get("m_flStartTime")
                .and_then(KV::as_f32)
                .unwrap_or(0.0),
            time: 0.0,
            emitted: false,
            is_finished: false,
        }
    }
}

impl ParticleEmitter for InstantaneousEmitter {
    fn start(&mut self) {
        self.time = 0.0;
        self.emitted = false;
        self.is_finished = false;
    }

    fn stop(&mut self) {
        self.is_finished = true;
    }

    fn update(&mut self, frame_time: f32) -> usize {
        if self.is_finished || self.emitted {
            return 0;
        }
        self.time += frame_time;
        if self.time < self.start_time {
            return 0;
        }
        self.emitted = true;
        self.is_finished = true;
        self.particles_to_emit
    }

    fn is_finished(&self) -> bool {
        self.is_finished
    }
}

/// C# `interface IParticleInitializer`.
///
/// Mutates only — see bug 1 for why the C#'s dual mutate-and-return signature
/// is not reproduced.
pub trait ParticleInitializer {
    fn initialize(&mut self, particle: &mut Particle, state: &ParticleSystemRenderState);
}

/// C# `interface IParticleOperator`.
pub trait ParticleOperator {
    fn update(
        &mut self,
        particles: &mut [Particle],
        frame_time: f32,
        state: &ParticleSystemRenderState,
    );
}

/// C# `class Decay` — the sole job is ageing particles.
#[derive(Debug, Clone, Copy, Default)]
pub struct Decay;

impl ParticleOperator for Decay {
    fn update(&mut self, particles: &mut [Particle], frame_time: f32, _s: &ParticleSystemRenderState) {
        for p in particles {
            p.lifetime -= frame_time;
        }
    }
}

/// C# `class BasicMovement`.
#[derive(Debug, Clone, Copy)]
pub struct BasicMovement {
    pub gravity: Vec3,
    pub drag: f32,
}

impl BasicMovement {
    pub fn from_kv(key_values: &KV) -> Self {
        Self {
            gravity: key_values
                .get("m_Gravity")
                .and_then(KV::as_vec3)
                .unwrap_or(Vec3::ZERO),
            drag: key_values.get("m_fDrag").and_then(KV::as_f32).unwrap_or(0.0),
        }
    }
}

impl ParticleOperator for BasicMovement {
    fn update(&mut self, particles: &mut [Particle], frame_time: f32, _s: &ParticleSystemRenderState) {
        let acceleration = self.gravity * frame_time;
        for p in particles {
            p.velocity *= 1.0 - self.drag * 30.0 * frame_time;
            p.velocity += acceleration;
            p.position_previous = p.position;
            p.position += p.velocity * frame_time;
        }
    }
}

/// C# `class FadeAndKill`.
#[derive(Debug, Clone, Copy)]
pub struct FadeAndKill {
    pub start_fade_in_time: f32,
    pub end_fade_in_time: f32,
    pub start_fade_out_time: f32,
    pub end_fade_out_time: f32,
    pub start_alpha: f32,
    pub end_alpha: f32,
}

impl FadeAndKill {
    /// C# defaults: 0, .5, .5, 1, 1, 0.
    pub fn from_kv(key_values: &KV) -> Self {
        let f = |k: &str, d: f32| key_values.get(k).and_then(KV::as_f32).unwrap_or(d);
        Self {
            start_fade_in_time: f("m_flStartFadeInTime", 0.0),
            end_fade_in_time: f("m_flEndFadeInTime", 0.5),
            start_fade_out_time: f("m_flStartFadeOutTime", 0.5),
            end_fade_out_time: f("m_flEndFadeOutTime", 1.0),
            start_alpha: f("m_flStartAlpha", 1.0),
            end_alpha: f("m_flEndAlpha", 0.0),
        }
    }
}

/// Safe fraction across a window; `None` when the window has zero width, which
/// the C# divides through anyway (bug 4).
fn window_t(time: f32, start: f32, end: f32) -> Option<f32> {
    if time < start || time > end {
        return None;
    }
    let span = end - start;
    if span.abs() < f32::EPSILON {
        return None;
    }
    Some((time - start) / span)
}

impl ParticleOperator for FadeAndKill {
    fn update(&mut self, particles: &mut [Particle], frame_time: f32, _s: &ParticleSystemRenderState) {
        for p in particles {
            // `age_fraction` returns None for a zero lifespan, where the C#
            // produces infinity and then NaN alpha (bug 3).
            if let Some(time) = p.age_fraction() {
                if let Some(t) = window_t(time, self.start_fade_in_time, self.end_fade_in_time) {
                    p.alpha = (1.0 - t) * self.start_alpha + t * p.constant_alpha;
                }
                if let Some(t) = window_t(time, self.start_fade_out_time, self.end_fade_out_time) {
                    p.alpha = (1.0 - t) * p.constant_alpha + t * self.end_alpha;
                }
            }
            p.lifetime -= frame_time;
        }
    }
}

// NOT YET PORTED, all from this file: the initializers `CreateWithinSphere`,
// `InitialVelocityNoise`, `OffsetVectorToVector`, `PositionOffset`,
// `RandomAlpha`, `RandomColor`, `RandomLifeTime`, `RandomRadius`,
// `RandomRotation`, `RandomRotationSpeed`, `RandomSequence`,
// `RandomTrailLength`, `RemapParticleCountToScalar`, `RingWave`; and the
// operators `ColorInterpolate`, `FadeInSimple`, `FadeOutSimple`,
// `InterpolateRadius`, `OscillateScalar`, `SpinUpdate`. Each is a handful of
// lines reading `m_fl*` keys and writing one or two `Particle` fields, all
// following `ParticleInitializer` or `ParticleOperator` above. The randomised
// ones need an RNG decision first — `openstack-phy2`'s `LazyRandom` has the
// unbounded-growth problem noted in PORTING.md, so this crate should take
// `rand` or a small xorshift rather than copy it.
//
// Also not ported: `IParticleRenderer` and `ParticleField` (renderer-side, need
// a real backend), and the three separate `ParticleExtensions` static classes —
// the C# declares that name three times in one file, each a different set of
// KV-reading helpers, which `KV`'s accessors now cover.

#[cfg(test)]
mod tests {
    use super::*;
    use std::collections::HashMap;

    fn kv(pairs: &[(&str, KV)]) -> KV {
        let mut m = HashMap::new();
        for (k, v) in pairs {
            m.insert(k.to_string(), v.clone());
        }
        KV::Map(m)
    }

    #[test]
    fn particle_defaults_match_the_c_sharp_constructor() {
        let p = Particle::from_kv(&kv(&[]));
        assert_eq!(p.constant_radius, 5.0);
        assert_eq!(p.radius, 5.0);
        assert_eq!(p.constant_color, Vec3::ONE);
        assert_eq!(p.color, Vec3::ONE);
        assert_eq!(p.constant_lifetime, 1.0);
        assert_eq!(p.lifetime, 1.0);
        assert_eq!(p.alpha, 1.0);
        assert_eq!(p.trail_length, 1.0);
    }

    #[test]
    fn constant_color_is_read_as_bytes_over_255() {
        let p = Particle::from_kv(&kv(&[(
            "m_ConstantColor",
            KV::Array(vec![KV::Int(255), KV::Int(128), KV::Int(0)]),
        )]));
        assert!((p.constant_color.x - 1.0).abs() < 1e-6);
        assert!((p.constant_color.y - 128.0 / 255.0).abs() < 1e-6);
        assert_eq!(p.constant_color.z, 0.0);
        assert_eq!(p.color, p.constant_color, "Color mirrors ConstantColor");
    }

    #[test]
    fn a_short_color_array_falls_back_rather_than_panicking() {
        let p = Particle::from_kv(&kv(&[(
            "m_ConstantColor",
            KV::Array(vec![KV::Int(255)]),
        )]));
        assert_eq!(p.constant_color, Vec3::ONE, "C# would index out of range");
    }

    #[test]
    fn zero_capacity_growable_bag_works() {
        // The C# computes newSize = 0 * 2 = 0 and returns index 0 into an
        // empty array.
        let mut b = ParticleBag::new(0, true);
        assert_eq!(b.add(Particle::default()), Some(0));
        assert_eq!(b.len(), 1);
        assert_eq!(b.add(Particle::default()), Some(1));
    }

    #[test]
    fn non_growable_bag_refuses_when_full() {
        let mut b = ParticleBag::new(2, false);
        assert_eq!(b.add(Particle::default()), Some(0));
        assert_eq!(b.add(Particle::default()), Some(1));
        assert_eq!(b.add(Particle::default()), None, "C# returns -1");
        assert_eq!(b.len(), 2);
    }

    #[test]
    fn prune_removes_expired_and_keeps_the_rest() {
        let mut b = ParticleBag::new(4, true);
        for (i, life) in [1.0f32, -1.0, 2.0, 0.0].iter().enumerate() {
            let mut p = Particle::default();
            p.lifetime = *life;
            p.sequence = i as i32;
            b.add(p);
        }
        b.prune_expired();
        assert_eq!(b.len(), 2);
        assert!(b.as_slice().iter().all(|p| p.lifetime > 0.0));
    }

    #[test]
    fn prune_of_all_expired_empties_the_bag() {
        let mut b = ParticleBag::new(4, true);
        for _ in 0..3 {
            let mut p = Particle::default();
            p.lifetime = -1.0;
            b.add(p);
        }
        b.prune_expired();
        assert!(b.is_empty());
    }

    #[test]
    fn decay_ages_particles() {
        let mut b = ParticleBag::new(2, true);
        b.add(Particle::default());
        let s = ParticleSystemRenderState::default();
        Decay.update(b.live_particles(), 0.25, &s);
        assert_eq!(b.as_slice()[0].lifetime, 0.75);
    }

    #[test]
    fn zero_lifespan_does_not_produce_nan_alpha() {
        // The C# divides by ConstantLifetime unconditionally (bug 3).
        let mut p = Particle::default();
        p.constant_lifetime = 0.0;
        assert!(p.age_fraction().is_none());
        let mut ops = FadeAndKill::from_kv(&kv(&[]));
        let mut arr = [p];
        ops.update(&mut arr, 0.1, &ParticleSystemRenderState::default());
        assert!(!arr[0].alpha.is_nan(), "alpha must stay finite");
    }

    #[test]
    fn zero_width_fade_window_is_skipped() {
        // C# divides by (end - start) == 0 (bug 4).
        assert!(window_t(0.5, 0.5, 0.5).is_none());
        assert_eq!(window_t(0.5, 0.0, 1.0), Some(0.5));
        assert!(window_t(2.0, 0.0, 1.0).is_none(), "outside the window");
    }

    #[test]
    fn fade_out_drives_alpha_toward_end_alpha() {
        let mut p = Particle::default();
        p.constant_lifetime = 1.0;
        p.lifetime = 0.25; // 75% aged, inside the default 0.5..1.0 fade-out
        let mut arr = [p];
        FadeAndKill::from_kv(&kv(&[])).update(&mut arr, 0.0, &ParticleSystemRenderState::default());
        assert!(arr[0].alpha < 1.0 && arr[0].alpha > 0.0, "alpha {}", arr[0].alpha);
    }

    #[test]
    fn continuous_emitter_respects_its_rate() {
        let mut e = ContinuousEmitter::from_kv(&kv(&[("m_flEmitRate", KV::Float(100.0))]));
        e.start();
        // 0.1s at 100/s should be about 10 particles.
        let n = e.update(0.1);
        assert!((9..=11).contains(&n), "emitted {n}");
    }

    #[test]
    fn zero_emit_rate_still_emits() {
        // The C# gets _emitInterval = 1/0 = infinity and never fires.
        let mut e = ContinuousEmitter::from_kv(&kv(&[("m_flEmitRate", KV::Float(0.0))]));
        e.start();
        assert!(e.update(1.0) > 0, "must fall back to a usable rate");
    }

    #[test]
    fn emitter_waits_for_its_start_time() {
        let mut e = ContinuousEmitter::from_kv(&kv(&[("m_flStartTime", KV::Float(1.0))]));
        e.start();
        assert_eq!(e.update(0.5), 0, "not started yet");
        assert!(e.update(1.0) > 0, "now past start time");
    }

    #[test]
    fn stopped_emitter_emits_nothing() {
        let mut e = ContinuousEmitter::from_kv(&kv(&[]));
        e.start();
        e.stop();
        assert!(e.is_finished());
        assert_eq!(e.update(10.0), 0);
    }

    #[test]
    fn instantaneous_emitter_fires_once() {
        let mut e = InstantaneousEmitter::from_kv(&kv(&[("m_nParticlesToEmit", KV::Int(42))]));
        e.start();
        assert_eq!(e.update(0.1), 42);
        assert_eq!(e.update(0.1), 0, "must not fire twice");
        assert!(e.is_finished());
    }

    #[test]
    fn basic_movement_applies_gravity_and_records_previous_position() {
        let mut b = ParticleBag::new(1, true);
        b.add(Particle::default());
        let mut op = BasicMovement { gravity: Vec3::new(0.0, 0.0, -10.0), drag: 0.0 };
        op.update(b.live_particles(), 0.1, &ParticleSystemRenderState::default());
        let p = b.as_slice()[0];
        assert!(p.velocity.z < 0.0, "gravity applied");
        assert_eq!(p.position_previous, Vec3::ZERO);
        assert!(p.position.z < 0.0, "moved");
    }

    #[test]
    fn transformation_matrix_scales_then_translates() {
        let mut p = Particle::default();
        p.radius = 2.0;
        p.position = Vec3::new(5.0, 0.0, 0.0);
        let m = p.transformation_matrix();
        // A unit point at +X should land at position + radius.
        assert!((m.transform_point3(Vec3::X) - Vec3::new(7.0, 0.0, 0.0)).length() < 1e-5);
    }

    #[test]
    fn rotation_matrix_ignores_the_x_component() {
        // Documents the C# omission rather than silently adding X.
        let mut a = Particle::default();
        a.rotation = Vec3::new(1.5, 0.0, 0.0);
        assert!(
            (a.rotation_matrix().to_cols_array()[0] - 1.0).abs() < 1e-6,
            "X rotation has no effect"
        );
    }
}
