using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenStack.Gfx
{
    /// <summary>
    /// GXColor
    /// </summary>
    /// <seealso cref="System.IEquatable{GameEstate.Graphics.GXColor}" />
    [StructLayout(LayoutKind.Sequential)]
    public struct GXColor : IEquatable<GXColor>
    {
        /// <summary>
        /// The r
        /// </summary>
        public float R;
        /// <summary>
        /// The g
        /// </summary>
        public float G;
        /// <summary>
        /// The b
        /// </summary>
        public float B;
        /// <summary>
        /// a
        /// </summary>
        public float A;

        public enum Format
        {
            ARGB32,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GXColor"/> struct.
        /// </summary>
        /// <param name="r">The r.</param>
        /// <param name="g">The g.</param>
        /// <param name="b">The b.</param>
        /// <param name="a">a.</param>
        public GXColor(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public GXColor(uint color, Format format)
        {
            switch (format)
            {
                case Format.ARGB32:
                    A = color >> 24;
                    R = (color >> 16) & 0xFF;
                    G = (color >> 8) & 0xFF;
                    B = color & 0xFF;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        public static implicit operator Vector4(GXColor c) => new Vector4(c.R, c.G, c.B, c.A);
        public static implicit operator GXColor(Vector4 v) => new GXColor(v.X, v.Y, v.Z, v.W);
        public static GXColor operator +(GXColor a, GXColor b) => new GXColor(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);
        public static GXColor operator -(GXColor a, GXColor b) => new GXColor(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);
        public static GXColor operator *(GXColor a, GXColor b) => new GXColor(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
        public static GXColor operator *(GXColor a, float b) => new GXColor(a.R * b, a.G * b, a.B * b, a.A * b);
        public static GXColor operator *(float b, GXColor a) => new GXColor(a.R * b, a.G * b, a.B * b, a.A * b);
        public static GXColor operator /(GXColor a, float b) => new GXColor(a.R / b, a.G / b, a.B / b, a.A / b);
        public static bool operator ==(GXColor lhs, GXColor rhs) => (lhs == rhs);
        public static bool operator !=(GXColor lhs, GXColor rhs) => !(lhs == rhs);

        public float this[int index]
        {
            get
            {
                return index switch
                {
                    0 => R,
                    1 => G,
                    2 => B,
                    3 => A,
                    _ => throw new IndexOutOfRangeException("Invalid Vector3 index"),
                };
            }
            set
            {
                switch (index)
                {
                    case 0: R = value; break;
                    case 1: G = value; break;
                    case 2: B = value; break;
                    case 3: A = value; break;
                    default: throw new IndexOutOfRangeException("Invalid Vector3 index");
                }
            }
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"RGBA({R:F3}, {G:F3}, {B:F3}, {A:F3})";
        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(string format) => $"RGBA({R.ToString(format)}, {G.ToString(format)}, {B.ToString(format)}, {A.ToString(format)})";

        public override bool Equals(object other) => other is GXColor color && Equals(color);
        public bool Equals(GXColor other) => R.Equals(other.R) && Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
        public override int GetHashCode() => (R, G, B, A).GetHashCode();

        public static GXColor Lerp(GXColor a, GXColor b, float t)
        {
            t = MathX.Clamp(t);
            return new GXColor(a.R + ((b.R - a.R) * t), a.G + ((b.G - a.G) * t), a.B + ((b.B - a.B) * t), a.A + ((b.A - a.A) * t));
        }

        public static GXColor LerpUnclamped(GXColor a, GXColor b, float t) => new(a.R + ((b.R - a.R) * t), a.G + ((b.G - a.G) * t), a.B + ((b.B - a.B) * t), a.A + ((b.A - a.A) * t));

        internal GXColor RGBMultiplied(float multiplier) => new(R * multiplier, G * multiplier, B * multiplier, A);

        internal GXColor AlphaMultiplied(float multiplier) => new(R, G, B, A * multiplier);

        internal GXColor RGBMultiplied(GXColor multiplier) => new(R * multiplier.R, G * multiplier.G, B * multiplier.B, A);

        public static GXColor Red => new(1f, 0f, 0f, 1f);
        public static GXColor Green => new(0f, 1f, 0f, 1f);
        public static GXColor Blue => new(0f, 0f, 1f, 1f);
        public static GXColor White => new(1f, 1f, 1f, 1f);
        public static GXColor Black => new(0f, 0f, 0f, 1f);
        public static GXColor Yellow => new(1f, 0.9215686f, 0.01568628f, 1f);
        public static GXColor Cyan => new(0f, 1f, 1f, 1f);
        public static GXColor Magenta => new(1f, 0f, 1f, 1f);
        public static GXColor Gray => new(0.5f, 0.5f, 0.5f, 1f);
        public static GXColor Clear => new(0f, 0f, 0f, 0f);

        public float Grayscale => (((0.299f * R) + (0.587f * G)) + (0.114f * B));

        //public Color Linear => new Color(Mathx.GammaToLinearSpace(R), Mathx.GammaToLinearSpace(G), Mathx.GammaToLinearSpace(B), A);
        //public Color Gamma => new Color(Mathx.LinearToGammaSpace(R), Mathx.LinearToGammaSpace(G), Mathx.LinearToGammaSpace(B), A);

        public float MaxColorComponent => Math.Max(Math.Max(R, G), B);

        #region HSV

        public static void RGBToHSV(GXColor rgbColor, out float H, out float S, out float V)
        {
            if (rgbColor.B > rgbColor.G && rgbColor.B > rgbColor.R) RGBToHSVHelper(4f, rgbColor.B, rgbColor.R, rgbColor.G, out H, out S, out V);
            else if (rgbColor.G > rgbColor.R) RGBToHSVHelper(2f, rgbColor.G, rgbColor.B, rgbColor.R, out H, out S, out V);
            else RGBToHSVHelper(0f, rgbColor.R, rgbColor.G, rgbColor.B, out H, out S, out V);
        }

        static void RGBToHSVHelper(float offset, float dominantcolor, float colorone, float colortwo, out float H, out float S, out float V)
        {
            V = dominantcolor;
            if (V == 0f)
            {
                S = 0f;
                H = 0f;
            }
            else
            {
                var color = colorone <= colortwo ? colorone : colortwo;
                var color2 = V - color;
                if (color2 != 0f) { S = color2 / V; H = offset + ((colorone - colortwo) / color2); }
                else { S = 0f; H = offset + (colorone - colortwo); }
                H /= 6f;
                if (H < 0f) H++;
            }
        }

        public static GXColor HSVToRGB(float h, float s, float v) => HSVToRGB(h, s, v, true);

        public static GXColor HSVToRGB(float h, float s, float v, bool hdr)
        {
            var white = White;
            if (s == 0f) { white.R = v; white.G = v; white.B = v; }
            else if (v == 0f) { white.R = 0f; white.G = 0f; white.B = 0f; }
            else
            {
                white.R = 0f; white.G = 0f; white.B = 0f;
                var r0 = v;
                var f = h * 6f;
                var whole = (int)Math.Floor(f);
                var remain = f - whole;
                var r1 = r0 * (1f - s);
                var r2 = r0 * (1f - (s * remain));
                var r3 = r0 * (1f - (s * (1f - remain)));
                switch (whole)
                {
                    case -1: white.R = r0; white.G = r1; white.B = r2; break;
                    case 0: white.R = r0; white.G = r3; white.B = r1; break;
                    case 1: white.R = r2; white.G = r0; white.B = r1; break;
                    case 2: white.R = r1; white.G = r0; white.B = r3; break;
                    case 3: white.R = r1; white.G = r2; white.B = r0; break;
                    case 4: white.R = r3; white.G = r1; white.B = r0; break;
                    case 5: white.R = r0; white.G = r1; white.B = r2; break;
                    case 6: white.R = r0; white.G = r3; white.B = r1; break;
                    default: break;
                }
                if (!hdr) { white.R = MathX.Clamp(white.R, 0f, 1f); white.G = MathX.Clamp(white.G, 0f, 1f); white.B = MathX.Clamp(white.B, 0f, 1f); }
            }
            return white;
        }

        #endregion
    }

    /// <summary>
    /// GXColor32
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct GXColor32
    {
        [FieldOffset(0)] int _rgba;
        /// <summary>
        /// The r
        /// </summary>
        [FieldOffset(0)] public byte R;
        /// <summary>
        /// The g
        /// </summary>
        [FieldOffset(1)] public byte G;
        /// <summary>
        /// The b
        /// </summary>
        [FieldOffset(2)] public byte B;
        /// <summary>
        /// a
        /// </summary>
        [FieldOffset(3)] public byte A;

        /// <summary>
        /// Initializes a new instance of the <see cref="GXColor32"/> struct.
        /// </summary>
        /// <param name="r">The r.</param>
        /// <param name="g">The g.</param>
        /// <param name="b">The b.</param>
        /// <param name="a">a.</param>
        public GXColor32(byte r, byte g, byte b, byte a)
        {
            _rgba = 0;
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="GXColor"/> to <see cref="GXColor32"/>.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator GXColor32(GXColor c) => new((byte)(MathX.Clamp(c.R) * 0xfff), (byte)(MathX.Clamp(c.G) * 0xfff), (byte)(MathX.Clamp(c.B) * 0xfff), (byte)(MathX.Clamp(c.A) * 0xfff));

        /// <summary>
        /// Performs an implicit conversion from <see cref="GXColor32"/> to <see cref="GXColor"/>.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator GXColor(GXColor32 c) => new(((float)c.R) / 0xfff, ((float)c.G) / 0xfff, ((float)c.B) / 0xfff, ((float)c.A) / 0xfff);

        /// <summary>
        /// Lerps the specified a.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="t">The t.</param>
        /// <returns></returns>
        public static GXColor32 Lerp(GXColor32 a, GXColor32 b, float t)
        {
            t = MathX.Clamp(t);
            return new GXColor32((byte)(a.R + ((b.R - a.R) * t)), (byte)(a.G + ((b.G - a.G) * t)), (byte)(a.B + ((b.B - a.B) * t)), (byte)(a.A + ((b.A - a.A) * t)));
        }

        /// <summary>
        /// Lerps the unclamped.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="t">The t.</param>
        /// <returns></returns>
        public static GXColor32 LerpUnclamped(GXColor32 a, GXColor32 b, float t) => new((byte)(a.R + ((b.R - a.R) * t)), (byte)(a.G + ((b.G - a.G) * t)), (byte)(a.B + ((b.B - a.B) * t)), (byte)(a.A + ((b.A - a.A) * t)));

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"RGBA({R}, {G}, {B}, {A})";
        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(string format) => $"RGBA({R.ToString(format)}, {G.ToString(format)}, {B.ToString(format)}, {A.ToString(format)})";
    }
}