using System.Runtime.InteropServices;

// https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header-dxt10
namespace OpenStack.Graphics.DirectX
{
    public enum DDS_ALPHA_MODE : uint
    {
        ALPHA_MODE_UNKNOWN = 0,
        ALPHA_MODE_STRAIGHT = 1,
        ALPHA_MODE_PREMULTIPLIED = 2,
        ALPHA_MODE_OPAQUE = 3,
        ALPHA_MODE_CUSTOM = 4,
    }

    /// <summary>
    /// DDS header extension to handle resource arrays, DXGI pixel formats that don't map to the legacy Microsoft DirectDraw pixel format structures, and additional metadata.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0x1)]
    public struct DDS_HEADER_DXT10
    {
        /// <summary>
        /// The size of
        /// </summary>
        public const int SizeOf = 20;

        /// <summary>
        /// The surface pixel format (see DXGI_FORMAT).
        /// </summary>
        [MarshalAs(UnmanagedType.I4)] public DXGI_FORMAT dxgiFormat;
        /// <summary>
        /// Identifies the type of resource. The following values for this member are a subset of the values in the D3D10_RESOURCE_DIMENSION or D3D11_RESOURCE_DIMENSION enumeration:
        /// </summary>
        [MarshalAs(UnmanagedType.U4)] public D3D10_RESOURCE_DIMENSION resourceDimension;
        /// <summary>
        /// Identifies other, less common options for resources. The following value for this member is a subset of the values in the D3D10_RESOURCE_MISC_FLAG or D3D11_RESOURCE_MISC_FLAG enumeration:
        /// </summary>
        public uint miscFlag;
        /// <summary>
        /// The number of elements in the array.
        /// </summary>
        public uint arraySize;
        /// <summary>
        /// Contains additional metadata (formerly was reserved). The lower 3 bits indicate the alpha mode of the associated resource. The upper 29 bits are reserved and are typically 0.
        /// </summary>
        public uint miscFlags2; // see DDS_MISC_FLAGS2
    }
}