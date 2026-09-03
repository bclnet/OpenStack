// PORT-SOURCE: Platforms/OpenStack.Platform.O3de/Platform_O3de.cs
// PORT-SHA: 524150ae413b7ac6
// PORT-STATUS: done
//
// Platform registration for the O3de backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches O3DE — there is no O3DE binding in the C# project either.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `O3dePlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct O3dePlatform;

impl Platform for O3dePlatform {
    /// C# `base("O3", ...)`.
    fn id(&self) -> &str {
        "O3"
    }

    /// C# `base(..., "O3de")`.
    fn name(&self) -> &str {
        "O3de"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl O3dePlatform {
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
        let p = O3dePlatform;
        assert_eq!(p.id(), "O3");
        assert_eq!(p.name(), "O3de");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = O3dePlatform.gfx_slots();
        assert!(!s.api, "api");
        assert!(!s.sprite2d, "sprite2d");
        assert!(s.sprite3d, "sprite3d");
        assert!(s.model, "model");
        assert!(!s.light, "light");
        assert!(!s.terrain, "terrain");
    }
}
