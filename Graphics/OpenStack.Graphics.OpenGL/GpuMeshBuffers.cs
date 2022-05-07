using OpenStack.Graphics.Renderer;
using OpenTK.Graphics.OpenGL;
using System;

namespace OpenStack.Graphics.OpenGL
{
    public class GpuMeshBuffers
    {
        public struct Buffer
        {
#pragma warning disable CA1051 // Do not declare visible instance fields
            public uint Handle;
            public long Size;
#pragma warning restore CA1051 // Do not declare visible instance fields
        }

        public Buffer[] VertexBuffers { get; private set; }
        public Buffer[] IndexBuffers { get; private set; }

        public GpuMeshBuffers(IVBIB vbib)
        {
            VertexBuffers = new Buffer[vbib.VertexBuffers.Count];
            IndexBuffers = new Buffer[vbib.IndexBuffers.Count];

            for (var i = 0; i < vbib.VertexBuffers.Count; i++)
            {
                VertexBuffers[i].Handle = (uint)GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffers[i].Handle);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vbib.VertexBuffers[i].Count * vbib.VertexBuffers[i].Size), vbib.VertexBuffers[i].Buffer, BufferUsageHint.StaticDraw);

                GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out VertexBuffers[i].Size);
            }

            for (var i = 0; i < vbib.IndexBuffers.Count; i++)
            {
                IndexBuffers[i].Handle = (uint)GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffers[i].Handle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(vbib.IndexBuffers[i].Count * vbib.IndexBuffers[i].Size), vbib.IndexBuffers[i].Buffer, BufferUsageHint.StaticDraw);

                GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out IndexBuffers[i].Size);
            }
        }
    }
}
