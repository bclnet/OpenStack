using System.Diagnostics;
using System.Globalization;

namespace System
{
    public static class MathX
    {
        public const int SizeOfVector2 = sizeof(float) * 2;
        public const int SizeOfVector3 = sizeof(float) * 3;
        public const int SizeOfVector4 = sizeof(float) * 4;

        public const float Pi = 3.141593f;
        public const float PiOver2 = 1.570796f;
        public const float PiOver3 = 1.047198f;
        public const float PiOver4 = 0.7853982f;
        public const float PiOver6 = 0.5235988f;


        #region Safe

        public static double Safe(double value) => value == double.NegativeInfinity
            ? double.MinValue
            : value == double.PositiveInfinity ? double.MaxValue : value == double.NaN ? 0 : value;

        #endregion

        #region Clamp

        public static int Clamp(int value, int min, int max)
            => value < min ? min
            : value > max ? max
            : value;

        public static float Clamp(float value, float min, float max)
            => value < min ? min
            : value > max ? max
            : value;

        public static float Clamp(float value)
            => value >= 0f ? value <= 1f
            ? value : 1f
            : 0f;

        #endregion

        #region Lerp

        public static float InverseLerp(float a, float b, float value) => a == b ? 0f : Clamp((value - a) / (b - a));

        public static float Lerp(float a, float b, float t) => a + ((b - a) * Clamp(t));

        public static float LerpAngle(float a, float b, float t)
        {
            var num = Repeat(b - a, 360f);
            if (num > 180f)
                num -= 360f;
            return (a + (num * Clamp(t)));
        }

        public static float LerpUnclamped(float a, float b, float t) => a + ((b - a) * t);

        #endregion

        public static float Repeat(float t, float length) => Clamp(t - ((float)Math.Floor(t / length) * length), 0f, length);

        #region Swap

        public static void Swap<T>(ref T a, ref T b) { var tmp = a; a = b; b = tmp; }
        public static ulong SwapEndian(ulong value) { var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt64(bytes, 0); }
        public static uint SwapEndian(uint value) { var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt32(bytes, 0); }
        public static int SwapEndian(int value) { var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToInt32(bytes, 0); }
        public static float SwapEndian(float value) { var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToSingle(bytes, 0); }
        public static ushort SwapEndian(ushort value) { var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt16(bytes, 0); }
        //
        public static ulong SwapEndianIf(ulong value, bool bigEndian = false) { if (!bigEndian) return value; var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt64(bytes, 0); }
        public static uint SwapEndianIf(uint value, bool bigEndian = false) { if (!bigEndian) return value; var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt32(bytes, 0); }
        public static int SwapEndianIf(int value, bool bigEndian = false) { if (!bigEndian) return value; var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToInt32(bytes, 0); }
        public static float SwapEndianIf(float value, bool bigEndian = false) { if (!bigEndian) return value; var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToSingle(bytes, 0); }
        public static ushort SwapEndianIf(ushort value, bool bigEndian = false) { if (!bigEndian) return value; var bytes = BitConverter.GetBytes(value); Array.Reverse(bytes, 0, bytes.Length); return BitConverter.ToUInt16(bytes, 0); }


        #endregion

        /// <summary>
        /// Extracts a range of bits from a byte array.
        /// </summary>
        /// <param name="bitOffset">An offset in bits from the most significant bit (byte 0, bit 0) of the byte array.</param>
        /// <param name="bitCount">The number of bits to extract. Cannot exceed 64.</param>
        /// <param name="bytes">A big-endian byte array.</param>
        /// <returns>A ulong containing the right-shifted extracted bits.</returns>
        public static ulong GetBits(uint bitOffset, uint bitCount, byte[] bytes)
        {
            Debug.Assert(bitCount <= 64 && (bitOffset + bitCount) <= (8 * bytes.Length));
            var bits = 0UL;
            var remainingBitCount = bitCount;
            var byteIndex = bitOffset / 8;
            var bitIndex = bitOffset - (8 * byteIndex);
            while (remainingBitCount > 0)
            {
                // Read bits from the byte array.
                var numBitsLeftInByte = 8 - bitIndex;
                var numBitsReadNow = Math.Min(remainingBitCount, numBitsLeftInByte);
                var unmaskedBits = (uint)bytes[byteIndex] >> (int)(8 - (bitIndex + numBitsReadNow));
                var bitMask = 0xFFu >> (int)(8 - numBitsReadNow);
                var bitsReadNow = unmaskedBits & bitMask;

                // Store the bits we read.
                bits <<= (int)numBitsReadNow;
                bits |= bitsReadNow;

                // Prepare for the next iteration.
                bitIndex += numBitsReadNow;

                if (bitIndex == 8) { byteIndex++; bitIndex = 0; }
                remainingBitCount -= numBitsReadNow;
            }
            return bits;
        }

        public static bool TryParseInt32(string s, out int result) => !s.StartsWith("0x") ? int.TryParse(s, out result) : int.TryParse(s.Substring(2), NumberStyles.HexNumber, null, out result);

        public static short Reverse(short value) => (short)(
                ((value & 0xFF00) >> 8) << 0 |
                ((value & 0x00FF) >> 0) << 8);
        public static ushort Reverse(ushort value) => (ushort)(
                ((value & 0xFF00) >> 8) << 0 |
                ((value & 0x00FF) >> 0) << 8);
        public static int Reverse(int value) => (int)(
                (((uint)value & 0xFF000000) >> 24) << 0 |
                (((uint)value & 0x00FF0000) >> 16) << 8 |
                (((uint)value & 0x0000FF00) >> 8) << 16 |
                (((uint)value & 0x000000FF) >> 0) << 24);
        public static uint Reverse(uint value) => (uint)(
                ((value & 0xFF000000) >> 24) << 0 |
                ((value & 0x00FF0000) >> 16) << 8 |
                ((value & 0x0000FF00) >> 8) << 16 |
                ((value & 0x000000FF) >> 0) << 24);
        public static long Reverse(long value) => (long)(
                (((ulong)value & 0xFF00000000000000UL) >> 56) << 0 |
                (((ulong)value & 0x00FF000000000000UL) >> 48) << 8 |
                (((ulong)value & 0x0000FF0000000000UL) >> 40) << 16 |
                (((ulong)value & 0x000000FF00000000UL) >> 32) << 24 |
                (((ulong)value & 0x00000000FF000000UL) >> 24) << 32 |
                (((ulong)value & 0x0000000000FF0000UL) >> 16) << 40 |
                (((ulong)value & 0x000000000000FF00UL) >> 8) << 48 |
                (((ulong)value & 0x00000000000000FFUL) >> 0) << 56);
        public static ulong Reverse(ulong value) => (ulong)(
                ((value & 0xFF00000000000000UL) >> 56) << 0 |
                ((value & 0x00FF000000000000UL) >> 48) << 8 |
                ((value & 0x0000FF0000000000UL) >> 40) << 16 |
                ((value & 0x000000FF00000000UL) >> 32) << 24 |
                ((value & 0x00000000FF000000UL) >> 24) << 32 |
                ((value & 0x0000000000FF0000UL) >> 16) << 40 |
                ((value & 0x000000000000FF00UL) >> 8) << 48 |
                ((value & 0x00000000000000FFUL) >> 0) << 56);
    }
}