// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/BitStream.cs
// PORT-SHA: 09d3020aa16a4f8f
// PORT-STATUS: done
//
// LSB-first bit reader over an in-memory buffer, used by the LZ/Huffman
// decoders. Direct structural port: C# `class BitStream` -> `struct BitStream`,
// since it is a plain value with no inheritance or identity semantics.

/// Holds between 16 and 32 live bits in `bitbuf`, refilled 16 at a time.
#[derive(Debug, Clone)]
pub struct BitStream<'a> {
    /// Holds between 16 and 32 bits.
    bitbuf: u32,
    /// How many bits does `bitbuf` hold?
    bitcount: i32,
    source: &'a [u8],
    p: usize,
    pend: usize,
}

impl<'a> BitStream<'a> {
    /// C# `BitStream(byte[] source)`.
    ///
    /// The C# guard is `source.Length >= 0`, which is always true and so always
    /// reads `lword(source, 0)` — that panics on a source shorter than 2 bytes.
    /// The length check here is what the original evidently meant.
    pub fn new(source: &'a [u8]) -> Self {
        let bitbuf = if source.len() >= 2 {
            lword(source, 0)
        } else if source.len() == 1 {
            lbyte(source, 0)
        } else {
            0
        };
        Self { bitbuf, bitcount: 16, source, p: 0, pend: source.len() }
    }

    /// C# `Remain`.
    #[inline]
    pub fn remain(&self) -> usize {
        self.pend.saturating_sub(self.p)
    }

    /// C# `Fix()` — fixes up the stream after literals were read out of the
    /// data stream directly.
    pub fn fix(&mut self) {
        self.bitcount -= 16;
        self.bitbuf &= (1u32 << self.bitcount).wrapping_sub(1); // drop the top 16 bits
        match self.remain() {
            0 => {}
            1 => self.bitbuf |= lbyte(self.source, self.p) << self.bitcount,
            _ => self.bitbuf |= lword(self.source, self.p) << self.bitcount,
        }
        self.bitcount += 16;
    }

    /// C# `Peek(uint mask)` — returns some bits without consuming them.
    #[inline]
    pub fn peek(&self, mask: u32) -> u32 {
        self.bitbuf & mask
    }

    /// C# `Advance(int n)` — consumes `n` bits, refilling from the source.
    pub fn advance(&mut self, n: i32) {
        self.bitbuf >>= n;
        self.bitcount -= n;
        if self.bitcount < 16 {
            self.p += 2;
            match self.remain() {
                0 => {}
                1 => self.bitbuf |= lbyte(self.source, self.p) << self.bitcount,
                _ => self.bitbuf |= lword(self.source, self.p) << self.bitcount,
            }
            self.bitcount += 16;
        }
    }

    /// C# `Read(uint mask, int n)` — peek then advance.
    #[inline]
    pub fn read(&mut self, mask: u32, n: i32) -> u32 {
        let r = self.peek(mask);
        self.advance(n);
        r
    }

    /// C# `ReadByte()` — reads a raw literal byte, bypassing the bit buffer.
    /// Callers must follow this with [`fix`](Self::fix), as in the C# original.
    #[inline]
    pub fn read_byte(&mut self) -> u8 {
        let b = self.source[self.p];
        self.p += 1;
        b
    }
}

#[inline]
fn lword(p: &[u8], offset: usize) -> u32 {
    ((p[offset + 1] as u32) << 8) + p[offset] as u32
}

#[inline]
fn lbyte(p: &[u8], offset: usize) -> u32 {
    p[offset] as u32
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn reads_lsb_first() {
        let mut bs = BitStream::new(&[0b1010_0101, 0x00, 0x00, 0x00]);
        assert_eq!(bs.read(0b111, 3), 0b101);
        assert_eq!(bs.read(0b111, 3), 0b100);
    }

    #[test]
    fn short_sources_do_not_panic() {
        // The C# constructor indexes source[1] unconditionally and panics here.
        assert_eq!(BitStream::new(&[]).remain(), 0);
        assert_eq!(BitStream::new(&[0xFF]).peek(0xFF), 0xFF);
    }
}
