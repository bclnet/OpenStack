using System;
using System.Collections.Generic;

namespace OpenStack.Gfx.Ogre;

#region Extensions

// OgreX
public static class OgreX {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];

}

#endregion
