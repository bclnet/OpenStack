using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenStack.Graphics
{
    /// <summary>
    /// TextureExtensions
    /// </summary>
    public static partial class TextureExtensions
    {
        static partial class Literal
        {
            public static readonly byte[] IMG_ = Encoding.ASCII.GetBytes("IMG ");
        }

        #region Size

        public static int GetMipMapTrueDataSize(this TextureGLFormat source, ITexture info, int index)
        {
            var bytesPerPixel = source.GetBlockSize();
            var currentWidth = info.Width >> index;
            var currentHeight = info.Height >> index;
            var currentDepth = info.Depth >> index;
            if (currentDepth < 1) currentDepth = 1;
            if (source == TextureGLFormat.CompressedRgbaS3tcDxt1Ext || source == TextureGLFormat.CompressedRgbaS3tcDxt5Ext || source == TextureGLFormat.CompressedRgbBptcUnsignedFloat || source == TextureGLFormat.CompressedRgbaBptcUnorm ||
                source == TextureGLFormat.CompressedRgb8Etc2 || source == TextureGLFormat.CompressedRgba8Etc2Eac || source == TextureGLFormat.CompressedRedRgtc1)
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

        public static int GetBlockSize(this TextureGLFormat source) => source switch
        {
            TextureGLFormat.CompressedRgbaS3tcDxt1Ext => 8,
            TextureGLFormat.CompressedRgbaS3tcDxt5Ext => 6,
            TextureGLFormat.Rgba8 => 4,
            TextureGLFormat.R16 => 2,
            //TextureGLFormat.RG1616 => 4;
            TextureGLFormat.Rgba16f => 8,
            TextureGLFormat.R16f => 2,
            //TextureGLFormat.RG1616F => 4;
            //TextureGLFormat.RGBA16161616F => 8;
            //TextureGLFormat.R32F => 4;
            //TextureGLFormat.RG3232F => 8;
            //TextureGLFormat.RGB323232F => 12;
            //TextureGLFormat.RGBA32323232F => 16;
            TextureGLFormat.CompressedRgbBptcUnsignedFloat => 16,
            TextureGLFormat.CompressedRgbaBptcUnorm => 16,
            TextureGLFormat.Intensity8 => 2,
            TextureGLFormat.CompressedRgb8Etc2 => 8,
            TextureGLFormat.CompressedRgba8Etc2Eac => 16,
            //TextureGLFormat.BGRA8888 => 4;
            TextureGLFormat.CompressedRedRgtc1 => 8,
            _ => 1,
        };

        #endregion

        #region TextureOpaque

        class TextureOpaque : ITexture
        {
            internal byte[][] Bytes;
            byte[] ITexture.this[int index]
            {
                get => Bytes[index];
                set => Bytes[index] = value;
            }
            public IDictionary<string, object> Data { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int Depth { get; set; }
            public TextureFlags Flags { get; set; }
            public object UnityFormat { get; set; }
            public object GLFormat { get; set; }
            public int NumMipMaps { get; set; }
            public void MoveToData() { }
        }

        public static ITexture DecodeOpaque(byte[] opaque)
        {
            using (var s = new MemoryStream(opaque))
            using (var r = new BinaryReader(s))
            {
                var magicString = r.ReadBytes(4);
                if (!Literal.IMG_.SequenceEqual(magicString))
                    throw new FormatException($"Invalid IMG file magic string: \"{Encoding.ASCII.GetString(magicString)}\".");
                var source = new TextureOpaque
                {
                    UnityFormat = (TextureUnityFormat)r.ReadInt16(),
                    GLFormat = (TextureGLFormat)r.ReadInt16(),
                    Flags = (TextureFlags)r.ReadInt32(),
                    Width = r.ReadInt32(),
                    Height = r.ReadInt32(),
                    Depth = r.ReadInt32(),
                    NumMipMaps = r.ReadByte(),
                };
                source.Bytes = new byte[source.NumMipMaps][];
                for (var i = 0; i < source.NumMipMaps; i++)
                    source.Bytes[i] = r.ReadBytes(r.ReadInt32());
                return source;
            }
        }

        public static byte[] EncodeOpaque(this ITexture source)
        {
            using (var s = new MemoryStream())
            using (var r = new BinaryWriter(s))
            {
                r.Write(Literal.IMG_);
                r.Write((short)source.UnityFormat);
                r.Write((short)source.GLFormat);
                r.Write((int)source.Flags);
                r.Write(source.Width);
                r.Write(source.Height);
                r.Write(source.NumMipMaps);
                for (var i = 0; i < source.NumMipMaps; i++)
                {
                    r.Write(source[0].Length); r.Write(source[0]);
                }
                s.Position = 0;
                return s.ToArray();
            }
        }

        #endregion

        #region Color Shift

        // TODO: Move? Unity?
        public static unsafe ITexture FromABGR555(this ITexture source, int index = 0)
        {
            if (!(source.UnityFormat is TextureUnityFormat format))
                throw new InvalidOperationException();
            var W = source.Width; var H = source.Height;
            var pixels = new byte[W * H * 4];
            fixed (byte* pPixels = pixels, pData = source[index])
            {
                var rPixels = (uint*)pPixels;
                var rData = (ushort*)pData;
                for (var i = 0; i < W * H; ++i)
                {
                    var d555 = *rData++;
                    //var a = 0;// (byte)Math.Min(((d555 & 0x8000) >> 15) * 0x1F, byte.MaxValue);
                    //var r = (byte)Math.Min(((d555 & 0x7C00) >> 10) * 8, byte.MaxValue);
                    //var g = (byte)Math.Min(((d555 & 0x03E0) >> 5) * 8, byte.MaxValue);
                    //var b = (byte)Math.Min(((d555 & 0x001F) >> 0) * 8, byte.MaxValue);

                    var r = (byte)Math.Min(((d555 & 0xF800) >> 11) * 8, byte.MaxValue);     // 1111 1000 0000 0000 = F800
                    var g = (byte)Math.Min(((d555 & 0x07C0) >> 6) * 8, byte.MaxValue);      // 0000 0111 1100 0000 = 07C0
                    var b = (byte)Math.Min(((d555 & 0x003E) >> 1) * 8, byte.MaxValue);      // 0000 0000 0011 1110 = 003E
                    var a = (byte)Math.Min((d555 & 0x0001) * 0x1F, byte.MaxValue);          // 0000 0000 0000 0001 = 0001
                    uint color;
                    if (format == TextureUnityFormat.RGBA32)
                        color =
                            ((uint)(a << 24) & 0xFF000000) |
                            ((uint)(b << 16) & 0x00FF0000) |
                            ((uint)(g << 8) & 0x0000FF00) |
                            ((uint)(r << 0) & 0x000000FF);
                    else if (format == TextureUnityFormat.ARGB32)
                        color =
                            ((uint)(b << 24) & 0xFF000000) |
                            ((uint)(g << 16) & 0x00FF0000) |
                            ((uint)(r << 8) & 0x0000FF00) |
                            ((uint)(a << 0) & 0x000000FF);
                    else throw new ArgumentOutOfRangeException(nameof(source.UnityFormat), source.UnityFormat.ToString());
                    *rPixels++ = color;
                }
            }
            source[index] = pixels;
            return source;
        }

        // TODO: Move? Unity?
        public static ITexture From8BitPallet(this ITexture source, byte[][] pallet, TextureUnityFormat palletFormat, int index = 0)
        {
            if (!(source.UnityFormat is TextureUnityFormat format))
                throw new InvalidOperationException();
            if (format != palletFormat)
                throw new InvalidOperationException();
            var b = new MemoryStream();
            var d = source[index];
            for (var y = 0; y < d.Length; y++)
                b.Write(pallet[d[y]], 0, 4);
            source[index] = b.ToArray();
            return source;
        }

        #endregion
    }
}