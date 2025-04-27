using GameX.Bethesda.Formats;
using GameX.Bethesda.Formats.Nif;
using System;
using UnityEngine;
using static OpenStack.Debug;
using Object = UnityEngine.GameObject;

namespace OpenStack.Gfx.Unity;

public class UnityNifObjectBuilder(Binary_Nif source, MaterialManager<Material, Texture2D> materialManager, bool isStatic) {
    const int YardInMWUnits = 64;
    const float MeterInYards = 1.09361f;
    const float MeterInUnits = MeterInYards * YardInMWUnits;
    const int MarkerLayer = 0;
    const bool KinematicRigidbody = false;

    readonly Binary_Nif _source = source;
    readonly MaterialManager<Material, Texture2D> _materialManager = materialManager;
    readonly bool _isStatic = isStatic;

    public Object BuildObject() {
        Assert(_source.Name != null && _source.Footer.Roots.Length > 0);

        // NIF files can have any number of root NiObjects.
        // If there is only one root, instantiate that directly.
        // If there are multiple roots, create a container Object and parent it to the roots.
        if (_source.Footer.Roots.Length == 1) {
            var rootNiObject = _source.Blocks[_source.Footer.Roots[0]];
            var gobj = InstantiateRootNiObject(rootNiObject);
            // If the file doesn't contain any NiObjects we are looking for, return an empty Object.
            if (gobj == null) {
                Log($"{_source.Name} resulted in A null Object when instantiated.");
                gobj = new Object(_source.Name);
            }
            // If gobj != null and the root NiObject is an NiNode, discard any transformations (Morrowind apparently does).
            else if (rootNiObject is NiNode) {
                gobj.transform.position = Vector3.zero;
                gobj.transform.rotation = Quaternion.identity;
                gobj.transform.localScale = Vector3.one;
            }
            return gobj;
        }
        else {
            Log(_source.Name + " has multiple roots.");
            var gobj = new Object(_source.Name);
            foreach (var rootRef in _source.Footer.Roots) {
                var child = InstantiateRootNiObject(_source.Blocks[rootRef]);
                child?.transform.SetParent(gobj.transform, false);
            }
            return gobj;
        }
    }

    Object InstantiateRootNiObject(NiObject obj) {
        var gobj = InstantiateNiObject(obj);
        ProcessExtraData(obj, out var shouldAddMissingColliders, out var isMarker);
        if (_source.Name != null && IsMarkerFileName(_source.Name)) { shouldAddMissingColliders = false; isMarker = true; }
        // Add colliders to the object if it doesn't already contain one.
        if (shouldAddMissingColliders && gobj.GetComponentInChildren<Collider>() == null) gobj.AddMissingMeshCollidersRecursively(_isStatic);
        if (isMarker) gobj.SetLayerRecursively(MarkerLayer);
        return gobj;
    }

    void ProcessExtraData(NiObject obj, out bool shouldAddMissingColliders, out bool isMarker) {
        shouldAddMissingColliders = true; isMarker = false;
        if (obj is NiObjectNET objNET) {
            var extraData = objNET.ExtraData.Value >= 0 ? (NiExtraData)_source.Blocks[objNET.ExtraData.Value] : null;
            while (extraData != null) {
                if (extraData is NiStringExtraData strExtraData) {
                    if (strExtraData.Str == "NCO" || strExtraData.Str == "NCC") shouldAddMissingColliders = false;
                    else if (strExtraData.Str == "MRK") { shouldAddMissingColliders = false; isMarker = true; }
                }
                // Move to the next NiExtraData.
                extraData = extraData.NextExtraData.Value >= 0 ? (NiExtraData)_source.Blocks[extraData.NextExtraData.Value] : default;
            }
        }
    }

    /// <summary>
    /// Creates a Object representation of an NiObject.
    /// </summary>
    /// <returns>Returns the created Object, or null if the NiObject does not need its own Object.</returns>
    Object InstantiateNiObject(NiObject obj) {
        if (obj.GetType() == typeof(NiNode)) return InstantiateNiNode((NiNode)obj);
        else if (obj.GetType() == typeof(NiBSAnimationNode)) return InstantiateNiNode((NiNode)obj);
        else if (obj.GetType() == typeof(NiTriShape)) return InstantiateNiTriShape((NiTriShape)obj, true, false);
        else if (obj.GetType() == typeof(RootCollisionNode)) return InstantiateRootCollisionNode((RootCollisionNode)obj);
        else if (obj.GetType() == typeof(NiTextureEffect)) return default;
        else if (obj.GetType() == typeof(NiBSAnimationNode)) return default;
        else if (obj.GetType() == typeof(NiBSParticleNode)) return default;
        else if (obj.GetType() == typeof(NiRotatingParticles)) return default;
        else if (obj.GetType() == typeof(NiAutoNormalParticles)) return default;
        else if (obj.GetType() == typeof(NiBillboardNode)) return default;
        else throw new NotImplementedException($"Tried to instantiate an unsupported NiObject ({obj.GetType().Name}).");
    }

    Object InstantiateNiNode(NiNode node) {
        var obj = new Object(node.Name);
        foreach (var childIndex in node.Children)
            // NiNodes can have child references < 0 meaning null.
            if (!childIndex.IsNull) {
                var child = InstantiateNiObject(_source.Blocks[childIndex.Value]);
                child?.transform.SetParent(obj.transform, false);
            }
        ApplyNiAVObject(node, obj);
        return obj;
    }

    //void AddMeshRenderer(Object obj, Mesh mesh, Material material, bool enabled)
    //{
    //    obj.AddComponent<MeshFilter>().mesh = mesh;
    //    var meshRenderer = obj.AddComponent<MeshRenderer>();
    //    meshRenderer.sharedMaterial = material;
    //    meshRenderer.enabled = enabled;
    //    obj.isStatic = _isStatic;
    //}

    //void AddSkinnedMeshRenderer(Object obj, Mesh mesh, Material material, bool enabled)
    //{
    //    var skin = obj.AddComponent<SkinnedMeshRenderer>();
    //    skin.sharedMesh = mesh;
    //    skin.bones = null;
    //    skin.rootBone = null;
    //    skin.sharedMaterial = material;
    //    skin.enabled = enabled;
    //    obj.isStatic = _isStatic;
    //}

    Object InstantiateNiTriShape(NiTriShape triShape, bool visual, bool collidable) {
        Assert(visual || collidable);
        var mesh = NiTriShapeDataToMesh((NiTriShapeData)_source.Blocks[triShape.Data.Value]);
        var obj = new Object(triShape.Name);
        if (visual) {
            var materialProps = NiAVObjectToMaterialProp(triShape);
            obj.AddComponent<MeshFilter>().mesh = mesh;
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.material = _materialManager.CreateMaterial(materialProps).mat;
            if (materialProps.Textures == null || triShape.Flags.HasFlag(NiAVObject.NiFlags.Hidden)) meshRenderer.enabled = false;
            obj.isStatic = true;
        }
        if (collidable) {
            if (!_isStatic) {
                obj.AddComponent<BoxCollider>();
                obj.AddComponent<Rigidbody>().isKinematic = KinematicRigidbody;
            }
            else obj.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        ApplyNiAVObject(triShape, obj);
        return obj;
    }

    Object InstantiateRootCollisionNode(RootCollisionNode collisionNode) {
        var obj = new Object("Root Collision Node");
        foreach (var childIndex in collisionNode.Children)
            // NiNodes can have child references < 0 meaning null.
            if (!childIndex.IsNull) AddColliderFromNiObject(_source.Blocks[childIndex.Value], obj);
        ApplyNiAVObject(collisionNode, obj);
        return obj;
    }

    void ApplyNiAVObject(NiAVObject niAVObject, Object obj) {
        obj.transform.position = niAVObject.Translation.ToUnity() / MeterInUnits;
        obj.transform.rotation = niAVObject.Rotation.ToUnityQuaternionAsRotation();
        obj.transform.localScale = niAVObject.Scale * Vector3.one;
    }

    Mesh NiTriShapeDataToMesh(NiTriShapeData data) {
        // vertex positions
        var vertices = new Vector3[data.Vertices.Length];
        for (var i = 0; i < vertices.Length; i++) vertices[i] = data.Vertices[i].ToUnity() / MeterInUnits;
        // vertex normals
        Vector3[] normals = null;
        if (data.HasNormals) {
            normals = new Vector3[vertices.Length];
            for (var i = 0; i < normals.Length; i++) normals[i] = data.Normals[i].ToUnity();
        }
        // vertex UV coordinates
        Vector2[] UVs = null;
        if (data.HasUV) {
            UVs = new Vector2[vertices.Length];
            for (var i = 0; i < UVs.Length; i++) {
                var NiTexCoord = data.UVSets[0, i];
                UVs[i] = new Vector2(NiTexCoord.U, NiTexCoord.V);
            }
        }
        // triangle vertex indices
        var triangles = new int[data.NumTrianglePoints];
        for (var i = 0; i < data.Triangles.Length; i++) {
            var baseI = 3 * i;
            // Reverse triangle winding order.
            triangles[baseI] = data.Triangles[i].V1;
            triangles[baseI + 1] = data.Triangles[i].V3;
            triangles[baseI + 2] = data.Triangles[i].V2;
        }

        // create the mesh.
        var mesh = new Mesh {
            vertices = vertices,
            normals = normals,
            uv = UVs,
            triangles = triangles
        };
        if (!data.HasNormals) mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    MaterialStd2Prop NiAVObjectToMaterialProp(NiAVObject obj) {
        // find relevant properties.
        NiTexturingProperty tex = null;
        NiMaterialProperty mat = null;
        NiAlphaProperty alpha = null;
        foreach (var propRef in obj.Properties) {
            var prop = _source.Blocks[propRef.Value];
            if (prop is NiTexturingProperty tp) tex = tp;
            else if (prop is NiMaterialProperty mp2) mat = mp2;
            else if (prop is NiAlphaProperty ap) alpha = ap;
        }

        // create the material properties.
        var mp = new MaterialStd2Prop();

        /*
        14 bits used:
        1 bit for alpha blend bool
        4 bits for src blend mode
        4 bits for dest blend mode
        1 bit for alpha test bool
        3 bits for alpha test mode
        1 bit for zwrite bool ( opposite value )
        Bit 0 : alpha blending enable
        Bits 1-4 : source blend mode 
        Bits 5-8 : destination blend mode
        Bit 9 : alpha test enable
        Bit 10-12 : alpha test mode
        Bit 13 : no sorter flag ( disables triangle sorting ) ( Unity ZWrite )
        */
        // apply alphaProperty
        if (alpha != null) {
            var flags = alpha.Flags;
            var srcbm = (byte)(BitConverter.GetBytes(flags >> 1)[0] & 15);
            var dstbm = (byte)(BitConverter.GetBytes(flags >> 5)[0] & 15);
            mp.ZWrite = BitConverter.GetBytes(flags >> 15)[0] == 1;
            mp.AlphaBlended = (flags & 0x01) != 0;
            mp.SrcBlendMode = (GfxBlendMode)Math.Min((int)srcbm, 10);
            mp.DstBlendMode = (GfxBlendMode)Math.Min((int)dstbm, 10);
            mp.AlphaTest = (flags & 0x100) != 0;
            mp.AlphaCutoff = (float)alpha.Threshold / 255;
        }

        // apply materialProperty
        if (mat != null) {
            mp.Alpha = mat.Alpha;
            mp.DiffuseColor = mat.DiffuseColor.ToColor();
            mp.EmissiveColor = mat.EmissiveColor.ToColor();
            mp.SpecularColor = mat.SpecularColor.ToColor();
            mp.Glossiness = mat.Glossiness;
        }

        // apply texturingProperty
        if (tex != null && tex.TextureCount > 0) {
            var mt = mp.Textures;
            if (tex.BaseTexture != null) mt.Add("Main", ((NiSourceTexture)_source.Blocks[tex.BaseTexture.Source.Value]).FileName);
            if (tex.DarkTexture != null) mt.Add("Dark", ((NiSourceTexture)_source.Blocks[tex.DarkTexture.Source.Value]).FileName);
            if (tex.DetailTexture != null) mt.Add("Detail", ((NiSourceTexture)_source.Blocks[tex.DetailTexture.Source.Value]).FileName);
            if (tex.GlossTexture != null) mt.Add("Gloss", ((NiSourceTexture)_source.Blocks[tex.GlossTexture.Source.Value]).FileName);
            if (tex.GlowTexture != null) mt.Add("Glow", ((NiSourceTexture)_source.Blocks[tex.GlowTexture.Source.Value]).FileName);
            if (tex.BumpMapTexture != null) mt.Add("Bump", ((NiSourceTexture)_source.Blocks[tex.BumpMapTexture.Source.Value]).FileName);
        }
        return mp;
    }

    void AddColliderFromNiObject(NiObject niObject, Object obj) {
        if (niObject.GetType() == typeof(NiTriShape)) InstantiateNiTriShape((NiTriShape)niObject, false, true).transform.SetParent(obj.transform, false);
        else if (niObject.GetType() == typeof(AvoidNode)) { }
        else Log($"Unsupported collider NiObject: {niObject.GetType().Name}");
    }

    bool IsMarkerFileName(string name) => name.ToLowerInvariant() switch {
        "marker_light" or "marker_north" or "marker_error" or "marker_arrow" or "editormarker" or "marker_creature" or
        "marker_travel" or "marker_temple" or "marker_prison" or "marker_radius" or "marker_divine" or "editormarker_box_01" => true,
        _ => false,
    };
}
