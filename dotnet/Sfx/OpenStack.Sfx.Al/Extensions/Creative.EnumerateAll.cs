using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenStack.Sfx.Al.Extensions;

/// <summary>
/// Exposes the API in the EnumerateAll extension.
/// </summary>
public class EnumerateAll : ALBase
{
    /// <summary>
    /// The name of this AL extension.
    /// </summary>
    public const string ExtensionName = "ALC_ENUMERATE_ALL_EXT";

    // We need to register the resolver for OpenAL before we can DllImport functions.
    static EnumerateAll() => RegisterOpenALResolver();
    EnumerateAll() { }

    /// <summary>
    /// Checks whether the extension is present.
    /// </summary>
    /// <returns>Whether the extension was present or not.</returns>
    public static bool IsExtensionPresent() => ALC.IsExtensionPresent(ALDevice.Null, ExtensionName);

    /// <summary>
    /// Checks whether the extension is present.
    /// </summary>
    /// <param name="device">The device to be queried.</param>
    /// <returns>Whether the extension was present or not.</returns>
    public static bool IsExtensionPresent(ALDevice device) => ALC.IsExtensionPresent(device, ExtensionName);

    /// <summary>
    /// Gets a named property on the context.
    /// </summary>
    /// <param name="device">The device for the context.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    [DllImport(ALC.Lib, EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = ALC.AlcCallingConv)] public static extern string GetString(ALDevice device, GetEnumerateAllContextString param);

    /// <summary>
    /// Gets a named property on the context.
    /// </summary>
    /// <param name="device">The device for the context.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    [DllImport(ALC.Lib, EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = ALC.AlcCallingConv)] public static extern unsafe byte* GetStringList(ALDevice device, GetEnumerateAllContextStringList param);

    /// <inheritdoc cref="GetStringList(ALDevice, GetEnumerateAllContextStringList)"/>
    public static unsafe IEnumerable<string> GetStringList(GetEnumerateAllContextStringList param) { var result = GetStringList(ALDevice.Null, param); return ALC.ALStringListToList(result); }
}

#region Enums

/// <summary>
/// Defines available parameters for <see cref="EnumerateAll.GetString(ALDevice, GetEnumerateAllContextString)" />.
/// </summary>
public enum GetEnumerateAllContextString
{
    /// <summary>
    /// Gets the specifier for the default device.
    /// </summary>
    DefaultAllDevicesSpecifier = 0x1012,
    /// <summary>
    /// Gets the specifier of the first available device.
    /// </summary>
    AllDevicesSpecifier = 0x1013,
}

/// <summary>
/// Defines available parameters for <see cref="EnumerateAll.GetStringList(ALDevice, GetEnumerateAllContextStringList)" />.
/// </summary>
public enum GetEnumerateAllContextStringList
{
    /// <summary>
    /// Gets the specifier strings for all available devices.
    /// </summary>
    AllDevicesSpecifier = 0x1013,
}

#endregion
