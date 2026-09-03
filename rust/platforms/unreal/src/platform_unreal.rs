// PORT-SOURCE: Platforms/OpenStack.Platform.Unreal/Platform_Unreal.cs
// PORT-SHA: 47f3a900ee3207a2
// PORT-STATUS: done
//
// Platform registration for the Unreal backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches Unreal — 19 of its ~35 members throw `NotImplementedException`.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `UnrealPlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct UnrealPlatform;

impl Platform for UnrealPlatform {
    /// C# `base("UR", ...)`.
    fn id(&self) -> &str {
        "UR"
    }

    /// C# `base(..., "Unreal")`.
    fn name(&self) -> &str {
        "Unreal"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl UnrealPlatform {
    /// C# `GfxFactory` — which of the six `GfX.X*` slots this backend fills.
    ///
    /// The C# returns `IOpenGfx[]` with nulls for unfilled slots, so a consumer
    /// indexing `gfx[GfX.XModel]` on a backend that does not provide one gets
    /// null and throws at the first call. `GfxSlots` names each slot, so an
    /// unfilled one is `false` and testable.
    pub const fn gfx_slots(&self) -> GfxSlots {
        GfxSlots {
            api: true,
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
        let p = UnrealPlatform;
        assert_eq!(p.id(), "UR");
        assert_eq!(p.name(), "Unreal");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = UnrealPlatform.gfx_slots();
        assert!(s.api, "api");
        assert!(!s.sprite2d, "sprite2d");
        assert!(s.sprite3d, "sprite3d");
        assert!(s.model, "model");
        assert!(!s.light, "light");
        assert!(!s.terrain, "terrain");
    }
}
