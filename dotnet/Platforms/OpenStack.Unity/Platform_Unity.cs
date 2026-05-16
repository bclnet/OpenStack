using OpenStack.Client;
using OpenStack.Gfx;
using OpenStack.Gfx.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static OpenStack.Gfx.TextureFormat;
using TextureFormat = UnityEngine.TextureFormat;
using XShader = UnityEngine.Shader;
#pragma warning disable CS0649, CS0169, CS8500

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack;

#region Client

public class UnityClientHost : MonoBehaviour, IClientHost {
    [field: SerializeField] public string Family { get; set; }
    [field: SerializeField] public Uri Game { get; set; }

    public void Dispose() {
        throw new NotImplementedException();
    }

    public void Run() => throw new NotSupportedException();

    public void SetClient(ClientBase client) {
        throw new NotImplementedException();
    }
}

#endregion

#region Platform

// UnityObjectModelBuilder
class UnityObjectModelBuilder : ObjectModelBuilderBase<GameObject, Material, Texture2D> {
    GameObject _prefabObj;

    public override GameObject InstanceObject(GameObject src, GameObject parent) {
        var s = UnityEngine.Object.Instantiate(src);
        if (parent != null) s.transform.parent = parent.transform;
        return s;
    }

    public override GameObject CreateObject(object path, bool isStatic, MaterialManager<Material, Texture2D> materialManager) {
        var builder = UnityPlatform.BuildersByType[path.GetType()];
        var s = builder(path, isStatic, materialManager);
        s.transform.parent = _prefabObj.transform;
        // Add LOD support to the prefab.
        var lod = s.AddComponent<LODGroup>();
        lod.SetLODs([new(0.015f, s.GetComponentsInChildren<UnityEngine.Renderer>())]);
        return s;
    }

    public override void EnsurePrefab() {
        if (_prefabObj != null) return;
        _prefabObj = new GameObject("_Prefabs");
        _prefabObj.SetActive(false);
    }
}

// UnityShaderBuilder
class UnityShaderBuilder : ShaderBuilderBase<XShader> {
    public override XShader CreateShader(object path, IDictionary<string, bool> args = null) => XShader.Find((string)path);
}

// UnityTextureBuilder
class UnityTextureBuilder : TextureBuilderBase<Texture2D> {
    Texture2D _defaultTexture;
    public override Texture2D DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release() {
        if (_defaultTexture != null) { UnityEngine.Object.Destroy(_defaultTexture); _defaultTexture = null; }
    }

    Texture2D CreateDefaultTexture() => new(4, 4) { name = "default" };

    /// <summary>
    /// Create a solid texture used by HDRP material.
    /// </summary>
    /// <param name="r">Metallic</param>
    /// <param name="g">Occlusion</param>
    /// <param name="b">Detail Mask</param>
    /// <param name="a">Smoothness</param>
    /// <returns>A mask texture.</returns>
    public override Texture2D CreateSolidTexture(int width, int height, float[] rgbas) {
        var s = new Texture2D(width, height) { name = "solid" };
        s.SetPixels([new Color(rgbas[0], rgbas[1], rgbas[2], rgbas[3])]);
        s.Apply();
        return s;
    }

    // https://gamedev.stackexchange.com/questions/106703/create-a-normal-map-using-a-script-unity
    public override Texture2D CreateNormalMapTexture(Texture2D src, float strength) {
        strength = Mathf.Clamp(strength, 0f, 100f);
        float xLeft, xRight, yUp, yDown, yDelta, xDelta;
        var s = new Texture2D(src.width, src.height, TextureFormat.RGB24, true) { name = "normal" };
        for (var y = 0; y < s.height; y++)
            for (var x = 0; x < s.width; x++) {
                xLeft = src.GetPixel(x - 1, y).grayscale * strength;
                xRight = src.GetPixel(x + 1, y).grayscale * strength;
                yUp = src.GetPixel(x, y - 1).grayscale * strength;
                yDown = src.GetPixel(x, y + 1).grayscale * strength;
                xDelta = (xLeft - xRight + 1) * 0.5f;
                yDelta = (yUp - yDown + 1) * 0.5f;
                s.SetPixel(x, y, new Color(xDelta, yDelta, 1.0f, yDelta));
            }
        s.Apply();
        return s;
    }

    public override Texture2D CreateTexture(Texture2D reuse, ITexture src, Range? range = null) => src.Create("UN", x => {
        switch (x) {
            case Texture_Bytes t:
                if (t.Bytes == null) return DefaultTexture;
                else if (t.Format is ValueTuple<Gfx.TextureFormat, TexturePixel> z) {
                    var (format, pixel) = z;
                    bool s = (pixel & TexturePixel.Signed) != 0, f = (pixel & TexturePixel.Float) != 0;
                    var textureFormat = format switch {
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
                    if (format == DXT3) { textureFormat = TextureFormat.DXT5; TextureConvert.Dxt3ToDtx5(t.Bytes, src.Width, src.Height, src.MipMaps); }
                    var tex = new Texture2D(src.Width, src.Height, textureFormat, src.MipMaps, false) { name = "tex" };
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

    public override void DeleteTexture(Texture2D src) => UnityEngine.Object.Destroy(src);
}

// UnityMaterialBuilder
/// <summary>
/// A material that uses the new Standard Shader.
/// </summary>
class UnityMaterialBuilder(TextureManager<Texture2D> textureManager) : MaterialBuilderBase<Material, Texture2D>(textureManager) {
    static readonly int BaseMap = XShader.PropertyToID("_BaseMap"), BumpMap = XShader.PropertyToID("_BumpMap"), Cutoff = XShader.PropertyToID("_Cutoff");
    static readonly XShader _litShader = XShader.Find("Universal Render Pipeline/Lit"), _terrainShader = XShader.Find("Universal Render Pipeline/Terrain/Lit");

    Material _defaultMaterial;
    public override Material DefaultMaterial => _defaultMaterial ??= new(_litShader ?? throw new Exception("Missing: _litShader"));

    Material _terrainMaterial;
    public override Material TerrainMaterial => _terrainMaterial ??= new(_terrainShader ?? throw new Exception("Missing: _terrainShader"));

    public override async Task<Material> CreateMaterial(object path) {
        switch (path) {
            case MaterialStdProp p: {
                    var m = new Material(_litShader ?? throw new Exception("Missing: _litShader"));
                    if (p.AlphaBlended) m.SetFloat(Cutoff, 0.5f);
                    else if (p.AlphaTest) m.EnableKeyword("_ALPHATEST_ON");
                    var mainTex = p.Textures.TryGetValue("Main", out var z) ? z : default;
                    if (mainTex != null) {
                        m.SetTexture(BaseMap, (await TextureManager.CreateTexture(mainTex)).tex);
                        var bumpTex = p.Textures.TryGetValue("Bump", out z) ? z : default;
                        if (bumpTex != null) {
                            m.EnableKeyword("_NORMALMAP");
                            m.SetTexture(BumpMap, (await TextureManager.CreateTexture(bumpTex)).tex);
                            //m.SetTexture(BumpMap, bumpTex != null ? TextureManager.CreateTexture(bumpTex).tex : TextureManager.CreateNormalMapTexture(tex));
                        }
                    }
                    return m;
                }
            //case MaterialTerrainProp _: return new Material(_terrainShader);
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

// UnityGfxApi
public class UnityGfxApi(ISource source) : IOpenGfxApi<GameObject, Material> {
    public ISource Source => source;
    public GameObject CreateObject(string name, string tag, GameObject parent = default) {
        var s = new GameObject(name);
        if (tag != null) s.tag = tag;
        if (parent != null) s.transform.parent = parent.transform;
        return s;
    }
    public void Parent(GameObject source, GameObject parent) => source?.transform.SetParent(parent.transform, false);
    public void Transform(GameObject source, System.Numerics.Vector3 position, System.Numerics.Quaternion rotation, System.Numerics.Vector3 localScale) {
        source.transform.position = position.ToUnity();
        source.transform.rotation = rotation.ToUnity();
        source.transform.localScale = localScale.ToUnity();
    }
    public void Transform(GameObject source, System.Numerics.Vector3 position, System.Numerics.Matrix4x4 rotation, System.Numerics.Vector3 localScale) {
        source.transform.position = position.ToUnity();
        source.transform.rotation = rotation.ToUnityRotation().ToUnityQuaternionAsRotation();
        source.transform.localScale = localScale.ToUnity();
    }
    public void AddMissingMeshCollidersRecursively(GameObject source, bool isStatic) { if (isStatic && source.GetComponentInChildren<UnityEngine.Collider>() == null) source.AddMissingMeshCollidersRecursively(); }
    public void SetLayerRecursively(GameObject source, int layer) => source.SetLayerRecursively(layer);
    public object CreateMesh(object mesh) => throw new NotImplementedException();
    public void AddMeshRenderer(GameObject source, object mesh, Material material, bool enabled, bool isStatic) {
        source.AddComponent<MeshFilter>().mesh = (Mesh)mesh;
        var meshRenderer = source.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        meshRenderer.enabled = enabled;
        source.isStatic = isStatic;
    }
    public void AddSkinnedMeshRenderer(GameObject source, object mesh, Material material, bool enabled, bool isStatic) {
        var skin = source.AddComponent<SkinnedMeshRenderer>();
        skin.sharedMesh = (Mesh)mesh;
        skin.bones = null;
        skin.rootBone = null;
        skin.sharedMaterial = material;
        skin.enabled = enabled;
        source.isStatic = isStatic;
    }
    public void AddMeshCollider(GameObject source, object mesh, bool isKinematic, bool isStatic) {
        if (!isStatic) {
            source.AddComponent<BoxCollider>();
            source.AddComponent<Rigidbody>().isKinematic = isKinematic;
        }
        else source.AddComponent<MeshCollider>().sharedMesh = (Mesh)mesh;
    }
    public void Attach(GfxAttach method, GameObject src, params object[] args) {
        GameObject v;
        switch (method) {
            case GfxAttach.Find:
                v = (GameObject)args[0];
                v = v.FindChildRecursively((string)args[1]) ?? v;
                src.transform.position = v.transform.position;
                src.transform.rotation = v.transform.rotation;
                src.transform.parent = v.transform;
                break;
            case GfxAttach.Transform:
                v = (GameObject)args[0];
                src.transform.parent = v.transform;
                break;
            case GfxAttach.All:
                v = (GameObject)args[0];
                src.transform.position = v.transform.position;
                src.transform.rotation = v.transform.rotation;
                src.transform.parent = v.transform;
                break;
            case GfxAttach.AllCenter:
                v = (GameObject)args[0];
                src.transform.position = v.CalcVisualBoundsRecursive().center;
                src.transform.rotation = v.transform.rotation;
                src.transform.parent = v.transform;
                break;
        }
    }
    public void SetVisible(GameObject src, bool visible) {
        if (visible) { if (!src.activeSelf) src.SetActive(true); }
        else { if (src.activeSelf) src.SetActive(false); }
    }
    public void Destroy(GameObject src) => UnityEngine.Object.Destroy(src);
}

// UnityGfx2dSprite
public class UnityGfxSprite2D : IOpenGfxSprite<GameObject, Sprite> {
    readonly ISource _source;
    readonly SpriteManager<Sprite> _spriteManager;
    public UnityGfxSprite2D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite>(source, new UnitySpriteBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<Sprite> SpriteManager => _spriteManager;
    public void PreloadSprite(object path) => _spriteManager.PreloadSprite(path);
    public Task<(Sprite spr, object tag)> CreateSprite(object path, GameObject parent = default) => _spriteManager.CreateSprite(path);
}

// UnityGfxSprite3D
public class UnityGfxSprite3D : IOpenGfxSprite<GameObject, Sprite> {
    readonly ISource _source;
    readonly SpriteManager<Sprite> _spriteManager;
    public UnityGfxSprite3D(ISource source) {
        _source = source;
        //_spriteManager = new SpriteManager<Sprite>(source, new UnitySpriteBuilder());
    }

    public ISource Source => _source;
    public SpriteManager<Sprite> SpriteManager => _spriteManager;
    public void PreloadSprite(object path) => _spriteManager.PreloadSprite(path);
    public Task<(Sprite spr, object tag)> CreateSprite(object path, GameObject parent = default) => _spriteManager.CreateSprite(path);
}

// UnityGfxModel
public class UnityGfxModel : IOpenGfxModel<GameObject, Material, Texture2D, XShader> {
    readonly ISource _source;
    readonly MaterialManager<Material, Texture2D> _materialManager;
    readonly ObjectModelManager<GameObject, Material, Texture2D> _objectManager;
    readonly ShaderManager<XShader> _shaderManager;
    readonly TextureManager<Texture2D> _textureManager;
    public UnityGfxModel(ISource source) {
        _source = source;
        _textureManager = new TextureManager<Texture2D>(source, new UnityTextureBuilder());
        _materialManager = new MaterialManager<Material, Texture2D>(source, _textureManager, new UnityMaterialBuilder(_textureManager));
        _objectManager = new ObjectModelManager<GameObject, Material, Texture2D>(source, _materialManager, new UnityObjectModelBuilder());
        _shaderManager = new ShaderManager<XShader>(source, new UnityShaderBuilder());
    }

    public ISource Source => _source;
    public MaterialManager<Material, Texture2D> MaterialManager => _materialManager;
    public ObjectModelManager<GameObject, Material, Texture2D> ObjectManager => _objectManager;
    public ShaderManager<XShader> ShaderManager => _shaderManager;
    public TextureManager<Texture2D> TextureManager => _textureManager;
    public void PreloadObject(object path) => _objectManager.PreloadObject(path);
    public void PreloadTexture(object path) => _textureManager.PreloadTexture(path);
    public Task<(GameObject obj, object tag)> CreateObject(object path, bool isStatic, GameObject parent = default) => _objectManager.CreateObject(path, parent);
    public Task<(XShader sha, object tag)> CreateShader(object path, IDictionary<string, bool> args = null) => _shaderManager.CreateShader(path, args);
    public Task<(Texture2D tex, object tag)> CreateTexture(object path, Range? level = null) => _textureManager.CreateTexture(path, level);

    const int YardInMWUnits = 64;
    const float MeterInYards = 1.09361f;
    const float MeterInUnits = MeterInYards * YardInMWUnits;
    public void PostObject(GameObject src, System.Numerics.Vector3 position, System.Numerics.Vector3 eulerAngles, float? scale, GameObject parent = default) {
        if (src == null) return;
        if (scale != null) src.transform.localScale = Vector3.one * scale.Value;
        if (src.name == "CaveMudcrab.NIF(Clone)") Debug.Log($"{src.name} @ {position} -> {position.ToUnity()}");
        src.transform.position += position.ToUnity() / MeterInUnits;
        src.transform.rotation *= eulerAngles.ToUnityQuaternionAsEulerAngles();
        var coll = src.GetComponentInChildren<Collider>(); // if the collider is on a child object and not on the object with the component, we need to set that object's tag instead.
        var tagTarget = coll != null ? coll.gameObject : src;
        //ProcessObjectType<DOORRecord>(tagTarget, refCellObjInfo, "Door");
        //ProcessObjectType<ACTIRecord>(tagTarget, refCellObjInfo, "Activator");
        //ProcessObjectType<CONTRecord>(tagTarget, refCellObjInfo, "ContObj");
        //ProcessObjectType<LIGHRecord>(tagTarget, refCellObjInfo, "Light");
        //ProcessObjectType<LOCKRecord>(tagTarget, refCellObjInfo, "Lock");
        //ProcessObjectType<PROBRecord>(tagTarget, refCellObjInfo, "Probe");
        //ProcessObjectType<REPARecord>(tagTarget, refCellObjInfo, "RepairTool");
        //ProcessObjectType<WEAPRecord>(tagTarget, refCellObjInfo, "Weapon");
        //ProcessObjectType<CLOTRecord>(tagTarget, refCellObjInfo, "Clothing");
        //ProcessObjectType<ARMORecord>(tagTarget, refCellObjInfo, "Armor");
        //ProcessObjectType<INGRRecord>(tagTarget, refCellObjInfo, "Ingredient");
        //ProcessObjectType<ALCHRecord>(tagTarget, refCellObjInfo, "Alchemical");
        //ProcessObjectType<APPARecord>(tagTarget, refCellObjInfo, "Apparatus");
        //ProcessObjectType<BOOKRecord>(tagTarget, refCellObjInfo, "Book");
        //ProcessObjectType<MISCRecord>(tagTarget, refCellObjInfo, "MiscObj");
        //ProcessObjectType<CREARecord>(tagTarget, refCellObjInfo, "Creature");
        //ProcessObjectType<NPC_Record>(tagTarget, refCellObjInfo, "NPC");
        if (parent != null) src.transform.parent = parent.transform;
    }

    //void ProcessObjectType<RecordType>(Object gameObject, RefCellObjInfo info, string tag) where RecordType : Record {
    //    if (info.Record is RecordType r) {
    //        var obj = GameObjectUtils.FindTopLevelObject(gameObject);
    //        if (obj == null) return;
    //        //var component = GenericObjectComponent.Create(obj, record, tag);
    //        ////only door records need access to the cell object data group so far
    //        //if (record is DOORRecord)
    //        //    ((DoorComponent)component).RefObj = info.RefObj;
    //    }
    //}
}

// UnityGfxLight
public class UnityGfxLight(ISource source) : IOpenGfxLight<GameObject> {
    const bool RenderLightShadows = false;
    const bool RenderExteriorCellLights = false;
    readonly ISource _source = source;
    public ISource Source => _source;
    public GameObject CreateLight(string name, System.Numerics.Vector3? position, float radius, System.Drawing.Color color, bool indoors, GameObject parent = default) {
        var s = new GameObject(name) { isStatic = true };
        if (parent != null) s.transform.parent = parent.transform;
        if (position != null) s.transform.position = position.Value.ToUnity();
        var c = s.AddComponent<Light>();
        c.range = 3 * radius;
        c.color = color.ToUnity();
        c.intensity = 1.5f;
        c.bounceIntensity = 0f;
        c.shadows = RenderLightShadows ? LightShadows.Soft : LightShadows.None;
        if (!indoors && !RenderExteriorCellLights) c.enabled = false; // disabling exterior cell lights because there is no day/night cycle
        return s;
    }
    public GameObject CreateReflectionProbe(string name, System.Numerics.Vector3? position, GameObject parent = default) {
        var s = new GameObject(name);
        if (parent != null) s.transform.parent = parent.transform;
        if (position != null) s.transform.position = position.Value.ToUnity();
        var rp = s.AddComponent<ReflectionProbe>();
        rp.size = new Vector3(120, 120, 120);
        rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        rp.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
        rp.RenderProbe();
        return s;
    }
}

// UnityGfxTerrain
public class UnityGfxTerrain(ISource source) : IOpenGfxTerrain<GameObject, Material, Texture2D> {
    readonly ISource _source = source;
    readonly MaterialManager<Material, Texture2D> _materialManager = new(source, null, new UnityMaterialBuilder(null));
    public ISource Source => _source;
    public object CreateTerrainData(int offset, float[,] heights, float heightRange, float sampleDistance, GfxTerrainLayer<Texture2D>[] layers, float[,,] alphaMap) {
        Debug.Assert(heights.GetLength(0) == heights.GetLength(1) && heightRange >= 0 && sampleDistance >= 0);
        // Create the TerrainData.
        var resolution = heights.GetLength(0);
        var s = new TerrainData { heightmapResolution = resolution };
        //Log($"{terrainData.heightmapResolution} == {heightmapResolution}");
        var terrainWidth = (resolution + offset) * sampleDistance;
        // If maxHeight is 0, leave all the heights in terrainData at 0 and make the vertical size of the terrain 1 to ensure valid AABBs.
        if (!Mathf.Approximately(heightRange, 0)) { s.size = new Vector3(terrainWidth, heightRange, terrainWidth); s.SetHeights(0, 0, heights); }
        else s.size = new Vector3(terrainWidth, 1, terrainWidth);
        s.terrainLayers = [.. layers.Select(s => new TerrainLayer {
            diffuseTexture = s.Texture,
            smoothness = s.Smoothness,
            metallic = s.Metallic,
            maskMapTexture = s.MaskMapTexture,
            normalMapTexture = s.NormalMapTexture,
            tileSize = s.TileSize.ToUnity() })];
        if (alphaMap != null) { Debug.Assert(alphaMap.GetLength(0) == alphaMap.GetLength(1)); s.alphamapResolution = alphaMap.GetLength(0); s.SetAlphamaps(0, 0, alphaMap); }
        return s;
    }
    public GameObject CreateTerrain(string name, System.Numerics.Vector3? position, object data, GameObject parent = default) {
        var data2 = (TerrainData)data;
        var terrainMaterial = _materialManager.TerrainMaterial;
        var terrainError = 1f;
        var treeDistance = 1f;
        var s = new GameObject(name) { isStatic = true };
        if (parent != null) s.transform.parent = parent.transform;
        if (position != null) s.transform.position = position.Value.ToUnity();
        var terrain = s.AddComponent<Terrain>();
        terrain.terrainData = data2;
        terrain.materialTemplate = terrainMaterial;
        terrain.heightmapPixelError = terrainError;
        terrain.treeDistance = treeDistance;
        s.AddComponent<TerrainCollider>().terrainData = data2;
        return s;
    }
}

// UnitySfx
public class UnitySfx(ISource source) : SystemSfx(source) { }

// UnityPlatform
public class UnityPlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, GameObject>> BuildersByType = [];
    public static readonly Platform This = new UnityPlatform();
    UnityPlatform() : base("UN", "Unity") {
        var task = Task.Run(Application.platform.ToString);
        try {
            Tag = task.Result;
            GfxFactory = source => [new UnityGfxApi(source), new UnityGfxSprite2D(source), new UnityGfxSprite3D(source), new UnityGfxModel(source), new UnityGfxLight(source), new UnityGfxTerrain(source)];
            SfxFactory = source => [new UnitySfx(source)];
            AssertFunc = x => UnityEngine.Debug.Assert(x);
            LogFunc = UnityEngine.Debug.Log;
        }
        catch { Debug.Log($"UnityPlatform: Error"); Enabled = false; }
    }
    public override unsafe void Activate() { base.Activate(); UnsafeX.Memcpy = (dest, src, count) => UnsafeUtility.MemCpy(dest, src, count); }
    public override void Deactivate() { base.Deactivate(); UnsafeX.Memcpy = null; }
}

// UnityShellPlatform
public class UnityShellPlatform : Platform {
    public static readonly Platform This = new UnityShellPlatform();
    UnityShellPlatform() : base("UN", "Unity") { }
}

#endregion