// PORT-SOURCE: Platforms/OpenStack.Platform.Sdl/Platform_Sdl.cs
// PORT-SHA: bd4d390b1cfb29a9
// PORT-STATUS: done
//
// Platform registration for the SDL 3 backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches SDL.
//
// The renderer half is portable via the `sdl2` crate; only this registration
// layer is translated here.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `SdlPlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct SdlPlatform;

impl Platform for SdlPlatform {
    /// C# `base("SD", ...)`.
    fn id(&self) -> &str {
        "SD"
    }

    /// C# `base(..., "SDL 3")`.
    fn name(&self) -> &str {
        "SDL 3"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl SdlPlatform {
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
        let p = SdlPlatform;
        assert_eq!(p.id(), "SD");
        assert_eq!(p.name(), "SDL 3");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = SdlPlatform.gfx_slots();
        assert!(!s.api, "api");
        assert!(s.sprite2d, "sprite2d");
        assert!(!s.sprite3d, "sprite3d");
        assert!(!s.model, "model");
        assert!(!s.light, "light");
        assert!(!s.terrain, "terrain");
    }
}
