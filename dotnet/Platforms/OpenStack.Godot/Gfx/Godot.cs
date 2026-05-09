using Godot;

namespace OpenStack.Gfx.Godot;

#region Extensions

// GodotExtensions
public static class GodotExtensions {
    /// <summary>
    /// ToGodot
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Vector3 ToGodot(this System.Numerics.Vector3 source) => new(source.X, source.Z, source.Y);
    /// <summary>
    /// ToGodot
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Quaternion ToGodot(this System.Numerics.Quaternion source) => new(source.X, source.Y, source.Z, source.W);

    /// <summary>
    /// Adds mesh colliders to every descandant object with a mesh filter but no mesh collider, including the object itself.
    /// </summary>
    public static void AddMissingMeshCollidersRecursively(this Node3D source, bool isStatic = true) {
        if (!isStatic) return;
    }
}

#endregion
