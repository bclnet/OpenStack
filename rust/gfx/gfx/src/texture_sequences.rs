// PORT-SOURCE: Gfx/OpenStack.Gfx/TextureSequences.cs
// PORT-SHA: d9c548630b73acb1
// PORT-STATUS: done
//
// Sprite-sheet animation metadata: sequences of frames, each frame holding
// images with normalised cropped/uncropped UV bounds.
//
// C# `class TextureSequences : List<Sequence>` inherits from `List<T>` to get
// collection syntax. Rust models has-a rather than is-a, so this wraps a `Vec`
// and derefs to a slice — same ergonomics, without the fragile-base-class
// problem inheriting from `List<T>` brings.
//
// 27 live lines against 79 commented; the commented half is an older
// `GetFrame`/`GetSequence` lookup API.

use glam::{IVec4, Vec2};
use std::collections::HashMap;

/// C# `TextureSequences.Image`.
#[derive(Debug, Clone, Copy, Default, PartialEq)]
pub struct Image {
    pub cropped_min: Vec2,
    pub cropped_max: Vec2,
    pub uncropped_min: Vec2,
    pub uncropped_max: Vec2,
}

impl Image {
    /// C# `GetCroppedRect(int width, int height)` — returns
    /// `Vector4<int>(minX, minY, maxX, maxY)`.
    ///
    /// This (with `GetUncroppedRect`) is one of only four real instantiations
    /// of the generic `Vector4<T>` in the whole solution; it maps to `IVec4`.
    ///
    /// Note the components are corners, not offset+size — the name "Rect"
    /// suggests otherwise and is worth keeping in mind at call sites.
    #[inline]
    pub fn cropped_rect(&self, width: i32, height: i32) -> IVec4 {
        rect(self.cropped_min, self.cropped_max, width, height)
    }

    /// C# `GetUncroppedRect(int width, int height)`.
    #[inline]
    pub fn uncropped_rect(&self, width: i32, height: i32) -> IVec4 {
        rect(self.uncropped_min, self.uncropped_max, width, height)
    }
}

/// C# truncates toward zero via `(int)` casts; `as i32` does the same.
#[inline]
fn rect(min: Vec2, max: Vec2, width: i32, height: i32) -> IVec4 {
    IVec4::new(
        (min.x * width as f32) as i32,
        (min.y * height as f32) as i32,
        (max.x * width as f32) as i32,
        (max.y * height as f32) as i32,
    )
}

/// C# `TextureSequences.Frame`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct Frame {
    pub images: Vec<Image>,
    pub display_time: f32,
}

/// C# `TextureSequences.Sequence`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct Sequence {
    pub frames: Vec<Frame>,
    pub frames_per_second: f32,
    pub name: String,
    pub clamp: bool,
    pub alpha_crop: bool,
    pub no_color: bool,
    pub no_alpha: bool,
    pub float_params: HashMap<String, f32>,
}

impl Sequence {
    /// Total run time. Uses each frame's `display_time` when set, falling back
    /// to `frames_per_second`. Not in the C#, which had no duration accessor.
    pub fn duration(&self) -> f32 {
        let explicit: f32 = self.frames.iter().map(|f| f.display_time).sum();
        if explicit > 0.0 {
            return explicit;
        }
        if self.frames_per_second > 0.0 {
            return self.frames.len() as f32 / self.frames_per_second;
        }
        0.0
    }
}

/// C# `class TextureSequences : List<Sequence>`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct TextureSequences {
    sequences: Vec<Sequence>,
}

impl TextureSequences {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn push(&mut self, s: Sequence) {
        self.sequences.push(s);
    }

    /// Look a sequence up by name — the common operation, and what the
    /// commented-out C# API was reaching for.
    pub fn by_name(&self, name: &str) -> Option<&Sequence> {
        self.sequences.iter().find(|s| s.name == name)
    }
}

impl std::ops::Deref for TextureSequences {
    type Target = [Sequence];
    fn deref(&self) -> &[Sequence] {
        &self.sequences
    }
}

impl FromIterator<Sequence> for TextureSequences {
    fn from_iter<I: IntoIterator<Item = Sequence>>(iter: I) -> Self {
        Self { sequences: iter.into_iter().collect() }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn rects_scale_normalised_bounds_to_pixels() {
        let img = Image {
            cropped_min: Vec2::new(0.0, 0.0),
            cropped_max: Vec2::new(0.5, 0.25),
            ..Default::default()
        };
        assert_eq!(img.cropped_rect(100, 80), IVec4::new(0, 0, 50, 20));
    }

    #[test]
    fn rect_components_are_corners_not_offset_and_size() {
        let img = Image {
            cropped_min: Vec2::new(0.25, 0.25),
            cropped_max: Vec2::new(0.75, 0.75),
            ..Default::default()
        };
        let r = img.cropped_rect(100, 100);
        assert_eq!(r, IVec4::new(25, 25, 75, 75));
    }

    #[test]
    fn fractional_pixels_truncate_like_the_c_sharp_cast() {
        let img = Image {
            cropped_min: Vec2::ZERO,
            cropped_max: Vec2::new(0.999, 0.999),
            ..Default::default()
        };
        assert_eq!(img.cropped_rect(10, 10), IVec4::new(0, 0, 9, 9));
    }

    #[test]
    fn duration_prefers_explicit_frame_times() {
        let s = Sequence {
            frames: vec![
                Frame { display_time: 0.5, ..Default::default() },
                Frame { display_time: 0.25, ..Default::default() },
            ],
            frames_per_second: 60.0,
            ..Default::default()
        };
        assert_eq!(s.duration(), 0.75);
    }

    #[test]
    fn duration_falls_back_to_fps_then_to_zero() {
        let s = Sequence {
            frames: vec![Frame::default(); 30],
            frames_per_second: 15.0,
            ..Default::default()
        };
        assert_eq!(s.duration(), 2.0);
        assert_eq!(Sequence::default().duration(), 0.0, "no frames, no fps");
    }

    #[test]
    fn lookup_by_name_and_slice_access() {
        let mut t = TextureSequences::new();
        t.push(Sequence { name: "idle".into(), ..Default::default() });
        t.push(Sequence { name: "walk".into(), ..Default::default() });
        assert_eq!(t.len(), 2, "derefs to a slice");
        assert!(t.by_name("walk").is_some());
        assert!(t.by_name("run").is_none());
    }
}
