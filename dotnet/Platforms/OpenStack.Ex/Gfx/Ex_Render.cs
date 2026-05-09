namespace OpenStack.Gfx.Ex;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly ExGfxSprite2D GfxSprite;

    public TestTriRenderer(IOpenGfx[] gfx, object obj) {
        GfxSprite = (ExGfxSprite2D)gfx[GfX.XSprite2D];
    }
}

#endregion

#region SpriteRenderer

/// <summary>
/// SpriteRenderer
/// </summary>
public class SpriteRenderer : Renderer {
    readonly ExGfxSprite2D GfxSprite;
    readonly object Obj;
    readonly object Sprite;

    public SpriteRenderer(IOpenGfx[] gfx, object obj) {
        GfxSprite = (ExGfxSprite2D)gfx[GfX.XSprite2D];
        Obj = obj;
        GfxSprite.SpriteManager.DeleteSprite(obj);
        Sprite = GfxSprite.SpriteManager.CreateSprite(obj).spr;
    }

    public override void Start() {
        Log.Info($"MakeSprite");
        Log.Info($"Done");
    }
}

#endregion