// PORT-SOURCE: Gfx/OpenStack.Gfx/Gfx_Bitmap.cs
// PORT-SHA: e09458ad7e0d280a
// PORT-STATUS: done
//
// `DirectBitmap` — an ARGB pixel buffer that the C# also pins so a
// `System.Drawing.Bitmap` can alias it.
//
// THE WHOLE BITMAP HALF IS DEAD, AND ONE PATH ALWAYS CRASHES.
// `static bool UseDrawing` is declared and **never assigned anywhere in the
// solution**, so it is permanently `false`. Therefore:
//
//   * The constructor always takes the `: null` branch — `Bitmap` is always
//     null.
//   * `Dispose()` calls `Bitmap.Dispose()` with no null check, so **every
//     dispose throws NullReferenceException**. The type cannot be used in a
//     `using` block at all.
//   * `Save(path)` is `if (path != "path" && UseDrawing) ...`, so it **never
//     writes a file**. Callers get a silent no-op.
//
// What survives is the pixel buffer, which is all Rust needs: a `Vec<u32>` is
// already contiguous and can be handed to any encoder as `&[u8]` via bytemuck,
// with no pinning, no GC handle, and no `System.Drawing` dependency (which is
// Windows-only in .NET 6+ anyway).
//
// PNG encoding is left to the caller. Adding an image crate here would pick a
// dependency for the whole workspace to serve a function that currently does
// nothing; `as_bgra_bytes` hands over exactly what an encoder wants.

use bytemuck::cast_slice;

/// ARGB colour, matching `System.Drawing.Color.ToArgb()` packing.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub struct Color {
    pub a: u8,
    pub r: u8,
    pub g: u8,
    pub b: u8,
}

impl Color {
    pub const fn new(a: u8, r: u8, g: u8, b: u8) -> Self {
        Self { a, r, g, b }
    }

    /// C# `Color.FromArgb(int)`.
    #[inline]
    pub const fn from_argb(v: u32) -> Self {
        Self {
            a: (v >> 24) as u8,
            r: (v >> 16) as u8,
            g: (v >> 8) as u8,
            b: v as u8,
        }
    }

    /// C# `Color.ToArgb()`.
    #[inline]
    pub const fn to_argb(self) -> u32 {
        ((self.a as u32) << 24)
            | ((self.r as u32) << 16)
            | ((self.g as u32) << 8)
            | (self.b as u32)
    }
}

/// C# `class DirectBitmap` — the pixel buffer, without the dead Bitmap half.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct DirectBitmap {
    pixels: Vec<u32>,
    width: usize,
    height: usize,
}

impl DirectBitmap {
    /// C# `DirectBitmap(int width, int height)`.
    ///
    /// The C# multiplies without checking, so a large width/height pair
    /// overflows `int` and allocates a buffer far smaller than the caller
    /// believes. Returns `None` here.
    pub fn new(width: usize, height: usize) -> Option<Self> {
        let n = width.checked_mul(height)?;
        Some(Self { pixels: vec![0; n], width, height })
    }

    #[inline]
    pub fn width(&self) -> usize {
        self.width
    }

    #[inline]
    pub fn height(&self) -> usize {
        self.height
    }

    /// C# `SetPixel(int x, int y, Color)`.
    ///
    /// The C# indexes `x + y * Width` unchecked: an out-of-range `x` silently
    /// writes into the neighbouring row rather than failing.
    #[inline]
    pub fn set_pixel(&mut self, x: usize, y: usize, color: Color) -> bool {
        if x >= self.width || y >= self.height {
            return false;
        }
        self.pixels[x + y * self.width] = color.to_argb();
        true
    }

    /// C# `GetPixel(int x, int y)`.
    #[inline]
    pub fn get_pixel(&self, x: usize, y: usize) -> Option<Color> {
        if x >= self.width || y >= self.height {
            return None;
        }
        Some(Color::from_argb(self.pixels[x + y * self.width]))
    }

    /// C# `Pixels` — the raw ARGB words.
    #[inline]
    pub fn pixels(&self) -> &[u32] {
        &self.pixels
    }

    #[inline]
    pub fn pixels_mut(&mut self) -> &mut [u32] {
        &mut self.pixels
    }

    /// The buffer as bytes, for handing to an image encoder.
    ///
    /// On a little-endian host an ARGB word lays out as B,G,R,A — i.e. BGRA
    /// byte order, which is what `PixelFormat.Format32bppPArgb` meant and what
    /// most encoders expect.
    #[inline]
    pub fn as_bgra_bytes(&self) -> &[u8] {
        cast_slice(&self.pixels)
    }

    /// Repacked to RGBA byte order, for encoders that want that instead.
    pub fn to_rgba_bytes(&self) -> Vec<u8> {
        let mut out = Vec::with_capacity(self.pixels.len() * 4);
        for &p in &self.pixels {
            let c = Color::from_argb(p);
            out.extend_from_slice(&[c.r, c.g, c.b, c.a]);
        }
        out
    }
}

// NOT PORTED: `Save(string path)`. It is gated on `UseDrawing`, which is never
// true, so it has never written a file. Use `as_bgra_bytes`/`to_rgba_bytes`
// with an encoder of your choosing.

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn pixels_round_trip() {
        let mut b = DirectBitmap::new(4, 3).unwrap();
        let c = Color::new(0xFF, 0x11, 0x22, 0x33);
        assert!(b.set_pixel(1, 2, c));
        assert_eq!(b.get_pixel(1, 2).unwrap(), c);
        assert_eq!(b.get_pixel(0, 0).unwrap(), Color::from_argb(0));
    }

    #[test]
    fn out_of_range_access_is_rejected_not_wrapped() {
        // The C# writes into the next row instead of failing.
        let mut b = DirectBitmap::new(4, 3).unwrap();
        assert!(!b.set_pixel(4, 0, Color::default()));
        assert!(b.get_pixel(0, 3).is_none());
    }

    #[test]
    fn argb_packing_matches_the_c_sharp() {
        let c = Color::new(0x12, 0x34, 0x56, 0x78);
        assert_eq!(c.to_argb(), 0x1234_5678);
        assert_eq!(Color::from_argb(0x1234_5678), c);
    }

    #[test]
    fn oversized_dimensions_are_rejected() {
        assert!(DirectBitmap::new(usize::MAX, 2).is_none());
    }

    #[test]
    fn byte_views_have_the_right_length_and_order() {
        let mut b = DirectBitmap::new(2, 1).unwrap();
        b.set_pixel(0, 0, Color::new(0xAA, 0x11, 0x22, 0x33));
        assert_eq!(b.as_bgra_bytes().len(), 8);
        assert_eq!(&b.to_rgba_bytes()[..4], &[0x11, 0x22, 0x33, 0xAA]);
    }

    #[test]
    fn zero_sized_bitmaps_are_allowed() {
        let b = DirectBitmap::new(0, 0).unwrap();
        assert!(b.pixels().is_empty());
    }
}
