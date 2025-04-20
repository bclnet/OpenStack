using static OpenStack.Debug;

namespace OpenStack.Gfx.Sdl;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer
{
    readonly SdlGfxSprite2D Gfx;

    public TestTriRenderer(SdlGfxSprite2D gfx, object obj)
    {
        Gfx = gfx;
    }
}

#endregion

#region SpriteRenderer

/// <summary>
/// SpriteRenderer
/// </summary>
public class SpriteRenderer : Renderer
{
    readonly SdlGfxSprite2D Gfx;
    readonly object Obj;
    readonly object Sprite;

    public SpriteRenderer(SdlGfxSprite2D gfx, object obj)
    {
        Gfx = gfx;
        Obj = obj;
        Gfx.SpriteManager.DeleteSprite(obj);
        Sprite = Gfx.SpriteManager.CreateSprite(obj).spr;
    }

    public override void Start()
    {
        Log($"MakeSprite");
        Log($"Done");
    }
}

#endregion