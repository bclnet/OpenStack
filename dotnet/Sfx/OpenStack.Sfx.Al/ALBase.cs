﻿using System;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenStack.Sfx.Al;

/// <summary>
/// This is a base class for OpenAL APIs that are using DllImport and want to resolve different dll names on different platforms.
/// </summary>
public abstract class ALBase {
    /// <summary>
    /// This needs to be called before trying to use any OpenAL functions.
    /// This should be done in the static constructor of any class that DllImports OpenAL functions.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RegisterOpenALResolver() => ALLoader.RegisterDllResolver();

    /// <summary>
    /// Calls alGetProcAddress and converts the resulting pointer into a delegate.
    /// </summary>
    /// <typeparam name="TDelegate">The delegate type to create.</typeparam>
    /// <param name="name">The name of the AL proc.</param>
    /// <returns>The created delegate.</returns>
    public static TDelegate LoadDelegate<TDelegate>(string name) where TDelegate : Delegate {
        var ptr = AL.GetProcAddress(name);
        if (ptr == IntPtr.Zero) {
            // If we can't load the function for whatever reason we dynamically generate a delegate to give the user an error message that is actually understandable.
            var invoke = typeof(TDelegate).GetMethod("Invoke");
            var returnType = invoke.ReturnType;
            var parameters = invoke.GetParameters().Select(p => p.ParameterType).ToArray();
            var method = new DynamicMethod($"OpenAL_AL_Extension_Null_GetProcAddress_Exception_Delegate_{Guid.NewGuid()}", returnType, parameters);
            // Here we are generating a delegate that looks like this: ((<the arguments that the delegate type takes>) => throw new Exception(<error string>);
            var g = method.GetILGenerator();
            g.Emit(OpCodes.Ldstr, $"This OpenAL function could not be loaded. This likely means that this extension isn't present in the current context.");
            g.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new[] { typeof(string) }));
            g.Emit(OpCodes.Throw);
            return (TDelegate)method.CreateDelegate(typeof(TDelegate));
        }
        else return Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr);
    }

    public static TDelegate LoadDelegate<TDelegate>(ALDevice device, string name) where TDelegate : Delegate {
        var ptr = ALC.GetProcAddress(device, name);
        if (ptr == IntPtr.Zero) {
            // If we can't load the function for whatever reason we dynamically generate a delegate to give the user an error message that is actually understandable.
            var invoke = typeof(TDelegate).GetMethod("Invoke");
            var returnType = invoke.ReturnType;
            var parameters = invoke.GetParameters().Select(p => p.ParameterType).ToArray();
            var method = new DynamicMethod($"OpenAL_ALC_Extension_Null_GetProcAddress_Exception_Delegate_{Guid.NewGuid()}", returnType, parameters);
            // Here we are generating a delegate that looks like this: ((<the arguments that the delegate type takes>) => throw new Exception(<error string>);
            var g = method.GetILGenerator();
            g.Emit(OpCodes.Ldstr, $"This OpenAL function could not be loaded. This likely means that this extension isn't present in the current context.");
            g.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new[] { typeof(string) }));
            g.Emit(OpCodes.Throw);
            return (TDelegate)method.CreateDelegate(typeof(TDelegate));
        }
        else return Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr);
    }
}
