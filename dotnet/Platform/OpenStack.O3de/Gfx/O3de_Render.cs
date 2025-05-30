﻿using System;
using static OpenStack.Debug;

namespace OpenStack.Gfx.O3de;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly O3deGfxModel Gfx;

    public TestTriRenderer(O3deGfxModel gfx, object obj) {
        Gfx = gfx;
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer : Renderer {
    readonly O3deGfxModel Gfx;
    readonly object Obj;
    readonly Range Level;
    readonly object Texture;
    int FrameDelay;

    public TextureRenderer(O3deGfxModel gfx, object obj, Range level) {
        Gfx = gfx;
        Obj = obj;
        Level = level;
        Gfx.TextureManager.DeleteTexture(obj);
        Texture = Gfx.TextureManager.CreateTexture(obj, level).tex;
    }

    public override void Start() {
        Log($"MakeTexture");
        Log($"Done");
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