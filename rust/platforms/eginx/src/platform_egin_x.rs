// PORT-SOURCE: Platforms/OpenStack.Platform.EginX/Platform_EginX.cs
// PORT-SHA: 0db8c6752d56a576
// PORT-STATUS: done
//
// Platform registration for the EginX backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches a graphics API at all — the EginX renderer half is still scaffolding in C# too.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `EginXPlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct EginXPlatform;

impl Platform for EginXPlatform {
    /// C# `base("EX", ...)`.
    fn id(&self) -> &str {
        "EX"
    }

    /// C# `base(..., "EginX")`.
    fn name(&self) -> &str {
        "EginX"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl EginXPlatform {
    /// C# `GfxFactory` — which of the six `GfX.X*` slots this backend fills.
    ///
    /// The C# returns `IOpenGfx[]` with nulls for unfilled slots, so a consumer
    /// indexing `gfx[GfX.XModel]` on a backend that does not provide one gets
    /// null and throws at the first call. `GfxSlots` names each slot, so an
    /// unfilled one is `false` and testable.
    pub const fn gfx_slots(&self) -> GfxSlots {
        GfxSlots {
            api: false,
            sprite2d: true,
            sprite3d: false,
            model: false,
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
        let p = EginXPlatform;
        assert_eq!(p.id(), "EX");
        assert_eq!(p.name(), "EginX");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = EginXPlatform.gfx_slots();
        assert!(!s.api, "api");
        assert!(s.sprite2d, "sprite2d");
        assert!(!s.sprite3d, "sprite3d");
        assert!(!s.model, "model");
        assert!(!s.light, "light");
        assert!(!s.terrain, "terrain");
    }
}
