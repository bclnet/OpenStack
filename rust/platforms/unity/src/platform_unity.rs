// PORT-SOURCE: Platforms/OpenStack.Platform.Unity/Platform_Unity.cs
// PORT-SHA: e8b0e03f9431f852
// PORT-STATUS: done
//
// Platform registration for the Unity backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches UnityEngine.
//
// Unity is the only backend here that fills all six slots. Its render half is
// C# by necessity (UnityEngine is the scripting API), so the practical split is
// to keep that in C# and have it call a Rust `cdylib`. Note the C# caps value
// is `Caps.None_` — with a trailing underscore, and the only platform not
// claiming `Drawing` despite filling every slot.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `UnityPlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct UnityPlatform;

impl Platform for UnityPlatform {
    /// C# `base("UN", ...)`.
    fn id(&self) -> &str {
        "UN"
    }

    /// C# `base(..., "Unity")`.
    fn name(&self) -> &str {
        "Unity"
    }

    fn caps(&self) -> Caps {
        Caps::NONE
    }
}

impl UnityPlatform {
    /// C# `GfxFactory` — which of the six `GfX.X*` slots this backend fills.
    ///
    /// The C# returns `IOpenGfx[]` with nulls for unfilled slots, so a consumer
    /// indexing `gfx[GfX.XModel]` on a backend that does not provide one gets
    /// null and throws at the first call. `GfxSlots` names each slot, so an
    /// unfilled one is `false` and testable.
    pub const fn gfx_slots(&self) -> GfxSlots {
        GfxSlots {
            api: true,
            sprite2d: true,
            sprite3d: true,
            model: true,
            light: true,
            terrain: true,
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
        let p = UnityPlatform;
        assert_eq!(p.id(), "UN");
        assert_eq!(p.name(), "Unity");
        assert_eq!(p.caps(), Caps::NONE);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = UnityPlatform.gfx_slots();
        assert!(s.api, "api");
        assert!(s.sprite2d, "sprite2d");
        assert!(s.sprite3d, "sprite3d");
        assert!(s.model, "model");
        assert!(s.light, "light");
        assert!(s.terrain, "terrain");
    }
}
