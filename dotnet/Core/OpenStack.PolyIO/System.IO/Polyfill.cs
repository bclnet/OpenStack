using System.Numerics;
using System.Runtime.InteropServices;

namespace System.IO;

public static partial class Polyfill {
    #region Lump

    // lumps
    [StructLayout(LayoutKind.Sequential)]
    public struct X_LumpON { public int Offset; public int Num; }

    [StructLayout(LayoutKind.Sequential)]
    public struct X_LumpNO { public int Num; public int Offset; }

    [StructLayout(LayoutKind.Sequential)]
    public struct X_LumpNO2 { public int Num; public int Offset; public int Offset2; }

    [StructLayout(LayoutKind.Sequential)]
    public struct X_Lump2NO { public int Num; public int Offset; public int Offset2; }

    #endregion

    #region BoundBox

    [StructLayout(LayoutKind.Sequential)]
    public struct X_BoundBox {
        public Vector3 Min;                // minimum values of X,Y,Z
        public Vector3 Max;                // maximum values of X,Y,Z
    }

    #endregion
}