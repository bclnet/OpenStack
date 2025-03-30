namespace OpenStack.Gfx.Sprite;

#region ISprite

/// <summary>
/// ISprite
/// </summary>
public interface ISprite
{
    int Width { get; }
    int Height { get; }
    //TextureFlags TexFlags { get; }
    (byte[] bytes, object format) Begin(string platform);
    void End();
}

#endregion
