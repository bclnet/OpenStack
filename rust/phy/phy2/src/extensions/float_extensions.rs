// PORT-SOURCE: Phy/OpenStack.Phy2/Extensions/FloatExtensions.cs
// PORT-SHA: 147756bfb82afd97
// PORT-STATUS: done
//
// C# extension methods on `float` -> a blanket-implemented trait, as elsewhere
// in this port. All three have exact `std` equivalents, noted per method.

/// C# `static class FloatExtensions`.
pub trait FloatExt {
    /// C# `ToRadians()`. `f32::to_radians` is the same conversion.
    fn to_radians_ext(self) -> f32;
    /// C# `ToDegrees()`. `f32::to_degrees` is the same conversion.
    fn to_degrees_ext(self) -> f32;
    /// C# `Clamp(min, max)`.
    ///
    /// The C# applies `min` then `max` unconditionally, so an inverted range
    /// (`min > max`) yields `max` rather than panicking as `f32::clamp` would.
    /// Preserved.
    fn clamp_ext(self, min: f32, max: f32) -> f32;
}

impl FloatExt for f32 {
    #[inline]
    fn to_radians_ext(self) -> f32 {
        std::f32::consts::PI / 180.0 * self
    }

    #[inline]
    fn to_degrees_ext(self) -> f32 {
        180.0 / std::f32::consts::PI * self
    }

    #[inline]
    fn clamp_ext(self, min: f32, max: f32) -> f32 {
        let mut f = self;
        if f < min {
            f = min;
        }
        if f > max {
            f = max;
        }
        f
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn angle_conversions_match_std() {
        for a in [0.0f32, 1.0, 90.0, 180.0, -45.0] {
            assert!((a.to_radians_ext() - a.to_radians()).abs() < 1e-6);
            assert!((a.to_degrees_ext() - a.to_degrees()).abs() < 1e-4);
        }
    }

    #[test]
    fn conversions_round_trip() {
        assert!((90.0f32.to_radians_ext().to_degrees_ext() - 90.0).abs() < 1e-4);
    }

    #[test]
    fn clamp_bounds_normally() {
        assert_eq!(5.0f32.clamp_ext(0.0, 10.0), 5.0);
        assert_eq!((-1.0f32).clamp_ext(0.0, 10.0), 0.0);
        assert_eq!(11.0f32.clamp_ext(0.0, 10.0), 10.0);
    }

    #[test]
    fn inverted_range_yields_max_instead_of_panicking() {
        // f32::clamp panics here; the C# returns max because it applies the
        // two bounds in sequence.
        assert_eq!(5.0f32.clamp_ext(10.0, 1.0), 1.0);
    }
}
