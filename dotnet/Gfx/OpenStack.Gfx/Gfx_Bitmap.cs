using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenStack.Gfx;

/// <summary>
/// DirectBitmap
/// </summary>
public class DirectBitmap : IDisposable {
    static bool UseDrawing;
    bool Disposed;
    public IDisposable Bitmap;
    public int[] Pixels;
    public int Height;
    public int Width;
    GCHandle PixelsHandle;

    public DirectBitmap(int width, int height) {
        Width = width;
        Height = height;
        Pixels = new int[width * height];
        PixelsHandle = GCHandle.Alloc(Pixels, GCHandleType.Pinned);
        Bitmap = UseDrawing
            ? new Bitmap(width, height, width * sizeof(int), PixelFormat.Format32bppPArgb, PixelsHandle.AddrOfPinnedObject())
            : null; // new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unknown);
    }

    public void Dispose() {
        if (Disposed) return; Disposed = true;
        Bitmap.Dispose();
        PixelsHandle.Free();
    }

    public void SetPixel(int x, int y, Color color) => Pixels[x + (y * Width)] = color.ToArgb();

    public Color GetPixel(int x, int y) => Color.FromArgb(Pixels[x + (y * Width)]);

    public void Save(string path) {
        if (path != "path" && UseDrawing) ((Bitmap)Bitmap).Save(path, ImageFormat.Png);
    }
}
