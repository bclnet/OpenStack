using System;
using System.Collections.Generic;
using System.Drawing;
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
/// <typeparam name="Object"></typeparam>
/// <typeparam name="Sprite"></typeparam>
public abstract class ObjectSpriteBuilder<Object, Sprite> {
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
public class ObjectSpriteManager<Object, Sprite>(ObjectSpriteBuilder<Object, Sprite> builder) {
    readonly ObjectSpriteBuilder<Object, Sprite> Builder = builder;
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
/// ObjectModelBuilder
/// </summary>
/// <typeparam name="B_Object"></typeparam>
/// <typeparam name="B_Material"></typeparam>
/// <typeparam name="B_Texture"></typeparam>
public abstract class ObjectModelBuilder<B_Object, B_Material, B_Texture> {
    public abstract B_Object InstanceObject(B_Object src, B_Object parent);
    public abstract Task<B_Object> CreateObject(ISource source, object src, bool isStatic, MaterialManager<B_Material, B_Texture> materialManager);
    public abstract void EnsurePrefab();
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
    readonly Dictionary<object, Task<object>> PreloadTasks = [];
    static readonly Dictionary<object, (B_Object obj, object tag)> CachedObjects = [];

    public async Task<(B_Object obj, object tag)> CreateObject(ISource source, object path, bool isStatic, B_Object parent = default) {
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

    async Task<(B_Object obj, object tag)> LoadObject(ISource source, object path, bool isStatic, B_Object parent) {
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
public class Shader(Func<int, string, int> uniformLocation, Func<int, string, int> attribLocation) {
    readonly Func<int, string, int> _uniformLocation = uniformLocation ?? throw new ArgumentNullException(nameof(uniformLocation));
    readonly Func<int, string, int> _attribLocation = attribLocation ?? throw new ArgumentNullException(nameof(attribLocation));
    public string Name;
    public int Program;
    public Dictionary<string, bool> Parameters;
    public List<string> RenderModes;
    Dictionary<string, int> _uniforms = [];

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
    public abstract B_Shader CreateShader(object path, Dictionary<string, bool> args = null);
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

    public async Task<(B_Shader sha, object tag)> CreateShader(ISource source, object path, Dictionary<string, bool> args = null) => (Builder.CreateShader(path, args ?? EmptyArgs), null);
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
    public abstract B_Sprite DefaultSprite { get; }
    public abstract B_Sprite CreateSprite(ISprite spr);
    public abstract void DeleteSprite(B_Sprite spr);
}

/// <summary>
/// SpriteManager
/// </summary>
/// <typeparam name="Texture"></typeparam>
/// <param name="builder"></param>
public class SpriteManager<B_Sprite>(SpriteBuilder<B_Sprite> builder) {
    readonly SpriteBuilder<B_Sprite> Builder = builder;
    readonly Dictionary<object, Task<ISprite>> PreloadTasks = [];
    static readonly Dictionary<object, (B_Sprite spr, object tag)> CachedSprites = [];

    public B_Sprite DefaultSprite => Builder.DefaultSprite;

    public async Task<(B_Sprite spr, object tag)> CreateSprite(ISource source, object path) {
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
    public abstract B_Texture DefaultTexture { get; }
    public abstract B_Texture CreateNormalMapTexture(B_Texture src, float strength);
    public abstract B_Texture CreateSolidTexture(int width, int height, float[] rgbas);
    public abstract B_Texture CreateTexture(B_Texture reuse, ITexture src, Range? level = null);
    public abstract void DeleteTexture(B_Texture src);
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
    readonly Dictionary<object, Task<ITexture>> PreloadTasks = [];
    static readonly Dictionary<B_Texture, B_Texture> CachedNormalMapTextures = [];
    static readonly Dictionary<Solid, B_Texture> CachedSolidTextures = [];
    static readonly Dictionary<object, (B_Texture tex, object tag)> CachedTextures = [];
    public B_Texture DefaultTexture => Builder.DefaultTexture;
    const float NormalMapIntensity = 0.75f;

    public B_Texture CreateNormalMapTexture(B_Texture src, float strength = -1) {
        if (CachedNormalMapTextures.TryGetValue(src, out var s)) return s;
        s = Builder.CreateNormalMapTexture(src, strength < 0 ? NormalMapIntensity : strength);
        CachedNormalMapTextures[src] = s;
        return s;
    }

    public B_Texture CreateSolidTexture(int width, int height, float[] rgbas) {
        var src = new Solid(width, height, rgbas);
        if (CachedSolidTextures.TryGetValue(src, out var s)) return s;
        s = Builder.CreateSolidTexture(width, height, rgbas);
        CachedSolidTextures[src] = s;
        return s;
    }

    public async Task<(B_Texture tex, object tag)> CreateTexture(ISource source, object path, Range? level = null) {
        var key = (source, path);
        if (CachedTextures.TryGetValue(key, out var c)) return c;
        var tag = path is ITexture z ? z : await LoadTexture(source, path);
        var obj = tag != null ? Builder.CreateTexture(default, tag, level) : Builder.DefaultTexture;
        return CachedTextures[key] = (obj, tag);
    }

    public (B_Texture tex, object tag) ReloadTexture(ISource source, object path, Range? level = null) {
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
    public abstract B_Material DefaultMaterial { get; }
    public abstract B_Material TerrainMaterial { get; }
    public abstract Task<B_Material> CreateMaterial(ISource source, object path);
}

/// <summary>
/// Manages loading and instantiation of materials.
/// </summary>
public class MaterialManager<B_Material, B_Texture>(TextureManager<B_Texture> textureManager, MaterialBuilder<B_Material, B_Texture> builder) {
    readonly MaterialBuilder<B_Material, B_Texture> Builder = builder;
    readonly Dictionary<object, Task<MaterialProp>> PreloadTasks = [];
    static readonly Dictionary<object, (B_Material material, object tag)> CachedMaterials = [];

    public TextureManager<B_Texture> TextureManager { get; } = textureManager;
    public B_Material DefaultMaterial => Builder.DefaultMaterial;
    public B_Material TerrainMaterial => Builder.TerrainMaterial;

    public async Task<(B_Material mat, object tag)> CreateMaterial(ISource source, object path) {
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
    void PreloadSprite(ISource source, object path);
    Task<(B_Sprite spr, object tag)> CreateSprite(ISource source, object path, B_Object parent = default);
}

/// <summary>
/// IOpenGfxModel
/// </summary>
public interface IOpenGfxModel<B_Object, B_Material, B_Texture, B_Shader> : IOpenGfx {
    MaterialManager<B_Material, B_Texture> MaterialManager { get; }
    ObjectModelManager<B_Object, B_Material, B_Texture> ObjectManager { get; }
    ShaderManager<B_Shader> ShaderManager { get; }
    TextureManager<B_Texture> TextureManager { get; }
    void PreloadObject(ISource source, object path);
    void PreloadTexture(ISource source, object path);
    Task<(B_Object obj, object tag)> CreateObject(ISource source, object path, bool isStatic, B_Object parent = default);
    Task<(B_Shader sha, object tag)> CreateShader(ISource source, object path, Dictionary<string, bool> args = null);
    Task<(B_Texture tex, object tag)> CreateTexture(ISource source, object path, Range? level = null);
    void PostObject(B_Object src, Vector3 position, Vector3 eulerAngles, float? scale, B_Object parent = default);
}

/// <summary>
/// IOpenGfxLight
/// </summary>
public interface IOpenGfxLight<B_Object> : IOpenGfx {
    B_Object CreateLight(string name, Vector3? position, float radius, Color color, bool indoors, B_Object parent = default);
    B_Object CreateReflectionProbe(string name, Vector3? position, B_Object parent = default);
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
    object CreateTerrainData(int offset, float[,] heights, float heightRange, float sampleDistance, GfxTerrainLayer<B_Texture>[] layers, float[,,] alphaMap);
    B_Object CreateTerrain(string name, Vector3? position, object data, B_Object parent = default);
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