using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenStack.Sfx.Al;

/// <summary>
/// Provides a base for ApiContext so that it can register dll intercepts.
/// </summary>
internal static class ALLoader
{
    static readonly OpenALLibraryNameContainer ALLibraryNameContainer = new OpenALLibraryNameContainer();
    static bool RegisteredResolver = false;
    static ALLoader() => RegisterDllResolver();

    internal static void RegisterDllResolver()
    {
        if (!RegisteredResolver) { NativeLibrary.SetDllImportResolver(typeof(ALLoader).Assembly, ImportResolver); RegisteredResolver = true; }
    }

    static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == AL.Lib || libraryName == ALC.Lib)
        {
            var libName = ALLibraryNameContainer.GetLibraryName();
            if (!NativeLibrary.TryLoad(libName, assembly, searchPath, out var libHandle))
                throw new DllNotFoundException($"Could not load the dll '{libName}' (this load is intercepted, specified in DllImport as '{libraryName}').");
            return libHandle;
        }
        else return NativeLibrary.Load(libraryName, assembly, searchPath);
    }
}

/// <summary>
/// Contains the library name of OpenAL.
/// </summary>
public class OpenALLibraryNameContainer
{
    /// <summary>
    /// Gets the library name to use on Windows.
    /// </summary>
    public string Windows => "openal32.dll";
    /// <summary>
    /// Gets the library name to use on Linux.
    /// </summary>
    public string Linux => "libopenal.so.1";
    /// <summary>
    /// Gets the library name to use on MacOS.
    /// </summary>
    public string MacOS => "/System/Library/Frameworks/OpenAL.framework/OpenAL";
    /// <summary>
    /// Gets the library name to use on Android.
    /// </summary>
    public string Android => Linux;
    /// <summary>
    /// Gets the library name to use on iOS.
    /// </summary>
    public string IOS => MacOS;
    public string GetLibraryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")) ? Android : Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return Windows;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS")) ? IOS : MacOS;
        else throw new NotSupportedException($"The library name couldn't be resolved for the given platform ('{RuntimeInformation.OSDescription}').");
    }
}

internal class ConstCharPtrMarshaler : ICustomMarshaler
{
    static readonly ConstCharPtrMarshaler Instance = new ConstCharPtrMarshaler();
    public void CleanUpManagedData(object ManagedObj) { }
    public void CleanUpNativeData(IntPtr pNativeData) { }
    public int GetNativeDataSize() => IntPtr.Size;
    public IntPtr MarshalManagedToNative(object ManagedObj)
        => ManagedObj switch
        {
            string str => Marshal.StringToHGlobalAnsi(str),
            _ => throw new ArgumentException($"{nameof(ConstCharPtrMarshaler)} only supports marshaling of strings. Got '{ManagedObj.GetType()}'"),
        };
    public object MarshalNativeToManaged(IntPtr pNativeData) => Marshal.PtrToStringAnsi(pNativeData);
    // See https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.custommarshalers.typetotypeinfomarshaler.getinstance
    public static ICustomMarshaler GetInstance(string cookie) => Instance;
}
