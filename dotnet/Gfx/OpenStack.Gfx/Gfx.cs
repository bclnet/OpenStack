using OpenStack.Gfx.Texture;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static OpenStack.Debug;

[assembly: InternalsVisibleTo("OpenStack.Gl")]
[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Gfx;

#region Extensions

/// <summary>
/// GfxExtensions
/// </summary>
public static class GfxExtensions
{
}

/*
test modes (glAlphaFunc):
000 GL_ALWAYS
001 GL_LESS
010 GL_EQUAL
011 GL_LEQUAL
100 GL_GREATER
101 GL_NOTEQUAL
110 GL_GEQUAL
111 GL_NEVER
*/
/// <summary>
/// GfxAlphaMode
/// </summary>
public enum GfxAlphaMode
{
    Always,
    Less,
    LEqual,
    Equal,
    GEqual,
    Greater,
    NotEqual,
    Never
}

/*
blend modes (glBlendFunc):
0000 GL_ONE
0001 GL_ZERO
0010 GL_SRC_COLOR
0011 GL_ONE_MINUS_SRC_COLOR
0100 GL_DST_COLOR
0101 GL_ONE_MINUS_DST_COLOR
0110 GL_SRC_ALPHA
0111 GL_ONE_MINUS_SRC_ALPHA
1000 GL_DST_ALPHA
1001 GL_ONE_MINUS_DST_ALPHA
1010 GL_SRC_ALPHA_SATURATE
*/
/// <summary>
/// GfxBlendMode
/// </summary>
public enum GfxBlendMode
{
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
    OneMinusSrcAlpha
}

#endregion

#region ObjectSprite

/// <summary>
/// ObjectSpriteBuilderBase
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Sprite"></typeparam>
public abstract class ObjectSpriteBuilderBase<Object, Sprite>
{
    public abstract void EnsurePrefab();
    public abstract Object CreateNewObject(Object prefab);
    public abstract Object CreateObject(object source);
}

/// <summary>
/// IObjectSpriteManager
/// </summary>
public interface IObjectSpriteManager<Object, Sprite>
{
    (Object obj, object tag) CreateObject(object path);
    void PreloadObject(object path);
}

/// <summary>
/// ObjectSpriteManager
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
/// <param name="source"></param>
/// <param name="materialManager"></param>
/// <param name="builder"></param>
public class ObjectSpriteManager<Object, Sprite>(ISource source, ObjectSpriteBuilderBase<Object, Sprite> builder) : IObjectSpriteManager<Object, Sprite>
{
    readonly ISource Source = source;
    readonly ObjectSpriteBuilderBase<Object, Sprite> Builder = builder;
    readonly Dictionary<object, (Object obj, object tag)> CachedObjects = [];
    readonly Dictionary<object, Task<object>> PreloadTasks = [];

    public (Object obj, object tag) CreateObject(object path)
    {
        Builder.EnsurePrefab();
        // load & cache the prefab.
        if (!CachedObjects.TryGetValue(path, out var prefab)) prefab = CachedObjects[path] = LoadObject(path).Result;
        return (Builder.CreateNewObject(prefab.obj), prefab.tag);
    }

    public void PreloadObject(object path)
    {
        if (CachedObjects.ContainsKey(path)) return;
        // start loading the object asynchronously if we haven't already started.
        if (!PreloadTasks.ContainsKey(path)) PreloadTasks[path] = Source.LoadFileObject<object>(path);
    }

    async Task<(Object obj, object tag)> LoadObject(object path)
    {
        Assert(!CachedObjects.ContainsKey(path));
        PreloadObject(path);
        var source = await PreloadTasks[path];
        PreloadTasks.Remove(path);
        return (Builder.CreateObject(source), source);
    }
}

#endregion

#region ObjectModel

/// <summary>
/// ObjectModelBuilderBase
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
public abstract class ObjectModelBuilderBase<Object, Material, Texture>
{
    public abstract void EnsurePrefab();
    public abstract Object CreateNewObject(Object prefab);
    public abstract Object CreateObject(object source, IMaterialManager<Material, Texture> materialManager);
}

/// <summary>
/// IObjectModelManager
/// </summary>
public interface IObjectModelManager<Object, Material, Texture>
{
    (Object obj, object tag) CreateObject(object path);
    void PreloadObject(object path);
}

/// <summary>
/// ObjectModelManager
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
/// <param name="source"></param>
/// <param name="materialManager"></param>
/// <param name="builder"></param>
public class ObjectModelManager<Object, Material, Texture>(ISource source, IMaterialManager<Material, Texture> materialManager, ObjectModelBuilderBase<Object, Material, Texture> builder) : IObjectModelManager<Object, Material, Texture>
{
    readonly ISource Source = source;
    readonly IMaterialManager<Material, Texture> MaterialManager = materialManager;
    readonly ObjectModelBuilderBase<Object, Material, Texture> Builder = builder;
    readonly Dictionary<object, (Object obj, object tag)> CachedObjects = [];
    readonly Dictionary<object, Task<object>> PreloadTasks = [];

    public (Object obj, object tag) CreateObject(object path)
    {
        Builder.EnsurePrefab();
        // load & cache the prefab.
        if (!CachedObjects.TryGetValue(path, out var prefab)) prefab = CachedObjects[path] = LoadObject(path).Result;
        return (Builder.CreateNewObject(prefab.obj), prefab.tag);
    }

    public void PreloadObject(object path)
    {
        if (CachedObjects.ContainsKey(path)) return;
        // start loading the object asynchronously if we haven't already started.
        if (!PreloadTasks.ContainsKey(path)) PreloadTasks[path] = Source.LoadFileObject<object>(path);
    }

    async Task<(Object obj, object tag)> LoadObject(object path)
    {
        Assert(!CachedObjects.ContainsKey(path));
        PreloadObject(path);
        var source = await PreloadTasks[path];
        PreloadTasks.Remove(path);
        return (Builder.CreateObject(source, MaterialManager), source);
    }
}

#endregion

#region Shader

/// <summary>
/// Shader
/// </summary>
public class Shader(Func<int, string, int> getUniformLocation, Func<int, string, int> getAttribLocation)
{
    readonly Func<int, string, int> _getUniformLocation = getUniformLocation ?? throw new ArgumentNullException(nameof(getUniformLocation));
    readonly Func<int, string, int> _getAttribLocation = getAttribLocation ?? throw new ArgumentNullException(nameof(getAttribLocation));
    Dictionary<string, int> _uniforms = [];
    public string Name;
    public int Program;
    public IDictionary<string, bool> Parameters;
    public List<string> RenderModes;

    public int GetUniformLocation(string name)
    {
        if (_uniforms.TryGetValue(name, out var value)) return value;
        value = _getUniformLocation(Program, name); _uniforms[name] = value;
        return value;
    }

    public int GetAttribLocation(string name) => _getAttribLocation(Program, name);
}

/// <summary>
/// ShaderBuilderBase
/// </summary>
/// <typeparam name="Shader"></typeparam>
public abstract class ShaderBuilderBase<Shader>
{
    public abstract Shader CreateShader(object path, IDictionary<string, bool> args = null);
}

/// <summary>
/// IShaderManager
/// </summary>
public interface IShaderManager<Shader>
{
    public (Shader sha, object tag) CreateShader(object path, IDictionary<string, bool> args = null);
}

/// <summary>
/// ShaderManager
/// </summary>
/// <typeparam name="Shader"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class ShaderManager<Shader>(ISource source, ShaderBuilderBase<Shader> builder) : IShaderManager<Shader>
{
    static readonly Dictionary<string, bool> EmptyArgs = [];
    readonly ISource Source = source;
    readonly ShaderBuilderBase<Shader> Builder = builder;

    public (Shader sha, object tag) CreateShader(object path, IDictionary<string, bool> args = null)
        => (Builder.CreateShader(path, args ?? EmptyArgs), null);
}

#endregion

#region Sprite

/// <summary>
/// ISprite
/// </summary>
public interface ISprite
{
    int Width { get; }
    int Height { get; }
    //TextureFlags TexFlags { get; }
    (byte[] bytes, object format) Begin(string platform);
    void End();
}

/// <summary>
/// SpriteBuilderBase
/// </summary>
/// <typeparam name="Sprite"></typeparam>
public abstract class SpriteBuilderBase<Sprite>
{
    public abstract Sprite DefaultSprite { get; }
    public abstract Sprite CreateSprite(ISprite source);
    public abstract void DeleteSprite(Sprite sprite);
}

/// <summary>
/// ISpriteManager
/// </summary>
public interface ISpriteManager<Sprite>
{
    Sprite DefaultSprite { get; }
    (Sprite spr, object tag) CreateSprite(object path);
    void PreloadSprite(object path);
    void DeleteSprite(object path);
}

/// <summary>
/// SpriteManager
/// </summary>
/// <typeparam name="Texture"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class SpriteManager<Sprite>(ISource source, SpriteBuilderBase<Sprite> builder) : ISpriteManager<Sprite>
{
    readonly ISource Source = source;
    readonly SpriteBuilderBase<Sprite> Builder = builder;
    readonly Dictionary<object, (Sprite spr, object tag)> CachedSprites = [];
    readonly Dictionary<object, Task<ISprite>> PreloadTasks = [];

    public Sprite DefaultSprite => Builder.DefaultSprite;

    public (Sprite spr, object tag) CreateSprite(object path)
    {
        if (CachedSprites.TryGetValue(path, out var c)) return c;
        // load & cache the sprite.
        var tag = path is ISprite z ? z : LoadSprite(path).Result;
        var sprite = tag != null ? Builder.CreateSprite(tag) : Builder.DefaultSprite;
        CachedSprites[path] = (sprite, tag);
        return (sprite, tag);
    }

    public void PreloadSprite(object path)
    {
        if (CachedSprites.ContainsKey(path)) return;
        // start loading the texture file asynchronously if we haven't already started.
        if (!PreloadTasks.ContainsKey(path)) PreloadTasks[path] = Source.LoadFileObject<ISprite>(path);
    }

    public void DeleteSprite(object path)
    {
        if (!CachedSprites.TryGetValue(path, out var c)) return;
        Builder.DeleteSprite(c.spr);
        CachedSprites.Remove(path);
    }

    async Task<ISprite> LoadSprite(object path)
    {
        Assert(!CachedSprites.ContainsKey(path));
        PreloadSprite(path);
        var source = await PreloadTasks[path];
        PreloadTasks.Remove(path);
        return source;
    }
}

#endregion

#region Texture

/// <summary>
/// Texture_Bytes
/// </summary>
public struct Texture_Bytes(byte[] bytes, object format, Range[] spans)
{
    public byte[] Bytes = bytes;
    public object Format = format;
    public Range[] Spans = spans;
}

/// <summary>
/// ITexture
/// </summary>
public interface ITexture
{
    int Width { get; }
    int Height { get; }
    int Depth { get; }
    int MipMaps { get; }
    TextureFlags TexFlags { get; }
    T Create<T>(string platform, Func<object, T> func);
}

/// <summary>
/// ITexture
/// </summary>
public interface ITextureSelect : ITexture
{
    void Select(int id);
}

/// <summary>
/// ITextureFrames
/// </summary>
public interface ITextureFrames : ITexture
{
    int Fps { get; }
    bool HasFrames { get; }
    bool DecodeFrame();
}

/// <summary>
/// ITextureFramesSelect
/// </summary>
public interface ITextureFramesSelect : ITextureFrames
{
    int FrameMax { get; }
    void FrameSelect(int id);
}

/// <summary>
/// TextureBuilderBase
/// </summary>
/// <typeparam name="Texture"></typeparam>
public abstract class TextureBuilderBase<Texture>
{
    public static int MaxTextureMaxAnisotropy
    {
        get => GfxStats.MaxTextureMaxAnisotropy;
        set => GfxStats.MaxTextureMaxAnisotropy = value;
    }

    public abstract Texture DefaultTexture { get; }
    public abstract Texture CreateTexture(Texture reuse, ITexture source, Range? level = null);
    public abstract Texture CreateSolidTexture(int width, int height, float[] rgba);
    public abstract Texture CreateNormalMap(Texture texture, float strength);
    public abstract void DeleteTexture(Texture texture);
}

/// <summary>
/// ITextureManager
/// </summary>
public interface ITextureManager<Texture>
{
    Texture DefaultTexture { get; }
    Texture CreateNormalMap(Texture texture, float strength);
    Texture CreateSolidTexture(int width, int height, params float[] rgba);
    (Texture tex, object tag) CreateTexture(object path, Range? level = null);
    (Texture tex, object tag) ReloadTexture(object path, Range? level = null);
    void PreloadTexture(object path);
    void DeleteTexture(object path);
}

/// <summary>
/// TextureManager
/// </summary>
/// <typeparam name="Texture"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class TextureManager<Texture>(ISource source, TextureBuilderBase<Texture> builder) : ITextureManager<Texture>
{
    readonly ISource Source = source;
    readonly TextureBuilderBase<Texture> Builder = builder;
    readonly Dictionary<object, (Texture tex, object tag)> CachedTextures = [];
    readonly Dictionary<object, Task<ITexture>> PreloadTasks = [];

    public Texture CreateSolidTexture(int width, int height, params float[] rgba) => Builder.CreateSolidTexture(width, height, rgba);

    public Texture CreateNormalMap(Texture source, float strength) => Builder.CreateNormalMap(source, strength);

    public Texture DefaultTexture => Builder.DefaultTexture;

    public (Texture tex, object tag) CreateTexture(object path, Range? level = null)
    {
        path = Source.FindPath<ITexture>(path);
        if (CachedTextures.TryGetValue(path, out var c)) return c;
        // load & cache the texture.
        var tag = path is ITexture z ? z : LoadTexture(path).Result;
        var texture = tag != null ? Builder.CreateTexture(default, tag, level) : Builder.DefaultTexture;
        CachedTextures[path] = (texture, tag);
        return (texture, tag);
    }

    public (Texture tex, object tag) ReloadTexture(object path, Range? level = null)
    {
        path = Source.FindPath<ITexture>(path);
        if (!CachedTextures.TryGetValue(path, out var c)) return (default, default);
        Builder.CreateTexture(c.tex, (ITexture)c.tag, level);
        return c;
    }

    public void PreloadTexture(object path)
    {
        path = Source.FindPath<ITexture>(path);
        if (CachedTextures.ContainsKey(path)) return;
        // start loading the texture file asynchronously if we haven't already started.
        if (!PreloadTasks.ContainsKey(path)) PreloadTasks[path] = Source.LoadFileObject<ITexture>(path);
    }

    public void DeleteTexture(object path)
    {
        path = Source.FindPath<ITexture>(path);
        if (!CachedTextures.TryGetValue(path, out var c)) return;
        Builder.DeleteTexture(c.tex);
        CachedTextures.Remove(path);
    }

    async Task<ITexture> LoadTexture(object path)
    {
        path = Source.FindPath<ITexture>(path);
        Assert(!CachedTextures.ContainsKey(path));
        PreloadTexture(path);
        var source = await PreloadTasks[path];
        PreloadTasks.Remove(path);
        return source;
    }
}

#endregion

#region Material

/// <summary>
/// IMaterial
/// </summary>
public interface IMaterial
{
    MaterialProp Begin(string platform);
    void End();
}

/// <summary>
/// MaterialProp
/// </summary>
public class MaterialProp
{
    public object Tag;
}

/// <summary>
/// MaterialPropStandard
/// </summary>
public class MaterialPropStandard : MaterialProp
{
    public Dictionary<string, string> Textures = [];
    //public string MainTexture => Textures.TryGetValue("Main", out var z) ? z : default;
    //public string BumpTexture => Textures.TryGetValue("Bump", out var z) ? z : default;
    // blend
    public bool AlphaBlended;
    public GfxBlendMode SrcBlendMode;
    public GfxBlendMode DstBlendMode;
    // alpha
    public bool AlphaTest;
    public float AlphaCutoff;
}

/// <summary>
/// MaterialPropStandard2
/// </summary>
public class MaterialPropStandard2 : MaterialPropStandard
{
    public bool ZWrite;
    public Color DiffuseColor;
    public Color SpecularColor;
    public Color EmissiveColor;
    public float Glossiness;
    public float Alpha;
}

/// <summary>
/// MaterialPropShader
/// </summary>
public class MaterialPropShader : MaterialProp
{
    public string ShaderName;
    //public IDictionary<string, object> Data;
    public IDictionary<string, bool> ShaderArgs;
}

/// <summary>
/// MaterialPropShaderV
/// </summary>
public class MaterialPropShaderV : MaterialPropShader
{
    public Dictionary<string, long> IntParams;
    public Dictionary<string, float> FloatParams;
    public Dictionary<string, Vector4> VectorParams;
    public Dictionary<string, string> TextureParams;
    public Dictionary<string, long> IntAttributes;
    //Dictionary<string, float> FloatAttributes { get; }
    //Dictionary<string, Vector4> VectorAttributes { get; }
    //Dictionary<string, string> StringAttributes { get; }
}

/// <summary>
/// MaterialTerrainProp
/// </summary>
public class MaterialTerrainProp : MaterialProp { }

/// <summary>
/// MaterialBuilderBase
/// </summary>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
/// <param name="textureManager"></param>
public abstract class MaterialBuilderBase<Material, Texture>(ITextureManager<Texture> textureManager)
{
    protected ITextureManager<Texture> TextureManager = textureManager;
    public float? NormalGeneratorIntensity = 0.75f;
    public abstract Material DefaultMaterial { get; }
    public abstract Material CreateMaterial(object path);
}

/// <summary>
/// IMaterialManager
/// </summary>
public interface IMaterialManager<Material, Texture>
{
    ITextureManager<Texture> TextureManager { get; }
    (Material mat, object tag) CreateMaterial(object path);
    void PreloadMaterial(object path);
}

/// <summary>
/// Manages loading and instantiation of materials.
/// </summary>
public class MaterialManager<Material, Texture>(ISource source, ITextureManager<Texture> textureManager, MaterialBuilderBase<Material, Texture> builder) : IMaterialManager<Material, Texture>
{
    readonly ISource Source = source;
    readonly MaterialBuilderBase<Material, Texture> Builder = builder;
    readonly Dictionary<object, (Material material, object tag)> CachedMaterials = [];
    readonly Dictionary<object, Task<MaterialProp>> PreloadTasks = [];
    public ITextureManager<Texture> TextureManager { get; } = textureManager;

    public (Material mat, object tag) CreateMaterial(object path)
    {
        if (CachedMaterials.TryGetValue(path, out var c)) return c;
        // load & cache the material.
        var source = path is MaterialProp z ? z : LoadMaterial(path).Result;
        var material = source != null ? Builder.CreateMaterial(source) : Builder.DefaultMaterial;
        var tag = source?.Tag;
        CachedMaterials[path] = (material, tag);
        return (material, tag);
    }

    public void PreloadMaterial(object path)
    {
        if (CachedMaterials.ContainsKey(path)) return;
        // start loading the material file asynchronously if we haven't already started.
        if (!PreloadTasks.ContainsKey(path)) PreloadTasks[path] = Source.LoadFileObject<MaterialProp>(path);
    }

    async Task<MaterialProp> LoadMaterial(object path)
    {
        Assert(!CachedMaterials.ContainsKey(path));
        PreloadMaterial(path);
        var source = await PreloadTasks[path];
        PreloadTasks.Remove(path);
        return source;
    }
}

#endregion

#region Particles

/// <summary>
/// IParticleSystem
/// </summary>
public interface IParticleSystem
{
    IDictionary<string, object> Data { get; }
    IEnumerable<IDictionary<string, object>> Renderers { get; }
    IEnumerable<IDictionary<string, object>> Operators { get; }
    IEnumerable<IDictionary<string, object>> Initializers { get; }
    IEnumerable<IDictionary<string, object>> Emitters { get; }
    IEnumerable<string> GetChildParticleNames(bool enabledOnly = false);
}

#endregion

#region Model

/// <summary>
/// IModel
/// </summary>
public interface IModel
{
    T Create<T>(string platform, Func<object, T> func);
    //    IDictionary<string, object> Data { get; }
    //    IVBIB RemapBoneIndices(IVBIB vbib, int meshIndex);
}

/// <summary>
/// ModelApi
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
public interface ModelApi<Object, Material>
{
    Object CreateObject(string name);
    object CreateMesh(object mesh);
    void AddMeshRenderer(Object source, object mesh, Material material, bool enabled, bool isStatic);
    void AddMeshCollider(Object source, object mesh, bool isKinematic, bool isStatic);
    //
    public void SetParent(Object source, Object parent);
    void Transform(Object source, Vector3 position, Quaternion rotation, Vector3 localScale);
    void Transform(Object source, Vector3 position, Matrix4x4 rotation, Vector3 localScale);
    public void AddMissingMeshCollidersRecursively(Object source, bool isStatic);
    public void SetLayerRecursively(Object source, int layer);
}

#endregion

#region Renderer

/// <summary>
/// Renderer
/// </summary>
public class Renderer : IDisposable
{
    /// <summary>
    /// Pass
    /// </summary>
    public enum Pass { Both, Opaque, Translucent }

    /// <summary>
    /// Start
    /// </summary>
    public virtual void Start() { }

    /// <summary>
    /// Stop
    /// </summary>
    public virtual void Stop() { }

    /// <summary>
    /// Update
    /// </summary>
    /// <param name="deltaTime"></param>
    public virtual void Update(float deltaTime) { }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose() { }
}

#endregion

#region OpenGfx

/// <summary>
/// Gfx
/// </summary>
public static class Gfx
{
    public const int XSprite2D = 0;
    public const int XSprite3D = 1;
    public const int XModel = 2;
}

/// <summary>
/// GfxStats
/// </summary>
public static class GfxStats
{
    public static int MaxTextureMaxAnisotropy;
    //    //static readonly bool _HighRes = Stopwatch.IsHighResolution;
    //    //static readonly double _HighFrequency = 1000.0 / Stopwatch.Frequency;
    //    //static readonly double _LowFrequency = 1000.0 / TimeSpan.TicksPerSecond;
    //    //static bool _UseHRT = false;
    //    //public static bool UsingHighResolutionTiming => _UseHRT && _HighRes && !Unix;
    //    //public static long TickCount => (long)Ticks;
    //    //public static double Ticks => _UseHRT && _HighRes && !Unix ? Stopwatch.GetTimestamp() * _HighFrequency : DateTime.UtcNow.Ticks * _LowFrequency;
    //    //public static readonly bool Is64Bit = Environment.Is64BitProcess;
    //    //public static bool MultiProcessor { get; private set; }
    //    //public static int ProcessorCount { get; private set; }
    //    //public static bool Unix { get; private set; }
}

/// <summary>
/// IOpenGfx
/// </summary>
public interface IOpenGfx
{
    Task<T> LoadFileObject<T>(object path);
    void PreloadObject(object path);
}

/// <summary>
/// IOpenGfxSprite
/// </summary>
public interface IOpenGfxSprite : IOpenGfx
{
    void PreloadSprite(object path);
}

/// <summary>
/// IOpenGfxSprite
/// </summary>
public interface IOpenGfxSprite<Object, Sprite> : IOpenGfxSprite
{
    ISpriteManager<Sprite> SpriteManager { get; }
    IObjectSpriteManager<Object, Sprite> ObjectManager { get; }
    Object CreateObject(object path);
}

/// <summary>
/// IOpenGfxModel
/// </summary>
public interface IOpenGfxModel : IOpenGfx
{
    void PreloadTexture(object path);
}

/// <summary>
/// IOpenGfxModel
/// </summary>
public interface IOpenGfxModel<Object, Material, Texture, Shader> : IOpenGfxModel
{
    ITextureManager<Texture> TextureManager { get; }
    IMaterialManager<Material, Texture> MaterialManager { get; }
    IObjectModelManager<Object, Material, Texture> ObjectManager { get; }
    IShaderManager<Shader> ShaderManager { get; }
    Texture CreateTexture(object path, Range? level = null);
    Object CreateObject(object path);
    Shader CreateShader(object path, IDictionary<string, bool> args = null);
}

#endregion
