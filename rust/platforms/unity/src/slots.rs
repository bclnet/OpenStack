// Shared slot map for the platform registration layer.
//
// C# `GfxFactory` returns `IOpenGfx[]` indexed by the `GfX.X*` constants
// (`XApi`=0, `XSprite2D`=1, `XSprite3D`=2, `XModel`=3, `XLight`=4,
// `XTerrain`=5), with `null` in every slot the backend does not fill. A
// consumer doing `(IOpenGfxModel<..>)gfx[GfX.XModel]` on a backend without one
// casts the null successfully and throws at the first real call, far from the
// cause.
//
// Naming the slots makes an unfilled one a `false` a caller can test, and makes
// the length mismatch below impossible to write.
//
// ===================== A C#-SIDE BUG THIS EXPOSES =========================
//
// `GfX.XTerrain` is **5**, but `Platform_OpenGL.cs`'s factory returns a
// **seven**-element array with `null` at index 5 and `OpenGLGfxTerrain()` at
// index 6:
//
//     GfxFactory = () => [new OpenGLGfxApi(), null, new OpenGLGfxSprite3D(),
//                         new OpenGLGfxModel(), null, null, new OpenGLGfxTerrain()];
//                        //  0                1     2                        3
//                        //  4     5     6 <- terrain lands here
//
// Every other platform returns six elements. So `gfx[GfX.XTerrain]` reads the
// `null` at index 5 and **OpenGL's terrain renderer is unreachable** — the
// object is constructed on every platform activation and never used. There is
// exactly one `gfx[GfX.XTerrain]` call site in the solution
// (`CellBuilder.CreateLand`), which is why nobody noticed: terrain simply never
// renders on the OpenGL backend. **Fix this in the C# tree** by dropping the
// stray `null` at index 5.

/// Which `GfX.X*` slots a backend fills.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct GfxSlots {
    /// C# `GfX.XApi` (0).
    pub api: bool,
    /// C# `GfX.XSprite2D` (1).
    pub sprite2d: bool,
    /// C# `GfX.XSprite3D` (2).
    pub sprite3d: bool,
    /// C# `GfX.XModel` (3).
    pub model: bool,
    /// C# `GfX.XLight` (4).
    pub light: bool,
    /// C# `GfX.XTerrain` (5).
    pub terrain: bool,
}

impl GfxSlots {
    /// How many slots are filled.
    pub const fn count(&self) -> usize {
        self.api as usize
            + self.sprite2d as usize
            + self.sprite3d as usize
            + self.model as usize
            + self.light as usize
            + self.terrain as usize
    }
}

/// C# `SfxFactory` — a one-element array in every platform.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct SfxSlots {
    pub audio: bool,
}
