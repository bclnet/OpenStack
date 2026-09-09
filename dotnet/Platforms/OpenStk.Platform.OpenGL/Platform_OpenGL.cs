using OpenStk.Client;
using OpenStk.Gfx;
using OpenStk.Gfx.OpenGL;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static OpenStk.Gfx.TextureFormat;
#pragma warning disable CS0649, CS0169

[assembly: InternalsVisibleTo("OpenStk.GfxTests")]

namespace OpenStk;

#region Client

public class OpenGLClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}


#endregion

#region Platform

/// <summary>
/// OpenGLObjectBuilder
/// </summary>
class OpenGLObjectBuilder : ObjectModelBuilder<object, GLRenderMaterial, int> {
    public override object Instance(object prefab, object parent) {
        return "clone";
    }
    public async override Task<object> Create(ISource source, object path, bool isStatic, MaterialManager<GLRenderMaterial, int> materialManager) {
        var builder = OpenGLX.BuildersByType[path.GetType()];
        var s = await builder(source, path, isStatic, materialManager);
        return s;
    }
    public override void Ensure() { }
}

/// <summary>
/// OpenGLShaderBuilder
/// </summary>
class OpenGLShaderBuilder : ShaderBuilder<GfxShader> {
    const string ShaderDirectory = "OpenStk.Platform.OpenGL.Gfx.Shaders";

    protected static string GetShaderFileByName(string name) {
        switch (name) {
            case "plane": return "plane";
            case "testtri": return "testtri";
            case "vrf.error": return "error";
            case "vrf.grid": return "debug_grid";
            case "vrf.picking": return "picking";
            case "vrf.particle.sprite": return "particle_sprite";
            case "vrf.particle.trail": return "particle_trail";
            case "tools_sprite.vfx": return "sprite";
            case "vr_unlit.vfx": return "vr_unlit";
            case "vr_black_unlit.vfx": return "vr_black_unlit";
            case "water_dota.vfx": return "water";
            case "hero.vfx":
            case "hero_underlords.vfx": return "dota_hero";
            case "multiblend.vfx": return "multiblend";
            default:
                if (name.StartsWith("vr_")) return "vr_standard";
                Log.Warn($"Unknown shader {name}, defaulting to simple.");
                return "simple";
        }
    }

    protected static string GetShaderSource(string name) {
#if DEBUG_SHADERS && DEBUG
        var path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName), "../../../../", ShaderDirectory.Replace(".", "/"), name);
        var stream = File.Open(path, FileMode.Open);
#else
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{ShaderDirectory}.{name}");
#endif
        using var r = new StreamReader(stream); return r.ReadToEnd();
    }

    public override GfxShader Create(object path, Dictionary<string, bool> args = null) {
        var name = (string)path;
        var shaderFileName = GetShaderFileByName(name);

        // defines
        List<string> defines = [];

        // vertex shader
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        {
            var shaderSource = GetShaderSource($"{shaderFileName}.vert");
            GL.ShaderSource(vertexShader, PreprocessVertexShader(shaderSource, args));
            // defines: find defines supported from source
            defines.AddRange(FindDefines(shaderSource));
        }
        GL.CompileShader(vertexShader);
        GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus, out var shaderStatus);
        if (shaderStatus != 1) {
            GL.GetShaderInfoLog(vertexShader, out var vsInfo);
            throw new Exception($"Error setting up Vertex B_Shader \"{name}\": {vsInfo}");
        }

        // fragment shader
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        {
            var shaderSource = GetShaderSource($"{shaderFileName}.frag");
            GL.ShaderSource(fragmentShader, UpdateDefines(shaderSource, args));
            // defines: find render modes supported from source, take union to avoid duplicates
            defines = [.. defines.Union(FindDefines(shaderSource))];
        }
        GL.CompileShader(fragmentShader);
        GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus, out shaderStatus);
        if (shaderStatus != 1) {
            GL.GetShaderInfoLog(fragmentShader, out var fsInfo);
            throw new Exception($"Error setting up Fragment B_Shader \"{name}\": {fsInfo}");
        }

        // defines: find render modes
        const string RenderMode = "renderMode_"; const int RenderModeLength = 11;
        var renderModes = defines.Where(k => k.StartsWith(RenderMode)).Select(k => k[RenderModeLength..]).ToList();

        // build shader
        var shader = new GfxShader(GL.GetUniformLocation, GL.GetAttribLocation) {
            Name = name,
            Parameters = args,
            Program = GL.CreateProgram(),
            RenderModes = renderModes,
        };
        GL.AttachShader(shader.Program, vertexShader);
        GL.AttachShader(shader.Program, fragmentShader);
        GL.LinkProgram(shader.Program);
        GL.ValidateProgram(shader.Program);
        GL.GetProgrami(shader.Program, ProgramProperty.LinkStatus, out var linkStatus);
        GL.DetachShader(shader.Program, vertexShader);
        GL.DeleteShader(vertexShader);
        GL.DetachShader(shader.Program, fragmentShader);
        GL.DeleteShader(fragmentShader);
        if (linkStatus != 1) {
            GL.GetProgramInfoLog(shader.Program, out var linkInfo);
            throw new Exception($"Error linking shaders: {linkInfo} (link status = {linkStatus})");
        }
        //Log.Info($"B_Shader {name}({string.Join(", ", args.Keys)}) compiled and linked succesfully");
        return shader;
    }

    // Preprocess a vertex shader's source to include the #version plus #defines for parameters
    static string PreprocessVertexShader(string source, IDictionary<string, bool> args) => ResolveIncludes(UpdateDefines(source, args));

    // Update default defines with possible overrides from the model
    static string UpdateDefines(string source, IDictionary<string, bool> args) {
        // find all #define param_(paramName) (paramValue) using regex
        var defines = Regex.Matches(source, @"#define param_(\S*?) (\S*?)\s*?\n");
        foreach (Match define in defines)
            if (args.TryGetValue(define.Groups[1].Value, out var value)) { var index = define.Groups[2].Index; var length = define.Groups[2].Length; source = source.Remove(index, Math.Min(length, source.Length - index)).Insert(index, value ? "1" : "0"); }
        return source;
    }

    // Remove any #includes from the shader and replace with the included code
    static string ResolveIncludes(string source) {
        var includes = Regex.Matches(source, @"#include ""([^""]*?)"";?\s*\n");
        foreach (Match define in includes) {
            var includedCode = GetShaderSource(define.Groups[1].Value);
            // recursively resolve includes in the included code. (Watch out for cyclic dependencies!)
            includedCode = ResolveIncludes(includedCode);
            if (!includedCode.EndsWith('\n')) includedCode += "\n";
            source = source.Replace(define.Value, includedCode);
        }
        return source;
    }

    static List<string> FindDefines(string source) { var defines = Regex.Matches(source, @"#define param_(\S+)"); return [.. defines.Cast<Match>().Select(_ => _.Groups[1].Value)]; }
}

/// <summary>
/// OpenGLTextureBuilder
/// </summary>
unsafe class OpenGLTextureBuilder : TextureBuilder<int> {
    static int _defaultTexture = -1;
    public override int Default => _defaultTexture > -1 ? _defaultTexture : _defaultTexture = CreateDefaultTexture();

    public void Release() {
        if (_defaultTexture > -1) { GL.DeleteTexture(_defaultTexture); _defaultTexture = -1; }
    }

    int CreateDefaultTexture() => CreateSolid(4, 4, [
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,

        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,

        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,

        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
    ]);

    public override int CreateNormalMap(int src, float strength) => 0;

    public override int CreateSolid(int width, int height, float[] pixels) {
        var tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, tex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba32f, width, height, 0, PixelFormat.Rgba, PixelType.Float, pixels);
        GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.BindTexture(TextureTarget.Texture2D, 0); // release texture
        return tex;
    }

    public override int Create(int reuse, ITexture src, Range? level2 = null) => src.Create("GL", x => {
        switch (x) {
            case TextureAsBytes t:
                var tex = reuse != 0 ? reuse : GL.GenTexture();
                var numMipMaps = Math.Max(1, src.MipMaps);
                (int start, int stop) level = (level2?.Start.Value ?? 0, numMipMaps);
                // bind
                GL.BindTexture(TextureTarget.Texture2D, tex);
                if (level.start > 0) GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, level.start);
                GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, level.stop - 1);
                var (bytes, fmt, spans) = (t.Bytes, t.Format, t.Spans);
                // decode
                bool CompressedTexImage2D(ITexture tex, (int start, int stop) level, InternalFormat internalFormat) {
                    int width = tex.Width, height = tex.Height;
                    if (spans != null)
                        for (var l = level.start; l < level.stop; l++) {
                            var span = spans[l];
                            if (span.Start.Value < 0) return false;
                            var pixels = bytes.AsSpan(span);
                            fixed (byte* data = pixels) GL.CompressedTexImage2D(TextureTarget.Texture2D, l, internalFormat, width >> l, height >> l, 0, pixels.Length, (IntPtr)data);
                        }
                    else fixed (byte* data = bytes) GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, bytes.Length, (IntPtr)data);
                    return true;
                }
                bool TexImage2D(ITexture tex, (int start, int stop) level, InternalFormat internalFormat, PixelFormat format, PixelType type) {
                    int width = tex.Width, height = tex.Height;
                    if (spans != null)
                        for (var l = level.start; l < level.stop; l++) {
                            var span = spans[l];
                            if (span.Start.Value < 0) return false;
                            var pixels = bytes.AsSpan(span);
                            fixed (byte* data = pixels) GL.TexImage2D(TextureTarget.Texture2D, l, internalFormat, width >> l, height >> l, 0, format, type, (IntPtr)data);
                        }
                    else fixed (byte* data = bytes) GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, type, (IntPtr)data);
                    return true;
                }
                // process
                if (bytes == null) return Default;
                else if (fmt is ValueTuple<TextureFormat, TexturePixel> z) {
                    var (formatx, pixel) = z;
                    var s = (pixel & TexturePixel.Signed) != 0;
                    var f = (pixel & TexturePixel.Float) != 0;
                    if ((formatx & Compressed) != 0) {
                        var internalFormat = formatx switch {
                            DXT1 => s ? InternalFormat.CompressedSrgbS3tcDxt1Ext : InternalFormat.CompressedRgbS3tcDxt1Ext,
                            DXT1A => s ? InternalFormat.CompressedSrgbAlphaS3tcDxt1Ext : InternalFormat.CompressedRgbaS3tcDxt1Ext,
                            DXT3 => s ? InternalFormat.CompressedSrgbAlphaS3tcDxt3Ext : InternalFormat.CompressedRgbaS3tcDxt3Ext,
                            DXT5 => s ? InternalFormat.CompressedSrgbAlphaS3tcDxt5Ext : InternalFormat.CompressedRgbaS3tcDxt5Ext,
                            BC4 => s ? InternalFormat.CompressedSignedRedRgtc1 : InternalFormat.CompressedRedRgtc1,
                            BC5 => s ? InternalFormat.CompressedSignedRgRgtc2 : InternalFormat.CompressedRgRgtc2,
                            BC6H => s ? InternalFormat.CompressedRgbBptcSignedFloat : InternalFormat.CompressedRgbBptcUnsignedFloat,
                            BC7 => s ? InternalFormat.CompressedSrgbAlphaBptcUnorm : InternalFormat.CompressedRgbaBptcUnorm,
                            ETC2 => s ? InternalFormat.CompressedSrgb8Etc2 : InternalFormat.CompressedRgb8Etc2,
                            ETC2_EAC => s ? InternalFormat.CompressedSrgb8Alpha8Etc2Eac : InternalFormat.CompressedRgba8Etc2Eac,
                            _ => throw new ArgumentOutOfRangeException("TextureFormat", $"{formatx}")
                        };
                        if (internalFormat == 0 || !CompressedTexImage2D(src, level, internalFormat)) return Default;
                    }
                    else {
                        var (internalFormat, format, type) = formatx switch {
                            I8 => (InternalFormat.Intensity8Ext, PixelFormat.Red, PixelType.UnsignedByte),
                            L8 => ((InternalFormat)6409, (PixelFormat)6409, PixelType.UnsignedByte),
                            R8 => (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
                            R16 => f ? (InternalFormat.R16f, PixelFormat.Red, PixelType.Float) : (InternalFormat.R16, PixelFormat.Red, PixelType.UnsignedShort),
                            RG16 => f ? (InternalFormat.Rg16f, PixelFormat.Red, PixelType.Float) : (InternalFormat.Rg16, PixelFormat.Red, PixelType.UnsignedShort),
                            RGB24 => (InternalFormat.Rgb8, PixelFormat.Rgb, PixelType.UnsignedByte),
                            RGB565 => (InternalFormat.Rgb5, PixelFormat.Rgb, PixelType.UnsignedByte), //UnsignedShort565
                            RGBA32 => (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte),
                            ARGB32 => (InternalFormat.Rgba, PixelFormat.Rgb, PixelType.UnsignedInt8888Rev), //: odd Rgb
                            BGRA32 => (InternalFormat.Rgba, PixelFormat.Bgra, PixelType.UnsignedInt8888),
                            BGRA1555 => (InternalFormat.Rgba, PixelFormat.Bgra, PixelType.UnsignedShort1555Rev),
                            _ => throw new ArgumentOutOfRangeException("TextureFormat", $"{formatx}")
                        };
                        if (internalFormat == 0 || !TexImage2D(src, level, internalFormat, format, type)) return Default;
                    }
                }
                else throw new ArgumentOutOfRangeException(nameof(fmt), $"{fmt}");
                // texture
                if (MaxTextureMaxAnisotropy >= 4) {
                    GL.TexParameteri(TextureTarget.Texture2D, (TextureParameterName)All.MaxTextureMaxAnisotropyExt, MaxTextureMaxAnisotropy);
                    GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                }
                else {
                    GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                }
                GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)(src.TexFlags.HasFlag(TextureFlags.SUGGEST_CLAMPS) ? TextureWrapMode.ClampToEdge : TextureWrapMode.Repeat));
                GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)(src.TexFlags.HasFlag(TextureFlags.SUGGEST_CLAMPT) ? TextureWrapMode.ClampToEdge : TextureWrapMode.Repeat));
                GL.BindTexture(TextureTarget.Texture2D, 0); // release texture
                return tex;
            default: throw new ArgumentOutOfRangeException(nameof(x), $"{x}");
        }
    });

    public override void Delete(int src) => GL.DeleteTexture(src);
}

/// <summary>
/// OpenGLMaterialBuilder
/// </summary>
class OpenGLMaterialBuilder(TextureManager<int> textureManager) : MaterialBuilder<GLRenderMaterial, int>(textureManager) {
    static GLRenderMaterial _defaultMaterial, _terrainMaterial;
    public override GLRenderMaterial Default => _defaultMaterial ??= CreateDefaultMaterial();
    public override GLRenderMaterial Terrain => _terrainMaterial ??= CreateTerrainMaterial();

    GLRenderMaterial CreateDefaultMaterial() {
        var m = new GLRenderMaterial(new MaterialShaderProp());
        m.Textures["g_tColor"] = TextureManager.Default;
        m.Material.ShaderName = "vrf.error";
        return m;
    }

    GLRenderMaterial CreateTerrainMaterial() {
        var m = new GLRenderMaterial(new MaterialShaderProp());
        m.Material.ShaderName = "vrf.error";
        return m;
    }

    public override async Task<GLRenderMaterial> Create(ISource source, object path) {
        var m = new GLRenderMaterial(path as MaterialShaderProp);
        switch (path) {
            case MaterialShaderVProp p:
                foreach (var tex in p.TextureParams) (m.Textures[tex.Key], _) = await TextureManager.Create(source, $"{tex.Value}_c");
                if (p.IntParams.ContainsKey("F_SOLID_COLOR") && p.IntParams["F_SOLID_COLOR"] == 1) {
                    var a = p.VectorParams["g_vColorTint"];
                    m.Textures["g_tColor"] = TextureManager.CreateSolid(1, 1, [a.X, a.Y, a.Z, a.W]);
                }
                if (!m.Textures.ContainsKey("g_tColor")) m.Textures["g_tColor"] = TextureManager.Default;

                // Since our shaders only use g_tColor, we have to find at least one texture to use here
                if (m.Textures["g_tColor"] == TextureManager.Default)
                    foreach (var name in new[] { "g_tColor2", "g_tColor1", "g_tColorA", "g_tColorB", "g_tColorC" })
                        if (m.Textures.ContainsKey(name)) {
                            m.Textures["g_tColor"] = m.Textures[name];
                            break;
                        }

                // Set default values for scale and positions
                if (!p.VectorParams.ContainsKey("g_vTexCoordScale")) p.VectorParams["g_vTexCoordScale"] = Vector4.One;
                if (!p.VectorParams.ContainsKey("g_vTexCoordOffset")) p.VectorParams["g_vTexCoordOffset"] = Vector4.Zero;
                if (!p.VectorParams.ContainsKey("g_vColorTint")) p.VectorParams["g_vColorTint"] = Vector4.One;
                return m;
            //case MaterialShaderProp s: return m;
            default: throw new ArgumentOutOfRangeException(nameof(path));
        }
    }
}

// OpenGLGfxApi
public class OpenGLGfxApi : IOpenGfxApi<object, GLRenderMaterial> {
    public void AddMeshCollider(object src, object mesh, bool isKinematic, bool isStatic) => throw new NotImplementedException();
    public void AddMeshRenderer(object src, object mesh, GLRenderMaterial material, bool enabled, bool isStatic) => throw new NotImplementedException();
    public void AddMissingMeshCollidersRecursively(object src, bool isStatic) => throw new NotImplementedException();
    public void Attach(GfxAttach method, object src, params object[] args) { }
    public object CreateMesh(object mesh) => throw new NotImplementedException();
    public object CreateObject(string name, string tag = null, object parent = null) => new();
    public void SetLayerRecursively(object src, int layer) => throw new NotImplementedException();
    public void Parent(object src, object parent) => throw new NotImplementedException();
    public void Transform(object src, Vector3 position, Quaternion rotation, Vector3 localScale) => throw new NotImplementedException();
    public void Transform(object src, Vector3 position, Matrix4x4 rotation, Vector3 localScale) => throw new NotImplementedException();
    public void SetVisible(object src, bool visible) { }
    public void Destroy(object src) { }
}

// OpenGLGfxSprite3D
public class OpenGLGfxSprite3D : IOpenGfxSprite<object, int> {
    readonly SpriteManager<int> _spriteManager;
    public OpenGLGfxSprite3D() {
        //_spriteManager = new SpriteManager<int>(source, new OpenGLSpriteBuilder());
    }

    public SpriteManager<int> SpriteManager => _spriteManager;
    public void Preload(ISource source, object path) => _spriteManager.Preload(source, path);
    public Task<(int spr, object tag)> Create(ISource source, object path, object parent = default) => _spriteManager.Create(source, path);
}

/// <summary>
/// OpenGLGfxModel
/// </summary>
public class OpenGLGfxModel : IOpenGfxModel<object, GLRenderMaterial, int, GfxShader> {
    readonly MaterialManager<GLRenderMaterial, int> _materialManager;
    readonly ObjectModelManager<object, GLRenderMaterial, int> _objectManager;
    readonly ShaderManager<GfxShader> _shaderManager;
    readonly TextureManager<int> _textureManager;
    public OpenGLGfxModel() {
        _textureManager = new TextureManager<int>(new OpenGLTextureBuilder());
        _materialManager = new MaterialManager<GLRenderMaterial, int>(_textureManager, new OpenGLMaterialBuilder(_textureManager));
        _objectManager = new ObjectModelManager<object, GLRenderMaterial, int>(_materialManager, new OpenGLObjectBuilder());
        _shaderManager = new ShaderManager<GfxShader>(new OpenGLShaderBuilder());
        MeshBufferCache = new GLMeshBufferCache();
    }

    public MaterialManager<GLRenderMaterial, int> MaterialManager => _materialManager;
    public ObjectModelManager<object, GLRenderMaterial, int> ObjectManager => _objectManager;
    public ShaderManager<GfxShader> ShaderManager => _shaderManager;
    public TextureManager<int> TextureManager => _textureManager;
    public void Preload(ISource source, object path) => _objectManager.Preload(source, path);
    public void PreloadTexture(ISource source, object path) => _textureManager.Preload(source, path);
    public Task<(object obj, object tag)> Create(ISource source, object path, bool isStatic, object parent = default) => _objectManager.Create(source, path, isStatic, parent);
    public Task<(GfxShader sha, object tag)> CreateShader(ISource source, object path, Dictionary<string, bool> args = null) => _shaderManager.Create(source, path, args);
    public Task<(int tex, object tag)> CreateTexture(ISource source, object path, Range? level = null) => _textureManager.Create(source, path, level);
    public void Post(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent) { }

    // cache
    QuadIndexBuffer _quadIndices;
    public QuadIndexBuffer QuadIndices => _quadIndices ??= new QuadIndexBuffer(65532);
    public GLMeshBufferCache MeshBufferCache { get; }
}

/// <summary>
/// OpenGLGfxTerrain
/// </summary>
public class OpenGLGfxTerrain : IOpenGfxTerrain<object, GLRenderMaterial, int> {
    readonly MaterialManager<GLRenderMaterial, int> _materialManager;
    readonly TextureManager<int> _textureManager;
    public OpenGLGfxTerrain() {
        _textureManager = new TextureManager<int>(new OpenGLTextureBuilder());
        _materialManager = new MaterialManager<GLRenderMaterial, int>(_textureManager, new OpenGLMaterialBuilder(_textureManager));
    }

    public object CreateData(int offset, float[,] heights, float heightRange, float sampleDistance, GfxTerrainLayer<int>[] layers, float[,,] alphaMap) => new();
    public object Create(string name, Vector3? position, object data, object parent = default) => null;
}

/// <summary>
/// OpenGLPlatform
/// </summary>
public class OpenGLPlatform : Platform {
    public static readonly Platform This = new OpenGLPlatform();
    OpenGLPlatform() : base("GL", "OpenGL") {
        Caps = PlatformX.Caps.Drawing;
        GfxFactory = () => [new OpenGLGfxApi(), null, new OpenGLGfxSprite3D(), new OpenGLGfxModel(), null, new OpenGLGfxTerrain()];
        SfxFactory = () => [new SystemSfx()];
    }
}

#endregion
