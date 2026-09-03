// PORT-SOURCE: Gfx/OpenStack.Gfx/Gfx_Texture.cs
// PORT-SHA: 18e4bbf2bcc46a69
// PORT-STATUS: done
//
// Texture format enums and the DDS container header. 864 live C# lines against
// 1029 commented — the largest file in `gfx`, and the most dead weight.
//
// THE ENUMS BELOW WERE GENERATED FROM THE C# SOURCE, not transcribed. DXGI_FORMAT
// alone has 121 variants and TextureUnrealFormat 88; hand-copying those is how
// a one-digit typo ends up decoding textures wrong six months later.
//
// THREE `[Flags]` ATTRIBUTES ARE WRONG. `TextureFormat`, `TexturePixel`, and
// `D3D10_RESOURCE_DIMENSION` are all marked `[Flags]` but hold sequential
// values, not disjoint bits. The consequences are real:
//
//   * `TexturePixel.Float` is 5, which is `Byte | Int` (1 | 4). So
//     `pixel.HasFlag(TexturePixel.Byte)` returns **true for a Float texture**.
//   * `TextureFormat.RGB565` is 7 = `I8 | L8 | R8`; `BGRA32` is 10 = `L8 | R16`.
//   * `D3D10_RESOURCE_DIMENSION.TEXTURE2D` is 3 = `BUFFER | TEXTURE1D`.
//
//   Any `HasFlag` call on these gives nonsense. They are ported as plain enums,
//   which is what the values actually describe. `TextureFlags`, `DDPF`, `DDSD`,
//   `DDSCAPS`, and `DDSCAPS2` are genuine bit flags and become `bitflags!`.
//
// NOT PORTED: `TextureGLFormat` (340 lines), `TextureGLPixelFormat` (156), and
// `TextureGLPixelType` (125) — 621 lines of OpenGL constants with **zero live
// references**. Every use of all three is commented out. When the GL backend is
// revived they should be generated from the `gl` crate's constants rather than
// maintained by hand in two languages.
//
// NOT PORTED: `TextureConvert.Dxt3ToDtx5`. `Dxt3BlockToDtx5Block` reads eight
// bytes into locals and **does nothing with them** — it has no output, no
// return, no writes. It is also wrong three ways over: `a2..a7` all read `p[1]`
// rather than `p[2]..p[7]`, and the caller does `Dxt3BlockToDtx5Block(p += 16)`,
// which advances *before* the first call, so it skips block 0 and reads one
// block past the end on the final iteration. Nothing calls it. A real DXT3->DXT5
// alpha conversion should be written fresh against a test vector.

#![allow(non_camel_case_types)]

use bytemuck::{Pod, Zeroable};
use openstack_polyio::prelude::{BinaryReaderExt, ReadError};
use std::io::{Read, Seek};

bitflags::bitflags! {
    /// C# `[Flags] enum TextureFlags : i32`.
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
    pub struct TextureFlags: i32 {
        const SUGGEST_CLAMPS = 0x1;
        const SUGGEST_CLAMPT = 0x2;
        const SUGGEST_CLAMPU = 0x4;
        const NO_LOD = 0x8;
        const CUBE_TEXTURE = 0x10;
        const VOLUME_TEXTURE = 0x20;
        const TEXTURE_ARRAY = 0x40;
    }
}

bitflags::bitflags! {
    /// C# `[Flags] enum DDPF : u32`.
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
    pub struct DDPF: u32 {
        const ALPHAPIXELS = 0x1;
        const ALPHA = 0x2;
        const FOURCC = 0x4;
        const RGB = 0x40;
        const YUV = 0x200;
        const LUMINANCE = 0x20000;
        const NORMAL = 0x80000000;
    }
}

bitflags::bitflags! {
    /// C# `[Flags] enum DDSD : u32`.
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
    pub struct DDSD: u32 {
        const CAPS = 0x1;
        const HEIGHT = 0x2;
        const WIDTH = 0x4;
        const PITCH = 0x8;
        const PIXELFORMAT = 0x1000;
        const MIPMAPCOUNT = 0x20000;
        const LINEARSIZE = 0x80000;
        const DEPTH = 0x800000;
        const HEADER_FLAGS_TEXTURE = 0x1007;
        const HEADER_FLAGS_MIPMAP = 0x20000;
        const HEADER_FLAGS_VOLUME = 0x800000;
        const HEADER_FLAGS_PITCH = 0x8;
        const HEADER_FLAGS_LINEARSIZE = 0x80000;
    }
}

bitflags::bitflags! {
    /// C# `[Flags] enum DDSCAPS : u32`.
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
    pub struct DDSCAPS: u32 {
        const COMPLEX = 0x8;
        const TEXTURE = 0x1000;
        const MIPMAP = 0x400000;
        const SURFACE_FLAGS_MIPMAP = 0x400008;
        const SURFACE_FLAGS_TEXTURE = 0x1000;
        const SURFACE_FLAGS_CUBEMAP = 0x8;
    }
}

bitflags::bitflags! {
    /// C# `[Flags] enum DDSCAPS2 : u32`.
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
    pub struct DDSCAPS2: u32 {
        const CUBEMAP = 0x200;
        const CUBEMAPPOSITIVEX = 0x400;
        const CUBEMAPNEGATIVEX = 0x800;
        const CUBEMAPPOSITIVEY = 0x1000;
        const CUBEMAPNEGATIVEY = 0x2000;
        const CUBEMAPPOSITIVEZ = 0x4000;
        const CUBEMAPNEGATIVEZ = 0x8000;
        const VOLUME = 0x200000;
        const CUBEMAP_POSITIVEX = 0x600;
        const CUBEMAP_NEGATIVEX = 0xa00;
        const CUBEMAP_POSITIVEY = 0x1200;
        const CUBEMAP_NEGATIVEY = 0x2200;
        const CUBEMAP_POSITIVEZ = 0x4200;
        const CUBEMAP_NEGATIVEZ = 0x8200;
        const CUBEMAP_ALLFACES = 0xfc00;
        const FLAGS_VOLUME = 0x200000;
    }
}

/// C# `[Flags] enum TextureFormat : i32`.
///
/// **The `[Flags]` attribute is wrong here.** The values are sequential, not
/// disjoint bits, so `HasFlag` gives nonsense — see the module header. Ported
/// as a plain enum, which is what the values actually describe.
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum TextureFormat {
    Unknown = 0x0,
    I8 = 0x1,
    L8 = 0x2,
    R8 = 0x3,
    R16 = 0x4,
    RG16 = 0x5,
    RGB24 = 0x6,
    RGB565 = 0x7,
    RGBA32 = 0x8,
    ARGB32 = 0x9,
    BGRA32 = 0xa,
    BGRA1555 = 0xb,
    Compressed = 0x10000000,
    DXT1 = 0x10000064,
    DXT1A = 0x10000065,
    DXT3 = 0x10000066,
    DXT5 = 0x10000067,
    BC4 = 0x10000068,
    BC5 = 0x10000069,
    BC6H = 0x1000006a,
    BC7 = 0x1000006b,
    ETC2 = 0x1000006c,
    ETC2_EAC = 0x1000006d,
}

impl TextureFormat {
    /// Parse from the raw on-disk value. C# cast blindly; unknown values
    /// produced an enum holding an undefined discriminant.
    pub fn from_raw(v: i32) -> Option<Self> {
        Some(match v {
            0x0 => Self::Unknown,
            0x1 => Self::I8,
            0x2 => Self::L8,
            0x3 => Self::R8,
            0x4 => Self::R16,
            0x5 => Self::RG16,
            0x6 => Self::RGB24,
            0x7 => Self::RGB565,
            0x8 => Self::RGBA32,
            0x9 => Self::ARGB32,
            0xa => Self::BGRA32,
            0xb => Self::BGRA1555,
            0x10000000 => Self::Compressed,
            0x10000064 => Self::DXT1,
            0x10000065 => Self::DXT1A,
            0x10000066 => Self::DXT3,
            0x10000067 => Self::DXT5,
            0x10000068 => Self::BC4,
            0x10000069 => Self::BC5,
            0x1000006a => Self::BC6H,
            0x1000006b => Self::BC7,
            0x1000006c => Self::ETC2,
            0x1000006d => Self::ETC2_EAC,
            _ => return None,
        })
    }
}

/// C# `[Flags] enum TexturePixel : i32`.
///
/// **The `[Flags]` attribute is wrong here.** The values are sequential, not
/// disjoint bits, so `HasFlag` gives nonsense — see the module header. Ported
/// as a plain enum, which is what the values actually describe.
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum TexturePixel {
    Unknown = 0x0,
    Byte = 0x1,
    Short = 0x2,
    Int = 0x4,
    Float = 0x5,
    Signed = 0x100,
    Reversed = 0x200,
}

impl TexturePixel {
    /// Parse from the raw on-disk value. C# cast blindly; unknown values
    /// produced an enum holding an undefined discriminant.
    pub fn from_raw(v: i32) -> Option<Self> {
        Some(match v {
            0x0 => Self::Unknown,
            0x1 => Self::Byte,
            0x2 => Self::Short,
            0x4 => Self::Int,
            0x5 => Self::Float,
            0x100 => Self::Signed,
            0x200 => Self::Reversed,
            _ => return None,
        })
    }
}

/// C# `[Flags] enum D3D10_RESOURCE_DIMENSION : u32`.
///
/// **The `[Flags]` attribute is wrong here.** The values are sequential, not
/// disjoint bits, so `HasFlag` gives nonsense — see the module header. Ported
/// as a plain enum, which is what the values actually describe.
#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum D3D10_RESOURCE_DIMENSION {
    UNKNOWN = 0x0,
    BUFFER = 0x1,
    TEXTURE1D = 0x2,
    TEXTURE2D = 0x3,
    TEXTURE3D = 0x4,
}

impl D3D10_RESOURCE_DIMENSION {
    /// Parse from the raw on-disk value. C# cast blindly; unknown values
    /// produced an enum holding an undefined discriminant.
    pub fn from_raw(v: u32) -> Option<Self> {
        Some(match v {
            0x0 => Self::UNKNOWN,
            0x1 => Self::BUFFER,
            0x2 => Self::TEXTURE1D,
            0x3 => Self::TEXTURE2D,
            0x4 => Self::TEXTURE3D,
            _ => return None,
        })
    }
}

/// C# `enum FourCC : u32`.
#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum FourCC {
    NONE = 0x0, // NONE
    DXT1 = 0x31545844, // DXT1
    DXT2 = 0x32545844, // DXT2
    DXT3 = 0x33545844, // DXT3
    DXT4 = 0x34545844, // DXT4
    DXT5 = 0x35545844, // DXT5
    RXGB = 0x42475852, // RXGB
    ATI1 = 0x31495441, // ATI1
    ATI2 = 0x32495441, // ATI2
    A2XY = 0x59583241, // A2XY
    DX10 = 0x30315844, // DX10
}

impl FourCC {
    /// Parse from the raw on-disk value. C# cast blindly; unknown values
    /// produced an enum holding an undefined discriminant.
    pub fn from_raw(v: u32) -> Option<Self> {
        Some(match v {
            0x0 => Self::NONE,
            0x31545844 => Self::DXT1,
            0x32545844 => Self::DXT2,
            0x33545844 => Self::DXT3,
            0x34545844 => Self::DXT4,
            0x35545844 => Self::DXT5,
            0x42475852 => Self::RXGB,
            0x31495441 => Self::ATI1,
            0x32495441 => Self::ATI2,
            0x59583241 => Self::A2XY,
            0x30315844 => Self::DX10,
            _ => return None,
        })
    }
}

/// C# `enum DDS_ALPHA_MODE : u32`.
#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum DDS_ALPHA_MODE {
    ALPHA_MODE_UNKNOWN = 0x0,
    ALPHA_MODE_STRAIGHT = 0x1,
    ALPHA_MODE_PREMULTIPLIED = 0x2,
    ALPHA_MODE_OPAQUE = 0x3,
    ALPHA_MODE_CUSTOM = 0x4,
}

impl DDS_ALPHA_MODE {
    /// Parse from the raw on-disk value. C# cast blindly; unknown values
    /// produced an enum holding an undefined discriminant.
    pub fn from_raw(v: u32) -> Option<Self> {
        Some(match v {
            0x0 => Self::ALPHA_MODE_UNKNOWN,
            0x1 => Self::ALPHA_MODE_STRAIGHT,
            0x2 => Self::ALPHA_MODE_PREMULTIPLIED,
            0x3 => Self::ALPHA_MODE_OPAQUE,
            0x4 => Self::ALPHA_MODE_CUSTOM,
            _ => return None,
        })
    }
}

/// C# `enum DXGI_FORMAT : u32`.
#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum DXGI_FORMAT {
    UNKNOWN = 0x0,
    R32G32B32A32_TYPELESS = 0x1,
    R32G32B32A32_FLOAT = 0x2,
    R32G32B32A32_UINT = 0x3,
    R32G32B32A32_SINT = 0x4,
    R32G32B32_TYPELESS = 0x5,
    R32G32B32_FLOAT = 0x6,
    R32G32B32_UINT = 0x7,
    R32G32B32_SINT = 0x8,
    R16G16B16A16_TYPELESS = 0x9,
    R16G16B16A16_FLOAT = 0xa,
    R16G16B16A16_UNORM = 0xb,
    R16G16B16A16_UINT = 0xc,
    R16G16B16A16_SNORM = 0xd,
    R16G16B16A16_SINT = 0xe,
    R32G32_TYPELESS = 0xf,
    R32G32_FLOAT = 0x10,
    R32G32_UINT = 0x11,
    R32G32_SINT = 0x12,
    R32G8X24_TYPELESS = 0x13,
    D32_FLOAT_S8X24_UINT = 0x14,
    R32_FLOAT_X8X24_TYPELESS = 0x15,
    X32_TYPELESS_G8X24_UINT = 0x16,
    R10G10B10A2_TYPELESS = 0x17,
    R10G10B10A2_UNORM = 0x18,
    R10G10B10A2_UINT = 0x19,
    R11G11B10_FLOAT = 0x1a,
    R8G8B8A8_TYPELESS = 0x1b,
    R8G8B8A8_UNORM = 0x1c,
    R8G8B8A8_UNORM_SRGB = 0x1d,
    R8G8B8A8_UINT = 0x1e,
    R8G8B8A8_SNORM = 0x1f,
    R8G8B8A8_SINT = 0x20,
    R16G16_TYPELESS = 0x21,
    R16G16_FLOAT = 0x22,
    R16G16_UNORM = 0x23,
    R16G16_UINT = 0x24,
    R16G16_SNORM = 0x25,
    R16G16_SINT = 0x26,
    R32_TYPELESS = 0x27,
    D32_FLOAT = 0x28,
    R32_FLOAT = 0x29,
    R32_UINT = 0x2a,
    R32_SINT = 0x2b,
    R24G8_TYPELESS = 0x2c,
    D24_UNORM_S8_UINT = 0x2d,
    R24_UNORM_X8_TYPELESS = 0x2e,
    X24_TYPELESS_G8_UINT = 0x2f,
    R8G8_TYPELESS = 0x30,
    R8G8_UNORM = 0x31,
    R8G8_UINT = 0x32,
    R8G8_SNORM = 0x33,
    R8G8_SINT = 0x34,
    R16_TYPELESS = 0x35,
    R16_FLOAT = 0x36,
    D16_UNORM = 0x37,
    R16_UNORM = 0x38,
    R16_UINT = 0x39,
    R16_SNORM = 0x3a,
    R16_SINT = 0x3b,
    R8_TYPELESS = 0x3c,
    R8_UNORM = 0x3d,
    R8_UINT = 0x3e,
    R8_SNORM = 0x3f,
    R8_SINT = 0x40,
    A8_UNORM = 0x41,
    R1_UNORM = 0x42,
    R9G9B9E5_SHAREDEXP = 0x43,
    R8G8_B8G8_UNORM = 0x44,
    G8R8_G8B8_UNORM = 0x45,
    BC1_TYPELESS = 0x46,
    BC1_UNORM = 0x47,
    BC1_UNORM_SRGB = 0x48,
    BC2_TYPELESS = 0x49,
    BC2_UNORM = 0x4a,
    BC2_UNORM_SRGB = 0x4b,
    BC3_TYPELESS = 0x4c,
    BC3_UNORM = 0x4d,
    BC3_UNORM_SRGB = 0x4e,
    BC4_TYPELESS = 0x4f,
    BC4_UNORM = 0x50,
    BC4_SNORM = 0x51,
    BC5_TYPELESS = 0x52,
    BC5_UNORM = 0x53,
    BC5_SNORM = 0x54,
    B5G6R5_UNORM = 0x55,
    B5G5R5A1_UNORM = 0x56,
    B8G8R8A8_UNORM = 0x57,
    B8G8R8X8_UNORM = 0x58,
    R10G10B10_XR_BIAS_A2_UNORM = 0x59,
    B8G8R8A8_TYPELESS = 0x5a,
    B8G8R8A8_UNORM_SRGB = 0x5b,
    B8G8R8X8_TYPELESS = 0x5c,
    B8G8R8X8_UNORM_SRGB = 0x5d,
    BC6H_TYPELESS = 0x5e,
    BC6H_UF16 = 0x5f,
    BC6H_SF16 = 0x60,
    BC7_TYPELESS = 0x61,
    BC7_UNORM = 0x62,
    BC7_UNORM_SRGB = 0x63,
    AYUV = 0x64,
    Y410 = 0x65,
    Y416 = 0x66,
    NV12 = 0x67,
    P010 = 0x68,
    P016 = 0x69,
    _420_OPAQUE = 0x6a,
    YUY2 = 0x6b,
    Y210 = 0x6c,
    Y216 = 0x6d,
    NV11 = 0x6e,
    AI44 = 0x6f,
    IA44 = 0x70,
    P8 = 0x71,
    A8P8 = 0x72,
    B4G4R4A4_UNORM = 0x73,
    P208 = 0x82,
    V208 = 0x83,
    V408 = 0x84,
    SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 0x85,
    SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 0x86,
}

impl DXGI_FORMAT {
    /// Parse from the raw on-disk value. C# cast blindly; unknown values
    /// produced an enum holding an undefined discriminant.
    pub fn from_raw(v: u32) -> Option<Self> {
        Some(match v {
            0x0 => Self::UNKNOWN,
            0x1 => Self::R32G32B32A32_TYPELESS,
            0x2 => Self::R32G32B32A32_FLOAT,
            0x3 => Self::R32G32B32A32_UINT,
            0x4 => Self::R32G32B32A32_SINT,
            0x5 => Self::R32G32B32_TYPELESS,
            0x6 => Self::R32G32B32_FLOAT,
            0x7 => Self::R32G32B32_UINT,
            0x8 => Self::R32G32B32_SINT,
            0x9 => Self::R16G16B16A16_TYPELESS,
            0xa => Self::R16G16B16A16_FLOAT,
            0xb => Self::R16G16B16A16_UNORM,
            0xc => Self::R16G16B16A16_UINT,
            0xd => Self::R16G16B16A16_SNORM,
            0xe => Self::R16G16B16A16_SINT,
            0xf => Self::R32G32_TYPELESS,
            0x10 => Self::R32G32_FLOAT,
            0x11 => Self::R32G32_UINT,
            0x12 => Self::R32G32_SINT,
            0x13 => Self::R32G8X24_TYPELESS,
            0x14 => Self::D32_FLOAT_S8X24_UINT,
            0x15 => Self::R32_FLOAT_X8X24_TYPELESS,
            0x16 => Self::X32_TYPELESS_G8X24_UINT,
            0x17 => Self::R10G10B10A2_TYPELESS,
            0x18 => Self::R10G10B10A2_UNORM,
            0x19 => Self::R10G10B10A2_UINT,
            0x1a => Self::R11G11B10_FLOAT,
            0x1b => Self::R8G8B8A8_TYPELESS,
            0x1c => Self::R8G8B8A8_UNORM,
            0x1d => Self::R8G8B8A8_UNORM_SRGB,
            0x1e => Self::R8G8B8A8_UINT,
            0x1f => Self::R8G8B8A8_SNORM,
            0x20 => Self::R8G8B8A8_SINT,
            0x21 => Self::R16G16_TYPELESS,
            0x22 => Self::R16G16_FLOAT,
            0x23 => Self::R16G16_UNORM,
            0x24 => Self::R16G16_UINT,
            0x25 => Self::R16G16_SNORM,
            0x26 => Self::R16G16_SINT,
            0x27 => Self::R32_TYPELESS,
            0x28 => Self::D32_FLOAT,
            0x29 => Self::R32_FLOAT,
            0x2a => Self::R32_UINT,
            0x2b => Self::R32_SINT,
            0x2c => Self::R24G8_TYPELESS,
            0x2d => Self::D24_UNORM_S8_UINT,
            0x2e => Self::R24_UNORM_X8_TYPELESS,
            0x2f => Self::X24_TYPELESS_G8_UINT,
            0x30 => Self::R8G8_TYPELESS,
            0x31 => Self::R8G8_UNORM,
            0x32 => Self::R8G8_UINT,
            0x33 => Self::R8G8_SNORM,
            0x34 => Self::R8G8_SINT,
            0x35 => Self::R16_TYPELESS,
            0x36 => Self::R16_FLOAT,
            0x37 => Self::D16_UNORM,
            0x38 => Self::R16_UNORM,
            0x39 => Self::R16_UINT,
            0x3a => Self::R16_SNORM,
            0x3b => Self::R16_SINT,
            0x3c => Self::R8_TYPELESS,
            0x3d => Self::R8_UNORM,
            0x3e => Self::R8_UINT,
            0x3f => Self::R8_SNORM,
            0x40 => Self::R8_SINT,
            0x41 => Self::A8_UNORM,
            0x42 => Self::R1_UNORM,
            0x43 => Self::R9G9B9E5_SHAREDEXP,
            0x44 => Self::R8G8_B8G8_UNORM,
            0x45 => Self::G8R8_G8B8_UNORM,
            0x46 => Self::BC1_TYPELESS,
            0x47 => Self::BC1_UNORM,
            0x48 => Self::BC1_UNORM_SRGB,
            0x49 => Self::BC2_TYPELESS,
            0x4a => Self::BC2_UNORM,
            0x4b => Self::BC2_UNORM_SRGB,
            0x4c => Self::BC3_TYPELESS,
            0x4d => Self::BC3_UNORM,
            0x4e => Self::BC3_UNORM_SRGB,
            0x4f => Self::BC4_TYPELESS,
            0x50 => Self::BC4_UNORM,
            0x51 => Self::BC4_SNORM,
            0x52 => Self::BC5_TYPELESS,
            0x53 => Self::BC5_UNORM,
            0x54 => Self::BC5_SNORM,
            0x55 => Self::B5G6R5_UNORM,
            0x56 => Self::B5G5R5A1_UNORM,
            0x57 => Self::B8G8R8A8_UNORM,
            0x58 => Self::B8G8R8X8_UNORM,
            0x59 => Self::R10G10B10_XR_BIAS_A2_UNORM,
            0x5a => Self::B8G8R8A8_TYPELESS,
            0x5b => Self::B8G8R8A8_UNORM_SRGB,
            0x5c => Self::B8G8R8X8_TYPELESS,
            0x5d => Self::B8G8R8X8_UNORM_SRGB,
            0x5e => Self::BC6H_TYPELESS,
            0x5f => Self::BC6H_UF16,
            0x60 => Self::BC6H_SF16,
            0x61 => Self::BC7_TYPELESS,
            0x62 => Self::BC7_UNORM,
            0x63 => Self::BC7_UNORM_SRGB,
            0x64 => Self::AYUV,
            0x65 => Self::Y410,
            0x66 => Self::Y416,
            0x67 => Self::NV12,
            0x68 => Self::P010,
            0x69 => Self::P016,
            0x6a => Self::_420_OPAQUE,
            0x6b => Self::YUY2,
            0x6c => Self::Y210,
            0x6d => Self::Y216,
            0x6e => Self::NV11,
            0x6f => Self::AI44,
            0x70 => Self::IA44,
            0x71 => Self::P8,
            0x72 => Self::A8P8,
            0x73 => Self::B4G4R4A4_UNORM,
            0x82 => Self::P208,
            0x83 => Self::V208,
            0x84 => Self::V408,
            0x85 => Self::SAMPLER_FEEDBACK_MIN_MIP_OPAQUE,
            0x86 => Self::SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE,
            _ => return None,
        })
    }
}

/// C# `enum TextureUnityFormat : i16`.
#[repr(i16)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum TextureUnityFormat {
    Unknown = 0x0,
    ATC_RGB4 = -0x7f,
    // ATC_RGBA8 = -0x7f, // duplicate of an earlier variant
    // PVRTC_2BPP_RGB = -0x7f, // duplicate of an earlier variant
    // PVRTC_2BPP_RGBA = -0x7f, // duplicate of an earlier variant
    // PVRTC_4BPP_RGB = -0x7f, // duplicate of an earlier variant
    // PVRTC_4BPP_RGBA = -0x7f, // duplicate of an earlier variant
    Alpha8 = 0x1,
    ARGB4444 = 0x2,
    RGB24 = 0x3,
    RGBA32 = 0x4,
    ARGB32 = 0x5,
    RGB565 = 0x7,
    R16 = 0x9,
    DXT1 = 0xa,
    DXT3_POLYFILL = 0xb,
    DXT5 = 0xc,
    RGBA4444 = 0xd,
    BGRA32 = 0xe,
    RHalf = 0xf,
    RGHalf = 0x10,
    RGBAHalf = 0x11,
    RFloat = 0x12,
    RGFloat = 0x13,
    RGBAFloat = 0x14,
    YUY2 = 0x15,
    RGB9e5Float = 0x16,
    BC6H = 0x18,
    BC7 = 0x19,
    BC4 = 0x1a,
    BC5 = 0x1b,
    DXT1Crunched = 0x1c,
    DXT5Crunched = 0x1d,
    PVRTC_RGB2 = 0x1e,
    PVRTC_RGBA2 = 0x1f,
    PVRTC_RGB4 = 0x20,
    PVRTC_RGBA4 = 0x21,
    ETC_RGB4 = 0x22,
    EAC_R = 0x29,
    EAC_R_SIGNED = 0x2a,
    EAC_RG = 0x2b,
    EAC_RG_SIGNED = 0x2c,
    ETC2_RGB = 0x2d,
    ETC2_RGBA1 = 0x2e,
    ETC2_RGBA8 = 0x2f,
    ASTC_4x4 = 0x30,
    // ASTC_RGB_4x4 = 0x30, // duplicate of an earlier variant
    ASTC_5x5 = 0x31,
    // ASTC_RGB_5x5 = 0x31, // duplicate of an earlier variant
    ASTC_6x6 = 0x32,
    // ASTC_RGB_6x6 = 0x32, // duplicate of an earlier variant
    ASTC_8x8 = 0x33,
    // ASTC_RGB_8x8 = 0x33, // duplicate of an earlier variant
    ASTC_10x10 = 0x34,
    // ASTC_RGB_10x10 = 0x34, // duplicate of an earlier variant
    ASTC_12x12 = 0x35,
    // ASTC_RGB_12x12 = 0x35, // duplicate of an earlier variant
    ASTC_RGBA_4x4 = 0x36,
    ASTC_RGBA_5x5 = 0x37,
    ASTC_RGBA_6x6 = 0x38,
    ASTC_RGBA_8x8 = 0x39,
    ASTC_RGBA_10x10 = 0x3a,
    ASTC_RGBA_12x12 = 0x3b,
    ETC_RGB4_3DS = 0x3c,
    ETC_RGBA8_3DS = 0x3d,
    RG16 = 0x3e,
    R8 = 0x3f,
    ETC_RGB4Crunched = 0x40,
    ETC2_RGBA8Crunched = 0x41,
    ASTC_HDR_4x4 = 0x42,
    ASTC_HDR_5x5 = 0x43,
    ASTC_HDR_6x6 = 0x44,
    ASTC_HDR_8x8 = 0x45,
    ASTC_HDR_10x10 = 0x46,
    ASTC_HDR_12x12 = 0x47,
}

impl TextureUnityFormat {
    /// Parse from the raw on-disk value. C# cast blindly; unknown values
    /// produced an enum holding an undefined discriminant.
    pub fn from_raw(v: i16) -> Option<Self> {
        Some(match v {
            0x0 => Self::Unknown,
            -0x7f => Self::ATC_RGB4,
            0x1 => Self::Alpha8,
            0x2 => Self::ARGB4444,
            0x3 => Self::RGB24,
            0x4 => Self::RGBA32,
            0x5 => Self::ARGB32,
            0x7 => Self::RGB565,
            0x9 => Self::R16,
            0xa => Self::DXT1,
            0xb => Self::DXT3_POLYFILL,
            0xc => Self::DXT5,
            0xd => Self::RGBA4444,
            0xe => Self::BGRA32,
            0xf => Self::RHalf,
            0x10 => Self::RGHalf,
            0x11 => Self::RGBAHalf,
            0x12 => Self::RFloat,
            0x13 => Self::RGFloat,
            0x14 => Self::RGBAFloat,
            0x15 => Self::YUY2,
            0x16 => Self::RGB9e5Float,
            0x18 => Self::BC6H,
            0x19 => Self::BC7,
            0x1a => Self::BC4,
            0x1b => Self::BC5,
            0x1c => Self::DXT1Crunched,
            0x1d => Self::DXT5Crunched,
            0x1e => Self::PVRTC_RGB2,
            0x1f => Self::PVRTC_RGBA2,
            0x20 => Self::PVRTC_RGB4,
            0x21 => Self::PVRTC_RGBA4,
            0x22 => Self::ETC_RGB4,
            0x29 => Self::EAC_R,
            0x2a => Self::EAC_R_SIGNED,
            0x2b => Self::EAC_RG,
            0x2c => Self::EAC_RG_SIGNED,
            0x2d => Self::ETC2_RGB,
            0x2e => Self::ETC2_RGBA1,
            0x2f => Self::ETC2_RGBA8,
            0x30 => Self::ASTC_4x4,
            0x31 => Self::ASTC_5x5,
            0x32 => Self::ASTC_6x6,
            0x33 => Self::ASTC_8x8,
            0x34 => Self::ASTC_10x10,
            0x35 => Self::ASTC_12x12,
            0x36 => Self::ASTC_RGBA_4x4,
            0x37 => Self::ASTC_RGBA_5x5,
            0x38 => Self::ASTC_RGBA_6x6,
            0x39 => Self::ASTC_RGBA_8x8,
            0x3a => Self::ASTC_RGBA_10x10,
            0x3b => Self::ASTC_RGBA_12x12,
            0x3c => Self::ETC_RGB4_3DS,
            0x3d => Self::ETC_RGBA8_3DS,
            0x3e => Self::RG16,
            0x3f => Self::R8,
            0x40 => Self::ETC_RGB4Crunched,
            0x41 => Self::ETC2_RGBA8Crunched,
            0x42 => Self::ASTC_HDR_4x4,
            0x43 => Self::ASTC_HDR_5x5,
            0x44 => Self::ASTC_HDR_6x6,
            0x45 => Self::ASTC_HDR_8x8,
            0x46 => Self::ASTC_HDR_10x10,
            0x47 => Self::ASTC_HDR_12x12,
            _ => return None,
        })
    }
}

/// C# `enum TextureUnrealFormat : i32`.
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum TextureUnrealFormat {
    Unknown = 0x0,
    A32B32G32R32F = 0x1,
    B8G8R8A8 = 0x2,
    G8 = 0x3,
    G16 = 0x4,
    DXT1 = 0x5,
    DXT3 = 0x6,
    DXT5 = 0x7,
    UYVY = 0x8,
    FloatRGB = 0x9,
    FloatRGBA = 0xa,
    DepthStencil = 0xb,
    ShadowDepth = 0xc,
    R32Float = 0xd,
    G16R16 = 0xe,
    G16R16F = 0xf,
    G16R16FFilter = 0x10,
    G32R32F = 0x11,
    A2B10G10R10 = 0x12,
    A16B16G16R16 = 0x13,
    D24 = 0x14,
    R16F = 0x15,
    R16FFilter = 0x16,
    BC5 = 0x17,
    V8U8 = 0x18,
    A1 = 0x19,
    FloatR11G11B10 = 0x1a,
    A8 = 0x1b,
    R32UInt = 0x1c,
    R32SInt = 0x1d,
    PVRTC2 = 0x1e,
    PVRTC4 = 0x1f,
    R16UInt = 0x20,
    R16SInt = 0x21,
    R16G16B16A16UInt = 0x22,
    R16G16B16A16SInt = 0x23,
    R5G6B5UNorm = 0x24,
    R8G8B8A8 = 0x25,
    A8R8G8B8 = 0x26,
    BC4 = 0x27,
    R8G8 = 0x28,
    ATCRGB = 0x29,
    ATCRGBAE = 0x2a,
    ATCRGBAI = 0x2b,
    X24G8 = 0x2c,
    ETC1 = 0x2d,
    ETC2RGB = 0x2e,
    ETC2RGBA = 0x2f,
    R32G32B32A32UInt = 0x30,
    R16G16UInt = 0x31,
    ASTC4x4 = 0x32,
    ASTC6x6 = 0x33,
    ASTC8x8 = 0x34,
    ASTC10x10 = 0x35,
    ASTC12x12 = 0x36,
    BC6H = 0x37,
    BC7 = 0x38,
    R8UInt = 0x39,
    L8 = 0x3a,
    XGXR8 = 0x3b,
    R8G8B8A8UInt = 0x3c,
    R8G8B8A8SNorm = 0x3d,
    R16G16B16A16UNorm = 0x3e,
    R16G16B16A16SNorm = 0x3f,
    PLATFORMHDR0 = 0x40,
    PLATFORMHDR1 = 0x41,
    PLATFORMHDR2 = 0x42,
    NV12 = 0x43,
    R32G32UInt = 0x44,
    ETC2R11EAC = 0x45,
    ETC2RG11EAC = 0x46,
    R8 = 0x47,
    B5G5R5A1UNorm = 0x48,
    ASTC4x4HDR = 0x49,
    ASTC6x6HDR = 0x4a,
    ASTC8x8HDR = 0x4b,
    ASTC10x10HDR = 0x4c,
    ASTC12x12HDR = 0x4d,
    G16R16SNorm = 0x4e,
    R8G8UInt = 0x4f,
    R32G32B32UInt = 0x50,
    R32G32B32SInt = 0x51,
    R32G32B32F = 0x52,
    R8SInt = 0x53,
    R64UInt = 0x54,
    R9G9B9EXP5 = 0x55,
    P010 = 0x56,
    MAX = 0x57,
}

impl TextureUnrealFormat {
    /// Parse from the raw on-disk value. C# cast blindly; unknown values
    /// produced an enum holding an undefined discriminant.
    pub fn from_raw(v: i32) -> Option<Self> {
        Some(match v {
            0x0 => Self::Unknown,
            0x1 => Self::A32B32G32R32F,
            0x2 => Self::B8G8R8A8,
            0x3 => Self::G8,
            0x4 => Self::G16,
            0x5 => Self::DXT1,
            0x6 => Self::DXT3,
            0x7 => Self::DXT5,
            0x8 => Self::UYVY,
            0x9 => Self::FloatRGB,
            0xa => Self::FloatRGBA,
            0xb => Self::DepthStencil,
            0xc => Self::ShadowDepth,
            0xd => Self::R32Float,
            0xe => Self::G16R16,
            0xf => Self::G16R16F,
            0x10 => Self::G16R16FFilter,
            0x11 => Self::G32R32F,
            0x12 => Self::A2B10G10R10,
            0x13 => Self::A16B16G16R16,
            0x14 => Self::D24,
            0x15 => Self::R16F,
            0x16 => Self::R16FFilter,
            0x17 => Self::BC5,
            0x18 => Self::V8U8,
            0x19 => Self::A1,
            0x1a => Self::FloatR11G11B10,
            0x1b => Self::A8,
            0x1c => Self::R32UInt,
            0x1d => Self::R32SInt,
            0x1e => Self::PVRTC2,
            0x1f => Self::PVRTC4,
            0x20 => Self::R16UInt,
            0x21 => Self::R16SInt,
            0x22 => Self::R16G16B16A16UInt,
            0x23 => Self::R16G16B16A16SInt,
            0x24 => Self::R5G6B5UNorm,
            0x25 => Self::R8G8B8A8,
            0x26 => Self::A8R8G8B8,
            0x27 => Self::BC4,
            0x28 => Self::R8G8,
            0x29 => Self::ATCRGB,
            0x2a => Self::ATCRGBAE,
            0x2b => Self::ATCRGBAI,
            0x2c => Self::X24G8,
            0x2d => Self::ETC1,
            0x2e => Self::ETC2RGB,
            0x2f => Self::ETC2RGBA,
            0x30 => Self::R32G32B32A32UInt,
            0x31 => Self::R16G16UInt,
            0x32 => Self::ASTC4x4,
            0x33 => Self::ASTC6x6,
            0x34 => Self::ASTC8x8,
            0x35 => Self::ASTC10x10,
            0x36 => Self::ASTC12x12,
            0x37 => Self::BC6H,
            0x38 => Self::BC7,
            0x39 => Self::R8UInt,
            0x3a => Self::L8,
            0x3b => Self::XGXR8,
            0x3c => Self::R8G8B8A8UInt,
            0x3d => Self::R8G8B8A8SNorm,
            0x3e => Self::R16G16B16A16UNorm,
            0x3f => Self::R16G16B16A16SNorm,
            0x40 => Self::PLATFORMHDR0,
            0x41 => Self::PLATFORMHDR1,
            0x42 => Self::PLATFORMHDR2,
            0x43 => Self::NV12,
            0x44 => Self::R32G32UInt,
            0x45 => Self::ETC2R11EAC,
            0x46 => Self::ETC2RG11EAC,
            0x47 => Self::R8,
            0x48 => Self::B5G5R5A1UNorm,
            0x49 => Self::ASTC4x4HDR,
            0x4a => Self::ASTC6x6HDR,
            0x4b => Self::ASTC8x8HDR,
            0x4c => Self::ASTC10x10HDR,
            0x4d => Self::ASTC12x12HDR,
            0x4e => Self::G16R16SNorm,
            0x4f => Self::R8G8UInt,
            0x50 => Self::R32G32B32UInt,
            0x51 => Self::R32G32B32SInt,
            0x52 => Self::R32G32B32F,
            0x53 => Self::R8SInt,
            0x54 => Self::R64UInt,
            0x55 => Self::R9G9B9EXP5,
            0x56 => Self::P010,
            0x57 => Self::MAX,
            _ => return None,
        })
    }
}

// ---------------------------------------------------------------------------
// DDS container
// ---------------------------------------------------------------------------

/// C# `struct DDS_PIXELFORMAT`. 32 bytes on disk.
///
/// Stored with raw integer fields rather than the C#'s `[MarshalAs]` enum
/// fields: an unknown `dwFourCC` on disk would produce an enum holding an
/// undefined discriminant, which is UB in Rust. Use the accessors to decode.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Pod, Zeroable)]
pub struct DdsPixelFormat {
    pub dw_size: u32,
    pub dw_flags: u32,
    pub dw_four_cc: u32,
    pub dw_rgb_bit_count: u32,
    pub dw_r_bit_mask: u32,
    pub dw_g_bit_mask: u32,
    pub dw_b_bit_mask: u32,
    pub dw_a_bit_mask: u32,
}

impl DdsPixelFormat {
    /// C# `DDS_PIXELFORMAT.SizeOf`.
    pub const SIZE_OF: usize = 32;

    #[inline]
    pub fn flags(&self) -> DDPF {
        DDPF::from_bits_truncate(self.dw_flags)
    }

    /// `None` when the on-disk code is not one this build knows.
    #[inline]
    pub fn four_cc(&self) -> Option<FourCC> {
        FourCC::from_raw(self.dw_four_cc)
    }

    pub fn read<R: Read + Seek>(r: &mut R) -> Result<Self, ReadError> {
        Ok(Self {
            dw_size: r.read_u32()?,
            dw_flags: r.read_u32()?,
            dw_four_cc: r.read_u32()?,
            dw_rgb_bit_count: r.read_u32()?,
            dw_r_bit_mask: r.read_u32()?,
            dw_g_bit_mask: r.read_u32()?,
            dw_b_bit_mask: r.read_u32()?,
            dw_a_bit_mask: r.read_u32()?,
        })
    }
}

/// C# `struct DDS_HEADER_DXT10`. 20 bytes, present only when the FourCC is DX10.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Pod, Zeroable)]
pub struct DdsHeaderDxt10 {
    pub dxgi_format: u32,
    pub resource_dimension: u32,
    pub misc_flag: u32,
    pub array_size: u32,
    pub misc_flags2: u32,
}

impl DdsHeaderDxt10 {
    pub const SIZE_OF: usize = 20;

    #[inline]
    pub fn format(&self) -> Option<DXGI_FORMAT> {
        DXGI_FORMAT::from_raw(self.dxgi_format)
    }

    #[inline]
    pub fn dimension(&self) -> Option<D3D10_RESOURCE_DIMENSION> {
        D3D10_RESOURCE_DIMENSION::from_raw(self.resource_dimension)
    }

    pub fn read<R: Read + Seek>(r: &mut R) -> Result<Self, ReadError> {
        Ok(Self {
            dxgi_format: r.read_u32()?,
            resource_dimension: r.read_u32()?,
            misc_flag: r.read_u32()?,
            array_size: r.read_u32()?,
            misc_flags2: r.read_u32()?,
        })
    }
}

/// C# `unsafe struct DDS_HEADER`. 124 bytes on disk.
///
/// The C# uses `fixed uint dwReserved1[11]`, which requires `unsafe` to touch.
/// A plain `[u32; 11]` here is safe and still `Pod`.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Pod, Zeroable)]
pub struct DdsHeader {
    pub dw_size: u32,
    pub dw_flags: u32,
    pub dw_height: u32,
    pub dw_width: u32,
    pub dw_pitch_or_linear_size: u32,
    /// Meaningful only when `DDSD::DEPTH` is set.
    pub dw_depth: u32,
    pub dw_mip_map_count: u32,
    pub dw_reserved1: [u32; 11],
    pub ddspf: DdsPixelFormat,
    pub dw_caps: u32,
    pub dw_caps2: u32,
    pub dw_caps3: u32,
    pub dw_caps4: u32,
    pub dw_reserved2: u32,
}

impl Default for DdsHeader {
    fn default() -> Self {
        Zeroable::zeroed()
    }
}

/// C# `FormatException` throws from `Verify`.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum DdsError {
    BadMagic(u32),
    BadHeaderSize(u32),
    MissingDimensionFlags(u32),
    NotATexture(u32),
    BadPixelFormatSize(u32),
    /// The header describes a format this build cannot decode.
    UnsupportedFormat(u32),
}

impl std::fmt::Display for DdsError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            DdsError::BadMagic(v) => write!(f, "invalid DDS magic: {v:#x}"),
            DdsError::BadHeaderSize(v) => write!(f, "invalid DDS header size: {v}"),
            DdsError::MissingDimensionFlags(v) => {
                write!(f, "DDS flags lack HEIGHT|WIDTH: {v:#x}")
            }
            DdsError::NotATexture(v) => write!(f, "DDS caps lack TEXTURE: {v:#x}"),
            DdsError::BadPixelFormatSize(v) => {
                write!(f, "invalid DDS pixel format size: {v}")
            }
            DdsError::UnsupportedFormat(v) => write!(f, "unsupported DDS format: {v:#x}"),
        }
    }
}

impl std::error::Error for DdsError {}

impl DdsHeader {
    /// C# `DDS_HEADER.MAGIC` — "DDS ".
    pub const MAGIC: u32 = 0x2053_4444;
    /// C# `DDS_HEADER.SizeOf`.
    pub const SIZE_OF: usize = 124;

    #[inline]
    pub fn flags(&self) -> DDSD {
        DDSD::from_bits_truncate(self.dw_flags)
    }

    #[inline]
    pub fn caps(&self) -> DDSCAPS {
        DDSCAPS::from_bits_truncate(self.dw_caps)
    }

    #[inline]
    pub fn caps2(&self) -> DDSCAPS2 {
        DDSCAPS2::from_bits_truncate(self.dw_caps2)
    }

    /// C# `Verify()`.
    pub fn verify(&self) -> Result<(), DdsError> {
        if self.dw_size != 124 {
            return Err(DdsError::BadHeaderSize(self.dw_size));
        }
        if !self.flags().contains(DDSD::HEIGHT | DDSD::WIDTH) {
            return Err(DdsError::MissingDimensionFlags(self.dw_flags));
        }
        if !self.caps().contains(DDSCAPS::TEXTURE) {
            return Err(DdsError::NotATexture(self.dw_caps));
        }
        if self.ddspf.dw_size != 32 {
            return Err(DdsError::BadPixelFormatSize(self.ddspf.dw_size));
        }
        Ok(())
    }

    /// Mip level count, treating an unset/zero field as a single level — the
    /// C# returned the raw 0 and left callers to loop zero times.
    #[inline]
    pub fn mip_map_count(&self) -> u32 {
        if self.flags().contains(DDSD::MIPMAPCOUNT) {
            self.dw_mip_map_count.max(1)
        } else {
            1
        }
    }

    /// Depth, treating an unset flag as 1 rather than the raw field.
    #[inline]
    pub fn depth(&self) -> u32 {
        if self.flags().contains(DDSD::DEPTH) {
            self.dw_depth.max(1)
        } else {
            1
        }
    }

    #[inline]
    pub fn is_cubemap(&self) -> bool {
        self.caps2().contains(DDSCAPS2::CUBEMAP)
    }

    /// C# `DDS_HEADER.Read(BinaryReader r, bool readMagic = true)`.
    ///
    /// Returns the header and, when the FourCC is DX10, the extension header.
    /// The C# also returned a `(object type, int blockSize, object value)`
    /// tuple; that mapping is `block_format()` below, typed rather than boxed.
    pub fn read<R: Read + Seek>(
        r: &mut R,
        read_magic: bool,
    ) -> Result<(Self, Option<DdsHeaderDxt10>), DdsError> {
        if read_magic {
            let magic = r.read_u32().map_err(|_| DdsError::BadMagic(0))?;
            if magic != Self::MAGIC {
                return Err(DdsError::BadMagic(magic));
            }
        }
        let read_u32 = |r: &mut R| r.read_u32().map_err(|_| DdsError::BadHeaderSize(0));
        let mut h = Self {
            dw_size: read_u32(r)?,
            dw_flags: read_u32(r)?,
            dw_height: read_u32(r)?,
            dw_width: read_u32(r)?,
            dw_pitch_or_linear_size: read_u32(r)?,
            dw_depth: read_u32(r)?,
            dw_mip_map_count: read_u32(r)?,
            dw_reserved1: [0; 11],
            ddspf: DdsPixelFormat::default(),
            dw_caps: 0,
            dw_caps2: 0,
            dw_caps3: 0,
            dw_caps4: 0,
            dw_reserved2: 0,
        };
        for slot in h.dw_reserved1.iter_mut() {
            *slot = read_u32(r)?;
        }
        h.ddspf = DdsPixelFormat::read(r).map_err(|_| DdsError::BadPixelFormatSize(0))?;
        h.dw_caps = read_u32(r)?;
        h.dw_caps2 = read_u32(r)?;
        h.dw_caps3 = read_u32(r)?;
        h.dw_caps4 = read_u32(r)?;
        h.dw_reserved2 = read_u32(r)?;
        h.verify()?;

        let dxt10 = if h.ddspf.four_cc() == Some(FourCC::DX10) {
            Some(DdsHeaderDxt10::read(r).map_err(|_| DdsError::BadHeaderSize(0))?)
        } else {
            None
        };
        Ok((h, dxt10))
    }

    /// C# `DDS_HEADER.Read(BinaryReader r, bool readMagic = true)` in full —
    /// header, optional DX10 extension, decoded format, and the remaining
    /// payload bytes.
    ///
    /// The C# returns a 4-tuple `(header, headerDxt10, format, bytes)`; this
    /// matches it. `read` above is the header-only form for callers that want
    /// to stream the payload rather than buffer it.
    pub fn read_full<R: Read + Seek>(
        r: &mut R,
        read_magic: bool,
    ) -> Result<(Self, Option<DdsHeaderDxt10>, TextureFormat, Vec<u8>), DdsError> {
        let (h, dxt10) = Self::read(r, read_magic)?;
        let (format, _block) = h.block_format()?;
        let mut bytes = Vec::new();
        r.read_to_end(&mut bytes)
            .map_err(|_| DdsError::BadHeaderSize(0))?;
        Ok((h, dxt10, format, bytes))
    }

    /// C# `DDS_HEADER.Write(BinaryWriter w, DDS_HEADER h, DDS_HEADER_DXT10? dxt10, byte[] bytes)`.
    ///
    /// Emits magic, the 124-byte header, the 20-byte DX10 extension when
    /// present, then the payload. Round-trips with [`read_full`](Self::read_full).
    pub fn write<W: Write>(
        &self,
        w: &mut W,
        dxt10: Option<&DdsHeaderDxt10>,
        bytes: &[u8],
    ) -> io::Result<()> {
        w.write_all(&Self::MAGIC.to_le_bytes())?;
        for v in [
            self.dw_size,
            self.dw_flags,
            self.dw_height,
            self.dw_width,
            self.dw_pitch_or_linear_size,
            self.dw_depth,
            self.dw_mip_map_count,
        ] {
            w.write_all(&v.to_le_bytes())?;
        }
        for v in self.dw_reserved1 {
            w.write_all(&v.to_le_bytes())?;
        }
        for v in [
            self.ddspf.dw_size,
            self.ddspf.dw_flags,
            self.ddspf.dw_four_cc,
            self.ddspf.dw_rgb_bit_count,
            self.ddspf.dw_r_bit_mask,
            self.ddspf.dw_g_bit_mask,
            self.ddspf.dw_b_bit_mask,
            self.ddspf.dw_a_bit_mask,
        ] {
            w.write_all(&v.to_le_bytes())?;
        }
        for v in [
            self.dw_caps,
            self.dw_caps2,
            self.dw_caps3,
            self.dw_caps4,
            self.dw_reserved2,
        ] {
            w.write_all(&v.to_le_bytes())?;
        }
        if let Some(d) = dxt10 {
            for v in [
                d.dxgi_format,
                d.resource_dimension,
                d.misc_flag,
                d.array_size,
                d.misc_flags2,
            ] {
                w.write_all(&v.to_le_bytes())?;
            }
        }
        w.write_all(bytes)
    }

    /// The C#'s FourCC -> (format, block size) switch, typed.
    ///
    /// Returns bytes per 4x4 block for compressed formats, bytes per pixel for
    /// uncompressed ones — the same convention `texture_helper::BlockFormat`
    /// uses.
    pub fn block_format(&self) -> Result<(TextureFormat, u32), DdsError> {
        match self.ddspf.four_cc() {
            Some(FourCC::DXT1) => Ok((TextureFormat::DXT1, 8)),
            // DXT2 is DXT3 with premultiplied alpha; same block layout.
            Some(FourCC::DXT2 | FourCC::DXT3) => Ok((TextureFormat::DXT3, 16)),
            // DXT4 is DXT5 with premultiplied alpha; same block layout.
            Some(FourCC::DXT4 | FourCC::DXT5) => Ok((TextureFormat::DXT5, 16)),
            // ATI1/ATI2 are the original FourCCs for what D3D10 renamed BC4/BC5.
            Some(FourCC::ATI1) => Ok((TextureFormat::BC4, 8)),
            Some(FourCC::ATI2 | FourCC::A2XY) => Ok((TextureFormat::BC5, 16)),
            // RXGB is DXT5 with the red and alpha channels swapped (Doom 3
            // normal maps); the block layout is unchanged, so the swap is the
            // decoder's business, not the header's.
            Some(FourCC::RXGB) => Ok((TextureFormat::DXT5, 16)),
            // NONE means an uncompressed format described by the bit masks.
            Some(FourCC::NONE) | None if self.ddspf.dw_four_cc == 0 => self.uncompressed_format(),
            _ => Err(DdsError::UnsupportedFormat(self.ddspf.dw_four_cc)),
        }
    }

    /// C# `MakeFormat(ref ddspf)` — decode an uncompressed layout from the
    /// channel bit masks.
    fn uncompressed_format(&self) -> Result<(TextureFormat, u32), DdsError> {
        let p = &self.ddspf;
        let bpp = p.dw_rgb_bit_count / 8;
        let f = match (p.dw_rgb_bit_count, p.dw_r_bit_mask, p.dw_g_bit_mask, p.dw_b_bit_mask, p.dw_a_bit_mask) {
            (32, 0x00ff_0000, 0x0000_ff00, 0x0000_00ff, 0xff00_0000) => TextureFormat::ARGB32,
            (32, 0x0000_00ff, 0x0000_ff00, 0x00ff_0000, 0xff00_0000) => TextureFormat::RGBA32,
            (32, 0x00ff_0000, 0x0000_ff00, 0x0000_00ff, 0) => TextureFormat::BGRA32,
            (24, 0x00ff_0000, 0x0000_ff00, 0x0000_00ff, 0) => TextureFormat::RGB24,
            (16, 0xf800, 0x07e0, 0x001f, 0) => TextureFormat::RGB565,
            (16, 0x7c00, 0x03e0, 0x001f, 0x8000) => TextureFormat::BGRA1555,
            (8, _, _, _, _) => TextureFormat::L8,
            _ => return Err(DdsError::UnsupportedFormat(p.dw_rgb_bit_count)),
        };
        Ok((f, bpp.max(1)))
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    fn valid_header_bytes() -> Vec<u8> {
        let mut v = Vec::new();
        v.extend_from_slice(&DdsHeader::MAGIC.to_le_bytes());
        v.extend_from_slice(&124u32.to_le_bytes()); // dwSize
        let flags = DDSD::CAPS | DDSD::HEIGHT | DDSD::WIDTH | DDSD::PIXELFORMAT;
        v.extend_from_slice(&flags.bits().to_le_bytes());
        v.extend_from_slice(&64u32.to_le_bytes()); // height
        v.extend_from_slice(&32u32.to_le_bytes()); // width
        v.extend_from_slice(&0u32.to_le_bytes()); // pitch
        v.extend_from_slice(&0u32.to_le_bytes()); // depth
        v.extend_from_slice(&0u32.to_le_bytes()); // mipcount
        v.extend_from_slice(&[0u8; 44]); // reserved1[11]
        // ddspf
        v.extend_from_slice(&32u32.to_le_bytes()); // size
        v.extend_from_slice(&DDPF::FOURCC.bits().to_le_bytes());
        v.extend_from_slice(&(FourCC::DXT1 as u32).to_le_bytes());
        v.extend_from_slice(&[0u8; 20]); // bitcount + 4 masks
        v.extend_from_slice(&DDSCAPS::TEXTURE.bits().to_le_bytes());
        v.extend_from_slice(&[0u8; 16]); // caps2..reserved2
        v
    }

    #[test]
    fn on_disk_sizes_match_the_spec() {
        assert_eq!(std::mem::size_of::<DdsHeader>(), DdsHeader::SIZE_OF);
        assert_eq!(std::mem::size_of::<DdsPixelFormat>(), DdsPixelFormat::SIZE_OF);
        assert_eq!(std::mem::size_of::<DdsHeaderDxt10>(), DdsHeaderDxt10::SIZE_OF);
    }

    #[test]
    fn reads_a_valid_dxt1_header() {
        let mut c = Cursor::new(valid_header_bytes());
        let (h, dxt10) = DdsHeader::read(&mut c, true).unwrap();
        assert_eq!((h.dw_width, h.dw_height), (32, 64));
        assert!(dxt10.is_none());
        assert_eq!(h.block_format().unwrap(), (TextureFormat::DXT1, 8));
    }

    #[test]
    fn bad_magic_is_rejected() {
        let mut bytes = valid_header_bytes();
        bytes[0] = 0xFF;
        let mut c = Cursor::new(bytes);
        assert!(matches!(
            DdsHeader::read(&mut c, true),
            Err(DdsError::BadMagic(_))
        ));
    }

    #[test]
    fn verify_catches_each_malformed_field() {
        let mut h = DdsHeader { dw_size: 100, ..Default::default() };
        assert!(matches!(h.verify(), Err(DdsError::BadHeaderSize(100))));
        h.dw_size = 124;
        assert!(matches!(h.verify(), Err(DdsError::MissingDimensionFlags(_))));
        h.dw_flags = (DDSD::HEIGHT | DDSD::WIDTH).bits();
        assert!(matches!(h.verify(), Err(DdsError::NotATexture(_))));
        h.dw_caps = DDSCAPS::TEXTURE.bits();
        assert!(matches!(h.verify(), Err(DdsError::BadPixelFormatSize(0))));
        h.ddspf.dw_size = 32;
        assert!(h.verify().is_ok());
    }

    #[test]
    fn mip_and_depth_default_to_one_when_unflagged() {
        // The C# returned the raw 0, so callers looped zero times.
        let h = DdsHeader { dw_mip_map_count: 0, dw_depth: 0, ..Default::default() };
        assert_eq!(h.mip_map_count(), 1);
        assert_eq!(h.depth(), 1);
    }

    #[test]
    fn unknown_enum_values_are_none_not_undefined() {
        // Casting an arbitrary u32 into a Rust enum would be UB; the C# did
        // exactly that via [MarshalAs].
        assert!(DXGI_FORMAT::from_raw(0xDEAD_BEEF).is_none());
        assert!(FourCC::from_raw(0x1234_5678).is_none());
        assert_eq!(DXGI_FORMAT::from_raw(0), Some(DXGI_FORMAT::UNKNOWN));
    }

    #[test]
    fn uncompressed_masks_decode_to_the_right_layout() {
        let mk = |bits, r, g, b, a| DdsHeader {
            dw_size: 124,
            dw_flags: (DDSD::HEIGHT | DDSD::WIDTH).bits(),
            dw_caps: DDSCAPS::TEXTURE.bits(),
            ddspf: DdsPixelFormat {
                dw_size: 32,
                dw_rgb_bit_count: bits,
                dw_r_bit_mask: r,
                dw_g_bit_mask: g,
                dw_b_bit_mask: b,
                dw_a_bit_mask: a,
                ..Default::default()
            },
            ..Default::default()
        };
        assert_eq!(
            mk(32, 0x00ff_0000, 0xff00, 0xff, 0xff00_0000).block_format().unwrap(),
            (TextureFormat::ARGB32, 4)
        );
        assert_eq!(
            mk(16, 0xf800, 0x07e0, 0x001f, 0).block_format().unwrap(),
            (TextureFormat::RGB565, 2)
        );
        assert!(mk(12, 1, 2, 3, 4).block_format().is_err());
    }

    #[test]
    fn genuine_bitflags_still_compose() {
        let f = DDSD::HEIGHT | DDSD::WIDTH;
        assert!(f.contains(DDSD::HEIGHT));
        assert!(!f.contains(DDSD::DEPTH));
    }

    #[test]
    fn misflagged_enums_are_plain_values_now() {
        // In C#, TexturePixel.Float (5) HasFlag TexturePixel.Byte (1) is TRUE.
        // As a plain enum the question cannot be asked, which is the point.
        assert_ne!(TexturePixel::Float, TexturePixel::Byte);
        assert_eq!(TexturePixel::from_raw(5), Some(TexturePixel::Float));
    }

    // ---- Vectors lifted from the C# test suite ---------------------------
    // `OpenStack.GfxTests/Gfx_Texture.cs` embeds these as base64 and asserts
    // width*height == 10000 and a payload of [1,2,3]. Using the same bytes
    // means this port is checked against real data the C# side already agrees
    // on, not just against itself.

    /// A 100x100 DXT1 header + 3 payload bytes.
    const DXT1_VECTOR: &str = "RERTIHwAAAAHEAAAZAAAAGQAAACIEwAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAARFhUMQAAAAAAAAAAAAAAAAAAAAAAAAAACBBAAAAAAAAAAAAAAAAAAAAAAAABAgM=";
    /// The same, as DX10 with a BC1_UNORM_SRGB extension header.
    const DX10_VECTOR: &str = "RERTIHwAAAAHEAAAZAAAAGQAAACIEwAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAARFgxMAAAAAAAAAAAAAAAAAAAAAAAAAAACBBAAAAAAAAAAAAAAAAAAAAAAABIAAAAAwAAAAAAAAABAAAAAAAAAAECAw==";

    /// Minimal base64 decoder, so the test data needs no dependency.
    fn b64(s: &str) -> Vec<u8> {
        const T: &[u8] = b"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        let mut out = Vec::new();
        let (mut acc, mut bits) = (0u32, 0u32);
        for c in s.bytes() {
            if c == b'=' {
                break;
            }
            let Some(v) = T.iter().position(|&x| x == c) else { continue };
            acc = (acc << 6) | v as u32;
            bits += 6;
            if bits >= 8 {
                bits -= 8;
                out.push((acc >> bits) as u8);
            }
        }
        out
    }

    #[test]
    fn base64_helper_is_correct() {
        // Guard the guard: if the decoder is wrong every vector test is void.
        assert_eq!(b64("AQID"), vec![1, 2, 3]);
        assert_eq!(b64("RERTIA=="), b"DDS ".to_vec());
    }

    #[test]
    fn reads_the_dxt1_vector_from_the_c_sharp_tests() {
        let data = b64(DXT1_VECTOR);
        let mut c = Cursor::new(data);
        let (h, dxt10, format, bytes) = DdsHeader::read_full(&mut c, true).unwrap();
        // The C# asserts exactly these two things.
        assert_eq!(h.dw_width * h.dw_height, 10000);
        assert_eq!(bytes, vec![1, 2, 3]);
        assert!(dxt10.is_none());
        assert_eq!(format, TextureFormat::DXT1);
        assert_eq!((h.dw_width, h.dw_height), (100, 100));
        assert_eq!(h.dw_mip_map_count, 1);
        assert_eq!(h.dw_pitch_or_linear_size, 100 * 100 / 2);
    }

    #[test]
    fn reads_the_dx10_vector_and_its_extension_header() {
        let data = b64(DX10_VECTOR);
        let mut c = Cursor::new(data);
        let (h, dxt10, _format, bytes) = DdsHeader::read_full(&mut c, true).unwrap();
        assert_eq!(h.dw_width * h.dw_height, 10000);
        assert_eq!(bytes, vec![1, 2, 3]);
        let d = dxt10.expect("DX10 fourcc must yield an extension header");
        // Independently confirms the generated enum values: 72 and 3.
        assert_eq!(d.format(), Some(DXGI_FORMAT::BC1_UNORM_SRGB));
        assert_eq!(d.dimension(), Some(D3D10_RESOURCE_DIMENSION::TEXTURE2D));
        assert_eq!(d.array_size, 1);
    }

    #[test]
    fn write_reproduces_the_c_sharp_bytes_exactly() {
        // The C# `Test_Write` asserts the same base64 output, so matching it
        // byte for byte means the two writers agree.
        let expected = b64(DXT1_VECTOR);
        let h = DdsHeader {
            dw_size: DdsHeader::SIZE_OF as u32,
            dw_flags: DDSD::HEADER_FLAGS_TEXTURE.bits(),
            dw_height: 100,
            dw_width: 100,
            dw_pitch_or_linear_size: 100 * 100 / 2,
            dw_mip_map_count: 1,
            dw_caps: (DDSCAPS::SURFACE_FLAGS_TEXTURE | DDSCAPS::SURFACE_FLAGS_MIPMAP).bits(),
            ddspf: DdsPixelFormat {
                dw_size: DdsPixelFormat::SIZE_OF as u32,
                dw_flags: DDPF::FOURCC.bits(),
                dw_four_cc: FourCC::DXT1 as u32,
                ..Default::default()
            },
            ..Default::default()
        };
        let mut out = Vec::new();
        h.write(&mut out, None, &[1, 2, 3]).unwrap();
        assert_eq!(out, expected);
    }

    #[test]
    fn write_reproduces_the_dx10_variant_exactly() {
        let expected = b64(DX10_VECTOR);
        let h = DdsHeader {
            dw_size: DdsHeader::SIZE_OF as u32,
            dw_flags: DDSD::HEADER_FLAGS_TEXTURE.bits(),
            dw_height: 100,
            dw_width: 100,
            dw_pitch_or_linear_size: 100 * 100 / 2,
            dw_mip_map_count: 1,
            dw_caps: (DDSCAPS::SURFACE_FLAGS_TEXTURE | DDSCAPS::SURFACE_FLAGS_MIPMAP).bits(),
            ddspf: DdsPixelFormat {
                dw_size: DdsPixelFormat::SIZE_OF as u32,
                dw_flags: DDPF::FOURCC.bits(),
                dw_four_cc: FourCC::DX10 as u32,
                ..Default::default()
            },
            ..Default::default()
        };
        let d = DdsHeaderDxt10 {
            dxgi_format: DXGI_FORMAT::BC1_UNORM_SRGB as u32,
            resource_dimension: D3D10_RESOURCE_DIMENSION::TEXTURE2D as u32,
            misc_flag: 0,
            array_size: 1,
            misc_flags2: DDS_ALPHA_MODE::ALPHA_MODE_UNKNOWN as u32,
        };
        let mut out = Vec::new();
        h.write(&mut out, Some(&d), &[1, 2, 3]).unwrap();
        assert_eq!(out, expected);
    }

    #[test]
    fn read_write_round_trips_both_vectors() {
        for v in [DXT1_VECTOR, DX10_VECTOR] {
            let data = b64(v);
            let mut c = Cursor::new(data.clone());
            let (h, dxt10, _f, bytes) = DdsHeader::read_full(&mut c, true).unwrap();
            let mut out = Vec::new();
            h.write(&mut out, dxt10.as_ref(), &bytes).unwrap();
            assert_eq!(out, data);
        }
    }

    #[test]
    fn the_c_sharp_composite_flags_have_the_values_the_vectors_carry() {
        // 0x1007 and 0x401008 are the literal words in the vector bytes.
        assert_eq!(DDSD::HEADER_FLAGS_TEXTURE.bits(), 0x1007);
        assert_eq!(
            (DDSCAPS::SURFACE_FLAGS_TEXTURE | DDSCAPS::SURFACE_FLAGS_MIPMAP).bits(),
            0x401008
        );
    }
}
