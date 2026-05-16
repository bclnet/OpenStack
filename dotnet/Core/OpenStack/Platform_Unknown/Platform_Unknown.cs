using OpenStack.Client;
using System;
using System.Collections.Generic;

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
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
    public static readonly Platform This = new UnknownPlatform();
    UnknownPlatform() : base("UK", "Unknown") {
        GfxFactory = source => [null, null, null, null, null, null];
        SfxFactory = source => [null];
    }
}

#endregion
