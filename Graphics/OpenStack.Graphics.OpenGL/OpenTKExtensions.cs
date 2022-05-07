using System.Numerics;

namespace OpenStack.Graphics
{
    public static class OpenTKExtensions
    {
        public static OpenTK.Vector3 ToOpenTK(this Vector3 vec) => new OpenTK.Vector3(vec.X, vec.Y, vec.Z);
        public static OpenTK.Vector4 ToOpenTK(this Vector4 vec) => new OpenTK.Vector4(vec.X, vec.Y, vec.Z, vec.W);
        public static OpenTK.Matrix4 ToOpenTK(this Matrix4x4 m) => new OpenTK.Matrix4(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);
    }
}
