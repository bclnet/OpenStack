using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.Graphics.OpenGL")]

namespace OpenStack.Graphics
{
    /// <summary>
    /// IObjectManager
    /// </summary>
    public interface IObjectManager<Object, Material, Texture>
    {
        Object CreateObject(string path, out object tag);
        void PreloadObject(string path);
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
        public Shader LoadShader(string path, IDictionary<string, bool> args = null);
        public Shader LoadPlaneShader(string path, IDictionary<string, bool> args = null);
    }

    /// <summary>
    /// ITextureManager
    /// </summary>
    public interface ITextureManager<Texture>
    {
        Texture DefaultTexture { get; }
        Texture BuildSolidTexture(int width, int height, params float[] rgba);
        Texture BuildNormalMap(Texture source, float strength);
        Texture LoadTexture(object key, out object tag, Range? level = null);
        void PreloadTexture(string path);
        void DeleteTexture(object key);
    }

    /// <summary>
    /// IMaterialManager
    /// </summary>
    public interface IMaterialManager<Material, Texture>
    {
        ITextureManager<Texture> TextureManager { get; }
        Material LoadMaterial(object key, out IDictionary<string, object> data);
        void PreloadMaterial(string path);
    }

    ///// <summary>
    ///// MaterialType
    ///// </summary>
    //public enum MaterialType { None, Default, Standard, BumpedDiffuse, Unlit }

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
        //object Source { get; }
        Task<T> LoadFileObject<T>(string path);
        void PreloadTexture(string texturePath);
        void PreloadObject(string filePath);
    }

    /// <summary>
    /// IOpenGraphicAny
    /// </summary>
    public interface IOpenGraphicAny<Object, Material, Texture, Shader> : IOpenGraphic
    {
        ITextureManager<Texture> TextureManager { get; }
        IMaterialManager<Material, Texture> MaterialManager { get; }
        IObjectManager<Object, Material, Texture> ObjectManager { get; }
        IShaderManager<Shader> ShaderManager { get; }
        Texture LoadTexture(string path, out object tag, Range? rng = null);
        Object CreateObject(string path, out object tag);
        Shader LoadShader(string path, IDictionary<string, bool> args = null);
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

        public static int MaxTextureMaxAnisotropy; // { get; set; }
    }
}