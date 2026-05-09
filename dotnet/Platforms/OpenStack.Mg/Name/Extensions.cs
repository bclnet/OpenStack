using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace OpenStack.Mg;

public static class Extensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float AngleBetween(this Vector2 source, Vector2 to) => (float)Math.Atan2(to.Y - source.Y, to.X - source.X);
}
