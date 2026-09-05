#if false
namespace OpenStack.Graphics.DirectX_
{
    public unsafe struct DDS_HEADER
    {
        /// <summary>
        /// ReadAndDecode
        /// </summary>
        public void ReadAndDecode(EmptyTexture source, BinaryReader r)
        {
            var hasMipmaps = source.HasMipmaps = dwCaps.HasFlag(DDSCAPS.MIPMAP);
            source.Mipmaps = hasMipmaps ? (ushort)dwMipMapCount : (ushort)1U;
            source.Width = (int)dwWidth;
            source.Height = (int)dwHeight;
            source.BytesPerPixel = 4;
            // If the DDS file contains uncompressed data.
            if (ddspf.dwFlags.HasFlag(DDPF.RGB))
            {
                // some permutation of RGB
                if (!ddspf.dwFlags.HasFlag(DDPF.ALPHAPIXELS)) throw new NotImplementedException("Unsupported DDS file pixel format.");
                else
                {
                    // some permutation of RGBA
                    if (ddspf.dwRGBBitCount != 32) throw new FormatException("Invalid DDS file pixel format.");
                    else if (ddspf.dwBBitMask == 0x000000FF && ddspf.dwGBitMask == 0x0000FF00 && ddspf.dwRBitMask == 0x00FF0000 && ddspf.dwABitMask == 0xFF000000) source.UnityFormat = TextureUnityFormat.BGRA32;
                    else if (ddspf.dwABitMask == 0x000000FF && ddspf.dwRBitMask == 0x0000FF00 && ddspf.dwGBitMask == 0x00FF0000 && ddspf.dwBBitMask == 0xFF000000) source.UnityFormat = TextureUnityFormat.ARGB32;
                    else throw new NotImplementedException("Unsupported DDS file pixel format.");
                    source.Data = new byte[!hasMipmaps ? (int)(dwPitchOrLinearSize * dwHeight) : TextureHelper.GetMipmapDataSize(source.Width, source.Height, source.BytesPerPixel)];
                    r.ReadToEnd(source.Data);
                }
            }
            else if (ddspf.dwFourCC == DXT1)
            {
                source.UnityFormat = TextureUnityFormat.ARGB32;
                source.Data = DecodeDXT1ToARGB(r.ReadToEnd(), dwWidth, dwHeight, ddspf, source.Mipmaps);
            }
            else if (ddspf.dwFourCC == DXT3)
            {
                source.UnityFormat = TextureUnityFormat.ARGB32;
                source.Data = DecodeDXT3ToARGB(r.ReadToEnd(), dwWidth, dwHeight, ddspf, source.Mipmaps);
            }
            else if (ddspf.dwFourCC == DXT5)
            {
                source.UnityFormat = TextureUnityFormat.ARGB32;
                source.Data = DecodeDXT5ToARGB(r.ReadToEnd(), dwWidth, dwHeight, ddspf, source.Mipmaps);
            }
            else throw new NotImplementedException("Unsupported DDS file pixel format.");
        }

        #region Decode

        /// <summary>
        /// Decodes a DXT1-compressed 4x4 block of texels using a prebuilt 4-color color table.
        /// </summary>
        /// <remarks>See https://msdn.microsoft.com/en-us/library/windows/desktop/bb694531(v=vs.85).aspx#BC1 </remarks>
        static GXColor32[] DecodeDXT1TexelBlock(BinaryReader r, GXColor[] colorTable)
        {
            Debug.Assert(colorTable.Length == 4);
            // Read pixel color indices.
            var colorIndices = new uint[16];
            var colorIndexBytes = new byte[4];
            r.Read(colorIndexBytes, 0, colorIndexBytes.Length);
            const uint bitsPerColorIndex = 2;
            for (var rowIndex = 0U; rowIndex < 4; rowIndex++)
            {
                var rowBaseColorIndexIndex = 4 * rowIndex;
                var rowBaseBitOffset = 8 * rowIndex;
                for (var columnIndex = 0U; columnIndex < 4; columnIndex++)
                {
                    // Color indices are arranged from right to left.
                    var bitOffset = rowBaseBitOffset + (bitsPerColorIndex * (3 - columnIndex));
                    colorIndices[rowBaseColorIndexIndex + columnIndex] = (uint)MathX.GetBits(bitOffset, bitsPerColorIndex, colorIndexBytes);
                }
            }
            // Calculate pixel colors.
            var colors = new GXColor32[16];
            for (var i = 0; i < 16; i++) colors[i] = colorTable[colorIndices[i]];
            return colors;
        }

        /// <summary>
        /// Builds a 4-color color table for a DXT1-compressed 4x4 block of texels and then decodes the texels.
        /// </summary>
        /// <remarks>See https://msdn.microsoft.com/en-us/library/windows/desktop/bb694531(v=vs.85).aspx#BC1 </remarks>
        static GXColor32[] DecodeDXT1TexelBlock(BinaryReader r, bool containsAlpha)
        {
            // Create the color table.
            var colorTable = new GXColor[4];
            colorTable[0] = r.ReadUInt16().B565ToColor();
            colorTable[1] = r.ReadUInt16().B565ToColor();
            if (!containsAlpha)
            {
                colorTable[2] = GXColor.Lerp(colorTable[0], colorTable[1], 1.0f / 3);
                colorTable[3] = GXColor.Lerp(colorTable[0], colorTable[1], 2.0f / 3);
            }
            else
            {
                colorTable[2] = GXColor.Lerp(colorTable[0], colorTable[1], 1.0f / 2);
                colorTable[3] = new GXColor(0, 0, 0, 0);
            }
            // Calculate pixel colors.
            return DecodeDXT1TexelBlock(r, colorTable);
        }

        /// <summary>
        /// Decodes a DXT3-compressed 4x4 block of texels.
        /// </summary>
        /// <remarks>See https://msdn.microsoft.com/en-us/library/windows/desktop/bb694531(v=vs.85).aspx#BC2 </remarks>
        static GXColor32[] DecodeDXT3TexelBlock(BinaryReader r)
        {
            // Read compressed pixel alphas.
            var compressedAlphas = new byte[16];
            for (var rowIndex = 0; rowIndex < 4; rowIndex++)
            {
                var compressedAlphaRow = r.ReadUInt16();
                // Each compressed alpha is 4 bits.
                for (var columnIndex = 0; columnIndex < 4; columnIndex++) compressedAlphas[(4 * rowIndex) + columnIndex] = (byte)((compressedAlphaRow >> (columnIndex * 4)) & 0xF);
            }
            // Calculate pixel alphas.
            var alphas = new byte[16];
            for (var i = 0; i < 16; i++) { var alphaPercent = (float)compressedAlphas[i] / 15; alphas[i] = (byte)Math.Round(alphaPercent * 255); }
            // Create the color table.
            var colorTable = new GXColor[4];
            colorTable[0] = r.ReadUInt16().B565ToColor();
            colorTable[1] = r.ReadUInt16().B565ToColor();
            colorTable[2] = GXColor.Lerp(colorTable[0], colorTable[1], 1.0f / 3);
            colorTable[3] = GXColor.Lerp(colorTable[0], colorTable[1], 2.0f / 3);
            // Calculate pixel colors.
            var colors = DecodeDXT1TexelBlock(r, colorTable);
            for (var i = 0; i < 16; i++) colors[i].A = alphas[i];
            return colors;
        }

        /// <summary>
        /// Decodes a DXT5-compressed 4x4 block of texels.
        /// </summary>
        /// <remarks>See https://msdn.microsoft.com/en-us/library/windows/desktop/bb694531(v=vs.85).aspx#BC3 </remarks>
        static GXColor32[] DecodeDXT5TexelBlock(BinaryReader r)
        {
            // Create the alpha table.
            var alphaTable = new float[8];
            alphaTable[0] = r.ReadByte();
            alphaTable[1] = r.ReadByte();
            if (alphaTable[0] > alphaTable[1])
            {
                for (var i = 0; i < 6; i++) alphaTable[2 + i] = MathX.Lerp(alphaTable[0], alphaTable[1], (float)(1 + i) / 7);
            }
            else
            {
                for (var i = 0; i < 4; i++) alphaTable[2 + i] = MathX.Lerp(alphaTable[0], alphaTable[1], (float)(1 + i) / 5);
                alphaTable[6] = 0;
                alphaTable[7] = 255;
            }

            // Read pixel alpha indices.
            var alphaIndices = new uint[16];
            var alphaIndexBytesRow0 = new byte[3];
            r.Read(alphaIndexBytesRow0, 0, alphaIndexBytesRow0.Length); Array.Reverse(alphaIndexBytesRow0); // Take care of little-endianness.
            var alphaIndexBytesRow1 = new byte[3];
            r.Read(alphaIndexBytesRow1, 0, alphaIndexBytesRow1.Length); Array.Reverse(alphaIndexBytesRow1); // Take care of little-endianness.
            const uint bitsPerAlphaIndex = 3U;
            alphaIndices[0] = (uint)MathX.GetBits(21, bitsPerAlphaIndex, alphaIndexBytesRow0);
            alphaIndices[1] = (uint)MathX.GetBits(18, bitsPerAlphaIndex, alphaIndexBytesRow0);
            alphaIndices[2] = (uint)MathX.GetBits(15, bitsPerAlphaIndex, alphaIndexBytesRow0);
            alphaIndices[3] = (uint)MathX.GetBits(12, bitsPerAlphaIndex, alphaIndexBytesRow0);
            alphaIndices[4] = (uint)MathX.GetBits(9, bitsPerAlphaIndex, alphaIndexBytesRow0);
            alphaIndices[5] = (uint)MathX.GetBits(6, bitsPerAlphaIndex, alphaIndexBytesRow0);
            alphaIndices[6] = (uint)MathX.GetBits(3, bitsPerAlphaIndex, alphaIndexBytesRow0);
            alphaIndices[7] = (uint)MathX.GetBits(0, bitsPerAlphaIndex, alphaIndexBytesRow0);
            alphaIndices[8] = (uint)MathX.GetBits(21, bitsPerAlphaIndex, alphaIndexBytesRow1);
            alphaIndices[9] = (uint)MathX.GetBits(18, bitsPerAlphaIndex, alphaIndexBytesRow1);
            alphaIndices[10] = (uint)MathX.GetBits(15, bitsPerAlphaIndex, alphaIndexBytesRow1);
            alphaIndices[11] = (uint)MathX.GetBits(12, bitsPerAlphaIndex, alphaIndexBytesRow1);
            alphaIndices[12] = (uint)MathX.GetBits(9, bitsPerAlphaIndex, alphaIndexBytesRow1);
            alphaIndices[13] = (uint)MathX.GetBits(6, bitsPerAlphaIndex, alphaIndexBytesRow1);
            alphaIndices[14] = (uint)MathX.GetBits(3, bitsPerAlphaIndex, alphaIndexBytesRow1);
            alphaIndices[15] = (uint)MathX.GetBits(0, bitsPerAlphaIndex, alphaIndexBytesRow1);
            // Create the color table.
            var colorTable = new GXColor[4];
            colorTable[0] = r.ReadUInt16().B565ToColor();
            colorTable[1] = r.ReadUInt16().B565ToColor();
            colorTable[2] = GXColor.Lerp(colorTable[0], colorTable[1], 1.0f / 3);
            colorTable[3] = GXColor.Lerp(colorTable[0], colorTable[1], 2.0f / 3);
            // Calculate pixel colors.
            var colors = DecodeDXT1TexelBlock(r, colorTable);
            for (var i = 0; i < 16; i++) colors[i].A = (byte)Math.Round(alphaTable[alphaIndices[i]]);
            return colors;
        }

        /// <summary>
        /// Copies a decoded texel block to a texture's data buffer. Takes into account DDS mipmap padding.
        /// </summary>
        /// <param name="decodedTexels">The decoded DDS texels.</param>
        /// <param name="argb">The texture's data buffer.</param>
        /// <param name="baseARGBIndex">The desired offset into the texture's data buffer. Used for mipmaps.</param>
        /// <param name="baseRowIndex">The base row index in the texture where decoded texels are copied.</param>
        /// <param name="baseColumnIndex">The base column index in the texture where decoded texels are copied.</param>
        /// <param name="textureWidth">The width of the texture.</param>
        /// <param name="textureHeight">The height of the texture.</param>
        static void CopyDecodedTexelBlock(GXColor32[] decodedTexels, byte[] argb, int baseARGBIndex, int baseRowIndex, int baseColumnIndex, int textureWidth, int textureHeight)
        {
            for (var i = 0; i < 4; i++) // row
                for (var j = 0; j < 4; j++) // column
                {
                    var rowIndex = baseRowIndex + i;
                    var columnIndex = baseColumnIndex + j;
                    // Don't copy padding on mipmaps.
                    if (rowIndex < textureHeight && columnIndex < textureWidth)
                    {
                        var decodedTexelIndex = (4 * i) + j;
                        var color = decodedTexels[decodedTexelIndex];
                        var ARGBPixelOffset = (textureWidth * rowIndex) + columnIndex;
                        var basePixelARGBIndex = baseARGBIndex + (4 * ARGBPixelOffset);
                        argb[basePixelARGBIndex] = color.A;
                        argb[basePixelARGBIndex + 1] = color.R;
                        argb[basePixelARGBIndex + 2] = color.G;
                        argb[basePixelARGBIndex + 3] = color.B;
                    }
                }
        }

        static byte[] DecodeDXT1ToARGB(byte[] compressedData, uint width, uint height, DDS_PIXELFORMAT pixelFormat, uint mipmapCount) => DecodeDXTToARGB(1, compressedData, width, height, pixelFormat, mipmapCount);
        static byte[] DecodeDXT3ToARGB(byte[] compressedData, uint width, uint height, DDS_PIXELFORMAT pixelFormat, uint mipmapCount) => DecodeDXTToARGB(3, compressedData, width, height, pixelFormat, mipmapCount);
        static byte[] DecodeDXT5ToARGB(byte[] compressedData, uint width, uint height, DDS_PIXELFORMAT pixelFormat, uint mipmapCount) => DecodeDXTToARGB(5, compressedData, width, height, pixelFormat, mipmapCount);

        /// <summary>
        /// Decodes DXT data to ARGB.
        /// </summary>
        static byte[] DecodeDXTToARGB(int DXTVersion, byte[] compressedData, uint width, uint height, DDS_PIXELFORMAT pixelFormat, uint mipmapCount)
        {
            var alphaFlag = pixelFormat.dwFlags.HasFlag(DDPF.ALPHAPIXELS);
            var containsAlpha = alphaFlag || (pixelFormat.dwRGBBitCount == 32 && pixelFormat.dwABitMask != 0);
            using var r = new BinaryReader(new MemoryStream(compressedData));
            var argb = new byte[TextureHelper.GetMipmapDataSize((int)width, (int)height, 4)];
            var mipMapWidth = (int)width;
            var mipMapHeight = (int)height;
            var baseARGBIndex = 0;
            for (var mipMapIndex = 0; mipMapIndex < mipmapCount; mipMapIndex++)
            {
                for (var rowIndex = 0; rowIndex < mipMapHeight; rowIndex += 4)
                    for (var columnIndex = 0; columnIndex < mipMapWidth; columnIndex += 4)
                    {
                        if (r.Position() == r.BaseStream.Length) return argb;
                        GXColor32[] colors = null;
                        colors = DXTVersion switch // Doing a switch instead of using a delegate for speed.
                        {
                            1 => DecodeDXT1TexelBlock(r, containsAlpha),
                            3 => DecodeDXT3TexelBlock(r),
                            5 => DecodeDXT5TexelBlock(r),
                            _ => throw new NotImplementedException($"Tried decoding a DDS file using an unsupported DXT format: DXT {DXTVersion}"),
                        };
                        CopyDecodedTexelBlock(colors, argb, baseARGBIndex, rowIndex, columnIndex, mipMapWidth, mipMapHeight);
                    }
                baseARGBIndex += mipMapWidth * mipMapHeight * 4;
                mipMapWidth /= 2;
                mipMapHeight /= 2;
            }
            return argb;
        }

        #endregion
    }
}
#endif