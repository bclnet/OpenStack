using OpenStack.Gfx;
using OpenStack.Gfx.Texture;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static OpenStack.Gfx.Texture.TextureFormat;
using TextureFormat = UnityEngine.TextureFormat;
using XShader = UnityEngine.Shader;
using XRenderer = UnityEngine.Renderer;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Unity;

// UnityExtensions
public static class UnityExtensions
{
    public static UnityEngine.Experimental.Rendering.GraphicsFormat ToUnity(this DXGI_FORMAT source) => (UnityEngine.Experimental.Rendering.GraphicsFormat)source;
    //public static UnityEngine.TextureFormat ToUnity(this TextureUnityFormat source) => (UnityEngine.TextureFormat)source;

    // NifUtils
    public static Vector3 ToUnity(this System.Numerics.Vector3 source) { MathX.Swap(ref source.Y, ref source.Z); return new Vector3(source.X, source.Y, source.Z); }
    public static Vector3 ToUnity(this System.Numerics.Vector3 source, float meterInUnits) => source.ToUnity() / meterInUnits;
    public static Matrix4x4 ToUnityRotationMatrix(this System.Numerics.Matrix4x4 rotationMatrix) => new()
    {
        m00 = rotationMatrix.M11,
        m01 = rotationMatrix.M13,
        m02 = rotationMatrix.M12,
        m03 = 0,
        m10 = rotationMatrix.M31,
        m11 = rotationMatrix.M33,
        m12 = rotationMatrix.M32,
        m13 = 0,
        m20 = rotationMatrix.M21,
        m21 = rotationMatrix.M23,
        m22 = rotationMatrix.M22,
        m23 = 0,
        m30 = 0,
        m31 = 0,
        m32 = 0,
        m33 = 1
    };
    public static Quaternion ToUnityQuaternionAsRotationMatrix(this System.Numerics.Matrix4x4 rotationMatrix) => ToQuaternionAsRotationMatrix(rotationMatrix.ToUnityRotationMatrix());
    public static Quaternion ToQuaternionAsRotationMatrix(this Matrix4x4 rotationMatrix) => Quaternion.LookRotation(rotationMatrix.GetColumn(2), rotationMatrix.GetColumn(1));
    public static Quaternion ToUnityQuaternionAsEulerAngles(this System.Numerics.Vector3 eulerAngles)
    {
        var newEulerAngles = eulerAngles.ToUnity();
        var xRot = Quaternion.AngleAxis(Mathf.Rad2Deg * newEulerAngles.x, Vector3.right);
        var yRot = Quaternion.AngleAxis(Mathf.Rad2Deg * newEulerAngles.y, Vector3.up);
        var zRot = Quaternion.AngleAxis(Mathf.Rad2Deg * newEulerAngles.z, Vector3.forward);
        return xRot * zRot * yRot;
    }
}

// UnityObjectModelBuilder
public class UnityObjectModelBuilder : ObjectModelBuilderBase<GameObject, Material, Texture2D>
{
    GameObject _prefab;
    readonly int _markerLayer = 0;

    public override GameObject CreateNewObject(GameObject prefab) => GameObject.Instantiate(prefab);

    public override GameObject CreateObject(object source, IMaterialManager<Material, Texture2D> materialManager)
    {
        //var abc = source.Begin("UN");
        //try
        //{
        //}
        //finally { source.End(); }
        return null;

        //var file = (NiFile)source;
        //// Start pre-loading all the NIF's textures.
        //foreach (var texturePath in file.GetTexturePaths())
        //    materialManager.TextureManager.PreloadTexture(texturePath);
        //var objBuilder = new NifObjectBuilder(file, materialManager, _markerLayer);
        //var prefab = objBuilder.BuildObject();
        //prefab.transform.parent = _prefab.transform;
        //// Add LOD support to the prefab.
        //var LODComponent = prefab.AddComponent<LODGroup>();
        //LODComponent.SetLODs(new LOD[1] { new(0.015f, prefab.GetComponentsInChildren<XRenderer>()) });
        //return prefab;
    }

    public override void EnsurePrefab()
    {
        if (_prefab != null) return;
        _prefab = new GameObject("_Prefabs");
        _prefab.SetActive(false);
    }
}

// UnityShaderBuilder
public class UnityShaderBuilder : ShaderBuilderBase<XShader>
{
    public override XShader CreateShader(object path, IDictionary<string, bool> args = null) => XShader.Find((string)path);
}

// UnityTextureBuilder
public class UnityTextureBuilder : TextureBuilderBase<Texture2D>
{
    Texture2D _defaultTexture;
    public override Texture2D DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release()
    {
        if (_defaultTexture != null) { UnityEngine.Object.Destroy(_defaultTexture); _defaultTexture = null; }
    }

    Texture2D CreateDefaultTexture() => new(4, 4);

    public override Texture2D CreateTexture(Texture2D reuse, ITexture source, Range? range = null) => source.Create("UN", x =>
    {
        switch (x)
        {
            case Texture_Bytes t:
                if (t.Bytes == null) return DefaultTexture;
                else if (t.Format is ValueTuple<Gfx.Texture.TextureFormat, TexturePixel> z)
                {
                    var (format, pixel) = z;
                    var s = (pixel & TexturePixel.Signed) != 0;
                    var f = (pixel & TexturePixel.Float) != 0;
                    var textureFormat = format switch
                    {
                        DXT1 => TextureFormat.DXT1,
                        DXT1A => default,
                        DXT3 => default,
                        DXT5 => TextureFormat.DXT5,
                        BC4 => TextureFormat.BC4,
                        BC5 => TextureFormat.BC5,
                        BC6H => TextureFormat.BC6H,
                        BC7 => TextureFormat.BC7,
                        ETC2 => TextureFormat.ETC2_RGB,
                        ETC2_EAC => TextureFormat.ETC2_RGBA8,
                        I8 => default,
                        L8 => default,
                        R8 => TextureFormat.R8,
                        R16 => f ? TextureFormat.RFloat : s ? TextureFormat.R16_SIGNED : TextureFormat.R16,
                        RG16 => f ? TextureFormat.RGFloat : s ? TextureFormat.RG16_SIGNED : TextureFormat.RG16,
                        RGB24 => f ? default : s ? TextureFormat.RGB24_SIGNED : TextureFormat.RGB24,
                        RGB565 => TextureFormat.RGB565,
                        RGBA32 => f ? TextureFormat.RGBAFloat : s ? TextureFormat.RGBA32_SIGNED : TextureFormat.RGBA32,
                        ARGB32 => TextureFormat.ARGB32,
                        BGRA32 => TextureFormat.BGRA32,
                        BGRA1555 => default,
                        _ => throw new ArgumentOutOfRangeException("TextureFormat", $"{format}")
                    };
                    if (format == DXT3)
                    {
                        textureFormat = TextureFormat.DXT5;
                        TextureConvert.Dxt3ToDtx5(t.Bytes, source.Width, source.Height, source.MipMaps);
                    }
                    var tex = new Texture2D(source.Width, source.Height, textureFormat, source.MipMaps, false);
                    tex.LoadRawTextureData(t.Bytes);
                    tex.Apply();
                    tex.Compress(true);
                    return tex;
                }
                else throw new ArgumentOutOfRangeException(nameof(t.Format), $"{t.Format}");
            default: throw new ArgumentOutOfRangeException(nameof(x), $"{x}");
        }
    });

    public override Texture2D CreateSolidTexture(int width, int height, float[] rgba) => new(width, height);

    public override Texture2D CreateNormalMap(Texture2D texture, float strength)
    {
        strength = Mathf.Clamp(strength, 0.0F, 1.0F);
        float xLeft, xRight, yUp, yDown, yDelta, xDelta;
        var normalTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, true);
        for (var y = 0; y < normalTexture.height; y++)
            for (var x = 0; x < normalTexture.width; x++)
            {
                xLeft = texture.GetPixel(x - 1, y).grayscale * strength;
                xRight = texture.GetPixel(x + 1, y).grayscale * strength;
                yUp = texture.GetPixel(x, y - 1).grayscale * strength;
                yDown = texture.GetPixel(x, y + 1).grayscale * strength;
                xDelta = (xLeft - xRight + 1) * 0.5f;
                yDelta = (yUp - yDown + 1) * 0.5f;
                normalTexture.SetPixel(x, y, new Color(xDelta, yDelta, 1.0f, yDelta));
            }
        normalTexture.Apply();
        return normalTexture;
    }

    public override void DeleteTexture(Texture2D texture) => UnityEngine.Object.Destroy(texture);
}

// UnityMaterialBuilder

/// <summary>
/// A material that uses the new Standard Shader.
/// </summary>
public class UnityMaterialBuilder(ITextureManager<Texture2D> textureManager) : MaterialBuilderBase<Material, Texture2D>(textureManager)
{
    static readonly int BaseMap = XShader.PropertyToID("_BaseMap"), BumpMap = XShader.PropertyToID("_BumpMap"), Cutoff = XShader.PropertyToID("_Cutoff");
    static readonly XShader _litShader = XShader.Find("Universal Render Pipeline/Lit"), _terrainShader = XShader.Find("Nature/Terrain/Diffuse");

    Material _defaultMaterial;
    public override Material DefaultMaterial => _defaultMaterial ??= new(_litShader);

    public override Material CreateMaterial(object key)
    {
        switch (key)
        {
            case MaterialPropStandard p:
                var m = new Material(_litShader);
                if (p.AlphaBlended) m.SetFloat(Cutoff, 0.5f);
                else if (p.AlphaTest) m.EnableKeyword("_ALPHATEST_ON");
                if (p.MainPath != null)
                {
                    var tex = TextureManager.CreateTexture(p.MainPath).tex;
                    m.SetTexture(BaseMap, tex);
                    if (p.BumpPath != null || NormalGeneratorIntensity != null)
                    {
                        m.EnableKeyword("_NORMALMAP");
                        m.SetTexture(BumpMap, p.BumpPath != null ? TextureManager.CreateTexture(p.BumpPath).tex : TextureManager.CreateNormalMap(tex, NormalGeneratorIntensity.Value));
                    }
                }
                return m;
            case MaterialTerrainProp _: return new Material(_terrainShader);
            default: throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
#if false
    //case MaterialStandardProp p:
    //    var m = new Material(_litShader);
    //    if (p.AlphaBlended) material = BuildMaterialBlended((BlendMode)p.SrcBlendMode, (BlendMode)p.DstBlendMode);
    //    if (p.AlphaTest) material = BuildMaterialTested(p.AlphaCutoff);
    //        if (mp.alphaBlended)
    //            material.SetFloat(Cutoff, 0.5f);
    //    else material = BuildMaterial();
    //    if (p.MainFilePath != null)
    //    {
    //        (material.mainTexture, _) = TextureManager.CreateTexture(p.MainFilePath);
    //        if (NormalGeneratorIntensity != null)
    //        {
    //            material.EnableKeyword("_NORMALMAP");
    //            material.SetTexture("_BumpMap", TextureManager.CreateNormalMap((Texture2D)material.mainTexture, NormalGeneratorIntensity.Value));
    //        }
    //    }
    //    else material.DisableKeyword("_NORMALMAP");
    //    if (p.BumpFilePath != null)
    //    {
    //        material.EnableKeyword("_NORMALMAP");
    //        material.SetTexture("_NORMALMAP", TextureManager.CreateTexture(p.BumpFilePath).tex);
    //    }
    //    return material;
    //case IFixedMaterial p:
    //    if (p.AlphaBlended) material = BuildMaterialBlended((BlendMode)p.SrcBlendMode, (BlendMode)p.DstBlendMode);
    //    else if (p.AlphaTest) material = BuildMaterialTested(p.AlphaCutoff);
    //    else material = BuildMaterial();
    //    if (p.MainFilePath != null && material.HasProperty("_MainTex")) material.SetTexture("_MainTex", TextureManager.CreateTexture(p.MainFilePath).tex);
    //    if (p.DetailFilePath != null && material.HasProperty("_DetailTex")) material.SetTexture("_DetailTex", TextureManager.CreateTexture(p.DetailFilePath).tex);
    //    if (p.DarkFilePath != null && material.HasProperty("_DarkTex")) material.SetTexture("_DarkTex", TextureManager.CreateTexture(p.DarkFilePath).tex);
    //    if (p.GlossFilePath != null && material.HasProperty("_GlossTex")) material.SetTexture("_GlossTex", TextureManager.CreateTexture(p.GlossFilePath).tex);
    //    if (p.GlowFilePath != null && material.HasProperty("_Glowtex")) material.SetTexture("_Glowtex", TextureManager.CreateTexture(p.GlowFilePath).tex);
    //    if (p.BumpFilePath != null && material.HasProperty("_BumpTex")) material.SetTexture("_BumpTex", TextureManager.CreateTexture(p.BumpFilePath).tex);
    //    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
    //    if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
    //    return material;
    //case IFixedMaterial p:
    //    Material material;
    //    if (p.AlphaBlended) material = BuildMaterialBlended((BlendMode)p.SrcBlendMode, (BlendMode)p.DstBlendMode);
    //    else if (p.AlphaTest) material = BuildMaterialTested(p.AlphaCutoff);
    //    else material = BuildMaterial();
    //    if (p.MainFilePath != null)
    //    {
    //        (material.mainTexture, _) = TextureManager.CreateTexture(p.MainFilePath);
    //        if (NormalGeneratorIntensity != null) material.SetTexture("_BumpMap", TextureManager.CreateNormalMap((Texture2D)material.mainTexture, NormalGeneratorIntensity.Value));
    //    }
    //    if (p.BumpFilePath != null) material.SetTexture("_BumpMap", TextureManager.CreateTexture(p.BumpFilePath).tex);
    //    return material;
    //Material BuildMaterial()
    //{
    //    var material = new Material(XShader.Find("Standard"));
    //    material.CopyPropertiesFromMaterial(_standardMaterial);
    //    return material;
    //}
    //Material BuildMaterialBlended(BlendMode srcBlendMode, BlendMode dstBlendMode)
    //{
    //    var material = BuildMaterialTested();
    //    material.SetInt("_SrcBlend", (int)srcBlendMode);
    //    material.SetInt("_DstBlend", (int)dstBlendMode);
    //    return material;
    //}
    //Material BuildMaterialTested(float cutoff = 0.5f)
    //{
    //    var material = new Material(XShader.Find("Standard"));
    //    material.CopyPropertiesFromMaterial(_standardCutoutMaterial);
    //    material.SetFloat("_Cutout", cutoff);
    //    return material;
    //}
#endif
}

//public interface IUnityGfx2dSprite : IOpenGfx2dSpriteAny<GameObject, Sprite> { }
//public interface IUnityGfx3dModel : IOpenGfx3dModelAny<GameObject, Material, Texture2D, XShader> { }

// UnityGfx2dSprite
public class UnityGfx2dSprite : IOpenGfx2dSprite<GameObject, Sprite>
{
    readonly ISource _source;
    readonly ISpriteManager<Sprite> _spriteManager;
    readonly ObjectSpriteManager<GameObject, Sprite> _objectManager;

    public UnityGfx2dSprite(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite>(source, new UnitySpriteBuilder());
        //_objectManager = new Object2dManager<GameObject, Sprite>(source, new UnityObjectBuilder());
    }

    public ISource Source => _source;
    public ISpriteManager<Sprite> SpriteManager => _spriteManager;
    public IObjectSpriteManager<GameObject, Sprite> ObjectManager => _objectManager;
    public Sprite CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public GameObject CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// UnityGfx3dSprite
public class UnityGfx3dSprite : IOpenGfx3dSprite<GameObject, Sprite>
{
    readonly ISource _source;
    readonly ISpriteManager<Sprite> _spriteManager;
    readonly ObjectSpriteManager<GameObject, Sprite> _objectManager;

    public UnityGfx3dSprite(ISource source)
    {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite>(source, new UnitySpriteBuilder());
        //_objectManager = new Object2dManager<GameObject, Sprite>(source, new UnityObjectBuilder());
    }

    public ISource Source => _source;
    public ISpriteManager<Sprite> SpriteManager => _spriteManager;
    public IObjectSpriteManager<GameObject, Sprite> ObjectManager => _objectManager;
    public Sprite CreateSprite(object path) => _spriteManager.CreateSprite(path).spr;
    public void PreloadSprite(object path) => throw new NotImplementedException();
    public GameObject CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// UnityGfx3dModel
public class UnityGfx3dModel : IOpenGfx3dModel<GameObject, Material, Texture2D, XShader>
{
    readonly ISource _source;
    readonly ITextureManager<Texture2D> _textureManager;
    readonly IMaterialManager<Material, Texture2D> _materialManager;
    readonly IObjectModelManager<GameObject, Material, Texture2D> _objectManager;
    readonly IShaderManager<XShader> _shaderManager;

    public UnityGfx3dModel(ISource source)
    {
        _source = source;
        _textureManager = new TextureManager<Texture2D>(source, new UnityTextureBuilder());
        _materialManager = new MaterialManager<Material, Texture2D>(source, _textureManager, new UnityMaterialBuilder(_textureManager));
        _objectManager = new ObjectModelManager<GameObject, Material, Texture2D>(source, _materialManager, new UnityObjectModelBuilder());
        _shaderManager = new ShaderManager<XShader>(source, new UnityShaderBuilder());
    }

    public ISource Source => _source;
    public ITextureManager<Texture2D> TextureManager => _textureManager;
    public IMaterialManager<Material, Texture2D> MaterialManager => _materialManager;
    public IObjectModelManager<GameObject, Material, Texture2D> ObjectManager => _objectManager;
    public IShaderManager<XShader> ShaderManager => _shaderManager;
    public Texture2D CreateTexture(object path, Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => _textureManager.PreloadTexture(path);
    public GameObject CreateObject(object path) => _objectManager.CreateObject(path).obj;
    public void PreloadObject(object path) => _objectManager.PreloadObject(path);
    public XShader CreateShader(object path, IDictionary<string, bool> args = null) => _shaderManager.CreateShader(path, args).sha;

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// UnitySfx
public class UnitySfx(ISource source) : SystemSfx(source) { }

// UnityPlatform
public class UnityPlatform : Platform
{
    public static readonly Platform This = new UnityPlatform();
    UnityPlatform() : base("UN", "Unity")
    {
        var task = Task.Run(Application.platform.ToString);
        try
        {
            Tag = task.Result;
            GfxFactory = source => [new UnityGfx2dSprite(source), new UnityGfx3dSprite(source), new UnityGfx3dModel(source)];
            SfxFactory = source => [new UnitySfx(source)];
            AssertFunc = x => UnityEngine.Debug.Assert(x);
            LogFunc = UnityEngine.Debug.Log;
            LogFormatFunc = UnityEngine.Debug.LogFormat;
        }
        catch { Debug.Log($"UnityPlatform: Error"); Enabled = false; }
    }

    public override unsafe void Activate()
    {
        base.Activate();
        UnsafeX.Memcpy = (dest, src, count) => UnsafeUtility.MemCpy(dest, src, count);
    }

    public override unsafe void Deactivate()
    {
        base.Deactivate();
        UnsafeX.Memcpy = null;
    }
}

// UnityShellPlatform
public class UnityShellPlatform : Platform
{
    public static readonly Platform This = new UnityShellPlatform();
    UnityShellPlatform() : base("UN", "Unity") { }
}