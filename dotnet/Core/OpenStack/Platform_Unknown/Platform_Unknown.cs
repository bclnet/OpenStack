using OpenStack.Client;
using OpenStack.Gfx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;

namespace OpenStack;

#region Client

public class UnknownClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}

#endregion

#region Platform

/// <summary>
/// UnknownPlatform
/// </summary>
public class UnknownPlatform : Platform {
    public static readonly Platform This = new UnknownPlatform();
    UnknownPlatform() : base("UK", "Unknown") {
        GfxFactory = source => [null, null, null, null, null, null];
        SfxFactory = source => [null];
    }
}


#endregion
