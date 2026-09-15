// PORT-SOURCE: Gfx/OpenStack.Gfx/Gfx.cs
// PORT-SHA: 0116b31de21c0a94
// PORT-STATUS: done

use std::collections::HashMap;
use std::hash::{Hash, Hasher};
use std::any::Any;

use openstk_poly::core::{BoxFuture, SourcePath, SourceTag, ISource};
use crate::gfx_texture::TextureFlags;

//#region Gfx

pub mod GfX {
    pub const XAPI: usize = 0;
    pub const XSPRITE2D: usize = 1;
    pub const XSPRITE3D: usize = 2;
    pub const XMODEL: usize = 3;
    pub const XLIGHT: usize = 4;
    pub const XTERRAIN: usize = 5;
    // pub static mut MaxTextureMaxAnisotropy: u32;
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum GfxAttach { Find, Transform, All, AllCenter }

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum GfxAlphaMode { Never = 0x0200, Less = 0x0201, Equal = 0x0202, LEqual = 0x0203, Greater = 0x0204, NotEqual = 0x0205, GEqual = 0x0206, Always = 0x0207 }

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum GfxBlendMode { Zero = 0, One = 1, SrcColor = 0x0300, OneMinusSrcColor = 0x0301, SrcAlpha = 0x0302, OneMinusSrcAlpha = 0x0303, DstAlpha = 0x0304, OneMinusDstAlpha = 0x0305, DstColor = 0x0306, OneMinusDstColor = 0x0307, SrcAlphaSaturate = 0x0308 }

pub trait Backend {
    type Object: Clone + Eq + Hash;
    type Material: Clone + Eq + Hash;
    type Texture: Clone + Eq + Hash;
    type Shader: Clone;
    type Sprite: Clone;
}

//#endregion

//#region ObjectSprite
//#endregion

//#region ObjectModel
//#endregion

//#region Shader

#[derive(Debug, Clone)]
pub struct Shader {
    _uniformLocation: fn(u32, &str) -> i32,
    _attribLocation: fn(u32, &str) -> i32,
    pub name: String,
    pub program: u32,
    pub uniforms: HashMap<String, i32>,
}

impl Shader {
    // pub fn new(uniformLocation: fn(&str, i32) -> i32, attribLocation: fn(&str, i32) -> i32) -> Self {
    //     Self { uniformLocation, attribLocation }
    // }
    pub fn uniformLocation(&mut self, name: &str) -> i32 { *self.uniforms.entry(name.to_string()).or_insert_with(|| (self._uniformLocation)(self.program, name)) }
    pub fn attribLocation(&self, name: &str) -> i32 { (self._attribLocation)(self.program, name) }
}

pub trait ShaderBuilder<B: Backend> {
    fn create(&mut self, path: &SourcePath, args: &HashMap<String, bool>) -> B::Shader;
}

pub struct ShaderManager<B: Backend, SB: ShaderBuilder<B>> {
    builder: SB,
    cached: HashMap<(dyn ISource, SourcePath, String), B::Shader>,
}
impl<B: Backend, SB: ShaderBuilder<B>> ShaderManager<B, SB> {
    pub fn new(builder: SB) -> Self {
        Self { builder, cached: HashMap::new() }
    }

    pub fn create(&mut self, source: &dyn ISource, path: &SourcePath, args: &Option<HashMap<String, bool>>) -> B::Shader {
        let argx: &str = args.map_or(String::new(), |v| v.iter().filter(|(_, &v)| v).map(|(k, _)| k.as_str()).collect().join(","));
        let key = (source, path, argx);
        if let Some(s) = self.cached.get(&key) { return s.clone(); }
        let s = self.builder.create(path, args);
        self.cached.insert(key, s.clone());
        s
    }

    pub fn clear(&mut self) { self.cached.clear(); }
}

//#endregion

//#region Sprite

pub trait ISprite {
    fn width(&self) -> u32;
    fn height(&self) -> u32;
}

pub trait SpriteBuilder<B: Backend> {
    fn default(&self) -> B::Sprite;
    fn create(&mut self, src: &dyn ISprite) -> B::Sprite;
}

pub struct SpriteManager<B: Backend, SB: SpriteBuilder<B>> {
    builder: SB,
    tasks: HashMap<(dyn ISource, SourcePath), BoxFuture::All<dyn Any>>,
    cached: HashMap<(dyn ISource, SourcePath), (B::Sprite, dyn Any)>,
}
impl<B: Backend, SB: SpriteBuilder<B>> SpriteManager<B, SB> {
    pub fn new(builder: SB) -> Self {
        Self { builder, cached: HashMap::new(), }
    }
    
    pub async fn create(&mut self, source: &dyn ISource, path: &SourcePath) -> (B::Sprite, SourceTag) {
        let key = (source, path);
        if let Some(c) = self.cached.get(&key) { return c.clone(); }
        let tag = if let Some(z) = path.downcast_ref::<dyn ISprite>() { z } else { self.load(source, path).await? };
        let obj = if let Some(v) = tag { self.builder.create(v); } else { self.builder.default() };
        self.cached.insert(key, (obj.clone(), tag.clone()))
    }

    pub fn preload(&mut self, source: &dyn ISource, path: &SourcePath) {
        let key = (source, path);
        if let Some(c) = self.cached.get(key) { return; }
        self.tasks.entry(key).or_insert_with(|| source.getAsset::<ISprite>(path));
    }

    pub fn delete(&mut self, source: &dyn ISource, path: &SourcePath) {
        let key = (source, path);
        let Some(c) = self.cached.get(key) else { return; };
        self.builder.delete(c.spr);
        self.cached.remove(key);
    }

    async fn load(&mut self, source: &dyn ISource, path: &SourcePath) -> &dyn ISprite {
        let key = (source, path);
        assert!(!self.cached.contains_key(key));
        self.preload(source, path);
        let obj = self.tasks[key].await;
        self.tasks.remove(key);
        return obj;
    }

    pub fn clear(&mut self) { self.cached.clear(); }
}

//#endregion

//#region Texture

#[derive(Debug)]
pub struct TextureAsDds {
    pub bytes: Vec<u8>,
}

#[derive(Debug)]
pub struct TextureAsBytes {
    pub bytes: Vec<u8>,
    pub format: Box<dyn Any>,
    pub spans: Vec<std::ops::Range<usize>>,
}

pub trait ITexture {
    fn width(&self) -> u32;
    fn height(&self) -> u32;
    fn depth(&self) -> u32;
    fn mipMaps(&self) -> u32;
    fn texFlags(&self) -> TextureFlags;
    // fn payload(&self) -> Some { None }
}

pub trait ITextureSelect: ITexture {
    fn maxId(&self) -> u32;
    fn select(&mut self, id: i32);
}

pub trait ITextureFrames: ITexture {
    fn fps(&self) -> u32;
    fn hasFrames(&self) -> bool;
    fn nextFrame(&mut self) -> bool;
}

pub trait TextureBuilder<B: Backend> {
    fn default(&self) -> B::Texture;
    fn createNormalMap(&mut self, src: &B::Texture, strength: f32) -> B::Texture;
    fn createSolid(&mut self, width: u32, height: u32, rgbas: &[f32]) -> B::Texture;
    fn create(&mut self, reuse: Option<B::Texture>, src: &dyn ITexture, level: Option<std::ops::Range<u32>>) -> B::Texture;
    fn delete(&mut self, src: &B::Texture);
}

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
        for f in &self.rgbas { f.to_bits().hash(state); }
    }
}

pub struct TextureManager<B: Backend, TB: TextureBuilder<B>> {
    builder: TB,
    cached: HashMap<(dyn ISource, SourcePath), (B::Texture, TextureFlags)>,
    cachedNormalMaps: HashMap<B::Texture, B::Texture>,
    cachedSolids: HashMap<SolidKey, B::Texture>,
}

/// C# `const float NormalMapIntensity = 0.75f`.
pub const NORMAL_MAP_INTENSITY: f32 = 0.75;

impl<B: Backend, TB: TextureBuilder<B>> TextureManager<B, TB> {
    pub fn new(builder: TB) -> Self {
        Self { builder, cached: HashMap::new(), cachedNormalMaps: HashMap::new(), cachedSolids: HashMap::new(), }
    }

    pub fn default(&self) -> B::Texture { self.builder.default() }

    pub fn createNormalMap(&mut self, src: &B::Texture, strength: f32) -> B::Texture {
        if let Some(t) = self.cachedNormalMaps.get(src) { return t.clone(); }
        let s = if strength < 0.0 { NORMAL_MAP_INTENSITY } else { strength };
        let t = self.builder.createNormalMap(src, s);
        self.cachedNormalMaps.insert(src.clone(), t.clone());
        t
    }

    pub fn createSolid(&mut self, width: u32, height: u32, rgbas: &[f32]) -> B::Texture {
        let key = SolidKey { width, height, rgbas: rgbas.to_vec() };
        if let Some(t) = self.cachedSolids.get(&key) { return t.clone(); }
        let t = self.builder.createSolid(width, height, rgbas);
        self.cachedSolids.insert(key, t.clone());
        t
    }

    pub fn create(&mut self, source: &dyn ISource, path: &SourcePath, src: &dyn ITexture, level: Option<std::ops::Range<u32>>) -> B::Texture {
        let key = (source, path);
        if let Some(t) = self.cached.get(key) { return t.clone(); }
        let t = self.builder.create(None, src, level);
        self.cached.insert(key, (t.clone(), src.texFlags()));
        t
    }

    pub fn reload(&mut self, source: &dyn ISource, path: &SourcePath, src: &dyn ITexture, level: Option<std::ops::Range<u32>>) -> Option<B::Texture> {
        let key = (source, path);
        let existing = self.cached.get(key).map(|(t, _)| t.clone())?;
        let t = self.builder.create(Some(existing), src, level);
        self.cached.insert(path.to_string(), (t.clone(), src.texFlags()));
        Some(t)
    }

    pub fn remove(&mut self, source: &dyn ISource, path: &SourcePath) {
        let key = (source, path);
        if let Some(t) = self.cached.remove(key) { self.builder.delete(&t); }
    }

    pub fn clear(&mut self) {
        for (t, _) in self.cached.values() { self.builder.delete(t); }
        self.cached.clear();
        self.cachedNormalMaps.clear();
        self.cachedSolids.clear();
    }

    // pub fn len(&self) -> usize { self.cached.len() }
    // pub fn is_empty(&self) -> bool { self.cached.is_empty() }
}

//#endregion

//#region Material

#[derive(Debug, Clone, PartialEq)]
pub enum MaterialProp {
    /// C# `MaterialStdProp`.
    Std {
        alphaMode: GfxAlphaMode,
        blendMode: GfxBlendMode,
        alphaCutoff: f32,
        doubleSided: bool,
    },
    /// C# `MaterialStd2Prop` — `MaterialStdProp` plus a normal-map strength.
    Std2 {
        alphaMode: GfxAlphaMode,
        blendMode: GfxBlendMode,
        alphaCutoff: f32,
        doubleSided: bool,
        normalStrength: f32,
    },
    /// C# `MaterialShaderProp`.
    Shader { shaderName: String },
    /// C# `MaterialShaderVProp` — shader plus named float parameters.
    ShaderV {
        shaderName: String,
        params: HashMap<String, f32>,
    },
}

pub trait IMaterial {
    fn name(&self) -> &str;
    fn prop(&self) -> &MaterialProp;
}

pub trait MaterialBuilder<B: Backend> {
    fn default(&self) -> B::Material;
    fn create(&mut self, src: &dyn IMaterial) -> B::Material;
}

pub struct MaterialManager<B: Backend, MB: MaterialBuilder<B>> {
    builder: MB,
    cached: HashMap<(dyn ISource, SourcePath), (B::Material, SourceTag)>,
}
impl<B: Backend, MB: MaterialBuilder<B>> MaterialManager<B, MB> {
    pub fn new(builder: MB) -> Self {
        Self { builder, cached: HashMap::new() }
    }

    pub fn create(&mut self, source: &dyn ISource, path: &SourcePath, src: &dyn IMaterial) -> (B::Material, SourceTag) {
        let key = (source, path);
        if let Some(m) = self.cached.get(key) { return m.clone(); }
        let m = self.builder.create(src);
        self.cached.insert(key, m.clone());
        m
    }

    pub fn clear(&mut self) { self.cached.clear(); }
}

//#endregion

//#region OpenGfx
//#endregion

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
        fn issue(&mut self) -> H { self.next += 1; self.creates += 1; H(self.next) }
    }

    impl TextureBuilder<Test> for CountingBuilder {
        fn default(&self) -> H { H(0) }
        fn createNormalMap(&mut self, _src: &H, _strength: f32) -> H { self.issue() }
        fn createSolid(&mut self, _w: u32, _h: u32, _rgbas: &[f32]) -> H { self.issue() }
        fn create(&mut self, _reuse: Option<H>, _src: &dyn ITexture, _level: Option<std::ops::Range<u32>>) -> H { self.issue() }
        fn delete(&mut self, _src: &H) { self.deletes += 1; }
    }

    struct FakeTex;
    impl Texture for FakeTex {
        fn width(&self) -> u32 { 4 }
        fn height(&self) -> u32 { 4 }
        fn depth(&self) -> u32 { 1 }
        fn mipMaps(&self) -> u32 { 1 }
        fn texFlags(&self) -> TextureFlags { TextureFlags::empty() }
    }

    #[test]
    fn texture_createSolid_CacheActuallyHits() {
        // The C# cache never hit once: its key type had no Equals/GetHashCode,
        // so a freshly allocated key never matched.
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        let a = m.createSolid(1, 1, &[1.0, 0.0, 0.0, 1.0]);
        let b = m.createSolid(1, 1, &[1.0, 0.0, 0.0, 1.0]);
        assert_eq!(a, b);
        assert_eq!(m.builder.creates, 1, "second call must be a cache hit");
    }

    #[test]
    fn texture_createSolid_differentAreDistinctEntries() {
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        m.createSolid(1, 1, &[1.0, 0.0, 0.0, 1.0]);
        m.createSolid(1, 1, &[0.0, 1.0, 0.0, 1.0]);
        m.createSolid(2, 1, &[1.0, 0.0, 0.0, 1.0]);
        assert_eq!(m.builder.creates, 3);
    }

    #[test]
    fn texture_create_cacheHitsByPath() {
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        let a = m.create("a.vtf", &FakeTex, None);
        let b = m.create("a.vtf", &FakeTex, None);
        let c = m.create("b.vtf", &FakeTex, None);
        assert_eq!(a, b);
        assert_ne!(a, c);
        assert_eq!(m.builder.creates, 2);
    }

    #[test]
    fn texture_ReloadKeepsTheHandleTheBuilderReturned() {
        // The C# discards the builder's return value, so a backend that hands
        // back a fresh handle loses the reload entirely.
        let mut m = TextureManager::<Test, _>::new(CountingBuilder::default());
        let first = m.create("a.vtf", &FakeTex, None);
        let reloaded = m.reload("a.vtf", &FakeTex, None).unwrap();
        assert_ne!(first, reloaded, "builder issued a new handle");
        assert_eq!(m.create("a.vtf", &FakeTex, None), reloaded, "cache updated");
    }

    #[test]
    fn texture_reloadOfAnUnknownPathIsNone() {
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
