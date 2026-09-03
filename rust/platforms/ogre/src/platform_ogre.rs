// PORT-SOURCE: Platforms/OpenStack.Platform.Ogre/Platform_Ogre.cs
// PORT-SHA: 995742497bf41480
// PORT-STATUS: done
//
// Platform registration for the Ogre backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches Ogre — there is no Ogre binding in the C# project either.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `OgrePlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct OgrePlatform;

impl Platform for OgrePlatform {
    /// C# `base("OG", ...)`.
    fn id(&self) -> &str {
        "OG"
    }

    /// C# `base(..., "Ogre")`.
    fn name(&self) -> &str {
        "Ogre"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl OgrePlatform {
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
        let p = OgrePlatform;
        assert_eq!(p.id(), "OG");
        assert_eq!(p.name(), "Ogre");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = OgrePlatform.gfx_slots();
        assert!(!s.api, "api");
        assert!(!s.sprite2d, "sprite2d");
        assert!(s.sprite3d, "sprite3d");
        assert!(s.model, "model");
        assert!(!s.light, "light");
        assert!(!s.terrain, "terrain");
    }
}
