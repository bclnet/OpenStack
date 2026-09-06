// PORT-SOURCE: Core/OpenStack/Algorithms/Crc32Digest.cs
// PORT-SHA: dd4ea5d59b228eb8
// PORT-STATUS: done
//
// Standard CRC-32 (IEEE 802.3, reflected, polynomial 0xEDB88320).
//
// The C# hard-codes the 256-entry table as a literal. Here it is derived at
// compile time from the polynomial by a `const fn`, and a test asserts the
// derived table reproduces the published check value — so a corrupted table is
// caught by construction rather than by eyeballing 256 hex constants.

/// C# `Crc32Digest.Table`, derived rather than transcribed.
static TABLE: [u32; 256] = build_table();

const POLY: u32 = 0xEDB8_8320;

const fn build_table() -> [u32; 256] {
    let mut t = [0u32; 256];
    let mut i = 0;
    while i < 256 {
        let mut c = i as u32;
        let mut k = 0;
        while k < 8 {
            c = if c & 1 != 0 { POLY ^ (c >> 1) } else { c >> 1 };
            k += 1;
        }
        t[i] = c;
        i += 1;
    }
    t
}

/// C# `Crc32Digest.Compute(byte[] buffer)`.
pub fn compute(buffer: &[u8]) -> u32 {
    update(0xFFFF_FFFF, buffer) ^ 0xFFFF_FFFF
}

/// Incremental form, for hashing a stream without buffering it. Not in the C#,
/// whose only entry point required the whole input in one array.
///
/// Start from `0xFFFF_FFFF` and finish by XOR-ing with `0xFFFF_FFFF`.
pub fn update(mut crc: u32, buffer: &[u8]) -> u32 {
    for &b in buffer {
        crc = (crc >> 8) ^ TABLE[((b as u32) ^ (crc & 0xFF)) as usize];
    }
    crc
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn matches_the_published_check_value() {
        // The CRC-32/ISO-HDLC check value for "123456789".
        assert_eq!(compute(b"123456789"), 0xCBF4_3926);
    }

    #[test]
    fn known_vectors() {
        assert_eq!(compute(b""), 0);
        assert_eq!(compute(b"a"), 0xE8B7_BE43);
        assert_eq!(compute(b"abc"), 0x3524_41C2);
        assert_eq!(compute(b"The quick brown fox jumps over the lazy dog"), 0x414F_A339);
    }

    #[test]
    fn derived_table_has_the_expected_endpoints() {
        // Guards the const fn against a transcription-free but still wrong table.
        assert_eq!(TABLE[0], 0x0000_0000);
        assert_eq!(TABLE[1], 0x7707_3096);
        assert_eq!(TABLE[255], 0x2D02_EF8D);
    }

    #[test]
    fn incremental_matches_one_shot() {
        let data = b"the quick brown fox";
        let mut crc = 0xFFFF_FFFFu32;
        for chunk in data.chunks(3) {
            crc = update(crc, chunk);
        }
        assert_eq!(crc ^ 0xFFFF_FFFF, compute(data));
    }
}
