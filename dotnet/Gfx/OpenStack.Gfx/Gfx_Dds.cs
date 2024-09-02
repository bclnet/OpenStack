using System;
using System.Runtime.InteropServices;
using static OpenStack.Gfx.FourCC;
using static OpenStack.Gfx.DXGI_FORMAT;
using System.IO;
using System.Runtime.CompilerServices;

namespace OpenStack.Gfx
{
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
        public static (string, int) Struct = ($"<5I", 20);

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
        public static (string, int) Struct = ($"<7I44s{"8I"}5I", SizeOf);
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
        public static (DDS_HEADER header, DDS_HEADER_DXT10? headerDxt10, (object type, int blockSize, object gl, object vulken, object unity, object unreal) format, byte[] bytes) Read(BinaryReader r, bool readMagic = true)
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
                DXT1 => (DXT1, 8, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1),
                DXT3 => (DXT3, 16, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureUnityFormat.DXT3_POLYFILL, TextureUnrealFormat.DXT3),
                DXT5 => (DXT5, 16, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureUnityFormat.DXT5, TextureUnrealFormat.DXT5),
                DX10 => (headerDxt10?.dxgiFormat) switch
                {
                    BC1_UNORM => (BC1_UNORM, 8, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1),
                    BC1_UNORM_SRGB => (BC1_UNORM_SRGB, 8, TextureGLFormat.CompressedSrgbS3tcDxt1Ext, TextureGLFormat.CompressedSrgbS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1),
                    BC2_UNORM => (BC2_UNORM, 16, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureUnityFormat.DXT3_POLYFILL, TextureUnrealFormat.DXT3),
                    BC2_UNORM_SRGB => (BC2_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaS3tcDxt1Ext, TextureGLFormat.CompressedSrgbAlphaS3tcDxt1Ext, TextureUnityFormat.Unknown, TextureUnrealFormat.Unknown),
                    BC3_UNORM => (BC3_UNORM, 16, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureUnityFormat.DXT5, TextureUnrealFormat.DXT5),
                    BC3_UNORM_SRGB => (BC3_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaS3tcDxt5Ext, TextureGLFormat.CompressedSrgbAlphaS3tcDxt5Ext, TextureUnityFormat.Unknown, TextureUnrealFormat.Unknown),
                    BC4_UNORM => (BC4_UNORM, 8, TextureGLFormat.CompressedRedRgtc1, TextureGLFormat.CompressedRedRgtc1, TextureUnityFormat.BC4, TextureUnrealFormat.BC4),
                    BC4_SNORM => (BC4_SNORM, 8, TextureGLFormat.CompressedSignedRedRgtc1, TextureGLFormat.CompressedSignedRedRgtc1, TextureUnityFormat.BC4, TextureUnrealFormat.BC4),
                    BC5_UNORM => (BC5_UNORM, 16, TextureGLFormat.CompressedRgRgtc2, TextureGLFormat.CompressedRgRgtc2, TextureUnityFormat.BC5, TextureUnrealFormat.BC5),
                    BC5_SNORM => (BC5_SNORM, 16, TextureGLFormat.CompressedSignedRgRgtc2, TextureGLFormat.CompressedSignedRgRgtc2, TextureUnityFormat.BC5, TextureUnrealFormat.BC5),
                    BC6H_UF16 => (BC6H_UF16, 16, TextureGLFormat.CompressedRgbBptcUnsignedFloat, TextureGLFormat.CompressedRgbBptcUnsignedFloat, TextureUnityFormat.BC6H, TextureUnrealFormat.BC6H),
                    BC6H_SF16 => (BC6H_SF16, 16, TextureGLFormat.CompressedRgbBptcSignedFloat, TextureGLFormat.CompressedRgbBptcSignedFloat, TextureUnityFormat.BC6H, TextureUnrealFormat.BC6H),
                    BC7_UNORM => (BC7_UNORM, 16, TextureGLFormat.CompressedRgbaBptcUnorm, TextureGLFormat.CompressedRgbaBptcUnorm, TextureUnityFormat.BC7, TextureUnrealFormat.BC7),
                    BC7_UNORM_SRGB => (BC7_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaBptcUnorm, TextureGLFormat.CompressedSrgbAlphaBptcUnorm, TextureUnityFormat.BC7, TextureUnrealFormat.BC7),
                    R8_UNORM => (R8_UNORM, 1, (TextureGLFormat.R8, TextureGLPixelFormat.Red, TextureGLPixelType.Byte), (TextureGLFormat.R8, TextureGLPixelFormat.Red, TextureGLPixelType.Byte), TextureUnityFormat.R8, TextureUnrealFormat.R8), //: guess
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

        static (object type, int blockSize, object gl, object vulken, object unity, object unreal) MakeFormat(ref DDS_PIXELFORMAT f) =>
            ("Raw", (int)f.dwRGBBitCount >> 2,
            (TextureGLFormat.Rgba, TextureGLPixelFormat.Rgba, TextureGLPixelType.Byte),
            (TextureGLFormat.Rgba, TextureGLPixelFormat.Rgba, TextureGLPixelType.Byte),
            TextureUnityFormat.RGBA32,
            TextureUnrealFormat.R8G8B8A8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ConvertDxt3BlockToDtx5Block(byte* p)
        {
            byte a0 = p[0], a1 = p[1], a2 = p[1], a3 = p[1], a4 = p[1], a5 = p[1], a6 = p[1], a7 = p[1];
        }

        public static void ConvertDxt3ToDtx5(byte[] data, int width, int height, int mipMaps)
        {
            fixed (byte* data_ = data)
            {
                var p = data_;
                var count = ((width + 3) / 4) * ((height + 3) / 4);
                while (count-- != 0) ConvertDxt3BlockToDtx5Block(p += 16);
                //int blockCountX = (width + 3) / 4, blockCountY = (height + 3) / 4;
                //for (var y = 0; y < blockCountY; y++) for (var x = 0; x < blockCountX; x++) ConvertDxt3BlockToDtx5Block(p += 16);
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
}