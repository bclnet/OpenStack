using System;
using System.Collections.Generic;

namespace OpenStk.Gfx.Sdl;

#region Extensions

// SdlX
public static class SdlX {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];

}

#endregion
