using System.Collections.Generic;

namespace OpenStack.Graphics.Renderer
{
    /// <summary>
    /// IVBIB
    /// </summary>
    public interface IVBIB
    {
        List<VertexBuffer> VertexBuffers { get; }
        List<IndexBuffer> IndexBuffers { get; }
    }
}
