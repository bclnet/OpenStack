namespace OpenStack.Gfx.Mg;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly MgGfxSprite2D Gfx;

    public TestTriRenderer(MgGfxSprite2D gfx, object obj) {
        Gfx = gfx;
    }
}

#endregion

#region SpriteRenderer

/// <summary>
/// SpriteRenderer
/// </summary>
public class SpriteRenderer : Renderer {
    readonly MgGfxSprite2D Gfx;
    readonly object Obj;
    readonly object Sprite;

    public SpriteRenderer(MgGfxSprite2D gfx, object obj) {
        Gfx = gfx;
        Obj = obj;
        Gfx.SpriteManager.DeleteSprite(obj);
        Sprite = Gfx.SpriteManager.CreateSprite(obj).spr;
    }

    public override void Start() {
        Log.Info($"MakeSprite");
        Log.Info($"Done");
    }
}

#endregion