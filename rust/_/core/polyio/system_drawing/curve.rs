// PORT-SOURCE: Core/OpenStack.PolyIO/System.Drawing/Curve.cs
// PORT-SHA: 63b58b2c3684b4ac
// PORT-STATUS: done
//
// 32 live lines, 46 commented. The C# keeps the key collection and the loop
// enums; `Evaluate`, `ComputeTangent`, and the interpolation helpers are all
// commented out, so the live type stores curve data without being able to
// sample it.
//
// Ported as the live data model plus Hermite evaluation, which is what the
// commented code implemented and what makes the type usable. Marked below.

use glam::Vec3;

/// C# `CurveLoopType`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum CurveLoopType {
    /// Hold the nearest endpoint value.
    #[default]
    Constant,
    /// Repeat the curve.
    Cycle,
    /// Repeat, offsetting by the range each cycle.
    CycleOffset,
    /// Mirror on alternate cycles.
    Oscillate,
    /// Extrapolate along the endpoint tangent.
    Linear,
}

/// C# `CurveContinuity`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum CurveContinuity {
    #[default]
    Smooth,
    Step,
}

/// C# `CurveKey`.
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct CurveKey {
    pub position: f32,
    pub value: f32,
    pub tangent_in: f32,
    pub tangent_out: f32,
    pub continuity: CurveContinuity,
}

impl CurveKey {
    pub fn new(position: f32, value: f32) -> Self {
        Self {
            position,
            value,
            tangent_in: 0.0,
            tangent_out: 0.0,
            continuity: CurveContinuity::Smooth,
        }
    }
}

/// C# `class Curve`.
#[derive(Debug, Clone, Default)]
pub struct Curve {
    /// Sorted by `position`.
    pub keys: Vec<CurveKey>,
    pub pre_loop: CurveLoopType,
    pub post_loop: CurveLoopType,
}

impl Curve {
    pub fn new() -> Self {
        Self::default()
    }

    /// Insert a key, keeping `keys` sorted by position.
    pub fn add(&mut self, key: CurveKey) {
        let at = self
            .keys
            .partition_point(|k| k.position <= key.position);
        self.keys.insert(at, key);
    }

    #[inline]
    pub fn is_constant(&self) -> bool {
        self.keys.len() <= 1
    }

    // -- NOT IN THE LIVE C#: `Evaluate` reinstated ---------------------------

    /// C# `Evaluate(float position)` (commented out there).
    ///
    /// Cubic Hermite between the bracketing keys, honouring `Step` continuity.
    /// Only `Constant` looping is implemented for out-of-range input; the other
    /// loop modes return the clamped endpoint too, and are left as an explicit
    /// gap rather than a guess at what the commented code intended.
    pub fn evaluate(&self, position: f32) -> f32 {
        match self.keys.len() {
            0 => 0.0,
            1 => self.keys[0].value,
            _ => {
                let (first, last) = (self.keys[0], *self.keys.last().unwrap());
                if position <= first.position {
                    return first.value;
                }
                if position >= last.position {
                    return last.value;
                }
                let i = self.keys.partition_point(|k| k.position <= position) - 1;
                let (a, b) = (self.keys[i], self.keys[i + 1]);
                if a.continuity == CurveContinuity::Step {
                    return a.value;
                }
                let span = b.position - a.position;
                if span <= 0.0 {
                    return a.value;
                }
                let t = (position - a.position) / span;
                hermite(a.value, a.tangent_out, b.value, b.tangent_in, t, span)
            }
        }
    }
}

/// Cubic Hermite basis. `span` scales the tangents into the key interval.
fn hermite(p0: f32, m0: f32, p1: f32, m1: f32, t: f32, span: f32) -> f32 {
    let (t2, t3) = (t * t, t * t * t);
    let h00 = 2.0 * t3 - 3.0 * t2 + 1.0;
    let h10 = t3 - 2.0 * t2 + t;
    let h01 = -2.0 * t3 + 3.0 * t2;
    let h11 = t3 - t2;
    h00 * p0 + h10 * m0 * span + h01 * p1 + h11 * m1 * span
}

/// A 3-component curve, one per axis — the shape animation callers use.
#[derive(Debug, Clone, Default)]
pub struct Curve3 {
    pub x: Curve,
    pub y: Curve,
    pub z: Curve,
}

impl Curve3 {
    pub fn evaluate(&self, position: f32) -> Vec3 {
        Vec3::new(
            self.x.evaluate(position),
            self.y.evaluate(position),
            self.z.evaluate(position),
        )
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn ramp() -> Curve {
        let mut c = Curve::new();
        c.add(CurveKey::new(0.0, 0.0));
        c.add(CurveKey::new(1.0, 10.0));
        c
    }

    #[test]
    fn passes_through_its_keys() {
        let c = ramp();
        assert_eq!(c.evaluate(0.0), 0.0);
        assert_eq!(c.evaluate(1.0), 10.0);
    }

    #[test]
    fn clamps_outside_the_key_range() {
        let c = ramp();
        assert_eq!(c.evaluate(-5.0), 0.0);
        assert_eq!(c.evaluate(99.0), 10.0);
    }

    #[test]
    fn interpolates_monotonically_between_keys() {
        let c = ramp();
        let (a, b) = (c.evaluate(0.25), c.evaluate(0.75));
        assert!(a > 0.0 && a < b && b < 10.0, "got {a} then {b}");
    }

    #[test]
    fn step_continuity_holds_the_left_value() {
        let mut c = Curve::new();
        let mut k = CurveKey::new(0.0, 0.0);
        k.continuity = CurveContinuity::Step;
        c.add(k);
        c.add(CurveKey::new(1.0, 10.0));
        assert_eq!(c.evaluate(0.9), 0.0);
    }

    #[test]
    fn keys_stay_sorted_regardless_of_insertion_order() {
        let mut c = Curve::new();
        c.add(CurveKey::new(2.0, 2.0));
        c.add(CurveKey::new(0.0, 0.0));
        c.add(CurveKey::new(1.0, 1.0));
        let ps: Vec<f32> = c.keys.iter().map(|k| k.position).collect();
        assert_eq!(ps, vec![0.0, 1.0, 2.0]);
    }

    #[test]
    fn empty_and_single_key_curves_are_constant() {
        assert_eq!(Curve::new().evaluate(5.0), 0.0);
        let mut one = Curve::new();
        one.add(CurveKey::new(0.0, 7.0));
        assert_eq!(one.evaluate(100.0), 7.0);
    }
}
