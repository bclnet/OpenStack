using static OpenStack.Debug;

namespace System.Numerics
{
    public static class NumericsExtensions
    {
        public static void LogVector3(this Vector3 s, string label = null)
        {
            Log($"*** WriteVector3 *** - {label}");
            Log($"{s.X:F7}  {s.Y:F7}  {s.Z:F7}");
            Log();
        }

        public static void LogVector4(this Vector4 s)
        {
            Log("=============================================");
            Log($"x:{s.X:F7}  y:{s.Y:F7}  z:{s.Z:F7} w:{s.W:F7}");
        }

        public static void LogMatrix3x3(this Matrix3x3 s, string label = null)
        {
            Log($"====== {label} ===========");
            Log($"{s.M11:F7}  {s.M12:F7}  {s.M13:F7}");
            Log($"{s.M21:F7}  {s.M22:F7}  {s.M23:F7}");
            Log($"{s.M31:F7}  {s.M32:F7}  {s.M33:F7}");
        }

        public static void LogMatrix4x4(this Matrix4x4 s)
        {
            Log($"=============================================");
            Log($"{s.M11:F7}  {s.M12:F7}  {s.M13:F7}  {s.M14:F7}");
            Log($"{s.M21:F7}  {s.M22:F7}  {s.M23:F7}  {s.M24:F7}");
            Log($"{s.M31:F7}  {s.M32:F7}  {s.M33:F7}  {s.M34:F7}");
            Log($"{s.M41:F7}  {s.M42:F7}  {s.M43:F7}  {s.M44:F7}");
            Log();
        }
    }
}
