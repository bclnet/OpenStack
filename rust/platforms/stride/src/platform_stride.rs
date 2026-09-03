// PORT-SOURCE: Platforms/OpenStack.Platform.Stride/Platform_Stride.cs
// PORT-SHA: e18bbe030c7700cd
// PORT-STATUS: done
//
// Platform registration for the Stride backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches Stride.
//
// Stride is a .NET-only engine with no Rust counterpart, so the render half of
// this crate does not port. This registration layer does.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `StridePlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct StridePlatform;

impl Platform for StridePlatform {
    /// C# `base("ST", ...)`.
    fn id(&self) -> &str {
        "ST"
    }

    /// C# `base(..., "Stride")`.
    fn name(&self) -> &str {
        "Stride"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl StridePlatform {
    /// C# `GfxFactory` — which of the six `GfX.X*` slots this backend fills.
    ///
    /// The C# returns `IOpenGfx[]` with nulls for unfilled slots, so a consumer
    /// indexing `gfx[GfX.XModel]` on a backend that does not provide one gets
    /// null and throws at the first call. `GfxSlots` names each slot, so an
    /// unfilled one is `false` and testable.
    pub const fn gfx_slots(&self) -> GfxSlots {
        GfxSlots {
            api: false,
            sprite2d: false,
            sprite3d: true,
            model: true,
            light: false,
            terrain: false,
        }
    }

    /// C# `SfxFactory`.
    pub const fn sfx_slots(&self) -> SfxSlots {
        SfxSlots { audio: true }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn registration_matches_the_c_sharp() {
        let p = StridePlatform;
        assert_eq!(p.id(), "ST");
        assert_eq!(p.name(), "Stride");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = StridePlatform.gfx_slots();
        assert!(!s.api, "api");
        assert!(!s.sprite2d, "sprite2d");
        assert!(s.sprite3d, "sprite3d");
        assert!(s.model, "model");
        assert!(!s.light, "light");
        assert!(!s.terrain, "terrain");
    }
}
