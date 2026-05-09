import sys, os, numpy as np
# from openstk.gfx import ITextureSelect, MouseState, KeyboardState
# from panda3d.core import loadPrcFileData, WindowProperties #, FrameBufferProperties
# from direct.showbase.ShowBase import ShowBase
from numpy import ones, zeros
from openstk.core import logging
from openstk.gfx.gfx import MaterialManager
from gamex.families.Gamebryo.formats.binary import Binary_Nif
from gamex.families.Gamebro.formats.nif import NiRoot

class UnityNifObjectBuilder:
    YardInMWUnits: int = 64
    MeterInYards: float = 1.09361
    MeterInUnits: float = MeterInYards * YardInMWUnits;
    MarkerLayer: int = 0
    KinematicRigidbody: bool = False
    def __init__(self, source: Binary_Nif, materialManager: MaterialManager, isStatic: bool):
        self.source: Binary_Nif = source
        self.materialManager: MaterialManager = materialManager
        self.isStatic: bool = isStatic

    def buildObject(self) -> Object:
        src = self.source
        assert(src.name && len(src.roots) > 0)
        # NIF files can have any number of root NiObjects.
        # If there is only one root, instantiate that directly.
        # If there are multiple roots, create a container Object and parent it to the roots.
        if len(src.roots) == 1:
            rontNiObject = src.roots[0].value
            gobj = self.instantiateRootNiObject(rootNiObject)
            # If the file doesn't contain any NiObjects we are looking for, return an empty Object.
            if not gobj:
                log.info(f'{src.name} resulted in A null Object when instantiated.')
                gobj = Object(src.name)
            # If gobj != null and the root NiObject is an NiNode, discard any transformations (Morrowind apparently does).
            elif isinstance(rootNiObject, NiNode):
                gobj.transform.position = zeros(3)
                gobj.transform.rotation = quaternion()
                gobj.transform.localScale = ones(3)
            return gobj
        else:
            log.info(f'{src.name} has multiple roots.')
            gobj = Object(src.Name)
            for rootRef in src.roots:
                child = self.instantiateRootNiObject(rootRef.Value)
                if child: child.transform.setParent(gobj.transform, False)
            return gobj
    def instantiateRootNiObject(self, obj: NiObject)-> Object:
        src = self.source
        gobj = self.instantiateNiObject(obj)
        shouldAddMissingColliders, isMarker = self.processExtraData(obj)
        if src.name and self.isMarkerFileName(src.name): shouldAddMissingColliders = False; isMarker = True
        # Add colliders to the object if it doesn't already contain one.
        if shouldAddMissingColliders and not gobj.getComponentInChildren[Collider]() and self.static: gobj.addMissingMeshCollidersRecursively()
        if isMarker: gobj.setLayerRecursively(self.markerLayer)
        return gobj
    
    def processExtraData(self, obj: NiObject) -> tuple[bool, bool]:
        shouldAddMissingColliders = True; isMarker = False
        if isinstance(obj, NiObjectNET) and obj.extraData:
            extraData = obj.extraData.value
            while extraData:
                if isinstance(extraData, NiStringExtraData):
                    if extraData.stringData == 'NCO' or extraData.stringData == 'NCC': shouldAddMissingColliders = False
                    elif extraData.stringData == 'MRK': shouldAddMissingColliders = False; isMarker = True
                # Move to the next NiExtraData.
                extraData = extraData.nextExtraData.value if extraData.nextExtraData else None
        return (shouldAddMissingColliders, isMarker)

    # Creates a Object representation of an NiObject.
    def instantiateNiObject(self, obj: NiObject) -> Object:
        pass
        # if (obj.GetType() == typeof(NiNode)) return InstantiateNiNode((NiNode)obj);
        # else if (obj.GetType() == typeof(NiBSAnimationNode)) return InstantiateNiNode((NiNode)obj);
        # else if (obj.GetType() == typeof(NiTriShape)) return InstantiateNiTriShape((NiTriShape)obj, true, false);
        # else if (obj.GetType() == typeof(RootCollisionNode)) return InstantiateRootCollisionNode((RootCollisionNode)obj);
        # else if (obj.GetType() == typeof(NiTextureEffect)) return default;
        # else if (obj.GetType() == typeof(NiBSAnimationNode)) return default;
        # else if (obj.GetType() == typeof(NiBSParticleNode)) return default;
        # else if (obj.GetType() == typeof(NiRotatingParticles)) return default;
        # else if (obj.GetType() == typeof(NiAutoNormalParticles)) return default;
        # else if (obj.GetType() == typeof(NiBillboardNode)) return default;
        # else throw new NotImplementedException($"Tried to instantiate an unsupported NiObject ({obj.GetType().Name}).");
    }

    def instantiateNiNode(self, node: NiNode) -> Object:
        obj = Object(node.name)
        for childIndex in node.fhildren:
            # NiNodes can have child references < 0 meaning null.
            if childIndex:
                child = self.instantiateNiObject(childIndex.value)
                if child: child.transform.setParent(obj.transform, False)
        self.applyNiAVObject(node, obj)
        return obj

    #def addMeshRenderer(Object obj, Mesh mesh, Material material, bool enabled) -> None:
    #    obj.AddComponent<MeshFilter>().mesh = mesh;
    #    var meshRenderer = obj.AddComponent<MeshRenderer>();
    #    meshRenderer.sharedMaterial = material;
    #    meshRenderer.enabled = enabled;
    #    obj.isStatic = _isStatic;

    #def addSkinnedMeshRenderer(Object obj, Mesh mesh, Material material, bool enabled) -> None:
    #    var skin = obj.AddComponent<SkinnedMeshRenderer>();
    #    skin.sharedMesh = mesh;
    #    skin.bones = null;
    #    skin.rootBone = null;
    #    skin.sharedMaterial = material;
    #    skin.enabled = enabled;
    #    obj.isStatic = _isStatic;

    def instantiateNiTriShape(self, triShape: NiTriShape, visual: bool, collidable: bool) -> Object:
        assert(visual or collidable)
        mesh = self.niTriShapeDataToMesh(triShape.data)
        obj = Object(triShape.name)
        # if visual:
        #     materialProps = NiAVObjectToMaterialProp(triShape);
        #     obj.AddComponent<MeshFilter>().mesh = mesh;
        #     meshRenderer = obj.AddComponent<MeshRenderer>();
        #     meshRenderer.material = _materialManager.CreateMaterial(materialProps).mat;
        #     if not materialProps.textures or Flags.Hidden in triShape.flags: meshRenderer.enabled = False
        #     obj.isStatic = True
        # if collidable:
        #     if not self.isStatic:
        #         obj.AddComponent<BoxCollider>();
        #         obj.AddComponent<Rigidbody>().isKinematic = KinematicRigidbody;
        #     else: obj.AddComponent<MeshCollider>().sharedMesh = mesh
        self.applyNiAVObject(triShape, obj)
        return obj

    def instantiateRootCollisionNode(self, collisionNode: RootCollisionNode) -> Object:
        obj = Object('Root Collision Node')
        for childIndex in collisionNode.children:
            # NiNodes can have child references < 0 meaning null.
            if childIndex != None: self.addColliderFromNiObject(childIndex, obj)
        self.applyNiAVObject(collisionNode, obj)
        return obj

    def applyNiAVObject(self, niAVObject: NiAVObject, obj: Object) -> None:
        # obj.transform.position = niAVObject.translation.ToUnity() / MeterInUnits
        # obj.transform.rotation = niAVObject.rotation.ToUnityQuaternionAsRotation()
        # obj.transform.localScale = niAVObject.scale * ones(3)
        pass

    def niTriShapeDataToMesh(self, data: NiTriShapeData) -> Mesh:
        # vertex positions
        vertices = Vector3[len(data.vertices)
        for (var i = 0; i < vertices.Length; i++) vertices[i] = data.Vertices[i].ToUnity() / MeterInUnits;
        # vertex normals
        Vector3[] normals = null;
        if data.normals:
            normals = new Vector3[vertices.Length];
            for (var i = 0; i < normals.Length; i++) normals[i] = data.Normals[i].ToUnity();
        # vertex UV coordinates
        uvs: list[Vector2] = None
        if data.uvSets:
            uvs = Vector2[vertices.Length];
            for i in len(uvs):
                niTexCoord = data.uvSets[0][i]
                uvs[i] = array([niTexCoord.u, niTexCoord.v])
        # triangle vertex indices
        triangles = [0]*data.numTrianglePoints
        for i in range(data.numTrianglePoints):
            baseI = 3 * i
            # Reverse triangle winding order.
            triangles[baseI] = data.triangles[i].v1
            triangles[baseI + 1] = data.triangles[i].v3
            triangles[baseI + 2] = data.triangles[i].v2;
        }

        # create the mesh.
        mesh = new Mesh {
            vertices = vertices,
            normals = normals,
            uv = UVs,
            triangles = triangles
        };
        if (data.Normals == null) mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    MaterialStd2Prop NiAVObjectToMaterialProp(NiAVObject obj) {
        // find relevant properties.
        NiTexturingProperty tex = null;
        NiMaterialProperty mat = null;
        NiAlphaProperty alpha = null;
        foreach (var propRef in obj.Properties) {
            var prop = propRef.Value;
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
            var flags = (ushort)alpha.Flags;
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
            mp.DiffuseColor = mat.DiffuseColor.AsColor;
            mp.EmissiveColor = mat.EmissiveColor.AsColor;
            mp.SpecularColor = mat.SpecularColor.AsColor;
            mp.Glossiness = mat.Glossiness;
        }

        // apply texturingProperty
        if (tex != null && tex.TextureCount > 0) {
            var mt = mp.Textures;
            if (tex.BaseTexture != null) mt.Add("Main", tex.BaseTexture.Source.Value.FileName);
            if (tex.DarkTexture != null) mt.Add("Dark", tex.DarkTexture.Source.Value.FileName);
            if (tex.DetailTexture != null) mt.Add("Detail", tex.DetailTexture.Source.Value.FileName);
            if (tex.GlossTexture != null) mt.Add("Gloss", tex.GlossTexture.Source.Value.FileName);
            if (tex.GlowTexture != null) mt.Add("Glow", tex.GlowTexture.Source.Value.FileName);
            if (tex.BumpMapTexture != null) mt.Add("Bump", tex.BumpMapTexture.Source.Value.FileName);
        }
        return mp;
    }

    void AddColliderFromNiObject(NiObject niObject, Object obj) {
        if (niObject.GetType() == typeof(NiTriShape)) InstantiateNiTriShape((NiTriShape)niObject, false, true).transform.SetParent(obj.transform, false);
        else if (niObject.GetType() == typeof(AvoidNode)) { }
        else Log.Info($"Unsupported collider NiObject: {niObject.GetType().Name}");
    }

    bool IsMarkerFileName(string name) => name.ToLowerInvariant() switch {
        "marker_light" or "marker_north" or "marker_error" or "marker_arrow" or "editormarker" or "marker_creature" or
        "marker_travel" or "marker_temple" or "marker_prison" or "marker_radius" or "marker_divine" or "editormarker_box_01" => true,
        _ => false,
    };
}
