using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenStack.Sfx.Al
{
    /// <summary>Alc = Audio Library Context.</summary>
    public class ALC : ALBase
    {
        internal const string Lib = AL.Lib;
        internal const CallingConvention AlcCallingConv = CallingConvention.Cdecl;

        // We need to register the resolver for OpenAL before we can DllImport functions.
        static ALC() => RegisterOpenALResolver();
        ALC() { }

        /// <summary>This function creates a context using a specified device.</summary>
        /// <param name="device">A pointer to a device.</param>
        /// <param name="attributeList">A zero terminated array of a set of attributes: ALC_FREQUENCY, ALC_MONO_SOURCES, ALC_REFRESH, ALC_STEREO_SOURCES, ALC_SYNC.</param>
        /// <returns>Returns a pointer to the new context (NULL on failure).</returns>
        /// <remarks>The attribute list can be NULL, or a zero terminated list of integer pairs composed of valid ALC attribute tokens and requested values.</remarks>
        [DllImport(Lib, EntryPoint = "alcCreateContext", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static unsafe extern ALContext CreateContext([In] ALDevice device, [In] int* attributeList);
        // ALC_API ALCcontext * ALC_APIENTRY alcCreateContext( ALCdevice *device, const ALCint* attrlist );

        /// <summary>This function creates a context using a specified device.</summary>
        /// <param name="device">A pointer to a device.</param>
        /// <param name="attributeList">A zero terminated array of a set of attributes: ALC_FREQUENCY, ALC_MONO_SOURCES, ALC_REFRESH, ALC_STEREO_SOURCES, ALC_SYNC.</param>
        /// <returns>Returns a pointer to the new context (NULL on failure).</returns>
        /// <remarks>The attribute list can be NULL, or a zero terminated list of integer pairs composed of valid ALC attribute tokens and requested values.</remarks>
        [DllImport(Lib, EntryPoint = "alcCreateContext", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern ALContext CreateContext([In] ALDevice device, [In] ref int attributeList);
        // ALC_API ALCcontext * ALC_APIENTRY alcCreateContext( ALCdevice *device, const ALCint* attrlist );

        /// <summary>This function creates a context using a specified device.</summary>
        /// <param name="device">A pointer to a device.</param>
        /// <param name="attributeList">A zero terminated array of a set of attributes: ALC_FREQUENCY, ALC_MONO_SOURCES, ALC_REFRESH, ALC_STEREO_SOURCES, ALC_SYNC.</param>
        /// <returns>Returns a pointer to the new context (NULL on failure).</returns>
        /// <remarks>The attribute list can be NULL, or a zero terminated list of integer pairs composed of valid ALC attribute tokens and requested values.</remarks>
        [DllImport(Lib, EntryPoint = "alcCreateContext", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern ALContext CreateContext([In] ALDevice device, [In] int[] attributeList);
        // ALC_API ALCcontext * ALC_APIENTRY alcCreateContext( ALCdevice *device, const ALCint* attrlist );

        /// <summary>This function creates a context using a specified device.</summary>
        /// <param name="device">A pointer to a device.</param>
        /// <param name="attributeList">A zero terminated span of a set of attributes: ALC_FREQUENCY, ALC_MONO_SOURCES, ALC_REFRESH, ALC_STEREO_SOURCES, ALC_SYNC.</param>
        /// <returns>Returns a pointer to the new context (NULL on failure).</returns>
        /// <remarks>The attribute list can be NULL, or a zero terminated list of integer pairs composed of valid ALC attribute tokens and requested values.</remarks>
        public static ALContext CreateContext(ALDevice device, Span<int> attributeList) => CreateContext(device, ref attributeList[0]);

        /// <summary>This function creates a context using a specified device.</summary>
        /// <param name="device">A pointer to a device.</param>
        /// <param name="attributes">The ALContext attributes to request.</param>
        /// <returns>Returns a pointer to the new context (NULL on failure).</returns>
        /// <remarks>The attribute list can be NULL, or a zero terminated list of integer pairs composed of valid ALC attribute tokens and requested values.</remarks>
        public static ALContext CreateContext(ALDevice device, ALContextAttributes attributes) => CreateContext(device, attributes.CreateAttributeArray());

        /// <summary>This function makes a specified context the current context.</summary>
        /// <param name="context">A pointer to the new context.</param>
        /// <returns>Returns True on success, or False on failure.</returns>
        [DllImport(Lib, EntryPoint = "alcMakeContextCurrent", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern bool MakeContextCurrent(ALContext context);
        // ALC_API ALCboolean ALC_APIENTRY alcMakeContextCurrent( ALCcontext *context );

        /// <summary>This function tells a context to begin processing. When a context is suspended, changes in OpenAL state will be accepted but will not be processed. alcSuspendContext can be used to suspend a context, and then all the OpenAL state changes can be applied at once, followed by a call to alcProcessContext to apply all the state changes immediately. In some cases, this procedure may be more efficient than application of properties in a non-suspended state. In some implementations, process and suspend calls are each a NOP.</summary>
        /// <param name="context">A pointer to the new context.</param>
        [DllImport(Lib, EntryPoint = "alcProcessContext", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern void ProcessContext(ALContext context);
        // ALC_API void ALC_APIENTRY alcProcessContext( ALCcontext *context );

        /// <summary>This function suspends processing on a specified context. When a context is suspended, changes in OpenAL state will be accepted but will not be processed. A typical use of alcSuspendContext would be to suspend a context, apply all the OpenAL state changes at once, and then call alcProcessContext to apply all the state changes at once. In some cases, this procedure may be more efficient than application of properties in a non-suspended state. In some implementations, process and suspend calls are each a NOP.</summary>
        /// <param name="context">A pointer to the context to be suspended.</param>
        [DllImport(Lib, EntryPoint = "alcSuspendContext", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern void SuspendContext(ALContext context);
        // ALC_API void ALC_APIENTRY alcSuspendContext( ALCcontext *context );

        /// <summary>This function destroys a context.</summary>
        /// <param name="context">A pointer to the new context.</param>
        [DllImport(Lib, EntryPoint = "alcDestroyContext", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern void DestroyContext(ALContext context);
        // ALC_API void ALC_APIENTRY alcDestroyContext( ALCcontext *context );

        /// <summary>This function retrieves the current context.</summary>
        /// <returns>Returns a pointer to the current context.</returns>
        [DllImport(Lib, EntryPoint = "alcGetCurrentContext", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern ALContext GetCurrentContext();
        // ALC_API ALCcontext * ALC_APIENTRY alcGetCurrentContext( void );

        /// <summary>This function retrieves a context's device pointer.</summary>
        /// <param name="context">A pointer to a context.</param>
        /// <returns>Returns a pointer to the specified context's device.</returns>
        [DllImport(Lib, EntryPoint = "alcGetContextsDevice", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern ALDevice GetContextsDevice(ALContext context);
        // ALC_API ALCdevice * ALC_APIENTRY alcGetContextsDevice( ALCcontext *context );

        /// <summary>This function opens a device by name.</summary>
        /// <param name="devicename">A null-terminated string describing a device.</param>
        /// <returns>Returns a pointer to the opened device. The return value will be NULL if there is an error.</returns>
        [DllImport(Lib, EntryPoint = "alcOpenDevice", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern ALDevice OpenDevice([In] string devicename);
        // ALC_API ALCdevice * ALC_APIENTRY alcOpenDevice( const ALCchar *devicename );

        /// <summary>This function closes a device by name.</summary>
        /// <param name="device">A pointer to an opened device.</param>
        /// <returns>True will be returned on success or False on failure. Closing a device will fail if the device contains any contexts or buffers.</returns>
        [DllImport(Lib, EntryPoint = "alcCloseDevice", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern bool CloseDevice([In] ALDevice device);
        // ALC_API ALCboolean ALC_APIENTRY alcCloseDevice( ALCdevice *device );

        /// <summary>This function retrieves the current context error state.</summary>
        /// <param name="device">A pointer to the device to retrieve the error state from.</param>
        /// <returns>Errorcode Int32.</returns>
        [DllImport(Lib, EntryPoint = "alcGetError", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern AlcError GetError([In] ALDevice device);
        // ALC_API ALCenum ALC_APIENTRY alcGetError( ALCdevice *device );

        /// <summary>This function queries if a specified context extension is available.</summary>
        /// <param name="device">A pointer to the device to be queried for an extension.</param>
        /// <param name="extname">A null-terminated string describing the extension.</param>
        /// <returns>Returns True if the extension is available, False if the extension is not available.</returns>
        [DllImport(Lib, EntryPoint = "alcIsExtensionPresent", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern bool IsExtensionPresent([In] ALDevice device, [In] string extname);
        // ALC_API ALCboolean ALC_APIENTRY alcIsExtensionPresent( ALCdevice *device, const ALCchar *extname );

        /// <summary>This function queries if a specified context extension is available.</summary>
        /// <param name="device">A pointer to the device to be queried for an extension.</param>
        /// <param name="extname">A null-terminated string describing the extension.</param>
        /// <returns>Returns True if the extension is available, False if the extension is not available.</returns>
        [DllImport(Lib, EntryPoint = "alcIsExtensionPresent", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern bool IsExtensionPresent([In] ALCaptureDevice device, [In] string extname);
        // ALC_API ALCboolean ALC_APIENTRY alcIsExtensionPresent( ALCdevice *device, const ALCchar *extname );

        /// <summary>This function retrieves the address of a specified context extension function.</summary>
        /// <param name="device">a pointer to the device to be queried for the function.</param>
        /// <param name="funcname">a null-terminated string describing the function.</param>
        /// <returns>Returns the address of the function, or NULL if it is not found.</returns>
        [DllImport(Lib, EntryPoint = "alcGetProcAddress", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern IntPtr GetProcAddress([In] ALDevice device, [In] string funcname);
        // ALC_API void * ALC_APIENTRY alcGetProcAddress( ALCdevice *device, const ALCchar *funcname );

        /// <summary>This function retrieves the enum value for a specified enumeration name.</summary>
        /// <param name="device">a pointer to the device to be queried.</param>
        /// <param name="enumname">a null terminated string describing the enum value.</param>
        /// <returns>Returns the enum value described by the enumName string. This is most often used for querying an enum value for an ALC extension.</returns>
        [DllImport(Lib, EntryPoint = "alcGetEnumValue", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern int GetEnumValue([In] ALDevice device, [In] string enumname);
        // ALC_API ALCenum ALC_APIENTRY alcGetEnumValue( ALCdevice *device, const ALCchar *enumname );

        /// <summary>This strings related to the context.</summary>
        /// <remarks>
        /// ALC_DEFAULT_DEVICE_SPECIFIER will return the name of the default output device.
        /// ALC_CAPTURE_DEFAULT_DEVICE_SPECIFIER will return the name of the default capture device.
        /// ALC_DEVICE_SPECIFIER will return the name of the specified output device if a pointer is supplied, or will return a list of all available devices if a NULL device pointer is supplied. A list is a pointer to a series of strings separated by NULL characters, with the list terminated by two NULL characters. See Enumeration Extension for more details.
        /// ALC_CAPTURE_DEVICE_SPECIFIER will return the name of the specified capture device if a pointer is supplied, or will return a list of all available devices if a NULL device pointer is supplied.
        /// ALC_EXTENSIONS returns a list of available context extensions, with each extension separated by a space and the list terminated by a NULL character.
        /// </remarks>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_DEFAULT_DEVICE_SPECIFIER, ALC_CAPTURE_DEFAULT_DEVICE_SPECIFIER, ALC_DEVICE_SPECIFIER, ALC_CAPTURE_DEVICE_SPECIFIER, ALC_EXTENSIONS.</param>
        /// <returns>A string containing the name of the Device.</returns>
        [DllImport(Lib, EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static unsafe extern byte* GetStringPtr([In] ALDevice device, AlcGetString param);
        // ALC_API const ALCchar * ALC_APIENTRY alcGetString( ALCdevice *device, ALCenum param );

        /// <summary>This strings related to the context.</summary>
        /// <remarks>
        /// ALC_DEFAULT_DEVICE_SPECIFIER will return the name of the default output device.
        /// ALC_CAPTURE_DEFAULT_DEVICE_SPECIFIER will return the name of the default capture device.
        /// ALC_DEVICE_SPECIFIER will return the name of the specified output device if a pointer is supplied, or will return a list of all available devices if a NULL device pointer is supplied. A list is a pointer to a series of strings separated by NULL characters, with the list terminated by two NULL characters. See Enumeration Extension for more details.
        /// ALC_CAPTURE_DEVICE_SPECIFIER will return the name of the specified capture device if a pointer is supplied, or will return a list of all available devices if a NULL device pointer is supplied.
        /// ALC_EXTENSIONS returns a list of available context extensions, with each extension separated by a space and the list terminated by a NULL character.
        /// </remarks>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_DEFAULT_DEVICE_SPECIFIER, ALC_CAPTURE_DEFAULT_DEVICE_SPECIFIER, ALC_DEVICE_SPECIFIER, ALC_CAPTURE_DEVICE_SPECIFIER, ALC_EXTENSIONS.</param>
        /// <returns>A string containing the name of the Device.</returns>
        [DllImport(Lib, EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)][return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstCharPtrMarshaler))] public static extern string GetString([In] ALDevice device, AlcGetString param);
        // ALC_API const ALCchar * ALC_APIENTRY alcGetString( ALCdevice *device, ALCenum param );

        /// <summary>This function returns a List of strings related to the context.</summary>
        /// <remarks>
        /// ALC_DEVICE_SPECIFIER will return the name of the specified output device if a pointer is supplied, or will return a list of all available devices if a NULL device pointer is supplied. A list is a pointer to a series of strings separated by NULL characters, with the list terminated by two NULL characters. See Enumeration Extension for more details.
        /// ALC_CAPTURE_DEVICE_SPECIFIER will return the name of the specified capture device if a pointer is supplied, or will return a list of all available devices if a NULL device pointer is supplied.
        /// ALC_EXTENSIONS returns a list of available context extensions, with each extension separated by a space and the list terminated by a NULL character.
        /// </remarks>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_DEVICE_SPECIFIER, ALC_CAPTURE_DEVICE_SPECIFIER, ALC_ALL_DEVICES_SPECIFIER.</param>
        /// <returns>A List of strings containing the names of the Devices.</returns>
        public static unsafe List<string> GetString(ALDevice device, AlcGetStringList param) { var result = GetStringPtr(device, (AlcGetString)param); return ALStringListToList(result); }

        /// <summary>This function returns a List of strings related to the context.</summary>
        /// <remarks>
        /// ALC_DEVICE_SPECIFIER will return the name of the specified output device if a pointer is supplied, or will return a list of all available devices if a NULL device pointer is supplied. A list is a pointer to a series of strings separated by NULL characters, with the list terminated by two NULL characters. See Enumeration Extension for more details.
        /// ALC_CAPTURE_DEVICE_SPECIFIER will return the name of the specified capture device if a pointer is supplied, or will return a list of all available devices if a NULL device pointer is supplied.
        /// ALC_EXTENSIONS returns a list of available context extensions, with each extension separated by a space and the list terminated by a NULL character.
        /// </remarks>
        /// <param name="param">An attribute to be retrieved: ALC_DEVICE_SPECIFIER, ALC_CAPTURE_DEVICE_SPECIFIER, ALC_ALL_DEVICES_SPECIFIER.</param>
        /// <returns>A List of strings containing the names of the Devices.</returns>
        public static List<string> GetString(AlcGetStringList param) => GetString(ALDevice.Null, param);

        /// <summary>This function returns integers related to the context.</summary>
        /// <param name="device">a pointer to the device to be queried.</param>
        /// <param name="param">an attribute to be retrieved: ALC_MAJOR_VERSION, ALC_MINOR_VERSION, ALC_ATTRIBUTES_SIZE, ALC_ALL_ATTRIBUTES.</param>
        /// <param name="size">the size of the destination buffer provided, in number of integers.</param>
        /// <param name="data">a pointer to the buffer to be returned.</param>
        [DllImport(Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static unsafe extern void GetInteger(ALDevice device, AlcGetInteger param, int size, int* data);
        // ALC_API void ALC_APIENTRY alcGetIntegerv( ALCdevice *device, ALCenum param, ALCsizei size, ALCint *buffer );

        /// <summary>This function returns integers related to the context.</summary>
        /// <param name="device">a pointer to the device to be queried.</param>
        /// <param name="param">an attribute to be retrieved: ALC_MAJOR_VERSION, ALC_MINOR_VERSION, ALC_ATTRIBUTES_SIZE, ALC_ALL_ATTRIBUTES.</param>
        /// <param name="size">the size of the destination buffer provided, in number of integers.</param>
        /// <param name="data">a pointer to the buffer to be returned.</param>
        [DllImport(Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern void GetInteger(ALDevice device, AlcGetInteger param, int size, int[] data);
        // ALC_API void ALC_APIENTRY alcGetIntegerv( ALCdevice *device, ALCenum param, ALCsizei size, ALCint *buffer );

        /// <summary>This function returns integers related to the context.</summary>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_MAJOR_VERSION, ALC_MINOR_VERSION, ALC_ATTRIBUTES_SIZE, ALC_ALL_ATTRIBUTES.</param>
        /// <param name="size">The size of the destination buffer provided, in number of integers.</param>
        /// <param name="data">A pointer to the buffer to be returned.</param>
        [DllImport(Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern void GetInteger(ALDevice device, AlcGetInteger param, int size, out int data);
        // ALC_API void ALC_APIENTRY alcGetIntegerv( ALCdevice *device, ALCenum param, ALCsizei size, ALCint *buffer );

        /// <summary>This function returns integers related to the context.</summary>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_MAJOR_VERSION, ALC_MINOR_VERSION, ALC_ATTRIBUTES_SIZE, ALC_ALL_ATTRIBUTES.</param>
        /// <param name="data">A pointer to the buffer to be returned.</param>
        public static void GetInteger(ALDevice device, AlcGetInteger param, out int data) => GetInteger(device, param, 1, out data);

        /// <summary>This function returns integers related to the context.</summary>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_MAJOR_VERSION, ALC_MINOR_VERSION, ALC_ATTRIBUTES_SIZE, ALC_ALL_ATTRIBUTES.</param>
        /// <returns>The value returned.</returns>
        public static int GetInteger(ALDevice device, AlcGetInteger param) { GetInteger(device, param, 1, out int data); return data; }

        /// <summary>
        /// Returns a list of attributes for the current context of the specified device.
        /// </summary>
        /// <param name="device">The device to get attributes from.</param>
        /// <returns>A list of attributes for the device.</returns>
        public static int[] GetAttributeArray(ALDevice device) { GetInteger(device, AlcGetInteger.AttributesSize, 1, out int size); var attributes = new int[size]; GetInteger(device, AlcGetInteger.AllAttributes, size, attributes); return attributes; }

        /// <summary>
        /// Returns a list of attributes for the current context of the specified device.
        /// </summary>
        /// <param name="device">The device to get attributes from.</param>
        /// <returns>A list of attributes for the device.</returns>
        public static ALContextAttributes GetContextAttributes(ALDevice device) { GetInteger(device, AlcGetInteger.AttributesSize, 1, out int size); var attributes = new int[size]; GetInteger(device, AlcGetInteger.AllAttributes, size, attributes); return ALContextAttributes.FromArray(attributes); }

        // -------- ALC_EXT_CAPTURE --------

        /// <summary>
        /// Checks to see that the ALC_EXT_CAPTURE extension is present. This will always be available in 1.1 devices or later.
        /// </summary>
        /// <param name="device">The device to check the extension is present for.</param>
        /// <returns>If the ALC_EXT_CAPTURE extension was present.</returns>
        public static bool IsCaptureExtensionPresent(ALDevice device) => IsExtensionPresent(device, "ALC_EXT_CAPTURE");

        /// <summary>
        /// Checks to see that the ALC_EXT_CAPTURE extension is present. This will always be available in 1.1 devices or later.
        /// </summary>
        /// <param name="device">The device to check the extension is present for.</param>
        /// <returns>If the ALC_EXT_CAPTURE extension was present.</returns>
        public static bool IsCaptureExtensionPresent(ALCaptureDevice device) => IsExtensionPresent(device, "ALC_EXT_CAPTURE");

        /// <summary>This function opens a capture device by name. </summary>
        /// <param name="devicename">A pointer to a device name string.</param>
        /// <param name="frequency">The frequency that the buffer should be captured at.</param>
        /// <param name="format">The requested capture buffer format.</param>
        /// <param name="buffersize">The size of the capture buffer in samples, not bytes.</param>
        /// <returns>Returns the capture device pointer, or <see cref="ALCaptureDevice.Null"/> on failure.</returns>
        [DllImport(Lib, EntryPoint = "alcCaptureOpenDevice", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern ALCaptureDevice CaptureOpenDevice(string devicename, uint frequency, ALFormat format, int buffersize);
        // ALC_API ALCdevice* ALC_APIENTRY alcCaptureOpenDevice( const ALCchar *devicename, ALCuint frequency, ALCenum format, ALCsizei buffersize );

        /// <summary>This function opens a capture device by name. </summary>
        /// <param name="devicename">A pointer to a device name string.</param>
        /// <param name="frequency">The frequency that the buffer should be captured at.</param>
        /// <param name="format">The requested capture buffer format.</param>
        /// <param name="buffersize">The size of the capture buffer in samples, not bytes.</param>
        /// <returns>Returns the capture device pointer, or <see cref="ALCaptureDevice.Null"/> on failure.</returns>
        [DllImport(Lib, EntryPoint = "alcCaptureOpenDevice", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern ALCaptureDevice CaptureOpenDevice(string devicename, int frequency, ALFormat format, int buffersize);
        // ALC_API ALCdevice* ALC_APIENTRY alcCaptureOpenDevice( const ALCchar *devicename, ALCuint frequency, ALCenum format, ALCsizei buffersize );

        /// <summary>This function closes the specified capture device.</summary>
        /// <param name="device">A pointer to a capture device.</param>
        /// <returns>Returns True if the close operation was successful, False on failure.</returns>
        [DllImport(Lib, EntryPoint = "alcCaptureCloseDevice", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern bool CaptureCloseDevice([In] ALCaptureDevice device);
        // ALC_API ALCboolean ALC_APIENTRY alcCaptureCloseDevice( ALCdevice *device );

        /// <summary>This function begins a capture operation.</summary>
        /// <remarks>alcCaptureStart will begin recording to an internal ring buffer of the size specified when opening the capture device. The application can then retrieve the number of samples currently available using the ALC_CAPTURE_SAPMPLES token with alcGetIntegerv. When the application determines that enough samples are available for processing, then it can obtain them with a call to alcCaptureSamples.</remarks>
        /// <param name="device">A pointer to a capture device.</param>
        [DllImport(Lib, EntryPoint = "alcCaptureStart", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern void CaptureStart([In] ALCaptureDevice device);
        // ALC_API void ALC_APIENTRY alcCaptureStart( ALCdevice *device );

        /// <summary>This function stops a capture operation.</summary>
        /// <param name="device">A pointer to a capture device.</param>
        [DllImport(Lib, EntryPoint = "alcCaptureStop", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern void CaptureStop([In] ALCaptureDevice device);
        // ALC_API void ALC_APIENTRY alcCaptureStop( ALCdevice *device );

        /// <summary>This function completes a capture operation, and does not block.</summary>
        /// <param name="device">A pointer to a capture device.</param>
        /// <param name="buffer">A pointer to a buffer, which must be large enough to accommodate the number of samples.</param>
        /// <param name="samples">The number of samples to be retrieved.</param>
        [DllImport(Lib, EntryPoint = "alcCaptureSamples", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern void CaptureSamples(ALCaptureDevice device, IntPtr buffer, int samples);
        // ALC_API void ALC_APIENTRY alcCaptureSamples( ALCdevice *device, ALCvoid *buffer, ALCsizei samples );

        /// <summary>This function completes a capture operation, and does not block.</summary>
        /// <param name="device">A pointer to a capture device.</param>
        /// <param name="buffer">A pointer to a buffer, which must be large enough to accommodate the number of samples.</param>
        /// <param name="samples">The number of samples to be retrieved.</param>
        [DllImport(Lib, EntryPoint = "alcCaptureSamples", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static unsafe extern void CaptureSamples(ALCaptureDevice device, void* buffer, int samples);
        // ALC_API void ALC_APIENTRY alcCaptureSamples( ALCdevice *device, ALCvoid *buffer, ALCsizei samples );

        /// <summary>This function completes a capture operation, and does not block.</summary>
        /// <param name="device">A pointer to a capture device.</param>
        /// <param name="buffer">A pointer to a buffer, which must be large enough to accommodate the number of samples.</param>
        /// <param name="samples">The number of samples to be retrieved.</param>
        [DllImport(Lib, EntryPoint = "alcCaptureSamples", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern void CaptureSamples(ALCaptureDevice device, ref byte buffer, int samples);
        // ALC_API void ALC_APIENTRY alcCaptureSamples( ALCdevice *device, ALCvoid *buffer, ALCsizei samples );

        /// <summary>This function completes a capture operation, and does not block.</summary>
        /// <param name="device">A pointer to a capture device.</param>
        /// <param name="buffer">A pointer to a buffer, which must be large enough to accommodate the number of samples.</param>
        /// <param name="samples">The number of samples to be retrieved.</param>
        [DllImport(Lib, EntryPoint = "alcCaptureSamples", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static extern void CaptureSamples(ALCaptureDevice device, ref short buffer, int samples);
        // ALC_API void ALC_APIENTRY alcCaptureSamples( ALCdevice *device, ALCvoid *buffer, ALCsizei samples );

        /// <summary>This function completes a capture operation, and does not block.</summary>
        /// <typeparam name="T">The buffer datatype.</typeparam>
        /// <param name="device">A pointer to a capture device.</param>
        /// <param name="buffer">A reference to a buffer, which must be large enough to accommodate the number of samples.</param>
        /// <param name="samples">The number of samples to be retrieved.</param>
        public static unsafe void CaptureSamples<T>(ALCaptureDevice device, ref T buffer, int samples) where T : unmanaged { fixed (T* ptr = &buffer) CaptureSamples(device, ptr, samples); }

        /// <summary>This function completes a capture operation, and does not block.</summary>
        /// <typeparam name="T">The buffer datatype.</typeparam>
        /// <param name="device">A pointer to a capture device.</param>
        /// <param name="buffer">A buffer, which must be large enough to accommodate the number of samples.</param>
        /// <param name="samples">The number of samples to be retrieved.</param>
        public static void CaptureSamples<T>(ALCaptureDevice device, T[] buffer, int samples) where T : unmanaged => CaptureSamples(device, ref buffer[0], samples);

        /// <summary>This function returns integers related to the context.</summary>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_MAJOR_VERSION, ALC_MINOR_VERSION, ALC_ATTRIBUTES_SIZE, ALC_ALL_ATTRIBUTES.</param>
        /// <param name="size">The size of the destination buffer provided, in number of integers.</param>
        /// <param name="data">A pointer to the buffer to be returned.</param>
        [DllImport(Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static unsafe extern void GetInteger(ALCaptureDevice device, AlcGetInteger param, int size, int* data);
        // ALC_API void ALC_APIENTRY alcGetIntegerv( ALCdevice *device, ALCenum param, ALCsizei size, ALCint *buffer );

        /// <summary>This function returns integers related to the context.</summary>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_MAJOR_VERSION, ALC_MINOR_VERSION, ALC_ATTRIBUTES_SIZE, ALC_ALL_ATTRIBUTES.</param>
        /// <param name="size">The size of the destination buffer provided, in number of integers.</param>
        /// <param name="data">A pointer to the buffer to be returned.</param>
        [DllImport(Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern void GetInteger(ALCaptureDevice device, AlcGetInteger param, int size, int[] data);
        // ALC_API void ALC_APIENTRY alcGetIntegerv( ALCdevice *device, ALCenum param, ALCsizei size, ALCint *buffer );

        /// <summary>This function returns integers related to the context.</summary>
        /// <param name="device">A pointer to the device to be queried.</param>
        /// <param name="param">An attribute to be retrieved: ALC_MAJOR_VERSION, ALC_MINOR_VERSION, ALC_ATTRIBUTES_SIZE, ALC_ALL_ATTRIBUTES.</param>
        /// <param name="size">The size of the destination buffer provided, in number of integers.</param>
        /// <param name="data">A pointer to the buffer to be returned.</param>
        [DllImport(Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)] public static extern void GetInteger(ALCaptureDevice device, AlcGetInteger param, int size, out int data);
        // ALC_API void ALC_APIENTRY alcGetIntegerv( ALCdevice *device, ALCenum param, ALCsizei size, ALCint *buffer );

        /// <summary>
        /// Gets the current number of available capture samples.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns>The number of capture samples available.</returns>
        public static int GetAvailableSamples(ALCaptureDevice device) { GetInteger(device, AlcGetInteger.CaptureSamples, 1, out int result); return result; }

        // -------- ALC_ENUMERATION_EXT --------

        /// <summary>
        /// Checks to see that the ALC_ENUMERATION_EXT extension is present. This will always be available in 1.1 devices or later.
        /// </summary>
        /// <param name="device">The device to check the extension is present for.</param>
        /// <returns>If the ALC_ENUMERATION_EXT extension was present.</returns>
        public static bool IsEnumerationExtensionPresent(ALDevice device) => IsExtensionPresent(device, "ALC_ENUMERATION_EXT");

        /// <summary>
        /// Checks to see that the ALC_ENUMERATION_EXT extension is present. This will always be available in 1.1 devices or later.
        /// </summary>
        /// <param name="device">The device to check the extension is present for.</param>
        /// <returns>If the ALC_ENUMERATION_EXT extension was present.</returns>
        public static bool IsEnumerationExtensionPresent(ALCaptureDevice device) => IsExtensionPresent(device, "ALC_ENUMERATION_EXT");

        /// <summary>
        /// Gets a named property on the context.
        /// </summary>
        /// <param name="device">The device for the context.</param>
        /// <param name="param">The named property.</param>
        /// <returns>The value.</returns>
        [DllImport(Lib, EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = AlcCallingConv)][return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstCharPtrMarshaler))] public static extern string GetString(ALDevice device, GetEnumerationString param);

        /// <summary>
        /// Gets a named property on the context.
        /// </summary>
        /// <param name="device">The device for the context.</param>
        /// <param name="param">The named property.</param>
        /// <returns>The value.</returns>
        [DllImport(Lib, EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = AlcCallingConv)] public static unsafe extern byte* GetStringListPtr(ALDevice device, GetEnumerationStringList param);

        /// <inheritdoc cref="GetString(ALDevice, GetEnumerationString)"/>
        public static unsafe IEnumerable<string> GetStringList(GetEnumerationStringList param) { var result = GetStringListPtr(ALDevice.Null, param); return ALStringListToList(result); }

        /// <summary>
        /// Used to convert a OpenAL string list to a C# List.
        /// </summary>
        /// <param name="alList">A pointer to the AL list. Usually returned from GetStringList like AL functions.</param>
        /// <returns>The string list.</returns>
        internal static unsafe List<string> ALStringListToList(byte* alList)
        {
            if (alList == (byte*)0) return new List<string>();
            var b = new List<string>();
            var currentPos = alList;
            while (true)
            {
                var currentString = Marshal.PtrToStringAnsi(new IntPtr(currentPos));
                if (string.IsNullOrEmpty(currentString)) break;
                b.Add(currentString);
                currentPos += currentString.Length + 1;
            }
            return b;
        }
    }

    #region Enums

    /// <summary>
    /// Defines available context attributes.
    /// </summary>
    public enum AlcContextAttributes : int
    {
        /// <summary>Followed by System.Int32 Hz</summary>
        Frequency = 0x1007,
        /// <summary>Followed by System.Int32 Hz</summary>
        Refresh = 0x1008,
        /// <summary>Followed by AlBoolean.True, or AlBoolean.False</summary>
        Sync = 0x1009,
        /// <summary>Followed by System.Int32 Num of requested Mono (3D) Sources</summary>
        MonoSources = 0x1010,
        /// <summary>Followed by System.Int32 Num of requested Stereo Sources</summary>
        StereoSources = 0x1011,
        /// <summary>(EFX Extension) This Context property can be passed to OpenAL during Context creation (alcCreateContext) to request a maximum number of Auxiliary Sends desired on each Source. It is not guaranteed that the desired number of sends will be available, so an application should query this property after creating the context using alcGetIntergerv. Default: 2</summary>
        EfxMaxAuxiliarySends = 0x20003,
    }

    /// <summary>
    /// Defines OpenAL context errors.
    /// </summary>
    public enum AlcError : int
    {
        /// <summary>There is no current error.</summary>
        NoError = 0,
        /// <summary>No Device. The device handle or specifier names an inaccessible driver/server.</summary>
        InvalidDevice = 0xA001,
        /// <summary>Invalid context ID. The Context argument does not name a valid context.</summary>
        InvalidContext = 0xA002,
        /// <summary>Bad enum. A token used is not valid, or not applicable.</summary>
        InvalidEnum = 0xA003,
        /// <summary>Bad value. A value (e.g. Attribute) is not valid, or not applicable.</summary>
        InvalidValue = 0xA004,
        /// <summary>Out of memory. Unable to allocate memory.</summary>
        OutOfMemory = 0xA005,
    }

    /// <summary>
    /// Defines available parameters for <see cref="ALC.GetString(ALDevice, AlcGetString)"/>.
    /// </summary>
    public enum AlcGetString : int
    {
        /// <summary>The specifier string for the default device.</summary>
        DefaultDeviceSpecifier = 0x1004,
        /// <summary>A list of available context extensions separated by spaces.</summary>
        Extensions = 0x1006,
        /// <summary>The name of the default capture device</summary>
        CaptureDefaultDeviceSpecifier = 0x311, // ALC_EXT_CAPTURE extension.
        /// <summary>a list of the default devices.</summary>
        DefaultAllDevicesSpecifier = 0x1012,
        // duplicates from AlcGetStringList:
        /// <summary>Will only return the first Device, not a list. Use AlcGetStringList.CaptureDeviceSpecifier. ALC_EXT_CAPTURE_EXT </summary>
        CaptureDeviceSpecifier = 0x310,
        /// <summary>Will only return the first Device, not a list. Use AlcGetStringList.DeviceSpecifier</summary>
        DeviceSpecifier = 0x1005,
        /// <summary>Will only return the first Device, not a list. Use AlcGetStringList.AllDevicesSpecifier</summary>
        AllDevicesSpecifier = 0x1013,
    }

    /// <summary>
    /// Defines available parameters for <see cref="ALC.GetString(ALDevice, AlcGetStringList)"/>.
    /// </summary>
    public enum AlcGetStringList : int
    {
        /// <summary>The name of the specified capture device, or a list of all available capture devices if no capture device is specified. ALC_EXT_CAPTURE_EXT </summary>
        CaptureDeviceSpecifier = 0x310,
        /// <summary>The specifier strings for all available devices. ALC_ENUMERATION_EXT</summary>
        DeviceSpecifier = 0x1005,
        /// <summary>The specifier strings for all available devices. ALC_ENUMERATE_ALL_EXT</summary>
        AllDevicesSpecifier = 0x1013,
    }

    /// <summary>
    /// Defines available parameters for <see cref="ALC.GetInteger(ALDevice, AlcGetInteger, int, int[])"/>.
    /// </summary>
    public enum AlcGetInteger : int
    {
        /// <summary>The specification revision for this implementation (major version). NULL is an acceptable device.</summary>
        MajorVersion = 0x1000,
        /// <summary>The specification revision for this implementation (minor version). NULL is an acceptable device.</summary>
        MinorVersion = 0x1001,
        /// <summary>The size (number of ALCint values) required for a zero-terminated attributes list, for the current context. NULL is an invalid device.</summary>
        AttributesSize = 0x1002,
        /// <summary>Expects a destination of ALC_ATTRIBUTES_SIZE, and provides an attribute list for the current context of the specified device. NULL is an invalid device.</summary>
        AllAttributes = 0x1003,
        /// <summary>The number of capture samples available. NULL is an invalid device.</summary>
        CaptureSamples = 0x312,
        /// <summary>(EFX Extension) This property can be used by the application to retrieve the Major version number of the Effects Extension supported by this OpenAL implementation. As this is a Context property is should be retrieved using alcGetIntegerv.</summary>
        EfxMajorVersion = 0x20001,
        /// <summary>(EFX Extension) This property can be used by the application to retrieve the Minor version number of the Effects Extension supported by this OpenAL implementation. As this is a Context property is should be retrieved using alcGetIntegerv.</summary>
        EfxMinorVersion = 0x20002,
        /// <summary>(EFX Extension) This Context property can be passed to OpenAL during Context creation (alcCreateContext) to request a maximum number of Auxiliary Sends desired on each Source. It is not guaranteed that the desired number of sends will be available, so an application should query this property after creating the context using alcGetIntergerv. Default: 2</summary>
        EfxMaxAuxiliarySends = 0x20003,
    }

    /// <summary>
    /// Defines available parameters for <see cref="ALC.GetString(ALDevice, GetEnumerationString)" />.
    /// </summary>
    public enum GetEnumerationString
    {
        /// <summary>
        /// Gets the specifier for the default device. ALC_ENUMERATION_EXT
        /// </summary>
        DefaultDeviceSpecifier = 0x1004,
        /// <summary>
        /// Gets a specific output device's specifier.
        /// Can also be used without a device to get a list of all available output devices, see <see cref="GetEnumerationStringList.DeviceSpecifier"/>. ALC_ENUMERATION_EXT
        /// </summary>
        DeviceSpecifier = 0x1005,
        /// <summary>
        /// Gets the specifier for the default capture device. ALC_ENUMERATION_EXT
        /// </summary>
        DefaultCaptureDeviceSpecifier = 0x311,
        /// <summary>
        /// Gets a specific capture device's specifier.
        /// Can also be used without a device to get a list of all available capture devices, see <see cref="GetEnumerationStringList.CaptureDeviceSpecifier"/>. ALC_ENUMERATION_EXT
        /// </summary>
        CaptureDeviceSpecifier = 0x310,
    }

    /// <summary>
    /// Defines available parameters for <see cref="ALC.GetStringListPtr(ALDevice, GetEnumerationStringList)" />.
    /// </summary>
    public enum GetEnumerationStringList
    {
        /// <summary>
        /// Gets the specifier strings for all available output devices.
        /// Can also be used to get the specifier for a specific device, see <see cref="GetEnumerationString.DeviceSpecifier"/>. ALC_ENUMERATION_EXT
        /// </summary>
        DeviceSpecifier = 0x1005,
        /// <summary>
        /// Gets the specifier strings for all available capture devices.
        /// Can also be used to get the specifier for a specific capture device, see <see cref="GetEnumerationString.DeviceSpecifier"/>. ALC_ENUMERATION_EXT
        /// </summary>
        CaptureDeviceSpecifier = 0x310,
    }

    #endregion

    #region Context Attributes

    /// <summary>
    /// Convenience class for handling ALContext attributes.
    /// </summary>
    public class ALContextAttributes
    {
        /// <summary>
        /// Gets or sets the output buffer frequency in Hz.
        /// This does not actually change any AL state. To apply these attributes see <see cref="ALC.CreateContext(ALDevice, ALContextAttributes)"/>.
        /// </summary>
        public int? Frequency { get; set; }

        /// <summary>
        /// Gets or sets the number of mono sources.
        /// This does not actually change any AL state. To apply these attributes see <see cref="ALC.CreateContext(ALDevice, ALContextAttributes)"/>.
        /// Not guaranteed to get exact number of mono sources when creating a context.
        /// </summary>
        public int? MonoSources { get; set; }

        /// <summary>
        /// Gets or sets the number of stereo sources.
        /// This does not actually change any AL state. To apply these attributes see <see cref="ALC.CreateContext(ALDevice, ALContextAttributes)"/>.
        /// Not guaranteed to get exact number of mono sources when creating a context.
        /// </summary>
        public int? StereoSources { get; set; }

        /// <summary>
        /// Gets or sets the refrash interval in Hz.
        /// This does not actually change any AL state. To apply these attributes see <see cref="ALC.CreateContext(ALDevice, ALContextAttributes)"/>.
        /// </summary>
        public int? Refresh { get; set; }

        /// <summary>
        /// Gets or sets if the context is synchronous.
        /// This does not actually change any AL state. To apply these attributes see <see cref="ALC.CreateContext(ALDevice, ALContextAttributes)"/>.
        /// </summary>
        public bool? Sync { get; set; }

        /// <summary>
        /// Gets or sets additional attributes.
        /// Will usually be the major and minor version numbers of the context. // FIXME: This needs verification. Docs say nothing about this.
        /// </summary>
        public int[] AdditionalAttributes { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ALContextAttributes"/> class.
        /// Leaving all attributes to the driver implementation default values.
        /// </summary>
        public ALContextAttributes() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ALContextAttributes"/> class.
        /// </summary>
        /// <param name="frequency">The mixing output buffer frequency in Hz.</param>
        /// <param name="monoSources">The number of mono sources available. Not guaranteed.</param>
        /// <param name="stereoSources">The number of stereo sources available. Not guaranteed.</param>
        /// <param name="refresh">The refresh interval in Hz.</param>
        /// <param name="sync">If the context is synchronous.</param>
        public ALContextAttributes(int? frequency, int? monoSources, int? stereoSources, int? refresh, bool? sync)
        {
            Frequency = frequency;
            MonoSources = monoSources;
            StereoSources = stereoSources;
            Refresh = refresh;
            Sync = sync;
        }

        /// <summary>
        /// Converts these context attributes to a <see cref="ALC.CreateContext(ALDevice, int[])"/> compatible list.
        /// Alternativly, consider using the more convenient <see cref="ALC.CreateContext(ALDevice, ALContextAttributes)"/> overload.
        /// </summary>
        /// <returns>The attibute list in the form of a span.</returns>
        public int[] CreateAttributeArray()
        {
            // The number of members * 2 + AdditionalAttributes
            var attributeList = new int[(5 * 2) + (AdditionalAttributes?.Length ?? 0) + 1];
            var index = 0;
            void AddAttribute(int? value, AlcContextAttributes attribute)
            {
                if (value != null) { attributeList[index++] = (int)attribute; attributeList[index++] = value ?? default; }
            }
            AddAttribute(Frequency, AlcContextAttributes.Frequency);
            AddAttribute(MonoSources, AlcContextAttributes.MonoSources);
            AddAttribute(StereoSources, AlcContextAttributes.StereoSources);
            AddAttribute(Refresh, AlcContextAttributes.Refresh);
            if (Sync != null) AddAttribute(Sync ?? false ? 1 : 0, AlcContextAttributes.Sync);
            if (AdditionalAttributes != null) { Array.Copy(AdditionalAttributes, 0, attributeList, index, AdditionalAttributes.Length); index += AdditionalAttributes.Length; }
            // Add the trailing null byte.
            attributeList[index++] = 0;
            return attributeList;
        }

        /// <summary>
        /// Parses a AL attribute list.
        /// </summary>
        /// <param name="attributeArray">The AL context attribute list.</param>
        /// <returns>The parsed <see cref="AlcContextAttributes"/> object.</returns>
        internal static ALContextAttributes FromArray(int[] attributeArray)
        {
            var extra = new List<int>();
            var attributes = new ALContextAttributes();
            void ParseAttribute(int @enum, int value)
            {
                switch (@enum)
                {
                    case (int)AlcContextAttributes.Frequency: attributes.Frequency = value; break;
                    case (int)AlcContextAttributes.MonoSources: attributes.MonoSources = value; break;
                    case (int)AlcContextAttributes.StereoSources: attributes.StereoSources = value; break;
                    case (int)AlcContextAttributes.Refresh: attributes.Refresh = value; break;
                    case (int)AlcContextAttributes.Sync: attributes.Sync = value == 1; break;
                    default: extra.Add(@enum); extra.Add(value); break;
                }
            }
            for (var i = 0; i < attributeArray.Length - 1; i += 2) ParseAttribute(attributeArray[i], attributeArray[i + 1]);
            attributes.AdditionalAttributes = extra.ToArray();
            return attributes;
        }

        // Used for ToString.
        string GetOptionalString<T>(string title, T? value) where T : unmanaged => value == null ? null : $"{title}: {value}";

        /// <summary>
        /// Converts the attributes to a string representation.
        /// </summary>
        /// <returns>The string representation of the attributes.</returns>
        public override string ToString()
            => $"{GetOptionalString(nameof(Frequency), Frequency)}, " +
                $"{GetOptionalString(nameof(MonoSources), MonoSources)}, " +
                $"{GetOptionalString(nameof(StereoSources), StereoSources)}, " +
                $"{GetOptionalString(nameof(Refresh), Refresh)}, " +
                $"{GetOptionalString(nameof(Sync), Sync)}" +
                $"{((AdditionalAttributes != null) ? ", " + string.Join(", ", AdditionalAttributes) : string.Empty)}";
    }

    #endregion

    #region Captured Device

    /// <summary>
    /// Handle to an OpenAL capture device.
    /// </summary>
    public struct ALCaptureDevice : IEquatable<ALCaptureDevice>
    {
        public static readonly ALCaptureDevice Null = new(IntPtr.Zero);
        public IntPtr Handle;
        public ALCaptureDevice(IntPtr handle) => Handle = handle;
        public override bool Equals(object obj) => obj is ALCaptureDevice device && Equals(device);
        public bool Equals([AllowNull] ALCaptureDevice other) => Handle.Equals(other.Handle);
        public override int GetHashCode() => HashCode.Combine(Handle);
        public static bool operator ==(ALCaptureDevice left, ALCaptureDevice right) => left.Equals(right);
        public static bool operator !=(ALCaptureDevice left, ALCaptureDevice right) => !(left == right);
        public static implicit operator IntPtr(ALCaptureDevice device) => device.Handle;
    }

    #endregion
}
