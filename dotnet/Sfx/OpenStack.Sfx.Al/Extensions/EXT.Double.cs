﻿using System;
using System.Runtime.InteropServices;

namespace OpenStack.Sfx.Al.Extensions.EXT.Double;

public class EXTDouble : ALBase {
    /// <summary>
    /// The name of this AL extension.
    /// </summary>
    public const string ExtensionName = "AL_EXT_double";

    // We need to register the resolver for OpenAL before we can DllImport functions.
    static EXTDouble() => RegisterOpenALResolver();
    EXTDouble() { }

    /// <summary>
    /// Checks if this extension is present.
    /// </summary>
    /// <returns>Whether the extension was present or not.</returns>
    public static bool IsExtensionPresent() => AL.IsExtensionPresent(ExtensionName);

    /// <summary>This function fills a buffer with audio buffer. All the pre-defined formats are PCM buffer, but this function may be used by extensions to load other buffer types as well.</summary>
    /// <param name="bid">buffer Handle/Name to be filled with buffer.</param>
    /// <param name="format">Format type from among the following: DoubleBufferFormat.Mono, DoubleBufferFormat.Stereo.</param>
    /// <param name="buffer">Pointer to a pinned audio buffer.</param>
    /// <param name="bytes">The size of the audio buffer in bytes.</param>
    /// <param name="freq">The frequency of the audio buffer.</param>
    [DllImport(AL.Lib, EntryPoint = "alBufferData", ExactSpelling = true, CallingConvention = AL.ALCallingConvention)] public static unsafe extern void BufferData(int bid, DoubleBufferFormat format, double* buffer, int bytes, int freq);
    // AL_API void AL_APIENTRY alBufferData( ALuint bid, ALenum format, const ALvoid* buffer, ALsizei size, ALsizei freq );

    /// <summary>This function fills a buffer with audio buffer. All the pre-defined formats are PCM buffer, but this function may be used by extensions to load other buffer types as well.</summary>
    /// <param name="bid">buffer Handle/Name to be filled with buffer.</param>
    /// <param name="format">Format type from among the following: DoubleBufferFormat.Mono, DoubleBufferFormat.Stereo.</param>
    /// <param name="buffer">Pointer to a pinned audio buffer.</param>
    /// <param name="bytes">The size of the audio buffer in bytes.</param>
    /// <param name="freq">The frequency of the audio buffer.</param>
    [DllImport(AL.Lib, EntryPoint = "alBufferData", ExactSpelling = true, CallingConvention = AL.ALCallingConvention)] public static extern void BufferData(int bid, DoubleBufferFormat format, ref double buffer, int bytes, int freq);
    // AL_API void AL_APIENTRY alBufferData( ALuint bid, ALenum format, const ALvoid* buffer, ALsizei size, ALsizei freq );

    /// <summary>This function fills a buffer with audio buffer. All the pre-defined formats are PCM buffer, but this function may be used by extensions to load other buffer types as well.</summary>
    /// <param name="bid">buffer Handle/Name to be filled with buffer.</param>
    /// <param name="format">Format type from among the following: DoubleBufferFormat.Mono, DoubleBufferFormat.Stereo.</param>
    /// <param name="buffer">The audio buffer.</param>
    /// <param name="freq">The frequency of the audio buffer.</param>
    /// FIXME: Should "size" be changed to "elements"?
    public static unsafe void BufferData(int bid, DoubleBufferFormat format, double[] buffer, int freq) => BufferData(bid, format, ref buffer[0], buffer.Length * sizeof(double), freq);

    /// <summary>This function fills a buffer with audio buffer. All the pre-defined formats are PCM buffer, but this function may be used by extensions to load other buffer types as well.</summary>
    /// <param name="bid">buffer Handle/Name to be filled with buffer.</param>
    /// <param name="format">Format type from among the following: DoubleBufferFormat.Mono, DoubleBufferFormat.Stereo.</param>
    /// <param name="buffer">Span representing the audio buffer.</param>
    /// <param name="freq">The frequency of the audio buffer.</param>
    public static unsafe void BufferData(int bid, DoubleBufferFormat format, Span<double> buffer, int freq) => BufferData(bid, format, ref buffer[0], buffer.Length * sizeof(double), freq);
}

#region Enums

/// <summary>
/// Defines valid format specifiers for sound samples. This covers the additions from the multi-channel buffers extension.
/// </summary>
public enum DoubleBufferFormat {
    /// <summary>
    /// 1 Channel, double-precision floating-point data.
    /// </summary>
    Mono = 0x10012,
    /// <summary>
    /// 2 Channels, double-precision floating-point data.
    /// </summary>
    Stereo = 0x10013,
}

#endregion
