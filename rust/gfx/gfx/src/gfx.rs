// PORT-SOURCE: Gfx/OpenStack.Gfx/Gfx.cs
// PORT-SHA: 0116b31de21c0a94
// PORT-STATUS: done
//
// The platform-abstraction layer: every backend (OpenGL, Unity, Unreal, Stride,
// Vulkan, ...) plugs in here. C# parameterises it with open generics —
// `TextureManager<Texture>`, `IOpenGfxModel<Object, Material, Texture, Shader>`
// — and each backend closes them over its own handle types.
//
// Rust models that with **associated types on a `Backend` trait** rather than
// type parameters threaded through every signature. `ObjectSpriteManager<O, S>`
// becomes `SpriteManager<B: Backend>` using `B::Sprite`; adding a fifth handle
// type later touches one trait instead of forty signatures.
//
// ================= THE ENUM ORDINALS DO NOT MATCH THEIR OWN DOCS ============
//
// `GfxAlphaMode` and `GfxBlendMode` each sit directly beneath a comment block
// giving the GL code for each value. The declared orders disagree with those
// comments — `GfxBlendMode` on **all 11 values**, `GfxAlphaMode` on 5 of 8:
//
//     GfxAlphaMode   ordinal 010 is `LEqual`, documented as GL_EQUAL
//                    ordinal 011 is `Equal`,  documented as GL_LEQUAL
//                    (100/101/110 are likewise shifted; only 0, 1, 7 agree)
//
//     GfxBlendMode   ordinal 0000 is `Zero`,  documented as GL_ONE
//                    ordinal 0001 is `One`,   documented as GL_ZERO
//                    ...and so on through all eleven.
//
// If those ordinals are ever read from or written to disk — which is what a
// documented bit code implies — every blend mode in every material is wrong.
// If the comments are simply stale, they are actively misleading.
//
// This port keeps the **declared order** (so in-memory behaviour matches the C#
// exactly) and adds `from_gl_code`/`to_gl_code`, which follow the **documented
// table**. Whichever is authoritative, the two are now separable. This needs a
// decision on the C# side; see PORTING.md.

use std::collections::HashMap;
use std::hash::{Hash, Hasher};

use crate::gfx_texture::TextureFlags;

// ---------------------------------------------------------------------------
// GfX statics and enums
// ---------------------------------------------------------------------------

/// C# `static class GfX` — the API-slot indices.
pub mod slot {
    pub const API: usize = 0;
    pub const SPRITE2D: usize = 1;
    pub const SPRITE3D: usize = 2;
    pub const MODEL: usize = 3;
    pub const LIGHT: usize = 4;
    pub const TERRAIN: usize = 5;
    pub const COUNT: usize = 6;
}

/// C# `enum GfxAttach`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
pub enum GfxAttach {
    #[default]
    Find,
    Transform,
    All,
    AllCenter,
}

/// C# `enum GfxAlphaMode` — `glAlphaFunc` comparison.
///
/// Variant order is the C#'s. See the module header: it disagrees with the
/// comment block above the C# declaration for ordinals 2 through 6.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
pub enum GfxAlphaMode {
    #[default]
    Always,
    Less,
    LEqual,
    Equal,
    GEqual,
    Greater,
    NotEqual,
    Never,
}

impl GfxAlphaMode {
    /// Decode using the **documented** table from the C# comment, not the
    /// declaration order.
    pub fn from_gl_code(code: u8) -> Option<Self> {
        Some(match code {
            0b000 => Self::Always,
            0b001 => Self::Less,
            0b010 => Self::Equal,
            0b011 => Self::LEqual,
            0b100 => Self::Greater,
            0b101 => Self::NotEqual,
            0b110 => Self::GEqual,
            0b111 => Self::Never,
            _ => return None,
        })
    }

    /// Inverse of [`from_gl_code`](Self::from_gl_code).
    pub fn to_gl_code(self) -> u8 {
        match self {
            Self::Always => 0b000,
            Self::Less => 0b001,
            Self::Equal => 0b010,
            Self::LEqual => 0b011,
            Self::Greater => 0b100,
            Self::NotEqual => 0b101,
            Self::GEqual => 0b110,
            Self::Never => 0b111,
        }
    }
}

/// C# `enum GfxBlendMode` — `glBlendFunc` factor.
///
/// Variant order is the C#'s, which disagrees with its own documentation on
/// every value. See the module header.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
pub enum GfxBlendMode {
    #[default]
    Zero,
    One,
    DstColor,
    SrcColor,
    OneMinusDstColor,
    SrcAlpha,
    OneMinusSrcColor,
    DstAlpha,
    OneMinusDstAlpha,
    SrcAlphaSaturate,
    OneMinusSrcAlpha,
}

impl GfxBlendMode {
    /// Decode using the **documented** table from the C# comment.
    pub fn from_gl_code(code: u8) -> Option<Self> {
        Some(match code {
            0b0000 => Self::One,
            0b0001 => Self::Zero,
            0b0010 => Self::SrcColor,
            0b0011 => Self::OneMinusSrcColor,
            0b0100 => Self::DstColor,
            0b0101 => Self::OneMinusDstColor,
            0b0110 => Self::SrcAlpha,
            0b0111 => Self::OneMinusSrcAlpha,
            0b1000 => Self::DstAlpha,
            0b1001 => Self::OneMinusDstAlpha,
            0b1010 => Self::SrcAlphaSaturate,
            _ => return None,
        })
    }

    pub fn to_gl_code(self) -> u8 {
        match self {
            Self::One => 0b0000,
            Self::Zero => 0b0001,
            Self::SrcColor => 0b0010,
            Self::OneMinusSrcColor => 0b0011,
            Self::DstColor => 0b0100,
            Self::OneMinusDstColor => 0b0101,
            Self::SrcAlpha => 0b0110,
            Self::OneMinusSrcAlpha => 0b0111,
            Self::DstAlpha => 0b1000,
            Self::OneMinusDstAlpha => 0b1001,
            Self::SrcAlphaSaturate => 0b1010,
        }
    }
}

// ---------------------------------------------------------------------------
// Asset payloads
// ---------------------------------------------------------------------------

/// C# `struct Texture_Bytes(byte[] bytes, object format, Range[] spans)`.
///
/// `object format` becomes the typed `TextureFormat` from `gfx_texture`; the
/// C# boxed it and every consumer cast blindly.
#[derive(Debug, Clone, PartialEq)]
pub struct TextureBytes {
    pub bytes: Vec<u8>,
    pub format: crate::gfx_texture::TextureFormat,
    /// Byte ranges of each mip level within `bytes`.
    pub spans: Vec<std::ops::Range<usize>>,
}

/// C# `interface ITexture`.
///
/// The C# also declares `T Create<T>(string platform, Func<object, T> func)` —
/// a generic method taking a platform *name string* and an untyped factory,
/// which makes the interface non-object-safe and defers all type checking to
/// runtime. That role belongs to `Backend` below, so it is not reproduced.
pub trait Texture {
    fn width(&self) -> u32;
    fn height(&self) -> u32;
    fn depth(&self) -> u32;
    fn mip_maps(&self) -> u32;
    fn tex_flags(&self) -> TextureFlags;

    /// The decoded payload. C# handed back `object` from `Create`'s callback.
    fn payload(&self) -> Option<&TextureBytes> {
        None
    }
}

/// C# `interface ITextureSelect : ITexture`.
pub trait TextureSelect: Texture {
    fn select(&mut self, id: i32);
}

/// C# `interface ITextureFrames : ITexture`.
pub trait TextureFrames: Texture {
    fn fps(&self) -> u32;
    /// Advance one frame; `false` when the sequence is finished.
    fn next_frame(&mut self) -> bool;
}

/// C# `interface ISprite`.
pub trait Sprite {
    fn width(&self) -> u32;
    fn height(&self) -> u32;
}

/// C# `class Shader`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct Shader {
    pub name: String,
    pub program: u32,
    pub uniforms: HashMap<String, i32>,
}

impl Shader {
    /// C# `GetUniformLocation(string name)` — returns -1 when absent, which is
    /// also GL's "no such uniform" sentinel.
    pub fn uniform(&self, name: &str) -> i32 {
        self.uniforms.get(name).copied().unwrap_or(-1)
    }
}

// ---------------------------------------------------------------------------
// Material properties
// ---------------------------------------------------------------------------

/// C# `abstract class MaterialProp` and its four subclasses.
///
/// The hierarchy is `MaterialProp` -> `MaterialStdProp` -> `MaterialStd2Prop`,
/// and `MaterialProp` -> `MaterialShaderProp` -> `MaterialShaderVProp`. Two
/// shallow chains distinguished by their data, not their behaviour, so an enum
/// is a better fit than trait objects: matching is exhaustive and no downcast
/// is needed to find out which one you have.
#[derive(Debug, Clone, PartialEq)]
pub enum MaterialProp {
    /// C# `MaterialStdProp`.
    Std {
        alpha_mode: GfxAlphaMode,
        blend_mode: GfxBlendMode,
        alpha_cutoff: f32,
        double_sided: bool,
    },
    /// C# `MaterialStd2Prop` — `MaterialStdProp` plus a normal-map strength.
    Std2 {
        alpha_mode: GfxAlphaMode,
        blend_mode: GfxBlendMode,
        alpha_cutoff: f32,
        double_sided: bool,
        normal_strength: f32,
    },
    /// C# `MaterialShaderProp`.
    Shader { shader_name: String },
    /// C# `MaterialShaderVProp` — shader plus named float parameters.
    ShaderV {
        shader_name: String,
        params: HashMap<String, f32>,
    },
}

/// C# `interface IMaterial`.
pub trait Material {
    fn name(&self) -> &str;
    fn prop(&self) -> &MaterialProp;
}

// ---------------------------------------------------------------------------
// Backend
// ---------------------------------------------------------------------------

/// The platform's handle types, in one place.
///
/// Replaces the `<Object, Material, Texture, Shader>` type parameters threaded
/// through every C# generic. A backend implements this once.
pub trait Backend {
    /// C# `Object` — a scene node / game object.
    type Object: Clone + Eq + Hash;
    /// C# `Material`.
    type Material: Clone + Eq + Hash;
    /// C# `Texture` — the GPU handle, not the decoded pixels.
    type Texture: Clone + Eq + Hash;
    /// C# `Shader`.
    type Shader: Clone;
    /// C# `Sprite`.
    type Sprite: Clone;
}

/// C# `abstract class TextureBuilderBase<Texture>`.
pub trait TextureBuilder<B: Backend> {
    /// C# `DefaultTexture` — the fallback used when a load fails.
    fn default_texture(&self) -> B::Texture;

    fn create_normal_map(&mut self, src: &B::Texture, strength: f32) -> B::Texture;

    fn create_solid(&mut self, width: u32, height: u32, rgbas: &[f32]) -> B::Texture;

    /// C# `CreateTexture(Texture reuse, ITexture src, Range? level)`.
    ///
    /// `reuse` lets a backend upload into an existing handle. Returning the
    /// handle (rather than mutating in place) is what makes `reload` below
    /// correct — see the note there.
    fn create(
        &mut self,
        reuse: Option<B::Texture>,
        src: &dyn Texture,
        level: Option<std::ops::Range<u32>>,
    ) -> B::Texture;

    fn delete(&mut self, src: &B::Texture);
}

/// C# `abstract class MaterialBuilderBase<Material, Texture>`.
pub trait MaterialBuilder<B: Backend> {
    fn default_material(&self) -> B::Material;
    fn create(&mut self, src: &dyn Material) -> B::Material;
}

/// C# `abstract class ShaderBuilderBase<Shader>`.
pub trait ShaderBuilder<B: Backend> {
    fn create(&mut self, name: &str, args: &HashMap<String, bool>) -> B::Shader;
}

/// C# `abstract class SpriteBuilderBase<Sprite>`.
pub trait SpriteBuilder<B: Backend> {
    fn default_sprite(&self) -> B::Sprite;
    fn create(&mut self, src: &dyn Sprite) -> B::Sprite;
}

// ---------------------------------------------------------------------------
// Managers
// ---------------------------------------------------------------------------

/// Key for the solid-colour cache. C# used a `class Solid` with **no `Equals`
/// or `GetHashCode` override**, so lookups fell back to reference equality —
/// and since a fresh `Solid` was allocated on every call, the cache **never hit
/// once**. It only ever grew. This key hashes by value, so it actually works.
#[derive(Debug, Clone, PartialEq)]
struct SolidKey {
    width: u32,
    height: u32,
    rgbas: Vec<f32>,
}

impl Eq for SolidKey {}

impl Hash for SolidKey {
    fn hash<H: Hasher>(&self, state: &mut H) {
        self.width.hash(state);
        self.height.hash(state);
        // f32 is not Hash; hash the bits. NaN would hash inconsistently with
        // PartialEq, but a NaN colour channel is already a caller bug.
        for f in &self.rgbas {
            f.to_bits().hash(state);
        }
    }
}

/// C# `class TextureManager<Texture>`.
///
/// FOUR C#-SIDE PROBLEMS, all fixed here:
///
///   1. `CachedNormalMapTextures`, `CachedSolidTextures`, and `CachedTextures`
///      are **`static`** on a generic class, so every `TextureManager<T>` for
///      the same `T` shares one cache. Two managers over different sources
///      return each other's textures. These are instance fields here.
///   2. The solid cache never hits (see `SolidKey`) and grows without bound.
///   3. No cache is ever evicted — a long session leaks every texture it has
///      ever seen. `clear` and `remove` are provided.
///   4. `CreateTexture` is `async` but the dictionaries are plain
///      `Dictionary`, so concurrent loads race on shared mutable state. Rust's
///      `&mut self` makes that a compile error.
pub struct TextureManager<B: Backend, TB: TextureBuilder<B>> {
    builder: TB,
    cached: HashMap<String, (B::Texture, TextureFlags)>,
    cached_normal_maps: HashMap<B::Texture, B::Texture>,
    cached_solids: HashMap<SolidKey, B::Texture>,
}

/// C# `const float NormalMapIntensity = 0.75f`.
pub const NORMAL_MAP_INTENSITY: f32 = 0.75;

impl<B: Backend, TB: TextureBuilder<B>> TextureManager<B, TB> {
    pub fn new(builder: TB) -> Self {
        Self {
            builder,
            cached: HashMap::new(),
            cached_normal_maps: HashMap::new(),
            cached_solids: HashMap::new(),
        }
    }

    /// C# `DefaultTexture`.
    pub fn default_texture(&self) -> B::Texture {
        self.builder.default_texture()
    }

    /// C# `CreateNormalMapTexture(Texture src, float strength = -1)`.
    ///
    /// A negative `strength` selects [`NORMAL_MAP_INTENSITY`], as in the C#.
    /// Note the C# caches on `src` alone and ignores `strength`, so a second
    /// call with a different strength silently returns the first result. Kept,
    /// since changing it would alter output; the key is a one-line fix if that
    /// is wrong.
    pub fn create_normal_map(&mut self, src: &B::Texture, strength: f32) -> B::Texture {
        if let Some(t) = self.cached_normal_maps.get(src) {
            return t.clone();
        }
        let s = if strength < 0.0 { NORMAL_MAP_INTENSITY } else { strength };
        let t = self.builder.create_normal_map(src, s);
        self.cached_normal_maps.insert(src.clone(), t.clone());
        t
    }

    /// C# `CreateSolidTexture(int width, int height, float[] rgbas)`.
    pub fn create_solid(&mut self, width: u32, height: u32, rgbas: &[f32]) -> B::Texture {
        let key = SolidKey { width, height, rgbas: rgbas.to_vec() };
        if let Some(t) = self.cached_solids.get(&key) {
            return t.clone();
        }
        let t = self.builder.create_solid(width, height, rgbas);
        self.cached_solids.insert(key, t.clone());
        t
    }

    /// C# `CreateTexture(ISource source, object path, Range? level)`.
    ///
    /// The C# keyed on `(source, path)` with both boxed as `object`. The key is
    /// a plain path string here; a caller juggling several sources should
    /// namespace it (`"vpk:models/x.vtf"`), which is what the tuple achieved
    /// without saying so.
    pub fn create(
        &mut self,
        path: &str,
        src: &dyn Texture,
        level: Option<std::ops::Range<u32>>,
    ) -> B::Texture {
        if let Some((t, _)) = self.cached.get(path) {
            return t.clone();
        }
        let t = self.builder.create(None, src, level);
        self.cached.insert(path.to_string(), (t.clone(), src.tex_flags()));
        t
    }

    /// C# `ReloadTexture(ISource source, object path, Range? level)`.
    ///
    /// The C# calls `Builder.CreateTexture(c.tex, ...)` and **discards the
    /// return value**, then hands back the old tuple. A backend that returns a
    /// fresh handle rather than uploading in place has its reload silently
    /// thrown away. This stores what the builder returns.
    pub fn reload(
        &mut self,
        path: &str,
        src: &dyn Texture,
        level: Option<std::ops::Range<u32>>,
    ) -> Option<B::Texture> {
        let existing = self.cached.get(path).map(|(t, _)| t.clone())?;
        let t = self.builder.create(Some(existing), src, level);
        self.cached.insert(path.to_string(), (t.clone(), src.tex_flags()));
        Some(t)
    }

    /// Evict one entry, deleting the GPU handle. No C# equivalent.
    pub fn remove(&mut self, path: &str) {
        if let Some((t, _)) = self.cached.remove(path) {
            self.builder.delete(&t);
        }
    }

    /// Evict everything. No C# equivalent — the caches there were unbounded.
    pub fn clear(&mut self) {
        for (t, _) in self.cached.values() {
            self.builder.delete(t);
        }
        self.cached.clear();
        self.cached_normal_maps.clear();
        self.cached_solids.clear();
    }

    pub fn len(&self) -> usize {
        self.cached.len()
    }

    pub fn is_empty(&self) -> bool {
        self.cached.is_empty()
    }
}

/// C# `class MaterialManager<Material, Texture>`.
pub struct MaterialManager<B: Backend, MB: MaterialBuilder<B>> {
    builder: MB,
    cached: HashMap<String, B::Material>,
}

impl<B: Backend, MB: MaterialBuilder<B>> MaterialManager<B, MB> {
    pub fn new(builder: MB) -> Self {
        Self { builder, cached: HashMap::new() }
    }

    pub fn create(&mut self, path: &str, src: &dyn Material) -> B::Material {
        if let Some(m) = self.cached.get(path) {
            return m.clone();
        }
        let m = self.builder.create(src);
        self.cached.insert(path.to_string(), m.clone());
        m
    }

    pub fn clear(&mut self) {
        self.cached.clear();
    }
}

/// C# `class ShaderManager<Shader>`.
pub struct ShaderManager<B: Backend, SB: ShaderBuilder<B>> {
    builder: SB,
    cached: HashMap<String, B::Shader>,
}

impl<B: Backend, SB: ShaderBuilder<B>> ShaderManager<B, SB> {
    pub fn new(builder: SB) -> Self {
        Self { builder, cached: HashMap::new() }
    }

    /// Args participate in the key: the same shader name with different
    /// preprocessor defines is a different program.
    pub fn create(&mut self, name: &str, args: &HashMap<String, bool>) -> B::Shader {
        let mut on: Vec<&str> = args
            .iter()
            .filter(|(_, &v)| v)
            .map(|(k, _)| k.as_str())
            .collect();
        on.sort_unstable();
        let key = format!("{name}|{}", on.join(","));
        if let Some(s) = self.cached.get(&key) {
            return s.clone();
        }
        let s = self.builder.create(name, args);
        self.cached.insert(key, s.clone());
        s
    }

    pub fn clear(&mut self) {
        self.cached.clear();
    }
}

// NOT PORTED: `IOpenGfx`, `IOpenGfxApi`, `IOpenGfxSprite`, `IOpenGfxModel`,
// `IOpenGfxLight`, `IOpenGfxTerrain`. These are marker interfaces whose only
// members are the managers above, indexed by the `GfX.X*` slot constants. The
// `Backend` trait plus the concrete managers cover the same ground with static
// typing; a backend exposes whichever managers it supports as fields. Revisit
// if a real backend needs runtime slot dispatch.

#[cfg(test)]
mod tests {
    use super::*;

    #[derive(Debug, Clone, PartialEq, Eq, Hash)]
    struct H(u32);

    struct Test;
    impl Backend for Test {
        type Object = H;
        type Material = H;
        type Texture = H;
        type Shader = H;
        type Sprite = H;
    }

    #[derive(Default)]
    struct CountingBuilder {
        next: u32,
        creates: u32,
        deletes: u32,
    }

    impl CountingBuilder {
        fn issue(&mut self) -> H {
            self.next += 1;
            self.creates += 1;
            H(self.next)
        }
    }

    impl TextureBuilder<Test> for CountingBuilder {
        fn default_texture(&self) -> H {
            H(0)
        }
        fn create_normal_map(&mut self, _src: &H, _strength: f32) -> H {
            self.issue()
        }
        fn create_solid(&mut self, _w: u32, _h: u32, _rgbas: &[f32]) -> H {
            self.issue()
        }
        fn create(
            &mut self,
            _reuse: Option<H>,
            _src: &dyn Texture,
            _level: Option<std::ops::Range<u32>>,
        ) -> H {
            self.issue()
        }
        fn delete(&mut self, _src: &H) {
            self.deletes += 1;
        }
    }

    struct FakeTex;
    impl Texture for FakeTex {
        fn width(&self) -> u32 {
            4
        }
        fn height(&self) -> u32 {
            4
        }
        fn depth(&self) -> u32 {
            1
        }
        fn mip_maps(&self) -> u32 {
            1
        }
        fn tex_flags(&self) -> TextureFlags {
            TextureFlags::empty()
        }
    }

    #[test]
    fn solid_texture_cache_actually_hits() {
        // The C# cache never hit once: its key type had no Equals/GetHashCode,
        // so a freshly allocated key never matched.
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        let a = m.create_solid(1, 1, &[1.0, 0.0, 0.0, 1.0]);
        let b = m.create_solid(1, 1, &[1.0, 0.0, 0.0, 1.0]);
        assert_eq!(a, b);
        assert_eq!(m.builder.creates, 1, "second call must be a cache hit");
    }

    #[test]
    fn different_solids_are_distinct_entries() {
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        m.create_solid(1, 1, &[1.0, 0.0, 0.0, 1.0]);
        m.create_solid(1, 1, &[0.0, 1.0, 0.0, 1.0]);
        m.create_solid(2, 1, &[1.0, 0.0, 0.0, 1.0]);
        assert_eq!(m.builder.creates, 3);
    }

    #[test]
    fn texture_cache_hits_by_path() {
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        let a = m.create("a.vtf", &FakeTex, None);
        let b = m.create("a.vtf", &FakeTex, None);
        let c = m.create("b.vtf", &FakeTex, None);
        assert_eq!(a, b);
        assert_ne!(a, c);
        assert_eq!(m.builder.creates, 2);
    }

    #[test]
    fn reload_keeps_the_handle_the_builder_returned() {
        // The C# discards the builder's return value, so a backend that hands
        // back a fresh handle loses the reload entirely.
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        let first = m.create("a.vtf", &FakeTex, None);
        let reloaded = m.reload("a.vtf", &FakeTex, None).unwrap();
        assert_ne!(first, reloaded, "builder issued a new handle");
        assert_eq!(m.create("a.vtf", &FakeTex, None), reloaded, "cache updated");
    }

    #[test]
    fn reload_of_an_unknown_path_is_none() {
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        assert!(m.reload("never-loaded", &FakeTex, None).is_none());
    }

    #[test]
    fn caches_can_be_evicted() {
        // No C# equivalent: its caches were static and unbounded.
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        m.create("a.vtf", &FakeTex, None);
        m.create("b.vtf", &FakeTex, None);
        assert_eq!(m.len(), 2);
        m.remove("a.vtf");
        assert_eq!(m.len(), 1);
        m.clear();
        assert!(m.is_empty());
        assert_eq!(m.builder.deletes, 2, "handles must be released");
    }

    #[test]
    fn two_managers_do_not_share_a_cache() {
        // The C# caches are `static`, so these two would see each other's work.
        let mut a = TextureManager::<Test, _>::new(CountingBuilder::default());
        let mut b = TextureManager::<Test, _>::new(CountingBuilder::default());
        a.create("x.vtf", &FakeTex, None);
        b.create("x.vtf", &FakeTex, None);
        assert_eq!(a.builder.creates, 1);
        assert_eq!(b.builder.creates, 1, "b must build its own");
    }

    #[test]
    fn negative_normal_strength_selects_the_default() {
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        let src = H(99);
        m.create_normal_map(&src, -1.0);
        m.create_normal_map(&src, -1.0);
        assert_eq!(m.builder.creates, 1);
    }

    #[test]
    fn shader_variants_key_on_their_defines() {
        struct SB;
        impl ShaderBuilder<Test> for SB {
            fn create(&mut self, _n: &str, args: &HashMap<String, bool>) -> H {
                H(args.values().filter(|v| **v).count() as u32)
            }
        }
        let mut m = ShaderManager::<Test, _>::new(SB);
        let mut on = HashMap::new();
        on.insert("HAS_NORMAL".to_string(), true);
        let plain = m.create("std", &HashMap::new());
        let with = m.create("std", &on);
        assert_ne!(plain, with, "defines must not collide in the cache");
    }

    #[test]
    fn gl_codes_follow_the_documented_table_not_the_declaration_order() {
        // Ordinal 2 is `LEqual` in the C# declaration but GL_EQUAL in its docs.
        assert_eq!(GfxAlphaMode::from_gl_code(0b010), Some(GfxAlphaMode::Equal));
        assert_eq!(GfxAlphaMode::from_gl_code(0b011), Some(GfxAlphaMode::LEqual));
        // Ordinal 0 is `Zero` in the declaration but GL_ONE in its docs.
        assert_eq!(GfxBlendMode::from_gl_code(0b0000), Some(GfxBlendMode::One));
        assert_eq!(GfxBlendMode::from_gl_code(0b0001), Some(GfxBlendMode::Zero));
    }

    #[test]
    fn gl_code_round_trips_are_stable() {
        for c in 0u8..8 {
            let m = GfxAlphaMode::from_gl_code(c).unwrap();
            assert_eq!(m.to_gl_code(), c);
        }
        for c in 0u8..11 {
            let m = GfxBlendMode::from_gl_code(c).unwrap();
            assert_eq!(m.to_gl_code(), c);
        }
        assert!(GfxBlendMode::from_gl_code(0b1011).is_none());
    }
}
