//#define DEBUG_SHADERS
using OpenStack.Gfx;
using OpenStack.Gfx.Algorithms;
using OpenStack.Gfx.Texture;
using OpenStack.Gl.Render;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static OpenStack.Debug;
using static OpenStack.Gfx.Texture.TextureFormat;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Gl;

/// <summary>
/// OpenGLExtensions
/// </summary>
public static class OpenGLExtensions
{
    public static OpenTK.Vector3 ToOpenTK(this Vector3 vec) => new(vec.X, vec.Y, vec.Z);
    public static OpenTK.Vector4 ToOpenTK(this Vector4 vec) => new(vec.X, vec.Y, vec.Z, vec.W);
    public static OpenTK.Matrix4 ToOpenTK(this Matrix4x4 m) => new(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);
}

// RenderPrimitiveType
public enum RenderPrimitiveType
{
    RENDER_PRIM_POINTS = 0x0,
    RENDER_PRIM_LINES = 0x1,
    RENDER_PRIM_LINES_WITH_ADJACENCY = 0x2,
    RENDER_PRIM_LINE_STRIP = 0x3,
    RENDER_PRIM_LINE_STRIP_WITH_ADJACENCY = 0x4,
    RENDER_PRIM_TRIANGLES = 0x5,
    RENDER_PRIM_TRIANGLES_WITH_ADJACENCY = 0x6,
    RENDER_PRIM_TRIANGLE_STRIP = 0x7,
    RENDER_PRIM_TRIANGLE_STRIP_WITH_ADJACENCY = 0x8,
    RENDER_PRIM_INSTANCED_QUADS = 0x9,
    RENDER_PRIM_HETEROGENOUS = 0xA,
    RENDER_PRIM_1_CONTROL_POINT_PATCHLIST = 0xB,
    RENDER_PRIM_2_CONTROL_POINT_PATCHLIST = 0xC,
    RENDER_PRIM_3_CONTROL_POINT_PATCHLIST = 0xD,
    RENDER_PRIM_4_CONTROL_POINT_PATCHLIST = 0xE,
    RENDER_PRIM_5_CONTROL_POINT_PATCHLIST = 0xF,
    RENDER_PRIM_6_CONTROL_POINT_PATCHLIST = 0x10,
    RENDER_PRIM_7_CONTROL_POINT_PATCHLIST = 0x11,
    RENDER_PRIM_8_CONTROL_POINT_PATCHLIST = 0x12,
    RENDER_PRIM_9_CONTROL_POINT_PATCHLIST = 0x13,
    RENDER_PRIM_10_CONTROL_POINT_PATCHLIST = 0x14,
    RENDER_PRIM_11_CONTROL_POINT_PATCHLIST = 0x15,
    RENDER_PRIM_12_CONTROL_POINT_PATCHLIST = 0x16,
    RENDER_PRIM_13_CONTROL_POINT_PATCHLIST = 0x17,
    RENDER_PRIM_14_CONTROL_POINT_PATCHLIST = 0x18,
    RENDER_PRIM_15_CONTROL_POINT_PATCHLIST = 0x19,
    RENDER_PRIM_16_CONTROL_POINT_PATCHLIST = 0x1A,
    RENDER_PRIM_17_CONTROL_POINT_PATCHLIST = 0x1B,
    RENDER_PRIM_18_CONTROL_POINT_PATCHLIST = 0x1C,
    RENDER_PRIM_19_CONTROL_POINT_PATCHLIST = 0x1D,
    RENDER_PRIM_20_CONTROL_POINT_PATCHLIST = 0x1E,
    RENDER_PRIM_21_CONTROL_POINT_PATCHLIST = 0x1F,
    RENDER_PRIM_22_CONTROL_POINT_PATCHLIST = 0x20,
    RENDER_PRIM_23_CONTROL_POINT_PATCHLIST = 0x21,
    RENDER_PRIM_24_CONTROL_POINT_PATCHLIST = 0x22,
    RENDER_PRIM_25_CONTROL_POINT_PATCHLIST = 0x23,
    RENDER_PRIM_26_CONTROL_POINT_PATCHLIST = 0x24,
    RENDER_PRIM_27_CONTROL_POINT_PATCHLIST = 0x25,
    RENDER_PRIM_28_CONTROL_POINT_PATCHLIST = 0x26,
    RENDER_PRIM_29_CONTROL_POINT_PATCHLIST = 0x27,
    RENDER_PRIM_30_CONTROL_POINT_PATCHLIST = 0x28,
    RENDER_PRIM_31_CONTROL_POINT_PATCHLIST = 0x29,
    RENDER_PRIM_32_CONTROL_POINT_PATCHLIST = 0x2A,
}

/// <summary>
/// OpenGLObjectBuilder
/// </summary>
public class OpenGLObjectBuilder : Object3dBuilderBase<object, GLRenderMaterial, int>
{
    public override void EnsurePrefab() { }
    public override object CreateNewObject(object prefab) => throw new NotImplementedException();
    public override object CreateObject(object path, IMaterialManager<GLRenderMaterial, int> materialManager) => throw new NotImplementedException();
}

/// <summary>
/// OpenGLShaderBuilder
/// </summary>
public class OpenGLShaderBuilder : ShaderBuilderBase<Shader>
{
    static readonly ShaderLoader _loader = new ShaderDebugLoader();
    public override Shader CreateShader(object path, IDictionary<string, bool> args = null) => _loader.CreateShader(path, args);
}

/// <summary>
/// OpenGLTextureBuilder
/// </summary>
public unsafe class OpenGLTextureBuilder : TextureBuilderBase<int>
{
    int _defaultTexture = -1;
    public override int DefaultTexture => _defaultTexture > -1 ? _defaultTexture : _defaultTexture = CreateDefaultTexture();

    public void Release()
    {
        if (_defaultTexture > -1) { GL.DeleteTexture(_defaultTexture); _defaultTexture = -1; }
    }

    int CreateDefaultTexture() => CreateSolidTexture(4, 4, [
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

    public override int CreateTexture(int reuse, ITexture source, Range? level2 = null)
    {
        var id = reuse != 0 ? reuse : GL.GenTexture();
        var numMipMaps = Math.Max(1, source.MipMaps);
        (int start, int stop) level = (level2?.Start.Value ?? 0, numMipMaps);

        // bind
        GL.BindTexture(TextureTarget.Texture2D, id);
        if (level.start > 0) GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, level.start);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, level.stop - 1);
        var (bytes, fmt, spans) = source.Begin("GL");

        // decode
        bool CompressedTexImage2D(ITexture source, (int start, int stop) level, InternalFormat internalFormat)
        {
            int width = source.Width, height = source.Height;
            if (spans != null)
                for (var l = level.start; l < level.stop; l++)
                {
                    var span = spans[l];
                    if (span.Start.Value < 0) return false;
                    var pixels = bytes.AsSpan(span);
                    fixed (byte* data = pixels) GL.CompressedTexImage2D(TextureTarget.Texture2D, l, internalFormat, width >> l, height >> l, 0, pixels.Length, (IntPtr)data);
                }
            else fixed (byte* data = bytes) GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, bytes.Length, (IntPtr)data);
            return true;
        }
        bool TexImage2D(ITexture source, (int start, int stop) level, PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
        {
            int width = source.Width, height = source.Height;
            if (spans != null)
                for (var l = level.start; l < level.stop; l++)
                {
                    var span = spans[l];
                    if (span.Start.Value < 0) return false;
                    var pixels = bytes.AsSpan(span);
                    fixed (byte* data = pixels) GL.TexImage2D(TextureTarget.Texture2D, l, internalFormat, width >> l, height >> l, 0, format, type, (IntPtr)data);
                }
            else fixed (byte* data = bytes) GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, type, (IntPtr)data);
            return true;
        }

        try
        {
            if (bytes == null) return DefaultTexture;
            else if (fmt is ValueTuple<TextureFormat, TexturePixel> z)
            {
                var (formatx, pixel) = z;
                var s = (pixel & TexturePixel.Signed) != 0;
                var f = (pixel & TexturePixel.Float) != 0;
                if ((formatx & Compressed) != 0)
                {
                    var internalFormat = formatx switch
                    {
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
                    if (internalFormat == 0 || !CompressedTexImage2D(source, level, internalFormat)) return DefaultTexture;
                }
                else
                {
                    var (internalFormat, format, type) = formatx switch
                    {
                        I8 => (PixelInternalFormat.Intensity8, PixelFormat.Red, PixelType.UnsignedByte),
                        L8 => (PixelInternalFormat.Luminance, PixelFormat.Luminance, PixelType.UnsignedByte),
                        R8 => (PixelInternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
                        R16 => f ? (PixelInternalFormat.R16f, PixelFormat.Red, PixelType.Float) : (PixelInternalFormat.R16, PixelFormat.Red, PixelType.UnsignedShort),
                        RG16 => f ? (PixelInternalFormat.Rg16f, PixelFormat.Red, PixelType.Float) : (PixelInternalFormat.Rg16, PixelFormat.Red, PixelType.UnsignedShort),
                        RGB24 => (PixelInternalFormat.Rgb8, PixelFormat.Rgb, PixelType.UnsignedByte),
                        RGB565 => (PixelInternalFormat.Rgb5, PixelFormat.Rgb, PixelType.UnsignedByte), //UnsignedShort565
                        RGBA32 => (PixelInternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte),
                        ARGB32 => (PixelInternalFormat.Rgba, PixelFormat.Rgb, PixelType.UnsignedInt8888Reversed), //: odd Rgb
                        BGRA32 => (PixelInternalFormat.Rgba, PixelFormat.Bgra, PixelType.UnsignedInt8888),
                        BGRA1555 => (PixelInternalFormat.Rgba, PixelFormat.Bgra, PixelType.UnsignedShort1555Reversed),
                        _ => throw new ArgumentOutOfRangeException("TextureFormat", $"{formatx}")
                    };
                    if (internalFormat == 0 || !TexImage2D(source, level, internalFormat, format, type)) return DefaultTexture;
                }
            }
            else throw new ArgumentOutOfRangeException(nameof(fmt), $"{fmt}");

            // texture
            if (MaxTextureMaxAnisotropy >= 4)
            {
                GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, MaxTextureMaxAnisotropy);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)(source.TexFlags.HasFlag(TextureFlags.SUGGEST_CLAMPS) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)(source.TexFlags.HasFlag(TextureFlags.SUGGEST_CLAMPT) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat));
            GL.BindTexture(TextureTarget.Texture2D, 0); // release texture
            return id;
        }
        finally { source.End(); }
    }

    public override int CreateSolidTexture(int width, int height, float[] pixels)
    {
        var id = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, id);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, width, height, 0, PixelFormat.Rgba, PixelType.Float, pixels);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.BindTexture(TextureTarget.Texture2D, 0); // release texture
        return id;
    }

    public override int CreateNormalMap(int texture, float strength) => throw new NotImplementedException();

    public override void DeleteTexture(int texture) => GL.DeleteTexture(texture);
}

/// <summary>
/// OpenGLMaterialBuilder
/// </summary>
public class OpenGLMaterialBuilder(TextureManager<int> textureManager) : MaterialBuilderBase<GLRenderMaterial, int>(textureManager)
{
    GLRenderMaterial _defaultMaterial;
    public override GLRenderMaterial DefaultMaterial => _defaultMaterial ??= CreateDefaultMaterial(-1);

    GLRenderMaterial CreateDefaultMaterial(int type)
    {
        var m = new GLRenderMaterial(null);
        m.Textures["g_tColor"] = TextureManager.DefaultTexture;
        m.Material.ShaderName = "vrf.error";
        return m;
    }

    public override GLRenderMaterial CreateMaterial(object key)
    {
        var m = new GLRenderMaterial(key as MaterialPropShader);
        switch (key)
        {
            //case IFixedMaterial _: return m;
            case MaterialPropShaderV p:
                foreach (var tex in p.TextureParams) m.Textures[tex.Key] = TextureManager.CreateTexture($"{tex.Value}_c").tex;
                if (p.IntParams.ContainsKey("F_SOLID_COLOR") && p.IntParams["F_SOLID_COLOR"] == 1)
                {
                    var a = p.VectorParams["g_vColorTint"];
                    m.Textures["g_tColor"] = TextureManager.CreateSolidTexture(1, 1, a.X, a.Y, a.Z, a.W);
                }
                if (!m.Textures.ContainsKey("g_tColor")) m.Textures["g_tColor"] = TextureManager.DefaultTexture;

                // Since our shaders only use g_tColor, we have to find at least one texture to use here
                if (m.Textures["g_tColor"] == TextureManager.DefaultTexture)
                    foreach (var name in new[] { "g_tColor2", "g_tColor1", "g_tColorA", "g_tColorB", "g_tColorC" })
                        if (m.Textures.ContainsKey(name))
                        {
                            m.Textures["g_tColor"] = m.Textures[name];
                            break;
                        }

                // Set default values for scale and positions
                if (!p.VectorParams.ContainsKey("g_vTexCoordScale")) p.VectorParams["g_vTexCoordScale"] = Vector4.One;
                if (!p.VectorParams.ContainsKey("g_vTexCoordOffset")) p.VectorParams["g_vTexCoordOffset"] = Vector4.Zero;
                if (!p.VectorParams.ContainsKey("g_vColorTint")) p.VectorParams["g_vColorTint"] = Vector4.One;
                return m;
            default: throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
}

/// <summary>
/// IOpenGLGfx3d
/// </summary>
public interface IOpenGLGfx3d : IOpenGfx3dAny<object, GLRenderMaterial, int, Shader>
{
    // cache
    public GLMeshBufferCache MeshBufferCache { get; }
    public QuadIndexBuffer QuadIndices { get; }
}

/// <summary>
/// OpenGLGfx3d
/// </summary>
public class OpenGLGfx3d : IOpenGLGfx3d
{
    readonly ISource _source;
    readonly TextureManager<int> _textureManager;
    readonly MaterialManager<GLRenderMaterial, int> _materialManager;
    readonly Object3dManager<object, GLRenderMaterial, int> _objectManager;
    readonly ShaderManager<Shader> _shaderManager;

    public OpenGLGfx3d(ISource source)
    {
        _source = source;
        _textureManager = new TextureManager<int>(source, new OpenGLTextureBuilder());
        _materialManager = new MaterialManager<GLRenderMaterial, int>(source, _textureManager, new OpenGLMaterialBuilder(_textureManager));
        _objectManager = new Object3dManager<object, GLRenderMaterial, int>(source, _materialManager, new OpenGLObjectBuilder());
        _shaderManager = new ShaderManager<Shader>(source, new OpenGLShaderBuilder());
        MeshBufferCache = new GLMeshBufferCache();
    }

    public ISource Source => _source;
    public ITextureManager<int> TextureManager => _textureManager;
    public IMaterialManager<GLRenderMaterial, int> MaterialManager => _materialManager;
    public IObject3dManager<object, GLRenderMaterial, int> ObjectManager => _objectManager;
    public IShaderManager<Shader> ShaderManager => _shaderManager;
    public int CreateTexture(object path, Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => _textureManager.PreloadTexture(path);
    public object CreateObject(object path) => _objectManager.CreateObject(path).obj;
    public void PreloadObject(object path) => _objectManager.PreloadObject(path);
    public Shader CreateShader(object path, IDictionary<string, bool> args = null) => _shaderManager.CreateShader(path, args).sha;
    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);

    // cache
    QuadIndexBuffer _quadIndices;
    public QuadIndexBuffer QuadIndices => _quadIndices ??= new QuadIndexBuffer(65532);
    public GLMeshBufferCache MeshBufferCache { get; }
}

/// <summary>
/// OpenGLPlatform
/// </summary>
public class OpenGLPlatform : Platform
{
    public static readonly Platform This = new OpenGLPlatform();
    OpenGLPlatform() : base("GL", "OpenGL")
    {
        GfxFactory = source => false ? null : new OpenGLGfx3d(source);
        SfxFactory = source => new SystemSfx(source);
    }
}

#region Shaders

/// <summary>
/// ShaderLoader
/// </summary>
public abstract class ShaderLoader
{
    const int ShaderSeed = 0x13141516;

    readonly Dictionary<uint, Shader> CachedShaders = [];
    readonly Dictionary<string, List<string>> ShaderDefines = [];

    uint CalculateShaderCacheHash(string name, IDictionary<string, bool> args)
    {
        var b = new StringBuilder(); b.AppendLine(name);
        var parameters = ShaderDefines[name].Intersect(args.Keys);
        foreach (var key in parameters)
        {
            b.AppendLine(key);
            b.AppendLine(args[key] ? "t" : "f");
        }
        return MurmurHash2.Hash(b.ToString(), ShaderSeed);
    }

    protected abstract string GetShaderFileByName(string name);

    protected abstract string GetShaderSource(string name);

    public Shader CreateShader(object path, IDictionary<string, bool> args)
    {
        var name = (string)path;
        var cache = !name.StartsWith("#");
        var shaderFileName = GetShaderFileByName(name);

        // cache
        if (cache && ShaderDefines.ContainsKey(shaderFileName))
        {
            var shaderCacheHash = CalculateShaderCacheHash(shaderFileName, args);
            if (CachedShaders.TryGetValue(shaderCacheHash, out var c)) return c;
        }

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
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out var shaderStatus);
        if (shaderStatus != 1)
        {
            GL.GetShaderInfoLog(vertexShader, out var vsInfo);
            throw new Exception($"Error setting up Vertex Shader \"{name}\": {vsInfo}");
        }

        // fragment shader
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        {
            var shaderSource = GetShaderSource($"{shaderFileName}.frag");
            GL.ShaderSource(fragmentShader, UpdateDefines(shaderSource, args));
            // defines: find render modes supported from source, take union to avoid duplicates
            defines = defines.Union(FindDefines(shaderSource)).ToList();
        }
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out shaderStatus);
        if (shaderStatus != 1)
        {
            GL.GetShaderInfoLog(fragmentShader, out var fsInfo);
            throw new Exception($"Error setting up Fragment Shader \"{name}\": {fsInfo}");
        }

        // defines: find render modes
        const string RenderMode = "renderMode_";
        var renderModes = defines.Where(k => k.StartsWith(RenderMode)).Select(k => k[RenderMode.Length..]).ToList();

        // build shader
        var shader = new Shader(GL.GetUniformLocation, GL.GetAttribLocation)
        {
            Name = name,
            Parameters = args,
            Program = GL.CreateProgram(),
            RenderModes = renderModes,
        };
        GL.AttachShader(shader.Program, vertexShader);
        GL.AttachShader(shader.Program, fragmentShader);
        GL.LinkProgram(shader.Program);
        GL.ValidateProgram(shader.Program);
        GL.GetProgram(shader.Program, GetProgramParameterName.LinkStatus, out var linkStatus);
        GL.DetachShader(shader.Program, vertexShader);
        GL.DeleteShader(vertexShader);
        GL.DetachShader(shader.Program, fragmentShader);
        GL.DeleteShader(fragmentShader);
        if (linkStatus != 1)
        {
            GL.GetProgramInfoLog(shader.Program, out var linkInfo);
            throw new Exception($"Error linking shaders: {linkInfo} (link status = {linkStatus})");
        }

#if !DEBUG_SHADERS || !DEBUG
        // cache shader
        if (cache)
        {
            ShaderDefines[shaderFileName] = defines;
            var newShaderCacheHash = CalculateShaderCacheHash(shaderFileName, args);
            CachedShaders[newShaderCacheHash] = shader;
            Log($"Shader {name}({string.Join(", ", args.Keys)}) compiled and linked succesfully");
        }
#endif
        return shader;
    }

    // Preprocess a vertex shader's source to include the #version plus #defines for parameters
    string PreprocessVertexShader(string source, IDictionary<string, bool> args)
        => ResolveIncludes(UpdateDefines(source, args));

    // Update default defines with possible overrides from the model
    static string UpdateDefines(string source, IDictionary<string, bool> args)
    {
        // find all #define param_(paramName) (paramValue) using regex
        var defines = Regex.Matches(source, @"#define param_(\S*?) (\S*?)\s*?\n");
        foreach (Match define in defines)
            // check if this parameter is in the arguments
            if (args.TryGetValue(define.Groups[1].Value, out var value))
            {
                // overwrite default value
                var index = define.Groups[2].Index;
                var length = define.Groups[2].Length;
                source = source.Remove(index, Math.Min(length, source.Length - index)).Insert(index, value ? "1" : "0");
            }
        return source;
    }

    // Remove any #includes from the shader and replace with the included code
    string ResolveIncludes(string source)
    {
        var includes = Regex.Matches(source, @"#include ""([^""]*?)"";?\s*\n");
        foreach (Match define in includes)
        {
            // read included code
            var includedCode = GetShaderSource(define.Groups[1].Value);
            // recursively resolve includes in the included code. (Watch out for cyclic dependencies!)
            includedCode = ResolveIncludes(includedCode);
            if (!includedCode.EndsWith("\n")) includedCode += "\n";
            // replace the include with the code
            source = source.Replace(define.Value, includedCode);
        }
        return source;
    }

    static List<string> FindDefines(string source)
    {
        var defines = Regex.Matches(source, @"#define param_(\S+)");
        return defines.Cast<Match>().Select(_ => _.Groups[1].Value).ToList();
    }
}

/// <summary>
/// ShaderDebugLoader
/// </summary>
public class ShaderDebugLoader : ShaderLoader
{
    const string ShaderDirectory = "OpenStack.Gl.Shaders";

    // Map shader names to shader files
    protected override string GetShaderFileByName(string name)
    {
        switch (name)
        {
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
                // Console.WriteLine($"Unknown shader {name}, defaulting to simple.");
                return "simple";
        }
    }

    protected override string GetShaderSource(string name)
    {
#if DEBUG_SHADERS && DEBUG
        var stream = File.Open(GetShaderDiskPath(name), FileMode.Open);
#else
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{ShaderDirectory}.{name}");
#endif
        using var r = new StreamReader(stream); return r.ReadToEnd();
    }

#if DEBUG_SHADERS && DEBUG
    // Reload shaders at runtime
    static string GetShaderDiskPath(string name) => Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName), "../../../../", ShaderDirectory.Replace(".", "/"), name);
#endif
}

#endregion
