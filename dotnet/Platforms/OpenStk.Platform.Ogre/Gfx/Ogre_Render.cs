using System;

namespace OpenStack.Gfx.Ogre;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly OgreGfxModel GfxModel;

    public TestTriRenderer(IOpenGfx[] gfx, ISource source, object obj) {
        GfxModel = (OgreGfxModel)gfx[GfX.XModel];
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer : Renderer {
    readonly OgreGfxModel GfxModel;
    readonly ISource Source;
    readonly object Obj;
    readonly Range Level;
    readonly object Texture;
    int FrameDelay;

    public TextureRenderer(IOpenGfx[] gfx, ISource source, object obj, Range level) {
        GfxModel = (OgreGfxModel)gfx[GfX.XModel];
        Source = source;
        Obj = obj;
        Level = level;
        GfxModel.TextureManager.DeleteTexture(source, obj);
        (Texture, _) = GfxModel.TextureManager.CreateTexture(source, obj, level).Result;
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
        GfxModel.TextureManager.ReloadTexture(Source, obj, Level);
    }
}

#endregion