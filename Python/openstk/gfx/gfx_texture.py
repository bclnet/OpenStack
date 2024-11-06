from __future__ import annotations
import os
from enum import Enum, Flag
from openstk.poly import Reader, Writer, findType

# forwards
# class DDS_HEADER: pass

#region Texture Enums

# TextureFlags
class TextureFlags(Flag):
    SUGGEST_CLAMPS = 0x00000001
    SUGGEST_CLAMPT = 0x00000002
    SUGGEST_CLAMPU = 0x00000004
    NO_LOD = 0x00000008
    CUBE_TEXTURE = 0x00000010
    VOLUME_TEXTURE = 0x00000020
    TEXTURE_ARRAY = 0x00000040

# TextureUnityFormat
class TextureUnityFormat(Enum):
    Unknown = 0
    ATC_RGB4 = -127
    ATC_RGBA8 = -127
    PVRTC_2BPP_RGB = -127
    PVRTC_2BPP_RGBA = -127
    PVRTC_4BPP_RGB = -127
    PVRTC_4BPP_RGBA = -127
    Alpha8 = 1
    ARGB4444 = 2
    RGB24 = 3
    RGBA32 = 4
    ARGB32 = 5
    RGB565 = 7
    R16 = 9
    DXT1 = 10
    DXT3_POLYFILL = 11
    DXT5 = 12
    RGBA4444 = 13
    BGRA32 = 14
    RHalf = 15
    RGHalf = 16
    RGBAHalf = 17
    RFloat = 18
    RGFloat = 19
    RGBAFloat = 20
    YUY2 = 21
    RGB9e5Float = 22
    BC6H = 24
    BC7 = 25
    BC4 = 26
    BC5 = 27
    DXT1Crunched = 28
    DXT5Crunched = 29
    PVRTC_RGB2 = 30
    PVRTC_RGBA2 = 31
    PVRTC_RGB4 = 32
    PVRTC_RGBA4 = 33
    ETC_RGB4 = 34
    EAC_R = 41
    EAC_R_SIGNED = 42
    EAC_RG = 43
    EAC_RG_SIGNED = 44
    ETC2_RGB = 45
    ETC2_RGBA1 = 46
    ETC2_RGBA8 = 47
    ASTC_4x4 = 48
    ASTC_RGB_4x4 = 48
    ASTC_5x5 = 49
    ASTC_RGB_5x5 = 49
    ASTC_6x6 = 50
    ASTC_RGB_6x6 = 50
    ASTC_8x8 = 51
    ASTC_RGB_8x8 = 51
    ASTC_10x10 = 52
    ASTC_RGB_10x10 = 52
    ASTC_12x12 = 53
    ASTC_RGB_12x12 = 53
    ASTC_RGBA_4x4 = 54
    ASTC_RGBA_5x5 = 55
    ASTC_RGBA_6x6 = 56
    ASTC_RGBA_8x8 = 57
    ASTC_RGBA_10x10 = 58
    ASTC_RGBA_12x12 = 59
    ETC_RGB4_3DS = 60
    ETC_RGBA8_3DS = 61
    RG16 = 62
    R8 = 63
    ETC_RGB4Crunched = 64
    ETC2_RGBA8Crunched = 65
    ASTC_HDR_4x4 = 66
    ASTC_HDR_5x5 = 67
    ASTC_HDR_6x6 = 68
    ASTC_HDR_8x8 = 69
    ASTC_HDR_10x10 = 70
    ASTC_HDR_12x12 = 71

# TextureUnrealFormat
class TextureUnrealFormat(Enum):
    Unknown = 0
    A32B32G32R32F = 1
    B8G8R8A8 = 2
    G8 = 3 # G8 means Gray/Grey, not Green, typically actually uses a red format with replication of R to RGB
    G16 = 4 # G16 means Gray/Grey like G8
    DXT1 = 5
    DXT3 = 6
    DXT5 = 7
    UYVY = 8
    FloatRGB = 9 # 16F
    FloatRGBA = 10 # 16F
    DepthStencil = 11
    ShadowDepth = 12
    R32Float = 13
    G16R16 = 14
    G16R16F = 15
    G16R16FFilter = 16
    G32R32F = 17
    A2B10G10R10 = 18
    A16B16G16R16 = 19
    D24 = 20
    R16F = 21
    R16FFilter = 22
    BC5 = 23
    V8U8 = 24
    A1 = 25
    FloatR11G11B10 = 26
    A8 = 27
    R32UInt = 28
    R32SInt = 29
    PVRTC2 = 30
    PVRTC4 = 31
    R16UInt = 32
    R16SInt = 33
    R16G16B16A16UInt = 34
    R16G16B16A16SInt = 35
    R5G6B5UNorm = 36
    R8G8B8A8 = 37
    A8R8G8B8 = 38 # Only used for legacy loading; do NOT us
    BC4 = 39
    R8G8 = 40
    ATCRGB = 41 # Unsupported Format
    ATCRGBAE = 42 # Unsupported Format
    ATCRGBAI = 43
    X24G8 = 44 # Used for creating SRVs to alias a DepthStencil buffer to read Stencil. Don't use for creating textures
    ETC1 = 45 # Unsupported Format
    ETC2RGB = 46
    ETC2RGBA = 47
    R32G32B32A32UInt = 48
    R16G16UInt = 49
    ASTC4x4 = 50 # 8.00 bpp
    ASTC6x6 = 51 # 3.56 bpp
    ASTC8x8 = 52 # 2.00 bpp
    ASTC10x10 = 53 # 1.28 bpp
    ASTC12x12 = 54 # 0.89 bpp
    BC6H = 55
    BC7 = 56
    R8UInt = 57
    L8 = 58
    XGXR8 = 59
    R8G8B8A8UInt = 60
    R8G8B8A8SNorm = 61
    R16G16B16A16UNorm = 62
    R16G16B16A16SNorm = 63
    PLATFORMHDR0 = 64
    PLATFORMHDR1 = 65 # Reserved
    PLATFORMHDR2 = 66 # Reserved
    NV12 = 67
    R32G32UInt = 68
    ETC2R11EAC = 69
    ETC2RG11EAC = 70
    R8 = 71
    B5G5R5A1UNorm = 72
    ASTC4x4HDR = 73
    ASTC6x6HDR = 74
    ASTC8x8HDR = 75
    ASTC10x10HDR = 76
    ASTC12x12HDR = 77
    G16R16SNorm = 78
    R8G8UInt = 79
    R32G32B32UInt = 80
    R32G32B32SInt = 81
    R32G32B32F = 82
    R8SInt = 83
    R64UInt = 84
    R9G9B9EXP5 = 85
    P010 = 86
    MAX = 87

# TextureGLFormat
class TextureGLFormat(Enum):
    DepthComponent = 0x1902 # GL_DEPTH_COMPONENT = 0x1902
    Red = 0x1903 # GL_RED = 0x1903 (compress only)
    RedExt = 0x1903 # GL_RED_EXT = 0x1903 (dup, compress only)
    Alpha = 0x1906 # GL_ALPHA = 0x1906 (internal only)
    Rgb = 0x1907 # GL_RGB = 0x1907
    Rgba = 0x1908 # GL_RGBA = 0x1908
    Luminance = 0x1909 # GL_LUMINANCE = 0x1909 (internal only)
    LuminanceAlpha = 0x190A # GL_LUMINANCE_ALPHA = 0x190A (internal only)
    R3G3B2 = 0x2A10 # GL_R3_G3_B2 = 0x2A10
    Alpha4 = 0x803B # GL_ALPHA4 = 0x803B
    Alpha8 = 0x803C # GL_ALPHA8 = 0x803C
    Alpha12 = 0x803D # GL_ALPHA12 = 0x803D
    Alpha16 = 0x803E # GL_ALPHA16 = 0x803E
    Luminance4 = 0x803F # GL_LUMINANCE4 = 0x803F
    Luminance8 = 0x8040 # GL_LUMINANCE8 = 0x8040
    Luminance12 = 0x8041 # GL_LUMINANCE12 = 0x8041
    Luminance16 = 0x8042 # GL_LUMINANCE16 = 0x8042
    Luminance4Alpha4 = 0x8043 # GL_LUMINANCE4_ALPHA4 = 0x8043
    Luminance6Alpha2 = 0x8044 # GL_LUMINANCE6_ALPHA2 = 0x8044
    Luminance8Alpha8 = 0x8045 # GL_LUMINANCE8_ALPHA8 = 0x8045
    Luminance12Alpha4 = 0x8046 # GL_LUMINANCE12_ALPHA4 = 0x8046
    Luminance12Alpha12 = 0x8047 # GL_LUMINANCE12_ALPHA12 = 0x8047
    Luminance16Alpha16 = 0x8048 # GL_LUMINANCE16_ALPHA16 = 0x8048
    Intensity = 0x8049 # GL_INTENSITY = 0x8049
    Intensity4 = 0x804A # GL_INTENSITY4 = 0x804A
    Intensity8 = 0x804B # GL_INTENSITY8 = 0x804B
    Intensity12 = 0x804C # GL_INTENSITY12 = 0x804C
    Intensity16 = 0x804D # GL_INTENSITY16 = 0x804D
    Rgb2Ext = 0x804E # GL_RGB2_EXT = 0x804E
    Rgb4 = 0x804F # GL_RGB4 = 0x804F
    Rgb4Ext = 0x804F # GL_RGB4_EXT = 0x804F (compress only)
    Rgb5 = 0x8050              # GL_RGB5 = 0x8050
    Rgb5Ext = 0x8050           # GL_RGB5_EXT = 0x8050 (dup, compress only)
    Rgb8 = 0x8051              # GL_RGB8 = 0x8051
    Rgb8Ext = 0x8051           # GL_RGB8_EXT = 0x8051 (dup, compress only)
    Rgb8Oes = 0x8051           # GL_RGB8_OES = 0x8051 (dup, compress only)
    Rgb10 = 0x8052             # GL_RGB10 = 0x8052
    Rgb10Ext = 0x8052          # GL_RGB10_EXT = 0x8052 (dup, compress only)
    Rgb12 = 0x8053             # GL_RGB12 = 0x8053
    Rgb12Ext = 0x8053          # GL_RGB12_EXT = 0x8053 (dup, compress only)
    Rgb16 = 0x8054             # GL_RGB16 = 0x8054
    Rgb16Ext = 0x8054          # GL_RGB16_EXT = 0x8054 (dup, compress only)
    Rgba2 = 0x8056             # GL_RGBA2 = 0x8055 (internal only)
    Rgba4 = 0x8056             # GL_RGBA4 = 0x8056
    Rgba4Ext = 0x8056          # GL_RGBA4_EXT = 0x8056 (dup, compress only)
    Rgba4Oes = 0x8056          # GL_RGBA4_OES = 0x8056 (dup, compress only)
    Rgb5A1 = 0x8057            # GL_RGB5_A1 = 0x8057
    Rgb5A1Ext = 0x8057         # GL_RGB5_A1_EXT = 0x8057 (dup, compress only)
    Rgb5A1Oes = 0x8057         # GL_RGB5_A1_OES = 0x8057 (dup, compress only)
    Rgba8 = 0x8058             # GL_RGBA8 = 0x8058
    Rgba8Ext = 0x8058          # GL_RGBA8_EXT = 0x8058 (dup, compress only)
    Rgba8Oes = 0x8058          # GL_RGBA8_OES = 0x8058 (dup, compress only)
    Rgb10A2 = 0x8059           # GL_RGB10_A2 = 0x8059
    Rgb10A2Ext = 0x8059        # GL_RGB10_A2_EXT = 0x8059 (dup, compress only)
    Rgba12 = 0x805A            # GL_RGBA12 = 0x805A
    Rgba12Ext = 0x805A         # GL_RGBA12_EXT = 0x805A (dup, compress only)
    Rgba16 = 0x805B            # GL_RGBA16 = 0x805B
    Rgba16Ext = 0x805B         # GL_RGBA16_EXT = 0x805B (dup, compress only)
    DualAlpha4Sgis = 0x8110        # GL_DUAL_ALPHA4_SGIS = 0x8110
    DualAlpha8Sgis = 0x8111        # GL_DUAL_ALPHA8_SGIS = 0x8111
    DualAlpha12Sgis = 0x8112       # GL_DUAL_ALPHA12_SGIS = 0x8112
    DualAlpha16Sgis = 0x8113       # GL_DUAL_ALPHA16_SGIS = 0x8113
    DualLuminance4Sgis = 0x8114    # GL_DUAL_LUMINANCE4_SGIS = 0x8114
    DualLuminance8Sgis = 0x8115    # GL_DUAL_LUMINANCE8_SGIS = 0x8115
    DualLuminance12Sgis = 0x8116   # GL_DUAL_LUMINANCE12_SGIS = 0x8116
    DualLuminance16Sgis = 0x8117   # GL_DUAL_LUMINANCE16_SGIS = 0x8117
    DualIntensity4Sgis = 0x8118    # GL_DUAL_INTENSITY4_SGIS = 0x8118
    DualIntensity8Sgis = 0x8119    # GL_DUAL_INTENSITY8_SGIS = 0x8119
    DualIntensity12Sgis = 0x811A   # GL_DUAL_INTENSITY12_SGIS = 0x811A
    DualIntensity16Sgis = 0x811B   # GL_DUAL_INTENSITY16_SGIS = 0x811B
    DualLuminanceAlpha4Sgis = 0x811C# GL_DUAL_LUMINANCE_ALPHA4_SGIS = 0x811C
    DualLuminanceAlpha8Sgis = 33053# GL_DUAL_LUMINANCE_ALPHA8_SGIS = 0x811D
    QuadAlpha4Sgis = 0x811E        # GL_QUAD_ALPHA4_SGIS = 0x811E
    QuadAlpha8Sgis = 0x811F        # GL_QUAD_ALPHA8_SGIS = 0x811F
    QuadLuminance4Sgis = 0x8120    # GL_QUAD_LUMINANCE4_SGIS = 0x8120
    QuadLuminance8Sgis = 0x8121    # GL_QUAD_LUMINANCE8_SGIS = 0x8121
    QuadIntensity4Sgis = 0x8122    # GL_QUAD_INTENSITY4_SGIS = 0x8122
    QuadIntensity8Sgis = 0x8123    # GL_QUAD_INTENSITY8_SGIS = 0x8123
    DepthComponent16 = 0x81A5      # GL_DEPTH_COMPONENT16 = 0x81A5
    DepthComponent16Arb = 0x81A5   # GL_DEPTH_COMPONENT16_ARB = 0x81A5 (dup, compress only)
    DepthComponent16Oes = 0x81A5   # GL_DEPTH_COMPONENT16_OES = 0x81A5 (dup, compress only)
    DepthComponent16Sgix = 0x81A5  # GL_DEPTH_COMPONENT16_SGIX = 0x81A5 (dup)
    DepthComponent24 = 0x81A6      # GL_DEPTH_COMPONENT24 = 0x81A6 (internal only)
    DepthComponent24Arb = 0x81A6   # GL_DEPTH_COMPONENT24_ARB = 0x81A6 (dup, compress only)
    DepthComponent24Oes = 0x81A6   # GL_DEPTH_COMPONENT24_OES = 0x81A6 (dup, compress only)
    DepthComponent24Sgix = 0x81A6  # GL_DEPTH_COMPONENT24_SGIX = 0x81A6
    DepthComponent32 = 0x81A7      # GL_DEPTH_COMPONENT32 = 0x81A7 (internal only)
    DepthComponent32Arb = 0x81A7   # GL_DEPTH_COMPONENT32_ARB = 0x81A7 (dup, compress only)
    DepthComponent32Oes = 0x81A7   # GL_DEPTH_COMPONENT32_OES = 0x81A7 (dup, compress only)
    DepthComponent32Sgix = 0x81A7  # GL_DEPTH_COMPONENT32_SGIX = 0x81A7 (dup)
    CompressedRed = 0x8225         # GL_COMPRESSED_RED = 0x8225
    CompressedRg = 0x8226          # GL_COMPRESSED_RG = 0x8226
    Rg = 0x8227                # GL_RG = 0x8227 (compress only)
    R8 = 0x8229                # GL_R8 = 0x8229
    R8Ext = 0x8229             # GL_R8_EXT = 0x8229 (dup, compress only)
    R16 = 0x822A               # GL_R16 = 0x822A
    R16Ext = 0x822A            # GL_R16_EXT = 0x822A (dup, compress only)
    Rg8 = 0x822B               # GL_RG8 = 0x822B
    Rg8Ext = 0x822B            # GL_RG8_EXT = 0x822B (dup, compress only)
    Rg16 = 0x822C              # GL_RG16 = 0x822C
    Rg16Ext = 0x822C           # GL_RG16_EXT = 0x822C (dup, compress only)
    R16f = 0x822D              # GL_R16F = 0x822D
    R16fExt = 0x822D           # GL_R16F_EXT = 0x822D (dup, compress only)
    R32f = 0x822E              # GL_R32F = 0x822E
    R32fExt = 0x822E           # GL_R32F_EXT = 0x822E (dup, compress only)
    Rg16f = 0x822F             # GL_RG16F = 0x822F
    Rg16fExt = 0x822F          # GL_RG16F_EXT = 0x822F (dup, compress only)
    Rg32f = 0x8230             # GL_RG32F = 0x8230
    Rg32fExt = 0x8230          # GL_RG32F_EXT = 0x8230 (dup, compress only)
    R8i = 0x8231               # GL_R8I = 0x8231
    R8ui = 0x8232              # GL_R8UI = 0x8232
    R16i = 0x8233              # GL_R16I = 0x8233
    R16ui = 0x8234             # GL_R16UI = 0x8234
    R32i = 0x8235              # GL_R32I = 0x8235
    R32ui = 0x8236             # GL_R32UI = 0x8236
    Rg8i = 0x8237              # GL_RG8I = 0x8237
    Rg8ui = 0x8238             # GL_RG8UI = 0x8238
    Rg16i = 0x8239             # GL_RG16I = 0x8239
    Rg16ui = 0x823A            # GL_RG16UI = 0x823A
    Rg32i = 0x823B             # GL_RG32I = 0x823B
    Rg32ui = 0x823C            # GL_RG32UI = 0x823C
    CompressedRgbS3tcDxt1Ext = 0x83F0# GL_COMPRESSED_RGB_S3TC_DXT1_EXT = 0x83F0
    CompressedRgbaS3tcDxt1Ext = 0x83F1# GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1
    CompressedRgbaS3tcDxt3Ext = 0x83F2# GL_COMPRESSED_RGBA_S3TC_DXT3_EXT = 0x83F2
    CompressedRgbaS3tcDxt5Ext = 0x83F3# GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3
    RgbIccSgix = 0x8460 # GL_RGB_ICC_SGIX = 0x8460 (internal only)
    RgbaIccSgix = 0x8461 # GL_RGBA_ICC_SGIX = 0x8461 (internal only)
    AlphaIccSgix = 0x8462 # GL_ALPHA_ICC_SGIX = 0x8462 (internal only)
    LuminanceIccSgix = 0x8460 # GL_LUMINANCE_ICC_SGIX = 0x8463 (internal only)
    IntensityIccSgix = 0x8464 # GL_INTENSITY_ICC_SGIX = 0x8460 (internal only)
    LuminanceAlphaIccSgix = 0x8465 # GL_LUMINANCE_ALPHA_ICC_SGIX = 0x8465 (internal only)
    R5G6B5IccSgix = 0x8466 # GL_R5_G6_B5_ICC_SGIX = 0x8466 (internal only)
    R5G6B5A8IccSgix = 0x8467 # GL_R5_G6_B5_A8_ICC_SGIX = 0x8467 (internal only)
    Alpha16IccSgix = 0x8468 # GL_RGB_ICC_SGIX = 0x8468 (internal only)
    Luminance16IccSgix = 0x8469 # GL_LUMINANCE16_ICC_SGIX = 0x8469 (internal only)
    Intensity16IccSgix = 0x846A # GL_INTENSITY16_ICC_SGIX = 0x846A (internal only)
    Luminance16Alpha8IccSgix = 0x846B # GL_LUMINANCE16_ALPHA8_ICC_SGIX = 0x846B (internal only)
    CompressedAlpha = 0x8469 # GL_COMPRESSED_ALPHA = 0x84E9 (internal only)
    CompressedLuminance = 0x84EA # GL_COMPRESSED_LUMINANCE = 0x84EA (internal only)
    CompressedLuminanceAlpha = 0x84EB # GL_COMPRESSED_LUMINANCE_ALPHA = 0x84EB (internal only)
    CompressedIntensity = 0x84EC # GL_COMPRESSED_INTENSITY = 0x84EC (internal only)
    CompressedRgb = 0x84ED     # GL_COMPRESSED_RGB = 0x84ED
    CompressedRgba = 0x84EE    # GL_COMPRESSED_RGBA = 0x84EE
    DepthStencil = 0x84F9      # GL_DEPTH_STENCIL = 0x84F9
    DepthStencilExt = 0x84F9   # GL_DEPTH_STENCIL_EXT = 0x84F9 (dup, compress only)
    DepthStencilNv = 0x84F9    # GL_DEPTH_STENCIL_NV = 0x84F9 (dup, compress only)
    DepthStencilOes = 0x84F9   # GL_DEPTH_STENCIL_OES = 0x84F9 (dup, compress only)
    DepthStencilMesa = 0x8750  # GL_DEPTH_STENCIL_MESA = 0x8750 (compress only)
    Rgba32f = 0x8814           # GL_RGBA32F = 0x8814
    Rgba32fArb = 0x8814        # GL_RGBA32F_ARB = 0x8814 (dup, compress only)
    Rgba32fExt = 0x8814        # GL_RGBA32F_EXT = 0x8814 (dup, compress only)
    Rgba16f = 0x881A           # GL_RGBA16F = 0x881A
    Rgba16fArb = 0x881A        # GL_RGBA16F_ARB = 0x881A (dup, compress only)
    Rgba16fExt = 0x881A        # GL_RGBA16F_EXT = 0x881A (dup, compress only)
    Rgb16f = 0x881B            # GL_RGB16F = 0x881B
    Rgb16fArb = 0x881B         # GL_RGB16F_ARB = 0x881B (dup, compress only)
    Rgb16fExt = 0x881B         # GL_RGB16F_EXT = 0x881B (dup, compress only)
    Depth24Stencil8 = 0x88F0   # GL_DEPTH24_STENCIL8 = 0x88F0
    Depth24Stencil8Ext = 0x88F0# GL_DEPTH24_STENCIL8_EXT = 0x88F0 (dup, compress only)
    Depth24Stencil8Oes = 0x88F0# GL_DEPTH24_STENCIL8_OES = 0x88F0 (dup, compress only)
    R11fG11fB10f = 0x8C3A      # GL_R11F_G11F_B10F = 0x8C3A
    R11fG11fB10fApple = 0x8C3A # GL_R11F_G11F_B10F_APPLE = 0x8C3A (dup, compress only)
    R11fG11fB10fExt = 0x8C3A   # GL_R11F_G11F_B10F_EXT = 0x8C3A (dup, compress only)
    Rgb9E5 = 0x8C3D            # GL_RGB9_E5 = 0x8C3D
    Rgb9E5Apple = 0x8C3D       # GL_RGB9_E5_APPLE = 0x8C3D (dup, compress only)
    Rgb9E5Ext = 0x8C3D         # GL_RGB9_E5_EXT = 0x8C3D (dup, compress only)
    Srgb = 0x8C40              # GL_SRGB = 0x8C40
    SrgbExt = 0x8C40           # GL_SRGB_EXT = 0x8C40 (dup, compress only)
    Srgb8 = 0x8C41             # GL_SRGB8 = 0x8C41
    Srgb8Ext = 0x8C41          # GL_SRGB8_EXT = 0x8C41 (dup, compress only)
    Srgb8Nv = 0x8C41           # GL_SRGB8_NV = 0x8C41 (dup, compress only)
    SrgbAlpha = 0x8C42         # GL_SRGB_ALPHA = 0x8C42
    SrgbAlphaExt = 0x8C42      # GL_SRGB_ALPHA_EXT = 0x8C42 (dup, compress only)
    Srgb8Alpha8 = 0x8C43       # GL_SRGB8_ALPHA8 = 0x8C43
    Srgb8Alpha8Ext = 0x8C43    # GL_SRGB8_ALPHA8_EXT = 0x8C43 (dup, compress only)
    SluminanceAlpha = 0x8C44       # GL_SLUMINANCE_ALPHA = 0x8C44 (internal only)
    Sluminance8Alpha8 = 0x8C45       # GL_SLUMINANCE8_ALPHA8 = 0x8C45 (internal only)
    Sluminance = 0x8C46       # GL_SLUMINANCE = 0x8C46 (internal only)
    Sluminance8 = 0x8C47       # GL_SLUMINANCE8 = 0x8C47 (internal only)
    CompressedSrgb = 0x8C48    # GL_COMPRESSED_SRGB = 0x8C48
    CompressedSrgbAlpha = 0x8C49# GL_COMPRESSED_SRGB_ALPHA = 0x8C49
    CompressedSluminance = 0x8C4A       # GL_COMPRESSED_SLUMINANCE = 0x8C4A (internal only)
    CompressedSluminanceAlpha = 0x8C4B       # GL_COMPRESSED_SLUMINANCE_ALPHA = 0x8C4B (internal only)
    CompressedSrgbS3tcDxt1Ext = 0x8C4C# GL_COMPRESSED_SRGB_S3TC_DXT1_EXT = 0x8C4C
    CompressedSrgbAlphaS3tcDxt1Ext = 0x8C4D# GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT1_EXT = 0x8C4D
    CompressedSrgbAlphaS3tcDxt3Ext = 0x8C4E# GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT3_EXT = 0x8C4E
    CompressedSrgbAlphaS3tcDxt5Ext = 0x8C4F# GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT = 0x8C4F
    DepthComponent32f = 0x8CAC # GL_DEPTH_COMPONENT32F = 0x8CAC
    Depth32fStencil8 = 0x8CAD  # GL_DEPTH32F_STENCIL8 = 0x8CAD
    Rgba32ui = 0x8D70          # GL_RGBA32UI = 0x8D70
    Rgb32ui = 0x8D71           # GL_RGB32UI = 0x8D71
    Rgba16ui = 0x8D76          # GL_RGBA16UI = 0x8D76
    Rgb16ui = 0x8D77           # GL_RGB16UI = 0x8D77
    Rgba8ui = 0x8D7C           # GL_RGBA8UI = 0x8D7C
    Rgb8ui = 0x8D7D            # GL_RGB8UI = 0x8D7D
    Rgba32i = 0x8D82           # GL_RGBA32I = 0x8D82
    Rgb32i = 0x8D83            # GL_RGB32I = 0x8D83
    Rgba16i = 0x8D88           # GL_RGBA16I = 0x8D88
    Rgb16i = 0x8D89            # GL_RGB16I = 0x8D89
    Rgba8i = 0x8D8E            # GL_RGBA8I = 0x8D8E
    Rgb8i = 0x8D8F             # GL_RGB8I = 0x8D8F
    DepthComponent32fNv = 0x8DAB# GL_DEPTH_COMPONENT32F_NV = 0x8DAB (compress only)
    Depth32fStencil8Nv = 0x8DAC# GL_DEPTH32F_STENCIL8_NV = 0x8DAC (compress only)
    Float32UnsignedInt248Rev = 0x8DAD# GL_FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD (internal only)
    CompressedRedRgtc1 = 0x8DBB# GL_COMPRESSED_RED_RGTC1 = 0x8DBB
    CompressedRedRgtc1Ext = 0x8DBB# GL_COMPRESSED_RED_RGTC1_EXT = 0x8DBB (dup, compress only)
    CompressedSignedRedRgtc1 = 0x8DBC# GL_COMPRESSED_SIGNED_RED_RGTC1 = 0x8DBC
    CompressedSignedRedRgtc1Ext = 0x8DBC # GL_COMPRESSED_SIGNED_RED_RGTC1_EXT = 0x8DBC (dup, compress only)
    CompressedRgRgtc2 = 0x8DBD # GL_COMPRESSED_RG_RGTC2 = 0x8DBD
    CompressedSignedRgRgtc2 = 0x8DBE # GL_COMPRESSED_SIGNED_RG_RGTC2 = 0x8DBE
    CompressedRgbaBptcUnorm = 0x8E8C # GL_COMPRESSED_RGBA_BPTC_UNORM = 0x8E8C
    CompressedSrgbAlphaBptcUnorm = 0x8E8D # GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM = 0x8E8D
    CompressedRgbBptcSignedFloat = 0x8E8E # GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT = 0x8E8E
    CompressedRgbBptcUnsignedFloat = 0x8E8F # GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT = 0x8E8F
    R8Snorm = 0x8F94           # GL_R8_SNORM = 0x8F94
    Rg8Snorm = 0x8F95          # GL_RG8_SNORM = 0x8F95
    Rgb8Snorm = 0x8F96         # GL_RGB8_SNORM = 0x8F96
    Rgba8Snorm = 0x8F97        # GL_RGBA8_SNORM = 0x8F97
    R16Snorm = 0x8F98          # GL_R16_SNORM = 0x8F98
    R16SnormExt = 0x8F98       # GL_R16_SNORM_EXT = 0x8F98 (dup, compress only)
    Rg16Snorm = 0x8F99         # GL_RG16_SNORM = 0x8F99
    Rg16SnormExt = 0x8F99      # GL_RG16_SNORM_EXT = 0x8F99 (dup, compress only)
    Rgb16Snorm = 0x8F9A        # GL_RGB16_SNORM = 0x8F9A
    Rgb16SnormExt = 0x8F9A     # GL_RGB16_SNORM_EXT = 0x8F9A (dup, compress only)
    Rgba16Snorm = 0x8F9B        # GL_RGBA16_SNORM = 0x8F9B (internal only)
    Rgb10A2ui = 0x906F         # GL_RGB10_A2UI = 0x906F (compress only)
    One = 1 # GL_ONE = 1 (internal only)
    Two = 2 # GL_TWO = 2 (internal only)
    Three = 3 # GL_THREE = 3 (internal only)
    Four = 4 # GL_FOUR = 4 (internal only)
    CompressedR11Eac = 0x9270  # GL_COMPRESSED_R11_EAC = 0x9270 (compress only)
    CompressedSignedR11Eac = 0x9271 # GL_COMPRESSED_SIGNED_R11_EAC = 0x9271 (compress only)
    CompressedRg11Eac = 0x9272 # GL_COMPRESSED_RG11_EAC = 0x9272 (compress only)
    CompressedSignedRg11Eac = 0x9273 # GL_COMPRESSED_SIGNED_RG11_EAC = 0x9273 (compress only)
    CompressedRgb8Etc2 = 0x9274 # GL_COMPRESSED_RGB8_ETC2 = 0x9274 (compress only)
    CompressedSrgb8Etc2 = 0x9275 # GL_COMPRESSED_SRGB8_ETC2 = 0x9275 (compress only)
    CompressedRgb8PunchthroughAlpha1Etc2 = 0x9276 # GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276 (compress only)
    CompressedSrgb8PunchthroughAlpha1Etc2 = 0x9277 # GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277 (compress only)
    CompressedRgba8Etc2Eac = 0x9278 # GL_COMPRESSED_RGBA8_ETC2_EAC = 0x9278 (compress only)
    CompressedSrgb8Alpha8Etc2Eac = 0x9279 # GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC = 0x9279 (compress only)

# TextureGLPixelFormat
class TextureGLPixelFormat(Enum):
    Unknown = 0
    UnsignedShort = 0x1403 # GL_UNSIGNED_SHORT = 0x1403
    UnsignedInt = 0x1405 # GL_UNSIGNED_INT = 0x1405
    ColorIndex = 0x1900 # GL_COLOR_INDEX = 0x1900
    StencilIndex = 0x1901 # GL_STENCIL_INDEX = 0x1901
    DepthComponent = 0x1902 # GL_DEPTH_COMPONENT = 0x1902
    Red = 0x1903 # GL_RED = 0x1903
    RedExt = 0x1903 # GL_RED_EXT = 0x1903
    Green = 0x1904 # GL_GREEN = 0x1904
    Blue = 0x1905 # GL_BLUE = 0x1905
    Alpha = 0x1906 # GL_ALPHA = 0x1906
    Rgb = 0x1907 # GL_RGB = 0x1907
    Rgba = 0x1908 # GL_RGBA = 0x1908
    Luminance = 0x1909 # GL_LUMINANCE = 0x1909
    LuminanceAlpha = 0x190A # GL_LUMINANCE_ALPHA = 0x190A
    AbgrExt = 0x8000 # GL_ABGR_EXT = 0x8000
    CmykExt = 0x800C # GL_CMYK_EXT = 0x800C
    CmykaExt = 0x800D # GL_CMYKA_EXT = 0x800D
    Bgr = 0x80E0 # GL_BGR = 0x80E0
    Bgra = 0x80E1 # GL_BGRA = 0x80E1
    Ycrcb422Sgix = 0x81BB # GL_YCRCB_422_SGIX = 0x81BB
    Ycrcb444Sgix = 0x81BC # GL_YCRCB_444_SGIX = 0x81BC
    Rg = 0x8227 # GL_RG = 0x8227
    RgInteger = 0x8228 # GL_RG_INTEGER = 0x8228
    R5G6B5IccSgix = 0x8466 # GL_R5_G6_B5_ICC_SGIX = 0x8466
    R5G6B5A8IccSgix = 0x8467 # GL_R5_G6_B5_A8_ICC_SGIX = 0x8467
    Alpha16IccSgix = 0x8468 # GL_ALPHA16_ICC_SGIX = 0x8468
    Luminance16IccSgix = 0x8469 # GL_LUMINANCE16_ICC_SGIX = 0x8469
    Luminance16Alpha8IccSgix = 0x846B # GL_LUMINANCE16_ALPHA8_ICC_SGIX = 0x846B
    DepthStencil = 0x84F9 # GL_DEPTH_STENCIL = 0x84F9
    RedInteger = 0x8D94 # GL_RED_INTEGER = 0x8D94
    GreenInteger = 0x8D95 # GL_GREEN_INTEGER = 0x8D95 
    BlueInteger = 0x8D96 # GL_BLUE_INTEGER = 0x8D96
    AlphaInteger = 0x8D97 # GL_ALPHA_INTEGER = 0x8D97
    RgbInteger = 0x8D98 # GL_RGB_INTEGER = 0x8D98
    RgbaInteger = 0x8D99 # GL_RGBA_INTEGER = 0x8D99
    BgrInteger = 0x8D9A # GL_BGR_INTEGER = 0x8D9A
    BgraInteger = 0x8D9B # GL_BGRA_INTEGER = 0x8D9B 

# TextureGLPixelType
class TextureGLPixelType(Enum):
    Byte = 0x1400 # GL_BYTE = 0x1400
    UnsignedByte = 0x1401 # GL_UNSIGNED_BYTE = 0x1401
    Short = 0x1402 # GL_SHORT = 0x1402
    UnsignedShort = 0x1403 # GL_UNSIGNED_SHORT = 0x1403
    Int = 0x1404 # GL_INT = 0x1404
    UnsignedInt = 0x1405 # GL_UNSIGNED_INT = 0x1405
    Float = 0x1406 # GL_FLOAT = 0x1406
    HalfFloat = 0x140B # GL_HALF_FLOAT = 0x140B
    Bitmap = 0x1A00 # GL_BITMAP = 0x1A00
    UnsignedByte332 = 0x8032 # GL_UNSIGNED_BYTE_3_3_2 = 0x8032
    UnsignedByte332Ext = 0x8032 # GL_UNSIGNED_BYTE_3_3_2_EXT = 0x8032
    UnsignedShort4444 = 0x8033 # GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033
    UnsignedShort4444Ext = 0x8033 # GL_UNSIGNED_SHORT_4_4_4_4_EXT = 0x8033
    UnsignedShort5551 = 0x8034 # GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034
    UnsignedShort5551Ext = 0x8034 # GL_UNSIGNED_SHORT_5_5_5_1_EXT = 0x8034
    UnsignedInt8888 = 0x8035 # GL_UNSIGNED_INT_8_8_8_8 = 0x8035
    UnsignedInt8888Ext = 0x8035 # GL_UNSIGNED_INT_8_8_8_8_EXT = 0x8035
    UnsignedInt1010102 = 0x8036 # GL_UNSIGNED_INT_10_10_10_2 = 0x8036
    UnsignedInt1010102Ext = 0x8036 # GL_UNSIGNED_INT_10_10_10_2_EXT = 0x8036
    UnsignedByte233Reversed = 0x8362 # GL_UNSIGNED_BYTE_2_3_3_REVERSED = 0x8362
    UnsignedShort565 = 0x8363 # GL_UNSIGNED_SHORT_5_6_5 = 0x8363
    UnsignedShort565Reversed = 0x8364 # GL_UNSIGNED_SHORT_5_6_5_REVERSED = 0x8364
    UnsignedShort4444Reversed = 0x8365 # GL_UNSIGNED_SHORT_4_4_4_4_REVERSED = 0x8365
    UnsignedShort1555Reversed = 0x8366 # GL_UNSIGNED_SHORT_1_5_5_5_REVERSED = 0x8366
    UnsignedInt8888Reversed = 0x8367 # GL_UNSIGNED_INT_8_8_8_8_REVERSED = 0x8367
    UnsignedInt2101010Reversed = 0x8368 # GL_UNSIGNED_INT_2_10_10_10_REVERSED = 0x8368
    UnsignedInt248 = 0x84FA # GL_UNSIGNED_INT_24_8 = 0x84FA
    UnsignedInt10F11F11FRev = 0x8C3B # GL_UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B
    UnsignedInt5999Rev = 0x8C3E # GL_UNSIGNED_INT_5_9_9_9_REV = 0x8C3E
    Float32UnsignedInt248Rev = 0x8DAD # GL_FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD

#endregion

#region ITexture

# ITexture
class ITexture:
    width: int
    height: int
    depth: int
    mipMaps: int
    flags: TextureFlags
    def begin(self, platform: int) -> (bytes, object, list[range]): pass
    def end(self) -> None: pass

# ITextureSelect
class ITextureSelect(ITexture):
    def select(self, id: int) -> None: pass

# ITextureVideo
class ITextureVideo(ITexture):
    fps: int
    hasFrames: bool
    def decodeFrame(self) -> bool: pass

# ITextureFrames
class ITextureFrames(ITextureVideo):
    frameMax: int
    def frameSelect(self, id: int) -> None: pass

#endregion

#region DDS_PIXELFORMAT
# https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-pixelformat

# FourCC
class FourCC(Enum):
    DXT1 = 0x31545844       # DXT1
    DXT2 = 0x32545844       # DXT2
    DXT3 = 0x33545844       # DXT3
    DXT4 = 0x34545844       # DXT4
    DXT5 = 0x35545844       # DXT5
    RXGB = 0x42475852       # RXGB
    ATI1 = 0x31495441       # ATI1
    ATI2 = 0x32495441       # ATI2
    A2XY = 0x59583241       # A2XY
    DX10 = 0x30315844       # DX10

# Values which indicate what type of data is in the surface
class DDPF(Flag):
    ALPHAPIXELS = 0x00000001    # Texture contains alpha data; dwRGBAlphaBitMask contains valid data
    ALPHA = 0x00000002          # Used in some older DDS files for alpha channel only uncompressed data (dwRGBBitCount contains the alpha channel bitcount; dwABitMask contains valid data)
    FOURCC = 0x00000004         # Texture contains compressed RGB data; dwFourCC contains valid data
    RGB = 0x00000040            # Texture contains uncompressed RGB data; dwRGBBitCount and the RGB masks (dwRBitMask, dwGBitMask, dwBBitMask) contain valid data
    YUV = 0x00000200            # Used in some older DDS files for YUV uncompressed data (dwRGBBitCount contains the YUV bit count; dwRBitMask contains the Y mask, dwGBitMask contains the U mask, dwBBitMask contains the V mask)
    LUMINANCE = 0x00020000      # Used in some older DDS files for single channel color uncompressed data (dwRGBBitCount contains the luminance channel bit count; dwRBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel DDS file
    NORMAL = 0x80000000         # The normal

# Surface pixel format.
class DDS_PIXELFORMAT:
    struct = ('8I', 32)
    dwSize: int                 # Structure size; set to 32 (bytes)
    dwFlags: DDPF               # Values which indicate what type of data is in the surface
    dwFourCC: FourCC            # Four-character codes for specifying compressed or custom formats. Possible values include: DXT1, DXT2, DXT3, DXT4, or DXT5. A FourCC of DX10 indicates the prescense of the DDS_HEADER_DXT10 extended header, and the dxgiFormat member of that structure indicates the true format. When using a four-character code, dwFlags must include DDPF_FOURCC
    dwRGBBitCount: int          # Number of bits in an RGB (possibly including alpha) format. Valid when dwFlags includes DDPF_RGB, DDPF_LUMINANCE, or DDPF_YUV
    dwRBitMask: int             # Red (or lumiannce or Y) mask for reading color data. For instance, given the A8R8G8B8 format, the red mask would be 0x00ff0000
    dwGBitMask: int             # Green (or U) mask for reading color data. For instance, given the A8R8G8B8 format, the green mask would be 0x0000ff00
    dwBBitMask: int             # Blue (or V) mask for reading color data. For instance, given the A8R8G8B8 format, the blue mask would be 0x000000ff
    dwABitMask: int             # Alpha mask for reading alpha data. dwFlags must include DDPF_ALPHAPIXELS or DDPF_ALPHA. For instance, given the A8R8G8B8 format, the alpha mask would be 0xff000000

#endregion

#region DDS_HEADER_DXT10
# https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header-dxt10
# https://docs.microsoft.com/en-us/windows/win32/api/d3d10/ne-d3d10-d3d10_resource_dimension

class DDS_ALPHA_MODE(Enum):
    ALPHA_MODE_UNKNOWN = 0
    ALPHA_MODE_STRAIGHT = 1
    ALPHA_MODE_PREMULTIPLIED = 2
    ALPHA_MODE_OPAQUE = 3
    ALPHA_MODE_CUSTOM = 4

class D3D10_RESOURCE_DIMENSION(Enum):
    UNKNOWN = 0         # Resource is of unknown type
    BUFFER = 1          # Resource is a buffer
    TEXTURE1D = 2       # Resource is a 1D texture. The dwWidth member of DDS_HEADER specifies the size of the texture. Typically, you set the dwHeight member of DDS_HEADER to 1; you also must set the DDSD_HEIGHT flag in the dwFlags member of DDS_HEADER
    TEXTURE2D = 3       # Resource is a 2D texture with an area specified by the dwWidth and dwHeight members of DDS_HEADER. You can also use this type to identify a cube-map texture. For more information about how to identify a cube-map texture, see miscFlag and arraySize members
    TEXTURE3D = 4       # Resource is a 3D texture with a volume specified by the dwWidth, dwHeight, and dwDepth members of DDS_HEADER. You also must set the DDSD_DEPTH flag in the dwFlags member of DDS_HEADER

# DDS header extension to handle resource arrays, DXGI pixel formats that don't map to the legacy Microsoft DirectDraw pixel format structures, and additional metadata
class DDS_HEADER_DXT10:
    struct = ('<5I', 20)
    def __init__(self, tuple):
        self.dxgiFormat, \
        self.resourceDimension, \
        self.miscFlag, \
        self.arraySize, \
        self.miscFlags2 = tuple
        # remap
        self.dxgiFormat = DXGI_FORMAT(self.dxgiFormat)
        self.resourceDimension = D3D10_RESOURCE_DIMENSION(self.resourceDimension)
        self.dwCaps2 = DDSCAPS2(self.dwCaps2)

#endregion

#region DDS_HEADER
# https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
# https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide

# Flags to indicate which members contain valid data
class DDSD(Flag):
    CAPS = 0x00000001           # Required in every .dds file
    HEIGHT = 0x00000002         # Required in every .dds file
    WIDTH = 0x00000004          # Required in every .dds file
    PITCH = 0x00000008          # Required when pitch is provided for an uncompressed texture
    PIXELFORMAT = 0x00001000    # Required in every .dds file
    MIPMAPCOUNT = 0x00020000    # Required in a mipmapped texture
    LINEARSIZE = 0x00080000     # Required when pitch is provided for a compressed texture
    DEPTH = 0x00800000          # Required in a depth texture
    HEADER_FLAGS_TEXTURE = CAPS | HEIGHT | WIDTH | PIXELFORMAT
    HEADER_FLAGS_MIPMAP = MIPMAPCOUNT
    HEADER_FLAGS_VOLUME = DEPTH
    HEADER_FLAGS_PITCH = PITCH
    HEADER_FLAGS_LINEARSIZE = LINEARSIZE

# Specifies the complexity of the surfaces stored
class DDSCAPS(Flag):
    COMPLEX = 0x00000008        # Optional; must be used on any file that contains more than one surface (a mipmap, a cubic environment map, or mipmapped volume texture)
    TEXTURE = 0x00001000        # Required
    MIPMAP = 0x00400000         # Optional; should be used for a mipmap
    SURFACE_FLAGS_MIPMAP = COMPLEX | MIPMAP
    SURFACE_FLAGS_TEXTURE = TEXTURE
    SURFACE_FLAGS_CUBEMAP = COMPLEX

# Additional detail about the surfaces stored
class DDSCAPS2(Flag):
    CUBEMAP = 0x00000200            # Required for a cube map
    CUBEMAPPOSITIVEX = 0x00000400   # Required when these surfaces are stored in a cube map
    CUBEMAPNEGATIVEX = 0x00000800   # Required when these surfaces are stored in a cube map
    CUBEMAPPOSITIVEY = 0x00001000   # Required when these surfaces are stored in a cube map
    CUBEMAPNEGATIVEY = 0x00002000   # Required when these surfaces are stored in a cube map
    CUBEMAPPOSITIVEZ = 0x00004000   # Required when these surfaces are stored in a cube map
    CUBEMAPNEGATIVEZ = 0x00008000   # Required when these surfaces are stored in a cube map
    VOLUME = 0x00200000             # Required for a volume texture
    CUBEMAP_POSITIVEX = CUBEMAP | CUBEMAPPOSITIVEX
    CUBEMAP_NEGATIVEX = CUBEMAP | CUBEMAPNEGATIVEX
    CUBEMAP_POSITIVEY = CUBEMAP | CUBEMAPPOSITIVEY
    CUBEMAP_NEGATIVEY = CUBEMAP | CUBEMAPNEGATIVEY
    CUBEMAP_POSITIVEZ = CUBEMAP | CUBEMAPPOSITIVEZ
    CUBEMAP_NEGATIVEZ = CUBEMAP | CUBEMAPNEGATIVEZ
    CUBEMAP_ALLFACES = CUBEMAPPOSITIVEX | CUBEMAPNEGATIVEX | CUBEMAPPOSITIVEY | CUBEMAPNEGATIVEY | CUBEMAPPOSITIVEZ | CUBEMAPNEGATIVEZ
    FLAGS_VOLUME = VOLUME

# Describes a DDS file header
class DDS_HEADER:
    struct = (f'<7I44s{DDS_PIXELFORMAT.struct[0]}5I', 124)
    MAGIC = 0x20534444 # DDS_
    def __init__(self, tuple):
        ddspf = self.ddspf = DDS_PIXELFORMAT()
        self.dwSize, \
        self.dwFlags, \
        self.dwHeight, \
        self.dwWidth, \
        self.dwPitchOrLinearSize, \
        self.dwDepth, \
        self.dwMipMapCount, \
        self.dwReserved1, \
        ddspf.dwSize, \
        ddspf.dwFlags, \
        ddspf.dwFourCC, \
        ddspf.dwRGBBitCount, \
        ddspf.dwRBitMask, \
        ddspf.dwGBitMask, \
        ddspf.dwBBitMask, \
        ddspf.dwABitMask, \
        self.dwCaps, \
        self.dwCaps2, \
        self.dwCaps3, \
        self.dwCaps4, \
        self.dwReserved2 = tuple
        # remap
        self.dwFlags = DDSD(self.dwFlags)
        ddspf.dwFlags = DDPF(ddspf.dwFlags)
        ddspf.dwFourCC = FourCC(ddspf.dwFourCC)
        self.dwCaps = DDSCAPS(self.dwCaps)
        self.dwCaps2 = DDSCAPS2(self.dwCaps2)

    # Verifies this instance
    def verify(self):
        if self.dwSize != 124: raise Exception(f'Invalid DDS file header size: {dwSize}.')
        elif not self.dwFlags & (DDSD.HEIGHT | DDSD.WIDTH): raise Exception(f'Invalid DDS file flags: {dwFlags}.')
        elif not self.dwCaps & DDSCAPS.TEXTURE: raise Exception(f'Invalid DDS file caps: {dwCaps}.')
        elif self.ddspf.dwSize != 32: raise Exception(f'Invalid DDS file pixel format size: {ddspf.dwSize}.')

    @staticmethod
    def read(r: Reader, readMagic: bool = True) -> (DDS_HEADER, DDS_HEADER_DXT10, object, bytes):
        if readMagic:
            magic = r.readUInt32()
            if magic != DDS_HEADER.MAGIC: raise Exception(f'Invalid DDS file magic: "{magic}".')
        header = r.readS(DDS_HEADER)
        header.verify()
        ddspf = header.ddspf
        headerDxt10 = r.ReadT(DDS_HEADER_DXT10) if ddspf.dwFourCC == FourCC.DX10 else None
        match ddspf.dwFourCC:
            case 0: format = _makeformat(ddspf)
            case FourCC.DXT1: format = (FourCC.DXT1, 8, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1)
            case FourCC.DXT3: format = (FourCC.DXT3, 16, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureUnityFormat.DXT3_POLYFILL, TextureUnrealFormat.DXT3)
            case FourCC.DXT5: format = (FourCC.DXT5, 16, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureUnityFormat.DXT5, TextureUnrealFormat.DXT5)
            case FourCC.DX10: 
                match headerDxt10.dxgiFormat:
                    case DXGI_FORMAT.BC1_UNORM: format = (BC1_UNORM, 8, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureGLFormat.CompressedRgbaS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1)
                    case DXGI_FORMAT.BC1_UNORM_SRGB: format = (BC1_UNORM_SRGB, 8, TextureGLFormat.CompressedSrgbS3tcDxt1Ext, TextureGLFormat.CompressedSrgbS3tcDxt1Ext, TextureUnityFormat.DXT1, TextureUnrealFormat.DXT1)
                    case DXGI_FORMAT.BC2_UNORM: format = (BC2_UNORM, 16, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureGLFormat.CompressedRgbaS3tcDxt3Ext, TextureUnityFormat.DXT3_POLYFILL, TextureUnrealFormat.DXT3)
                    case DXGI_FORMAT.BC2_UNORM_SRGB : format = (BC2_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaS3tcDxt1Ext, TextureGLFormat.CompressedSrgbAlphaS3tcDxt1Ext, TextureUnityFormat.Unknown, TextureUnrealFormat.Unknown)
                    case DXGI_FORMAT.BC3_UNORM: format = (BC3_UNORM, 16, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureGLFormat.CompressedRgbaS3tcDxt5Ext, TextureUnityFormat.DXT5, TextureUnrealFormat.DXT5)
                    case DXGI_FORMAT.BC3_UNORM_SRGB: format = (BC3_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaS3tcDxt5Ext, TextureGLFormat.CompressedSrgbAlphaS3tcDxt5Ext, TextureUnityFormat.Unknown, TextureUnrealFormat.Unknown)
                    case DXGI_FORMAT.BC4_UNORM: format = (BC4_UNORM, 8, TextureGLFormat.CompressedRedRgtc1, TextureGLFormat.CompressedRedRgtc1, TextureUnityFormat.BC4, TextureUnrealFormat.BC4)
                    case DXGI_FORMAT.BC4_SNORM: format = (BC4_SNORM, 8, TextureGLFormat.CompressedSignedRedRgtc1, TextureGLFormat.CompressedSignedRedRgtc1, TextureUnityFormat.BC4, TextureUnrealFormat.BC4)
                    case DXGI_FORMAT.BC5_UNORM: format = (BC5_UNORM, 16, TextureGLFormat.CompressedRgRgtc2, TextureGLFormat.CompressedRgRgtc2, TextureUnityFormat.BC5, TextureUnrealFormat.BC5)
                    case DXGI_FORMAT.BC5_SNORM: format = (BC5_SNORM, 16, TextureGLFormat.CompressedSignedRgRgtc2, TextureGLFormat.CompressedSignedRgRgtc2, TextureUnityFormat.BC5, TextureUnrealFormat.BC5)
                    case DXGI_FORMAT.BC6H_UF16: format = (BC6H_UF16, 16, TextureGLFormat.CompressedRgbBptcUnsignedFloat, TextureGLFormat.CompressedRgbBptcUnsignedFloat, TextureUnityFormat.BC6H, TextureUnrealFormat.BC6H)
                    case DXGI_FORMAT.BC6H_SF16: format = (BC6H_SF16, 16, TextureGLFormat.CompressedRgbBptcSignedFloat, TextureGLFormat.CompressedRgbBptcSignedFloat, TextureUnityFormat.BC6H, TextureUnrealFormat.BC6H)
                    case DXGI_FORMAT.BC7_UNORM: format = (BC7_UNORM, 16, TextureGLFormat.CompressedRgbaBptcUnorm, TextureGLFormat.CompressedRgbaBptcUnorm, TextureUnityFormat.BC7, TextureUnrealFormat.BC7)
                    case DXGI_FORMAT.BC7_UNORM_SRGB: format = (BC7_UNORM_SRGB, 16, TextureGLFormat.CompressedSrgbAlphaBptcUnorm, TextureGLFormat.CompressedSrgbAlphaBptcUnorm, TextureUnityFormat.BC7, TextureUnrealFormat.BC7)
                    case DXGI_FORMAT.R8_UNORM: format = (R8_UNORM, 1, (TextureGLFormat.R8, TextureGLPixelFormat.Red, TextureGLPixelType.Byte), (TextureGLFormat.R8, TextureGLPixelFormat.Red, TextureGLPixelType.Byte), TextureUnityFormat.R8, TextureUnrealFormat.R8)
                    case _: raise Exception(f'Unknown dxgiFormat: 0x{ddspf.dxgiFormat:x}')
            # BC4U/BC4S/ATI2/BC55/R8G8_B8G8/G8R8_G8B8/UYVY-packed/YUY2-packed unsupported
            case _: raise Exception(f'Unknown dwFourCC: 0x{ddspf.dwFourCC:x}')
        return header, headerDxt10, format, r.readToEnd()

    @staticmethod
    def write(w: Writer, header: DDS_HEADER, headerDxt10: DDS_HEADER_DXT10, object, bytes: bytearray, writeMagic: bool = True) -> None:
        pass

    @staticmethod
    def _makeformat(f: DDS_PIXELFORMAT) -> object:
        return ('Raw', f.dwRGBBitCount >> 2, \
            (TextureGLFormat.Rgba, TextureGLPixelFormat.Rgba, TextureGLPixelType.Byte), \
            (TextureGLFormat.Rgba, TextureGLPixelFormat.Rgba, TextureGLPixelType.Byte), \
            TextureUnityFormat.RGBA32, \
            TextureUnrealFormat.R8G8B8A8)

    @staticmethod
    def convertDxt3ToDtx5(data: bytes, width: int, height: int, mipMaps: int) -> None:
        raise Exception('Not Implemented')

#endregion

#region DXGI_FORMAT
# https://docs.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format

# DirectX Graphics Infrastructure formats
class DXGI_FORMAT(Enum):
    UNKNOWN = 0 # The format is not known.
    R32G32B32A32_TYPELESS = 1 # A four-component, 128-bit typeless format that supports 32 bits per channel including alpha.
    R32G32B32A32_FLOAT = 2 # A four-component, 128-bit floating-point format that supports 32 bits per channel including alpha.
    R32G32B32A32_UINT = 3 # A four-component, 128-bit unsigned-integer format that supports 32 bits per channel including alpha.
    R32G32B32A32_SINT = 4 # A four-component, 128-bit signed-integer format that supports 32 bits per channel including alpha.
    R32G32B32_TYPELESS = 5 # A three-component, 96-bit typeless format that supports 32 bits per color channel.
    R32G32B32_FLOAT = 6 # A three-component, 96-bit floating-point format that supports 32 bits per color channel.
    R32G32B32_UINT = 7 # A three-component, 96-bit unsigned-integer format that supports 32 bits per color channel.
    R32G32B32_SINT = 8 # A three-component, 96-bit signed-integer format that supports 32 bits per color channel.
    R16G16B16A16_TYPELESS = 9 # A four-component, 64-bit typeless format that supports 16 bits per channel including alpha.
    R16G16B16A16_FLOAT = 10 # A four-component, 64-bit floating-point format that supports 16 bits per channel including alpha.
    R16G16B16A16_UNORM = 11 # A four-component, 64-bit unsigned-normalized-integer format that supports 16 bits per channel including alpha.
    R16G16B16A16_UINT = 12 # A four-component, 64-bit unsigned-integer format that supports 16 bits per channel including alpha.
    R16G16B16A16_SNORM = 13 # A four-component, 64-bit signed-normalized-integer format that supports 16 bits per channel including alpha.
    R16G16B16A16_SINT = 14 # A four-component, 64-bit signed-integer format that supports 16 bits per channel including alpha.
    R32G32_TYPELESS = 15 # A two-component, 64-bit typeless format that supports 32 bits for the red channel and 32 bits for the green channel.
    R32G32_FLOAT = 16 # A two-component, 64-bit floating-point format that supports 32 bits for the red channel and 32 bits for the green channel.
    R32G32_UINT = 17 # A two-component, 64-bit unsigned-integer format that supports 32 bits for the red channel and 32 bits for the green channel.
    R32G32_SINT = 18 # A two-component, 64-bit signed-integer format that supports 32 bits for the red channel and 32 bits for the green channel.
    R32G8X24_TYPELESS = 19 # A two-component, 64-bit typeless format that supports 32 bits for the red channel, 8 bits for the green channel, and 24 bits are unused.
    D32_FLOAT_S8X24_UINT = 20 # A 32-bit floating-point component, and two unsigned-integer components (with an additional 32 bits). This format supports 32-bit depth, 8-bit stencil, and 24 bits are unused.
    R32_FLOAT_X8X24_TYPELESS = 21 # A 32-bit floating-point component, and two typeless components (with an additional 32 bits). This format supports 32-bit red channel, 8 bits are unused, and 24 bits are unused.
    X32_TYPELESS_G8X24_UINT = 22 # A 32-bit typeless component, and two unsigned-integer components (with an additional 32 bits). This format has 32 bits unused, 8 bits for green channel, and 24 bits are unused.
    R10G10B10A2_TYPELESS = 23 # A four-component, 32-bit typeless format that supports 10 bits for each color and 2 bits for alpha.
    R10G10B10A2_UNORM = 24 # A four-component, 32-bit unsigned-normalized-integer format that supports 10 bits for each color and 2 bits for alpha.
    R10G10B10A2_UINT = 25 # A four-component, 32-bit unsigned-integer format that supports 10 bits for each color and 2 bits for alpha.
    R11G11B10_FLOAT = 26 # Three partial-precision floating-point numbers encoded into a single 32-bit value (a variant of s10e5, which is sign bit, 10-bit mantissa, and 5-bit biased (15) exponent).
                            # There are no sign bits, and there is a 5-bit biased (15) exponent for each channel, 6-bit mantissa for R and G, and a 5-bit mantissa for B.
    R8G8B8A8_TYPELESS = 27 # A four-component, 32-bit typeless format that supports 8 bits per channel including alpha.
    R8G8B8A8_UNORM = 28 # A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits per channel including alpha.
    R8G8B8A8_UNORM_SRGB = 29 # A four-component, 32-bit unsigned-normalized integer sRGB format that supports 8 bits per channel including alpha.
    R8G8B8A8_UINT = 30 # A four-component, 32-bit unsigned-integer format that supports 8 bits per channel including alpha.
    R8G8B8A8_SNORM = 31 # A four-component, 32-bit signed-normalized-integer format that supports 8 bits per channel including alpha.
    R8G8B8A8_SINT = 32 # A four-component, 32-bit signed-integer format that supports 8 bits per channel including alpha.
    R16G16_TYPELESS = 33 # A two-component, 32-bit typeless format that supports 16 bits for the red channel and 16 bits for the green channel.
    R16G16_FLOAT = 34 # A two-component, 32-bit floating-point format that supports 16 bits for the red channel and 16 bits for the green channel.
    R16G16_UNORM = 35 # A two-component, 32-bit unsigned-normalized-integer format that supports 16 bits each for the green and red channels.
    R16G16_UINT = 36 # A two-component, 32-bit unsigned-integer format that supports 16 bits for the red channel and 16 bits for the green channel.
    R16G16_SNORM = 37 # A two-component, 32-bit signed-normalized-integer format that supports 16 bits for the red channel and 16 bits for the green channel.
    R16G16_SINT = 38 # A two-component, 32-bit signed-integer format that supports 16 bits for the red channel and 16 bits for the green channel.
    R32_TYPELESS = 39 # A single-component, 32-bit typeless format that supports 32 bits for the red channel.
    D32_FLOAT = 40 # A single-component, 32-bit floating-point format that supports 32 bits for depth.
    R32_FLOAT = 41 # A single-component, 32-bit floating-point format that supports 32 bits for the red channel.
    R32_UINT = 42 # A single-component, 32-bit unsigned-integer format that supports 32 bits for the red channel.
    R32_SINT = 43 # A single-component, 32-bit signed-integer format that supports 32 bits for the red channel.
    R24G8_TYPELESS = 44 # A two-component, 32-bit typeless format that supports 24 bits for the red channel and 8 bits for the green channel.
    D24_UNORM_S8_UINT = 45 # A 32-bit z-buffer format that supports 24 bits for depth and 8 bits for stencil.
    R24_UNORM_X8_TYPELESS = 46 # A 32-bit format, that contains a 24 bit, single-component, unsigned-normalized integer, with an additional typeless 8 bits. This format has 24 bits red channel and 8 bits unused.
    X24_TYPELESS_G8_UINT = 47 # A 32-bit format, that contains a 24 bit, single-component, typeless format, with an additional 8 bit unsigned integer component. This format has 24 bits unused and 8 bits green channel.
    R8G8_TYPELESS = 48 # A two-component, 16-bit typeless format that supports 8 bits for the red channel and 8 bits for the green channel.
    R8G8_UNORM = 49 # A two-component, 16-bit unsigned-normalized-integer format that supports 8 bits for the red channel and 8 bits for the green channel.
    R8G8_UINT = 50 # A two-component, 16-bit unsigned-integer format that supports 8 bits for the red channel and 8 bits for the green channel.
    R8G8_SNORM = 51 # A two-component, 16-bit signed-normalized-integer format that supports 8 bits for the red channel and 8 bits for the green channel.
    R8G8_SINT = 52 # A two-component, 16-bit signed-integer format that supports 8 bits for the red channel and 8 bits for the green channel.
    R16_TYPELESS = 53 # A single-component, 16-bit typeless format that supports 16 bits for the red channel.
    R16_FLOAT = 54 # A single-component, 16-bit floating-point format that supports 16 bits for the red channel.
    D16_UNORM = 55 # A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for depth.
    R16_UNORM = 56 # A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for the red channel.
    R16_UINT = 57 # A single-component, 16-bit unsigned-integer format that supports 16 bits for the red channel.
    R16_SNORM = 58 # A single-component, 16-bit signed-normalized-integer format that supports 16 bits for the red channel.
    R16_SINT = 59 # A single-component, 16-bit signed-integer format that supports 16 bits for the red channel.
    R8_TYPELESS = 60 # A single-component, 8-bit typeless format that supports 8 bits for the red channel.
    R8_UNORM = 61 # A single-component, 8-bit unsigned-normalized-integer format that supports 8 bits for the red channel.
    R8_UINT = 62 # A single-component, 8-bit unsigned-integer format that supports 8 bits for the red channel.
    R8_SNORM = 63 # A single-component, 8-bit signed-normalized-integer format that supports 8 bits for the red channel.
    R8_SINT = 64 # A single-component, 8-bit signed-integer format that supports 8 bits for the red channel.
    A8_UNORM = 65 # A single-component, 8-bit unsigned-normalized-integer format for alpha only.
    R1_UNORM = 66 # A single-component, 1-bit unsigned-normalized integer format that supports 1 bit for the red channel.
    R9G9B9E5_SHAREDEXP = 67 # Three partial-precision floating-point numbers encoded into a single 32-bit value all sharing the same 5-bit exponent (variant of s10e5, which is sign bit, 10-bit mantissa, and 5-bit biased (15) exponent).
                            # There is no sign bit, and there is a shared 5-bit biased (15) exponent and a 9-bit mantissa for each channel.
    R8G8_B8G8_UNORM = 68 # A four-component, 32-bit unsigned-normalized-integer format. This packed RGB format is analogous to the UYVY format. Each 32-bit block describes a pair of pixels: (R8, G8, B8) and (R8, G8, B8) where the R8/B8 values are repeated, and the G8 values are unique to each pixel.
                            # Width must be even.
    G8R8_G8B8_UNORM = 69 # A four-component, 32-bit unsigned-normalized-integer format. This packed RGB format is analogous to the YUY2 format. Each 32-bit block describes a pair of pixels: (R8, G8, B8) and (R8, G8, B8) where the R8/B8 values are repeated, and the G8 values are unique to each pixel.
                            # Width must be even.
    BC1_TYPELESS = 70 # Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC1_UNORM = 71 # Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC1_UNORM_SRGB = 72 # Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC2_TYPELESS = 73 # Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC2_UNORM = 74 # Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC2_UNORM_SRGB = 75 # Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC3_TYPELESS = 76 # Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC3_UNORM = 77 # Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC3_UNORM_SRGB = 78 # Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC4_TYPELESS = 79 # One-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC4_UNORM = 80 # One-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC4_SNORM = 81 # One-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC5_TYPELESS = 82 # Two-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC5_UNORM = 83 # Two-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC5_SNORM = 84 # Two-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    B5G6R5_UNORM = 85 # A three-component, 16-bit unsigned-normalized-integer format that supports 5 bits for blue, 6 bits for green, and 5 bits for red.
    B5G5R5A1_UNORM = 86 # A four-component, 16-bit unsigned-normalized-integer format that supports 5 bits for each color channel and 1-bit alpha.
    B8G8R8A8_UNORM = 87 # A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8-bit alpha.
    B8G8R8X8_UNORM = 88 # A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8 bits unused.
    R10G10B10_XR_BIAS_A2_UNORM = 89 # A four-component, 32-bit 2.8-biased fixed-point format that supports 10 bits for each color channel and 2-bit alpha.
    B8G8R8A8_TYPELESS = 90 # A four-component, 32-bit typeless format that supports 8 bits for each channel including alpha.
    B8G8R8A8_UNORM_SRGB = 91 # A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each channel including alpha.
    B8G8R8X8_TYPELESS = 92 # A four-component, 32-bit typeless format that supports 8 bits for each color channel, and 8 bits are unused.
    B8G8R8X8_UNORM_SRGB = 93 # A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each color channel, and 8 bits are unused.
    BC6H_TYPELESS = 94 # A typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC6H_UF16 = 95 # A block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC6H_SF16 = 96 # A block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC7_TYPELESS = 97 # A typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC7_UNORM = 98 # A block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    BC7_UNORM_SRGB = 99 # A block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.
    AYUV = 100 # Most common YUV 4:4:4 video resource format.
    Y410 = 101 # 10-bit per channel packed YUV 4:4:4 video resource format.
    Y416 = 102 # 16-bit per channel packed YUV 4:4:4 video resource format.
    NV12 = 103 # Most common YUV 4:2:0 video resource format.
    P010 = 104 # 10-bit per channel planar YUV 4:2:0 video resource format.
    P016 = 105 # 16-bit per channel planar YUV 4:2:0 video resource format.
    _420_OPAQUE = 106 # 8-bit per channel planar YUV 4:2:0 video resource format.
    YUY2 = 107 # Most common YUV 4:2:2 video resource format.
    Y210 = 108 # 10-bit per channel packed YUV 4:2:2 video resource format.
    Y216 = 109 # 16-bit per channel packed YUV 4:2:2 video resource format.
    NV11 = 110 # Most common planar YUV 4:1:1 video resource format.
    AI44 = 111 # 4-bit palletized YUV format that is commonly used for DVD subpicture.
    IA44 = 112 # 4-bit palletized YUV format that is commonly used for DVD subpicture.
    P8 = 113 # 8-bit palletized format that is used for palletized RGB data when the processor processes ISDB-T data and for palletized YUV data when the processor processes BluRay data.
    A8P8 = 114 # 8-bit palletized format with 8 bits of alpha that is used for palletized YUV data when the processor processes BluRay data.
    B4G4R4A4_UNORM = 115 # A four-component, 16-bit unsigned-normalized integer format that supports 4 bits for each channel including alpha.
    P208 = 130 # A video format; an 8-bit version of a hybrid planar 4:2:2 format.
    V208 = 131 # An 8 bit YCbCrA 4:4 rendering format.
    V408 = 132 # An 8 bit YCbCrA 4:4:4:4 rendering format.
    SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 133 # SAMPLER_FEEDBACK_MIN_MIP_OPAQUE
    SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 134 # SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE

#endregion