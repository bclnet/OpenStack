using System;
using System.Collections.Generic;

namespace OpenStack.Gfx.O3de;

#region Extensions

// O3deX
public static class O3deX {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
}

#endregion
