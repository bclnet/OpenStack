// PORT-SOURCE: Core/OpenStack.Polyfills/System.Drawing/ColorX.cs
// PORT-SHA: 039d2c5a9a54e84f
// PORT-STATUS: done

/// C# `ColorX.ToRGBA(uint color)` — format an ARGB word for display.
///
/// The alpha component is shown only when it is not fully opaque, matching the
/// C#. Note the trailing space the C# leaves after the alpha value is preserved
/// so log output diffs cleanly between the two trees.
pub fn to_rgba(color: u32) -> String {
    let a = color >> 24;
    let r = (color >> 16) & 0xFF;
    let g = (color >> 8) & 0xFF;
    let b = color & 0xFF;
    if a < 255 {
        format!("R: {r} G: {g} B: {b} A: {a} ")
    } else {
        format!("R: {r} G: {g} B: {b}")
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn opaque_colors_omit_alpha() {
        assert_eq!(to_rgba(0xFF_11_22_33), "R: 17 G: 34 B: 51");
    }

    #[test]
    fn translucent_colors_include_alpha() {
        assert_eq!(to_rgba(0x80_11_22_33), "R: 17 G: 34 B: 51 A: 128 ");
    }

    #[test]
    fn fully_transparent_black_is_handled() {
        assert_eq!(to_rgba(0), "R: 0 G: 0 B: 0 A: 0 ");
    }
}
