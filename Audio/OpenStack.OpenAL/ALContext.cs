﻿using System.Diagnostics.CodeAnalysis;

namespace System.NumericsX.OpenAL
{
    public struct ALContext : IEquatable<ALContext>
    {
        public static readonly ALContext Null = new(IntPtr.Zero);

        public IntPtr Handle;

        public ALContext(IntPtr handle)
            => Handle = handle;

        public override bool Equals(object obj)
            => obj is ALContext handle && Equals(handle);

        public bool Equals([AllowNull] ALContext other)
            => Handle.Equals(other.Handle);

        public override int GetHashCode()
            => HashCode.Combine(Handle);

        public static bool operator ==(ALContext left, ALContext right)
            => left.Equals(right);

        public static bool operator !=(ALContext left, ALContext right)
            => !(left == right);

        public static implicit operator IntPtr(ALContext context) => context.Handle;
    }
}
