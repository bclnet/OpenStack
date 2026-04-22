using System;
using UnityEngine;
using static OpenStack.CellManager;
using XShader = UnityEngine.Shader;

namespace OpenStack.Gfx.Unity;

#region Extensions

// UnityExtensions
public static class UnityExtensions {
    /// <summary>
    /// FromUnity
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static System.Numerics.Vector3 FromUnity(this Vector3 source) => new(source.x, source.z, source.y);

    /// <summary>
    /// ToUnity
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Vector2 ToUnity(this System.Numerics.Vector2 source) => new(source.X, source.Y);
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
    /// ToUnityRotation
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Matrix4x4 ToUnityRotation(this System.Numerics.Matrix4x4 source) => new() { m00 = source.M11, m01 = source.M13, m02 = source.M12, m03 = 0, m10 = source.M31, m11 = source.M33, m12 = source.M32, m13 = 0, m20 = source.M21, m21 = source.M23, m22 = source.M22, m23 = 0, m30 = 0, m31 = 0, m32 = 0, m33 = 1 };

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
        var newAngles = source.ToUnity();
        return Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.x, Vector3.right) *
            Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.y, Vector3.up) *
            Quaternion.AngleAxis(Mathf.Rad2Deg * newAngles.z, Vector3.forward);
    }

    /// <summary>
    /// ToUnityX
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    //public static Vector3 ToUnity(this System.Numerics.Vector3 source) { MathX.Swap(ref source.Y, ref source.Z); return new Vector3(source.X, source.Y, source.Z); }

    /// <summary>
    /// FromUnityX
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    //public static System.Numerics.Vector3 FromUnityX(this Vector3 source) { MathX.Swap(ref source.y, ref source.z); return new System.Numerics.Vector3(source.x, source.y, source.z); }
}

#endregion

#region GameObjectX

public static class GameObjectX {
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
        var light = s.AddComponent<Light>(); light.type = LightType.Directional;
        s.transform.position = position;
        s.transform.rotation = orientation;
        return s;
    }

    /// <summary>
    /// Creates a terrain from heights.
    /// </summary>
    /// <param name="offset">offset.</param>
    /// <param name="heights">Terrain height percentages ranging from 0 to 1.</param>
    /// <param name="maxHeight">The maximum height of the terrain, corresponding to a height percentage of 1.</param>
    /// <param name="sampleDistance">The horizontal/vertical distance between height samples.</param>
    /// <param name="layers">The textures used by the terrain.</param>
    /// <param name="alphaMap">Texture blending information.</param>
    /// <param name="position">The position of the terrain.</param>
    /// <param name="template">The material template.</param>
    /// <param name="parent">The parent.</param>
    /// <returns>A terrain GameObject.</returns>
    //public static GameObject CreateTerrain(int offset, float[,] heights, float heightRange, float sampleDistance, TerrainLayer[] layers, float[,,] alphaMap, Vector3 position, Material template, GameObject parent = default) {
    //    var data = CreateTerrainData(offset, heights, heightRange, sampleDistance, layers, alphaMap);
    //    var s = CreateTerrain(data, position, template);
    //    return s;
    //}

    /// <summary>
    /// Creates terrain data from heights.
    /// </summary>
    /// <param name="heights">Terrain height percentages ranging from 0 to 1.</param>
    /// <param name="heightRange">The maximum height of the terrain, corresponding to a height percentage of 1.</param>
    /// <param name="sampleDistance">The horizontal/vertical distance between height samples.</param>
    /// <param name="layers">The textures used by the terrain.</param>
    /// <param name="alphaMap">Texture blending information.</param>
    /// <returns>A TerrainData instance.</returns>
    public static TerrainData CreateTerrainData(int offset, float[,] heights, float heightRange, float sampleDistance, TerrainLayer[] layers, float[,,] alphaMap) {
        Debug.Assert(heights.GetLength(0) == heights.GetLength(1) && heightRange >= 0 && sampleDistance >= 0);
        // Create the TerrainData.
        var heightmapResolution = heights.GetLength(0);
        var s = new TerrainData { heightmapResolution = heightmapResolution };
        //Log($"{terrainData.heightmapResolution} == {heightmapResolution}");
        var terrainWidth = (heightmapResolution + offset) * sampleDistance;
        // If maxHeight is 0, leave all the heights in terrainData at 0 and make the vertical size of the terrain 1 to ensure valid AABBs.
        if (!Mathf.Approximately(heightRange, 0)) { s.size = new Vector3(terrainWidth, heightRange, terrainWidth); s.SetHeights(0, 0, heights); }
        else s.size = new Vector3(terrainWidth, 1, terrainWidth);
        s.terrainLayers = layers;
        if (alphaMap != null) { Debug.Assert(alphaMap.GetLength(0) == alphaMap.GetLength(1)); s.alphamapResolution = alphaMap.GetLength(0); s.SetAlphamaps(0, 0, alphaMap); }
        return s;
    }

    public static GameObject CreateTerrain(TerrainData data, Vector3 position, Material template, GameObject parent = default) {
        // Create the terrain game object.
        var s = new GameObject("terrain") { isStatic = true };
        var terrain = s.AddComponent<Terrain>();
        if (template != null) terrain.materialTemplate = template;
        terrain.terrainData = data;
        s.AddComponent<TerrainCollider>().terrainData = data;
        s.transform.position = position;
        //s.GetComponent<Terrain>().materialType = Terrain.MaterialType.BuiltInLegacyDiffuse;
        if (parent != null) s.transform.parent = parent.transform;
        return s;
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

#region CellManager

public class UnityCellBuilder(IQuery query, IOpenGfx[] gfx) : CellBuilder<GameObject, Material, Texture2D, XShader>(query, gfx) { }

#endregion

#region InputManager

public static class InputManager {
    //struct XRButtonMapping(XRButton button, bool left) {
    //    public XRButton Button { get; set; } = button;
    //    public bool LeftHand { get; set; } = left;
    //}

    //static Dictionary<string, XRButtonMapping> XRMapping = new()
    //{
    //    { "Jump", new XRButtonMapping(XRButton.Thumbstick, true) },
    //    { "Light", new XRButtonMapping(XRButton.Thumbstick, false) },
    //    { "Run", new XRButtonMapping(XRButton.Grip, true) },
    //    { "Slow", new XRButtonMapping(XRButton.Grip, false) },
    //    { "Attack", new XRButtonMapping(XRButton.Trigger, false) },
    //    { "Recenter", new XRButtonMapping(XRButton.Menu, false) },
    //    { "Use", new XRButtonMapping(XRButton.Trigger, true) },
    //    { "Menu", new XRButtonMapping(XRButton.Menu, true) }
    //};

    public static float GetAxis(string axis) {
        var result = 1.0f; // Input.GetAxis(axis);
        //if (XRSettings.enabled) {
        //    var input = XRInput.Instance;
        //    if (axis == "Horizontal") result += input.GetAxis(XRAxis.ThumbstickX, true);
        //    else if (axis == "Vertical") result += input.GetAxis(XRAxis.ThumbstickY, true);
        //    else if (axis == "Mouse X") result += input.GetAxis(XRAxis.ThumbstickX, false);
        //    else if (axis == "Mouse Y") result += input.GetAxis(XRAxis.ThumbstickY, false);
        //    // Deadzone
        //    if (Mathf.Abs(result) < 0.15f) result = 0.0f;
        //}
        return result;
    }

    public static bool GetButton(string button) {
        var result = false; // Input.GetButtonDown(button);
        //if (XRSettings.enabled) {
        //    var input = XRInput.Instance;
        //    if (XRMapping.ContainsKey(button)) {
        //        var mapping = XRMapping[button];
        //        result |= input.GetButton(mapping.Button, mapping.LeftHand);
        //    }
        //}
        return result;
    }

    public static bool GetButtonUp(string button) {
        var result = false; // UnityEngine.Input.GetButtonUp(button);
        //if (XRSettings.enabled) {
        //    var input = XRInput.Instance;
        //    if (XRMapping.ContainsKey(button)) { var mapping = XRMapping[button]; result |= input.GetButtonUp(mapping.Button, mapping.LeftHand); }
        //}
        return result;
    }

    public static bool GetButtonDown(string button) {
        var result = false; // UnityEngine.Input.GetButtonDown(button);
        //if (XRSettings.enabled) {
        //    var input = XRInput.Instance;
        //    if (XRMapping.ContainsKey(button)) { var mapping = XRMapping[button]; result |= input.GetButtonDown(mapping.Button, mapping.LeftHand); }
        //}
        return result;
    }

    internal static bool GetKeyDown(KeyCode tab) {
        throw new NotImplementedException();
    }

    internal static bool GetMouseButtonDown(int v) {
        throw new NotImplementedException();
    }
}

#endregion