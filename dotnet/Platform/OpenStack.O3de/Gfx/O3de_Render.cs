using System;

namespace OpenStack.Gfx.O3de;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly O3deGfxModel GfxModel;

    public TestTriRenderer(IOpenGfx[] gfx, object obj) {
        GfxModel = (O3deGfxModel)gfx[GfX.XModel];
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer : Renderer {
    readonly O3deGfxModel GfxModel;
    readonly object Obj;
    readonly Range Level;
    readonly object Texture;
    int FrameDelay;

    public TextureRenderer(IOpenGfx[] gfx, object obj, Range level) {
        GfxModel = (O3deGfxModel)gfx[GfX.XModel];
        Obj = obj;
        Level = level;
        GfxModel.TextureManager.DeleteTexture(obj);
        Texture = GfxModel.TextureManager.CreateTexture(obj, level).tex;
    }

    public override void Start() {
        Log.Info($"MakeTexture");
        Log.Info($"Done");
    }

    public override void Update(float deltaTime) {
        if (Obj is not ITextureFrames obj || GfxModel == null || !obj.HasFrames) return;
        FrameDelay += (int)deltaTime;
        if (FrameDelay <= obj.Fps || !obj.DecodeFrame()) return;
        FrameDelay = 0; // reset delay between frames
        GfxModel.TextureManager.ReloadTexture(obj, Level);
    }
}

#endregion