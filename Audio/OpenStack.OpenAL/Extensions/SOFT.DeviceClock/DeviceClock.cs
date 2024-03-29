﻿using System.Runtime.InteropServices;

namespace System.NumericsX.OpenAL.Extensions.SOFT.DeviceClock
{
    public class DeviceClock : ALBase
    {
        /// <summary>
        /// The name of this AL extension.
        /// </summary>
        public const string ExtensionName = "ALC_SOFT_device_clock";

        // We need to register the resolver for OpenAL before we can DllImport functions.
        static DeviceClock()
            => RegisterOpenALResolver();

        DeviceClock() { }

        /// <summary>
        /// Checks if this extension is present.
        /// </summary>
        /// <param name="device">The device to query.</param>
        /// <returns>Whether the extension was present or not.</returns>
        public static bool IsExtensionPresent(ALDevice device)
            => ALC.IsExtensionPresent(device, ExtensionName);

#pragma warning disable SA1516 // Elements should be separated by blank line
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
#pragma warning restore SA1516 // Elements should be separated by blank line

        public static void GetInteger(ALDevice device, GetInteger64 param, long[] values)
            => GetInteger(device, param, values.Length, values);

        public static unsafe void GetInteger(ALDevice device, GetInteger64 param, Span<long> values)
            => GetInteger(device, param, values.Length, ref values[0]);

        public static unsafe void GetSource(int source, SourceInteger64 param, Span<long> values)
            => GetSource(source, param, ref values[0]);

        public static unsafe void GetSource(int source, SourceInteger64 param, out int value1, out int value2, out long value3)
        {
            var values = stackalloc int[4];
            GetSource(source, param, (long*)values);
            value1 = values[0];
            value2 = values[1];
            value3 = ((long*)values)[2];
        }

        public static unsafe void GetSource(int source, SourceDouble param, Span<double> values)
            => GetSource(source, param, ref values[0]);

        public static unsafe void GetSource(int source, SourceDouble param, out double value1, out double value2)
        {
            Span<double> values = stackalloc double[2];
            GetSource(source, param, values);
            value1 = values[0];
            value2 = values[1];
        }
    }
}
