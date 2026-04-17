using OpenStack.Client;
using OpenStack.Gfx;
using System;
using System.Threading.Tasks;

namespace OpenStack;

#region Client

public class UnknownClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}

#endregion

#region Platform

public class UnknownGfxSprite(ISource source) : IOpenGfxSprite {
    readonly ISource _source = source;
    public object Source => _source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public void PreloadSprite(object path) => throw new NotSupportedException();
    public void PreloadObject(object path) => throw new NotSupportedException();
}

public class UnknownGfxModel(ISource source) : IOpenGfxModel {
    readonly ISource _source = source;
    public object Source => _source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public void PreloadTexture(object path) => throw new NotSupportedException();
    public void PreloadObject(object path) => throw new NotSupportedException();
}

/// <summary>
/// UnknownPlatform
/// </summary>
public class UnknownPlatform : Platform {
    public static readonly Platform This = new UnknownPlatform();
    UnknownPlatform() : base("UK", "Unknown") {
        GfxFactory = source => [new UnknownGfxSprite(source), new UnknownGfxSprite(source), new UnknownGfxModel(source)];
        SfxFactory = source => [new SystemSfx(source)];
    }
}


#endregion
