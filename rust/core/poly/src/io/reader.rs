// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/Polyfill+BinaryReader.cs
// PORT-SHA: 2c5aa88138cbe33c
// PORT-STATUS: done
//
// C# `public static partial class Polyfill` holds ~200 extension methods on
// `BinaryReader`. Rust has no extension methods, so the direct analogue is a
// blanket-implemented trait: any `Read + Seek` gets the whole surface for free,
// exactly as any `BinaryReader` did in C#.
//
//   C#   r.ReadL32AString()      Rust   r.read_l32_a_string(0, false)?
//   C#   r.Skip(4).ReadInt32()   Rust   { r.skip(4)?; r.read_i32()? }
//
// Naming: C# `ReadXxxE` (big-endian) -> `read_xxx_be`; `ReadXxxX(endian)` ->
// `read_xxx_x(big)`. C# chained fluently by returning `BinaryReader`; Rust
// returns the new position instead, since `&mut self` chaining fights the
// borrow checker for no real gain.

use std::io::{copy, Error, ErrorKind, Read, Seek, Write}; //, SeekFrom, Write}; //self
// use rust_decimal::Decimal;

// use super::polyfill::X_BoundBox;

pub type Result<T> = std::result::Result<T, Error>;

// /// Mirrors C# `SeekOrigin`, used by the `Peek` family.
// #[derive(Debug, Clone, Copy, PartialEq, Eq)]
// pub enum Origin {
//     Begin,
//     Current,
//     End,
// }

// macro_rules! prim {
//     ($le:ident, $be:ident, $x:ident, $ty:ty, $n:expr, $cs:literal) => {
//         #[doc = concat!("Little-endian. C# `Read", $cs, "`.")]
//         #[inline]
//         fn $le(&mut self) -> Result<$ty> {
//             let mut b = [0u8; $n];
//             self.read_exact(&mut b)?;
//             Ok(<$ty>::from_le_bytes(b))
//         }
//         #[doc = concat!("Big-endian. C# `Read", $cs, "E`.")]
//         #[inline]
//         fn $be(&mut self) -> Result<$ty> {
//             let mut b = [0u8; $n];
//             self.read_exact(&mut b)?;
//             Ok(<$ty>::from_be_bytes(b))
//         }
//         #[doc = concat!("Endian-switched. C# `Read", $cs, "X(bool endian)`; `big == true` -> big-endian.")]
//         #[inline]
//         fn $x(&mut self, big: bool) -> Result<$ty> {
//             if big { self.$be() } else { self.$le() }
//         }
//     };
// }

pub trait BinaryReaderExt: Read + Seek {
    //#region Base

    fn copyTo<W: Write>(&mut self, dst: &mut W) -> Result<u64> { Ok(copy(self, dst)?) }

    //#endregion

    //#region Bytes

    fn readBytes(&mut self, count: usize) -> Result<Vec<u8>> { let mut v = vec![0u8; count]; self.read_exact(&mut v)?; Ok(v) }
    fn readL8Bytes(&mut self, maxLength: usize) -> Result<Vec<u8>> { let length = self.readByte()? as usize; if maxLength > 0 && length > maxLength { return Err(Error::new(ErrorKind::Other, "byte length exceeds maximum length")) } return Ok(if length > 0 { self.readBytes(length)? } else { Vec::<u8>::new() }); }
    fn readL16Bytes(&mut self, maxLength: usize, big: bool) -> Result<Vec<u8>> { let length = self.readUInt16X(big)? as usize; if maxLength > 0 && length > maxLength { return Err(Error::new(ErrorKind::Other, "byte length exceeds maximum length")) } return Ok(if length > 0 { self.readBytes(length)? } else { Vec::<u8>::new() }); }
    fn readL32Bytes(&mut self, maxLength: usize, big: bool) -> Result<Vec<u8>> { let length = self.readUInt32X(big)? as usize; if maxLength > 0 && length > maxLength { return Err(Error::new(ErrorKind::Other, "byte length exceeds maximum length")) } return Ok(if length > 0 { self.readBytes(length)? } else { Vec::<u8>::new() }); }
    fn readToEnd(&mut self) -> Result<Vec<u8>> { let mut v = Vec::new(); self.read_to_end(&mut v)?; Ok(v) }
    fn readToValue(&mut self, value: u8, length: usize, ms: Option<Vec<u8>>) -> Result<Vec<u8>> {
        let mut ms = ms.map_or_else(Vec::new, |mut v| { v.clear(); v }); let mut length = length; let mut b = [0u8; 1];
        while length > 0 {
            match self.read(&mut b) {
                Ok(0) => break, Ok(_) => {}
                Err(e) if e.kind() == ErrorKind::Interrupted => continue, Err(e) => return Err(e.into()),
            }
            length -= 1;
            if b[0] == value { break; }
            ms.push(b[0]);
        }
        Ok(ms)
    }

    //#endregion


    //#region Primitives

    // # primatives : normal
    #[inline] fn readBoolean(&mut self) -> Result<bool> { let mut b = [0u8; 1]; self.read_exact(&mut b)?; Ok(b[0] != 0) } // any non-zero byte is true
    #[inline] fn readByte(&mut self) -> Result<u8> { let mut b = [0u8; 1]; self.read_exact(&mut b)?; Ok(b[0]) }
    #[inline] fn readSByte(&mut self) -> Result<i8> { let mut b = [0u8; 1]; self.read_exact(&mut b)?; Ok(b[0] as i8) }
    #[inline] fn readInt16(&mut self) -> Result<i16> { let mut b = [0u8; 2]; self.read_exact(&mut b)?; Ok(<i16>::from_le_bytes(b)) }
    #[inline] fn readInt32(&mut self) -> Result<i32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(<i32>::from_le_bytes(b)) }
    #[inline] fn readInt64(&mut self) -> Result<i64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(<i64>::from_le_bytes(b)) }
    #[inline] fn readSingle(&mut self) -> Result<f32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(<f32>::from_le_bytes(b)) }
    #[inline] fn readDouble(&mut self) -> Result<f64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(<f64>::from_le_bytes(b)) }
    #[inline] fn readUInt16(&mut self) -> Result<u16> { let mut b = [0u8; 2]; self.read_exact(&mut b)?; Ok(<u16>::from_le_bytes(b)) }
    #[inline] fn readUInt32(&mut self) -> Result<u32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(<u32>::from_le_bytes(b)) }
    #[inline] fn readUInt64(&mut self) -> Result<u64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(<u64>::from_le_bytes(b)) }
    // #[inline] fn readDecimal(&mut self) -> Result<Decimal> { }

    //# primatives : big
    #[inline] fn readInt16E(&mut self) -> Result<i16> { let mut b = [0u8; 2]; self.read_exact(&mut b)?; Ok(<i16>::from_be_bytes(b)) }
    #[inline] fn readInt32E(&mut self) -> Result<i32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(<i32>::from_be_bytes(b)) }
    #[inline] fn readInt64E(&mut self) -> Result<i64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(<i64>::from_be_bytes(b)) }
    #[inline] fn readSingleE(&mut self) -> Result<f32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(<f32>::from_be_bytes(b)) }
    #[inline] fn readDoubleE(&mut self) -> Result<f64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(<f64>::from_be_bytes(b)) }
    #[inline] fn readUInt16E(&mut self) -> Result<u16> { let mut b = [0u8; 2]; self.read_exact(&mut b)?; Ok(<u16>::from_be_bytes(b)) }
    #[inline] fn readUInt32E(&mut self) -> Result<u32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(<u32>::from_be_bytes(b)) }
    #[inline] fn readUInt64E(&mut self) -> Result<u64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(<u64>::from_be_bytes(b)) }
    // #[inline] fn readDecimalE(&mut self) -> Result<Decimal> { }
    
    //# primatives : endianX
    #[inline] fn readInt16x(&mut self, big: bool) -> Result<i16> { let mut b = [0u8; 2]; self.read_exact(&mut b)?; Ok(if big { <i16>::from_be_bytes(b) } else { <i16>::from_le_bytes(b) }) }
    #[inline] fn readInt32X(&mut self, big: bool) -> Result<i32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(if big { <i32>::from_be_bytes(b) } else { <i32>::from_le_bytes(b) }) }
    #[inline] fn readInt64X(&mut self, big: bool) -> Result<i64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(if big { <i64>::from_be_bytes(b) } else { <i64>::from_le_bytes(b) }) }
    #[inline] fn readSingleX(&mut self, big: bool) -> Result<f32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(if big { <f32>::from_be_bytes(b) } else { <f32>::from_le_bytes(b) }) }
    #[inline] fn readDoubleX(&mut self, big: bool) -> Result<f64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(if big { <f64>::from_be_bytes(b) } else { <f64>::from_le_bytes(b) }) }
    #[inline] fn readUInt16X(&mut self, big: bool) -> Result<u16> { let mut b = [0u8; 2]; self.read_exact(&mut b)?; Ok(if big { <u16>::from_be_bytes(b) } else { <u16>::from_le_bytes(b) }) }
    #[inline] fn readUInt32X(&mut self, big: bool) -> Result<u32> { let mut b = [0u8; 4]; self.read_exact(&mut b)?; Ok(if big { <u32>::from_be_bytes(b) } else { <u32>::from_le_bytes(b) }) }
    #[inline] fn readUInt64X(&mut self, big: bool) -> Result<u64> { let mut b = [0u8; 8]; self.read_exact(&mut b)?; Ok(if big { <u64>::from_be_bytes(b) } else { <u64>::from_le_bytes(b) }) }
    // #[inline] fn readDecimalX(&mut self, big: bool) -> Result<Decimal> { }

    //# primatives : specialized

    fn readIntV7(&mut self) -> Result<i32> { // LEB128-style varint.
        let (mut r, mut b) = (0i32, 0i32);
        loop {
            let v = self.readByte()? as i32; r |= (v & 0x7f) << b; b += 7;
            if v & 0x80 == 0 { return Ok(r); }
            else if b > 31 { return Err(Error::new(ErrorKind::Other, "7-bit encoding too long")) }
        }
    }
    #[inline] fn readIntV7X(&mut self, big: bool) -> Result<i32> { if big { Err(Error::new(ErrorKind::Other, "Not Implemented")) } else { self.readIntV7() } }
    fn readUIntV8(&mut self) -> Result<u32> { // 1/2/4-byte length prefix selected by the top bits.
        let b0 = self.readByte()? as u32; if b0 & 0x80 == 0 { return Ok(b0); }
        let b1 = self.readByte()? as u32; if b0 & 0x40 == 0 { return Ok(((b0 & 0x3f) << 8) | b1); }
        Ok(((((b0 & 0x3F) << 8) | b1) << 16) | self.readUInt16()? as u32)
    }
    #[inline] fn readUIntV8X(&mut self, big: bool) -> Result<u32> { if big { Err(Error::new(ErrorKind::Other, "Not Implemented")) } else { self.readUIntV8() } }

    //#endregion

    // //#region Position

    // /// C# `Tell()`.
    // #[inline]
    // fn tell(&mut self) -> Result<u64> {
    //     Ok(self.stream_position()?)
    // }

    // /// C# `Seek(long offset)` — absolute.
    // #[inline]
    // fn seek_to(&mut self, offset: u64) -> Result<u64> {
    //     Ok(self.seek(SeekFrom::Start(offset))?)
    // }

    // /// C# `Skip(long count)` — relative.
    // #[inline]
    // fn skip(&mut self, count: i64) -> Result<u64> {
    //     Ok(self.seek(SeekFrom::Current(count))?)
    // }

    // /// C# `End(long offset)`.
    // #[inline]
    // fn seek_end(&mut self, offset: i64) -> Result<u64> {
    //     Ok(self.seek(SeekFrom::End(offset))?)
    // }

    // /// C# `Align(int align = 4)` — round the position up to a multiple of `align`.
    // ///
    // /// The C# body is `(pos + --align) & ~align`, which is only correct for
    // /// powers of two. Preserved, with the hidden precondition made explicit.
    // #[inline]
    // fn align(&mut self, align: u64) -> Result<u64> {
    //     debug_assert!(
    //         align.is_power_of_two(),
    //         "Align() masks and so requires a power of two, got {align}"
    //     );
    //     let pos = self.stream_position()?;
    //     let m = align - 1;
    //     Ok(self.seek(SeekFrom::Start((pos + m) & !m))?)
    // }

    // /// C# `SeekAndAlign(long offset, int align = 4)`. Uses modulo, so unlike
    // /// `Align` this one is correct for non-power-of-two alignments.
    // #[inline]
    // fn seek_and_align(&mut self, offset: u64, align: u64) -> Result<u64> {
    //     let rem = offset % align;
    //     let target = if rem != 0 { offset + align - rem } else { offset };
    //     Ok(self.seek(SeekFrom::Start(target))?)
    // }

    // /// C# `SkipAndAlign(long count, int align = 4)`.
    // #[inline]
    // fn skip_and_align(&mut self, count: i64, align: u64) -> Result<u64> {
    //     let pos = self.stream_position()?;
    //     let offset = (pos as i64 + count) as u64;
    //     self.seek_and_align(offset, align)
    // }

    // /// Total length of the underlying stream (C# `BaseStream.Length`).
    // fn stream_len(&mut self) -> Result<u64> {
    //     let pos = self.stream_position()?;
    //     let end = self.seek(SeekFrom::End(0))?;
    //     self.seek(SeekFrom::Start(pos))?;
    //     Ok(end)
    // }

    // /// Bytes between the current position and the end.
    // #[inline]
    // fn remaining(&mut self) -> Result<u64> {
    //     Ok(self.stream_len()?.saturating_sub(self.stream_position()?))
    // }

    // /// C# `AtEnd(long? end = null)`.
    // fn at_end(&mut self, end: Option<u64>) -> Result<bool> {
    //     let end = match end {
    //         Some(e) => e,
    //         None => self.stream_len()?,
    //     };
    //     Ok(self.stream_position()? >= end)
    // }

    // /// C# `EnsureAtEnd(long? end, string message)`.
    // fn ensure_at_end(&mut self, end: Option<u64>) -> Result<()> {
    //     let end = match end {
    //         Some(e) => e,
    //         None => self.stream_len()?,
    //     };
    //     let pos = self.stream_position()?;
    //     if pos != end {
    //         return Err(ReadError::NotAtEnd { position: pos, expected: end });
    //     }
    //     Ok(())
    // }

    // /// C# `Peek<T>(Func<BinaryReader,T>, long offset, SeekOrigin origin)` — run
    // /// `f` at a temporary position, then restore.
    // ///
    // /// Deviation: the position is restored even when `f` fails. The C# version
    // /// leaks the seek on throw; that is a bug, not behaviour worth mirroring.
    // fn peek<T, F>(&mut self, offset: i64, origin: Origin, f: F) -> Result<T>
    // where
    //     F: FnOnce(&mut Self) -> Result<T>,
    //     Self: Sized,
    // {
    //     let saved = self.stream_position()?;
    //     let from = match origin {
    //         Origin::Begin => SeekFrom::Start(offset as u64),
    //         Origin::Current => SeekFrom::Current(offset),
    //         Origin::End => SeekFrom::End(offset),
    //     };
    //     self.seek(from)?;
    //     let out = f(self);
    //     self.seek(SeekFrom::Start(saved))?;
    //     out
    // }

    // //#endregion

    // //#region Strings

    // //
    // // C# returns `null` for zero-length strings and trims trailing NULs; both
    // // are preserved, as `Option<String>` and `trim_end_matches('\0')`.
    // //
    // // ON THE `W` FAMILY: the names say "wide" (UTF-16), but every BinaryReader
    // // in the C# tree is constructed without an explicit encoding, so
    // // `ReadChars` decodes UTF-8. These are ported to the *observed* behaviour,
    // // not the name. If UTF-16 was the intent then the C# side has a bug and
    // // both trees need the same fix — do not let them silently diverge here.

    // /// C# `ReadFAString(int length)` — fixed-length ASCII.
    // fn read_fa_string(&mut self, length: usize) -> Result<Option<String>> {
    //     if length == 0 {
    //         return Ok(None);
    //     }
    //     let b = self.read_bytes(length)?;
    //     Ok(Some(decode_ascii(&b)))
    // }

    // /// C# `ReadFUString(int length)` — fixed-length UTF-8.
    // fn read_fu_string(&mut self, length: usize) -> Result<Option<String>> {
    //     if length == 0 {
    //         return Ok(None);
    //     }
    //     let b = self.read_bytes(length)?;
    //     Ok(Some(decode_utf8(&b)?))
    // }

    // /// C# `ReadVAString(int length, byte stopValue)` — NUL-terminated ASCII.
    // fn read_va_string(&mut self, length: usize, stop: u8) -> Result<String> {
    //     let b = self.read_to_value(stop, length)?;
    //     Ok(decode_ascii(&b))
    // }

    // /// C# `ReadVUString(int length, byte stopValue)` — NUL-terminated UTF-8.
    // fn read_vu_string(&mut self, length: usize, stop: u8) -> Result<String> {
    //     let b = self.read_to_value(stop, length)?;
    //     decode_utf8(&b)
    // }

    // /// C# `ReadL8AString` — u8 length prefix, ASCII.
    // fn read_l8_a_string(&mut self, max_length: usize) -> Result<Option<String>> {
    //     let len = self.readByte()? as usize;
    //     check_len(len, max_length)?;
    //     if len == 0 {
    //         return Ok(None);
    //     }
    //     Ok(Some(decode_ascii(&self.read_bytes(len)?)))
    // }

    // /// C# `ReadL16AString` — u16 length prefix, ASCII.
    // fn read_l16_a_string(&mut self, max_length: usize, big: bool) -> Result<Option<String>> {
    //     let len = self.read_u16_x(big)? as usize;
    //     check_len(len, max_length)?;
    //     if len == 0 {
    //         return Ok(None);
    //     }
    //     Ok(Some(decode_ascii(&self.read_bytes(len)?)))
    // }

    // /// C# `ReadL32AString` — u32 length prefix, ASCII.
    // fn read_l32_a_string(&mut self, max_length: usize, big: bool) -> Result<Option<String>> {
    //     let len = self.read_u32_x(big)? as usize;
    //     check_len(len, max_length)?;
    //     if len == 0 {
    //         return Ok(None);
    //     }
    //     Ok(Some(decode_ascii(&self.read_bytes(len)?)))
    // }

    // /// C# `ReadL8UString` — u8 length prefix, UTF-8.
    // fn read_l8_u_string(&mut self, max_length: usize) -> Result<Option<String>> {
    //     let len = self.readByte()? as usize;
    //     check_len(len, max_length)?;
    //     if len == 0 {
    //         return Ok(None);
    //     }
    //     decode_utf8(&self.read_bytes(len)?).map(Some)
    // }

    // /// C# `ReadL16UString` — u16 length prefix, UTF-8.
    // fn read_l16_u_string(&mut self, max_length: usize, big: bool) -> Result<Option<String>> {
    //     let len = self.read_u16_x(big)? as usize;
    //     check_len(len, max_length)?;
    //     if len == 0 {
    //         return Ok(None);
    //     }
    //     decode_utf8(&self.read_bytes(len)?).map(Some)
    // }

    // /// C# `ReadL32UString` — u32 length prefix, UTF-8.
    // fn read_l32_u_string(&mut self, max_length: usize, big: bool) -> Result<Option<String>> {
    //     let len = self.read_u32_x(big)? as usize;
    //     check_len(len, max_length)?;
    //     if len == 0 {
    //         return Ok(None);
    //     }
    //     decode_utf8(&self.read_bytes(len)?).map(Some)
    // }

    // /// C# `ReadL16OString(int codepage = 1252)` — "obfuscated" string: u16
    // /// length prefix, then every byte has its nibbles swapped.
    // fn read_l16_o_string(&mut self) -> Result<String> {
    //     let len = self.read_u16()? as usize;
    //     if len == 0 {
    //         return Ok(String::new());
    //     }
    //     let mut b = self.read_bytes(len)?;
    //     for x in b.iter_mut() {
    //         *x = (*x >> 4) | (*x << 4);
    //     }
    //     Ok(decode_cp1252(&b))
    // }

    // /// C# `ReadLV8W2String` — varint length prefix, then that many u16 code units.
    // fn read_lv8_w2_string(&mut self, max_length: usize, big: bool) -> Result<Option<String>> {
    //     let len = self.read_uint_v8_x(big)? as usize;
    //     check_len(len, max_length)?;
    //     if len == 0 {
    //         return Ok(None);
    //     }
    //     let mut units = Vec::with_capacity(len);
    //     for _ in 0..len {
    //         units.push(self.read_u16()?);
    //     }
    //     Ok(Some(
    //         String::from_utf16(&units).map_err(|_| ReadError::Encoding("UTF-16"))?,
    //     ))
    // }

    // /// C# `ReadVAStringList(int length, byte stopValue)` — consecutive
    // /// NUL-terminated ASCII strings within a byte budget.
    // fn read_va_string_list(&mut self, length: usize, stop: u8) -> Result<Vec<String>> {
    //     let mut out = Vec::new();
    //     let mut remaining = length;
    //     while remaining > 0 {
    //         let before = remaining;
    //         let mut buf = Vec::new();
    //         while remaining > 0 {
    //             let mut b = [0u8; 1];
    //             match self.read(&mut b) {
    //                 Ok(0) => break,
    //                 Ok(_) => {}
    //                 Err(e) if e.kind() == io::ErrorKind::Interrupted => continue,
    //                 Err(e) => return Err(e.into()),
    //             }
    //             remaining -= 1;
    //             if b[0] == stop {
    //                 break;
    //             }
    //             buf.push(b[0]);
    //         }
    //         if remaining == before {
    //             break; // stream ended without consuming anything
    //         }
    //         out.push(decode_ascii(&buf));
    //     }
    //     Ok(out)
    // }

    // //#endregion

    // //#region Struct / Bulk

    // /// C# `ReadTArray<T>(int count)` for `T : struct`.
    // ///
    // /// The C# version reinterprets raw bytes through `MemoryMarshal`, which is
    // /// endianness- and padding-sensitive. Here the caller passes an explicit
    // /// per-element decoder: safe, portable, and the layout is written down
    // /// rather than inherited from whatever the compiler chose.
    // fn read_array<T, F>(&mut self, count: usize, mut f: F) -> Result<Vec<T>>
    // where
    //     F: FnMut(&mut Self) -> Result<T>,
    //     Self: Sized,
    // {
    //     let mut v = Vec::with_capacity(count);
    //     for _ in 0..count {
    //         v.push(f(self)?);
    //     }
    //     Ok(v)
    // }

    // /// C# `ReadL32TArray<T>` — u32 count prefix followed by that many elements.
    // fn read_l32_array<T, F>(&mut self, big: bool, f: F) -> Result<Vec<T>>
    // where
    //     F: FnMut(&mut Self) -> Result<T>,
    //     Self: Sized,
    // {
    //     let n = self.read_u32_x(big)? as usize;
    //     self.read_array(n, f)
    // }

    // //#endregion

    // //#region Numerics

    // /// C# `ReadVector2`.
    // #[inline]
    // fn readVector2(&mut self) -> Result<Vec2> {
    //     Ok([self.readSingle()?, self.readSingle()?])
    // }

    // /// C# `ReadVector3`.
    // #[inline]
    // fn readVector3(&mut self) -> Result<Vec3> {
    //     Ok([self.readSingle()?, self.readSingle()?, self.readSingle()?])
    // }

    // /// C# `ReadVector4`.
    // #[inline]
    // fn readVector4(&mut self) -> Result<[f32; 4]> {
    //     Ok([
    //         self.readSingle()?,
    //         self.readSingle()?,
    //         self.readSingle()?,
    //         self.readSingle()?,
    //     ])
    // }

    // /// C# `ReadMatrix4x4` — row-major, matching the C# writer.
    // fn readMatrix4x4(&mut self) -> Result<[f32; 16]> {
    //     let mut m = [0f32; 16];
    //     for slot in m.iter_mut() {
    //         *slot = self.readSingle()?;
    //     }
    //     Ok(m)
    // }

    // /// C# `Polyfill.X_BoundBox`.
    // #[inline]
    // fn readBoundBox(&mut self) -> Result<X_BoundBox> {
    //     Ok(XBoundBox {
    //         min: self.readVector3()?,
    //         max: self.readVector3()?,
    //     })
    // }

    // //#endregion
}

/// Blanket impl — the Rust equivalent of C# extension methods applying to every
/// `BinaryReader`.
impl<T: Read + Seek + ?Sized> BinaryReaderExt for T {}

// ---------------------------------------------------------------------------
// helpers
// ---------------------------------------------------------------------------

// #[inline]
// fn check_len(got: usize, max: usize) -> Result<()> {
//     if max > 0 && got > max {
//         return Err(ReadError::LengthExceeded { got, max });
//     }
//     Ok(())
// }

// /// C# `Encoding.ASCII.GetString(..).TrimEnd('\0')`.
// ///
// /// .NET's ASCII decoder maps bytes >= 0x80 to U+FFFD rather than failing, so
// /// this is lossy in exactly the same way.
// fn decode_ascii(b: &[u8]) -> String {
//     let s: String = b
//         .iter()
//         .map(|&c| if c < 0x80 { c as char } else { '\u{FFFD}' })
//         .collect();
//     s.trim_end_matches('\0').to_string()
// }

// /// C# `Encoding.UTF8.GetString(..).TrimEnd('\0')`.
// fn decode_utf8(b: &[u8]) -> Result<String> {
//     let s = String::from_utf8(b.to_vec()).map_err(|_| ReadError::Encoding("UTF-8"))?;
//     Ok(s.trim_end_matches('\0').to_string())
// }

// /// C# `Encoding.GetEncoding(1252).GetString(..)` — Windows-1252.
// ///
// /// Differs from Latin-1 only in 0x80..=0x9F, spelled out here so the port does
// /// not need an encoding crate.
// fn decode_cp1252(b: &[u8]) -> String {
//     const HIGH: [char; 32] = [
//         '\u{20AC}', '\u{FFFD}', '\u{201A}', '\u{0192}', '\u{201E}', '\u{2026}', '\u{2020}',
//         '\u{2021}', '\u{02C6}', '\u{2030}', '\u{0160}', '\u{2039}', '\u{0152}', '\u{FFFD}',
//         '\u{017D}', '\u{FFFD}', '\u{FFFD}', '\u{2018}', '\u{2019}', '\u{201C}', '\u{201D}',
//         '\u{2022}', '\u{2013}', '\u{2014}', '\u{02DC}', '\u{2122}', '\u{0161}', '\u{203A}',
//         '\u{0153}', '\u{FFFD}', '\u{017E}', '\u{0178}',
//     ];
//     b.iter()
//         .map(|&c| match c {
//             0x80..=0x9F => HIGH[(c - 0x80) as usize],
//             _ => c as char,
//         })
//         .collect()
// }

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn endianness_pairs_agree() {
        let mut c = Cursor::new(vec![0x01, 0x02, 0x03, 0x04]);
        assert_eq!(c.read_u32().unwrap(), 0x04030201);
        c.seek_to(0).unwrap();
        assert_eq!(c.read_u32_be().unwrap(), 0x01020304);
        c.seek_to(0).unwrap();
        assert_eq!(c.read_u32_x(true).unwrap(), 0x01020304);
    }

    #[test]
    fn align_rounds_up_and_is_idempotent() {
        let mut c = Cursor::new(vec![0u8; 32]);
        c.seek_to(5).unwrap();
        assert_eq!(c.align(4).unwrap(), 8);
        assert_eq!(c.align(4).unwrap(), 8, "already aligned must not advance");
    }

    #[test]
    fn length_prefixed_zero_is_none_not_empty() {
        // Mirrors the C# `null` return, which callers branch on.
        let mut c = Cursor::new(vec![0x00]);
        assert!(c.read_l8_a_string(0).unwrap().is_none());
    }

    #[test]
    fn max_length_is_enforced() {
        let mut c = Cursor::new(vec![0x10, b'a']);
        assert!(matches!(
            c.read_l8_a_string(4),
            Err(ReadError::LengthExceeded { got: 16, max: 4 })
        ));
    }

    #[test]
    fn strings_trim_trailing_nuls() {
        let mut c = Cursor::new(b"hi\0\0".to_vec());
        assert_eq!(c.read_fa_string(4).unwrap().unwrap(), "hi");
    }

    #[test]
    fn read_to_value_consumes_sentinel_but_excludes_it() {
        let mut c = Cursor::new(b"ab\0cd".to_vec());
        assert_eq!(c.read_to_value(0, usize::MAX).unwrap(), b"ab");
        assert_eq!(c.tell().unwrap(), 3, "sentinel must be consumed");
    }

    #[test]
    fn peek_restores_position() {
        let mut c = Cursor::new(vec![1, 2, 3, 4, 5, 6, 7, 8]);
        c.seek_to(2).unwrap();
        let v = c.peek(2, Origin::Current, |r| r.readByte()).unwrap();
        assert_eq!(v, 5);
        assert_eq!(c.tell().unwrap(), 2);
    }

    #[test]
    fn varint_decodes_known_value() {
        let mut c = Cursor::new(vec![0xE5, 0x8E, 0x26]);
        assert_eq!(c.read_int_v7().unwrap(), 624485);
    }

    #[test]
    fn obfuscated_string_swaps_nibbles() {
        // 'A' == 0x41; stored nibble-swapped as 0x14.
        let mut c = Cursor::new(vec![0x01, 0x00, 0x14]);
        assert_eq!(c.read_l16_o_string().unwrap(), "A");
    }

    #[test]
    fn uint_v8_selects_width_by_top_bits() {
        assert_eq!(Cursor::new(vec![0x7F]).read_uint_v8().unwrap(), 0x7F);
        assert_eq!(Cursor::new(vec![0x81, 0x02]).read_uint_v8().unwrap(), 0x0102);
    }
}
