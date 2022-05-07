using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace OpenStack.Graphics
{
    public interface ITextureManager<Texture>
    {
        Texture DefaultTexture { get; }
        Texture LoadTexture(object key, out IDictionary<string, object> data);
        void PreloadTexture(string path);
        public Texture BuildSolidTexture(int width, int height, params float[] rgba);
        public Texture BuildNormalMap(Texture source, float strength);
    }

    public enum TextureFlags : int
    {
#pragma warning disable 1591
        SUGGEST_CLAMPS = 0x00000001,
        SUGGEST_CLAMPT = 0x00000002,
        SUGGEST_CLAMPU = 0x00000004,
        NO_LOD = 0x00000008,
        CUBE_TEXTURE = 0x00000010,
        VOLUME_TEXTURE = 0x00000020,
        TEXTURE_ARRAY = 0x00000040,
#pragma warning restore 1591
    }

    public enum TextureUnityFormat : short
    {
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

    public enum TextureGLFormat
    {
        DepthComponent = 0x1902,    //: GL_DEPTH_COMPONENT = 0x1902
        Red = 0x1903,               //: GL_RED = 0x1903
        RedExt = 0x1903,            //: GL_RED_EXT = 0x1903
        Rgb = 0x1907,               //: GL_RGB = 0x1907
        Rgba = 0x1908,              //: GL_RGBA = 0x1908
        R3G3B2 = 0x2A10,            //: GL_R3_G3_B2 = 0x2A10
        Alpha4 = 0x803B,            //: GL_ALPHA4 = 0x803B
        Alpha8 = 0x803C,            //: GL_ALPHA8 = 0x803C
        Alpha12 = 0x803D,           //: GL_ALPHA12 = 0x803D
        Alpha16 = 0x803E,           //: GL_ALPHA16 = 0x803E
        Luminance4 = 0x803F,        //: GL_LUMINANCE4 = 0x803F
        Luminance8 = 0x8040,        //: GL_LUMINANCE8 = 0x8040
        Luminance12 = 0x8041,       //: GL_LUMINANCE12 = 0x8041
        Luminance16 = 0x8042,       //: GL_LUMINANCE16 = 0x8042
        Luminance4Alpha4 = 0x8043,  //: GL_LUMINANCE4_ALPHA4 = 0x8043
        Luminance6Alpha2 = 0x8044,  //: GL_LUMINANCE6_ALPHA2 = 0x8044
        Luminance8Alpha8 = 0x8045,  //: GL_LUMINANCE8_ALPHA8 = 0x8045
        Luminance12Alpha4 = 0x8046, //: GL_LUMINANCE12_ALPHA4 = 0x8046
        Luminance12Alpha12 = 0x8047,//: GL_LUMINANCE12_ALPHA12 = 0x8047
        Luminance16Alpha16 = 0x8048,//: GL_LUMINANCE16_ALPHA16 = 0x8048
        Intensity = 0x8049,         //: GL_INTENSITY = 0x8049
        Intensity4 = 0x804A,        //: GL_INTENSITY4 = 0x804A
        Intensity8 = 0x804B,        //: GL_INTENSITY8 = 0x804B
        Intensity12 = 0x804C,       //: GL_INTENSITY12 = 0x804C
        Intensity16 = 0x804D,       //: GL_INTENSITY16 = 0x804D
        Rgb2Ext = 0x804E,           //: GL_RGB2_EXT = 0x804E
        Rgb4 = 0x804F,              //: GL_RGB4 = 0x804F
        Rgb4Ext = 0x804F,           //: GL_RGB4_EXT = 0x804F
        Rgb5 = 0x8050,              //: GL_RGB5 = 0x8050
        Rgb5Ext = 0x8050,           //: GL_RGB5_EXT = 0x8050
        Rgb8 = 0x8051,              //: GL_RGB8 = 0x8051
        Rgb8Ext = 0x8051,           //: GL_RGB8_EXT = 0x8051
        Rgb8Oes = 0x8051,           //: GL_RGB8_OES = 0x8051
        Rgb10 = 0x8052,             //: GL_RGB10 = 0x8052
        Rgb10Ext = 0x8052,          //: GL_RGB10_EXT = 0x8052
        Rgb12 = 0x8053,             //: GL_RGB12 = 0x8053
        Rgb12Ext = 0x8053,          //: GL_RGB12_EXT = 0x8053
        Rgb16 = 0x8054,             //: GL_RGB16 = 0x8054
        Rgb16Ext = 0x8054,          //: GL_RGB16_EXT = 0x8054
        Rgba4 = 0x8056,             //: GL_RGBA4 = 0x8056
        Rgba4Ext = 0x8056,          //: GL_RGBA4_EXT = 0x8056
        Rgba4Oes = 0x8056,          //: GL_RGBA4_OES = 0x8056
        Rgb5A1 = 0x8057,            //: GL_RGB5_A1 = 0x8057
        Rgb5A1Ext = 0x8057,         //: GL_RGB5_A1_EXT = 0x8057
        Rgb5A1Oes = 0x8057,         //: GL_RGB5_A1_OES = 0x8057
        Rgba8 = 0x8058,             //: GL_RGBA8 = 0x8058
        Rgba8Ext = 0x8058,          //: GL_RGBA8_EXT = 0x8058
        Rgba8Oes = 0x8058,          //: GL_RGBA8_OES = 0x8058
        Rgb10A2 = 0x8059,           //: GL_RGB10_A2 = 0x8059
        Rgb10A2Ext = 0x8059,        //: GL_RGB10_A2_EXT = 0x8059
        Rgba12 = 0x805A,            //: GL_RGBA12 = 0x805A
        Rgba12Ext = 0x805A,         //: GL_RGBA12_EXT = 0x805A
        Rgba16 = 0x805B,            //: GL_RGBA16 = 0x805B
        Rgba16Ext = 0x805B,         //: GL_RGBA16_EXT = 0x805B
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
        DepthComponent16Arb = 0x81A5,   //: GL_DEPTH_COMPONENT16_ARB = 0x81A5
        DepthComponent16Oes = 0x81A5,   //: GL_DEPTH_COMPONENT16_OES = 0x81A5
        DepthComponent16Sgix = 0x81A5,  //: GL_DEPTH_COMPONENT16_SGIX = 0x81A5
        DepthComponent24Arb = 0x81A6,   //: GL_DEPTH_COMPONENT24_ARB = 0x81A6
        DepthComponent24Oes = 0x81A6,   //: GL_DEPTH_COMPONENT24_OES = 0x81A6
        DepthComponent24Sgix = 0x81A6,  //: GL_DEPTH_COMPONENT24_SGIX = 0x81A6
        DepthComponent32Arb = 0x81A7,   //: GL_DEPTH_COMPONENT32_ARB = 0x81A7
        DepthComponent32Oes = 0x81A7,   //: GL_DEPTH_COMPONENT32_OES = 0x81A7
        DepthComponent32Sgix = 0x81A7,  //: GL_DEPTH_COMPONENT32_SGIX = 0x81A7
        CompressedRed = 0x8225,         //: GL_COMPRESSED_RED = 0x8225
        CompressedRg = 0x8226,          //: GL_COMPRESSED_RG = 0x8226
        Rg = 0x8227,                //: GL_RG = 0x8227
        R8 = 0x8229,                //: GL_R8 = 0x8229
        R8Ext = 0x8229,             //: GL_R8_EXT = 0x8229
        R16 = 0x822A,               //: GL_R16 = 0x822A
        R16Ext = 0x822A,            //: GL_R16_EXT = 0x822A
        Rg8 = 0x822B,               //: GL_RG8 = 0x822B
        Rg8Ext = 0x822B,            //: GL_RG8_EXT = 0x822B
        Rg16 = 0x822C,              //: GL_RG16 = 0x822C
        Rg16Ext = 0x822C,           //: GL_RG16_EXT = 0x822C
        R16f = 0x822D,              //: GL_R16F = 0x822D
        R16fExt = 0x822D,           //: GL_R16F_EXT = 0x822D
        R32f = 0x822E,              //: GL_R32F = 0x822E
        R32fExt = 0x822E,           //: GL_R32F_EXT = 0x822E
        Rg16f = 0x822F,             //: GL_RG16F = 0x822F
        Rg16fExt = 0x822F,          //: GL_RG16F_EXT = 0x822F
        Rg32f = 0x8230,             //: GL_RG32F = 0x8230
        Rg32fExt = 0x8230,          //: GL_RG32F_EXT = 0x8230
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
        CompressedRgb = 0x84ED,     //: GL_COMPRESSED_RGB = 0x84ED
        CompressedRgba = 0x84EE,    //: GL_COMPRESSED_RGBA = 0x84EE
        DepthStencil = 0x84F9,      //: GL_DEPTH_STENCIL = 0x84F9
        DepthStencilExt = 0x84F9,   //: GL_DEPTH_STENCIL_EXT = 0x84F9
        DepthStencilNv = 0x84F9,    //: GL_DEPTH_STENCIL_NV = 0x84F9
        DepthStencilOes = 0x84F9,   //: GL_DEPTH_STENCIL_OES = 0x84F9
        DepthStencilMesa = 0x8750,  //: GL_DEPTH_STENCIL_MESA = 0x8750
        Rgba32f = 0x8814,           //: GL_RGBA32F = 0x8814
        Rgba32fArb = 0x8814,        //: GL_RGBA32F_ARB = 0x8814
        Rgba32fExt = 0x8814,        //: GL_RGBA32F_EXT = 0x8814
        Rgba16f = 0x881A,           //: GL_RGBA16F = 0x881A
        Rgba16fArb = 0x881A,        //: GL_RGBA16F_ARB = 0x881A
        Rgba16fExt = 0x881A,        //: GL_RGBA16F_EXT = 0x881A
        Rgb16f = 0x881B,            //: GL_RGB16F = 0x881B
        Rgb16fArb = 0x881B,         //: GL_RGB16F_ARB = 0x881B
        Rgb16fExt = 0x881B,         //: GL_RGB16F_EXT = 0x881B
        Depth24Stencil8 = 0x88F0,   //: GL_DEPTH24_STENCIL8 = 0x88F0
        Depth24Stencil8Ext = 0x88F0,//: GL_DEPTH24_STENCIL8_EXT = 0x88F0
        Depth24Stencil8Oes = 0x88F0,//: GL_DEPTH24_STENCIL8_OES = 0x88F0
        R11fG11fB10f = 0x8C3A,      //: GL_R11F_G11F_B10F = 0x8C3A
        R11fG11fB10fApple = 0x8C3A, //: GL_R11F_G11F_B10F_APPLE = 0x8C3A
        R11fG11fB10fExt = 0x8C3A,   //: GL_R11F_G11F_B10F_EXT = 0x8C3A
        Rgb9E5 = 0x8C3D,            //: GL_RGB9_E5 = 0x8C3D
        Rgb9E5Apple = 0x8C3D,       //: GL_RGB9_E5_APPLE = 0x8C3D
        Rgb9E5Ext = 0x8C3D,         //: GL_RGB9_E5_EXT = 0x8C3D
        Srgb = 0x8C40,              //: GL_SRGB = 0x8C40
        SrgbExt = 0x8C40,           //: GL_SRGB_EXT = 0x8C40
        Srgb8 = 0x8C41,             //: GL_SRGB8 = 0x8C41
        Srgb8Ext = 0x8C41,          //: GL_SRGB8_EXT = 0x8C41
        Srgb8Nv = 0x8C41,           //: GL_SRGB8_NV = 0x8C41
        SrgbAlpha = 0x8C42,         //: GL_SRGB_ALPHA = 0x8C42
        SrgbAlphaExt = 0x8C42,      //: GL_SRGB_ALPHA_EXT = 0x8C42
        Srgb8Alpha8 = 0x8C43,       //: GL_SRGB8_ALPHA8 = 0x8C43
        Srgb8Alpha8Ext = 0x8C43,    //: GL_SRGB8_ALPHA8_EXT = 0x8C43
        CompressedSrgb = 0x8C48,    //: GL_COMPRESSED_SRGB = 0x8C48
        CompressedSrgbAlpha = 0x8C49,//: GL_COMPRESSED_SRGB_ALPHA = 0x8C49
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
        DepthComponent32fNv = 0x8DAB,//: GL_DEPTH_COMPONENT32F_NV = 0x8DAB
        Depth32fStencil8Nv = 0x8DAC,//: GL_DEPTH32F_STENCIL8_NV = 0x8DAC
        CompressedRedRgtc1 = 0x8DBB,//: GL_COMPRESSED_RED_RGTC1 = 0x8DBB
        CompressedRedRgtc1Ext = 0x8DBB,//: GL_COMPRESSED_RED_RGTC1_EXT = 0x8DBB
        CompressedSignedRedRgtc1 = 0x8DBC,//: GL_COMPRESSED_SIGNED_RED_RGTC1 = 0x8DBC
        CompressedSignedRedRgtc1Ext = 0x8DBC, //: GL_COMPRESSED_SIGNED_RED_RGTC1_EXT = 0x8DBC
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
        R16SnormExt = 0x8F98,       //: GL_R16_SNORM_EXT = 0x8F98
        Rg16Snorm = 0x8F99,         //: GL_RG16_SNORM = 0x8F99
        Rg16SnormExt = 0x8F99,      //: GL_RG16_SNORM_EXT = 0x8F99
        Rgb16Snorm = 0x8F9A,        //: GL_RGB16_SNORM = 0x8F9A
        Rgb16SnormExt = 0x8F9A,     //: GL_RGB16_SNORM_EXT = 0x8F9A
        Rgb10A2ui = 0x906F,         //: GL_RGB10_A2UI = 0x906F
        CompressedR11Eac = 0x9270,  //: GL_COMPRESSED_R11_EAC = 0x9270
        CompressedSignedR11Eac = 0x9271, //: GL_COMPRESSED_SIGNED_R11_EAC = 0x9271
        CompressedRg11Eac = 0x9272, //: GL_COMPRESSED_RG11_EAC = 0x9272
        CompressedSignedRg11Eac = 0x9273, //: GL_COMPRESSED_SIGNED_RG11_EAC = 0x9273
        CompressedRgb8Etc2 = 0x9274, //: GL_COMPRESSED_RGB8_ETC2 = 0x9274
        CompressedSrgb8Etc2 = 0x9275, //: GL_COMPRESSED_SRGB8_ETC2 = 0x9275
        CompressedRgb8PunchthroughAlpha1Etc2 = 0x9276, //: GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276
        CompressedSrgb8PunchthroughAlpha1Etc2 = 0x9277, //: GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277
        CompressedRgba8Etc2Eac = 0x9278, //: GL_COMPRESSED_RGBA8_ETC2_EAC = 0x9278
        CompressedSrgb8Alpha8Etc2Eac = 0x9279 //: GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC = 0x9279
    }

//    public enum TextureGLFormat_Old : short
//    {
//#pragma warning disable 1591
//        UNKNOWN = 0,
//        DXT1 = 1,
//        DXT5 = 2,
//        I8 = 3,
//        RGBA8888 = 4,
//        R16 = 5,
//        RG1616 = 6,
//        RGBA16161616 = 7,
//        R16F = 8,
//        RG1616F = 9,
//        RGBA16161616F = 10,
//        R32F = 11,
//        RG3232F = 12,
//        RGB323232F = 13,
//        RGBA32323232F = 14,
//        JPEG_RGBA8888 = 15,
//        PNG_RGBA8888 = 16,
//        JPEG_DXT5 = 17,
//        PNG_DXT5 = 18,
//        BC6H = 19,
//        BC7 = 20,
//        ATI2N = 21,
//        IA88 = 22,
//        ETC2 = 23,
//        ETC2_EAC = 24,
//        R11_EAC = 25,
//        RG11_EAC = 26,
//        ATI1N = 27,
//        BGRA8888 = 28,
//#pragma warning restore 1591
//    }
//        switch (info.GLFormat)
//    {
//        case TextureGLFormat.DXT1: format = InternalFormat.CompressedRgbaS3tcDxt1Ext; break; 
//        //case TextureGLFormat.DXT3: format = InternalFormat.CompressedRgbaS3tcDxt3Ext; break;
//        case TextureGLFormat.DXT5: format = InternalFormat.CompressedRgbaS3tcDxt5Ext; break;
//        case TextureGLFormat.ETC2: format = InternalFormat.CompressedRgb8Etc2; break;
//        case TextureGLFormat.ETC2_EAC: format = InternalFormat.CompressedRgba8Etc2Eac; break;
//        case TextureGLFormat.ATI1N: format = InternalFormat.CompressedRedRgtc1; break;
//        case TextureGLFormat.ATI2N: format = InternalFormat.CompressedRgRgtc2; break;
//        case TextureGLFormat.BC6H: format = InternalFormat.CompressedRgbBptcUnsignedFloat; break;
//        case TextureGLFormat.BC7: format = InternalFormat.CompressedRgbaBptcUnorm; break;
//        case TextureGLFormat.RGBA8888: format = InternalFormat.Rgba8; break;
//        case TextureGLFormat.RGBA16161616F: format = InternalFormat.Rgba16f; break;
//        case TextureGLFormat.I8: format = InternalFormat.Intensity8; break;
//        default: Console.Error.WriteLine($"Don't support {info.GLFormat} but don't want to crash either. Using error texture!"); return DefaultTexture;
//    }

/// <summary>
/// ITextureInfo
/// </summary>
public interface ITextureInfo
    {
        byte[] this[int index] { get; set; }
        IDictionary<string, object> Data { get; }
        int Width { get; }
        int Height { get; }
        int Depth { get; }
        TextureFlags Flags { get; }
        TextureUnityFormat UnityFormat { get; }
        TextureGLFormat GLFormat { get; }
        int NumMipMaps { get; }
        void MoveToData();
    }
    
    //public interface ITextureInfoLoad1 : ITextureInfo { }

    /// <summary>
    /// Stores information about a texture.
    /// </summary>
    public class TextureInfo : Dictionary<string, object> //, IGetExplorerInfo
    {
        public int Width, Height, Depth;
        public TextureUnityFormat UnityFormat;
        public TextureGLFormat GLFormat;
        public TextureFlags Flags;
        public bool HasMipmaps;
        public ushort Mipmaps;
        public byte BytesPerPixel;
        public byte[] Data;
        public Action Decompress;
        public int[] CompressedSizeForMipLevel;

        //public TextureInfo() { }
        //public TextureInfo(int width, int height, ushort mipmaps, byte[] data)
        //{
        //    Width = width;
        //    Height = height;
        //    Mipmaps = mipmaps;
        //    Data = data;
        //}

        //List<ExplorerInfoNode> IGetExplorerInfo.GetInfoNodes(ExplorerManager resource, FileMetadata file, object tag)
        //    => new List<ExplorerInfoNode> {
        //    new ExplorerInfoNode(null, new ExplorerContentTab { Type = "Texture", Value = this }),
        //    new ExplorerInfoNode("Texture", items: new List<ExplorerInfoNode> {
        //        new ExplorerInfoNode($"Width: {Width}"),
        //        new ExplorerInfoNode($"Height: {Height}"),
        //        new ExplorerInfoNode($"GLFormat: {GLFormat}"),
        //        new ExplorerInfoNode($"Mipmaps: {Mipmaps}"),
        //    }),
        //};

        public BinaryReader GetDecompressedBuffer(int offset)
           => throw new NotImplementedException();

        //BinaryReader GetDecompressedBuffer()
        //{
        //    if (!IsActuallyCompressedMips)
        //        return Reader;
        //    var outStream = new MemoryStream(GetDecompressedTextureAtMipLevel(MipmapLevelToExtract), false);
        //    return new BinaryReader(outStream); // TODO: dispose
        //}

        //public byte[] GetDecompressedTextureAtMipLevel(int mipLevel)
        //{
        //    var uncompressedSize = CalculateBufferSizeForMipLevel(mipLevel);
        //    if (!IsActuallyCompressedMips)
        //        return Reader.ReadBytes(uncompressedSize);
        //    var compressedSize = CompressedMips[mipLevel];
        //    if (compressedSize >= uncompressedSize)
        //        return Reader.ReadBytes(uncompressedSize);
        //    var input = Reader.ReadBytes(compressedSize);
        //    var output = new Span<byte>(new byte[uncompressedSize]);
        //    LZ4Codec.Decode(input, output);
        //    return output.ToArray();
        //}

        #region MipMap

        public int GetDataOffsetForMip(int mipLevel)
        {
            if (Mipmaps < 2) return 0;

            var offset = 0;
            for (var j = Mipmaps - 1; j > mipLevel; j--)
                offset += CompressedSizeForMipLevel != null
                    ? CompressedSizeForMipLevel[j]
                    : GetMipmapTrueDataSize(Width, Height, Depth, GLFormat, j) * (Flags.HasFlag(TextureFlags.CUBE_TEXTURE) ? 6 : 1);
            return offset;
        }

        public object GetDataSpanForMip(int mipLevel)
        {
            return null;
            //var offset = GetDataOffsetForMip(mipLevel);
            //var dataSize = GetMipmapDataSize(Width, Height, Depth, GLFormat, mipLevel);
            //if (CompressedSizeForMipLevel == null)
            //    return new Span<byte>(Data, 10, 10);
            //var compressedSize = CompressedSizeForMipLevel[mipLevel];
            //if (compressedSize >= dataSize)
            //    return Reader.ReadBytes(dataSize);
            //var input = Reader.ReadBytes(compressedSize);
            //var output = new Span<byte>(new byte[dataSize]);
            //LZ4Codec.Decode(input, output);
            //return output.ToArray();
        }

        public static int GetMipmapCount(int width, int height)
        {
            Debug.Assert(width > 0 && height > 0);
            var longerLength = Math.Max(width, height);
            var mipMapCount = 0;
            var currentLongerLength = longerLength;
            while (currentLongerLength > 0) { mipMapCount++; currentLongerLength /= 2; }
            return mipMapCount;
        }

        public static int GetMipmapDataSize(int width, int height, int bytesPerPixel)
        {
            Debug.Assert(width > 0 && height > 0 && bytesPerPixel > 0);
            var dataSize = 0;
            var currentWidth = width;
            var currentHeight = height;
            while (true)
            {
                dataSize += currentWidth * currentHeight * bytesPerPixel;
                if (currentWidth == 1 && currentHeight == 1) break;
                currentWidth = currentWidth > 1 ? (currentWidth / 2) : currentWidth;
                currentHeight = currentHeight > 1 ? (currentHeight / 2) : currentHeight;
            }
            return dataSize;
        }

        public static int GetMipmapTrueDataSize(int width, int height, int depth, TextureGLFormat format, int mipLevel)
        {
            var bytesPerPixel = format.GetBlockSize();
            var currentWidth = width >> mipLevel;
            var currentHeight = height >> mipLevel;
            var currentDepth = depth >> mipLevel;
            if (currentDepth < 1) currentDepth = 1;
            if (format == TextureGLFormat.CompressedRgbaS3tcDxt1Ext || format == TextureGLFormat.CompressedRgbaS3tcDxt5Ext || format == TextureGLFormat.CompressedRgbBptcUnsignedFloat || format == TextureGLFormat.CompressedRgbaBptcUnorm ||
                format == TextureGLFormat.CompressedRgb8Etc2 || format == TextureGLFormat.CompressedRgba8Etc2Eac || format == TextureGLFormat.CompressedRedRgtc1)
            {
                var misalign = currentWidth % 4;
                if (misalign > 0) currentWidth += 4 - misalign;
                misalign = currentHeight % 4;
                if (misalign > 0) currentHeight += 4 - misalign;
                if (currentWidth < 4 && currentWidth > 0) currentWidth = 4;
                if (currentHeight < 4 && currentHeight > 0) currentHeight = 4;
                if (currentDepth < 4 && currentDepth > 1) currentDepth = 4;
                var numBlocks = (currentWidth * currentHeight) >> 4;
                numBlocks *= currentDepth;
                return numBlocks * bytesPerPixel;
            }
            return currentWidth * currentHeight * currentDepth * bytesPerPixel;
        }

        // TODO: Improve algorithm for images with odd dimensions.
        public static void Downscale4Component32BitPixelsX2(byte[] srcBytes, int srcStartIndex, int srcRowCount, int srcColumnCount, byte[] dstBytes, int dstStartIndex)
        {
            var bytesPerPixel = 4;
            var componentCount = 4;
            Debug.Assert(srcStartIndex >= 0 && srcRowCount >= 0 && srcColumnCount >= 0 && (srcStartIndex + (bytesPerPixel * srcRowCount * srcColumnCount)) <= srcBytes.Length);
            var dstRowCount = srcRowCount / 2;
            var dstColumnCount = srcColumnCount / 2;
            Debug.Assert(dstStartIndex >= 0 && (dstStartIndex + (bytesPerPixel * dstRowCount * dstColumnCount)) <= dstBytes.Length);
            for (var dstRowIndex = 0; dstRowIndex < dstRowCount; dstRowIndex++)
                for (var dstColumnIndex = 0; dstColumnIndex < dstColumnCount; dstColumnIndex++)
                {
                    var srcRowIndex0 = 2 * dstRowIndex;
                    var srcColumnIndex0 = 2 * dstColumnIndex;
                    var srcPixel0Index = (srcColumnCount * srcRowIndex0) + srcColumnIndex0;

                    var srcPixelStartIndices = new int[4];
                    srcPixelStartIndices[0] = srcStartIndex + (bytesPerPixel * srcPixel0Index); // top-left
                    srcPixelStartIndices[1] = srcPixelStartIndices[0] + bytesPerPixel; // top-right
                    srcPixelStartIndices[2] = srcPixelStartIndices[0] + (bytesPerPixel * srcColumnCount); // bottom-left
                    srcPixelStartIndices[3] = srcPixelStartIndices[2] + bytesPerPixel; // bottom-right

                    var dstPixelIndex = (dstColumnCount * dstRowIndex) + dstColumnIndex;
                    var dstPixelStartIndex = dstStartIndex + (bytesPerPixel * dstPixelIndex);
                    for (var componentIndex = 0; componentIndex < componentCount; componentIndex++)
                    {
                        var averageComponent = 0F;
                        for (var srcPixelIndex = 0; srcPixelIndex < srcPixelStartIndices.Length; srcPixelIndex++) averageComponent += srcBytes[srcPixelStartIndices[srcPixelIndex] + componentIndex];
                        averageComponent /= srcPixelStartIndices.Length;
                        dstBytes[dstPixelStartIndex + componentIndex] = (byte)Math.Round(averageComponent);
                    }
                }
        }

        public byte[] GetTexture(int offset)
            => throw new NotImplementedException();

        public byte[] GetDecompressedTextureAtMipLevel(int offset, int v)
            => throw new NotImplementedException();

        internal static int GetMipmapDataSize(int dwWidth, int dwHeight, int v, object bytesPerPixel)
            => throw new NotImplementedException();

        #endregion
    }
}