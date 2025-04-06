using OpenStack.Gfx;
using static OpenStack.Debug;

namespace OpenStack.Sdl.Renderers;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer
{
    readonly SdlGfx2dSprite Gfx;

    public TestTriRenderer(SdlGfx2dSprite gfx, object obj)
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
    readonly SdlGfx2dSprite Gfx;
    readonly object Obj;
    readonly object Sprite;

    public SpriteRenderer(SdlGfx2dSprite gfx, object obj)
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