using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Graphics
{
    public interface IMaterialManager<Material, Texture>
    {
        ITextureManager<Texture> TextureManager { get; }
        Material LoadMaterial(object key, out IDictionary<string, object> data);
        void PreloadMaterial(string path);
    }

    /// <summary>
    /// IMaterialInfo
    /// </summary>
    public interface IMaterialInfo
    {
        string Name { get; }
        string ShaderName { get; set; }
        IDictionary<string, bool> GetShaderArgs();
        IDictionary<string, object> Data { get; }
    }

    public interface IFixedMaterialInfo : IMaterialInfo
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

    public interface IParamMaterialInfo : IMaterialInfo
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

    ///// <summary>
    ///// MaterialType
    ///// </summary>
    //public enum MaterialType { None, Default, Standard, BumpedDiffuse, Unlit }
}