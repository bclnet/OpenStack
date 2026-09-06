// PORT-SOURCE: Phy/OpenStack.Phy2/Extensions/QuaternionExtensions.cs
// PORT-SHA: bea8e44f7572cd2e
// PORT-STATUS: done

use glam::Quat;

use crate::physics_globals::EPSILON;

/// C# `static class QuaternionExtensions`.
pub trait QuatExt {
    /// C# `IsValid()` — finite components and unit length within 5 epsilon.
    fn is_valid(self) -> bool;
}

impl QuatExt for Quat {
    fn is_valid(self) -> bool {
        if self.x.is_nan() || self.y.is_nan() || self.z.is_nan() || self.w.is_nan() {
            return false;
        }
        let length = self.length();
        if length.is_nan() {
            return false;
        }
        (length - 1.0).abs() <= EPSILON * 5.0
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn unit_quaternions_are_valid() {
        assert!(Quat::IDENTITY.is_valid());
        assert!(Quat::from_rotation_z(1.2).is_valid());
    }

    #[test]
    fn nan_components_are_rejected() {
        assert!(!Quat::from_xyzw(f32::NAN, 0.0, 0.0, 1.0).is_valid());
    }

    #[test]
    fn non_unit_length_is_rejected() {
        assert!(!Quat::from_xyzw(0.0, 0.0, 0.0, 2.0).is_valid());
        assert!(!Quat::from_xyzw(0.0, 0.0, 0.0, 0.0).is_valid(), "zero length");
    }

    #[test]
    fn the_tolerance_is_five_epsilon() {
        // Just inside and just outside the documented band.
        let inside = Quat::from_xyzw(0.0, 0.0, 0.0, 1.0 + EPSILON * 4.0);
        let outside = Quat::from_xyzw(0.0, 0.0, 0.0, 1.0 + EPSILON * 6.0);
        assert!(inside.is_valid());
        assert!(!outside.is_valid());
    }
}
