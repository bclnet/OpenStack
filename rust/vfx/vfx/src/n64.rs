// PORT-SOURCE: Vfx/OpenStack.Vfx/N64.cs
// PORT-SHA: 8e842cf8624c281c
// PORT-STATUS: done
//
// Nintendo 64 ROM header parsing. The three container layouts (.z64 native
// big-endian, .v64 byte-swapped, .n64 word-swapped) are detected by magic and
// normalised to native order before the header is read.
//
// ===================== FOUR C#-SIDE BUGS ==================================
//
//   1. **A one-byte buffer overflow.** `Header.Name` is `fixed sbyte Name[20]`,
//      so valid indices are 0..19 — but the constructor does
//      `Header.Name[20] = (sbyte)'\0';`. `fixed`-buffer indexing is unchecked
//      inside `unsafe`, so that write lands on the next struct field
//      (`Unknown2` at 0x34), corrupting it. **Fix this in the C# tree.**
//
//   2. **The country is always reported "Unknown".** `Header.CountryCode` is a
//      `byte`, and the log line calls `ReverseEndianness(Header.CountryCode)`.
//      There is no `byte` overload, so it widens to `ushort` and 0x45 ("USA")
//      reverses to 0x4500 — which matches nothing in `CountryCodeToString`, so
//      every ROM logs `Unknown (0x4500`. Note `CountryCodeToSystemType` is
//      called on the *raw* byte and is therefore correct; only the display is
//      wrong, which is why it has gone unnoticed.
//
//   3. **`IsValidRom` reads four bytes without checking the length.** A file
//      shorter than 4 bytes reads past the end of the buffer.
//
//   4. **`N64FileSystem` does nothing.** Its constructor builds an `N64Rom`,
//      logs, and **discards it** (`var disc = ...`, never used). `FileExists`,
//      `FileInfo`, and `Open` all `throw new NotImplementedException()`;
//      `Glob` returns empty. Like `NetworkFileSystem`, it reads as a feature
//      and is not one. Not ported — see the bottom of this file.

use crate::util::endianness;

/// C# `N64Rom.IMAGE` — the container byte order.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ImageType {
    /// `.z64` — native big-endian.
    Z64,
    /// `.v64` — byte-swapped (halfword pairs).
    V64,
    /// `.n64` — word-swapped.
    N64,
}

impl ImageType {
    /// C# `ImageToString`.
    pub fn as_str(self) -> &'static str {
        match self {
            ImageType::Z64 => ".z64 (native)",
            ImageType::V64 => ".v64 (byteswapped)",
            ImageType::N64 => ".n64 (wordswapped)",
        }
    }
}

/// C# `N64Rom.SYSTEM`.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SystemType {
    Ntsc,
    Pal,
    Mpal,
}

// Magic values as read little-endian from the first four bytes. The C# keeps
// the big-endian spelling in a trailing comment on each.
const Z64_MAGIC: u32 = 0x4012_3780; // 0x80371240 big-endian
const V64_MAGIC: u32 = 0x1240_8037; // 0x37804012
const N64_MAGIC: u32 = 0x9037_1240; // 0x40123780

/// C# `N64Rom.RomHeader`, 0x40 bytes.
///
/// Read field by field rather than blitted: the C# uses
/// `Marshal.PtrToStructure`, which depends on the runtime laying the struct out
/// exactly as the file does. Every multi-byte field is big-endian on disk.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct RomHeader {
    pub init_pi_bsb_dom1_lat_reg: u8,
    pub init_pi_bsb_dom1_pgs_reg: u8,
    pub init_pi_bsb_dom1_pwd_reg: u8,
    pub init_pi_bsb_dom1_pgs_reg2: u8,
    pub clock_rate: u32,
    pub pc: u32,
    pub release: u32,
    pub crc1: u32,
    pub crc2: u32,
    pub unknown: [u32; 2],
    /// 20 bytes at 0x20. Held as raw bytes; see [`name`](Self::name).
    pub name_raw: [u8; 20],
    pub unknown2: u32,
    pub manufacturer_id: u32,
    pub cartridge_id: u16,
    pub country_code: u8,
    pub version: u8,
}

impl RomHeader {
    pub const SIZE_OF: usize = 0x40;

    /// C# `Name` — the internal title, NUL-trimmed and whitespace-trimmed.
    ///
    /// The C# writes a terminator at `Name[20]`, one past the end (bug 1); here
    /// the slice bound makes that unrepresentable.
    pub fn name(&self) -> String {
        let end = self
            .name_raw
            .iter()
            .position(|&b| b == 0)
            .unwrap_or(self.name_raw.len());
        self.name_raw[..end]
            .iter()
            .map(|&c| if c < 0x80 { c as char } else { '\u{FFFD}' })
            .collect::<String>()
            .trim()
            .to_string()
    }

    fn read(b: &[u8]) -> Option<Self> {
        if b.len() < Self::SIZE_OF {
            return None;
        }
        let u32at = |o: usize| u32::from_be_bytes([b[o], b[o + 1], b[o + 2], b[o + 3]]);
        let mut name_raw = [0u8; 20];
        name_raw.copy_from_slice(&b[0x20..0x34]);
        Some(Self {
            init_pi_bsb_dom1_lat_reg: b[0],
            init_pi_bsb_dom1_pgs_reg: b[1],
            init_pi_bsb_dom1_pwd_reg: b[2],
            init_pi_bsb_dom1_pgs_reg2: b[3],
            clock_rate: u32at(0x04),
            pc: u32at(0x08),
            release: u32at(0x0C),
            crc1: u32at(0x10),
            crc2: u32at(0x14),
            unknown: [u32at(0x18), u32at(0x1C)],
            name_raw,
            unknown2: u32at(0x34),
            manufacturer_id: u32at(0x38),
            cartridge_id: u16::from_be_bytes([b[0x3C], b[0x3D]]),
            country_code: b[0x3E],
            version: b[0x3F],
        })
    }
}

/// C# `CountryCodeToSystemType`.
pub fn country_to_system(code: u8) -> SystemType {
    match code {
        0x44 | 0x46 | 0x49 | 0x50 | 0x53 | 0x55 | 0x58 | 0x59 => SystemType::Pal,
        0x37 | 0x41 | 0x45 | 0x4A => SystemType::Ntsc,
        // The C# comment says "Fallback for unknown codes"; MPAL is never
        // returned by this function even though the enum has it.
        _ => SystemType::Ntsc,
    }
}

/// C# `CountryCodeToString`.
///
/// Takes the **raw** byte. The C# passed a byte-swapped `ushort` here, which is
/// why it always printed "Unknown" — see bug 2.
pub fn country_to_string(code: u8) -> String {
    match code {
        0x00 => "Demo".into(),
        0x37 => "Beta".into(),
        0x41 => "USA/Japan".into(),
        0x44 => "Germany".into(),
        0x45 => "USA".into(),
        0x46 => "France".into(),
        0x49 => "Italy".into(),
        0x4A => "Japan".into(),
        0x53 => "Spain".into(),
        // The C# format strings are missing their closing parenthesis; kept so
        // log output matches between the trees.
        0x55 | 0x59 => format!("Australia (0x{code:02X}"),
        0x50 | 0x58 | 0x20 | 0x21 | 0x38 | 0x70 => format!("Europe (0x{code:02X}"),
        _ => format!("Unknown (0x{code:02X}"),
    }
}

/// C# `IsValidRom` — length-checked, unlike the original.
pub fn is_valid_rom(image: &[u8]) -> bool {
    if image.len() < 4 {
        return false;
    }
    let magic = u32::from_le_bytes([image[0], image[1], image[2], image[3]]);
    magic == Z64_MAGIC
        || (magic == V64_MAGIC && image.len() % 2 == 0)
        || (magic == N64_MAGIC && image.len() % 4 == 0)
}

/// C# `SwapCopyRom` — normalise any container to native big-endian order.
pub fn swap_copy_rom(image: &[u8]) -> Option<(Vec<u8>, ImageType)> {
    if !is_valid_rom(image) {
        return None;
    }
    let magic = u32::from_le_bytes([image[0], image[1], image[2], image[3]]);
    let mut out = image.to_vec();
    let kind = if magic == V64_MAGIC {
        endianness::byte_swap16(&mut out)?;
        ImageType::V64
    } else if magic == N64_MAGIC {
        endianness::byte_swap32(&mut out)?;
        ImageType::N64
    } else {
        ImageType::Z64
    };
    Some((out, kind))
}

/// C# `class N64Rom`.
#[derive(Debug, Clone, PartialEq)]
pub struct N64Rom {
    pub image: Vec<u8>,
    pub image_type: ImageType,
    pub system_type: SystemType,
    pub header: RomHeader,
    pub name: String,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum N64Error {
    /// C#: `throw new Exception("not a valid ROM image")`.
    NotAValidRom,
    /// Shorter than a 0x40-byte header.
    TooShort,
}

impl std::fmt::Display for N64Error {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            N64Error::NotAValidRom => write!(f, "not a valid ROM image"),
            N64Error::TooShort => write!(f, "ROM is shorter than its header"),
        }
    }
}

impl std::error::Error for N64Error {}

impl N64Rom {
    /// C# `N64Rom(FileSystem vfx, string path)`, taking the bytes directly.
    ///
    /// The C# also computed an MD5 of the normalised image and logged a dozen
    /// header fields. The hash is not used for anything and would pull in a
    /// crypto dependency, so it is left to the caller; the logging belongs at
    /// the call site rather than in a parser.
    pub fn parse(image: &[u8]) -> Result<Self, N64Error> {
        let (image, image_type) = swap_copy_rom(image).ok_or(N64Error::NotAValidRom)?;
        let header = RomHeader::read(&image).ok_or(N64Error::TooShort)?;
        let system_type = country_to_system(header.country_code);
        let name = header.name();
        Ok(Self { image, image_type, system_type, header, name })
    }

    /// C# `RomSize`.
    #[inline]
    pub fn rom_size(&self) -> usize {
        self.image.len()
    }
}

// NOT PORTED: `N64FileSystem`. Its constructor parses a ROM and throws the
// result away; `FileExists`, `FileInfo`, and `Open` are all
// `NotImplementedException` and `Glob` returns empty. There is no filesystem
// here to translate. When the N64 container layout is implemented, build it on
// `N64Rom` above and implement `crate::vfx::FileSystem`.

#[cfg(test)]
mod tests {
    use super::*;

    /// A minimal native-order (.z64) ROM: magic, then a 0x40 header.
    fn z64(name: &[u8], country: u8) -> Vec<u8> {
        let mut v = vec![0u8; 0x40];
        v[0..4].copy_from_slice(&[0x80, 0x37, 0x12, 0x40]);
        v[0x20..0x20 + name.len()].copy_from_slice(name);
        v[0x3E] = country;
        v
    }

    #[test]
    fn parses_a_native_rom() {
        let r = N64Rom::parse(&z64(b"SUPER MARIO 64", 0x45)).unwrap();
        assert_eq!(r.image_type, ImageType::Z64);
        assert_eq!(r.name, "SUPER MARIO 64");
        assert_eq!(r.system_type, SystemType::Ntsc);
    }

    #[test]
    fn byteswapped_and_wordswapped_normalise_to_the_same_rom() {
        let native = z64(b"TEST", 0x45);

        let mut v64 = native.clone();
        endianness::byte_swap16(&mut v64).unwrap();
        assert_eq!(&v64[0..4], &[0x37, 0x80, 0x40, 0x12], "V64 magic on disk");

        let mut n64 = native.clone();
        endianness::byte_swap32(&mut n64).unwrap();
        assert_eq!(&n64[0..4], &[0x40, 0x12, 0x37, 0x80], "N64 magic on disk");

        let a = N64Rom::parse(&v64).unwrap();
        let b = N64Rom::parse(&n64).unwrap();
        let c = N64Rom::parse(&native).unwrap();
        assert_eq!(a.image_type, ImageType::V64);
        assert_eq!(b.image_type, ImageType::N64);
        assert_eq!((a.name.as_str(), b.name.as_str()), ("TEST", "TEST"));
        assert_eq!(a.image, c.image, "all three normalise identically");
        assert_eq!(b.image, c.image);
    }

    #[test]
    fn short_input_is_rejected_without_reading_past_the_end() {
        // The C# `IsValidRom` dereferences a uint* with no length check.
        assert!(!is_valid_rom(&[]));
        assert!(!is_valid_rom(&[0x80, 0x37]));
        assert!(matches!(N64Rom::parse(&[0x80]), Err(N64Error::NotAValidRom)));
    }

    #[test]
    fn a_valid_magic_but_truncated_body_is_too_short() {
        let mut v = vec![0u8; 8];
        v[0..4].copy_from_slice(&[0x80, 0x37, 0x12, 0x40]);
        assert!(matches!(N64Rom::parse(&v), Err(N64Error::TooShort)));
    }

    #[test]
    fn name_stops_at_the_first_nul_and_never_overruns() {
        // The C# writes a terminator at Name[20], clobbering the next field.
        let mut raw = z64(b"AB", 0x45);
        raw[0x34..0x38].copy_from_slice(&[0xDE, 0xAD, 0xBE, 0xEF]); // Unknown2
        let r = N64Rom::parse(&raw).unwrap();
        assert_eq!(r.name, "AB");
        assert_eq!(r.header.unknown2, 0xDEAD_BEEF, "adjacent field intact");
    }

    #[test]
    fn a_full_twenty_byte_name_is_not_truncated() {
        let r = N64Rom::parse(&z64(b"12345678901234567890", 0x45)).unwrap();
        assert_eq!(r.name, "12345678901234567890");
    }

    #[test]
    fn country_display_uses_the_raw_byte() {
        // The C# byte-swaps this as a ushort, so 0x45 becomes 0x4500 and every
        // ROM prints "Unknown".
        assert_eq!(country_to_string(0x45), "USA");
        assert_eq!(country_to_string(0x4A), "Japan");
        assert!(country_to_string(0xFF).starts_with("Unknown"));
    }

    #[test]
    fn pal_and_ntsc_regions_map_correctly() {
        assert_eq!(country_to_system(0x50), SystemType::Pal);
        assert_eq!(country_to_system(0x45), SystemType::Ntsc);
        assert_eq!(country_to_system(0xFF), SystemType::Ntsc, "fallback");
    }

    #[test]
    fn header_fields_are_read_big_endian() {
        let mut raw = z64(b"X", 0x45);
        raw[0x10..0x14].copy_from_slice(&[0x12, 0x34, 0x56, 0x78]); // CRC1
        let r = N64Rom::parse(&raw).unwrap();
        assert_eq!(r.header.crc1, 0x1234_5678);
    }
}
