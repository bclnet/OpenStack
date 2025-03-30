using OpenStack.Gfx;
using static OpenStack.Debug;

namespace OpenStack.Sdl.Renderers;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer
{
    readonly ISdlGfx2d Gfx;

    public TestTriRenderer(ISdlGfx2d gfx, object obj)
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
    readonly ISdlGfx2d Gfx;
    readonly object Obj;
    readonly object Sprite;

    public SpriteRenderer(ISdlGfx2d gfx, object obj)
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