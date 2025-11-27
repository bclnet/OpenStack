namespace System;

public static class BitReverseX {
    public static readonly byte[] Byte8;

    static BitReverseX() {
        int bits = 8;
        const int n = 1 << 8;
        Byte8 = new byte[n];
        int m = 1, a = n >> 1, j = 2;
        Byte8[0] = 0;
        Byte8[1] = (byte)a;
        while ((--bits) != 0) {
            m <<= 1;
            a >>= 1;
            for (var i = 0; i < m; i++) Byte8[j++] = (byte)(Byte8[i] + a);
        }
    }

    public static uint Reverse32(uint v) => (uint)(
        (Byte8[v & 0xFF] << 24)
            | (Byte8[(v >> 8) & 0xFF] << 16)
            | (Byte8[(v >> 16) & 0xFF] << 8)
            | Byte8[(v >> 24) & 0xFF]
    );
}
