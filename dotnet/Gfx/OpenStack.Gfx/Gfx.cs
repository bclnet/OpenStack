using OpenStack.Gfx.Render;
using OpenStack.Gfx.Sprite;
using OpenStack.Gfx.Texture;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static OpenStack.Debug;

[assembly: InternalsVisibleTo("OpenStack.Gl")]
[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Gfx;

#region Object2d

/// <summary>
/// Object2dBuilderBase
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
public abstract class Object2dBuilderBase<Object, Sprite>
{
    public abstract void EnsurePrefab();
    public abstract Object CreateNewObject(Object prefab);
    public abstract Object CreateObject(object source);
}

/// <summary>
/// IObject2dManager
/// </summary>
public interface IObjectSpriteManager<Object, Sprite>
{
    (Object obj, object tag) CreateObject(object path);
    void PreloadObject(object path);
}

/// <summary>
/// Object2dManager
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
/// <param name="source"></param>
/// <param name="materialManager"></param>
/// <param name="builder"></param>
public class ObjectSpriteManager<Object, Sprite>(ISource source, Object2dBuilderBase<Object, Sprite> builder) : IObjectSpriteManager<Object, Sprite>
{
    readonly ISource Source = source;
    readonly Object2dBuilderBase<Object, Sprite> Builder = builder;
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

#region Object3d

/// <summary>
/// Object3dBuilderBase
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
public abstract class Object3dBuilderBase<Object, Material, Texture>
{
    public abstract void EnsurePrefab();
    public abstract Object CreateNewObject(Object prefab);
    public abstract Object CreateObject(object source, IMaterialManager<Material, Texture> materialManager);
}

/// <summary>
/// IObject3dManager
/// </summary>
public interface IObjectModelManager<Object, Material, Texture>
{
    (Object obj, object tag) CreateObject(object path);
    void PreloadObject(object path);
}

/// <summary>
/// Object3dManager
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
/// <param name="source"></param>
/// <param name="materialManager"></param>
/// <param name="builder"></param>
public class ObjectModelManager<Object, Material, Texture>(ISource source, IMaterialManager<Material, Texture> materialManager, Object3dBuilderBase<Object, Material, Texture> builder) : IObjectModelManager<Object, Material, Texture>
{
    readonly ISource Source = source;
    readonly IMaterialManager<Material, Texture> MaterialManager = materialManager;
    readonly Object3dBuilderBase<Object, Material, Texture> Builder = builder;
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
        if (CachedTextures.TryGetValue(path, out var c)) return c;
        // load & cache the texture.
        var tag = path is ITexture z ? z : LoadTexture(path).Result;
        var texture = tag != null ? Builder.CreateTexture(default, tag, level) : Builder.DefaultTexture;
        CachedTextures[path] = (texture, tag);
        return (texture, tag);
    }

    public (Texture tex, object tag) ReloadTexture(object path, Range? level = null)
    {
        if (!CachedTextures.TryGetValue(path, out var c)) return (default, default);
        Builder.CreateTexture(c.tex, (ITexture)c.tag, level);
        return c;
    }

    public void PreloadTexture(object path)
    {
        if (CachedTextures.ContainsKey(path)) return;
        // start loading the texture file asynchronously if we haven't already started.
        if (!PreloadTasks.ContainsKey(path)) PreloadTasks[path] = Source.LoadFileObject<ITexture>(path);
    }

    public void DeleteTexture(object path)
    {
        if (!CachedTextures.TryGetValue(path, out var c)) return;
        Builder.DeleteTexture(c.tex);
        CachedTextures.Remove(path);
    }

    async Task<ITexture> LoadTexture(object path)
    {
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
    //public string Name;
    public object Tag;
}

/// <summary>
/// MaterialPropStandard
/// </summary>
public class MaterialPropStandard : MaterialProp
{
    public string MainPath;
    public string BumpPath;
    public bool AlphaBlended;
    public bool AlphaTest;
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
/// IFixedMaterial
/// </summary>
//public interface IFixedMaterial : MaterialProp
//{
//    string MainFilePath { get; }
//    string DarkFilePath { get; }
//    string DetailFilePath { get; }
//    string GlossFilePath { get; }
//    string GlowFilePath { get; }
//    string BumpFilePath { get; }
//    bool AlphaBlended { get; }
//    int SrcBlendMode { get; }
//    int DstBlendMode { get; }
//    bool AlphaTest { get; }
//    float AlphaCutoff { get; }
//    bool ZWrite { get; }
//}

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
    IDictionary<string, object> Data { get; }
    IVBIB RemapBoneIndices(IVBIB vbib, int meshIndex);
}

#endregion

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

/// <summary>
/// RendererWithViewport
/// </summary>
public abstract class RendererWithViewport : Renderer
{
    //public virtual AABB BoundingBox { get; }
    public virtual (int, int)? GetViewport((int, int) size) => default;
    public abstract void Render(Camera camera, Pass pass);
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
/// IOpenGfx2dSprite
/// </summary>
public interface IOpenGfx2dSprite : IOpenGfx
{
    void PreloadSprite(object path);
}

/// <summary>
/// IOpenGfx2dSprite
/// </summary>
public interface IOpenGfx2dSprite<Object, Sprite> : IOpenGfx2dSprite
{
    ISpriteManager<Sprite> SpriteManager { get; }
    IObjectSpriteManager<Object, Sprite> ObjectManager { get; }
    Object CreateObject(object path);
}

/// <summary>
/// IOpenGfx3dSprite
/// </summary>
public interface IOpenGfx3dSprite : IOpenGfx
{
    void PreloadSprite(object path);
}

/// <summary>
/// IOpenGfx3dSprite
/// </summary>
public interface IOpenGfx3dSprite<Object, Sprite> : IOpenGfx3dSprite
{
    ISpriteManager<Sprite> SpriteManager { get; }
    IObjectSpriteManager<Object, Sprite> ObjectManager { get; }
    Object CreateObject(object path);
}

/// <summary>
/// IOpenGfx3d
/// </summary>
public interface IOpenGfx3dModel : IOpenGfx
{
    void PreloadTexture(object path);
}

/// <summary>
/// IOpenGfx3dModel
/// </summary>
public interface IOpenGfx3dModel<Object, Material, Texture, Shader> : IOpenGfx3dModel
{
    ITextureManager<Texture> TextureManager { get; }
    IMaterialManager<Material, Texture> MaterialManager { get; }
    IObjectModelManager<Object, Material, Texture> ObjectManager { get; }
    IShaderManager<Shader> ShaderManager { get; }
    Texture CreateTexture(object path, Range? level = null);
    Object CreateObject(object path);
    Shader CreateShader(object path, IDictionary<string, bool> args = null);
}

/// <summary>
/// GfxStats
/// </summary>
public static class GfxStats
{
    public static int MaxTextureMaxAnisotropy;
}

/// <summary>
/// GFX
/// </summary>
public static class GFX
{
    public const int X2dSprite = 0;
    public const int X3dSprite = 1;
    public const int X3dModel = 2;
}

/// <summary>
/// PlatformStats
/// </summary>
//public static class PlatformStats
//{
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

//    public static int MaxTextureMaxAnisotropy;
//}