using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Drawing;

namespace OpenStack.Mg;

public static class SolidColorTextureCache {
    static readonly Dictionary<Color, Texture2D> Textures = [];
    static GraphicsDevice Device;
    public static void Load(GraphicsDevice device) => Device = device;

    public static Texture2D GetTexture(Color color) {
        if (Textures.TryGetValue(color, out var texture)) return texture;
        texture = new Texture2D(Device, 1, 1, false, SurfaceFormat.Color);
        texture.SetData([color]);
        Textures[color] = texture;
        return texture;
    }
}