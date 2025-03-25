using System;
using System.Runtime.InteropServices;

namespace OpenStack.Sfx.Al.Extensions.SOFT.DeviceClock;

public class DeviceClock : ALBase
{
    /// <summary>
    /// The name of this AL extension.
    /// </summary>
    public const string ExtensionName = "ALC_SOFT_device_clock";

    // We need to register the resolver for OpenAL before we can DllImport functions.
    static DeviceClock() => RegisterOpenALResolver();
    DeviceClock() { }

    /// <summary>
    /// Checks if this extension is present.
    /// </summary>
    /// <param name="device">The device to query.</param>
    /// <returns>Whether the extension was present or not.</returns>
    public static bool IsExtensionPresent(ALDevice device) => ALC.IsExtensionPresent(device, ExtensionName);

    public static unsafe void GetInteger(ALDevice device, GetInteger64 param, int size, long* values) => _GetIntegerPtr(device, param, size, values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetIntegerPtrDelegate(ALDevice device, GetInteger64 param, int size, long* values);
    static readonly GetIntegerPtrDelegate _GetIntegerPtr = LoadDelegate<GetIntegerPtrDelegate>("alcGetInteger64vSOFT");

    public static void GetInteger(ALDevice device, GetInteger64 param, int size, ref long values) => _GetIntegerRef(device, param, size, ref values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetIntegerRefDelegate(ALDevice device, GetInteger64 param, int size, ref long values);
    static readonly GetIntegerRefDelegate _GetIntegerRef = LoadDelegate<GetIntegerRefDelegate>("alcGetInteger64vSOFT");

    public static void GetInteger(ALDevice device, GetInteger64 param, int size, long[] values) => _GetIntegerArray(device, param, size, values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetIntegerArrayDelegate(ALDevice device, GetInteger64 param, int size, long[] values);
    static readonly GetIntegerArrayDelegate _GetIntegerArray = LoadDelegate<GetIntegerArrayDelegate>("alcGetInteger64vSOFT");

    public static unsafe void GetSource(int source, SourceInteger64 param, long* values) => _GetSourcei64vPtr(source, param, values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetSourcei64vPtrDelegate(int source, SourceInteger64 param, long* values);
    static readonly GetSourcei64vPtrDelegate _GetSourcei64vPtr = LoadDelegate<GetSourcei64vPtrDelegate>("alGetSourcei64vSOFT");

    public static void GetSource(int source, SourceInteger64 param, ref long values) => _GetSourcei64vRef(source, param, ref values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourcei64vRefDelegate(int source, SourceInteger64 param, ref long values);
    static readonly GetSourcei64vRefDelegate _GetSourcei64vRef = LoadDelegate<GetSourcei64vRefDelegate>("alGetSourcei64vSOFT");

    public static void GetSource(int source, SourceInteger64 param, long[] values) => _GetSourcei64vArray(source, param, values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourcei64vArrayDelegate(int source, SourceInteger64 param, long[] values);
    static readonly GetSourcei64vArrayDelegate _GetSourcei64vArray = LoadDelegate<GetSourcei64vArrayDelegate>("alGetSourcei64vSOFT");

    public static unsafe void GetSource(int source, SourceDouble param, double* values) => _GetSourcedvPtr(source, param, values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetSourcedvPtrDelegate(int source, SourceDouble param, double* values);
    static readonly GetSourcedvPtrDelegate _GetSourcedvPtr = LoadDelegate<GetSourcedvPtrDelegate>("alGetSourcedvSOFT");

    public static void GetSource(int source, SourceDouble param, ref double values) => _GetSourcedvRef(source, param, ref values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourcedvRefDelegate(int source, SourceDouble param, ref double values);
    static readonly GetSourcedvRefDelegate _GetSourcedvRef = LoadDelegate<GetSourcedvRefDelegate>("alGetSourcedvSOFT");

    public static void GetSource(int source, SourceDouble param, double[] values) => _GetSourcedvArray(source, param, values);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourcedvArrayDelegate(int source, SourceDouble param, double[] values);
    static readonly GetSourcedvArrayDelegate _GetSourcedvArray = LoadDelegate<GetSourcedvArrayDelegate>("alGetSourcedvSOFT");

    public static void GetInteger(ALDevice device, GetInteger64 param, long[] values) => GetInteger(device, param, values.Length, values);

    public static unsafe void GetInteger(ALDevice device, GetInteger64 param, Span<long> values) => GetInteger(device, param, values.Length, ref values[0]);

    public static unsafe void GetSource(int source, SourceInteger64 param, Span<long> values) => GetSource(source, param, ref values[0]);

    public static unsafe void GetSource(int source, SourceInteger64 param, out int value1, out int value2, out long value3) { var values = stackalloc int[4]; GetSource(source, param, (long*)values); value1 = values[0]; value2 = values[1]; value3 = ((long*)values)[2]; }

    public static unsafe void GetSource(int source, SourceDouble param, Span<double> values) => GetSource(source, param, ref values[0]);

    public static unsafe void GetSource(int source, SourceDouble param, out double value1, out double value2) { Span<double> values = stackalloc double[2]; GetSource(source, param, values); value1 = values[0]; value2 = values[1]; }
}

#region Enums

public enum GetInteger64
{
    /// <summary>
    /// The audio device clock time, expressed
    /// in nanoseconds.
    /// NULL is an invalid device.
    /// ALC_DEVICE_CLOCK_SOFT
    /// </summary>
    DeviceClock = 0x1600,
    /// <summary>
    /// The current audio device latency, in nanoseconds.
    /// This is effectively the delay for the samples rendered at the the device's current clock time from reaching the physical output.
    /// NULL is an invalid device.
    /// ALC_DEVICE_LATENCY_SOFT
    /// </summary>
    DeviceLatency = 0x1601,
    /// <summary>
    /// Expects a destination size of 2, and provides both the audio device clock time and latency, both in nanoseconds.
    /// The two values are measured atomically with respect to one another (i.e. the latency value was measured at the same time the device clock value was retrieved).
    /// NULL is an invalid device.
    /// ALC_DEVICE_CLOCK_LATENCY_SOFT
    /// </summary>
    DeviceClockLatency = 0x1602,
}

public enum SourceDouble
{
    /// <summary>
    /// AL_SEC_OFFSET_LATENCY_SOFT
    /// <br/>
    /// The playback position, along with the device clock, both expressed in seconds.
    /// This attribute is read-only.
    /// <br/>
    /// The first value in the returned vector is the offset in seconds.
    /// The value is similar to that returned by <see cref="ALSourcef.SecOffset"/>, just with more precision.
    /// <br/>
    /// The second value is the device clock, in seconds.
    /// This updates at the same rate as the offset, and both are measured atomically with respect to one another.
    /// Be aware that this value may be subtly different from the other device clock queries due to the variable precision of floating-point values.
    /// </summary>
    SecOffsetClock = 0x1203,
}

public enum SourceInteger64
{
    /// <summary>
    /// AL_SAMPLE_OFFSET_CLOCK_SOFT
    /// <br/>
    /// The playback position, expressed in fixed-point samples, along with the device clock, expressed in nanoseconds.
    /// This attribute is read-only.
    /// <br/>
    /// The first value in the returned vector is the sample offset, which is a 32.32 fixed-point value.
    /// The whole number is stored in the upper 32 bits and the fractional component is in the lower 32 bits.
    /// The value is similar to that returned by <see cref="ALSourcei.SampleOffset"/>, just with more precision.
    /// <br/>
    /// The second value is the device clock, in nanoseconds.
    /// This updates at the same rate as the offset, and both are measured atomically with respect to one another.
    /// </summary>
    SampleOffsetClock = 0x1202,
}

#endregion
