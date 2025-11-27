using MathNet.Numerics.LinearAlgebra;
using static OpenStack.Log;

namespace System.Numerics;

public static class NumericsExtensions {
    #region Matrix3x3

    public static Matrix3x3 Inverse(this Matrix3x3 source) => source.ToMathMatrix().Inverse().ToMatrix3x3();
    public static Matrix3x3 Conjugate(this Matrix3x3 source) => source.ToMathMatrix().Conjugate().ToMatrix3x3();
    public static Matrix3x3 ConjugateTranspose(this Matrix3x3 source) => source.ToMathMatrix().ConjugateTranspose().ToMatrix3x3();
    public static Matrix3x3 ConjugateTransposeThisAndMultiply(this Matrix3x3 source, Matrix3x3 inputMatrix) => source.ToMathMatrix().ConjugateTransposeThisAndMultiply(inputMatrix.ToMathMatrix()).ToMatrix3x3();
    public static Vector3 Diagonal(this Matrix3x3 source) => new Vector3().ToVector3(source.ToMathMatrix().Diagonal());

    #endregion

    #region Info

    public static void LogVector3(this Vector3 s, string label = null) {
        Info($"*** WriteVector3 *** - {label}");
        Info($"{s.X:F7}  {s.Y:F7}  {s.Z:F7}");
        Info();
    }

    public static void LogVector4(this Vector4 s) {
        Info("=============================================");
        Info($"x:{s.X:F7}  y:{s.Y:F7}  z:{s.Z:F7} w:{s.W:F7}");
    }

    public static void LogMatrix3x3(this Matrix3x3 s, string label = null) {
        Info($"====== {label} ===========");
        Info($"{s.M11:F7}  {s.M12:F7}  {s.M13:F7}");
        Info($"{s.M21:F7}  {s.M22:F7}  {s.M23:F7}");
        Info($"{s.M31:F7}  {s.M32:F7}  {s.M33:F7}");
    }

    public static void LogMatrix4x4(this Matrix4x4 s) {
        Info($"=============================================");
        Info($"{s.M11:F7}  {s.M12:F7}  {s.M13:F7}  {s.M14:F7}");
        Info($"{s.M21:F7}  {s.M22:F7}  {s.M23:F7}  {s.M24:F7}");
        Info($"{s.M31:F7}  {s.M32:F7}  {s.M33:F7}  {s.M34:F7}");
        Info($"{s.M41:F7}  {s.M42:F7}  {s.M43:F7}  {s.M44:F7}");
        Info();
    }

    #endregion
}
