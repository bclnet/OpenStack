// PORT-SOURCE: Core/OpenStack.PolyIO/System/UnsafeX.cs
// PORT-SHA: 638ab139e00ac244
// PORT-STATUS: done
//
// C# `UnsafeX` is the struct-blitting layer under the binary parsers:
// `MarshalT<T>` reinterprets a byte array as a struct, `MarshalS<T>` does the
// same via `Marshal.PtrToStructure` with an explicit size, `MarshalP<T>` walks a
// Perl-pack-style pattern string, plus `Memset`/`Memcpy` and pointer helpers.
//
// This is ported onto `bytemuck`, per the project decision. The mapping:
//
//   C# MarshalT<T>(byte[])          -> from_bytes::<T>(&[u8])
//   C# MarshalTArray<T>(byte[], n)  -> slice_from_bytes::<T>(&[u8], n)
//   C# MarshalT<T>(T, sizeOf)       -> to_bytes(&T)
//   C# Memset / Memcpy              -> slice::fill / copy_from_slice
//   C# FixedAString(byte*, len)     -> fixed_a_string(&[u8])
//
// WHAT BYTEMUCK BUYS. `MarshalT` is `unsafe` in C# and unchecked: it will
// happily reinterpret bytes as a struct containing references, read past the
// end of a short buffer, or produce invalid bit patterns for `bool`/`char`.
// `Pod` makes those into compile errors, and the length and alignment checks
// become runtime `Result`s instead of undefined behaviour. Callers must derive
// `#[derive(Pod, Zeroable)]` on `#[repr(C)]` structs, which also forces padding
// to be spelled out rather than left to the compiler.
//
// STILL NOT ENDIAN-SAFE. Blitting reads native byte order, exactly as the C#
// did. On a big-endian host every multi-byte field comes out reversed. The
// formats here are little-endian on disk, so prefer `BinaryReaderExt`'s
// field-by-field reads for anything new; these helpers exist for the hot paths
// that already depend on blitting.
//
// NOT PORTED: `MarshalP`/`MarshalPArray`/`MarshalPSymbol*`/`Shape<T>` — the
// pattern-string machinery. It is driven by runtime format strings and is
// entangled with the reflection design still open in `openstack-core`; see
// PORTING.md. Nothing in the ported tree calls it yet.

use bytemuck::{AnyBitPattern, NoUninit, Pod, PodCastError};

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum MarshalError {
    /// Buffer was shorter than the struct(s) requested.
    TooShort { need: usize, got: usize },
    /// Buffer start was not aligned for `T`.
    Misaligned,
    /// Buffer length was not a whole multiple of `size_of::<T>()`.
    NotMultiple { size_of: usize, got: usize },
}

impl std::fmt::Display for MarshalError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            MarshalError::TooShort { need, got } => {
                write!(f, "need {need} bytes, got {got}")
            }
            MarshalError::Misaligned => write!(f, "buffer is misaligned for this type"),
            MarshalError::NotMultiple { size_of, got } => {
                write!(f, "{got} bytes is not a multiple of {size_of}")
            }
        }
    }
}

impl std::error::Error for MarshalError {}

impl From<PodCastError> for MarshalError {
    fn from(e: PodCastError) -> Self {
        match e {
            PodCastError::TargetAlignmentGreaterAndInputNotAligned => MarshalError::Misaligned,
            PodCastError::OutputSliceWouldHaveSlop => {
                MarshalError::NotMultiple { size_of: 0, got: 0 }
            }
            _ => MarshalError::TooShort { need: 0, got: 0 },
        }
    }
}

pub type Result<T> = std::result::Result<T, MarshalError>;

/// C# `MarshalT<T>(byte[] bytes)` — reinterpret the head of `bytes` as a `T`.
///
/// Copies rather than borrowing, so alignment never matters; this is what the
/// C# effectively did via `PtrToStructure`.
pub fn from_bytes<T: AnyBitPattern>(bytes: &[u8]) -> Result<T> {
    let need = std::mem::size_of::<T>();
    if bytes.len() < need {
        return Err(MarshalError::TooShort { need, got: bytes.len() });
    }
    Ok(bytemuck::pod_read_unaligned(&bytes[..need]))
}

/// Borrowing form — no copy, but `bytes` must be aligned for `T`.
pub fn from_bytes_ref<T: Pod>(bytes: &[u8]) -> Result<&T> {
    let need = std::mem::size_of::<T>();
    if bytes.len() < need {
        return Err(MarshalError::TooShort { need, got: bytes.len() });
    }
    bytemuck::try_from_bytes(&bytes[..need]).map_err(MarshalError::from)
}

/// C# `MarshalTArray<T>(byte[] bytes, int count)`.
pub fn slice_from_bytes<T: AnyBitPattern>(bytes: &[u8], count: usize) -> Result<Vec<T>> {
    let one = std::mem::size_of::<T>();
    let need = one
        .checked_mul(count)
        .ok_or(MarshalError::TooShort { need: usize::MAX, got: bytes.len() })?;
    if bytes.len() < need {
        return Err(MarshalError::TooShort { need, got: bytes.len() });
    }
    Ok((0..count)
        .map(|i| bytemuck::pod_read_unaligned(&bytes[i * one..(i + 1) * one]))
        .collect())
}

/// C# `MarshalT<T>(T value, int sizeOf)` — the struct's bytes in native order.
#[inline]
pub fn to_bytes<T: NoUninit>(value: &T) -> &[u8] {
    bytemuck::bytes_of(value)
}

/// C# `MarshalTArray<T>(T[] values, int count)`.
#[inline]
pub fn slice_to_bytes<T: NoUninit>(values: &[T]) -> &[u8] {
    bytemuck::cast_slice(values)
}

/// C# `Memset(byte[] array, byte what, int length)`.
///
/// The C# writes `length` bytes without checking it against `array.Length`.
/// Here the slice bound does that.
#[inline]
pub fn memset(dst: &mut [u8], what: u8) {
    dst.fill(what);
}

/// C# `Memcpy` (a delegate resolved to `Buffer.MemoryCopy`).
///
/// # Panics
/// If the slices differ in length — the C# silently truncated or overran.
#[inline]
pub fn memcpy(dst: &mut [u8], src: &[u8]) {
    dst.copy_from_slice(src);
}

/// C# `FixedAString(byte* data, int length)` — a fixed-width ASCII field,
/// truncated at the first NUL.
pub fn fixed_a_string(data: &[u8]) -> String {
    let end = data.iter().position(|&b| b == 0).unwrap_or(data.len());
    data[..end]
        .iter()
        .map(|&c| if c < 0x80 { c as char } else { '\u{FFFD}' })
        .collect()
}

/// C# `FixedAStringScan(byte* data, int length)` — as above, but stops at the
/// first byte outside printable ASCII rather than only at NUL.
pub fn fixed_a_string_scan(data: &[u8]) -> String {
    let end = data
        .iter()
        .position(|&b| !(0x20..0x7f).contains(&b))
        .unwrap_or(data.len());
    data[..end].iter().map(|&c| c as char).collect()
}

/// C# `FixedByteArray(byte* data, int size)`.
#[inline]
pub fn fixed_byte_array(data: &[u8]) -> Vec<u8> {
    data.to_vec()
}

/// C# `Atoi(string data)` — lenient leading-integer parse.
///
/// C# returns 0 for unparseable input rather than throwing; preserved.
pub fn atoi(data: &str) -> i32 {
    let s = data.trim_start();
    let mut end = 0;
    for (i, c) in s.char_indices() {
        if c.is_ascii_digit() || (i == 0 && (c == '-' || c == '+')) {
            end = i + c.len_utf8();
        } else {
            break;
        }
    }
    s[..end].parse().unwrap_or(0)
}

#[cfg(test)]
mod tests {
    use super::*;
    use bytemuck::{Pod, Zeroable};

    #[repr(C)]
    #[derive(Debug, Clone, Copy, PartialEq, Pod, Zeroable)]
    struct Header {
        magic: u32,
        version: u16,
        flags: u16,
    }

    #[test]
    fn roundtrips_a_pod_struct() {
        let h = Header { magic: 0xDEADBEEF, version: 3, flags: 0x11 };
        let bytes = to_bytes(&h);
        assert_eq!(bytes.len(), 8);
        assert_eq!(from_bytes::<Header>(bytes).unwrap(), h);
    }

    #[test]
    fn short_buffer_is_an_error_not_a_read_past_the_end() {
        // The C# MarshalT would read out of bounds here.
        assert!(matches!(
            from_bytes::<Header>(&[0u8; 3]),
            Err(MarshalError::TooShort { need: 8, got: 3 })
        ));
    }

    #[test]
    fn unaligned_buffers_still_work_via_copy() {
        let mut raw = vec![0u8; 9];
        let h = Header { magic: 1, version: 2, flags: 3 };
        raw[1..].copy_from_slice(to_bytes(&h));
        assert_eq!(from_bytes::<Header>(&raw[1..]).unwrap(), h);
    }

    #[test]
    fn array_marshalling_round_trips() {
        let items = [
            Header { magic: 1, version: 1, flags: 1 },
            Header { magic: 2, version: 2, flags: 2 },
        ];
        let bytes = slice_to_bytes(&items);
        let back: Vec<Header> = slice_from_bytes(bytes, 2).unwrap();
        assert_eq!(back, items);
    }

    #[test]
    fn fixed_strings_stop_where_they_should() {
        assert_eq!(fixed_a_string(b"name\0\0\0\0"), "name");
        assert_eq!(fixed_a_string_scan(b"name\x01junk"), "name");
    }

    #[test]
    fn atoi_matches_c_sharp_leniency() {
        assert_eq!(atoi("42abc"), 42);
        assert_eq!(atoi("-7"), -7);
        assert_eq!(atoi("nope"), 0);
        assert_eq!(atoi(""), 0);
    }
}
