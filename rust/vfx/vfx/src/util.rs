// PORT-SOURCE: Vfx/OpenStack.Vfx/Util.cs
// PORT-SHA: 98a97cc7127da51b
// PORT-STATUS: done
//
// Endianness swaps, alignment, and stream copy/pad helpers used by the disc and
// cartridge readers.
//
// C#-SIDE BUG — `CopyFile` ignores its read count:
//
//     src.Read(buf, 0, size_);          // return value discarded
//     dst.Write(buf, 0, size_);         // writes size_ regardless
//     size -= size_;
//
// `Stream.Read` may legally return fewer bytes than asked for — routinely so on
// network and compressed streams, which is exactly what this VFS layers over.
// When it does, the write emits whatever stale bytes were left in `buf` from the
// previous iteration, and the loop still counts them as copied. **Silent data
// corruption**, no error, no short-copy signal. The port uses `read_exact` and
// reports truncation.
//
// Three members are `throw new NotImplementedException()` with no callers:
// `ToSha256`, `Resize<T>`, and `Seek2`. Not ported; see the note at the bottom.

use std::io::{self, Read, Seek, SeekFrom, Write};

/// C# `static class EndiannessUtils`.
pub mod endianness {
    /// C# `MutatingByteSwap16` — swap each pair of bytes in place.
    ///
    /// The C# guards the length with `Log.Assert`, whose body is empty, so a
    /// misaligned buffer there silently swaps everything but the trailing byte.
    /// `None` here.
    pub fn byte_swap16(a: &mut [u8]) -> Option<()> {
        if a.len() % 2 != 0 {
            return None;
        }
        for c in a.chunks_exact_mut(2) {
            c.swap(0, 1);
        }
        Some(())
    }

    /// C# `MutatingByteSwap32` — reverse each group of four bytes.
    pub fn byte_swap32(a: &mut [u8]) -> Option<()> {
        if a.len() % 4 != 0 {
            return None;
        }
        for c in a.chunks_exact_mut(4) {
            c.reverse();
        }
        Some(())
    }

    /// C# `MutatingShortSwap32` — swap the two 16-bit halves of each 32-bit
    /// group, leaving the bytes within each half in place.
    ///
    /// The C# composes `byteSwap32` then `byteSwap16`; the two together are
    /// exactly a halfword swap, which is what this does directly.
    pub fn short_swap32(a: &mut [u8]) -> Option<()> {
        if a.len() % 4 != 0 {
            return None;
        }
        for c in a.chunks_exact_mut(4) {
            c.swap(0, 2);
            c.swap(1, 3);
        }
        Some(())
    }
}

/// C# `Util.bufferSize`.
const BUFFER_SIZE: usize = 0x10_0000;

/// C# `Util.Align(long s, long alignment)` — round up to a multiple.
///
/// Uses division, so unlike the mask-based `Align` in `polyio` this is correct
/// for non-powers-of-two. Returns `s` unchanged for a zero alignment rather
/// than dividing by zero.
#[inline]
pub fn align(s: u64, alignment: u64) -> u64 {
    if alignment == 0 {
        return s;
    }
    s.div_ceil(alignment) * alignment
}

/// C# `CopyFile(this Stream dst, Stream src, long srcOffset, long size)`.
///
/// Copies exactly `size` bytes from `src_offset`. Errors on a short source
/// rather than emitting stale buffer contents — see the module header.
pub fn copy_file<D: Write, S: Read + Seek>(
    dst: &mut D,
    src: &mut S,
    src_offset: u64,
    size: u64,
) -> io::Result<()> {
    src.seek(SeekFrom::Start(src_offset))?;
    let mut buf = vec![0u8; BUFFER_SIZE.min(size.max(1) as usize)];
    let mut left = size;
    while left > 0 {
        let n = (left as usize).min(buf.len());
        src.read_exact(&mut buf[..n]).map_err(|e| {
            if e.kind() == io::ErrorKind::UnexpectedEof {
                io::Error::new(
                    io::ErrorKind::UnexpectedEof,
                    format!("source ended {left} bytes short of the requested {size}"),
                )
            } else {
                e
            }
        })?;
        dst.write_all(&buf[..n])?;
        left -= n as u64;
    }
    Ok(())
}

/// C# `PadFile(this Stream s, long size, byte padData)`.
pub fn pad_file<W: Write>(w: &mut W, size: u64, pad: u8) -> io::Result<()> {
    if size == 0 {
        return Ok(());
    }
    let buf = vec![pad; BUFFER_SIZE.min(size as usize)];
    let mut left = size;
    while left > 0 {
        let n = (left as usize).min(buf.len());
        w.write_all(&buf[..n])?;
        left -= n as u64;
    }
    Ok(())
}

/// C# `Util.FromHexString(string)`.
///
/// `None` on odd length or a non-hex digit; the C# threw `FormatException`.
pub fn from_hex_string(s: &str) -> Option<Vec<u8>> {
    if s.len() % 2 != 0 {
        return None;
    }
    (0..s.len())
        .step_by(2)
        .map(|i| u8::from_str_radix(&s[i..i + 2], 16).ok())
        .collect()
}

/// C# `Util.ToHexString(byte[])` — lowercase, no separators.
pub fn to_hex_string(d: &[u8]) -> String {
    use std::fmt::Write as _;
    let mut s = String::with_capacity(d.len() * 2);
    for b in d {
        let _ = write!(s, "{b:02x}");
    }
    s
}

// NOT PORTED: `ToSha256(byte[], int, byte*)`, `Resize<T>(this List<T>, int, T)`,
// and `Seek2(this Stream, long)` — all three are
// `throw new NotImplementedException()` with no call sites. Rust equivalents
// exist if they are ever needed: the `sha2` crate, `Vec::resize`, and
// `Seek::seek`. Left out so their absence is visible rather than latent.

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn byte_swap16_pairs_bytes() {
        let mut a = [1u8, 2, 3, 4];
        endianness::byte_swap16(&mut a).unwrap();
        assert_eq!(a, [2, 1, 4, 3]);
    }

    #[test]
    fn byte_swap32_reverses_each_word() {
        let mut a = [1u8, 2, 3, 4, 5, 6, 7, 8];
        endianness::byte_swap32(&mut a).unwrap();
        assert_eq!(a, [4, 3, 2, 1, 8, 7, 6, 5]);
    }

    #[test]
    fn short_swap32_swaps_halfwords_only() {
        let mut a = [1u8, 2, 3, 4];
        endianness::short_swap32(&mut a).unwrap();
        assert_eq!(a, [3, 4, 1, 2]);
        // Equivalent to byte_swap32 then byte_swap16, as the C# composed them.
        let mut b = [1u8, 2, 3, 4];
        endianness::byte_swap32(&mut b).unwrap();
        endianness::byte_swap16(&mut b).unwrap();
        assert_eq!(a, b);
    }

    #[test]
    fn misaligned_buffers_are_rejected() {
        // The C# guard is Log.Assert, which does nothing.
        assert!(endianness::byte_swap16(&mut [1, 2, 3]).is_none());
        assert!(endianness::byte_swap32(&mut [1, 2, 3]).is_none());
    }

    #[test]
    fn align_handles_non_powers_of_two_and_zero() {
        assert_eq!(align(10, 4), 12);
        assert_eq!(align(12, 4), 12);
        assert_eq!(align(10, 3), 12);
        assert_eq!(align(0, 4), 0);
        assert_eq!(align(7, 0), 7, "must not divide by zero");
    }

    #[test]
    fn copy_file_copies_the_requested_window() {
        let mut src = Cursor::new((0u8..20).collect::<Vec<_>>());
        let mut dst = Vec::new();
        copy_file(&mut dst, &mut src, 5, 4).unwrap();
        assert_eq!(dst, vec![5, 6, 7, 8]);
    }

    #[test]
    fn copy_file_reports_a_short_source() {
        // The C# writes stale buffer bytes here and reports success.
        let mut src = Cursor::new(vec![1u8, 2, 3]);
        let mut dst = Vec::new();
        let e = copy_file(&mut dst, &mut src, 0, 100).unwrap_err();
        assert_eq!(e.kind(), io::ErrorKind::UnexpectedEof);
    }

    #[test]
    fn pad_file_writes_the_fill_byte() {
        let mut w = Vec::new();
        pad_file(&mut w, 5, 0xAB).unwrap();
        assert_eq!(w, vec![0xAB; 5]);
        let mut z = Vec::new();
        pad_file(&mut z, 0, 0xFF).unwrap();
        assert!(z.is_empty());
    }

    #[test]
    fn hex_round_trips() {
        let d = [0x00u8, 0x0f, 0xff, 0xa5];
        assert_eq!(to_hex_string(&d), "000fffa5");
        assert_eq!(from_hex_string("000fffa5").unwrap(), d);
    }

    #[test]
    fn malformed_hex_is_none_not_an_exception() {
        assert!(from_hex_string("abc").is_none(), "odd length");
        assert!(from_hex_string("zz").is_none(), "non-hex digit");
        assert_eq!(from_hex_string("").unwrap(), Vec::<u8>::new());
    }
}
