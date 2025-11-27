using System;

namespace OpenStack.Gfx.Ogre;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly OgreGfxModel Gfx;

    public TestTriRenderer(OgreGfxModel gfx, object obj) {
        Gfx = gfx;
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer : Renderer {
    readonly OgreGfxModel Gfx;
    readonly object Obj;
    readonly Range Level;
    readonly object Texture;
    int FrameDelay;

    public TextureRenderer(OgreGfxModel gfx, object obj, Range level) {
        Gfx = gfx;
        Obj = obj;
        Level = level;
        Gfx.TextureManager.DeleteTexture(obj);
        Texture = Gfx.TextureManager.CreateTexture(obj, level).tex;
    }

    public override void Start() {
        Log.Info($"MakeTexture");
        Log.Info($"Done");
    }

    public override void Update(float deltaTime) {
        if (Obj is not ITextureFrames obj || Gfx == null || !obj.HasFrames) return;
        FrameDelay += (int)deltaTime;
        if (FrameDelay <= obj.Fps || !obj.DecodeFrame()) return;
        FrameDelay = 0; // reset delay between frames
        Gfx.TextureManager.ReloadTexture(obj, Level);
    }
}

#endregion