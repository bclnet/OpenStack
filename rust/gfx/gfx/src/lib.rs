//! `openstack-gfx` — 1:1 port of .NET project `OpenStack.Gfx`.
//!
//! Module layout mirrors the C# file layout. See PORT_MAP.tsv and PORTING.md.
//!
//! All 6 files ported.

pub mod gfx;
pub mod gfx_bitmap;
pub mod gfx_render;
pub mod gfx_texture;
pub mod texture_helper;
pub mod texture_sequences;

pub mod prelude {
    pub use crate::gfx::{
        Backend, GfxAlphaMode, GfxAttach, GfxBlendMode, Material, MaterialManager, MaterialProp,
        Shader, ShaderManager, Sprite, Texture, TextureBuilder, TextureBytes, TextureManager,
    };
    pub use crate::gfx_texture::{DdsError, DdsHeader, DdsPixelFormat, TextureFlags, TextureFormat};
    pub use crate::gfx_render::{blit_by_palette, Color32, Colorf, Pass, Renderer};
    pub use crate::gfx_bitmap::{Color, DirectBitmap};
    pub use crate::texture_helper::{
        calculate_png_size, downscale_rgba8_x2, mip_level_size, mipmap_count, mipmap_data_size,
        mipmap_true_data_size, BlockFormat,
    };
    pub use crate::texture_sequences::{Frame, Image, Sequence, TextureSequences};
}
