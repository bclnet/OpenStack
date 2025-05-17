using System.Collections.Generic;
using static System.Console;

namespace OpenStack.Rom.Nintendo._3ds;

public class Space {
    public struct SBuffer(long top, long bottom) {
        public long Top = top;
        public long Bottom = bottom;
    }

    readonly List<SBuffer> Buffers = [];

    public bool AddSpace(long offset, long size) {
        if (size == 0) return true;
        long top = offset, bottom = offset + size;
        for (var i = 0; i < Buffers.Count; i++) {
            var buffer = Buffers[i];
            if ((top >= buffer.Top && top < buffer.Bottom) || (bottom > buffer.Top && bottom <= buffer.Bottom)) { WriteLine($"ERROR: [0x{top:x}, 0x{bottom:x}) [0x{buffer.Top:x}, 0x{buffer.Bottom:x}) overlap\n\n"); return false; }
            if (bottom < buffer.Top) { Buffers.Insert(i, new SBuffer(top, bottom)); return true; }
            else if (bottom == buffer.Top) { buffer.Top = top; return true; }
            else if (top == buffer.Bottom) {
                var next = Buffers[++i];
                if (i == Buffers.Count || bottom < next.Top) { buffer.Bottom = bottom; return true; }
                else if (bottom == next.Top) {
                    buffer.Bottom = next.Bottom;
                    Buffers.RemoveAt(i);
                    return true;
                }
            }
        }
        Buffers.Add(new SBuffer(top, bottom));
        return true;
    }

    public bool SubSpace(long offset, long size) {
        if (size == 0) return true;
        long top = offset, bottom = offset + size;
        for (var i = 0; i < Buffers.Count; i++) {
            var buffer = Buffers[i];
            if (top == buffer.Top && bottom == buffer.Bottom) { Buffers.RemoveAt(i); return true; }
            else if (top == buffer.Top && bottom < buffer.Bottom) { buffer.Top = bottom; return true; }
            else if (top > buffer.Top && bottom == buffer.Bottom) { buffer.Bottom = top; return true; }
            else if (top > buffer.Top && bottom < buffer.Bottom) {
                ++i;
                Buffers.Insert(i, new SBuffer(bottom, buffer.Bottom));
                buffer.Bottom = top;
                return true;
            }
        }
        return false;
    }

    public void Clear() => Buffers.Clear();

    public long GetSpace(long size) {
        foreach (var buffer in Buffers)
            if (buffer.Bottom - buffer.Top >= size) return buffer.Top;
        return -1;
    }
}