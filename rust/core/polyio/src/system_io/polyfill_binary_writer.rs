// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/Polyfill+BinaryWriter.cs
// PORT-SHA: 837d324ef083062d
// PORT-STATUS: done
//
// Mirror of `polyfill_binary_reader.rs`. C# extension methods on `BinaryWriter`
// -> a blanket-implemented trait over `Write + Seek`.
//
// C# overloads every endian variant as `WriteE(value)` / `WriteX(value, endian)`
// and lets overload resolution pick by argument type. Rust has no overloading,
// so each width is named: `write_i32_be`, `write_i32_x`, and so on. This is the
// one place the two trees do not line up name-for-name.

use std::io::{self, Seek, SeekFrom, Write};

pub type Result<T> = io::Result<T>;

macro_rules! prim {
    ($le:ident, $be:ident, $x:ident, $ty:ty, $cs:literal) => {
        #[doc = concat!("Little-endian. C# `Write(", $cs, ")`.")]
        #[inline]
        fn $le(&mut self, v: $ty) -> Result<()> {
            self.write_all(&v.to_le_bytes())
        }
        #[doc = concat!("Big-endian. C# `WriteE(", $cs, ")`.")]
        #[inline]
        fn $be(&mut self, v: $ty) -> Result<()> {
            self.write_all(&v.to_be_bytes())
        }
        #[doc = concat!("Endian-switched. C# `WriteX(", $cs, ", bool endian)`.")]
        #[inline]
        fn $x(&mut self, v: $ty, big: bool) -> Result<()> {
            if big { self.$be(v) } else { self.$le(v) }
        }
    };
}

pub trait BinaryWriterExt: Write + Seek {
    // -- primitives ---------------------------------------------------------

    /// C# `Write(byte)`.
    #[inline]
    fn write_u8(&mut self, v: u8) -> Result<()> {
        self.write_all(&[v])
    }

    /// C# `Write(sbyte)`.
    #[inline]
    fn write_i8(&mut self, v: i8) -> Result<()> {
        self.write_all(&[v as u8])
    }

    /// C# `Write(bool)` — one byte.
    #[inline]
    fn write_bool(&mut self, v: bool) -> Result<()> {
        self.write_u8(u8::from(v))
    }

    /// C# `WriteBool32(bool)` — four bytes.
    #[inline]
    fn write_bool32(&mut self, v: bool) -> Result<()> {
        self.write_i32(i32::from(v))
    }

    prim!(write_i16, write_i16_be, write_i16_x, i16, "short");
    prim!(write_u16, write_u16_be, write_u16_x, u16, "ushort");
    prim!(write_i32, write_i32_be, write_i32_x, i32, "int");
    prim!(write_u32, write_u32_be, write_u32_x, u32, "uint");
    prim!(write_i64, write_i64_be, write_i64_x, i64, "long");
    prim!(write_u64, write_u64_be, write_u64_x, u64, "ulong");
    prim!(write_f32, write_f32_be, write_f32_x, f32, "float");
    prim!(write_f64, write_f64_be, write_f64_x, f64, "double");

    /// C# `WriteE(byte[] bytes, int sizeOf)` — byte-swap each `size_of`-wide
    /// element in place, then write.
    fn write_swapped(&mut self, bytes: &[u8], size_of: usize) -> Result<()> {
        if size_of <= 1 {
            return self.write_all(bytes);
        }
        let mut buf = bytes.to_vec();
        for chunk in buf.chunks_mut(size_of) {
            chunk.reverse();
        }
        self.write_all(&buf)
    }

    /// C# `WriteX(byte[] bytes, int sizeOf, bool endian)`.
    #[inline]
    fn write_swapped_x(&mut self, bytes: &[u8], size_of: usize, big: bool) -> Result<()> {
        if big {
            self.write_swapped(bytes, size_of)
        } else {
            self.write_all(bytes)
        }
    }

    /// C# `WriteGuid(Guid)` — 16 bytes in .NET's mixed-endian layout
    /// (`Guid.ToByteArray`: first three fields little-endian, last eight raw).
    /// Callers must hand over bytes already in that order.
    #[inline]
    fn write_guid(&mut self, bytes: &[u8; 16]) -> Result<()> {
        self.write_all(bytes)
    }

    // -- strings ------------------------------------------------------------

    /// C# `WriteZASCII(string, int length)` — ASCII bytes then a NUL.
    ///
    /// The C# `length` parameter is declared but never used; it is dropped here
    /// rather than carried as a dead argument.
    fn write_z_ascii(&mut self, s: &str) -> Result<()> {
        for c in s.chars() {
            // .NET's ASCII encoder substitutes '?' for anything non-ASCII.
            self.write_u8(if c.is_ascii() { c as u8 } else { b'?' })?;
        }
        self.write_u8(0)
    }

    /// u8 length prefix then ASCII bytes. Pairs with `read_l8_a_string`.
    fn write_l8_a_string(&mut self, s: &str) -> Result<()> {
        let bytes: Vec<u8> = s
            .chars()
            .map(|c| if c.is_ascii() { c as u8 } else { b'?' })
            .collect();
        let len = u8::try_from(bytes.len()).map_err(|_| {
            io::Error::new(io::ErrorKind::InvalidInput, "string exceeds u8 length prefix")
        })?;
        self.write_u8(len)?;
        self.write_all(&bytes)
    }

    /// u32 length prefix then ASCII bytes. Pairs with `read_l32_a_string`.
    fn write_l32_a_string(&mut self, s: &str, big: bool) -> Result<()> {
        let bytes: Vec<u8> = s
            .chars()
            .map(|c| if c.is_ascii() { c as u8 } else { b'?' })
            .collect();
        self.write_u32_x(bytes.len() as u32, big)?;
        self.write_all(&bytes)
    }

    // -- arrays -------------------------------------------------------------

    /// C# `WriteFArray<T>(T[], Action<BinaryWriter,T>)`.
    fn write_array<T, F>(&mut self, items: &[T], mut f: F) -> Result<()>
    where
        F: FnMut(&mut Self, &T) -> Result<()>,
        Self: Sized,
    {
        for it in items {
            f(self, it)?;
        }
        Ok(())
    }

    /// C# `WriteL32FArray<T>` — u32 count prefix then the elements.
    fn write_l32_array<T, F>(&mut self, items: &[T], big: bool, f: F) -> Result<()>
    where
        F: FnMut(&mut Self, &T) -> Result<()>,
        Self: Sized,
    {
        self.write_u32_x(items.len() as u32, big)?;
        self.write_array(items, f)
    }

    /// C# `WriteL8FArray<T>` — u8 count prefix then the elements.
    fn write_l8_array<T, F>(&mut self, items: &[T], f: F) -> Result<()>
    where
        F: FnMut(&mut Self, &T) -> Result<()>,
        Self: Sized,
    {
        let len = u8::try_from(items.len()).map_err(|_| {
            io::Error::new(io::ErrorKind::InvalidInput, "array exceeds u8 count prefix")
        })?;
        self.write_u8(len)?;
        self.write_array(items, f)
    }

    // -- position -----------------------------------------------------------

    /// C# `Tell()`.
    #[inline]
    fn tell(&mut self) -> Result<u64> {
        self.stream_position()
    }

    /// C# `Seek(long offset)` — absolute.
    #[inline]
    fn seek_to(&mut self, offset: u64) -> Result<u64> {
        self.seek(SeekFrom::Start(offset))
    }

    /// C# `Skip(long count)` — relative.
    #[inline]
    fn skip(&mut self, count: i64) -> Result<u64> {
        self.seek(SeekFrom::Current(count))
    }

    /// C# `End(long offset)`.
    #[inline]
    fn seek_end(&mut self, offset: i64) -> Result<u64> {
        self.seek(SeekFrom::End(offset))
    }

    /// C# `Align(int align = 4)`.
    ///
    /// Deviation worth knowing: the C# only moves the *position*, so on a
    /// growing stream the skipped bytes are whatever was already there (or a
    /// zero-fill hole). This writes explicit zero padding, which is what every
    /// caller assumes and what the reader's `align` expects to find.
    fn align(&mut self, align: u64) -> Result<u64> {
        debug_assert!(
            align.is_power_of_two(),
            "Align() masks and so requires a power of two, got {align}"
        );
        let pos = self.stream_position()?;
        let m = align - 1;
        let target = (pos + m) & !m;
        let pad = (target - pos) as usize;
        if pad > 0 {
            self.write_all(&vec![0u8; pad])?;
        }
        Ok(target)
    }

    /// C# `SeekAndAlign(long offset, int align = 4)`.
    #[inline]
    fn seek_and_align(&mut self, offset: u64, align: u64) -> Result<u64> {
        let rem = offset % align;
        let target = if rem != 0 { offset + align - rem } else { offset };
        self.seek(SeekFrom::Start(target))
    }

    // -- numerics -----------------------------------------------------------

    /// Pairs with `read_vec3`.
    fn write_vec3(&mut self, v: [f32; 3]) -> Result<()> {
        for c in v {
            self.write_f32(c)?;
        }
        Ok(())
    }

    /// Pairs with `read_vec4`.
    fn write_vec4(&mut self, v: [f32; 4]) -> Result<()> {
        for c in v {
            self.write_f32(c)?;
        }
        Ok(())
    }
}

impl<T: Write + Seek + ?Sized> BinaryWriterExt for T {}

// NOT PORTED: `WriteCInt32` / `WriteCInt32X` both `throw new
// NotImplementedException()` in the C#. Nothing calls them. Left out rather
// than ported as a panic, so the absence is visible instead of latent.

#[cfg(test)]
mod tests {
    use super::*;
    use crate::system_io::polyfill_binary_reader::BinaryReaderExt;
    use std::io::Cursor;

    #[test]
    fn writer_and_reader_roundtrip() {
        let mut c = Cursor::new(Vec::new());
        c.write_u32(0xDEADBEEF).unwrap();
        c.write_u32_be(0xDEADBEEF).unwrap();
        c.write_f32(1.5).unwrap();
        c.seek_to(0).unwrap();
        assert_eq!(BinaryReaderExt::read_u32(&mut c).unwrap(), 0xDEADBEEF);
        assert_eq!(BinaryReaderExt::read_u32_be(&mut c).unwrap(), 0xDEADBEEF);
        assert_eq!(BinaryReaderExt::read_f32(&mut c).unwrap(), 1.5);
    }

    #[test]
    fn length_prefixed_string_roundtrips() {
        let mut c = Cursor::new(Vec::new());
        c.write_l8_a_string("hello").unwrap();
        c.seek_to(0).unwrap();
        assert_eq!(c.read_l8_a_string(0).unwrap().unwrap(), "hello");
    }

    #[test]
    fn align_writes_zero_padding() {
        let mut c = Cursor::new(Vec::new());
        c.write_u8(1).unwrap();
        assert_eq!(BinaryWriterExt::align(&mut c, 4).unwrap(), 4);
        assert_eq!(c.get_ref().as_slice(), &[1, 0, 0, 0]);
    }

    #[test]
    fn swapped_write_reverses_each_element() {
        let mut c = Cursor::new(Vec::new());
        c.write_swapped(&[1, 2, 3, 4, 5, 6, 7, 8], 4).unwrap();
        assert_eq!(c.get_ref().as_slice(), &[4, 3, 2, 1, 8, 7, 6, 5]);
    }

    #[test]
    fn oversized_l8_string_is_rejected() {
        let mut c = Cursor::new(Vec::new());
        assert!(c.write_l8_a_string(&"x".repeat(300)).is_err());
    }
}
