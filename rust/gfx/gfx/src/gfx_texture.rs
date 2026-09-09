// PORT-SOURCE: Gfx/OpenStack.Gfx/Gfx_Texture.cs
// PORT-SHA: 18e4bbf2bcc46a69
// PORT-STATUS: done

#![allow(non_camel_case_types)]

// use bytemuck::{Pod, Zeroable};
// use poly::prelude::{BinaryReaderExt};
// use std::io::{Read, Seek};

//#region Texture Enums

bitflags::bitflags! {
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
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

//#endregion

//#region DDS_PIXELFORMAT

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

bitflags::bitflags! {
    /// C# `[Flags] enum DDPF : u32`.
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
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

#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct DDS_PIXELFORMAT {
    pub dwSize: u32,
    pub dwFlags: DDPF,
    pub dwFourCC: FourCC,
    pub dwRGBBitCount: u32,
    pub dwRBitMask: u32,
    pub dwGBitMask: u32,
    pub dwBBitMask: u32,
    pub dwABitMask: u32,
}

impl DDS_PIXELFORMAT {
    pub const SIZE_OF: usize = 32;

    // #[inline]
    // pub fn flags(&self) -> DDPF {
    //     DDPF::from_bits_truncate(self.dw_flags)
    // }

    // /// `None` when the on-disk code is not one this build knows.
    // #[inline]
    // pub fn four_cc(&self) -> Option<FourCC> {
    //     FourCC::from_raw(self.dw_four_cc)
    // }

    // pub fn read<R: Read + Seek>(r: &mut R) -> Result<Self, ReadError> {
    //     Ok(Self {
    //         dw_size: r.read_u32()?,
    //         dw_flags: r.read_u32()?,
    //         dw_four_cc: r.read_u32()?,
    //         dw_rgb_bit_count: r.read_u32()?,
    //         dw_r_bit_mask: r.read_u32()?,
    //         dw_g_bit_mask: r.read_u32()?,
    //         dw_b_bit_mask: r.read_u32()?,
    //         dw_a_bit_mask: r.read_u32()?,
    //     })
    // }
}

//#endregion

//#region DDS_HEADER_DXT10

#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum DDS_ALPHA_MODE {
    ALPHA_MODE_UNKNOWN = 0,
    ALPHA_MODE_STRAIGHT = 1,
    ALPHA_MODE_PREMULTIPLIED = 2,
    ALPHA_MODE_OPAQUE = 3,
    ALPHA_MODE_CUSTOM = 4,
}

#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum D3D10_RESOURCE_DIMENSION {
    UNKNOWN = 0,
    BUFFER = 1,
    TEXTURE1D = 2,
    TEXTURE2D = 3,
    TEXTURE3D = 4,
}


#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct DDS_HEADER_DXT10 {
    pub dxgiFormat: DXGI_FORMAT,
    pub resourceDimension: D3D10_RESOURCE_DIMENSION,
    pub miscFlag: u32,
    pub arraySize: u32,
    pub miscFlags2: u32,
}

impl DDS_HEADER_DXT10 {
    // pub const SIZE_OF: usize = 20;

    // #[inline]
    // pub fn format(&self) -> Option<DXGI_FORMAT> {
    //     DXGI_FORMAT::from_raw(self.dxgi_format)
    // }

    // #[inline]
    // pub fn dimension(&self) -> Option<D3D10_RESOURCE_DIMENSION> {
    //     D3D10_RESOURCE_DIMENSION::from_raw(self.resource_dimension)
    // }

    // pub fn read<R: Read + Seek>(r: &mut R) -> Result<Self, ReadError> {
    //     Ok(Self {
    //         dxgi_format: r.read_u32()?,
    //         resource_dimension: r.read_u32()?,
    //         misc_flag: r.read_u32()?,
    //         array_size: r.read_u32()?,
    //         misc_flags2: r.read_u32()?,
    //     })
    // }
}

//#endregion

//#region DDS_HEADER

bitflags::bitflags! {
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
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
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
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
    #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
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

#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct DDS_HEADER {
    pub dwSize: u32,
    pub dwFlags: DDSD,
    pub dwHeight: u32,
    pub dwWidth: u32,
    pub dwPitchOrLinearSize: u32,
    pub dwDepth: u32,
    pub dwMipMapCount: u32,
    pub dwReserved1: [u32; 11],
    pub ddspf: DDS_PIXELFORMAT,
    pub dwCaps: DDSCAPS,
    pub dwCaps2: DDSCAPS2,
    pub dwCaps3: u32,
    pub dwCaps4: u32,
    pub dwReserved2: u32,
}

// impl Default for DDS_HEADER {
//     fn default() -> Self {
//         Zeroable::zeroed()
//     }
// }

impl DDS_HEADER {
    pub const MAGIC: u32 = 0x2053_4444;
    pub const SIZE_OF: usize = 124;

    // #[inline]
    // pub fn flags(&self) -> DDSD {
    //     DDSD::from_bits_truncate(self.dw_flags)
    // }

    // #[inline]
    // pub fn caps(&self) -> DDSCAPS {
    //     DDSCAPS::from_bits_truncate(self.dw_caps)
    // }

    // #[inline]
    // pub fn caps2(&self) -> DDSCAPS2 {
    //     DDSCAPS2::from_bits_truncate(self.dw_caps2)
    // }

    // /// C# `Verify()`.
    // pub fn verify(&self) -> Result<(), DdsError> {
    //     if self.dw_size != 124 {
    //         return Err(DdsError::BadHeaderSize(self.dw_size));
    //     }
    //     if !self.flags().contains(DDSD::HEIGHT | DDSD::WIDTH) {
    //         return Err(DdsError::MissingDimensionFlags(self.dw_flags));
    //     }
    //     if !self.caps().contains(DDSCAPS::TEXTURE) {
    //         return Err(DdsError::NotATexture(self.dw_caps));
    //     }
    //     if self.ddspf.dw_size != 32 {
    //         return Err(DdsError::BadPixelFormatSize(self.ddspf.dw_size));
    //     }
    //     Ok(())
    // }

    // /// Mip level count, treating an unset/zero field as a single level — the
    // /// C# returned the raw 0 and left callers to loop zero times.
    // #[inline]
    // pub fn mip_map_count(&self) -> u32 {
    //     if self.flags().contains(DDSD::MIPMAPCOUNT) {
    //         self.dw_mip_map_count.max(1)
    //     } else {
    //         1
    //     }
    // }

    // /// Depth, treating an unset flag as 1 rather than the raw field.
    // #[inline]
    // pub fn depth(&self) -> u32 {
    //     if self.flags().contains(DDSD::DEPTH) {
    //         self.dw_depth.max(1)
    //     } else {
    //         1
    //     }
    // }

    // #[inline]
    // pub fn is_cubemap(&self) -> bool {
    //     self.caps2().contains(DDSCAPS2::CUBEMAP)
    // }

    // /// C# `DDS_HEADER.Read(BinaryReader r, bool readMagic = true)`.
    // ///
    // /// Returns the header and, when the FourCC is DX10, the extension header.
    // /// The C# also returned a `(object type, int blockSize, object value)`
    // /// tuple; that mapping is `block_format()` below, typed rather than boxed.
    // pub fn read<R: Read + Seek>(
    //     r: &mut R,
    //     read_magic: bool,
    // ) -> Result<(Self, Option<DdsHeaderDxt10>), DdsError> {
    //     if read_magic {
    //         let magic = r.read_u32().map_err(|_| DdsError::BadMagic(0))?;
    //         if magic != Self::MAGIC {
    //             return Err(DdsError::BadMagic(magic));
    //         }
    //     }
    //     let read_u32 = |r: &mut R| r.read_u32().map_err(|_| DdsError::BadHeaderSize(0));
    //     let mut h = Self {
    //         dw_size: read_u32(r)?,
    //         dw_flags: read_u32(r)?,
    //         dw_height: read_u32(r)?,
    //         dw_width: read_u32(r)?,
    //         dw_pitch_or_linear_size: read_u32(r)?,
    //         dw_depth: read_u32(r)?,
    //         dw_mip_map_count: read_u32(r)?,
    //         dw_reserved1: [0; 11],
    //         ddspf: DdsPixelFormat::default(),
    //         dw_caps: 0,
    //         dw_caps2: 0,
    //         dw_caps3: 0,
    //         dw_caps4: 0,
    //         dw_reserved2: 0,
    //     };
    //     for slot in h.dw_reserved1.iter_mut() {
    //         *slot = read_u32(r)?;
    //     }
    //     h.ddspf = DdsPixelFormat::read(r).map_err(|_| DdsError::BadPixelFormatSize(0))?;
    //     h.dw_caps = read_u32(r)?;
    //     h.dw_caps2 = read_u32(r)?;
    //     h.dw_caps3 = read_u32(r)?;
    //     h.dw_caps4 = read_u32(r)?;
    //     h.dw_reserved2 = read_u32(r)?;
    //     h.verify()?;

    //     let dxt10 = if h.ddspf.four_cc() == Some(FourCC::DX10) {
    //         Some(DdsHeaderDxt10::read(r).map_err(|_| DdsError::BadHeaderSize(0))?)
    //     } else {
    //         None
    //     };
    //     Ok((h, dxt10))
    // }

    // /// C# `DDS_HEADER.Read(BinaryReader r, bool readMagic = true)` in full —
    // /// header, optional DX10 extension, decoded format, and the remaining
    // /// payload bytes.
    // ///
    // /// The C# returns a 4-tuple `(header, headerDxt10, format, bytes)`; this
    // /// matches it. `read` above is the header-only form for callers that want
    // /// to stream the payload rather than buffer it.
    // pub fn read_full<R: Read + Seek>(
    //     r: &mut R,
    //     read_magic: bool,
    // ) -> Result<(Self, Option<DdsHeaderDxt10>, TextureFormat, Vec<u8>), DdsError> {
    //     let (h, dxt10) = Self::read(r, read_magic)?;
    //     let (format, _block) = h.block_format()?;
    //     let mut bytes = Vec::new();
    //     r.read_to_end(&mut bytes)
    //         .map_err(|_| DdsError::BadHeaderSize(0))?;
    //     Ok((h, dxt10, format, bytes))
    // }

    // /// C# `DDS_HEADER.Write(BinaryWriter w, DDS_HEADER h, DDS_HEADER_DXT10? dxt10, byte[] bytes)`.
    // ///
    // /// Emits magic, the 124-byte header, the 20-byte DX10 extension when
    // /// present, then the payload. Round-trips with [`read_full`](Self::read_full).
    // pub fn write<W: Write>(
    //     &self,
    //     w: &mut W,
    //     dxt10: Option<&DdsHeaderDxt10>,
    //     bytes: &[u8],
    // ) -> io::Result<()> {
    //     w.write_all(&Self::MAGIC.to_le_bytes())?;
    //     for v in [
    //         self.dw_size,
    //         self.dw_flags,
    //         self.dw_height,
    //         self.dw_width,
    //         self.dw_pitch_or_linear_size,
    //         self.dw_depth,
    //         self.dw_mip_map_count,
    //     ] {
    //         w.write_all(&v.to_le_bytes())?;
    //     }
    //     for v in self.dw_reserved1 {
    //         w.write_all(&v.to_le_bytes())?;
    //     }
    //     for v in [
    //         self.ddspf.dw_size,
    //         self.ddspf.dw_flags,
    //         self.ddspf.dw_four_cc,
    //         self.ddspf.dw_rgb_bit_count,
    //         self.ddspf.dw_r_bit_mask,
    //         self.ddspf.dw_g_bit_mask,
    //         self.ddspf.dw_b_bit_mask,
    //         self.ddspf.dw_a_bit_mask,
    //     ] {
    //         w.write_all(&v.to_le_bytes())?;
    //     }
    //     for v in [
    //         self.dw_caps,
    //         self.dw_caps2,
    //         self.dw_caps3,
    //         self.dw_caps4,
    //         self.dw_reserved2,
    //     ] {
    //         w.write_all(&v.to_le_bytes())?;
    //     }
    //     if let Some(d) = dxt10 {
    //         for v in [
    //             d.dxgi_format,
    //             d.resource_dimension,
    //             d.misc_flag,
    //             d.array_size,
    //             d.misc_flags2,
    //         ] {
    //             w.write_all(&v.to_le_bytes())?;
    //         }
    //     }
    //     w.write_all(bytes)
    // }

    // /// The C#'s FourCC -> (format, block size) switch, typed.
    // ///
    // /// Returns bytes per 4x4 block for compressed formats, bytes per pixel for
    // /// uncompressed ones — the same convention `texture_helper::BlockFormat`
    // /// uses.
    // pub fn block_format(&self) -> Result<(TextureFormat, u32), DdsError> {
    //     match self.ddspf.four_cc() {
    //         Some(FourCC::DXT1) => Ok((TextureFormat::DXT1, 8)),
    //         // DXT2 is DXT3 with premultiplied alpha; same block layout.
    //         Some(FourCC::DXT2 | FourCC::DXT3) => Ok((TextureFormat::DXT3, 16)),
    //         // DXT4 is DXT5 with premultiplied alpha; same block layout.
    //         Some(FourCC::DXT4 | FourCC::DXT5) => Ok((TextureFormat::DXT5, 16)),
    //         // ATI1/ATI2 are the original FourCCs for what D3D10 renamed BC4/BC5.
    //         Some(FourCC::ATI1) => Ok((TextureFormat::BC4, 8)),
    //         Some(FourCC::ATI2 | FourCC::A2XY) => Ok((TextureFormat::BC5, 16)),
    //         // RXGB is DXT5 with the red and alpha channels swapped (Doom 3
    //         // normal maps); the block layout is unchanged, so the swap is the
    //         // decoder's business, not the header's.
    //         Some(FourCC::RXGB) => Ok((TextureFormat::DXT5, 16)),
    //         // NONE means an uncompressed format described by the bit masks.
    //         Some(FourCC::NONE) | None if self.ddspf.dw_four_cc == 0 => self.uncompressed_format(),
    //         _ => Err(DdsError::UnsupportedFormat(self.ddspf.dw_four_cc)),
    //     }
    // }

    // /// C# `MakeFormat(ref ddspf)` — decode an uncompressed layout from the
    // /// channel bit masks.
    // fn uncompressed_format(&self) -> Result<(TextureFormat, u32), DdsError> {
    //     let p = &self.ddspf;
    //     let bpp = p.dw_rgb_bit_count / 8;
    //     let f = match (p.dw_rgb_bit_count, p.dw_r_bit_mask, p.dw_g_bit_mask, p.dw_b_bit_mask, p.dw_a_bit_mask) {
    //         (32, 0x00ff_0000, 0x0000_ff00, 0x0000_00ff, 0xff00_0000) => TextureFormat::ARGB32,
    //         (32, 0x0000_00ff, 0x0000_ff00, 0x00ff_0000, 0xff00_0000) => TextureFormat::RGBA32,
    //         (32, 0x00ff_0000, 0x0000_ff00, 0x0000_00ff, 0) => TextureFormat::BGRA32,
    //         (24, 0x00ff_0000, 0x0000_ff00, 0x0000_00ff, 0) => TextureFormat::RGB24,
    //         (16, 0xf800, 0x07e0, 0x001f, 0) => TextureFormat::RGB565,
    //         (16, 0x7c00, 0x03e0, 0x001f, 0x8000) => TextureFormat::BGRA1555,
    //         (8, _, _, _, _) => TextureFormat::L8,
    //         _ => return Err(DdsError::UnsupportedFormat(p.dw_rgb_bit_count)),
    //     };
    //     Ok((f, bpp.max(1)))
    // }
}


// /// C# `FormatException` throws from `Verify`.
// #[derive(Debug, Clone, PartialEq, Eq)]
// pub enum DdsError {
//     BadMagic(u32),
//     BadHeaderSize(u32),
//     MissingDimensionFlags(u32),
//     NotATexture(u32),
//     BadPixelFormatSize(u32),
//     /// The header describes a format this build cannot decode.
//     UnsupportedFormat(u32),
// }

// impl std::fmt::Display for DdsError {
//     fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
//         match self {
//             DdsError::BadMagic(v) => write!(f, "invalid DDS magic: {v:#x}"),
//             DdsError::BadHeaderSize(v) => write!(f, "invalid DDS header size: {v}"),
//             DdsError::MissingDimensionFlags(v) => {
//                 write!(f, "DDS flags lack HEIGHT|WIDTH: {v:#x}")
//             }
//             DdsError::NotATexture(v) => write!(f, "DDS caps lack TEXTURE: {v:#x}"),
//             DdsError::BadPixelFormatSize(v) => {
//                 write!(f, "invalid DDS pixel format size: {v}")
//             }
//             DdsError::UnsupportedFormat(v) => write!(f, "unsupported DDS format: {v:#x}"),
//         }
//     }
// }

// impl std::error::Error for DdsError {}



//#endregion

//#region DXGI_FORMAT

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

//#endregion

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
