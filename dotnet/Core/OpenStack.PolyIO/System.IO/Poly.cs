using System.Numerics;
using System.Runtime.InteropServices;

namespace System.IO;

public static partial class Poly {
    #region BoundBox

    [StructLayout(LayoutKind.Sequential)]
    public struct X_BoundBox {
        public Vector3 Min;                // Minimum values of X,Y,Z
        public Vector3 Max;                // Maximum values of X,Y,Z
    }

    #endregion
}