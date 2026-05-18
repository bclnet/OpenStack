using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.Gl")]
[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Gfx;

#region GfX

/// <summary>
/// GfX
/// </summary>
public static class GfX {
    public const int XApi = 0;
    public const int XSprite2D = 1;
    public const int XSprite3D = 2;
    public const int XModel = 3;
    public const int XLight = 4;
    public const int XTerrain = 5;
    public static int MaxTextureMaxAnisotropy;
}

public enum GfxAttach { Find, Transform, All, AllCenter }

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
public enum GfxAlphaMode { Always, Less, LEqual, Equal, GEqual, Greater, NotEqual, Never }

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
public enum GfxBlendMode { Zero, One, DstColor, SrcColor, OneMinusDstColor, SrcAlpha, OneMinusSrcColor, DstAlpha, OneMinusDstAlpha, SrcAlphaSaturate, OneMinusSrcAlpha }

//public record GfxKey(ISource Source, object Path);

#endregion

#region ObjectSprite

/// <summary>
/// ObjectSpriteBuilderBase
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Sprite"></typeparam>
public abstract class ObjectSpriteBuilderBase<Object, Sprite> {
    public abstract void EnsurePrefab();
    public abstract Object InstanceObject(Object src, Object parent);
    public abstract Object CreateObject(object src);
}

/// <summary>
/// ObjectSpriteManager
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Sprite"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class ObjectSpriteManager<Object, Sprite>(ObjectSpriteBuilderBase<Object, Sprite> builder) {
    readonly ObjectSpriteBuilderBase<Object, Sprite> Builder = builder;
    readonly Dictionary<object, Task<object>> PreloadTasks = [];
    static readonly Dictionary<object, (Object obj, object tag)> CachedObjects = [];

    public async Task<(Object obj, object tag)> CreateObject(ISource source, object path, Object parent = default) {
        var key = (source, path);
        if (!CachedObjects.TryGetValue(key, out var obj)) obj = CachedObjects[key] = await LoadObject(source, path);
        return (Builder.InstanceObject(obj.obj, parent), obj.tag);
    }

    public void PreloadObject(ISource source, object path) {
        var key = (source, path);
        if (CachedObjects.ContainsKey(key)) return;
        if (!PreloadTasks.ContainsKey(key)) PreloadTasks[key] = source.GetAsset<object>(path);
    }

    async Task<(Object obj, object tag)> LoadObject(ISource source, object path) {
        var key = (source, path);
        Log.Assert(!CachedObjects.ContainsKey(key));
        Builder.EnsurePrefab();
        PreloadObject(source, path);
        var obj = await PreloadTasks[key];
        PreloadTasks.Remove(key);
        return (Builder.CreateObject(obj), obj);
    }
}

#endregion

#region ObjectModel

/// <summary>
/// IObjectModel
/// </summary>
public interface IObjectModel {
    T Create<T>(string platform, Func<object, T> func);
}

/// <summary>
/// ObjectModelBuilderBase
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
public abstract class ObjectModelBuilderBase<Object, Material, Texture> {
    public abstract Object InstanceObject(Object src, Object parent);
    public abstract Task<Object> CreateObject(ISource source, object src, bool isStatic, MaterialManager<Material, Texture> materialManager);
    public abstract void EnsurePrefab();
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
public class ObjectModelManager<Object, Material, Texture>(MaterialManager<Material, Texture> materialManager, ObjectModelBuilderBase<Object, Material, Texture> builder) {
    readonly MaterialManager<Material, Texture> MaterialManager = materialManager;
    readonly ObjectModelBuilderBase<Object, Material, Texture> Builder = builder;
    readonly Dictionary<object, Task<object>> PreloadTasks = [];
    static readonly Dictionary<object, (Object obj, object tag)> CachedObjects = [];

    public async Task<(Object obj, object tag)> CreateObject(ISource source, object path, bool isStatic, Object parent = default) {
        var key = (source, path);
        try {
            if (!CachedObjects.TryGetValue(key, out var s)) s = CachedObjects[key] = await LoadObject(source, path, isStatic, parent);
            return (Builder.InstanceObject(s.obj, parent), s.tag);
        }
        catch (Exception e) { Log.Error($"{e.Message}\n{e.StackTrace}"); return (default, null); }
    }

    public void PreloadObject(ISource source, object path) {
        var key = (source, path);
        if (CachedObjects.ContainsKey(key)) return;
        if (!PreloadTasks.ContainsKey(key)) PreloadTasks[key] = source.GetAsset<object>(path);
    }

    async Task<(Object obj, object tag)> LoadObject(ISource source, object path, bool isStatic, Object parent) {
        var key = (source, path);
        Log.Assert(!CachedObjects.ContainsKey(key));
        Builder.EnsurePrefab();
        PreloadObject(source, path);
        try {
            var obj = await PreloadTasks[key];
            return (await Builder.CreateObject(source, obj, isStatic, MaterialManager), obj);
        }
        finally { PreloadTasks.Remove(key); }
    }
}

#endregion

#region Shader

/// <summary>
/// Shader
/// </summary>
public class Shader(Func<int, string, int> getUniformLocation, Func<int, string, int> getAttribLocation) {
    readonly Func<int, string, int> _getUniformLocation = getUniformLocation ?? throw new ArgumentNullException(nameof(getUniformLocation));
    readonly Func<int, string, int> _getAttribLocation = getAttribLocation ?? throw new ArgumentNullException(nameof(getAttribLocation));
    public string Name;
    public int Program;
    public IDictionary<string, bool> Parameters;
    public List<string> RenderModes;
    Dictionary<string, int> _uniforms = [];

    public int GetUniformLocation(string name) {
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
public abstract class ShaderBuilderBase<Shader> {
    public abstract Shader CreateShader(object path, IDictionary<string, bool> args = null);
}

/// <summary>
/// ShaderManager
/// </summary>
/// <typeparam name="Shader"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class ShaderManager<Shader>(ShaderBuilderBase<Shader> builder) {
    static readonly Dictionary<string, bool> EmptyArgs = [];
    readonly ShaderBuilderBase<Shader> Builder = builder;

    public async Task<(Shader sha, object tag)> CreateShader(ISource source, object path, IDictionary<string, bool> args = null) => (Builder.CreateShader(path, args ?? EmptyArgs), null);
}

#endregion

#region Sprite

/// <summary>
/// ISprite
/// </summary>
public interface ISprite {
    int Width { get; }
    int Height { get; }
    T Create<T>(string platform, Func<object, T> func);
}

/// <summary>
/// SpriteBuilderBase
/// </summary>
/// <typeparam name="Sprite"></typeparam>
public abstract class SpriteBuilderBase<Sprite> {
    public abstract Sprite DefaultSprite { get; }
    public abstract Sprite CreateSprite(ISprite spr);
    public abstract void DeleteSprite(Sprite spr);
}

/// <summary>
/// SpriteManager
/// </summary>
/// <typeparam name="Texture"></typeparam>
/// <param name="builder"></param>
public class SpriteManager<Sprite>(SpriteBuilderBase<Sprite> builder) {
    readonly SpriteBuilderBase<Sprite> Builder = builder;
    readonly Dictionary<object, Task<ISprite>> PreloadTasks = [];
    static readonly Dictionary<object, (Sprite spr, object tag)> CachedSprites = [];

    public Sprite DefaultSprite => Builder.DefaultSprite;

    public async Task<(Sprite spr, object tag)> CreateSprite(ISource source, object path) {
        var key = (source, path);
        if (CachedSprites.TryGetValue(key, out var c)) return c;
        var tag = path is ISprite z ? z : await LoadSprite(source, path);
        var obj = tag != null ? Builder.CreateSprite(tag) : Builder.DefaultSprite;
        return CachedSprites[key] = (obj, tag);
    }

    public void PreloadSprite(ISource source, object path) {
        var key = (source, path);
        if (CachedSprites.ContainsKey(key)) return;
        if (!PreloadTasks.ContainsKey(key)) PreloadTasks[key] = source.GetAsset<ISprite>(path);
    }

    public void DeleteSprite(ISource source, object path) {
        var key = (source, path);
        if (!CachedSprites.TryGetValue(key, out var c)) return;
        Builder.DeleteSprite(c.spr);
        CachedSprites.Remove(key);
    }

    async Task<ISprite> LoadSprite(ISource source, object path) {
        var key = (source, path);
        Log.Assert(!CachedSprites.ContainsKey(key));
        PreloadSprite(source, path);
        var obj = await PreloadTasks[key];
        PreloadTasks.Remove(key);
        return obj;
    }
}

#endregion

#region Texture

/// <summary>
/// Texture_Dds
/// </summary>
public struct Texture_Dds(byte[] bytes) {
    public byte[] Bytes = bytes;
}

/// <summary>
/// Texture_Bytes
/// </summary>
public struct Texture_Bytes(byte[] bytes, object format, Range[] spans) {
    public byte[] Bytes = bytes;
    public object Format = format;
    public Range[] Spans = spans;
}

/// <summary>
/// ITexture
/// </summary>
public interface ITexture {
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
public interface ITextureSelect : ITexture {
    void Select(int id);
}

/// <summary>
/// ITextureFrames
/// </summary>
public interface ITextureFrames : ITexture {
    int Fps { get; }
    bool HasFrames { get; }
    bool DecodeFrame();
}

/// <summary>
/// ITextureFramesSelect
/// </summary>
public interface ITextureFramesSelect : ITextureFrames {
    int FrameMax { get; }
    void FrameSelect(int id);
}

/// <summary>
/// TextureBuilderBase
/// </summary>
/// <typeparam name="Texture"></typeparam>
public abstract class TextureBuilderBase<Texture> {
    public static int MaxTextureMaxAnisotropy => GfX.MaxTextureMaxAnisotropy;
    public abstract Texture DefaultTexture { get; }
    public abstract Texture CreateNormalMapTexture(Texture src, float strength);
    public abstract Texture CreateSolidTexture(int width, int height, float[] rgbas);
    public abstract Texture CreateTexture(Texture reuse, ITexture src, Range? level = null);
    public abstract void DeleteTexture(Texture src);
}

/// <summary>
/// TextureManager
/// </summary>
/// <typeparam name="Texture"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class TextureManager<Texture>(TextureBuilderBase<Texture> builder) {
    class Solid(int width, int height, float[] rgbas) {
        public int Width = width;
        public int Height = height;
        public float[] Rgbas = rgbas;
    }

    readonly TextureBuilderBase<Texture> Builder = builder;
    readonly Dictionary<object, Task<ITexture>> PreloadTasks = [];
    static readonly Dictionary<Texture, Texture> CachedNormalMapTextures = [];
    static readonly Dictionary<Solid, Texture> CachedSolidTextures = [];
    static readonly Dictionary<object, (Texture tex, object tag)> CachedTextures = [];
    public Texture DefaultTexture => Builder.DefaultTexture;
    const float NormalMapIntensity = 0.75f;

    public Texture CreateNormalMapTexture(Texture src, float strength = -1) {
        if (CachedNormalMapTextures.TryGetValue(src, out var s)) return s;
        s = Builder.CreateNormalMapTexture(src, strength < 0 ? NormalMapIntensity : strength);
        CachedNormalMapTextures[src] = s;
        return s;
    }

    public Texture CreateSolidTexture(int width, int height, float[] rgbas) {
        var src = new Solid(width, height, rgbas);
        if (CachedSolidTextures.TryGetValue(src, out var s)) return s;
        s = Builder.CreateSolidTexture(width, height, rgbas);
        CachedSolidTextures[src] = s;
        return s;
    }

    public async Task<(Texture tex, object tag)> CreateTexture(ISource source, object path, Range? level = null) {
        var key = (source, path);
        if (CachedTextures.TryGetValue(key, out var c)) return c;
        var tag = path is ITexture z ? z : await LoadTexture(source, path);
        var obj = tag != null ? Builder.CreateTexture(default, tag, level) : Builder.DefaultTexture;
        return CachedTextures[key] = (obj, tag);
    }

    public (Texture tex, object tag) ReloadTexture(ISource source, object path, Range? level = null) {
        var key = (source, path);
        if (!CachedTextures.TryGetValue(key, out var c)) return (default, default);
        Builder.CreateTexture(c.tex, (ITexture)c.tag, level);
        return c;
    }

    public void PreloadTexture(ISource source, object path) {
        var key = (source, path);
        if (CachedTextures.ContainsKey(key)) return;
        if (!PreloadTasks.ContainsKey(key)) PreloadTasks[key] = source.GetAsset<ITexture>(path);
    }

    public void DeleteTexture(ISource source, object path) {
        var key = (source, path);
        if (!CachedTextures.TryGetValue(key, out var c)) return;
        Builder.DeleteTexture(c.tex);
        CachedTextures.Remove(key);
    }

    async Task<ITexture> LoadTexture(ISource source, object path) {
        var key = (source, path);
        Log.Assert(!CachedTextures.ContainsKey(key));
        PreloadTexture(source, path);
        var obj = await PreloadTasks[key];
        PreloadTasks.Remove(key);
        return obj;
    }
}

#endregion

#region Material

/// <summary>
/// IMaterial
/// </summary>
public interface IMaterial {
    T Create<T>(string platform, Func<object, T> func);
}

/// <summary>
/// MaterialProp
/// </summary>
public abstract class MaterialProp {
    public object Tag;
}

/// <summary>
/// MaterialStdProp
/// </summary>
public class MaterialStdProp : MaterialProp {
    public Dictionary<string, object> Textures = [];
    public bool AlphaBlended;
    public GfxBlendMode SrcBlendMode;
    public GfxBlendMode DstBlendMode;
    public bool AlphaTest;
    public float AlphaCutoff;
}

/// <summary>
/// MaterialStd2Prop
/// </summary>
public class MaterialStd2Prop : MaterialStdProp {
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
public class MaterialShaderProp : MaterialProp {
    public string ShaderName;
    public IDictionary<string, bool> ShaderArgs;
    //public IDictionary<string, object> Data;
}

/// <summary>
/// MaterialPropShaderV
/// </summary>
public class MaterialShaderVProp : MaterialShaderProp {
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
//public class MaterialTerrainProp : MaterialProp { }

/// <summary>
/// MaterialBuilderBase
/// </summary>
/// <typeparam name="Material"></typeparam>
/// <typeparam name="Texture"></typeparam>
/// <param name="textureManager"></param>
public abstract class MaterialBuilderBase<Material, Texture>(TextureManager<Texture> textureManager) {
    protected TextureManager<Texture> TextureManager = textureManager;
    public abstract Material DefaultMaterial { get; }
    public abstract Material TerrainMaterial { get; }
    public abstract Task<Material> CreateMaterial(ISource source, object path);
}

/// <summary>
/// Manages loading and instantiation of materials.
/// </summary>
public class MaterialManager<Material, Texture>(TextureManager<Texture> textureManager, MaterialBuilderBase<Material, Texture> builder) {
    readonly MaterialBuilderBase<Material, Texture> Builder = builder;
    readonly Dictionary<object, Task<MaterialProp>> PreloadTasks = [];
    static readonly Dictionary<object, (Material material, object tag)> CachedMaterials = [];

    public TextureManager<Texture> TextureManager { get; } = textureManager;
    public Material DefaultMaterial => Builder.DefaultMaterial;
    public Material TerrainMaterial => Builder.TerrainMaterial;

    public async Task<(Material mat, object tag)> CreateMaterial(ISource source, object path) {
        var key = (source, path);
        if (CachedMaterials.TryGetValue(key, out var c)) return c;
        var src = path is MaterialProp z ? z : await LoadMaterial(source, path);
        var obj = src != null ? await Builder.CreateMaterial(source, src) : Builder.DefaultMaterial;
        var tag = src?.Tag;
        return CachedMaterials[key] = (obj, tag);
    }

    public void PreloadMaterial(ISource source, object path) {
        var key = (source, path);
        if (CachedMaterials.ContainsKey(key)) return;
        if (!PreloadTasks.ContainsKey(key)) PreloadTasks[key] = source.GetAsset<MaterialProp>(path);
    }

    async Task<MaterialProp> LoadMaterial(ISource source, object path) {
        var key = (source, path);
        Log.Assert(!CachedMaterials.ContainsKey(key));
        PreloadMaterial(source, path);
        var obj = await PreloadTasks[key];
        PreloadTasks.Remove(key);
        return obj;
    }
}

#endregion

#region OpenGfx

/// <summary>
/// IOpenGfx
/// </summary>
public interface IOpenGfx { }

/// <summary>
/// IOpenGfxApi
/// </summary>
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Material"></typeparam>
public interface IOpenGfxApi<Object, Material> : IOpenGfx {
    Object CreateObject(string name, string tag = null, Object parent = default);
    object CreateMesh(object mesh);
    void AddMeshRenderer(Object src, object mesh, Material material, bool enabled, bool isStatic);
    void AddMeshCollider(Object src, object mesh, bool isKinematic, bool isStatic);
    void Attach(GfxAttach method, Object src, params object[] args);
    void Parent(Object src, Object parent);
    void Transform(Object src, Vector3 position, Quaternion rotation, Vector3 localScale);
    void Transform(Object src, Vector3 position, Matrix4x4 rotation, Vector3 localScale);
    void AddMissingMeshCollidersRecursively(Object src, bool isStatic);
    void SetLayerRecursively(Object src, int layer);
    void SetVisible(Object src, bool visible);
    void Destroy(Object src);
}

/// <summary>
/// IOpenGfxSprite
/// </summary>
public interface IOpenGfxSprite<Object, Sprite> : IOpenGfx {
    SpriteManager<Sprite> SpriteManager { get; }
    void PreloadSprite(ISource source, object path);
    Task<(Sprite spr, object tag)> CreateSprite(ISource source, object path, Object parent = default);
}

/// <summary>
/// IOpenGfxModel
/// </summary>
public interface IOpenGfxModel<Object, Material, Texture, Shader> : IOpenGfx {
    MaterialManager<Material, Texture> MaterialManager { get; }
    ObjectModelManager<Object, Material, Texture> ObjectManager { get; }
    ShaderManager<Shader> ShaderManager { get; }
    TextureManager<Texture> TextureManager { get; }
    void PreloadObject(ISource source, object path);
    void PreloadTexture(ISource source, object path);
    Task<(Object obj, object tag)> CreateObject(ISource source, object path, bool isStatic, Object parent = default);
    Task<(Shader sha, object tag)> CreateShader(ISource source, object path, IDictionary<string, bool> args = null);
    Task<(Texture tex, object tag)> CreateTexture(ISource source, object path, Range? level = null);
    void PostObject(Object src, Vector3 position, Vector3 eulerAngles, float? scale, Object parent = default);
}

/// <summary>
/// IOpenGfxLight
/// </summary>
public interface IOpenGfxLight<Object> : IOpenGfx {
    Object CreateLight(string name, Vector3? position, float radius, Color color, bool indoors, Object parent = default);
    Object CreateReflectionProbe(string name, Vector3? position, Object parent = default);
}

/// <summary>
/// GfxTerrainLayer
/// </summary>
public class GfxTerrainLayer<Texture_> {
    public Texture_ Texture;
    public float Smoothness;
    public float Metallic;
    public Color Specular;
    public Texture_ MaskMapTexture;
    public Texture_ NormalMapTexture;
    public Vector2 TileSize;
}

/// <summary>
/// IOpenGfxTerrain
/// </summary>
public interface IOpenGfxTerrain<Object, Material, Texture> : IOpenGfx {
    object CreateTerrainData(int offset, float[,] heights, float heightRange, float sampleDistance, GfxTerrainLayer<Texture>[] layers, float[,,] alphaMap);
    Object CreateTerrain(string name, Vector3? position, object data, Object parent = default);
}

#endregion

/// <summary>
/// GfxStats
/// </summary>
//public static class GfxStats
//{
//    //    //static readonly bool _HighRes = Stopwatch.IsHighResolution;
//    //    //static readonly double _HighFrequency = 1000.0 / Stopwatch.Frequency;
//    //    //static readonly double _LowFrequency = 1000.0 / TimeSpan.TicksPerSecond;
//    //    //static bool _UseHRT = false;
//    //    //public static bool UsingHighResolutionTiming => _UseHRT && _HighRes && !Unix;
//    //    //public static long TickCount => (long)Ticks;
//    //    //public static double Ticks => _UseHRT && _HighRes && !Unix ? Stopwatch.GetTimestamp() * _HighFrequency : DateTime.UtcNow.Ticks * _LowFrequency;
//    //    //public static readonly bool Is64Bit = Environment.Is64BitProcess;
//    //    //public static bool MultiProcessor { get; private set; }
//    //    //public static int ProcessorCount { get; private set; }
//    //    //public static bool Unix { get; private set; }
//}