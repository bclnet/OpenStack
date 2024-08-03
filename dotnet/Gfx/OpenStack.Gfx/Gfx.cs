using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.Gfx.Gl")]
[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Gfx
{
    /// <summary>
    /// IAudioManager
    /// </summary>
    public interface IAudioManager<Audio>
    {
        (Audio aud, object tag) CreateAudio(object path);
        void PreloadAudio(object path);
        void DeleteAudio(object path);
    }

    /// <summary>
    /// IObjectManager
    /// </summary>
    public interface IObjectManager<Object, Material, Texture>
    {
        (Object obj, object tag) CreateObject(object path);
        void PreloadObject(object path);
    }

    /// <summary>
    /// IModel
    /// </summary>
    public interface IModel
    {
        IDictionary<string, object> Data { get; }
        IVBIB RemapBoneIndices(IVBIB vbib, int meshIndex);
    }

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

    /// <summary>
    /// IShaderManager
    /// </summary>
    public interface IShaderManager<Shader>
    {
        public (Shader sha, object tag) CreateShader(object path, IDictionary<string, bool> args = null);
        public (Shader sha, object tag) CreatePlaneShader(object path, IDictionary<string, bool> args = null);
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
    /// IMaterialManager
    /// </summary>
    public interface IMaterialManager<Material, Texture>
    {
        ITextureManager<Texture> TextureManager { get; }
        (Material mat, object tag) CreateMaterial(object path);
        void PreloadMaterial(object path);
    }

    /// <summary>
    /// IMaterial
    /// </summary>
    public interface IMaterial
    {
        string Name { get; }
        string ShaderName { get; set; }
        IDictionary<string, object> Data { get; }
        IDictionary<string, bool> GetShaderArgs();
    }

    /// <summary>
    /// IFixedMaterial
    /// </summary>
    public interface IFixedMaterial : IMaterial
    {
        string MainFilePath { get; }
        string DarkFilePath { get; }
        string DetailFilePath { get; }
        string GlossFilePath { get; }
        string GlowFilePath { get; }
        string BumpFilePath { get; }
        bool AlphaBlended { get; }
        int SrcBlendMode { get; }
        int DstBlendMode { get; }
        bool AlphaTest { get; }
        float AlphaCutoff { get; }
        bool ZWrite { get; }
    }

    /// <summary>
    /// IParamMaterial
    /// </summary>
    public interface IParamMaterial : IMaterial
    {
        Dictionary<string, long> IntParams { get; }
        Dictionary<string, float> FloatParams { get; }
        Dictionary<string, Vector4> VectorParams { get; }
        Dictionary<string, string> TextureParams { get; }
        Dictionary<string, long> IntAttributes { get; }
        //Dictionary<string, float> FloatAttributes { get; }
        //Dictionary<string, Vector4> VectorAttributes { get; }
        //Dictionary<string, string> StringAttributes { get; }
    }

    /// <summary>
    /// IOpenGraphic
    /// </summary>
    public interface IOpenGraphic
    {
        Task<T> LoadFileObject<T>(object path);
        void PreloadTexture(object path);
        void PreloadObject(object path);
    }

    /// <summary>
    /// IOpenGraphicAny
    /// </summary>
    public interface IOpenGraphicAny<Audio, Object, Material, Texture, Shader> : IOpenGraphic
    {
        IAudioManager<Audio> AudioManager { get; }
        ITextureManager<Texture> TextureManager { get; }
        IMaterialManager<Material, Texture> MaterialManager { get; }
        IObjectManager<Object, Material, Texture> ObjectManager { get; }
        IShaderManager<Shader> ShaderManager { get; }
        Audio CreateAudio(object path);
        Texture CreateTexture(object path, Range? level = null);
        Object CreateObject(object path);
        Shader CreateShader(object path, IDictionary<string, bool> args = null);
    }

    /// <summary>
    /// PlatformStats
    /// </summary>
    public static class PlatformStats
    {
        static readonly bool _HighRes = Stopwatch.IsHighResolution;
        static readonly double _HighFrequency = 1000.0 / Stopwatch.Frequency;
        static readonly double _LowFrequency = 1000.0 / TimeSpan.TicksPerSecond;
        static bool _UseHRT = false;

        public static bool UsingHighResolutionTiming => _UseHRT && _HighRes && !Unix;
        public static long TickCount => (long)Ticks;
        public static double Ticks => _UseHRT && _HighRes && !Unix ? Stopwatch.GetTimestamp() * _HighFrequency : DateTime.UtcNow.Ticks * _LowFrequency;

        public static readonly bool Is64Bit = Environment.Is64BitProcess;
        public static bool MultiProcessor { get; private set; }
        public static int ProcessorCount { get; private set; }
        public static bool Unix { get; private set; }

        public static int MaxTextureMaxAnisotropy;
    }
}