using GameX.Bethesda.Formats;
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

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Unity;

// UnityExtensions
public static class UnityExtensions
{
    // NifUtils
    /// <summary>
    /// ToUnity
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Vector3 ToUnity(this System.Numerics.Vector3 source) { MathX.Swap(ref source.Y, ref source.Z); return new Vector3(source.X, source.Y, source.Z); }
    //public static Vector3 ToUnity(this System.Numerics.Vector3 source) => new(source.X, source.Z, source.Y);
    /// <summary>
    /// ToUnity
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Quaternion ToUnity(this System.Numerics.Quaternion source) => new(source.X, source.Y, source.Z, source.W);
    /// <summary>
    /// ToUnityRotation
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Matrix4x4 ToUnityRotation(this System.Numerics.Matrix4x4 source) => new()
    {
        m00 = source.M11,
        m01 = source.M13,
        m02 = source.M12,
        m03 = 0,
        m10 = source.M31,
        m11 = source.M33,
        m12 = source.M32,
        m13 = 0,
        m20 = source.M21,
        m21 = source.M23,
        m22 = source.M22,
        m23 = 0,
        m30 = 0,
        m31 = 0,
        m32 = 0,
        m33 = 1
    };
    /// <summary>
    /// ToUnityQuaternionAsRotation
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Quaternion ToUnityQuaternionAsRotation(this Matrix4x4 source) => Quaternion.LookRotation(source.GetColumn(2), source.GetColumn(1));
    /// <summary>
    /// ToUnityQuaternionAsRotation
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Quaternion ToUnityQuaternionAsRotation(this System.Numerics.Matrix4x4 source) => source.ToUnityRotation().ToUnityQuaternionAsRotation();
    /// <summary>
    /// ToUnityQuaternionAsEulerAngles
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Quaternion ToUnityQuaternionAsEulerAngles(this System.Numerics.Vector3 source) // NifEulerAnglesToUnityQuaternion
    {
        var newAngles = source.ToUnity();
        return Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.x, Vector3.right) *
            Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.y, Vector3.up) *
            Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.z, Vector3.forward);
    }

    // GameObjectUtils

    /// <summary>
    /// Sets the layer of an object and all of it's descendants.
    /// </summary>
    public static void SetLayerRecursively(this GameObject source, int layer)
    {
        source.layer = layer;
        foreach (Transform childTransform in source.transform)
            SetLayerRecursively(childTransform.gameObject, layer);
    }

    /// <summary>
    /// Adds mesh colliders to every descandant object with a mesh filter but no mesh collider, including the object itself.
    /// </summary>
    public static void AddMissingMeshCollidersRecursively(this GameObject source, bool isStatic = true)
    {
        if (!isStatic) return;
        MeshFilter filter;
        if (source.GetComponent<Collider>() == null && (filter = source.GetComponent<MeshFilter>()) != null && filter.mesh != null)
            source.AddComponent<MeshCollider>();
        foreach (Transform childTransform in source.transform)
            AddMissingMeshCollidersRecursively(childTransform.gameObject);
    }
}

// UnityObjectModelBuilder
public class UnityObjectModelBuilder : ObjectModelBuilderBase<GameObject, Material, Texture>
{
    GameObject _prefab;

    public override GameObject CreateNewObject(GameObject prefab) => GameObject.Instantiate(prefab);

    public override GameObject CreateObject(object source, IMaterialManager<Material, Texture> materialManager)
    {
        //var abc = source.Begin("UN");
        //try
        //{
        //}
        //finally { source.End(); }
        var file = (Binary_Nif)source;
        // Start pre-loading all the NIF's textures.
        foreach (var texturePath in file.GetTexturePaths())
            materialManager.TextureManager.PreloadTexture(texturePath);
        var objBuilder = new UnityNifObjectBuilder(file, materialManager, false);
        var prefab = objBuilder.BuildObject();
        prefab.transform.parent = _prefab.transform;
        // Add LOD support to the prefab.
        var LODComponent = prefab.AddComponent<LODGroup>();
        LODComponent.SetLODs([new(0.015f, prefab.GetComponentsInChildren<UnityEngine.Renderer>())]);
        return prefab;
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
public class UnityTextureBuilder : TextureBuilderBase<Texture>
{
    Texture _defaultTexture;
    public override Texture DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release()
    {
        if (_defaultTexture != null) { UnityEngine.Object.Destroy(_defaultTexture); _defaultTexture = null; }
    }

    Texture CreateDefaultTexture() => new Texture2D(4, 4);

    public override Texture CreateTexture(Texture reuse, ITexture source, Range? range = null) => source.Create("UN", x =>
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
                    //var tex = new Texture2D(source.Width, source.Height, textureFormat, source.MipMaps > 0);
                    tex.LoadRawTextureData(t.Bytes);
                    tex.Apply();
                    tex.Compress(true);
                    return tex;
                }
                else throw new ArgumentOutOfRangeException(nameof(t.Format), $"{t.Format}");
            default: throw new ArgumentOutOfRangeException(nameof(x), $"{x}");
        }
    });

    public override Texture CreateSolidTexture(int width, int height, float[] rgba) => new Texture2D(width, height);

    public override Texture CreateNormalMap(Texture texture, float strength)
    {
        var texture2d = (Texture2D)texture;
        strength = Mathf.Clamp(strength, 0.0F, 1.0F);
        float xLeft, xRight, yUp, yDown, yDelta, xDelta;
        var normalTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, true);
        for (var y = 0; y < normalTexture.height; y++)
            for (var x = 0; x < normalTexture.width; x++)
            {
                xLeft = texture2d.GetPixel(x - 1, y).grayscale * strength;
                xRight = texture2d.GetPixel(x + 1, y).grayscale * strength;
                yUp = texture2d.GetPixel(x, y - 1).grayscale * strength;
                yDown = texture2d.GetPixel(x, y + 1).grayscale * strength;
                xDelta = (xLeft - xRight + 1) * 0.5f;
                yDelta = (yUp - yDown + 1) * 0.5f;
                normalTexture.SetPixel(x, y, new Color(xDelta, yDelta, 1.0f, yDelta));
            }
        normalTexture.Apply();
        return normalTexture;
    }

    public override void DeleteTexture(Texture texture) => UnityEngine.Object.Destroy(texture);
}

// UnityMaterialBuilder
/// <summary>
/// A material that uses the new Standard Shader.
/// </summary>
public class UnityMaterialBuilder(ITextureManager<Texture> textureManager) : MaterialBuilderBase<Material, Texture>(textureManager)
{
    static readonly int BaseMap = XShader.PropertyToID("_BaseMap"), BumpMap = XShader.PropertyToID("_BumpMap"), Cutoff = XShader.PropertyToID("_Cutoff");
    static readonly XShader _litShader = XShader.Find("Universal Render Pipeline/Lit"), _terrainShader = XShader.Find("Nature/Terrain/Diffuse");

    Material _defaultMaterial;
    public override Material DefaultMaterial => _defaultMaterial ??= new(_litShader);

    public override Material CreateMaterial(object path)
    {
        switch (path)
        {
            case MaterialPropStandard p:
                {
                    var m = new Material(_litShader);
                    if (p.AlphaBlended) m.SetFloat(Cutoff, 0.5f);
                    else if (p.AlphaTest) m.EnableKeyword("_ALPHATEST_ON");
                    var mainTexture = p.Textures.TryGetValue("Main", out var z) ? z : default;
                    if (mainTexture != null)
                    {
                        var tex = TextureManager.CreateTexture(mainTexture).tex;
                        m.SetTexture(BaseMap, tex);
                        var bumpTexture = p.Textures.TryGetValue("Bump", out z) ? z : default;
                        if (bumpTexture != null || NormalGeneratorIntensity != null)
                        {
                            m.EnableKeyword("_NORMALMAP");
                            m.SetTexture(BumpMap, bumpTexture != null ? TextureManager.CreateTexture(bumpTexture).tex : TextureManager.CreateNormalMap(tex, NormalGeneratorIntensity.Value));
                        }
                    }
                    return m;
                }
            case MaterialTerrainProp _: return new Material(_terrainShader);
            default: throw new ArgumentOutOfRangeException(nameof(path));
        }
    }
#if false
    //case MaterialStandardProp p:
    //    var m = new Material(_litShader);
    //    if (p.AlphaBlended) m = BuildMaterialBlended((BlendMode)p.SrcBlendMode, (BlendMode)p.DstBlendMode);
    //    if (p.AlphaTest) m = BuildMaterialTested(p.AlphaCutoff);
    //        if (mp.alphaBlended)
    //            m.SetFloat(Cutoff, 0.5f);
    //    else m = BuildMaterial();
    //    if (p.MainFilePath != null)
    //    {
    //        (m.mainTexture, _) = TextureManager.CreateTexture(p.MainFilePath);
    //        if (NormalGeneratorIntensity != null)
    //        {
    //            m.EnableKeyword("_NORMALMAP");
    //            m.SetTexture("_BumpMap", TextureManager.CreateNormalMap((Texture)m.mainTexture, NormalGeneratorIntensity.Value));
    //        }
    //    }
    //    else m.DisableKeyword("_NORMALMAP");
    //    if (p.BumpFilePath != null)
    //    {
    //        m.EnableKeyword("_NORMALMAP");
    //        m.SetTexture("_NORMALMAP", TextureManager.CreateTexture(p.BumpFilePath).tex);
    //    }
    //    return m;
    //case IFixedMaterial p:
    //    if (p.AlphaBlended) m = BuildMaterialBlended((BlendMode)p.SrcBlendMode, (BlendMode)p.DstBlendMode);
    //    else if (p.AlphaTest) m = BuildMaterialTested(p.AlphaCutoff);
    //    else m = BuildMaterial();
    //    if (p.MainFilePath != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", TextureManager.CreateTexture(p.MainFilePath).tex);
    //    if (p.DetailFilePath != null && m.HasProperty("_DetailTex")) m.SetTexture("_DetailTex", TextureManager.CreateTexture(p.DetailFilePath).tex);
    //    if (p.DarkFilePath != null && m.HasProperty("_DarkTex")) m.SetTexture("_DarkTex", TextureManager.CreateTexture(p.DarkFilePath).tex);
    //    if (p.GlossFilePath != null && m.HasProperty("_GlossTex")) m.SetTexture("_GlossTex", TextureManager.CreateTexture(p.GlossFilePath).tex);
    //    if (p.GlowFilePath != null && m.HasProperty("_Glowtex")) m.SetTexture("_Glowtex", TextureManager.CreateTexture(p.GlowFilePath).tex);
    //    if (p.BumpFilePath != null && m.HasProperty("_BumpTex")) m.SetTexture("_BumpTex", TextureManager.CreateTexture(p.BumpFilePath).tex);
    //    if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
    //    if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0f);
    //    return m;
    //case IFixedMaterial p:
    //    Material m;
    //    if (p.AlphaBlended) m = BuildMaterialBlended((BlendMode)p.SrcBlendMode, (BlendMode)p.DstBlendMode);
    //    else if (p.AlphaTest) m = BuildMaterialTested(p.AlphaCutoff);
    //    else m = BuildMaterial();
    //    if (p.MainFilePath != null)
    //    {
    //        (m.mainTexture, _) = TextureManager.CreateTexture(p.MainFilePath);
    //        if (NormalGeneratorIntensity != null) m.SetTexture("_BumpMap", TextureManager.CreateNormalMap((Texture)m.mainTexture, NormalGeneratorIntensity.Value));
    //    }
    //    if (p.BumpFilePath != null) m.SetTexture("_BumpMap", TextureManager.CreateTexture(p.BumpFilePath).tex);
    //    return m;
    //Material BuildMaterial()
    //{
    //    var m = new Material(XShader.Find("Standard"));
    //    m.CopyPropertiesFromMaterial(_standardMaterial);
    //    return m;
    //}
    //Material BuildMaterialBlended(BlendMode srcBlendMode, BlendMode dstBlendMode)
    //{
    //    var m = BuildMaterialTested();
    //    m.SetInt("_SrcBlend", (int)srcBlendMode);
    //    m.SetInt("_DstBlend", (int)dstBlendMode);
    //    return m;
    //}
    //Material BuildMaterialTested(float cutoff = 0.5f)
    //{
    //    var m = new Material(XShader.Find("Standard"));
    //    m.CopyPropertiesFromMaterial(_standardCutoutMaterial);
    //    m.SetFloat("_Cutout", cutoff);
    //    return m;
    //}
#endif
}

// UnityModelApi
public class UnityModelApi : ModelApi<GameObject, Material>
{
    public GameObject CreateObject(string name) => new(name);
    public void SetParent(GameObject source, GameObject parent) => source?.transform.SetParent(parent.transform, false);
    public void Transform(GameObject source, System.Numerics.Vector3 position, System.Numerics.Quaternion rotation, System.Numerics.Vector3 localScale)
    {
        source.transform.position = position.ToUnity();
        source.transform.rotation = rotation.ToUnity();
        source.transform.localScale = localScale.ToUnity();
    }
    public void Transform(GameObject source, System.Numerics.Vector3 position, System.Numerics.Matrix4x4 rotation, System.Numerics.Vector3 localScale)
    {
        source.transform.position = position.ToUnity();
        source.transform.rotation = rotation.ToUnityRotation().ToUnityQuaternionAsRotation();
        source.transform.localScale = localScale.ToUnity();
    }
    public void AddMissingMeshCollidersRecursively(GameObject source, bool isStatic) { if (source.GetComponentInChildren<UnityEngine.Collider>() == null) source.AddMissingMeshCollidersRecursively(isStatic); }
    public void SetLayerRecursively(GameObject source, int layer) => source.SetLayerRecursively(layer);

    public object CreateMesh(object mesh)
    {
        throw new NotImplementedException();
    }

    public void AddMeshRenderer(GameObject source, object mesh, Material material, bool enabled, bool isStatic)
    {
        source.AddComponent<MeshFilter>().mesh = (Mesh)mesh;
        var meshRenderer = source.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        meshRenderer.enabled = enabled;
        source.isStatic = isStatic;
    }

    public void AddSkinnedMeshRenderer(GameObject source, object mesh, Material material, bool enabled, bool isStatic)
    {
        var skin = source.AddComponent<SkinnedMeshRenderer>();
        skin.sharedMesh = (Mesh)mesh;
        skin.bones = null;
        skin.rootBone = null;
        skin.sharedMaterial = material;
        skin.enabled = enabled;
        source.isStatic = isStatic;
    }

    public void AddMeshCollider(GameObject source, object mesh, bool isKinematic, bool isStatic)
    {
        if (!isStatic)
        {
            source.AddComponent<BoxCollider>();
            source.AddComponent<Rigidbody>().isKinematic = isKinematic;
        }
        else source.AddComponent<MeshCollider>().sharedMesh = (Mesh)mesh;
    }
}

//public interface IUnityGfx2dSprite : IOpenGfx2dSpriteAny<GameObject, Sprite> { }
//public interface IUnityGfx3dModel : IOpenGfx3dModelAny<GameObject, Material, Texture, XShader> { }

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
public class UnityGfx3dModel : IOpenGfx3dModel<GameObject, Material, Texture, XShader>
{
    readonly ISource _source;
    readonly ITextureManager<Texture> _textureManager;
    readonly IMaterialManager<Material, Texture> _materialManager;
    readonly IObjectModelManager<GameObject, Material, Texture> _objectManager;
    readonly IShaderManager<XShader> _shaderManager;

    public UnityGfx3dModel(ISource source)
    {
        _source = source;
        _textureManager = new TextureManager<Texture>(source, new UnityTextureBuilder());
        _materialManager = new MaterialManager<Material, Texture>(source, _textureManager, new UnityMaterialBuilder(_textureManager));
        _objectManager = new ObjectModelManager<GameObject, Material, Texture>(source, _materialManager, new UnityObjectModelBuilder());
        _shaderManager = new ShaderManager<XShader>(source, new UnityShaderBuilder());
    }

    public ISource Source => _source;
    public ITextureManager<Texture> TextureManager => _textureManager;
    public IMaterialManager<Material, Texture> MaterialManager => _materialManager;
    public IObjectModelManager<GameObject, Material, Texture> ObjectManager => _objectManager;
    public IShaderManager<XShader> ShaderManager => _shaderManager;
    public Texture CreateTexture(object path, Range? level = null) => _textureManager.CreateTexture(path, level).tex;
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
