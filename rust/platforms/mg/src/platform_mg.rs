// PORT-SOURCE: Platforms/OpenStack.Platform.Mg/Platform_Mg.cs
// PORT-SHA: a3c7e5e9d567aefc
// PORT-STATUS: done
//
// Platform registration for the MonoGame backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches MonoGame.
//
// MonoGame has no Rust counterpart; the render half of this crate would be
// rewritten against `wgpu` or `bevy`. This registration layer stands alone.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `MgPlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct MgPlatform;

impl Platform for MgPlatform {
    /// C# `base("MG", ...)`.
    fn id(&self) -> &str {
        "MG"
    }

    /// C# `base(..., "MonoGame")`.
    fn name(&self) -> &str {
        "MonoGame"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl MgPlatform {
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
        let p = MgPlatform;
        assert_eq!(p.id(), "MG");
        assert_eq!(p.name(), "MonoGame");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = MgPlatform.gfx_slots();
        assert!(!s.api, "api");
        assert!(s.sprite2d, "sprite2d");
        assert!(!s.sprite3d, "sprite3d");
        assert!(!s.model, "model");
        assert!(!s.light, "light");
        assert!(!s.terrain, "terrain");
    }
}
