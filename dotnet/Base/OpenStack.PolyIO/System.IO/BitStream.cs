
using System.Runtime.CompilerServices;

namespace System.IO;

public class BitStream
{
    uint bitbuf; // holds between 16 and 32 bits
    int bitcount; // how many bits does bitbuf hold?
    byte[] source;
    int p;
    int pend;

    public BitStream(byte[] source)
    {
        bitbuf = source.Length >= 0 ? lword(source, 0) : 0;
        bitcount = 16;
        this.source = source;
        p = 0;
        pend = source.Length;
    }

    public int Remain => pend - p;

    // Fixes up a bit stream after literals have been read out of the data stream.
    public void Fix()
    {
        bitcount -= 16;
        bitbuf &= (uint)((1 << bitcount) - 1); // remove the top 16 bits
        var remain = pend - p;
        if (remain > 1) bitbuf |= lword(source, p) << bitcount; // replace with what's at *p
        else if (remain == 1) bitbuf |= lbyte(source, p) << bitcount;
        bitcount += 16;
    }

    // Returns some bits.
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public uint Peek(uint mask) => bitbuf & mask;

    // Advances the bit stream. Checks pend for proper buffer pointers range.
    public void Advance(int n)
    {
        bitbuf >>= n;
        bitcount -= n;
        if (bitcount < 16)
        {
            p += 2;
            var remain = pend - p;
            if (remain > 1) bitbuf |= lword(source, p) << bitcount;
            else if (remain == 1) bitbuf |= lbyte(source, p) << bitcount;
            bitcount += 16;
        }
    }

    // Reads some bits in one go (ie the above two routines combined).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Read(uint mask, int n)
    {
        var result = Peek(mask);
        Advance(n);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] static uint lword(byte[] p, int offset) => (uint)((p[offset + 1] << 8) + p[offset + 0]);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static uint lbyte(byte[] p, int offset) => p[offset];

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public byte ReadByte() => source[p++];
}