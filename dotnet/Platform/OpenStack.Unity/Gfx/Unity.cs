using System;
using UnityEngine;

namespace OpenStack.Gfx.Unity;

#region Extensions

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

#endregion
