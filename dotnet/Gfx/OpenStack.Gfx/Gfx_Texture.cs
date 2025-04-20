using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static OpenStack.Gfx.DXGI_FORMAT;
using static OpenStack.Gfx.FourCC;

namespace OpenStack.Gfx;

#region Texture Enums

/// <summary>
/// TextureFlags
/// </summary>
[Flags]
public enum TextureFlags : int
{
    SUGGEST_CLAMPS = 0x00000001,
    SUGGEST_CLAMPT = 0x00000002,
    SUGGEST_CLAMPU = 0x00000004,
    NO_LOD = 0x00000008,
    CUBE_TEXTURE = 0x00000010,
    VOLUME_TEXTURE = 0x00000020,
    TEXTURE_ARRAY = 0x00000040,
}

[Flags]
public enum TextureFormat : int
{
    Unknown = 0,
    I8 = 1,
    L8 = 2,
    R8 = 3,
    R16 = 4,
    RG16 = 5,
    RGB24 = 6,
    RGB565 = 7,
    RGBA32 = 8,
    ARGB32 = 9,
    BGRA32 = 10,
    BGRA1555 = 11,
    Compressed = 0x10000000,
    DXT1 = 100 | Compressed,
    DXT1A = 101 | Compressed,
    DXT3 = 102 | Compressed,
    DXT5 = 103 | Compressed,
    BC4 = 104 | Compressed,
    BC5 = 105 | Compressed,
    BC6H = 106 | Compressed,
    BC7 = 107 | Compressed,
    ETC2 = 108 | Compressed,
    ETC2_EAC = 109 | Compressed,
}

[Flags]
public enum TexturePixel : int
{
    Unknown = 0,
    Byte = 1,
    Short = 2,
    Int = 4,
    Float = 5,
    Signed = 0x100,
    Reversed = 0x200,
}

#endregion

#region DDS_PIXELFORMAT
// https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-pixelformat

/// <summary>
/// FourCC
/// </summary>
public enum FourCC : uint
{
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

/// <summary>
/// Values which indicate what type of data is in the surface
/// </summary>
[Flags]
public enum DDPF : uint
{
    /// <summary>
    /// Texture contains alpha data; dwRGBAlphaBitMask contains valid data
    /// </summary>
    ALPHAPIXELS = 0x00000001,
    /// <summary>
    /// Used in some older DDS files for alpha channel only uncompressed data (dwRGBBitCount contains the alpha channel bitcount; dwABitMask contains valid data)
    /// </summary>
    ALPHA = 0x00000002,
    /// <summary>
    /// Texture contains compressed RGB data; dwFourCC contains valid data
    /// </summary>
    FOURCC = 0x00000004,
    /// <summary>
    /// Texture contains uncompressed RGB data; dwRGBBitCount and the RGB masks (dwRBitMask, dwGBitMask, dwBBitMask) contain valid data
    /// </summary>
    RGB = 0x00000040,
    /// <summary>
    /// Used in some older DDS files for YUV uncompressed data (dwRGBBitCount contains the YUV bit count; dwRBitMask contains the Y mask, dwGBitMask contains the U mask, dwBBitMask contains the V mask)
    /// </summary>
    YUV = 0x00000200,
    /// <summary>
    /// Used in some older DDS files for single channel color uncompressed data (dwRGBBitCount contains the luminance channel bit count; dwRBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel DDS file
    /// </summary>
    LUMINANCE = 0x00020000,
    /// <summary>
    /// The normal
    /// </summary>
    NORMAL = 0x80000000,
}

/// <summary>
/// Surface pixel format.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DDS_PIXELFORMAT
{
    public const int SizeOf = 32;
    /// <summary>
    /// Struct
    /// </summary>
    public static (string, int) Struct = ("8I", 32);
    /// <summary>
    /// Structure size; set to 32 (bytes)
    /// </summary>
    public uint dwSize;
    /// <summary>
    /// Values which indicate what type of data is in the surface
    /// </summary>
    [MarshalAs(UnmanagedType.U4)] public DDPF dwFlags;
    /// <summary>
    /// Four-character codes for specifying compressed or custom formats. Possible values include: DXT1, DXT2, DXT3, DXT4, or DXT5. A FourCC of DX10 indicates the prescense of the DDS_HEADER_DXT10 extended header, and the dxgiFormat member of that structure indicates the true format. When using a four-character code, dwFlags must include DDPF_FOURCC
    /// </summary>
    [MarshalAs(UnmanagedType.U4)] public FourCC dwFourCC;
    /// <summary>
    /// Number of bits in an RGB (possibly including alpha) format. Valid when dwFlags includes DDPF_RGB, DDPF_LUMINANCE, or DDPF_YUV
    /// </summary>
    public uint dwRGBBitCount;
    /// <summary>
    /// Red (or lumiannce or Y) mask for reading color data. For instance, given the A8R8G8B8 format, the red mask would be 0x00ff0000
    /// </summary>
    public uint dwRBitMask;
    /// <summary>
    /// Green (or U) mask for reading color data. For instance, given the A8R8G8B8 format, the green mask would be 0x0000ff00
    /// </summary>
    public uint dwGBitMask;
    /// <summary>
    /// Blue (or V) mask for reading color data. For instance, given the A8R8G8B8 format, the blue mask would be 0x000000ff
    /// </summary>
    public uint dwBBitMask;
    /// <summary>
    /// Alpha mask for reading alpha data. dwFlags must include DDPF_ALPHAPIXELS or DDPF_ALPHA. For instance, given the A8R8G8B8 format, the alpha mask would be 0xff000000
    /// </summary>
    public uint dwABitMask;
}

#endregion

#region DDS_HEADER_DXT10
// https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header-dxt10
// https://docs.microsoft.com/en-us/windows/win32/api/d3d10/ne-d3d10-d3d10_resource_dimension

public enum DDS_ALPHA_MODE : uint
{
    ALPHA_MODE_UNKNOWN = 0,
    ALPHA_MODE_STRAIGHT = 1,
    ALPHA_MODE_PREMULTIPLIED = 2,
    ALPHA_MODE_OPAQUE = 3,
    ALPHA_MODE_CUSTOM = 4,
}

[Flags]
public enum D3D10_RESOURCE_DIMENSION : uint
{
    /// <summary>
    /// Resource is of unknown type
    /// </summary>
    UNKNOWN = 0,
    /// <summary>
    /// Resource is a buffer
    /// </summary>
    BUFFER = 1,
    /// <summary>
    /// Resource is a 1D texture. The dwWidth member of DDS_HEADER specifies the size of the texture. Typically, you set the dwHeight member of DDS_HEADER to 1; you also must set the DDSD_HEIGHT flag in the dwFlags member of DDS_HEADER
    /// </summary>
    TEXTURE1D = 2,
    /// <summary>
    /// Resource is a 2D texture with an area specified by the dwWidth and dwHeight members of DDS_HEADER. You can also use this type to identify a cube-map texture. For more information about how to identify a cube-map texture, see miscFlag and arraySize members
    /// </summary>
    TEXTURE2D = 3,
    /// <summary>
    /// Resource is a 3D texture with a volume specified by the dwWidth, dwHeight, and dwDepth members of DDS_HEADER. You also must set the DDSD_DEPTH flag in the dwFlags member of DDS_HEADER
    /// </summary>
    TEXTURE3D = 4,
}

/// <summary>
/// DDS header extension to handle resource arrays, DXGI pixel formats that don't map to the legacy Microsoft DirectDraw pixel format structures, and additional metadata
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DDS_HEADER_DXT10
{
    /// <summary>
    /// Struct
    /// </summary>
    public static (string, int) Struct = ("<5I", 20);

    /// <summary>
    /// The surface pixel format (see DXGI_FORMAT)
    /// </summary>
    [MarshalAs(UnmanagedType.I4)] public DXGI_FORMAT dxgiFormat;
    /// <summary>
    /// Identifies the type of resource. The following values for this member are a subset of the values in the D3D10_RESOURCE_DIMENSION or D3D11_RESOURCE_DIMENSION enumeration
    /// </summary>
    [MarshalAs(UnmanagedType.U4)] public D3D10_RESOURCE_DIMENSION resourceDimension;
    /// <summary>
    /// Identifies other, less common options for resources. The following value for this member is a subset of the values in the D3D10_RESOURCE_MISC_FLAG or D3D11_RESOURCE_MISC_FLAG enumeration
    /// </summary>
    public uint miscFlag;
    /// <summary>
    /// The number of elements in the array
    /// </summary>
    public uint arraySize;
    /// <summary>
    /// Contains additional metadata (formerly was reserved). The lower 3 bits indicate the alpha mode of the associated resource. The upper 29 bits are reserved and are typically 0
    /// </summary>
    public uint miscFlags2; // see DDS_MISC_FLAGS2
}

#endregion

#region DDS_HEADER
// https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
// https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide

/// <summary>
/// Flags to indicate which members contain valid data
/// </summary>
[Flags]
public enum DDSD : uint
{
    /// <summary>
    /// Required in every .dds file
    /// </summary>
    CAPS = 0x00000001,
    /// <summary>
    /// Required in every .dds file
    /// </summary>
    HEIGHT = 0x00000002,
    /// <summary>
    /// Required in every .dds file
    /// </summary>
    WIDTH = 0x00000004,
    /// <summary>
    /// Required when pitch is provided for an uncompressed texture
    /// </summary>
    PITCH = 0x00000008,
    /// <summary>
    /// Required in every .dds file
    /// </summary>
    PIXELFORMAT = 0x00001000,
    /// <summary>
    /// Required in a mipmapped texture
    /// </summary>
    MIPMAPCOUNT = 0x00020000,
    /// <summary>
    /// Required when pitch is provided for a compressed texture
    /// </summary>
    LINEARSIZE = 0x00080000,
    /// <summary>
    /// Required in a depth texture
    /// </summary>
    DEPTH = 0x00800000,
    HEADER_FLAGS_TEXTURE = CAPS | HEIGHT | WIDTH | PIXELFORMAT,
    HEADER_FLAGS_MIPMAP = MIPMAPCOUNT,
    HEADER_FLAGS_VOLUME = DEPTH,
    HEADER_FLAGS_PITCH = PITCH,
    HEADER_FLAGS_LINEARSIZE = LINEARSIZE,
}

/// <summary>
/// Specifies the complexity of the surfaces stored
/// </summary>
[Flags]
public enum DDSCAPS : uint
{
    /// <summary>
    /// Optional; must be used on any file that contains more than one surface (a mipmap, a cubic environment map, or mipmapped volume texture)
    /// </summary>
    COMPLEX = 0x00000008,
    /// <summary>
    /// Required
    /// </summary>
    TEXTURE = 0x00001000,
    /// <summary>
    /// Optional; should be used for a mipmap
    /// </summary>
    MIPMAP = 0x00400000,
    SURFACE_FLAGS_MIPMAP = COMPLEX | MIPMAP,
    SURFACE_FLAGS_TEXTURE = TEXTURE,
    SURFACE_FLAGS_CUBEMAP = COMPLEX,
}

/// <summary>
/// Additional detail about the surfaces stored
/// </summary>
[Flags]
public enum DDSCAPS2 : uint
{
    /// <summary>
    /// Required for a cube map
    /// </summary>
    CUBEMAP = 0x00000200,
    /// <summary>
    /// Required when these surfaces are stored in a cube map.
    /// </summary>
    CUBEMAPPOSITIVEX = 0x00000400,
    /// <summary>
    /// Required when these surfaces are stored in a cube map
    /// </summary>
    CUBEMAPNEGATIVEX = 0x00000800,
    /// <summary>
    /// Required when these surfaces are stored in a cube map
    /// </summary>
    CUBEMAPPOSITIVEY = 0x00001000,
    /// <summary>
    /// Required when these surfaces are stored in a cube map
    /// </summary>
    CUBEMAPNEGATIVEY = 0x00002000,
    /// <summary>
    /// Required when these surfaces are stored in a cube map
    /// </summary>
    CUBEMAPPOSITIVEZ = 0x00004000,
    /// <summary>
    /// Required when these surfaces are stored in a cube map
    /// </summary>
    CUBEMAPNEGATIVEZ = 0x00008000,
    /// <summary>
    /// Required for a volume texture
    /// </summary>
    VOLUME = 0x00200000,
    CUBEMAP_POSITIVEX = CUBEMAP | CUBEMAPPOSITIVEX,
    CUBEMAP_NEGATIVEX = CUBEMAP | CUBEMAPNEGATIVEX,
    CUBEMAP_POSITIVEY = CUBEMAP | CUBEMAPPOSITIVEY,
    CUBEMAP_NEGATIVEY = CUBEMAP | CUBEMAPNEGATIVEY,
    CUBEMAP_POSITIVEZ = CUBEMAP | CUBEMAPPOSITIVEZ,
    CUBEMAP_NEGATIVEZ = CUBEMAP | CUBEMAPNEGATIVEZ,
    CUBEMAP_ALLFACES = CUBEMAPPOSITIVEX | CUBEMAPNEGATIVEX | CUBEMAPPOSITIVEY | CUBEMAPNEGATIVEY | CUBEMAPPOSITIVEZ | CUBEMAPNEGATIVEZ,
    FLAGS_VOLUME = VOLUME,
}

/// <summary>
/// Describes a DDS file header
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct DDS_HEADER
{
    /// <summary>
    /// Struct
    /// </summary>
    public static (string, int) Struct = ($"<7I44s{DDS_PIXELFORMAT.Struct.Item1}5I", SizeOf);
    /// <summary>
    /// MAGIC
    /// </summary>
    public const uint MAGIC = 0x20534444; // DDS_
    /// <summary>
    /// Struct
    /// </summary>
    public const int SizeOf = 124;
    /// <summary>
    /// Size of structure. This member must be set to 124
    /// </summary>
    /// <value>
    /// The size of the dw
    /// </value>
    public uint dwSize;
    /// <summary>
    /// Flags to indicate which members contain valid data
    /// </summary>
    [MarshalAs(UnmanagedType.U4)] public DDSD dwFlags;
    /// <summary>
    /// Surface height (in pixels)
    /// </summary>
    public uint dwHeight;
    /// <summary>
    /// Surface width (in pixels)
    /// </summary>
    public uint dwWidth;
    /// <summary>
    /// The pitch or number of bytes per scan line in an uncompressed texture; the total number of bytes in the top level texture for a compressed texture. For information about how to compute the pitch, see the DDS File Layout section of the Programming Guide for DDS
    /// </summary>
    public uint dwPitchOrLinearSize;
    /// <summary>
    /// Depth of a volume texture (in pixels), otherwise unused
    /// </summary>
    public uint dwDepth; // only if DDS_HEADER_FLAGS_VOLUME is set in flags
    /// <summary>
    /// Number of mipmap levels, otherwise unused
    /// </summary>
    public uint dwMipMapCount;
    /// <summary>
    /// Unused
    /// </summary>
    public fixed uint dwReserved1[11];
    /// <summary>
    /// The pixel format (see DDS_PIXELFORMAT)
    /// </summary>
    public DDS_PIXELFORMAT ddspf;
    /// <summary>
    /// Specifies the complexity of the surfaces stored
    /// </summary>
    [MarshalAs(UnmanagedType.U4)] public DDSCAPS dwCaps;
    /// <summary>
    /// Additional detail about the surfaces stored
    /// </summary>
    [MarshalAs(UnmanagedType.U4)] public DDSCAPS2 dwCaps2;
    /// <summary>
    /// The dw caps3
    /// </summary>
    public uint dwCaps3;
    /// <summary>
    /// The dw caps4
    /// </summary>
    public uint dwCaps4;
    /// <summary>
    /// The dw reserved2
    /// </summary>
    public uint dwReserved2;

    /// <summary>
    /// Verifies this instance
    /// </summary>
    public readonly void Verify()
    {
        if (dwSize != 124) throw new FormatException($"Invalid DDS file header size: {dwSize}.");
        else if (!dwFlags.HasFlag(DDSD.HEIGHT | DDSD.WIDTH)) throw new FormatException($"Invalid DDS file flags: {dwFlags}.");
        else if (!dwCaps.HasFlag(DDSCAPS.TEXTURE)) throw new FormatException($"Invalid DDS file caps: {dwCaps}.");
        else if (ddspf.dwSize != 32) throw new FormatException($"Invalid DDS file pixel format size: {ddspf.dwSize}.");
    }

    /// <summary>
    /// Read
    /// </summary>
    /// https://gist.github.com/tilkinsc/13191c0c1e5d6b25fbe79bbd2288a673
    /// https://github.com/BinomialLLC/basis_universal/wiki/OpenGL-texture-format-enums-table
    /// https://www.g-truc.net/post-0335.html
    /// https://www.reedbeta.com/blog/understanding-bcn-texture-compression-formats/
    public static (DDS_HEADER header, DDS_HEADER_DXT10? headerDxt10, (object type, int blockSize, object value) format, byte[] bytes) Read(BinaryReader r, bool readMagic = true)
    {
        if (readMagic)
        {
            var magic = r.ReadUInt32();
            if (magic != MAGIC) throw new FormatException($"Invalid DDS file magic: \"{magic}\".");
        }
        var header = r.ReadS<DDS_HEADER>();
        header.Verify();
        ref DDS_PIXELFORMAT ddspf = ref header.ddspf;
        var headerDxt10 = ddspf.dwFourCC == DX10 ? (DDS_HEADER_DXT10?)r.ReadS<DDS_HEADER_DXT10>() : null;
        var format = ddspf.dwFourCC switch
        {
            0 => MakeFormat(ref ddspf),
            //DXT1 => (DXT1, 8, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1),
            //DXT3 => (DXT3, 16, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureUnityFormat.DXT3_POLYFILL, TextureUnrealFormat.DXT3),
            //DXT5 => (DXT5, 16, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureUnityFormat.DXT5, TextureUnrealFormat.DXT5),
            DXT1 => (DXT1, 8, (TextureFormat.DXT1, TexturePixel.Unknown)),
            DXT3 => (DXT3, 16, (TextureFormat.DXT3, TexturePixel.Unknown)),
            DXT5 => (DXT5, 16, (TextureFormat.DXT5, TexturePixel.Unknown)),
            DX10 => (headerDxt10?.dxgiFormat) switch
            {
                //BC1_UNORM => (BC1_UNORM, 8, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1),
                //BC1_UNORM_SRGB => (BC1_UNORM_SRGB, 8, TextureGLFormat.CompressedSrgbS3tcDxt1Ext, TextureGLFormat.CompressedSrgbS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1),
                //BC2_UNORM => (BC2_UNORM, 16, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureUnityFormat.DXT3_POLYFILL, TextureUnrealFormat.DXT3),
                //BC2_UNORM_SRGB => (BC2_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaS3tcDxt1Ext, TextureGLFormat.CompressedSrgbAlphaS3tcDxt1Ext, TextureUnityFormat.Unknown, TextureUnrealFormat.Unknown),
                //BC3_UNORM => (BC3_UNORM, 16, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureUnityFormat.DXT5, TextureUnrealFormat.DXT5),
                //BC3_UNORM_SRGB => (BC3_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaS3tcDxt5Ext, TextureGLFormat.CompressedSrgbAlphaS3tcDxt5Ext, TextureUnityFormat.Unknown, TextureUnrealFormat.Unknown),
                //BC4_UNORM => (BC4_UNORM, 8, TextureGLFormat.CompressedRedRgtc1, TextureGLFormat.CompressedRedRgtc1, TextureUnityFormat.BC4, TextureUnrealFormat.BC4),
                //BC4_SNORM => (BC4_SNORM, 8, TextureGLFormat.CompressedSignedRedRgtc1, TextureGLFormat.CompressedSignedRedRgtc1, TextureUnityFormat.BC4, TextureUnrealFormat.BC4),
                //BC5_UNORM => (BC5_UNORM, 16, TextureGLFormat.CompressedRgRgtc2, TextureGLFormat.CompressedRgRgtc2, TextureUnityFormat.BC5, TextureUnrealFormat.BC5),
                //BC5_SNORM => (BC5_SNORM, 16, TextureGLFormat.CompressedSignedRgRgtc2, TextureGLFormat.CompressedSignedRgRgtc2, TextureUnityFormat.BC5, TextureUnrealFormat.BC5),
                //BC6H_UF16 => (BC6H_UF16, 16, TextureGLFormat.CompressedRgbBptcUnsignedFloat, TextureGLFormat.CompressedRgbBptcUnsignedFloat, TextureUnityFormat.BC6H, TextureUnrealFormat.BC6H),
                //BC6H_SF16 => (BC6H_SF16, 16, TextureGLFormat.CompressedRgbBptcSignedFloat, TextureGLFormat.CompressedRgbBptcSignedFloat, TextureUnityFormat.BC6H, TextureUnrealFormat.BC6H),
                //BC7_UNORM => (BC7_UNORM, 16, TextureGLFormat.CompressedRgbaBptcUnorm, TextureGLFormat.CompressedRgbaBptcUnorm, TextureUnityFormat.BC7, TextureUnrealFormat.BC7),
                //BC7_UNORM_SRGB => (BC7_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaBptcUnorm, TextureGLFormat.CompressedSrgbAlphaBptcUnorm, TextureUnityFormat.BC7, TextureUnrealFormat.BC7),
                //R8_UNORM => (R8_UNORM, 1, (TextureGLFormat.R8, TextureGLPixelFormat.Red, TextureGLPixelType.Byte), (TextureGLFormat.R8, TextureGLPixelFormat.Red, TextureGLPixelType.Byte), TextureUnityFormat.R8, TextureUnrealFormat.R8), //: guess
                BC1_UNORM => (BC1_UNORM, 8, (TextureFormat.DXT1, TexturePixel.Unknown)),
                BC1_UNORM_SRGB => (BC1_UNORM_SRGB, 8, (TextureFormat.DXT1, TexturePixel.Signed)),
                BC2_UNORM => (BC2_UNORM, 16, (TextureFormat.DXT3, TexturePixel.Unknown)),
                BC2_UNORM_SRGB => (BC2_UNORM_SRGB, 16, (TextureFormat.DXT3, TexturePixel.Signed)),
                BC3_UNORM => (BC3_UNORM, 16, (TextureFormat.DXT5, TexturePixel.Unknown)),
                BC3_UNORM_SRGB => (BC3_UNORM_SRGB, 16, (TextureFormat.DXT5, TexturePixel.Signed)),
                BC4_UNORM => (BC4_UNORM, 8, (TextureFormat.BC4, TexturePixel.Unknown)),
                BC4_SNORM => (BC4_SNORM, 8, (TextureFormat.BC4, TexturePixel.Signed)),
                BC5_UNORM => (BC5_UNORM, 16, (TextureFormat.BC5, TexturePixel.Unknown)),
                BC5_SNORM => (BC5_SNORM, 16, (TextureFormat.BC5, TexturePixel.Signed)),
                BC6H_UF16 => (BC6H_UF16, 16, (TextureFormat.BC6H, TexturePixel.Unknown)),
                BC6H_SF16 => (BC6H_SF16, 16, (TextureFormat.BC6H, TexturePixel.Signed)),
                BC7_UNORM => (BC7_UNORM, 16, (TextureFormat.BC5, TexturePixel.Unknown)),
                BC7_UNORM_SRGB => (BC7_UNORM_SRGB, 16, (TextureFormat.BC5, TexturePixel.Signed)),
                R8_UNORM => (R8_UNORM, 1, (TextureFormat.R8, TexturePixel.Unknown)),
                _ => throw new ArgumentOutOfRangeException(nameof(headerDxt10.Value.dxgiFormat), $"{headerDxt10?.dxgiFormat}"),
            },
            // BC4U/BC4S/ATI2/BC55/R8G8_B8G8/G8R8_G8B8/UYVY-packed/YUY2-packed unsupported
            _ => throw new ArgumentOutOfRangeException(nameof(ddspf.dwFourCC), $"{ddspf.dwFourCC}"),
        };
        return (header, headerDxt10, format, r.ReadToEnd());
    }

    public static void Write(BinaryWriter w, DDS_HEADER header, DDS_HEADER_DXT10? headerDxt10, byte[] bytes, bool writeMagic = true)
    {
        header.Verify();
        if (writeMagic) w.Write(MAGIC);
        w.WriteS(header);
        if (header.ddspf.dwFourCC == DX10)
        {
            if (headerDxt10 == null) throw new ArgumentNullException(nameof(headerDxt10));
            w.WriteS(headerDxt10.Value);
        }
        w.Write(bytes);
    }

    //static (object type, int blockSize, object gl, object vulken, object unity, object unreal) MakeFormat(ref DDS_PIXELFORMAT f) =>
    //    ("Raw", (int)f.dwRGBBitCount >> 2,
    //    (TextureGLFormat.Rgba, TextureGLPixelFormat.Rgba, TextureGLPixelType.Byte),
    //    (TextureGLFormat.Rgba, TextureGLPixelFormat.Rgba, TextureGLPixelType.Byte),
    //    TextureUnityFormat.RGBA32,
    //    TextureUnrealFormat.R8G8B8A8);
    static (object type, int blockSize, object value) MakeFormat(ref DDS_PIXELFORMAT f) => ("Raw", (int)f.dwRGBBitCount >> 2, (TextureFormat.RGBA32, TexturePixel.Unknown));
}

public static unsafe class TextureConvert
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Dxt3BlockToDtx5Block(byte* p) { byte a0 = p[0], a1 = p[1], a2 = p[1], a3 = p[1], a4 = p[1], a5 = p[1], a6 = p[1], a7 = p[1]; }

    public static void Dxt3ToDtx5(byte[] data, int width, int height, int mipMaps)
    {
        fixed (byte* data_ = data)
        {
            var p = data_;
            var count = ((width + 3) / 4) * ((height + 3) / 4);
            while (count-- != 0) Dxt3BlockToDtx5Block(p += 16);
            //int blockCountX = (width + 3) / 4, blockCountY = (height + 3) / 4;
            //for (var y = 0; y < blockCountY; y++) for (var x = 0; x < blockCountX; x++) Dxt3BlockToDtx5Block(p += 16);
        }
    }
}

#endregion

#region DXGI_FORMAT
// https://docs.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format

/// <summary>
/// DirectX Graphics Infrastructure formats
/// </summary>
public enum DXGI_FORMAT : uint
{
    /// <summary>
    /// The format is not known.
    /// </summary>
    UNKNOWN = 0,
    /// <summary>
    /// A four-component, 128-bit typeless format that supports 32 bits per channel including alpha.
    /// </summary>
    R32G32B32A32_TYPELESS = 1,
    /// <summary>
    /// A four-component, 128-bit floating-point format that supports 32 bits per channel including alpha.
    /// </summary>
    R32G32B32A32_FLOAT = 2,
    /// <summary>
    /// A four-component, 128-bit unsigned-integer format that supports 32 bits per channel including alpha.
    /// </summary>
    R32G32B32A32_UINT = 3,
    /// <summary>
    /// A four-component, 128-bit signed-integer format that supports 32 bits per channel including alpha.
    /// </summary>
    R32G32B32A32_SINT = 4,
    /// <summary>
    /// A three-component, 96-bit typeless format that supports 32 bits per color channel.
    /// </summary>
    R32G32B32_TYPELESS = 5,
    /// <summary>
    /// A three-component, 96-bit floating-point format that supports 32 bits per color channel.
    /// </summary>
    R32G32B32_FLOAT = 6,
    /// <summary>
    /// A three-component, 96-bit unsigned-integer format that supports 32 bits per color channel.
    /// </summary>
    R32G32B32_UINT = 7,
    /// <summary>
    /// A three-component, 96-bit signed-integer format that supports 32 bits per color channel.
    /// </summary>
    R32G32B32_SINT = 8,
    /// <summary>
    /// A four-component, 64-bit typeless format that supports 16 bits per channel including alpha.
    /// </summary>
    R16G16B16A16_TYPELESS = 9,
    /// <summary>
    /// A four-component, 64-bit floating-point format that supports 16 bits per channel including alpha.
    /// </summary>
    R16G16B16A16_FLOAT = 10,
    /// <summary>
    /// A four-component, 64-bit unsigned-normalized-integer format that supports 16 bits per channel including alpha.
    /// </summary>
    R16G16B16A16_UNORM = 11,
    /// <summary>
    /// A four-component, 64-bit unsigned-integer format that supports 16 bits per channel including alpha.
    /// </summary>
    R16G16B16A16_UINT = 12,
    /// <summary>
    /// A four-component, 64-bit signed-normalized-integer format that supports 16 bits per channel including alpha.
    /// </summary>
    R16G16B16A16_SNORM = 13,
    /// <summary>
    /// A four-component, 64-bit signed-integer format that supports 16 bits per channel including alpha.
    /// </summary>
    R16G16B16A16_SINT = 14,
    /// <summary>
    /// A two-component, 64-bit typeless format that supports 32 bits for the red channel and 32 bits for the green channel.
    /// </summary>
    R32G32_TYPELESS = 15,
    /// <summary>
    /// A two-component, 64-bit floating-point format that supports 32 bits for the red channel and 32 bits for the green channel.
    /// </summary>
    R32G32_FLOAT = 16,
    /// <summary>
    /// A two-component, 64-bit unsigned-integer format that supports 32 bits for the red channel and 32 bits for the green channel.
    /// </summary>
    R32G32_UINT = 17,
    /// <summary>
    /// A two-component, 64-bit signed-integer format that supports 32 bits for the red channel and 32 bits for the green channel.
    /// </summary>
    R32G32_SINT = 18,
    /// <summary>
    /// A two-component, 64-bit typeless format that supports 32 bits for the red channel, 8 bits for the green channel, and 24 bits are unused.
    /// </summary>
    R32G8X24_TYPELESS = 19,
    /// <summary>
    /// A 32-bit floating-point component, and two unsigned-integer components (with an additional 32 bits). This format supports 32-bit depth, 8-bit stencil, and 24 bits are unused.
    /// </summary>
    D32_FLOAT_S8X24_UINT = 20,
    /// <summary>
    /// A 32-bit floating-point component, and two typeless components (with an additional 32 bits). This format supports 32-bit red channel, 8 bits are unused, and 24 bits are unused.
    /// </summary>
    R32_FLOAT_X8X24_TYPELESS = 21,
    /// <summary>
    /// A 32-bit typeless component, and two unsigned-integer components (with an additional 32 bits). This format has 32 bits unused, 8 bits for green channel, and 24 bits are unused.
    /// </summary>
    X32_TYPELESS_G8X24_UINT = 22,
    /// <summary>
    /// A four-component, 32-bit typeless format that supports 10 bits for each color and 2 bits for alpha.
    /// </summary>
    R10G10B10A2_TYPELESS = 23,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized-integer format that supports 10 bits for each color and 2 bits for alpha.
    /// </summary>
    R10G10B10A2_UNORM = 24,
    /// <summary>
    /// A four-component, 32-bit unsigned-integer format that supports 10 bits for each color and 2 bits for alpha.
    /// </summary>
    R10G10B10A2_UINT = 25,
    /// <summary>
    /// Three partial-precision floating-point numbers encoded into a single 32-bit value (a variant of s10e5, which is sign bit, 10-bit mantissa, and 5-bit biased (15) exponent).
    /// There are no sign bits, and there is a 5-bit biased (15) exponent for each channel, 6-bit mantissa for R and G, and a 5-bit mantissa for B.
    /// </summary>
    R11G11B10_FLOAT = 26,
    /// <summary>
    /// A four-component, 32-bit typeless format that supports 8 bits per channel including alpha.
    /// </summary>
    R8G8B8A8_TYPELESS = 27,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits per channel including alpha.
    /// </summary>
    R8G8B8A8_UNORM = 28,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized integer sRGB format that supports 8 bits per channel including alpha.
    /// </summary>
    R8G8B8A8_UNORM_SRGB = 29,
    /// <summary>
    /// A four-component, 32-bit unsigned-integer format that supports 8 bits per channel including alpha.
    /// </summary>
    R8G8B8A8_UINT = 30,
    /// <summary>
    /// A four-component, 32-bit signed-normalized-integer format that supports 8 bits per channel including alpha.
    /// </summary>
    R8G8B8A8_SNORM = 31,
    /// <summary>
    /// A four-component, 32-bit signed-integer format that supports 8 bits per channel including alpha.
    /// </summary>
    R8G8B8A8_SINT = 32,
    /// <summary>
    /// A two-component, 32-bit typeless format that supports 16 bits for the red channel and 16 bits for the green channel.
    /// </summary>
    R16G16_TYPELESS = 33,
    /// <summary>
    /// A two-component, 32-bit floating-point format that supports 16 bits for the red channel and 16 bits for the green channel.
    /// </summary>
    R16G16_FLOAT = 34,
    /// <summary>
    /// A two-component, 32-bit unsigned-normalized-integer format that supports 16 bits each for the green and red channels.
    /// </summary>
    R16G16_UNORM = 35,
    /// <summary>
    /// A two-component, 32-bit unsigned-integer format that supports 16 bits for the red channel and 16 bits for the green channel.
    /// </summary>
    R16G16_UINT = 36,
    /// <summary>
    /// A two-component, 32-bit signed-normalized-integer format that supports 16 bits for the red channel and 16 bits for the green channel.
    /// </summary>
    R16G16_SNORM = 37,
    /// <summary>
    /// A two-component, 32-bit signed-integer format that supports 16 bits for the red channel and 16 bits for the green channel.
    /// </summary>
    R16G16_SINT = 38,
    /// <summary>
    /// A single-component, 32-bit typeless format that supports 32 bits for the red channel.
    /// </summary>
    R32_TYPELESS = 39,
    /// <summary>
    /// A single-component, 32-bit floating-point format that supports 32 bits for depth.
    /// </summary>
    D32_FLOAT = 40,
    /// <summary>
    /// A single-component, 32-bit floating-point format that supports 32 bits for the red channel.
    /// </summary>
    R32_FLOAT = 41,
    /// <summary>
    /// A single-component, 32-bit unsigned-integer format that supports 32 bits for the red channel.
    /// </summary>
    R32_UINT = 42,
    /// <summary>
    /// A single-component, 32-bit signed-integer format that supports 32 bits for the red channel.
    /// </summary>
    R32_SINT = 43,
    /// <summary>
    /// A two-component, 32-bit typeless format that supports 24 bits for the red channel and 8 bits for the green channel.
    /// </summary>
    R24G8_TYPELESS = 44,
    /// <summary>
    /// A 32-bit z-buffer format that supports 24 bits for depth and 8 bits for stencil.
    /// </summary>
    D24_UNORM_S8_UINT = 45,
    /// <summary>
    /// A 32-bit format, that contains a 24 bit, single-component, unsigned-normalized integer, with an additional typeless 8 bits. This format has 24 bits red channel and 8 bits unused.
    /// </summary>
    R24_UNORM_X8_TYPELESS = 46,
    /// <summary>
    /// A 32-bit format, that contains a 24 bit, single-component, typeless format, with an additional 8 bit unsigned integer component. This format has 24 bits unused and 8 bits green channel.
    /// </summary>
    X24_TYPELESS_G8_UINT = 47,
    /// <summary>
    /// A two-component, 16-bit typeless format that supports 8 bits for the red channel and 8 bits for the green channel.
    /// </summary>
    R8G8_TYPELESS = 48,
    /// <summary>
    /// A two-component, 16-bit unsigned-normalized-integer format that supports 8 bits for the red channel and 8 bits for the green channel.
    /// </summary>
    R8G8_UNORM = 49,
    /// <summary>
    /// A two-component, 16-bit unsigned-integer format that supports 8 bits for the red channel and 8 bits for the green channel.
    /// </summary>
    R8G8_UINT = 50,
    /// <summary>
    /// A two-component, 16-bit signed-normalized-integer format that supports 8 bits for the red channel and 8 bits for the green channel.
    /// </summary>
    R8G8_SNORM = 51,
    /// <summary>
    /// A two-component, 16-bit signed-integer format that supports 8 bits for the red channel and 8 bits for the green channel.
    /// </summary>
    R8G8_SINT = 52,
    /// <summary>
    /// A single-component, 16-bit typeless format that supports 16 bits for the red channel.
    /// </summary>
    R16_TYPELESS = 53,
    /// <summary>
    /// A single-component, 16-bit floating-point format that supports 16 bits for the red channel.
    /// </summary>
    R16_FLOAT = 54,
    /// <summary>
    /// A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for depth.
    /// </summary>
    D16_UNORM = 55,
    /// <summary>
    /// A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for the red channel.
    /// </summary>
    R16_UNORM = 56,
    /// <summary>
    /// A single-component, 16-bit unsigned-integer format that supports 16 bits for the red channel.
    /// </summary>
    R16_UINT = 57,
    /// <summary>
    /// A single-component, 16-bit signed-normalized-integer format that supports 16 bits for the red channel.
    /// </summary>
    R16_SNORM = 58,
    /// <summary>
    /// A single-component, 16-bit signed-integer format that supports 16 bits for the red channel.
    /// </summary>
    R16_SINT = 59,
    /// <summary>
    /// A single-component, 8-bit typeless format that supports 8 bits for the red channel.
    /// </summary>
    R8_TYPELESS = 60,
    /// <summary>
    /// A single-component, 8-bit unsigned-normalized-integer format that supports 8 bits for the red channel.
    /// </summary>
    R8_UNORM = 61,
    /// <summary>
    /// A single-component, 8-bit unsigned-integer format that supports 8 bits for the red channel.
    /// </summary>
    R8_UINT = 62,
    /// <summary>
    /// A single-component, 8-bit signed-normalized-integer format that supports 8 bits for the red channel.
    /// </summary>
    R8_SNORM = 63,
    /// <summary>
    /// A single-component, 8-bit signed-integer format that supports 8 bits for the red channel.
    /// </summary>
    R8_SINT = 64,
    /// <summary>
    /// A single-component, 8-bit unsigned-normalized-integer format for alpha only.
    /// </summary>
    A8_UNORM = 65,
    /// <summary>
    /// A single-component, 1-bit unsigned-normalized integer format that supports 1 bit for the red channel.
    /// </summary>
    R1_UNORM = 66,
    /// <summary>
    /// Three partial-precision floating-point numbers encoded into a single 32-bit value all sharing the same 5-bit exponent (variant of s10e5, which is sign bit, 10-bit mantissa, and 5-bit biased (15) exponent).
    /// There is no sign bit, and there is a shared 5-bit biased (15) exponent and a 9-bit mantissa for each channel.
    /// </summary>
    R9G9B9E5_SHAREDEXP = 67,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized-integer format. This packed RGB format is analogous to the UYVY format. Each 32-bit block describes a pair of pixels: (R8, G8, B8) and (R8, G8, B8) where the R8/B8 values are repeated, and the G8 values are unique to each pixel.
    /// Width must be even.
    /// </summary>
    R8G8_B8G8_UNORM = 68,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized-integer format. This packed RGB format is analogous to the YUY2 format. Each 32-bit block describes a pair of pixels: (R8, G8, B8) and (R8, G8, B8) where the R8/B8 values are repeated, and the G8 values are unique to each pixel.
    /// Width must be even.
    /// </summary>
    G8R8_G8B8_UNORM = 69,
    /// <summary>
    /// Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC1_TYPELESS = 70,
    /// <summary>
    /// Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC1_UNORM = 71,
    /// <summary>
    /// Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC1_UNORM_SRGB = 72,
    /// <summary>
    /// Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC2_TYPELESS = 73,
    /// <summary>
    /// Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC2_UNORM = 74,
    /// <summary>
    /// Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC2_UNORM_SRGB = 75,
    /// <summary>
    /// Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC3_TYPELESS = 76,
    /// <summary>
    /// Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC3_UNORM = 77,
    /// <summary>
    /// Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC3_UNORM_SRGB = 78,
    /// <summary>
    /// One-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC4_TYPELESS = 79,
    /// <summary>
    /// One-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC4_UNORM = 80,
    /// <summary>
    /// One-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC4_SNORM = 81,
    /// <summary>
    /// Two-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC5_TYPELESS = 82,
    /// <summary>
    /// Two-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC5_UNORM = 83,
    /// <summary>
    /// Two-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC5_SNORM = 84,
    /// <summary>
    /// A three-component, 16-bit unsigned-normalized-integer format that supports 5 bits for blue, 6 bits for green, and 5 bits for red.
    /// </summary>
    B5G6R5_UNORM = 85,
    /// <summary>
    /// A four-component, 16-bit unsigned-normalized-integer format that supports 5 bits for each color channel and 1-bit alpha.
    /// </summary>
    B5G5R5A1_UNORM = 86,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8-bit alpha.
    /// </summary>
    B8G8R8A8_UNORM = 87,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8 bits unused.
    /// </summary>
    B8G8R8X8_UNORM = 88,
    /// <summary>
    /// A four-component, 32-bit 2.8-biased fixed-point format that supports 10 bits for each color channel and 2-bit alpha.
    /// </summary>
    R10G10B10_XR_BIAS_A2_UNORM = 89,
    /// <summary>
    /// A four-component, 32-bit typeless format that supports 8 bits for each channel including alpha.
    /// </summary>
    B8G8R8A8_TYPELESS = 90,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each channel including alpha.
    /// </summary>
    B8G8R8A8_UNORM_SRGB = 91,
    /// <summary>
    /// A four-component, 32-bit typeless format that supports 8 bits for each color channel, and 8 bits are unused.
    /// </summary>
    B8G8R8X8_TYPELESS = 92,
    /// <summary>
    /// A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each color channel, and 8 bits are unused.
    /// </summary>
    B8G8R8X8_UNORM_SRGB = 93,
    /// <summary>
    /// A typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC6H_TYPELESS = 94,
    /// <summary>
    /// A block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC6H_UF16 = 95,
    /// <summary>
    /// A block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC6H_SF16 = 96,
    /// <summary>
    /// A typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC7_TYPELESS = 97,
    /// <summary>
    /// A block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC7_UNORM = 98,
    /// <summary>
    /// A block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    /// </summary>
    BC7_UNORM_SRGB = 99,
    /// <summary>
    /// Most common YUV 4:4:4 video resource format.
    /// </summary>
    AYUV = 100,
    /// <summary>
    /// 10-bit per channel packed YUV 4:4:4 video resource format.
    /// </summary>
    Y410 = 101,
    /// <summary>
    /// 16-bit per channel packed YUV 4:4:4 video resource format.
    /// </summary>
    Y416 = 102,
    /// <summary>
    /// Most common YUV 4:2:0 video resource format.
    /// </summary>
    NV12 = 103,
    /// <summary>
    /// 10-bit per channel planar YUV 4:2:0 video resource format.
    /// </summary>
    P010 = 104,
    /// <summary>
    /// 16-bit per channel planar YUV 4:2:0 video resource format.
    /// </summary>
    P016 = 105,
    /// <summary>
    /// 8-bit per channel planar YUV 4:2:0 video resource format.
    /// </summary>
    _420_OPAQUE = 106,
    /// <summary>
    /// Most common YUV 4:2:2 video resource format.
    /// </summary>
    YUY2 = 107,
    /// <summary>
    /// 10-bit per channel packed YUV 4:2:2 video resource format.
    /// </summary>
    Y210 = 108,
    /// <summary>
    /// 16-bit per channel packed YUV 4:2:2 video resource format.
    /// </summary>
    Y216 = 109,
    /// <summary>
    /// Most common planar YUV 4:1:1 video resource format.
    /// </summary>
    NV11 = 110,
    /// <summary>
    /// 4-bit palletized YUV format that is commonly used for DVD subpicture.
    /// </summary>
    AI44 = 111,
    /// <summary>
    /// 4-bit palletized YUV format that is commonly used for DVD subpicture.
    /// </summary>
    IA44 = 112,
    /// <summary>
    /// 8-bit palletized format that is used for palletized RGB data when the processor processes ISDB-T data and for palletized YUV data when the processor processes BluRay data.
    /// </summary>
    P8 = 113,
    /// <summary>
    /// 8-bit palletized format with 8 bits of alpha that is used for palletized YUV data when the processor processes BluRay data.
    /// </summary>
    A8P8 = 114,
    /// <summary>
    /// A four-component, 16-bit unsigned-normalized integer format that supports 4 bits for each channel including alpha.
    /// </summary>
    B4G4R4A4_UNORM = 115,
    /// <summary>
    /// A video format; an 8-bit version of a hybrid planar 4:2:2 format.
    /// </summary>
    P208 = 130,
    /// <summary>
    /// An 8 bit YCbCrA 4:4 rendering format.
    /// </summary>
    V208 = 131,
    /// <summary>
    /// An 8 bit YCbCrA 4:4:4:4 rendering format.
    /// </summary>
    V408 = 132,
    /// <summary>
    /// SAMPLER_FEEDBACK_MIN_MIP_OPAQUE
    /// </summary>
    SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 133,
    /// <summary>
    /// SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE
    /// </summary>
    SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 134,
}

#endregion

#region LEGACY
#if false
/// <summary>
/// TextureUnityFormat
/// </summary>
public enum TextureUnityFormat : short
{
    Unknown = 0,
    ATC_RGB4 = -127,
    ATC_RGBA8 = -127,
    PVRTC_2BPP_RGB = -127,
    PVRTC_2BPP_RGBA = -127,
    PVRTC_4BPP_RGB = -127,
    PVRTC_4BPP_RGBA = -127,
    Alpha8 = 1,
    ARGB4444 = 2,
    RGB24 = 3,
    RGBA32 = 4,
    ARGB32 = 5,
    RGB565 = 7,
    R16 = 9,
    DXT1 = 10,
    DXT3_POLYFILL = 11,
    DXT5 = 12,
    RGBA4444 = 13,
    BGRA32 = 14,
    RHalf = 15,
    RGHalf = 16,
    RGBAHalf = 17,
    RFloat = 18,
    RGFloat = 19,
    RGBAFloat = 20,
    YUY2 = 21,
    RGB9e5Float = 22,
    BC6H = 24,
    BC7 = 25,
    BC4 = 26,
    BC5 = 27,
    DXT1Crunched = 28,
    DXT5Crunched = 29,
    PVRTC_RGB2 = 30,
    PVRTC_RGBA2 = 31,
    PVRTC_RGB4 = 32,
    PVRTC_RGBA4 = 33,
    ETC_RGB4 = 34,
    EAC_R = 41,
    EAC_R_SIGNED = 42,
    EAC_RG = 43,
    EAC_RG_SIGNED = 44,
    ETC2_RGB = 45,
    ETC2_RGBA1 = 46,
    ETC2_RGBA8 = 47,
    ASTC_4x4 = 48,
    ASTC_RGB_4x4 = 48,
    ASTC_5x5 = 49,
    ASTC_RGB_5x5 = 49,
    ASTC_6x6 = 50,
    ASTC_RGB_6x6 = 50,
    ASTC_8x8 = 51,
    ASTC_RGB_8x8 = 51,
    ASTC_10x10 = 52,
    ASTC_RGB_10x10 = 52,
    ASTC_12x12 = 53,
    ASTC_RGB_12x12 = 53,
    ASTC_RGBA_4x4 = 54,
    ASTC_RGBA_5x5 = 55,
    ASTC_RGBA_6x6 = 56,
    ASTC_RGBA_8x8 = 57,
    ASTC_RGBA_10x10 = 58,
    ASTC_RGBA_12x12 = 59,
    ETC_RGB4_3DS = 60,
    ETC_RGBA8_3DS = 61,
    RG16 = 62,
    R8 = 63,
    ETC_RGB4Crunched = 64,
    ETC2_RGBA8Crunched = 65,
    ASTC_HDR_4x4 = 66,
    ASTC_HDR_5x5 = 67,
    ASTC_HDR_6x6 = 68,
    ASTC_HDR_8x8 = 69,
    ASTC_HDR_10x10 = 70,
    ASTC_HDR_12x12 = 71
}

/// <summary>
/// TextureUnrealFormat
/// </summary>
public enum TextureUnrealFormat : int
{
    /// <summary/>
    Unknown = 0,
    /// <summary/>
    A32B32G32R32F = 1,
    /// <summary/>
    B8G8R8A8 = 2,
    /// <summary>G8 means Gray/Grey, not Green, typically actually uses a red format with replication of R to RGB<summary/>
    G8 = 3,
    /// <summary>G16 means Gray/Grey like G8</summary>
    G16 = 4,
    /// <summary/>
    DXT1 = 5,
    /// <summary/>
    DXT3 = 6,
    /// <summary/>
    DXT5 = 7,
    /// <summary/>
    UYVY = 8,
    /// <summary>16F</summary>
    FloatRGB = 9,
    /// <summary>16F</summary>
    FloatRGBA = 10,
    /// <summary/>
    DepthStencil = 11,
    /// <summary/>
    ShadowDepth = 12,
    /// <summary/>
    R32Float = 13,
    /// <summary/>
    G16R16 = 14,
    /// <summary/>
    G16R16F = 15,
    /// <summary/>
    G16R16FFilter = 16,
    /// <summary/>
    G32R32F = 17,
    /// <summary/>
    A2B10G10R10 = 18,
    /// <summary/>
    A16B16G16R16 = 19,
    /// <summary/>
    D24 = 20,
    /// <summary/>
    R16F = 21,
    /// <summary/>
    R16FFilter = 22,
    /// <summary/>
    BC5 = 23,
    /// <summary/>
    V8U8 = 24,
    /// <summary/>
    A1 = 25,
    /// <summary/>
    FloatR11G11B10 = 26,
    /// <summary/>
    A8 = 27,
    /// <summary/>
    R32UInt = 28,
    /// <summary/>
    R32SInt = 29,
    /// <summary/>
    PVRTC2 = 30,
    /// <summary/>
    PVRTC4 = 31,
    /// <summary/>
    R16UInt = 32,
    /// <summary/>
    R16SInt = 33,
    /// <summary/>
    R16G16B16A16UInt = 34,
    /// <summary/>
    R16G16B16A16SInt = 35,
    /// <summary/>
    R5G6B5UNorm = 36,
    /// <summary/>
    R8G8B8A8 = 37,
    /// <summary>Only used for legacy loading; do NOT us</summary>
    A8R8G8B8 = 38,
    /// <summary/>
    BC4 = 39,
    /// <summary/>
    R8G8 = 40,
    /// <summary>Unsupported Format</summary>
    ATCRGB = 41,
    /// <summary>Unsupported Format</summary>
    ATCRGBAE = 42,
    /// <summary/>
    ATCRGBAI = 43,
    /// <summary>Used for creating SRVs to alias a DepthStencil buffer to read Stencil. Don't use for creating textures</summary>
    X24G8 = 44,
    /// <summary>Unsupported Format</summary>
    ETC1 = 45,
    /// <summary/>
    ETC2RGB = 46,
    /// <summary/>
    ETC2RGBA = 47,
    /// <summary/>
    R32G32B32A32UInt = 48,
    /// <summary/>
    R16G16UInt = 49,
    /// <summary>8.00 bpp</summary>
    ASTC4x4 = 50,
    /// <summary>3.56 bpp</summary>
    ASTC6x6 = 51,
    /// <summary>2.00 bpp</summary>
    ASTC8x8 = 52,
    /// <summary>1.28 bpp</summary>
    ASTC10x10 = 53,
    /// <summary>0.89 bpp</summary>
    ASTC12x12 = 54,
    /// <summary/>
    BC6H = 55,
    /// <summary/>
    BC7 = 56,
    /// <summary/>
    R8UInt = 57,
    /// <summary/>
    L8 = 58,
    /// <summary/>
    XGXR8 = 59,
    /// <summary/>
    R8G8B8A8UInt = 60,
    /// <summary/>
    R8G8B8A8SNorm = 61,
    /// <summary/>
    R16G16B16A16UNorm = 62,
    /// <summary/>
    R16G16B16A16SNorm = 63,
    /// <summary/>
    PLATFORMHDR0 = 64,
    /// <summary>Reserved</summary>
    PLATFORMHDR1 = 65,
    /// <summary>Reserved</summary>
    PLATFORMHDR2 = 66,
    /// <summary/>
    NV12 = 67,
    /// <summary/>
    R32G32UInt = 68,
    /// <summary/>
    ETC2R11EAC = 69,
    /// <summary/>
    ETC2RG11EAC = 70,
    /// <summary/>
    R8 = 71,
    /// <summary/>
    B5G5R5A1UNorm = 72,
    /// <summary/>
    ASTC4x4HDR = 73,
    /// <summary/>
    ASTC6x6HDR = 74,
    /// <summary/>
    ASTC8x8HDR = 75,
    /// <summary/>
    ASTC10x10HDR = 76,
    /// <summary/>
    ASTC12x12HDR = 77,
    /// <summary/>
    G16R16SNorm = 78,
    /// <summary/>
    R8G8UInt = 79,
    /// <summary/>
    R32G32B32UInt = 80,
    /// <summary/>
    R32G32B32SInt = 81,
    /// <summary/>
    R32G32B32F = 82,
    /// <summary/>
    R8SInt = 83,
    /// <summary/>
    R64UInt = 84,
    /// <summary/>
    R9G9B9EXP5 = 85,
    /// <summary/>
    P010 = 86,
    /// <summary/>
    MAX = 87,
}

/// <summary>
/// TextureGLFormat
/// </summary>
public enum TextureGLFormat
{
    /// <summary>
    /// GL_DEPTH_COMPONENT = 0x1902
    /// </summary>
    DepthComponent = 0x1902,
    /// <summary>
    /// GL_RED = 0x1903 (compress only)
    /// </summary>
    Red = 0x1903,
    /// <summary>
    /// GL_RED_EXT = 0x1903 (dup, compress only)
    /// </summary>
    RedExt = 0x1903,
    /// <summary>
    /// GL_ALPHA = 0x1906 (internal only)
    /// </summary>
    Alpha = 0x1906,
    /// <summary>
    /// GL_RGB = 0x1907
    /// </summary>
    Rgb = 0x1907,
    /// <summary>
    /// GL_RGBA = 0x1908
    /// </summary>
    Rgba = 0x1908,
    /// <summary>
    /// GL_LUMINANCE = 0x1909 (internal only)
    /// </summary>
    Luminance = 0x1909,
    /// <summary>
    /// GL_LUMINANCE_ALPHA = 0x190A (internal only)
    /// </summary>
    LuminanceAlpha = 0x190A,
    /// <summary>
    /// GL_R3_G3_B2 = 0x2A10
    /// </summary>
    R3G3B2 = 0x2A10,
    /// <summary>
    /// GL_ALPHA4 = 0x803B
    /// </summary>
    Alpha4 = 0x803B,
    /// <summary>
    /// GL_ALPHA8 = 0x803C
    /// </summary>
    Alpha8 = 0x803C,
    /// <summary>
    /// GL_ALPHA12 = 0x803D
    /// </summary>
    Alpha12 = 0x803D,
    /// <summary>
    /// GL_ALPHA16 = 0x803E
    /// </summary>
    Alpha16 = 0x803E,
    /// <summary>
    /// GL_LUMINANCE4 = 0x803F
    /// </summary>
    Luminance4 = 0x803F,
    /// <summary>
    /// GL_LUMINANCE8 = 0x8040
    /// </summary>
    Luminance8 = 0x8040,
    /// <summary>
    /// GL_LUMINANCE12 = 0x8041
    /// </summary>
    Luminance12 = 0x8041,
    /// <summary>
    /// GL_LUMINANCE16 = 0x8042
    /// </summary>
    Luminance16 = 0x8042,
    /// <summary>
    /// GL_LUMINANCE4_ALPHA4 = 0x8043
    /// </summary>
    Luminance4Alpha4 = 0x8043,
    /// <summary>
    /// GL_LUMINANCE6_ALPHA2 = 0x8044
    /// </summary>
    Luminance6Alpha2 = 0x8044,
    /// <summary>
    /// GL_LUMINANCE8_ALPHA8 = 0x8045
    /// </summary>
    Luminance8Alpha8 = 0x8045,
    /// <summary>
    /// GL_LUMINANCE12_ALPHA4 = 0x8046
    /// </summary>
    Luminance12Alpha4 = 0x8046,
    /// <summary>
    /// GL_LUMINANCE12_ALPHA12 = 0x8047
    /// </summary>
    Luminance12Alpha12 = 0x8047,
    /// <summary>
    /// GL_LUMINANCE16_ALPHA16 = 0x8048
    /// </summary>
    Luminance16Alpha16 = 0x8048,
    /// <summary>
    /// GL_INTENSITY = 0x8049
    /// </summary>
    Intensity = 0x8049,
    /// <summary>
    /// GL_INTENSITY4 = 0x804A
    /// </summary>
    Intensity4 = 0x804A,
    /// <summary>
    /// GL_INTENSITY8 = 0x804B
    /// </summary>
    Intensity8 = 0x804B,
    /// <summary>
    /// GL_INTENSITY12 = 0x804C
    /// </summary>
    Intensity12 = 0x804C,
    /// <summary>
    /// GL_INTENSITY16 = 0x804D
    /// </summary>
    Intensity16 = 0x804D,
    /// <summary>
    /// GL_RGB2_EXT = 0x804E
    /// </summary>
    Rgb2Ext = 0x804E,
    /// <summary>
    /// GL_RGB4 = 0x804F
    /// </summary>
    Rgb4 = 0x804F,
    /// <summary>
    /// GL_RGB4_EXT = 0x804F (compress only)
    /// </summary>
    Rgb4Ext = 0x804F,
    Rgb5 = 0x8050,              //: GL_RGB5 = 0x8050
    Rgb5Ext = 0x8050,           //: GL_RGB5_EXT = 0x8050 (dup, compress only)
    Rgb8 = 0x8051,              //: GL_RGB8 = 0x8051
    Rgb8Ext = 0x8051,           //: GL_RGB8_EXT = 0x8051 (dup, compress only)
    Rgb8Oes = 0x8051,           //: GL_RGB8_OES = 0x8051 (dup, compress only)
    Rgb10 = 0x8052,             //: GL_RGB10 = 0x8052
    Rgb10Ext = 0x8052,          //: GL_RGB10_EXT = 0x8052 (dup, compress only)
    Rgb12 = 0x8053,             //: GL_RGB12 = 0x8053
    Rgb12Ext = 0x8053,          //: GL_RGB12_EXT = 0x8053 (dup, compress only)
    Rgb16 = 0x8054,             //: GL_RGB16 = 0x8054
    Rgb16Ext = 0x8054,          //: GL_RGB16_EXT = 0x8054 (dup, compress only)
    Rgba2 = 0x8056,             //: GL_RGBA2 = 0x8055 (internal only)
    Rgba4 = 0x8056,             //: GL_RGBA4 = 0x8056
    Rgba4Ext = 0x8056,          //: GL_RGBA4_EXT = 0x8056 (dup, compress only)
    Rgba4Oes = 0x8056,          //: GL_RGBA4_OES = 0x8056 (dup, compress only)
    Rgb5A1 = 0x8057,            //: GL_RGB5_A1 = 0x8057
    Rgb5A1Ext = 0x8057,         //: GL_RGB5_A1_EXT = 0x8057 (dup, compress only)
    Rgb5A1Oes = 0x8057,         //: GL_RGB5_A1_OES = 0x8057 (dup, compress only)
    Rgba8 = 0x8058,             //: GL_RGBA8 = 0x8058
    Rgba8Ext = 0x8058,          //: GL_RGBA8_EXT = 0x8058 (dup, compress only)
    Rgba8Oes = 0x8058,          //: GL_RGBA8_OES = 0x8058 (dup, compress only)
    Rgb10A2 = 0x8059,           //: GL_RGB10_A2 = 0x8059
    Rgb10A2Ext = 0x8059,        //: GL_RGB10_A2_EXT = 0x8059 (dup, compress only)
    Rgba12 = 0x805A,            //: GL_RGBA12 = 0x805A
    Rgba12Ext = 0x805A,         //: GL_RGBA12_EXT = 0x805A (dup, compress only)
    Rgba16 = 0x805B,            //: GL_RGBA16 = 0x805B
    Rgba16Ext = 0x805B,         //: GL_RGBA16_EXT = 0x805B (dup, compress only)
    DualAlpha4Sgis = 0x8110,        //: GL_DUAL_ALPHA4_SGIS = 0x8110
    DualAlpha8Sgis = 0x8111,        //: GL_DUAL_ALPHA8_SGIS = 0x8111
    DualAlpha12Sgis = 0x8112,       //: GL_DUAL_ALPHA12_SGIS = 0x8112
    DualAlpha16Sgis = 0x8113,       //: GL_DUAL_ALPHA16_SGIS = 0x8113
    DualLuminance4Sgis = 0x8114,    //: GL_DUAL_LUMINANCE4_SGIS = 0x8114
    DualLuminance8Sgis = 0x8115,    //: GL_DUAL_LUMINANCE8_SGIS = 0x8115
    DualLuminance12Sgis = 0x8116,   //: GL_DUAL_LUMINANCE12_SGIS = 0x8116
    DualLuminance16Sgis = 0x8117,   //: GL_DUAL_LUMINANCE16_SGIS = 0x8117
    DualIntensity4Sgis = 0x8118,    //: GL_DUAL_INTENSITY4_SGIS = 0x8118
    DualIntensity8Sgis = 0x8119,    //: GL_DUAL_INTENSITY8_SGIS = 0x8119
    DualIntensity12Sgis = 0x811A,   //: GL_DUAL_INTENSITY12_SGIS = 0x811A
    DualIntensity16Sgis = 0x811B,   //: GL_DUAL_INTENSITY16_SGIS = 0x811B
    DualLuminanceAlpha4Sgis = 0x811C,//: GL_DUAL_LUMINANCE_ALPHA4_SGIS = 0x811C
    DualLuminanceAlpha8Sgis = 33053,//: GL_DUAL_LUMINANCE_ALPHA8_SGIS = 0x811D
    QuadAlpha4Sgis = 0x811E,        //: GL_QUAD_ALPHA4_SGIS = 0x811E
    QuadAlpha8Sgis = 0x811F,        //: GL_QUAD_ALPHA8_SGIS = 0x811F
    QuadLuminance4Sgis = 0x8120,    //: GL_QUAD_LUMINANCE4_SGIS = 0x8120
    QuadLuminance8Sgis = 0x8121,    //: GL_QUAD_LUMINANCE8_SGIS = 0x8121
    QuadIntensity4Sgis = 0x8122,    //: GL_QUAD_INTENSITY4_SGIS = 0x8122
    QuadIntensity8Sgis = 0x8123,    //: GL_QUAD_INTENSITY8_SGIS = 0x8123
    DepthComponent16 = 0x81A5,      //: GL_DEPTH_COMPONENT16 = 0x81A5
    DepthComponent16Arb = 0x81A5,   //: GL_DEPTH_COMPONENT16_ARB = 0x81A5 (dup, compress only)
    DepthComponent16Oes = 0x81A5,   //: GL_DEPTH_COMPONENT16_OES = 0x81A5 (dup, compress only)
    DepthComponent16Sgix = 0x81A5,  //: GL_DEPTH_COMPONENT16_SGIX = 0x81A5 (dup)
    DepthComponent24 = 0x81A6,      //: GL_DEPTH_COMPONENT24 = 0x81A6 (internal only)
    DepthComponent24Arb = 0x81A6,   //: GL_DEPTH_COMPONENT24_ARB = 0x81A6 (dup, compress only)
    DepthComponent24Oes = 0x81A6,   //: GL_DEPTH_COMPONENT24_OES = 0x81A6 (dup, compress only)
    DepthComponent24Sgix = 0x81A6,  //: GL_DEPTH_COMPONENT24_SGIX = 0x81A6
    DepthComponent32 = 0x81A7,      //: GL_DEPTH_COMPONENT32 = 0x81A7 (internal only)
    DepthComponent32Arb = 0x81A7,   //: GL_DEPTH_COMPONENT32_ARB = 0x81A7 (dup, compress only)
    DepthComponent32Oes = 0x81A7,   //: GL_DEPTH_COMPONENT32_OES = 0x81A7 (dup, compress only)
    DepthComponent32Sgix = 0x81A7,  //: GL_DEPTH_COMPONENT32_SGIX = 0x81A7 (dup)
    CompressedRed = 0x8225,         //: GL_COMPRESSED_RED = 0x8225
    CompressedRg = 0x8226,          //: GL_COMPRESSED_RG = 0x8226
    Rg = 0x8227,                //: GL_RG = 0x8227 (compress only)
    R8 = 0x8229,                //: GL_R8 = 0x8229
    R8Ext = 0x8229,             //: GL_R8_EXT = 0x8229 (dup, compress only)
    R16 = 0x822A,               //: GL_R16 = 0x822A
    R16Ext = 0x822A,            //: GL_R16_EXT = 0x822A (dup, compress only)
    Rg8 = 0x822B,               //: GL_RG8 = 0x822B
    Rg8Ext = 0x822B,            //: GL_RG8_EXT = 0x822B (dup, compress only)
    Rg16 = 0x822C,              //: GL_RG16 = 0x822C
    Rg16Ext = 0x822C,           //: GL_RG16_EXT = 0x822C (dup, compress only)
    R16f = 0x822D,              //: GL_R16F = 0x822D
    R16fExt = 0x822D,           //: GL_R16F_EXT = 0x822D (dup, compress only)
    R32f = 0x822E,              //: GL_R32F = 0x822E
    R32fExt = 0x822E,           //: GL_R32F_EXT = 0x822E (dup, compress only)
    Rg16f = 0x822F,             //: GL_RG16F = 0x822F
    Rg16fExt = 0x822F,          //: GL_RG16F_EXT = 0x822F (dup, compress only)
    Rg32f = 0x8230,             //: GL_RG32F = 0x8230
    Rg32fExt = 0x8230,          //: GL_RG32F_EXT = 0x8230 (dup, compress only)
    R8i = 0x8231,               //: GL_R8I = 0x8231
    R8ui = 0x8232,              //: GL_R8UI = 0x8232
    R16i = 0x8233,              //: GL_R16I = 0x8233
    R16ui = 0x8234,             //: GL_R16UI = 0x8234
    R32i = 0x8235,              //: GL_R32I = 0x8235
    R32ui = 0x8236,             //: GL_R32UI = 0x8236
    Rg8i = 0x8237,              //: GL_RG8I = 0x8237
    Rg8ui = 0x8238,             //: GL_RG8UI = 0x8238
    Rg16i = 0x8239,             //: GL_RG16I = 0x8239
    Rg16ui = 0x823A,            //: GL_RG16UI = 0x823A
    Rg32i = 0x823B,             //: GL_RG32I = 0x823B
    Rg32ui = 0x823C,            //: GL_RG32UI = 0x823C
    CompressedRgbS3tcDxt1Ext = 0x83F0,//: GL_COMPRESSED_RGB_S3TC_DXT1_EXT = 0x83F0
    CompressedRgbaS3tcDxt1Ext = 0x83F1,//: GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1
    CompressedRgbaS3tcDxt3Ext = 0x83F2,//: GL_COMPRESSED_RGBA_S3TC_DXT3_EXT = 0x83F2
    CompressedRgbaS3tcDxt5Ext = 0x83F3,//: GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3
    RgbIccSgix = 0x8460, //: GL_RGB_ICC_SGIX = 0x8460 (internal only)
    RgbaIccSgix = 0x8461, //: GL_RGBA_ICC_SGIX = 0x8461 (internal only)
    AlphaIccSgix = 0x8462, //: GL_ALPHA_ICC_SGIX = 0x8462 (internal only)
    LuminanceIccSgix = 0x8460, //: GL_LUMINANCE_ICC_SGIX = 0x8463 (internal only)
    IntensityIccSgix = 0x8464, //: GL_INTENSITY_ICC_SGIX = 0x8460 (internal only)
    LuminanceAlphaIccSgix = 0x8465, //: GL_LUMINANCE_ALPHA_ICC_SGIX = 0x8465 (internal only)
    R5G6B5IccSgix = 0x8466, //: GL_R5_G6_B5_ICC_SGIX = 0x8466 (internal only)
    R5G6B5A8IccSgix = 0x8467, //: GL_R5_G6_B5_A8_ICC_SGIX = 0x8467 (internal only)
    Alpha16IccSgix = 0x8468, //: GL_RGB_ICC_SGIX = 0x8468 (internal only)
    Luminance16IccSgix = 0x8469, //: GL_LUMINANCE16_ICC_SGIX = 0x8469 (internal only)
    Intensity16IccSgix = 0x846A, //: GL_INTENSITY16_ICC_SGIX = 0x846A (internal only)
    Luminance16Alpha8IccSgix = 0x846B, //: GL_LUMINANCE16_ALPHA8_ICC_SGIX = 0x846B (internal only)
    CompressedAlpha = 0x8469, //: GL_COMPRESSED_ALPHA = 0x84E9 (internal only)
    CompressedLuminance = 0x84EA, //: GL_COMPRESSED_LUMINANCE = 0x84EA (internal only)
    CompressedLuminanceAlpha = 0x84EB, //: GL_COMPRESSED_LUMINANCE_ALPHA = 0x84EB (internal only)
    CompressedIntensity = 0x84EC, //: GL_COMPRESSED_INTENSITY = 0x84EC (internal only)
    CompressedRgb = 0x84ED,     //: GL_COMPRESSED_RGB = 0x84ED
    CompressedRgba = 0x84EE,    //: GL_COMPRESSED_RGBA = 0x84EE
    DepthStencil = 0x84F9,      //: GL_DEPTH_STENCIL = 0x84F9
    DepthStencilExt = 0x84F9,   //: GL_DEPTH_STENCIL_EXT = 0x84F9 (dup, compress only)
    DepthStencilNv = 0x84F9,    //: GL_DEPTH_STENCIL_NV = 0x84F9 (dup, compress only)
    DepthStencilOes = 0x84F9,   //: GL_DEPTH_STENCIL_OES = 0x84F9 (dup, compress only)
    DepthStencilMesa = 0x8750,  //: GL_DEPTH_STENCIL_MESA = 0x8750 (compress only)
    Rgba32f = 0x8814,           //: GL_RGBA32F = 0x8814
    Rgba32fArb = 0x8814,        //: GL_RGBA32F_ARB = 0x8814 (dup, compress only)
    Rgba32fExt = 0x8814,        //: GL_RGBA32F_EXT = 0x8814 (dup, compress only)
    Rgba16f = 0x881A,           //: GL_RGBA16F = 0x881A
    Rgba16fArb = 0x881A,        //: GL_RGBA16F_ARB = 0x881A (dup, compress only)
    Rgba16fExt = 0x881A,        //: GL_RGBA16F_EXT = 0x881A (dup, compress only)
    Rgb16f = 0x881B,            //: GL_RGB16F = 0x881B
    Rgb16fArb = 0x881B,         //: GL_RGB16F_ARB = 0x881B (dup, compress only)
    Rgb16fExt = 0x881B,         //: GL_RGB16F_EXT = 0x881B (dup, compress only)
    Depth24Stencil8 = 0x88F0,   //: GL_DEPTH24_STENCIL8 = 0x88F0
    Depth24Stencil8Ext = 0x88F0,//: GL_DEPTH24_STENCIL8_EXT = 0x88F0 (dup, compress only)
    Depth24Stencil8Oes = 0x88F0,//: GL_DEPTH24_STENCIL8_OES = 0x88F0 (dup, compress only)
    R11fG11fB10f = 0x8C3A,      //: GL_R11F_G11F_B10F = 0x8C3A
    R11fG11fB10fApple = 0x8C3A, //: GL_R11F_G11F_B10F_APPLE = 0x8C3A (dup, compress only)
    R11fG11fB10fExt = 0x8C3A,   //: GL_R11F_G11F_B10F_EXT = 0x8C3A (dup, compress only)
    Rgb9E5 = 0x8C3D,            //: GL_RGB9_E5 = 0x8C3D
    Rgb9E5Apple = 0x8C3D,       //: GL_RGB9_E5_APPLE = 0x8C3D (dup, compress only)
    Rgb9E5Ext = 0x8C3D,         //: GL_RGB9_E5_EXT = 0x8C3D (dup, compress only)
    Srgb = 0x8C40,              //: GL_SRGB = 0x8C40
    SrgbExt = 0x8C40,           //: GL_SRGB_EXT = 0x8C40 (dup, compress only)
    Srgb8 = 0x8C41,             //: GL_SRGB8 = 0x8C41
    Srgb8Ext = 0x8C41,          //: GL_SRGB8_EXT = 0x8C41 (dup, compress only)
    Srgb8Nv = 0x8C41,           //: GL_SRGB8_NV = 0x8C41 (dup, compress only)
    SrgbAlpha = 0x8C42,         //: GL_SRGB_ALPHA = 0x8C42
    SrgbAlphaExt = 0x8C42,      //: GL_SRGB_ALPHA_EXT = 0x8C42 (dup, compress only)
    Srgb8Alpha8 = 0x8C43,       //: GL_SRGB8_ALPHA8 = 0x8C43
    Srgb8Alpha8Ext = 0x8C43,    //: GL_SRGB8_ALPHA8_EXT = 0x8C43 (dup, compress only)
    SluminanceAlpha = 0x8C44,       //: GL_SLUMINANCE_ALPHA = 0x8C44 (internal only)
    Sluminance8Alpha8 = 0x8C45,       //: GL_SLUMINANCE8_ALPHA8 = 0x8C45 (internal only)
    Sluminance = 0x8C46,       //: GL_SLUMINANCE = 0x8C46 (internal only)
    Sluminance8 = 0x8C47,       //: GL_SLUMINANCE8 = 0x8C47 (internal only)
    CompressedSrgb = 0x8C48,    //: GL_COMPRESSED_SRGB = 0x8C48
    CompressedSrgbAlpha = 0x8C49,//: GL_COMPRESSED_SRGB_ALPHA = 0x8C49
    CompressedSluminance = 0x8C4A,       //: GL_COMPRESSED_SLUMINANCE = 0x8C4A (internal only)
    CompressedSluminanceAlpha = 0x8C4B,       //: GL_COMPRESSED_SLUMINANCE_ALPHA = 0x8C4B (internal only)
    CompressedSrgbS3tcDxt1Ext = 0x8C4C,//: GL_COMPRESSED_SRGB_S3TC_DXT1_EXT = 0x8C4C
    CompressedSrgbAlphaS3tcDxt1Ext = 0x8C4D,//: GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT1_EXT = 0x8C4D
    CompressedSrgbAlphaS3tcDxt3Ext = 0x8C4E,//: GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT3_EXT = 0x8C4E
    CompressedSrgbAlphaS3tcDxt5Ext = 0x8C4F,//: GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT = 0x8C4F
    DepthComponent32f = 0x8CAC, //: GL_DEPTH_COMPONENT32F = 0x8CAC
    Depth32fStencil8 = 0x8CAD,  //: GL_DEPTH32F_STENCIL8 = 0x8CAD
    Rgba32ui = 0x8D70,          //: GL_RGBA32UI = 0x8D70
    Rgb32ui = 0x8D71,           //: GL_RGB32UI = 0x8D71
    Rgba16ui = 0x8D76,          //: GL_RGBA16UI = 0x8D76
    Rgb16ui = 0x8D77,           //: GL_RGB16UI = 0x8D77
    Rgba8ui = 0x8D7C,           //: GL_RGBA8UI = 0x8D7C
    Rgb8ui = 0x8D7D,            //: GL_RGB8UI = 0x8D7D
    Rgba32i = 0x8D82,           //: GL_RGBA32I = 0x8D82
    Rgb32i = 0x8D83,            //: GL_RGB32I = 0x8D83
    Rgba16i = 0x8D88,           //: GL_RGBA16I = 0x8D88
    Rgb16i = 0x8D89,            //: GL_RGB16I = 0x8D89
    Rgba8i = 0x8D8E,            //: GL_RGBA8I = 0x8D8E
    Rgb8i = 0x8D8F,             //: GL_RGB8I = 0x8D8F
    DepthComponent32fNv = 0x8DAB,//: GL_DEPTH_COMPONENT32F_NV = 0x8DAB (compress only)
    Depth32fStencil8Nv = 0x8DAC,//: GL_DEPTH32F_STENCIL8_NV = 0x8DAC (compress only)
    Float32UnsignedInt248Rev = 0x8DAD,//: GL_FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD (internal only)
    CompressedRedRgtc1 = 0x8DBB,//: GL_COMPRESSED_RED_RGTC1 = 0x8DBB
    CompressedRedRgtc1Ext = 0x8DBB,//: GL_COMPRESSED_RED_RGTC1_EXT = 0x8DBB (dup, compress only)
    CompressedSignedRedRgtc1 = 0x8DBC,//: GL_COMPRESSED_SIGNED_RED_RGTC1 = 0x8DBC
    CompressedSignedRedRgtc1Ext = 0x8DBC, //: GL_COMPRESSED_SIGNED_RED_RGTC1_EXT = 0x8DBC (dup, compress only)
    CompressedRgRgtc2 = 0x8DBD, //: GL_COMPRESSED_RG_RGTC2 = 0x8DBD
    CompressedSignedRgRgtc2 = 0x8DBE, //: GL_COMPRESSED_SIGNED_RG_RGTC2 = 0x8DBE
    CompressedRgbaBptcUnorm = 0x8E8C, //: GL_COMPRESSED_RGBA_BPTC_UNORM = 0x8E8C
    CompressedSrgbAlphaBptcUnorm = 0x8E8D, //: GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM = 0x8E8D
    CompressedRgbBptcSignedFloat = 0x8E8E, //: GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT = 0x8E8E
    CompressedRgbBptcUnsignedFloat = 0x8E8F, //: GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT = 0x8E8F
    R8Snorm = 0x8F94,           //: GL_R8_SNORM = 0x8F94
    Rg8Snorm = 0x8F95,          //: GL_RG8_SNORM = 0x8F95
    Rgb8Snorm = 0x8F96,         //: GL_RGB8_SNORM = 0x8F96
    Rgba8Snorm = 0x8F97,        //: GL_RGBA8_SNORM = 0x8F97
    R16Snorm = 0x8F98,          //: GL_R16_SNORM = 0x8F98
    R16SnormExt = 0x8F98,       //: GL_R16_SNORM_EXT = 0x8F98 (dup, compress only)
    Rg16Snorm = 0x8F99,         //: GL_RG16_SNORM = 0x8F99
    Rg16SnormExt = 0x8F99,      //: GL_RG16_SNORM_EXT = 0x8F99 (dup, compress only)
    Rgb16Snorm = 0x8F9A,        //: GL_RGB16_SNORM = 0x8F9A
    Rgb16SnormExt = 0x8F9A,     //: GL_RGB16_SNORM_EXT = 0x8F9A (dup, compress only)
    Rgba16Snorm = 0x8F9B,        //: GL_RGBA16_SNORM = 0x8F9B (internal only)
    Rgb10A2ui = 0x906F,         //: GL_RGB10_A2UI = 0x906F (compress only)
    One = 1, //: GL_ONE = 1 (internal only)
    Two = 2, //: GL_TWO = 2 (internal only)
    Three = 3, //: GL_THREE = 3 (internal only)
    Four = 4, //: GL_FOUR = 4 (internal only)
    CompressedR11Eac = 0x9270,  //: GL_COMPRESSED_R11_EAC = 0x9270 (compress only)
    CompressedSignedR11Eac = 0x9271, //: GL_COMPRESSED_SIGNED_R11_EAC = 0x9271 (compress only)
    CompressedRg11Eac = 0x9272, //: GL_COMPRESSED_RG11_EAC = 0x9272 (compress only)
    CompressedSignedRg11Eac = 0x9273, //: GL_COMPRESSED_SIGNED_RG11_EAC = 0x9273 (compress only)
    CompressedRgb8Etc2 = 0x9274, //: GL_COMPRESSED_RGB8_ETC2 = 0x9274 (compress only)
    CompressedSrgb8Etc2 = 0x9275, //: GL_COMPRESSED_SRGB8_ETC2 = 0x9275 (compress only)
    CompressedRgb8PunchthroughAlpha1Etc2 = 0x9276, //: GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276 (compress only)
    CompressedSrgb8PunchthroughAlpha1Etc2 = 0x9277, //: GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277 (compress only)
    CompressedRgba8Etc2Eac = 0x9278, //: GL_COMPRESSED_RGBA8_ETC2_EAC = 0x9278 (compress only)
    CompressedSrgb8Alpha8Etc2Eac = 0x9279 //: GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC = 0x9279 (compress only)
}

/// <summary>
/// TextureGLPixelFormat
/// </summary>
public enum TextureGLPixelFormat
{
    Unknown = 0,
    /// <summary>
    /// GL_UNSIGNED_SHORT = 0x1403
    /// </summary>
    UnsignedShort = 0x1403,
    /// <summary>
    /// GL_UNSIGNED_INT = 0x1405
    /// </summary>
    UnsignedInt = 0x1405,
    /// <summary>
    /// GL_COLOR_INDEX = 0x1900
    /// </summary>
    ColorIndex = 0x1900,
    /// <summary>
    /// GL_STENCIL_INDEX = 0x1901
    /// </summary>
    StencilIndex = 0x1901,
    /// <summary>
    /// GL_DEPTH_COMPONENT = 0x1902
    /// </summary>
    DepthComponent = 0x1902,
    /// <summary>
    /// GL_RED = 0x1903
    /// </summary>
    Red = 0x1903,
    /// <summary>
    /// GL_RED_EXT = 0x1903
    /// </summary>
    RedExt = 0x1903,
    /// <summary>
    /// GL_GREEN = 0x1904
    /// </summary>
    Green = 0x1904,
    /// <summary>
    /// GL_BLUE = 0x1905
    /// </summary>
    Blue = 0x1905,
    /// <summary>
    /// GL_ALPHA = 0x1906
    /// </summary>
    Alpha = 0x1906,
    /// <summary>
    /// GL_RGB = 0x1907
    /// </summary>
    Rgb = 0x1907,
    /// <summary>
    /// GL_RGBA = 0x1908
    /// </summary>
    Rgba = 0x1908,
    /// <summary>
    /// GL_LUMINANCE = 0x1909
    /// </summary>
    Luminance = 0x1909,
    /// <summary>
    /// GL_LUMINANCE_ALPHA = 0x190A
    /// </summary>
    LuminanceAlpha = 0x190A,
    /// <summary>
    /// GL_ABGR_EXT = 0x8000
    /// </summary>
    AbgrExt = 0x8000,
    /// <summary>
    /// GL_CMYK_EXT = 0x800C
    /// </summary>
    CmykExt = 0x800C,
    /// <summary>
    /// GL_CMYKA_EXT = 0x800D
    /// </summary>
    CmykaExt = 0x800D,
    /// <summary>
    /// GL_BGR = 0x80E0
    /// </summary>
    Bgr = 0x80E0,
    /// <summary>
    /// GL_BGRA = 0x80E1
    /// </summary>
    Bgra = 0x80E1,
    /// <summary>
    /// GL_YCRCB_422_SGIX = 0x81BB
    /// </summary>
    Ycrcb422Sgix = 0x81BB,
    /// <summary>
    /// GL_YCRCB_444_SGIX = 0x81BC
    /// </summary>
    Ycrcb444Sgix = 0x81BC,
    /// <summary>
    /// GL_RG = 0x8227
    /// </summary>
    Rg = 0x8227,
    /// <summary>
    /// GL_RG_INTEGER = 0x8228
    /// </summary>
    RgInteger = 0x8228,
    /// <summary>
    /// GL_R5_G6_B5_ICC_SGIX = 0x8466
    /// </summary>
    R5G6B5IccSgix = 0x8466,
    /// <summary>
    /// GL_R5_G6_B5_A8_ICC_SGIX = 0x8467
    /// </summary>
    R5G6B5A8IccSgix = 0x8467,
    /// <summary>
    /// GL_ALPHA16_ICC_SGIX = 0x8468
    /// </summary>
    Alpha16IccSgix = 0x8468,
    /// <summary>
    /// GL_LUMINANCE16_ICC_SGIX = 0x8469
    /// </summary>
    Luminance16IccSgix = 0x8469,
    /// <summary>
    /// GL_LUMINANCE16_ALPHA8_ICC_SGIX = 0x846B
    /// </summary>
    Luminance16Alpha8IccSgix = 0x846B,
    /// <summary>
    /// GL_DEPTH_STENCIL = 0x84F9
    /// </summary>
    DepthStencil = 0x84F9,
    /// <summary>
    /// GL_RED_INTEGER = 0x8D94
    /// </summary>
    RedInteger = 0x8D94,
    /// <summary>
    /// GL_GREEN_INTEGER = 0x8D95 
    /// </summary>
    GreenInteger = 0x8D95,
    /// <summary>
    /// GL_BLUE_INTEGER = 0x8D96
    /// </summary>
    BlueInteger = 0x8D96,
    /// <summary>
    /// GL_ALPHA_INTEGER = 0x8D97
    /// </summary>
    AlphaInteger = 0x8D97,
    /// <summary>
    /// GL_RGB_INTEGER = 0x8D98
    /// </summary>
    RgbInteger = 0x8D98,
    /// <summary>
    /// GL_RGBA_INTEGER = 0x8D99
    /// </summary>
    RgbaInteger = 0x8D99,
    /// <summary>
    /// GL_BGR_INTEGER = 0x8D9A
    /// </summary>
    BgrInteger = 0x8D9A,
    /// <summary>
    /// GL_BGRA_INTEGER = 0x8D9B 
    /// </summary>
    BgraInteger = 0x8D9B
}

/// <summary>
/// TextureGLPixelType
/// </summary>
public enum TextureGLPixelType
{
    /// <summary>
    /// GL_BYTE = 0x1400
    /// </summary>
    Byte = 0x1400,
    /// <summary>
    /// GL_UNSIGNED_BYTE = 0x1401
    /// </summary>
    UnsignedByte = 0x1401,
    /// <summary>
    /// GL_SHORT = 0x1402
    /// </summary>
    Short = 0x1402,
    /// <summary>
    /// GL_UNSIGNED_SHORT = 0x1403
    /// </summary>
    UnsignedShort = 0x1403,
    /// <summary>
    /// GL_INT = 0x1404
    /// </summary>
    Int = 0x1404,
    /// <summary>
    /// GL_UNSIGNED_INT = 0x1405
    /// </summary>
    UnsignedInt = 0x1405,
    /// <summary>
    /// GL_FLOAT = 0x1406
    /// </summary>
    Float = 0x1406,
    /// <summary>
    /// GL_HALF_FLOAT = 0x140B
    /// </summary>
    HalfFloat = 0x140B,
    /// <summary>
    /// GL_BITMAP = 0x1A00
    /// </summary>
    Bitmap = 0x1A00,
    /// <summary>
    /// GL_UNSIGNED_BYTE_3_3_2 = 0x8032
    /// </summary>
    UnsignedByte332 = 0x8032,
    /// <summary>
    /// GL_UNSIGNED_BYTE_3_3_2_EXT = 0x8032
    /// </summary>
    UnsignedByte332Ext = 0x8032,
    /// <summary>
    /// GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033
    /// </summary>
    UnsignedShort4444 = 0x8033,
    /// <summary>
    /// GL_UNSIGNED_SHORT_4_4_4_4_EXT = 0x8033
    /// </summary>
    UnsignedShort4444Ext = 0x8033,
    /// <summary>
    /// GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034
    /// </summary>
    UnsignedShort5551 = 0x8034,
    /// <summary>
    /// GL_UNSIGNED_SHORT_5_5_5_1_EXT = 0x8034
    /// </summary>
    UnsignedShort5551Ext = 0x8034,
    /// <summary>
    /// GL_UNSIGNED_INT_8_8_8_8 = 0x8035
    /// </summary>
    UnsignedInt8888 = 0x8035,
    /// <summary>
    /// GL_UNSIGNED_INT_8_8_8_8_EXT = 0x8035
    /// </summary>
    UnsignedInt8888Ext = 0x8035,
    /// <summary>
    /// GL_UNSIGNED_INT_10_10_10_2 = 0x8036
    /// </summary>
    UnsignedInt1010102 = 0x8036,
    /// <summary>
    /// GL_UNSIGNED_INT_10_10_10_2_EXT = 0x8036
    /// </summary>
    UnsignedInt1010102Ext = 0x8036,
    /// <summary>
    /// GL_UNSIGNED_BYTE_2_3_3_REVERSED = 0x8362
    /// </summary>
    UnsignedByte233Reversed = 0x8362,
    /// <summary>
    /// GL_UNSIGNED_SHORT_5_6_5 = 0x8363
    /// </summary>
    UnsignedShort565 = 0x8363,
    /// <summary>
    /// GL_UNSIGNED_SHORT_5_6_5_REVERSED = 0x8364
    /// </summary>
    UnsignedShort565Reversed = 0x8364,
    /// <summary>
    /// GL_UNSIGNED_SHORT_4_4_4_4_REVERSED = 0x8365
    /// </summary>
    UnsignedShort4444Reversed = 0x8365,
    /// <summary>
    /// GL_UNSIGNED_SHORT_1_5_5_5_REVERSED = 0x8366
    /// </summary>
    UnsignedShort1555Reversed = 0x8366,
    /// <summary>
    /// GL_UNSIGNED_INT_8_8_8_8_REVERSED = 0x8367
    /// </summary>
    UnsignedInt8888Reversed = 0x8367,
    /// <summary>
    /// GL_UNSIGNED_INT_2_10_10_10_REVERSED = 0x8368
    /// </summary>
    UnsignedInt2101010Reversed = 0x8368,
    /// <summary>
    /// GL_UNSIGNED_INT_24_8 = 0x84FA
    /// </summary>
    UnsignedInt248 = 0x84FA,
    /// <summary>
    /// GL_UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B
    /// </summary>
    UnsignedInt10F11F11FRev = 0x8C3B,
    /// <summary>
    /// GL_UNSIGNED_INT_5_9_9_9_REV = 0x8C3E
    /// </summary>
    UnsignedInt5999Rev = 0x8C3E,
    /// <summary>
    /// GL_FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD
    /// </summary>
    Float32UnsignedInt248Rev = 0x8DAD
}
#endif
#endregion
