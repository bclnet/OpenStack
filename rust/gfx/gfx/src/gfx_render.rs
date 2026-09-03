// PORT-SOURCE: Gfx/OpenStack.Gfx/Gfx_Render.cs
// PORT-SHA: b6e36e57ac677d7f
// PORT-STATUS: done
//
// Colour types, the renderer base class, and palette blitting.
//
// ================= FOUR BUGS IN `Colorf`, TWO OF THEM FATAL ================
//
//   1. `operator ==` IS INFINITE RECURSION:
//          public static bool operator ==(Colorf lhs, Colorf rhs) => (lhs == rhs);
//      The body invokes the operator it defines. Any `a == b` on a `Colorf`
//      **overflows the stack**, and `!=` calls `==`, so it dies too.
//
//   2. `Equals(Colorf)` ALWAYS RETURNS FALSE:
//          R.Equals(other.R) && Equals(other.G) && B.Equals(other.B) && ...
//      The second term is `Equals(other.G)`, not `G.Equals(other.G)` — it calls
//      `this.Equals(object)` with a boxed float, whose `other is Colorf` test
//      fails. So two identical colours never compare equal. Combined with (1),
//      `Colorf` has no working equality at all: one path lies, the other
//      crashes.
//
//   3. The `(uint, Format.ARGB32)` constructor DOES NOT NORMALISE. It assigns
//      `A = color >> 24` and so on, yielding 0..255 — but every other member
//      treats components as 0..1 floats (`White` is `1,1,1,1`). A colour built
//      from ARGB32 is 255x too bright and breaks every blend it touches.
//
//   4. The indexer's out-of-range message says "Invalid Vector3 index" on a
//      four-component colour.
//
// And in `Color32`, the byte<->float conversions use **`0xfff` (4095) where
// they mean `0xff` (255)**, in both directions. `Colorf -> Color32` multiplies
// by 4095 before a `(byte)` cast (out-of-range float->byte is undefined in an
// unchecked context), and `Color32 -> Colorf` divides by 4095, so 255 decodes
// to 0.062 instead of 1.0. The round trip is destroyed in both directions.
//
// This port derives working equality, normalises the ARGB32 constructor, and
// uses 255. Each is called out at its definition. **All of these want fixing in
// the C# tree**; see PORTING.md.

use glam::Vec4;

use openstack_polyfills::system::math_x::clamp;

// ---------------------------------------------------------------------------
// Colorf
// ---------------------------------------------------------------------------

/// C# `struct Colorf` — linear float RGBA, components nominally 0..1.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Default)]
pub struct Colorf {
    pub r: f32,
    pub g: f32,
    pub b: f32,
    pub a: f32,
}

impl Colorf {
    pub const RED: Self = Self::new(1.0, 0.0, 0.0, 1.0);
    pub const GREEN: Self = Self::new(0.0, 1.0, 0.0, 1.0);
    pub const BLUE: Self = Self::new(0.0, 0.0, 1.0, 1.0);
    pub const WHITE: Self = Self::new(1.0, 1.0, 1.0, 1.0);
    pub const BLACK: Self = Self::new(0.0, 0.0, 0.0, 1.0);
    pub const YELLOW: Self = Self::new(1.0, 0.921_568_6, 0.015_686_28, 1.0);
    pub const CYAN: Self = Self::new(0.0, 1.0, 1.0, 1.0);
    pub const MAGENTA: Self = Self::new(1.0, 0.0, 1.0, 1.0);
    pub const GRAY: Self = Self::new(0.5, 0.5, 0.5, 1.0);
    pub const CLEAR: Self = Self::new(0.0, 0.0, 0.0, 0.0);

    pub const fn new(r: f32, g: f32, b: f32, a: f32) -> Self {
        Self { r, g, b, a }
    }

    /// C# `Colorf(uint color, Format.ARGB32)`.
    ///
    /// **Divides by 255**, which the C# does not — see bug 3 in the module
    /// header. Without it every colour from this path is 255x too bright.
    pub fn from_argb32(color: u32) -> Self {
        Self {
            a: ((color >> 24) & 0xFF) as f32 / 255.0,
            r: ((color >> 16) & 0xFF) as f32 / 255.0,
            g: ((color >> 8) & 0xFF) as f32 / 255.0,
            b: (color & 0xFF) as f32 / 255.0,
        }
    }

    /// The C#'s literal behaviour, for any caller that depends on the 0..255
    /// range coming out of this constructor.
    #[deprecated(note = "mirrors a C#-side bug: yields 0..255, not 0..1")]
    pub fn from_argb32_unnormalized(color: u32) -> Self {
        Self {
            a: (color >> 24) as f32,
            r: ((color >> 16) & 0xFF) as f32,
            g: ((color >> 8) & 0xFF) as f32,
            b: (color & 0xFF) as f32,
        }
    }

    /// C# `this[int index]`. The C# message said "Invalid Vector3 index".
    #[inline]
    pub fn get(&self, index: usize) -> Option<f32> {
        Some(match index {
            0 => self.r,
            1 => self.g,
            2 => self.b,
            3 => self.a,
            _ => return None,
        })
    }

    #[inline]
    pub fn set(&mut self, index: usize, value: f32) -> bool {
        match index {
            0 => self.r = value,
            1 => self.g = value,
            2 => self.b = value,
            3 => self.a = value,
            _ => return false,
        }
        true
    }

    /// C# `Grayscale` — Rec. 601 luma.
    #[inline]
    pub fn grayscale(&self) -> f32 {
        0.299 * self.r + 0.587 * self.g + 0.114 * self.b
    }

    /// C# `MaxColorComponent` — alpha excluded, as in the C#.
    #[inline]
    pub fn max_color_component(&self) -> f32 {
        self.r.max(self.g).max(self.b)
    }

    /// C# `Lerp(Colorf a, Colorf b, float t)` — `t` clamped to 0..1.
    pub fn lerp(a: Self, b: Self, t: f32) -> Self {
        Self::lerp_unclamped(a, b, clamp(t, 0.0, 1.0))
    }

    /// C# `LerpUnclamped`.
    pub fn lerp_unclamped(a: Self, b: Self, t: f32) -> Self {
        Self::new(
            a.r + (b.r - a.r) * t,
            a.g + (b.g - a.g) * t,
            a.b + (b.b - a.b) * t,
            a.a + (b.a - a.a) * t,
        )
    }

    /// C# `RGBMultiplied(float)` — alpha untouched.
    #[inline]
    pub fn rgb_multiplied(self, m: f32) -> Self {
        Self::new(self.r * m, self.g * m, self.b * m, self.a)
    }

    /// C# `AlphaMultiplied(float)`.
    #[inline]
    pub fn alpha_multiplied(self, m: f32) -> Self {
        Self::new(self.r, self.g, self.b, self.a * m)
    }

    /// C# `RGBToHSV(Colorf, out H, out S, out V)`. Hue is 0..1, not degrees.
    pub fn to_hsv(self) -> (f32, f32, f32) {
        if self.b > self.g && self.b > self.r {
            hsv_helper(4.0, self.b, self.r, self.g)
        } else if self.g > self.r {
            hsv_helper(2.0, self.g, self.b, self.r)
        } else {
            hsv_helper(0.0, self.r, self.g, self.b)
        }
    }

    /// C# `HSVToRGB(float h, float s, float v, bool hdr = true)`.
    pub fn from_hsv(h: f32, s: f32, v: f32, hdr: bool) -> Self {
        let mut c = Self::WHITE;
        if s == 0.0 {
            c.r = v;
            c.g = v;
            c.b = v;
        } else if v == 0.0 {
            c.r = 0.0;
            c.g = 0.0;
            c.b = 0.0;
        } else {
            let f = h * 6.0;
            let whole = f.floor() as i32;
            let remain = f - whole as f32;
            let r1 = v * (1.0 - s);
            let r2 = v * (1.0 - s * remain);
            let r3 = v * (1.0 - s * (1.0 - remain));
            let (r, g, b) = match whole {
                -1 => (v, r1, r2),
                0 => (v, r3, r1),
                1 => (r2, v, r1),
                2 => (r1, v, r3),
                3 => (r1, r2, v),
                4 => (r3, r1, v),
                5 => (v, r1, r2),
                6 => (v, r3, r1),
                // C# `default: break` leaves the components at 0.
                _ => (0.0, 0.0, 0.0),
            };
            c.r = r;
            c.g = g;
            c.b = b;
            if !hdr {
                c.r = clamp(c.r, 0.0, 1.0);
                c.g = clamp(c.g, 0.0, 1.0);
                c.b = clamp(c.b, 0.0, 1.0);
            }
        }
        c
    }
}

fn hsv_helper(offset: f32, dominant: f32, one: f32, two: f32) -> (f32, f32, f32) {
    let v = dominant;
    if v == 0.0 {
        return (0.0, 0.0, 0.0);
    }
    let min = if one <= two { one } else { two };
    let delta = v - min;
    let (s, mut h) = if delta != 0.0 {
        (delta / v, offset + (one - two) / delta)
    } else {
        (0.0, offset + (one - two))
    };
    h /= 6.0;
    if h < 0.0 {
        h += 1.0;
    }
    (h, s, v)
}

impl From<Colorf> for Vec4 {
    fn from(c: Colorf) -> Self {
        Vec4::new(c.r, c.g, c.b, c.a)
    }
}

impl From<Vec4> for Colorf {
    fn from(v: Vec4) -> Self {
        Colorf::new(v.x, v.y, v.z, v.w)
    }
}

impl std::ops::Add for Colorf {
    type Output = Self;
    fn add(self, o: Self) -> Self {
        Self::new(self.r + o.r, self.g + o.g, self.b + o.b, self.a + o.a)
    }
}

impl std::ops::Sub for Colorf {
    type Output = Self;
    fn sub(self, o: Self) -> Self {
        Self::new(self.r - o.r, self.g - o.g, self.b - o.b, self.a - o.a)
    }
}

impl std::ops::Mul for Colorf {
    type Output = Self;
    fn mul(self, o: Self) -> Self {
        Self::new(self.r * o.r, self.g * o.g, self.b * o.b, self.a * o.a)
    }
}

impl std::ops::Mul<f32> for Colorf {
    type Output = Self;
    fn mul(self, s: f32) -> Self {
        Self::new(self.r * s, self.g * s, self.b * s, self.a * s)
    }
}

impl std::ops::Mul<Colorf> for f32 {
    type Output = Colorf;
    fn mul(self, c: Colorf) -> Colorf {
        c * self
    }
}

impl std::ops::Div<f32> for Colorf {
    type Output = Self;
    fn div(self, s: f32) -> Self {
        Self::new(self.r / s, self.g / s, self.b / s, self.a / s)
    }
}

impl std::fmt::Display for Colorf {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "RGBA({:.3}, {:.3}, {:.3}, {:.3})",
            self.r, self.g, self.b, self.a
        )
    }
}

// ---------------------------------------------------------------------------
// Color32
// ---------------------------------------------------------------------------

/// C# `struct Color32` — 8-bit RGBA.
///
/// The C# overlays an unused `int _rgba` at offset 0 via `[FieldOffset]`;
/// `to_rgba`/`from_rgba` give the same view without a union.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default, Hash)]
pub struct Color32 {
    pub r: u8,
    pub g: u8,
    pub b: u8,
    pub a: u8,
}

impl Color32 {
    pub const fn new(r: u8, g: u8, b: u8, a: u8) -> Self {
        Self { r, g, b, a }
    }

    /// The packed little-endian word the C#'s `_rgba` field aliased.
    #[inline]
    pub const fn to_rgba(self) -> u32 {
        (self.r as u32) | ((self.g as u32) << 8) | ((self.b as u32) << 16) | ((self.a as u32) << 24)
    }

    #[inline]
    pub const fn from_rgba(v: u32) -> Self {
        Self { r: v as u8, g: (v >> 8) as u8, b: (v >> 16) as u8, a: (v >> 24) as u8 }
    }

    /// C# `Lerp(Color32 a, Color32 b, float t)` — `t` clamped.
    pub fn lerp(a: Self, b: Self, t: f32) -> Self {
        Self::lerp_unclamped(a, b, clamp(t, 0.0, 1.0))
    }

    /// C# `LerpUnclamped`.
    ///
    /// Each channel is clamped to 0..255 before the cast; the C# casts a
    /// possibly-out-of-range float straight to `byte`, which is undefined in an
    /// unchecked context.
    pub fn lerp_unclamped(a: Self, b: Self, t: f32) -> Self {
        let c = |x: u8, y: u8| {
            let v = x as f32 + (y as f32 - x as f32) * t;
            clamp(v, 0.0, 255.0) as u8
        };
        Self::new(c(a.r, b.r), c(a.g, b.g), c(a.b, b.b), c(a.a, b.a))
    }
}

/// C# `implicit operator Color32(Colorf)`.
///
/// **Scales by 255**, where the C# used `0xfff` (4095) — see the module header.
impl From<Colorf> for Color32 {
    fn from(c: Colorf) -> Self {
        let q = |v: f32| (clamp(v, 0.0, 1.0) * 255.0).round() as u8;
        Color32::new(q(c.r), q(c.g), q(c.b), q(c.a))
    }
}

/// C# `implicit operator Colorf(Color32)`. Divides by 255, not 4095.
impl From<Color32> for Colorf {
    fn from(c: Color32) -> Self {
        Colorf::new(
            c.r as f32 / 255.0,
            c.g as f32 / 255.0,
            c.b as f32 / 255.0,
            c.a as f32 / 255.0,
        )
    }
}

impl std::fmt::Display for Color32 {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "RGBA({}, {}, {}, {})", self.r, self.g, self.b, self.a)
    }
}

// ---------------------------------------------------------------------------
// Renderer
// ---------------------------------------------------------------------------

/// C# `Renderer.Pass`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
pub enum Pass {
    #[default]
    Both,
    Opaque,
    Translucent,
}

/// C# `abstract class Renderer : IDisposable`.
///
/// Every member is `virtual` with an empty body, so this is an interface with
/// defaults — a trait with provided methods. `Dispose` is dropped: `Drop` runs
/// automatically, and an implementor that needs teardown implements it.
pub trait Renderer {
    fn start(&mut self) {}
    fn stop(&mut self) {}
    fn update(&mut self, _delta_time: f32) {}
}

// ---------------------------------------------------------------------------
// Raster
// ---------------------------------------------------------------------------

/// C# `Raster.BlitByPalette(Span<byte> data, int bbp, byte[] source, byte[] palette, int pbp, byte? alpha)`.
///
/// Expands palette-indexed pixels into `dst`. `dst_bpp` and `pal_bpp` are bytes
/// per output pixel and per palette entry; both must be 3 or 4.
///
/// The C# takes a raw pointer to `data` and writes `source.Length * bbp` bytes
/// with **no bounds check on either `data` or `palette`** — an undersized
/// destination or a palette index past the end corrupts memory silently. Every
/// access is checked here, and unsupported bpp combinations return `None`
/// rather than falling through the `if` chain and writing nothing (which is
/// what the C# does: neither `pbp` nor `bbp` matching leaves `data` untouched,
/// with no indication anything went wrong).
pub fn blit_by_palette(
    dst: &mut [u8],
    dst_bpp: usize,
    source: &[u8],
    palette: &[u8],
    pal_bpp: usize,
    alpha_index: Option<u8>,
) -> Option<()> {
    if !matches!(pal_bpp, 3 | 4) || !matches!(dst_bpp, 3 | 4) {
        return None;
    }
    if dst.len() < source.len().checked_mul(dst_bpp)? {
        return None;
    }
    for (i, &idx) in source.iter().enumerate() {
        let p = (idx as usize).checked_mul(pal_bpp)?;
        if p + pal_bpp > palette.len() {
            return None;
        }
        let o = i * dst_bpp;
        dst[o] = palette[p];
        dst[o + 1] = palette[p + 1];
        dst[o + 2] = palette[p + 2];
        if dst_bpp == 4 {
            dst[o + 3] = match (pal_bpp, alpha_index) {
                // 4-byte palette carries its own alpha.
                (4, _) => palette[p + 3],
                // 3-byte palette: one index is the transparent colour.
                (_, Some(a)) => {
                    if idx == a {
                        0x00
                    } else {
                        0xFF
                    }
                }
                _ => 0xFF,
            };
        }
    }
    Some(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn equality_works_at_all() {
        // In C#, `a == b` overflows the stack and `Equals` always returns false.
        let a = Colorf::new(0.25, 0.5, 0.75, 1.0);
        let b = Colorf::new(0.25, 0.5, 0.75, 1.0);
        assert_eq!(a, b);
        assert_ne!(a, Colorf::new(0.25, 0.6, 0.75, 1.0));
    }

    #[test]
    fn argb32_is_normalised_to_zero_one() {
        // The C# yields 255/255/255/255 here, not 1/1/1/1.
        assert_eq!(Colorf::from_argb32(0xFFFF_FFFF), Colorf::WHITE);
        let c = Colorf::from_argb32(0x8000_0000);
        assert!((c.a - 128.0 / 255.0).abs() < 1e-6);
        assert_eq!((c.r, c.g, c.b), (0.0, 0.0, 0.0));
    }

    #[test]
    fn color32_round_trips_through_colorf() {
        // The C# scales by 0xfff, so 255 came back as 0.062.
        for v in [0u8, 1, 128, 254, 255] {
            let c = Color32::new(v, v, v, v);
            assert_eq!(Color32::from(Colorf::from(c)), c, "value {v}");
        }
        assert_eq!(Color32::from(Colorf::WHITE), Color32::new(255, 255, 255, 255));
    }

    #[test]
    fn indexer_rejects_out_of_range() {
        let c = Colorf::new(1.0, 2.0, 3.0, 4.0);
        assert_eq!(c.get(0), Some(1.0));
        assert_eq!(c.get(3), Some(4.0));
        assert_eq!(c.get(4), None);
    }

    #[test]
    fn lerp_clamps_and_hits_the_endpoints() {
        let a = Colorf::BLACK;
        let b = Colorf::WHITE;
        assert_eq!(Colorf::lerp(a, b, 0.0), a);
        assert_eq!(Colorf::lerp(a, b, 1.0), b);
        assert_eq!(Colorf::lerp(a, b, 5.0), b, "t is clamped");
        assert_eq!(Colorf::lerp_unclamped(a, b, 2.0).r, 2.0);
    }

    #[test]
    fn hsv_round_trips_the_primaries() {
        for c in [Colorf::RED, Colorf::GREEN, Colorf::BLUE, Colorf::WHITE] {
            let (h, s, v) = c.to_hsv();
            let back = Colorf::from_hsv(h, s, v, false);
            assert!(
                (back.r - c.r).abs() < 1e-3
                    && (back.g - c.g).abs() < 1e-3
                    && (back.b - c.b).abs() < 1e-3,
                "{c} -> hsv({h},{s},{v}) -> {back}"
            );
        }
    }

    #[test]
    fn black_has_no_hue_or_saturation() {
        let (h, s, v) = Colorf::BLACK.to_hsv();
        assert_eq!((h, s, v), (0.0, 0.0, 0.0));
    }

    #[test]
    fn color32_lerp_does_not_wrap_on_overshoot() {
        // The C# casts an out-of-range float straight to byte.
        let a = Color32::new(0, 0, 0, 0);
        let b = Color32::new(255, 255, 255, 255);
        assert_eq!(Color32::lerp_unclamped(a, b, 2.0), b);
        assert_eq!(Color32::lerp(a, b, 0.5).r, 127);
    }

    #[test]
    fn palette_blit_expands_indices() {
        let palette = [0u8, 0, 0, 255, 128, 64]; // two RGB entries
        let source = [0u8, 1, 1, 0];
        let mut dst = [0u8; 16];
        blit_by_palette(&mut dst, 4, &source, &palette, 3, None).unwrap();
        assert_eq!(&dst[0..4], &[0, 0, 0, 255]);
        assert_eq!(&dst[4..8], &[255, 128, 64, 255]);
    }

    #[test]
    fn palette_blit_honours_the_transparent_index() {
        let palette = [1u8, 2, 3, 4, 5, 6];
        let source = [0u8, 1];
        let mut dst = [0u8; 8];
        blit_by_palette(&mut dst, 4, &source, &palette, 3, Some(1)).unwrap();
        assert_eq!(dst[3], 0xFF, "index 0 is opaque");
        assert_eq!(dst[7], 0x00, "index 1 is the transparent colour");
    }

    #[test]
    fn palette_blit_rejects_what_the_c_sharp_would_corrupt() {
        let palette = [0u8; 6]; // only two 3-byte entries
        let mut dst = [0u8; 8];
        // Index 200 is far past the end of the palette.
        assert!(blit_by_palette(&mut dst, 4, &[200], &palette, 3, None).is_none());
        // Destination too small for the source.
        assert!(blit_by_palette(&mut dst, 4, &[0; 99], &palette, 3, None).is_none());
        // Unsupported bpp: the C# silently writes nothing at all.
        assert!(blit_by_palette(&mut dst, 2, &[0], &palette, 3, None).is_none());
    }

    #[test]
    fn packed_word_matches_field_order() {
        let c = Color32::new(0x11, 0x22, 0x33, 0x44);
        assert_eq!(c.to_rgba(), 0x4433_2211);
        assert_eq!(Color32::from_rgba(0x4433_2211), c);
    }
}
