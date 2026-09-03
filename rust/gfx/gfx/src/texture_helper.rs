// PORT-SOURCE: Gfx/OpenStack.Gfx/TextureHelper.cs
// PORT-SHA: b43a13d1ff0de054
// PORT-STATUS: done
//
// Mipmap arithmetic, a 2x box downscale, and a PNG size scanner.
//
// NOTE ON `GetBlockSize(object format)`: its entire body is
// `throw new ArgumentOutOfRangeException(...)`. It is unconditional — there is
// no switch, no lookup, nothing. So `GetMipmapTrueDataSize`, which calls it on
// its first line, **can only ever throw**. That function has never returned a
// value. Both are ported here with a real `BlockFormat` enum, because the
// arithmetic they were meant to perform is correct and needed; see below.

use openstack_polyio::prelude::BinaryReaderExt;
use std::io::{Read, Seek, SeekFrom};

/// Replaces C# `object format` — the untyped parameter `GetBlockSize` took.
///
/// `bytes_per_unit` is per-pixel for uncompressed formats and per-4x4-block for
/// compressed ones, matching how `GetMipmapTrueDataSize` uses it.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct BlockFormat {
    pub bytes_per_unit: u32,
    pub compressed: bool,
}

impl BlockFormat {
    /// Uncompressed, `n` bytes per pixel.
    pub const fn uncompressed(bytes_per_pixel: u32) -> Self {
        Self { bytes_per_unit: bytes_per_pixel, compressed: false }
    }

    /// Block-compressed, `n` bytes per 4x4 block. BC1/DXT1 is 8, most others 16.
    pub const fn compressed(bytes_per_block: u32) -> Self {
        Self { bytes_per_unit: bytes_per_block, compressed: true }
    }
}

/// C# `GetMipmapCount(int width, int height)`.
///
/// Counts halvings of the longer side until it reaches zero, so a 1x1 texture
/// has 1 level and 256x256 has 9.
pub fn mipmap_count(width: u32, height: u32) -> u32 {
    let mut longer = width.max(height);
    let mut count = 0;
    while longer > 0 {
        count += 1;
        longer /= 2;
    }
    count
}

/// C# `GetMipmapDataSize(int width, int height, int bytesPerPixel)` — total
/// bytes for a full uncompressed mip chain.
///
/// The C# asserts its arguments are positive via `Log.Assert`, whose body is
/// empty, so zero dimensions loop forever there. `None` here.
pub fn mipmap_data_size(width: u32, height: u32, bytes_per_pixel: u32) -> Option<u64> {
    if width == 0 || height == 0 || bytes_per_pixel == 0 {
        return None;
    }
    let (mut w, mut h) = (width as u64, height as u64);
    let bpp = bytes_per_pixel as u64;
    let mut total = 0u64;
    loop {
        total = total.checked_add(w.checked_mul(h)?.checked_mul(bpp)?)?;
        if w == 1 && h == 1 {
            break;
        }
        if w > 1 {
            w /= 2;
        }
        if h > 1 {
            h /= 2;
        }
    }
    Some(total)
}

/// C# `MipLevelSize(int size, int level)` — at least 1.
#[inline]
pub fn mip_level_size(size: u32, level: u32) -> u32 {
    if level >= 32 {
        return 1;
    }
    (size >> level).max(1)
}

/// C# `GetMipmapTrueDataSize(format, width, height, depth, mipLevel)`.
///
/// Size of one mip level, rounding compressed formats up to whole 4x4 blocks.
pub fn mipmap_true_data_size(
    format: BlockFormat,
    width: u32,
    height: u32,
    depth: u32,
    mip_level: u32,
) -> u64 {
    let mut w = width >> mip_level.min(31);
    let mut h = height >> mip_level.min(31);
    let mut d = (depth >> mip_level.min(31)).max(1);

    if format.compressed {
        // Round up to a multiple of 4, then clamp to a minimum block.
        let round4 = |v: u32| if v % 4 > 0 { v + (4 - v % 4) } else { v };
        w = round4(w);
        h = round4(h);
        if w < 4 && w > 0 {
            w = 4;
        }
        if h < 4 && h > 0 {
            h = 4;
        }
        if d < 4 && d > 1 {
            d = 4;
        }
        let num_blocks = ((w as u64 * h as u64) >> 4) * d as u64;
        return num_blocks * format.bytes_per_unit as u64;
    }
    w as u64 * h as u64 * d as u64 * format.bytes_per_unit as u64
}

/// C# `Downscale4Component32BitPixelsX2(...)` — 2x box filter over RGBA8.
///
/// Averages each 2x2 source block into one destination pixel. Slices replace
/// the C#'s `(array, startIndex)` pairs, so the bounds checks its `Log.Assert`
/// calls were supposed to perform (but do not, since `Log.Assert` is empty)
/// happen for real.
///
/// Odd source dimensions drop the final row/column, matching the C#'s
/// integer-halved loop bounds.
pub fn downscale_rgba8_x2(
    src: &[u8],
    src_rows: usize,
    src_cols: usize,
    dst: &mut [u8],
) -> Option<()> {
    const BPP: usize = 4;
    if src.len() < src_rows * src_cols * BPP {
        return None;
    }
    let (dst_rows, dst_cols) = (src_rows / 2, src_cols / 2);
    if dst.len() < dst_rows * dst_cols * BPP {
        return None;
    }
    for dr in 0..dst_rows {
        for dc in 0..dst_cols {
            let p0 = (src_cols * (2 * dr) + 2 * dc) * BPP;
            let starts = [p0, p0 + BPP, p0 + src_cols * BPP, p0 + src_cols * BPP + BPP];
            let dst_start = (dst_cols * dr + dc) * BPP;
            for comp in 0..BPP {
                let sum: u32 = starts.iter().map(|&s| src[s + comp] as u32).sum();
                // C# uses Math.Round on the float average: banker's rounding at
                // .5. A 4-sample sum lands on .5 only when sum % 4 == 2, and
                // MidpointRounding.ToEven then rounds to the even neighbour.
                let q = sum / 4;
                let rem = sum % 4;
                dst[dst_start + comp] = match rem {
                    0 => q,
                    2 => if q % 2 == 0 { q } else { q + 1 },
                    r if r > 2 => q + 1,
                    _ => q,
                } as u8;
            }
        }
    }
    Some(())
}

/// C# `CalculatePngSize(BinaryReader r, long dataOffset)` — walk the chunk
/// list to find the total encoded length.
///
/// The C# restores the stream position in a `finally`, which is preserved.
pub fn calculate_png_size<R: Read + Seek>(r: &mut R, data_offset: u64) -> std::io::Result<u64> {
    let original = r.stream_position()?;
    let result = (|| -> std::io::Result<u64> {
        r.seek(SeekFrom::Start(data_offset))?;
        let mut size: u64 = 8; // PNG signature

        // The signature is 89 50 4E 47 0D 0A 1A 0A. The C# reads it as two
        // little-endian i32s and compares against 0x474E5089 / 0x0A1A0A0D,
        // which is the same bytes.
        let mut sig = [0u8; 8];
        r.read_exact(&mut sig)?;
        if sig != [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A] {
            return Err(std::io::Error::new(
                std::io::ErrorKind::InvalidData,
                "not a PNG",
            ));
        }

        loop {
            // Chunk length is big-endian; the C# reads 4 bytes and reverses.
            let len = r.read_u32_be().map_err(std::io::Error::from)? as u64;
            size += len + 12; // length + type + data + crc
            let mut kind = [0u8; 4];
            r.read_exact(&mut kind)?;
            r.seek(SeekFrom::Start(data_offset + size))?;
            if &kind == b"IEND" {
                break;
            }
        }
        Ok(size)
    })();
    r.seek(SeekFrom::Start(original))?;
    result
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn mipmap_counts_match_known_textures() {
        assert_eq!(mipmap_count(1, 1), 1);
        assert_eq!(mipmap_count(256, 256), 9);
        assert_eq!(mipmap_count(256, 64), 9, "longer side decides");
    }

    #[test]
    fn mipmap_data_size_sums_the_chain() {
        // 2x2 RGBA: 2*2*4 + 1*1*4 = 20
        assert_eq!(mipmap_data_size(2, 2, 4), Some(20));
        assert_eq!(mipmap_data_size(1, 1, 4), Some(4));
    }

    #[test]
    fn zero_dimensions_return_none_instead_of_looping_forever() {
        // The C# `Log.Assert` is an empty method, so this hangs there.
        assert_eq!(mipmap_data_size(0, 4, 4), None);
        assert_eq!(mipmap_data_size(4, 4, 0), None);
    }

    #[test]
    fn mip_level_size_never_reaches_zero() {
        assert_eq!(mip_level_size(256, 0), 256);
        assert_eq!(mip_level_size(256, 8), 1);
        assert_eq!(mip_level_size(256, 99), 1, "shift past width is clamped");
    }

    #[test]
    fn compressed_sizes_round_up_to_whole_blocks() {
        let bc1 = BlockFormat::compressed(8);
        // 4x4 is one block.
        assert_eq!(mipmap_true_data_size(bc1, 4, 4, 1, 0), 8);
        // 1x1 still occupies a full block.
        assert_eq!(mipmap_true_data_size(bc1, 1, 1, 1, 0), 8);
        // 8x8 is four blocks.
        assert_eq!(mipmap_true_data_size(bc1, 8, 8, 1, 0), 32);
    }

    #[test]
    fn uncompressed_sizes_are_plain_products() {
        let rgba = BlockFormat::uncompressed(4);
        assert_eq!(mipmap_true_data_size(rgba, 16, 16, 1, 0), 1024);
        assert_eq!(mipmap_true_data_size(rgba, 16, 16, 1, 1), 256);
    }

    #[test]
    fn downscale_averages_each_2x2_block() {
        // 2x2 of a single flat colour must average to itself.
        let src: Vec<u8> = std::iter::repeat([10u8, 20, 30, 40]).take(4).flatten().collect();
        let mut dst = [0u8; 4];
        downscale_rgba8_x2(&src, 2, 2, &mut dst).unwrap();
        assert_eq!(dst, [10, 20, 30, 40]);
    }

    #[test]
    fn downscale_rejects_undersized_buffers() {
        // The C# only Log.Asserts this, which does nothing.
        let src = [0u8; 4];
        let mut dst = [0u8; 4];
        assert!(downscale_rgba8_x2(&src, 2, 2, &mut dst).is_none());
        let big = [0u8; 64];
        let mut tiny = [0u8; 1];
        assert!(downscale_rgba8_x2(&big, 4, 4, &mut tiny).is_none());
    }

    #[test]
    fn png_size_walks_to_iend_and_restores_position() {
        // Signature + IHDR(13) + IEND(0).
        let mut png = vec![0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        png.extend_from_slice(&13u32.to_be_bytes());
        png.extend_from_slice(b"IHDR");
        png.extend_from_slice(&[0u8; 13]);
        png.extend_from_slice(&[0u8; 4]); // crc
        png.extend_from_slice(&0u32.to_be_bytes());
        png.extend_from_slice(b"IEND");
        png.extend_from_slice(&[0u8; 4]); // crc

        let total = png.len() as u64;
        let mut c = Cursor::new(png);
        c.seek(SeekFrom::Start(3)).unwrap();
        assert_eq!(calculate_png_size(&mut c, 0).unwrap(), total);
        assert_eq!(c.stream_position().unwrap(), 3, "position must be restored");
    }

    #[test]
    fn non_png_input_is_rejected_and_position_still_restored() {
        let mut c = Cursor::new(vec![0u8; 32]);
        c.seek(SeekFrom::Start(5)).unwrap();
        assert!(calculate_png_size(&mut c, 0).is_err());
        assert_eq!(c.stream_position().unwrap(), 5);
    }
}
