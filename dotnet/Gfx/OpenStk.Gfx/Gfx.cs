using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStk.GfxTests")]

namespace OpenStk.Gfx;

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

/// <summary>
/// GfxAlphaMode (glAlphaFunc)
/// </summary>
public enum GfxAlphaMode { Never = 0x0200, Less = 0x0201, Equal = 0x0202, LEqual = 0x0203, Greater = 0x0204, NotEqual = 0x0205, GEqual = 0x0206, Always = 0x0207 }

/// <summary>
/// GfxBlendMode (glBlendFunc)
/// </summary>
public enum GfxBlendMode { Zero = 0, One = 1, SrcColor = 0x0300, OneMinusSrcColor = 0x0301, SrcAlpha = 0x0302, OneMinusSrcAlpha = 0x0303, DstAlpha = 0x0304, OneMinusDstAlpha = 0x0305, DstColor = 0x0306, OneMinusDstColor = 0x0307, SrcAlphaSaturate = 0x0308 }

//public record GfxKey(ISource Source, object Path);

#endregion

#region ObjectSprite

/// <summary>
/// ObjectSpriteBuilder
/// </summary>
/// <typeparam name="B_Object"></typeparam>
/// <typeparam name="B_Sprite"></typeparam>
public abstract class ObjectSpriteBuilder<B_Object, B_Sprite> {
    public abstract void Ensure();
    public abstract B_Object Instance(B_Object src, B_Object parent);
    public abstract B_Object Create(object src);
}

/// <summary>
/// ObjectSpriteManager
/// </summary>
/// <typeparam name="B_Object"></typeparam>
/// <typeparam name="B_Sprite"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class ObjectSpriteManager<B_Object, B_Sprite>(ObjectSpriteBuilder<B_Object, B_Sprite> builder) {
    readonly ObjectSpriteBuilder<B_Object, B_Sprite> Builder = builder;
    readonly Dictionary<object, Task<object>> Tasks = [];
    static readonly Dictionary<object, (B_Object obj, object tag)> Cached = [];

    public async Task<(B_Object obj, object tag)> Create(ISource source, object path, B_Object parent = default) {
        var key = (source, path);
        if (!Cached.TryGetValue(key, out var obj)) obj = Cached[key] = await Load(source, path);
        return (Builder.Instance(obj.obj, parent), obj.tag);
    }

    public void Preload(ISource source, object path) {
        var key = (source, path);
        if (Cached.ContainsKey(key)) return;
        if (!Tasks.ContainsKey(key)) Tasks[key] = source.GetAsset<object>(path);
    }

    async Task<(B_Object obj, object tag)> Load(ISource source, object path) {
        var key = (source, path);
        Debug.Assert(!Cached.ContainsKey(key));
        Builder.Ensure();
        Preload(source, path);
        var obj = await Tasks[key];
        Tasks.Remove(key);
        return (Builder.Create(obj), obj);
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
/// ObjectModelBuilder
/// </summary>
/// <typeparam name="B_Object"></typeparam>
/// <typeparam name="B_Material"></typeparam>
/// <typeparam name="B_Texture"></typeparam>
public abstract class ObjectModelBuilder<B_Object, B_Material, B_Texture> {
    public abstract B_Object Instance(B_Object src, B_Object parent);
    public abstract Task<B_Object> Create(ISource source, object src, bool static_, MaterialManager<B_Material, B_Texture> materialManager);
    public abstract void Ensure();
}

/// <summary>
/// ObjectModelManager
/// </summary>
/// <typeparam name="B_Object"></typeparam>
/// <typeparam name="B_Material"></typeparam>
/// <typeparam name="B_Texture"></typeparam>
/// <param name="source"></param>
/// <param name="materialManager"></param>
/// <param name="builder"></param>
public class ObjectModelManager<B_Object, B_Material, B_Texture>(MaterialManager<B_Material, B_Texture> materialManager, ObjectModelBuilder<B_Object, B_Material, B_Texture> builder) {
    readonly MaterialManager<B_Material, B_Texture> MaterialManager = materialManager;
    readonly ObjectModelBuilder<B_Object, B_Material, B_Texture> Builder = builder;
    readonly Dictionary<object, Task<object>> Tasks = [];
    static readonly Dictionary<object, (B_Object obj, object tag)> Cached = [];

    public async Task<(B_Object obj, object tag)> Create(ISource source, object path, bool static_, B_Object parent = default) {
        var key = (source, path);
        try {
            if (!Cached.TryGetValue(key, out var s)) s = Cached[key] = await Load(source, path, static_, parent);
            return (Builder.Instance(s.obj, parent), s.tag);
        }
        catch (Exception e) { Log.Error($"{e.Message}\n{e.StackTrace}"); return (default, null); }
    }

    public void Preload(ISource source, object path) {
        var key = (source, path);
        if (Cached.ContainsKey(key)) return;
        if (!Tasks.ContainsKey(key)) Tasks[key] = source.GetAsset<object>(path);
    }

    async Task<(B_Object obj, object tag)> Load(ISource source, object path, bool static_, B_Object parent) {
        var key = (source, path);
        Debug.Assert(!Cached.ContainsKey(key));
        Builder.Ensure();
        Preload(source, path);
        try {
            var obj = await Tasks[key];
            return (await Builder.Create(source, obj, static_, MaterialManager), obj);
        }
        finally { Tasks.Remove(key); }
    }
}

#endregion

#region Shader

//const int ShaderSeed = 0x13141516;
//readonly Dictionary<uint, Shader> CachedShaders = [];
//readonly Dictionary<string, List<string>> ShaderDefines = [];
// cache
//var cache = !name.StartsWith("#");
//if (cache && ShaderDefines.ContainsKey(shaderFileName)) {
//    var shaderCacheHash = CalculateShaderCacheHash(shaderFileName, args);
//    if (CachedShaders.TryGetValue(shaderCacheHash, out var c)) return c;
//}
//uint CalculateShaderCacheHash(string name, IDictionary<string, bool> args) {
//    var b = new StringBuilder(); b.AppendLine(name);
//    var parameters = ShaderDefines[name].Intersect(args.Keys);
//    foreach (var key in parameters) { b.AppendLine(key); b.AppendLine(args[key] ? "t" : "f"); }
//    return MurmurHash2.Hash(b.ToString(), ShaderSeed);
//}
//#if !DEBUG_SHADERS || !DEBUG
//        // cache shader
//        if (cache) {
//            ShaderDefines[shaderFileName] = defines;
//            var newShaderCacheHash = CalculateShaderCacheHash(shaderFileName, args);
//            CachedShaders[newShaderCacheHash] = shader;
//        }
//#endif

/// <summary>
/// GfxShader
/// </summary>
public class GfxShader(Func<int, string, int> uniformLocation, Func<int, string, int> attribLocation) {
    readonly Func<int, string, int> _uniformLocation = uniformLocation ?? throw new ArgumentNullException(nameof(uniformLocation));
    readonly Func<int, string, int> _attribLocation = attribLocation ?? throw new ArgumentNullException(nameof(attribLocation));
    public string Name;
    public int Program;
    public Dictionary<string, bool> Parameters;
    public List<string> RenderModes;
    readonly Dictionary<string, int> _uniforms = [];

    public int UniformLocation(string name) {
        if (_uniforms.TryGetValue(name, out var value)) return value;
        value = _uniformLocation(Program, name); _uniforms[name] = value;
        return value;
    }

    public uint AttribLocation(string name) => (uint)_attribLocation(Program, name);
}

/// <summary>
/// ShaderBuilder
/// </summary>
/// <typeparam name="B_Shader"></typeparam>
public abstract class ShaderBuilder<B_Shader> {
    public abstract B_Shader Create(object path, Dictionary<string, bool> args = null);
}

/// <summary>
/// ShaderManager
/// </summary>
/// <typeparam name="B_Shader"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class ShaderManager<B_Shader>(ShaderBuilder<B_Shader> builder) {
    static readonly Dictionary<string, bool> EmptyArgs = [];
    readonly ShaderBuilder<B_Shader> Builder = builder;
    static readonly Dictionary<object, (B_Shader sha, object tag)> Cached = [];

    public async Task<(B_Shader sha, object tag)> Create(ISource source, object path, Dictionary<string, bool> args = null) {
        var argx = args != null ? string.Join(",", args.Where(x => x.Value).Select(x => x.Key)) : null;
        var key = (source, path, argx);
        if (Cached.TryGetValue(key, out var c)) return c;
        var sha = Builder.Create(path, args ?? EmptyArgs);
        return Cached[key] = (sha, null);
    }
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
/// SpriteBuilder
/// </summary>
/// <typeparam name="B_Sprite"></typeparam>
public abstract class SpriteBuilder<B_Sprite> {
    public abstract B_Sprite Default { get; }
    public abstract B_Sprite Create(ISprite spr);
    public abstract void Delete(B_Sprite spr);
}

/// <summary>
/// SpriteManager
/// </summary>
/// <typeparam name="Texture"></typeparam>
/// <param name="builder"></param>
public class SpriteManager<B_Sprite>(SpriteBuilder<B_Sprite> builder) {
    readonly SpriteBuilder<B_Sprite> Builder = builder;
    readonly Dictionary<object, Task<ISprite>> Tasks = [];
    static readonly Dictionary<object, (B_Sprite spr, object tag)> Cached = [];

    public B_Sprite Default => Builder.Default;

    public async Task<(B_Sprite spr, object tag)> Create(ISource source, object path) {
        var key = (source, path);
        if (Cached.TryGetValue(key, out var c)) return c;
        var tag = path is ISprite z ? z : await Load(source, path);
        var obj = tag != null ? Builder.Create(tag) : Builder.Default;
        return Cached[key] = (obj, tag);
    }

    public void Preload(ISource source, object path) {
        var key = (source, path);
        if (Cached.ContainsKey(key)) return;
        if (!Tasks.ContainsKey(key)) Tasks[key] = source.GetAsset<ISprite>(path);
    }

    public void Delete(ISource source, object path) {
        var key = (source, path);
        if (!Cached.TryGetValue(key, out var c)) return;
        Builder.Delete(c.spr);
        Cached.Remove(key);
    }

    async Task<ISprite> Load(ISource source, object path) {
        var key = (source, path);
        Debug.Assert(!Cached.ContainsKey(key));
        Preload(source, path);
        var obj = await Tasks[key];
        Tasks.Remove(key);
        return obj;
    }
}

#endregion

#region Texture

/// <summary>
/// TextureAsDds
/// </summary>
public struct TextureAsDds(byte[] bytes) {
    public byte[] Bytes = bytes;
}

/// <summary>
/// TextureAsBytes
/// </summary>
public struct TextureAsBytes(byte[] bytes, object format, Range[] spans) {
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
    int MaxId { get; }
    void Select(int id);
}

/// <summary>
/// ITextureFrames
/// </summary>
public interface ITextureFrames : ITexture {
    int Fps { get; }
    bool HasFrames { get; }
    bool NextFrame();
}

/// <summary>
/// TextureBuilder
/// </summary>
/// <typeparam name="B_Texture"></typeparam>
public abstract class TextureBuilder<B_Texture> {
    public static int MaxTextureMaxAnisotropy => GfX.MaxTextureMaxAnisotropy;
    public abstract B_Texture Default { get; }
    public abstract B_Texture CreateNormalMap(B_Texture src, float strength);
    public abstract B_Texture CreateSolid(int width, int height, float[] rgbas);
    public abstract B_Texture Create(B_Texture reuse, ITexture src, Range? level = null);
    public abstract void Delete(B_Texture src);
}

/// <summary>
/// TextureManager
/// </summary>
/// <typeparam name="B_Texture"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class TextureManager<B_Texture>(TextureBuilder<B_Texture> builder) {
    class Solid(int width, int height, float[] rgbas) {
        public int Width = width;
        public int Height = height;
        public float[] Rgbas = rgbas;
    }

    readonly TextureBuilder<B_Texture> Builder = builder;
    readonly Dictionary<object, Task<ITexture>> Tasks = [];
    static readonly Dictionary<B_Texture, B_Texture> CachedNormalMaps = [];
    static readonly Dictionary<Solid, B_Texture> CachedSolids = [];
    static readonly Dictionary<object, (B_Texture tex, object tag)> Cached = [];
    public B_Texture Default => Builder.Default;
    const float NormalMapIntensity = 0.75f;

    public B_Texture CreateNormalMap(B_Texture src, float strength = -1) {
        if (CachedNormalMaps.TryGetValue(src, out var s)) return s;
        s = Builder.CreateNormalMap(src, strength < 0 ? NormalMapIntensity : strength);
        CachedNormalMaps[src] = s;
        return s;
    }

    public B_Texture CreateSolid(int width, int height, float[] rgbas) {
        var src = new Solid(width, height, rgbas);
        if (CachedSolids.TryGetValue(src, out var s)) return s;
        s = Builder.CreateSolid(width, height, rgbas);
        CachedSolids[src] = s;
        return s;
    }

    public async Task<(B_Texture tex, object tag)> Create(ISource source, object path, Range? level = null) {
        var key = (source, path);
        if (Cached.TryGetValue(key, out var c)) return c;
        var tag = path is ITexture z ? z : await Load(source, path);
        var obj = tag != null ? Builder.Create(default, tag, level) : Builder.Default;
        return Cached[key] = (obj, tag);
    }

    public (B_Texture tex, object tag) Reload(ISource source, object path, Range? level = null) {
        var key = (source, path);
        if (!Cached.TryGetValue(key, out var c)) return (default, default);
        Builder.Create(c.tex, (ITexture)c.tag, level);
        return c;
    }

    public void Preload(ISource source, object path) {
        var key = (source, path);
        if (Cached.ContainsKey(key)) return;
        if (!Tasks.ContainsKey(key)) Tasks[key] = source.GetAsset<ITexture>(path);
    }

    public void Delete(ISource source, object path) {
        var key = (source, path);
        if (!Cached.TryGetValue(key, out var c)) return;
        Builder.Delete(c.tex);
        Cached.Remove(key);
    }

    async Task<ITexture> Load(ISource source, object path) {
        var key = (source, path);
        Debug.Assert(!Cached.ContainsKey(key));
        Preload(source, path);
        var obj = await Tasks[key];
        Tasks.Remove(key);
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
    public Dictionary<string, bool> ShaderArgs;
    //public Dictionary<string, object> Data;
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
/// MaterialBuilder
/// </summary>
/// <typeparam name="B_Material"></typeparam>
/// <typeparam name="B_Texture"></typeparam>
/// <param name="textureManager"></param>
public abstract class MaterialBuilder<B_Material, B_Texture>(TextureManager<B_Texture> textureManager) {
    protected TextureManager<B_Texture> TextureManager = textureManager;
    public abstract B_Material Default { get; }
    public abstract B_Material Terrain { get; }
    public abstract Task<B_Material> Create(ISource source, object path);
}

/// <summary>
/// Manages loading and instantiation of materials.
/// </summary>
public class MaterialManager<B_Material, B_Texture>(TextureManager<B_Texture> textureManager, MaterialBuilder<B_Material, B_Texture> builder) {
    readonly MaterialBuilder<B_Material, B_Texture> Builder = builder;
    readonly Dictionary<object, Task<MaterialProp>> Tasks = [];
    static readonly Dictionary<object, (B_Material material, object tag)> Cached = [];

    public TextureManager<B_Texture> TextureManager { get; } = textureManager;
    public B_Material Default => Builder.Default;
    public B_Material Terrain => Builder.Terrain;

    public async Task<(B_Material mat, object tag)> Create(ISource source, object path) {
        var key = (source, path);
        if (Cached.TryGetValue(key, out var c)) return c;
        var src = path is MaterialProp z ? z : await Load(source, path);
        var obj = src != null ? await Builder.Create(source, src) : Builder.Default;
        var tag = src?.Tag;
        return Cached[key] = (obj, tag);
    }

    public void Preload(ISource source, object path) {
        var key = (source, path);
        if (Cached.ContainsKey(key)) return;
        if (!Tasks.ContainsKey(key)) Tasks[key] = source.GetAsset<MaterialProp>(path);
    }

    async Task<MaterialProp> Load(ISource source, object path) {
        var key = (source, path);
        Debug.Assert(!Cached.ContainsKey(key));
        Preload(source, path);
        var obj = await Tasks[key];
        Tasks.Remove(key);
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
/// <typeparam name="B_Object"></typeparam>
/// <typeparam name="B_Material"></typeparam>
public interface IOpenGfxApi<B_Object, B_Material> : IOpenGfx {
    B_Object CreateObject(string name, string tag = null, B_Object parent = default);
    object CreateMesh(object mesh);
    void AddMeshRenderer(B_Object src, object mesh, B_Material material, bool enabled, bool isStatic);
    void AddMeshCollider(B_Object src, object mesh, bool isKinematic, bool isStatic);
    void Attach(GfxAttach method, B_Object src, params object[] args);
    void Parent(B_Object src, B_Object parent);
    void Transform(B_Object src, Vector3 position, Quaternion rotation, Vector3 localScale);
    void Transform(B_Object src, Vector3 position, Matrix4x4 rotation, Vector3 localScale);
    void AddMissingMeshCollidersRecursively(B_Object src, bool isStatic);
    void SetLayerRecursively(B_Object src, int layer);
    void SetVisible(B_Object src, bool visible);
    void Destroy(B_Object src);
}

/// <summary>
/// IOpenGfxSprite
/// </summary>
public interface IOpenGfxSprite<B_Object, B_Sprite> : IOpenGfx {
    SpriteManager<B_Sprite> SpriteManager { get; }
    void Preload(ISource source, object path);
    Task<(B_Sprite spr, object tag)> Create(ISource source, object path, B_Object parent = default);
}

/// <summary>
/// IOpenGfxModel
/// </summary>
public interface IOpenGfxModel<B_Object, B_Material, B_Texture, B_Shader> : IOpenGfx {
    MaterialManager<B_Material, B_Texture> MaterialManager { get; }
    ObjectModelManager<B_Object, B_Material, B_Texture> ObjectManager { get; }
    ShaderManager<B_Shader> ShaderManager { get; }
    TextureManager<B_Texture> TextureManager { get; }
    void Preload(ISource source, object path);
    void PreloadTexture(ISource source, object path);
    Task<(B_Object obj, object tag)> Create(ISource source, object path, bool isStatic, B_Object parent = default);
    Task<(B_Shader sha, object tag)> CreateShader(ISource source, object path, Dictionary<string, bool> args = null);
    Task<(B_Texture tex, object tag)> CreateTexture(ISource source, object path, Range? level = null);
    void Post(B_Object src, Vector3 position, Vector3 eulerAngles, float? scale, B_Object parent = default);
}

/// <summary>
/// IOpenGfxLight
/// </summary>
public interface IOpenGfxLight<B_Object> : IOpenGfx {
    B_Object Create(string name, Vector3? position, float radius, Color color, bool indoors, B_Object parent = default);
    B_Object CreateProbe(string name, Vector3? position, B_Object parent = default);
}

/// <summary>
/// GfxTerrainLayer
/// </summary>
public class GfxTerrainLayer<B_Texture> {
    public B_Texture Texture;
    public float Smoothness;
    public float Metallic;
    public Color Specular;
    public B_Texture MaskMapTexture;
    public B_Texture NormalMapTexture;
    public Vector2 TileSize;
}

/// <summary>
/// IOpenGfxTerrain
/// </summary>
public interface IOpenGfxTerrain<B_Object, B_Material, B_Texture> : IOpenGfx {
    object CreateData(int offset, float[,] heights, float heightRange, float sampleDistance, GfxTerrainLayer<B_Texture>[] layers, float[,,] alphaMap);
    B_Object Create(string name, Vector3? position, object data, B_Object parent = default);
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