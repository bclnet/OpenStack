// PORT-SOURCE: Platforms/OpenStack.Platform.OpenGL/Platform_OpenGL.cs
// PORT-SHA: a3ccdc40914a4099
// PORT-STATUS: done
//
// Platform registration for the OpenGL backend. The engine-independent half —
// id, name, caps, slot map — ports directly; the render half (2,453 lines
// across `Gfx/OpenGL_Render.cs` and `Egin/Gl_Render.cs`) is viable via `glow`
// and `glutin` but wants a GPU to develop against, so it is not attempted here.
//
// ============ THIS BACKEND'S TERRAIN RENDERER IS UNREACHABLE ==============
//
// `GfX.XTerrain` is 5, but the C# factory returns a **seven**-element array
// with `null` at 5 and the terrain object at 6:
//
//     GfxFactory = () => [new OpenGLGfxApi(), null, new OpenGLGfxSprite3D(),
//                         new OpenGLGfxModel(), null, null, new OpenGLGfxTerrain()];
//
// Every other platform returns six. So `gfx[GfX.XTerrain]` reads the stray
// `null`, `CellBuilder.CreateLand` sees no terrain provider, and
// `OpenGLGfxTerrain` is constructed on every activation and never called.
// Terrain does not render on OpenGL. See `slots.rs` for the full write-up.
//
// The slot map below is what the C# *intended*; `gfx_slots_bug_compat` is what
// it actually produces.

use openstack::platform::{Caps, Platform};

use crate::slots::{GfxSlots, SfxSlots};

/// C# `OpenGLPlatform`.
#[derive(Debug, Clone, Copy, Default)]
pub struct OpenGlPlatform;

impl Platform for OpenGlPlatform {
    fn id(&self) -> &str {
        "GL"
    }

    fn name(&self) -> &str {
        "OpenGL"
    }

    fn caps(&self) -> Caps {
        Caps::DRAWING
    }
}

impl OpenGlPlatform {
    /// The slots this backend provides, with terrain in the slot `GfX.XTerrain`
    /// actually names.
    pub const fn gfx_slots(&self) -> GfxSlots {
        GfxSlots {
            api: true,
            sprite2d: false,
            sprite3d: true,
            model: true,
            light: false,
            terrain: true,
        }
    }

    /// What the C# array actually yields when indexed by the `GfX.X*`
    /// constants: terrain missing, because it sits one slot past where the
    /// constant points.
    #[deprecated(note = "mirrors a C#-side bug: terrain lands at index 6, but GfX.XTerrain is 5")]
    pub const fn gfx_slots_bug_compat(&self) -> GfxSlots {
        GfxSlots { terrain: false, ..self.gfx_slots() }
    }

    pub const fn sfx_slots(&self) -> SfxSlots {
        SfxSlots { audio: true }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn registration_matches_the_c_sharp() {
        let p = OpenGlPlatform;
        assert_eq!(p.id(), "GL");
        assert_eq!(p.name(), "OpenGL");
        assert_eq!(p.caps(), Caps::DRAWING);
    }

    #[test]
    fn intended_slots_include_terrain() {
        let s = OpenGlPlatform.gfx_slots();
        assert!(s.api && s.sprite3d && s.model && s.terrain);
        assert!(!s.sprite2d && !s.light);
        assert_eq!(s.count(), 4);
    }

    #[test]
    fn the_c_sharp_array_loses_terrain() {
        // Documents the off-by-one rather than silently correcting it.
        #[allow(deprecated)]
        let actual = OpenGlPlatform.gfx_slots_bug_compat();
        assert!(!actual.terrain, "GfX.XTerrain (5) reads the stray null");
        assert_eq!(actual.count(), 3, "one fewer than intended");
    }
}
