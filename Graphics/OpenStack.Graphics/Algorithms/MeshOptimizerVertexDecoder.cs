using System;

namespace OpenStack.Graphics.Algorithms
{
    public static class MeshOptimizerVertexDecoder
    {
        const byte VertexHeader = 0xa0;
        const int VertexBlockSizeBytes = 8192;
        const int VertexBlockMaxSize = 256;
        const int ByteGroupSize = 16;
        const int TailMaxSize = 32;

        static int GetVertexBlockSize(int vertexSize)
        {
            var result = VertexBlockSizeBytes / vertexSize;
            result &= ~(ByteGroupSize - 1);
            return result < VertexBlockMaxSize ? result : VertexBlockMaxSize;
        }

        static byte Unzigzag8(byte v) => (byte)(-(v & 1) ^ (v >> 1));

        static Span<byte> DecodeBytesGroup(Span<byte> data, Span<byte> destination, int bitslog2)
        {
            var dataOffset = 0;
            int dataVar;
            byte b;

            byte Next(int bits, byte encv)
            {
                var enc = b >> (8 - bits);
                b <<= bits;
                var isSame = enc == (1 << bits) - 1;
                dataVar += isSame ? 1 : 0;
                return isSame ? encv : (byte)enc;
            }

            switch (bitslog2)
            {
                case 0:
                    for (var k = 0; k < ByteGroupSize; k++) destination[k] = 0;
                    return data;
                case 1:
                    dataVar = 4;
                    b = data[dataOffset++];
                    destination[0] = Next(2, data[dataVar]);
                    destination[1] = Next(2, data[dataVar]);
                    destination[2] = Next(2, data[dataVar]);
                    destination[3] = Next(2, data[dataVar]);

                    b = data[dataOffset++];
                    destination[4] = Next(2, data[dataVar]);
                    destination[5] = Next(2, data[dataVar]);
                    destination[6] = Next(2, data[dataVar]);
                    destination[7] = Next(2, data[dataVar]);

                    b = data[dataOffset++];
                    destination[8] = Next(2, data[dataVar]);
                    destination[9] = Next(2, data[dataVar]);
                    destination[10] = Next(2, data[dataVar]);
                    destination[11] = Next(2, data[dataVar]);

                    b = data[dataOffset++];
                    destination[12] = Next(2, data[dataVar]);
                    destination[13] = Next(2, data[dataVar]);
                    destination[14] = Next(2, data[dataVar]);
                    destination[15] = Next(2, data[dataVar]);

                    return data[dataVar..];
                case 2:
                    dataVar = 8;
                    b = data[dataOffset++];
                    destination[0] = Next(4, data[dataVar]);
                    destination[1] = Next(4, data[dataVar]);

                    b = data[dataOffset++];
                    destination[2] = Next(4, data[dataVar]);
                    destination[3] = Next(4, data[dataVar]);

                    b = data[dataOffset++];
                    destination[4] = Next(4, data[dataVar]);
                    destination[5] = Next(4, data[dataVar]);

                    b = data[dataOffset++];
                    destination[6] = Next(4, data[dataVar]);
                    destination[7] = Next(4, data[dataVar]);

                    b = data[dataOffset++];
                    destination[8] = Next(4, data[dataVar]);
                    destination[9] = Next(4, data[dataVar]);

                    b = data[dataOffset++];
                    destination[10] = Next(4, data[dataVar]);
                    destination[11] = Next(4, data[dataVar]);

                    b = data[dataOffset++];
                    destination[12] = Next(4, data[dataVar]);
                    destination[13] = Next(4, data[dataVar]);

                    b = data[dataOffset++];
                    destination[14] = Next(4, data[dataVar]);
                    destination[15] = Next(4, data[dataVar]);

                    return data[dataVar..];
                case 3:
                    data[..ByteGroupSize].CopyTo(destination);
                    return data[ByteGroupSize..];
                default: throw new ArgumentException("Unexpected bit length");
            }
        }

        static Span<byte> DecodeBytes(Span<byte> data, Span<byte> destination)
        {
            if (destination.Length % ByteGroupSize != 0) throw new ArgumentException("Expected data length to be a multiple of ByteGroupSize.");

            var headerSize = ((destination.Length / ByteGroupSize) + 3) / 4;
            var header = data[..];
            data = data[headerSize..];
            for (var i = 0; i < destination.Length; i += ByteGroupSize)
            {
                if (data.Length < TailMaxSize) throw new InvalidOperationException("Cannot decode");
                var headerOffset = i / ByteGroupSize;
                var bitslog2 = (header[headerOffset / 4] >> ((headerOffset % 4) * 2)) & 3;
                data = DecodeBytesGroup(data, destination.Slice(i), bitslog2);
            }
            return data;
        }

        static Span<byte> DecodeVertexBlock(Span<byte> data, Span<byte> vertexData, int vertexCount, int vertexSize, Span<byte> lastVertex)
        {
            if (vertexCount <= 0 || vertexCount > VertexBlockMaxSize) throw new ArgumentException("Expected vertexCount to be between 0 and VertexMaxBlockSize");

            var buffer = new Span<byte>(new byte[VertexBlockMaxSize]);
            var transposed = new Span<byte>(new byte[VertexBlockSizeBytes]);
            var vertexCountAligned = (vertexCount + ByteGroupSize - 1) & ~(ByteGroupSize - 1);

            for (var k = 0; k < vertexSize; ++k)
            {
                data = DecodeBytes(data, buffer.Slice(0, vertexCountAligned));
                var vertexOffset = k;
                var p = lastVertex[k];
                for (var i = 0; i < vertexCount; ++i)
                {
                    var v = (byte)(Unzigzag8(buffer[i]) + p);
                    transposed[vertexOffset] = v;
                    p = v;
                    vertexOffset += vertexSize;
                }
            }

            transposed[..(vertexCount * vertexSize)].CopyTo(vertexData);
            transposed.Slice(vertexSize * (vertexCount - 1), vertexSize).CopyTo(lastVertex);
            return data;
        }

        public static byte[] DecodeVertexBuffer(int vertexCount, int vertexSize, byte[] vertexBuffer)
        {
            if (vertexSize <= 0 || vertexSize > 256) throw new ArgumentException("Vertex size is expected to be between 1 and 256");
            if (vertexSize % 4 != 0) throw new ArgumentException("Vertex size is expected to be a multiple of 4.");
            if (vertexBuffer.Length < 1 + vertexSize) throw new ArgumentException("Vertex buffer is too short.");

            var vertexSpan = new Span<byte>(vertexBuffer);
            var header = vertexSpan[0];
            vertexSpan = vertexSpan.Slice(1);
            if (header != VertexHeader) throw new ArgumentException($"Invalid vertex buffer header, expected {VertexHeader} but got {header}.");

            var lastVertex = new byte[vertexSize];
            vertexSpan.Slice(vertexBuffer.Length - 1 - vertexSize, vertexSize).CopyTo(lastVertex);
            var vertexBlockSize = GetVertexBlockSize(vertexSize);
            var vertexOffset = 0;
            var result = new Span<byte>(new byte[vertexCount * vertexSize]);
            while (vertexOffset < vertexCount)
            {
                var blockSize = vertexOffset + vertexBlockSize < vertexCount
                    ? vertexBlockSize
                    : vertexCount - vertexOffset;
                vertexSpan = DecodeVertexBlock(vertexSpan, result.Slice(vertexOffset * vertexSize), blockSize, vertexSize, lastVertex);
                vertexOffset += blockSize;
            }
            return result.ToArray();
        }
    }
}
