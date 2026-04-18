using System;
using UnityEngine;

namespace OpenStack.Gfx.Unity;

#region Extensions

// UnityExtensions
public static class UnityExtensions {
    /// <summary>
    /// FromUnity
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static System.Numerics.Vector3 FromUnity(this Vector3 source) => new System.Numerics.Vector3(source.x, source.y, source.z);

    /// <summary>
    /// FromUnityX
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static System.Numerics.Vector3 FromUnityX(this Vector3 source) { MathX.Swap(ref source.y, ref source.z); return new System.Numerics.Vector3(source.x, source.y, source.z); }

    /// <summary>
    /// ToUnity
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Color ToUnity(this System.Drawing.Color source) => new(source.R, source.G, source.B, source.A);
    /// <summary>
    /// ToUnity
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Vector3 ToUnity(this System.Numerics.Vector3 source) => new(source.X, source.Z, source.Y);
    /// <summary>
    /// ToUnity
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Quaternion ToUnity(this System.Numerics.Quaternion source) => new(source.X, source.Y, source.Z, source.W);

    /// <summary>
    /// ToUnityX
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Vector3 ToUnityX(this System.Numerics.Vector3 source) { MathX.Swap(ref source.Y, ref source.Z); return new Vector3(source.X, source.Y, source.Z); }

    /// <summary>
    /// ToUnityRotation
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Matrix4x4 ToUnityRotation(this System.Numerics.Matrix4x4 source) => new() {
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
    public static Quaternion ToUnityQuaternionAsEulerAnglesX(this System.Numerics.Vector3 source) // NifEulerAnglesToUnityQuaternion
    {
        var newAngles = source.ToUnityX();
        return Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.x, Vector3.right) *
            Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.y, Vector3.up) *
            Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.z, Vector3.forward);
    }
}

#endregion

#region GameObjectX

public static class GameObjectX {
    public static void AttachObject(this GameObject source, AttachObjectMethod method, params object[] args) {
        GameObject v;
        switch (method) {
            case AttachObjectMethod.Find:
                v = (GameObject)args[0];
                var found = v.FindChildRecursively((string)args[1]);
                if (found != null) {
                    source.transform.position = v.transform.position;
                    source.transform.rotation = v.transform.rotation;
                    source.transform.parent = v.transform;
                    break;
                }
                goto case AttachObjectMethod.AllCenter;
            case AttachObjectMethod.Transform:
                v = (GameObject)args[0];
                source.transform.parent = v.transform;
                break;
            case AttachObjectMethod.All:
                v = (GameObject)args[0];
                source.transform.position = v.transform.position;
                source.transform.rotation = v.transform.rotation;
                source.transform.parent = v.transform;
                break;
            case AttachObjectMethod.AllCenter:
                v = (GameObject)args[0];
                source.transform.position = v.CalcVisualBoundsRecursive().center;
                source.transform.rotation = v.transform.rotation;
                source.transform.parent = v.transform;
                break;
        }
    }

    /// <summary>
    /// Creates a camera identical to the one added to new scenes by default.
    /// </summary>
    public static GameObject CreateMainCamera(Vector3 position, Quaternion orientation) {
        var s = new GameObject("Main Camera") { tag = "MainCamera" };
        s.AddComponent<Camera>();
        s.AddComponent<FlareLayer>();
        s.AddComponent<AudioListener>();
        s.transform.position = position;
        s.transform.rotation = orientation;
        return s;
    }

    public static GameObject CreateDirectionalLight(Vector3 position, Quaternion orientation) {
        var s = new GameObject("Directional Light");
        var c = s.AddComponent<Light>(); c.type = LightType.Directional;
        s.transform.position = position;
        s.transform.rotation = orientation;
        return s;
    }

    /// <summary>
    /// Creates terrain data from heights.
    /// </summary>
    /// <param name="heightPercents">Terrain height percentages ranging from 0 to 1.</param>
    /// <param name="maxHeight">The maximum height of the terrain, corresponding to a height percentage of 1.</param>
    /// <param name="heightSampleDistance">The horizontal/vertical distance between height samples.</param>
    /// <param name="terrainLayers">The textures used by the terrain.</param>
    /// <param name="alphaMap">Texture blending information.</param>
    /// <returns>A TerrainData instance.</returns>
    public static TerrainData CreateTerrainData(int offset, float[,] heightPercents, float maxHeight, float heightSampleDistance, TerrainLayer[] terrainLayers, float[,,] alphaMap) {
        Debug.Assert(heightPercents.GetLength(0) == heightPercents.GetLength(1) && maxHeight >= 0 && heightSampleDistance >= 0);
        // Create the TerrainData.
        var heightmapResolution = heightPercents.GetLength(0);
        var terrainData = new TerrainData { heightmapResolution = heightmapResolution };
        //Log($"{terrainData.heightmapResolution} == {heightmapResolution}");
        var terrainWidth = (heightmapResolution + offset) * heightSampleDistance;
        // If maxHeight is 0, leave all the heights in terrainData at 0 and make the vertical size of the terrain 1 to ensure valid AABBs.
        if (!Mathf.Approximately(maxHeight, 0)) {
            terrainData.size = new Vector3(terrainWidth, maxHeight, terrainWidth);
            terrainData.SetHeights(0, 0, heightPercents);
        }
        else terrainData.size = new Vector3(terrainWidth, 1, terrainWidth);
        terrainData.terrainLayers = terrainLayers;
        if (alphaMap != null) {
            Debug.Assert(alphaMap.GetLength(0) == alphaMap.GetLength(1));
            terrainData.alphamapResolution = alphaMap.GetLength(0);
            terrainData.SetAlphamaps(0, 0, alphaMap);
        }
        return terrainData;
    }

    /// <summary>
    /// Creates a terrain from heights.
    /// </summary>
    /// <param name="heightPercents">Terrain height percentages ranging from 0 to 1.</param>
    /// <param name="maxHeight">The maximum height of the terrain, corresponding to a height percentage of 1.</param>
    /// <param name="heightSampleDistance">The horizontal/vertical distance between height samples.</param>
    /// <param name="terrainLayers">The textures used by the terrain.</param>
    /// <param name="alphaMap">Texture blending information.</param>
    /// <param name="position">The position of the terrain.</param>
    /// <returns>A terrain GameObject.</returns>
    public static GameObject CreateTerrain(int offset, float[,] heightPercents, float maxHeight, float heightSampleDistance, TerrainLayer[] terrainLayers, float[,,] alphaMap, Vector3 position, Material materialTemplate) {
        var terrainData = CreateTerrainData(offset, heightPercents, maxHeight, heightSampleDistance, terrainLayers, alphaMap);
        return CreateTerrainFromTerrainData(terrainData, position, materialTemplate);
    }

    public static GameObject CreateTerrainFromTerrainData(TerrainData terrainData, Vector3 position, Material materialTemplate) {
        // Create the terrain game object.
        var terrainObject = new GameObject("terrain") { isStatic = true };
        var terrain = terrainObject.AddComponent<Terrain>();
        if (materialTemplate != null) terrain.materialTemplate = materialTemplate;
        terrain.terrainData = terrainData;
        terrainObject.AddComponent<TerrainCollider>().terrainData = terrainData;
        terrainObject.transform.position = position;
        return terrainObject;
    }

    /// <summary>
    /// Calculate the AABB of an object and it's descendants.
    /// </summary>
    public static Bounds CalcVisualBoundsRecursive(this GameObject gameObject) {
        Debug.Assert(gameObject != null);

        // Gets all the renderers in the object and it's descendants.
        var renderers = gameObject.transform.GetComponentsInChildren<UnityEngine.Renderer>();
        if (renderers.Length > 0) {
            // Encapsulate the first renderer.
            var visualBounds = renderers[0].bounds;
            // Encapsulate the rest of the renderers.
            for (var i = 1; i < renderers.Length; i++) visualBounds.Encapsulate(renderers[i].bounds);
            return visualBounds;
        }
        // If there are no renderers in the object or any of it's children, simply return a degenerate AABB where the object is.
        else return new Bounds(gameObject.transform.position, Vector3.zero);
    }

    /// <summary>
    /// Finds a descendant game object by name.
    /// </summary>
    public static GameObject FindChildRecursively(this GameObject parent, string name) {
        var resultTransform = parent.transform.Find(name);
        // Search through each of parent's children.
        if (resultTransform != null) return resultTransform.gameObject;
        // Perform the search recursively for each child of parent.
        foreach (Transform childTransform in parent.transform) {
            var result = FindChildRecursively(childTransform.gameObject, name);
            if (result != null) return result;
        }
        return null;
    }

    /// <summary>
    /// Finds a descendant game object with a name containing nameSubstring.
    /// </summary>
    public static GameObject FindChildWithNameSubstringRecursively(this GameObject parent, string nameSubstring) {
        // Search through each of parent's children.
        foreach (Transform childTransform in parent.transform) if (childTransform.name.Contains(nameSubstring)) return childTransform.gameObject;
        // Perform the search recursively for each child of parent.
        foreach (Transform childTransform in parent.transform) {
            var result = FindChildWithNameSubstringRecursively(childTransform.gameObject, nameSubstring);
            if (result != null) return result;
        }
        return null;
    }

    /// <summary>
    /// Find an ancestor object, or the object itself, with a tag.
    /// </summary>
    public static GameObject FindObjectWithTagUpHeirarchy(this GameObject gameObject, string tag) {
        while (gameObject != null) {
            if (gameObject.tag == tag) return gameObject;
            // Go up one level in the object hierarchy.
            var parentTransform = gameObject.transform.parent;
            gameObject = parentTransform?.gameObject;
        }
        return null;
    }

    public static GameObject FindTopLevelObject(this GameObject baseObject) {
        if (baseObject.transform.parent == null) return baseObject;
        var p = baseObject.transform;
        while (p.parent != null) {
            if (p.parent.gameObject.name == "objects") break;
            p = p.parent;
        }
        return p.gameObject;
    }

    /// <summary>
    /// Set the layer of an object and all of it's descendants.
    /// </summary>
    public static void SetLayerRecursively(this GameObject gameObject, int layer) {
        gameObject.layer = layer;
        foreach (Transform childTransform in gameObject.transform) SetLayerRecursively(childTransform.gameObject, layer);
    }

    /// <summary>
    /// Adds mesh colliders to every descandant object with a mesh filter but no mesh collider, including the object itself.
    /// </summary>
    public static void AddMissingMeshCollidersRecursively(this GameObject source) {
        MeshFilter filter;
        // If gameObject has a MeshFilter but no Collider, add a MeshCollider.
        if (source.GetComponent<Collider>() == null && (filter = source.GetComponent<MeshFilter>()) != null && filter.mesh != null) source.AddComponent<MeshCollider>();
        foreach (Transform childTransform in source.transform) AddMissingMeshCollidersRecursively(childTransform.gameObject);
    }
}

#endregion