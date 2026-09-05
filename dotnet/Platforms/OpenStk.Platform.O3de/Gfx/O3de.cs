using System;
using System.Collections.Generic;

namespace OpenStk.Gfx.O3de;

#region Extensions

// O3deX
public static class O3deX {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
}

#endregion
