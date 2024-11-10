namespace System.IO
{
    public static partial class Polyfill
    {
        #region Lump

        // lumps
        public struct X_LumpON { public int Offset; public int Num; }

        public struct X_LumpNO { public int Num; public int Offset; }

        public struct X_LumpNO2 { public int Num; public int Offset; public int Offset2; }

        public struct X_Lump2NO { public int Num; public int Offset; public int Offset2; }

        #endregion
    }
}