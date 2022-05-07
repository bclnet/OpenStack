using OpenStack.Graphics.DirectX;
using System.Collections.Generic;

namespace OpenStack.Graphics.Renderer
{
    public struct VertexBuffer
    {
        public struct VertexAttribute
        {
            public string Name;
            public DXGI_FORMAT Type;
            public uint Offset;
        }

        public uint Count;
        public uint Size;
        public List<VertexAttribute> Attributes;
        public byte[] Buffer;
    }
}
