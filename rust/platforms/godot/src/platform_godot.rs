// PORT-SOURCE: Platforms/OpenStack.Platform.Godot/Platform_Godot.cs
// PORT-SHA: 739d3d9cba092795
// PORT-STATUS: done
//
// Platform registration for the Godot backend.
//
// This is the engine-*independent* half of the C# file and it ports directly:
// the platform id, display name, capability flags, and which graphics/audio
// manager slots the backend fills. None of it touches Godot itself.
//
// The rest of this crate references Godot types (`XShader`) with no Godot
// package reference, so the C# project does not compile as given — the same
// defect as `phy2`. When it is fixed, `godot` (gdext) is the Rust binding.
//

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `GodotPlatform` — `PlatformX` registration.
#[derive(Debug, Clone, Copy, Default)]
pub struct GodotPlatform;

impl Platform for GodotPlatform {
    /// C# `base("GD", ...)`.
    fn id(&self) -> &str {
        "GD"
    }

    /// C# `base(..., "Godot")`.
    fn name(&self) -> &str {
        "Godot"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl GodotPlatform {
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
        let p = GodotPlatform;
        assert_eq!(p.id(), "GD");
        assert_eq!(p.name(), "Godot");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn slot_map_matches_the_c_sharp_factory_array() {
        let s = GodotPlatform.gfx_slots();
        assert!(s.api, "api");
        assert!(s.sprite2d, "sprite2d");
        assert!(s.sprite3d, "sprite3d");
        assert!(s.model, "model");
        assert!(!s.light, "light");
        assert!(!s.terrain, "terrain");
    }
}
