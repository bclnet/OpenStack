using OpenStack.Client;
using OpenStack.Gfx;
using OpenStack.Sfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OpenStack;

#region Client

public class TestClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}

#endregion

#region Platform

public class TestGfxSprite(ISource source) : IOpenGfxSprite {
    readonly ISource _source = source;
    public object Source => _source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public void PreloadSprite(object path) => throw new NotSupportedException();
    public void PreloadObject(object path) => throw new NotSupportedException();
}

public class TestGfxModel(ISource source) : IOpenGfxModel {
    readonly ISource _source = source;
    public object Source => _source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public void PreloadTexture(object path) => throw new NotSupportedException();
    public void PreloadObject(object path) => throw new NotSupportedException();
}

public class TestSfx(ISource source) : IOpenSfx {
    readonly ISource _source = source;
    public object Source => _source;
}

/// <summary>
/// TestPlatform
/// </summary>
public class TestPlatform : Platform {
    public static readonly Platform This = new TestPlatform();
    TestPlatform() : base("TT", "Test") {
        GfxFactory = source => [new TestGfxSprite(source), new TestGfxSprite(source), new TestGfxModel(source)];
        SfxFactory = source => [new TestSfx(source)];
    }
}

#endregion

