using System.Collections.Generic;

namespace OpenStack.Graphics.Renderer
{
    /// <summary>
    /// IVBIB
    /// </summary>
    public interface IVBIB
    {
        List<OnDiskBufferData> VertexBuffers { get; }
        List<OnDiskBufferData> IndexBuffers { get; }
    }
}
