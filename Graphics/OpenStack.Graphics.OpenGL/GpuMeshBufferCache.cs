using OpenStack.Graphics.DirectX;
using OpenStack.Graphics.Renderer;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace OpenStack.Graphics.OpenGL
{
    public class GpuMeshBufferCache
    {
        Dictionary<IVBIB, GpuMeshBuffers> _gpuBuffers = new Dictionary<IVBIB, GpuMeshBuffers>();
        Dictionary<VAOKey, uint> _vertexArrayObjects = new Dictionary<VAOKey, uint>();

        struct VAOKey
        {
            public GpuMeshBuffers VBIB;
            public Shader Shader;
            public uint VertexIndex;
            public uint IndexIndex;
        }

        public GpuMeshBufferCache() { }

        public GpuMeshBuffers GetVertexIndexBuffers(IVBIB vbib)
        {
            if (_gpuBuffers.TryGetValue(vbib, out var gpuVbib)) return gpuVbib;
            else
            {
                var newGpuVbib = new GpuMeshBuffers(vbib);
                _gpuBuffers.Add(vbib, newGpuVbib);
                return newGpuVbib;
            }
        }

        public uint GetVertexArrayObject(IVBIB vbib, Shader shader, uint vtxIndex, uint idxIndex)
        {
            var gpuVbib = GetVertexIndexBuffers(vbib);
            var vaoKey = new VAOKey { VBIB = gpuVbib, Shader = shader, VertexIndex = vtxIndex, IndexIndex = idxIndex };

            if (_vertexArrayObjects.TryGetValue(vaoKey, out var vaoHandle)) return vaoHandle;
            else
            {
                GL.GenVertexArrays(1, out uint newVaoHandle);

                GL.BindVertexArray(newVaoHandle);
                GL.BindBuffer(BufferTarget.ArrayBuffer, gpuVbib.VertexBuffers[vtxIndex].Handle);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, gpuVbib.IndexBuffers[idxIndex].Handle);

                var curVertexBuffer = vbib.VertexBuffers[(int)vtxIndex];
                var texCoordNum = 0;
                foreach (var attribute in curVertexBuffer.Attributes)
                {
                    var attributeName = $"v{attribute.Name}";

                    // TODO: other params too?
                    if (attribute.Name == "TEXCOORD" && texCoordNum++ > 0) attributeName += texCoordNum;

                    BindVertexAttrib(attribute, attributeName, shader.Program, (int)curVertexBuffer.Size);
                }

                GL.BindVertexArray(0);

                _vertexArrayObjects.Add(vaoKey, newVaoHandle);
                return newVaoHandle;
            }
        }

        static void BindVertexAttrib(VertexBuffer.VertexAttribute attribute, string attributeName, int shaderProgram, int stride)
        {
            var attributeLocation = GL.GetAttribLocation(shaderProgram, attributeName);
            // Ignore this attribute if it is not found in the shader
            if (attributeLocation == -1) return;

            GL.EnableVertexAttribArray(attributeLocation);
            switch (attribute.Type)
            {
                case DXGI_FORMAT.R32G32B32_FLOAT: GL.VertexAttribPointer(attributeLocation, 3, VertexAttribPointerType.Float, false, stride, (IntPtr)attribute.Offset); break;
                case DXGI_FORMAT.R8G8B8A8_UNORM: GL.VertexAttribPointer(attributeLocation, 4, VertexAttribPointerType.UnsignedByte, false, stride, (IntPtr)attribute.Offset); break;
                case DXGI_FORMAT.R32G32_FLOAT: GL.VertexAttribPointer(attributeLocation, 2, VertexAttribPointerType.Float, false, stride, (IntPtr)attribute.Offset); break;
                case DXGI_FORMAT.R16G16_FLOAT: GL.VertexAttribPointer(attributeLocation, 2, VertexAttribPointerType.HalfFloat, false, stride, (IntPtr)attribute.Offset); break;
                case DXGI_FORMAT.R32G32B32A32_FLOAT: GL.VertexAttribPointer(attributeLocation, 4, VertexAttribPointerType.Float, false, stride, (IntPtr)attribute.Offset); break;
                case DXGI_FORMAT.R8G8B8A8_UINT: GL.VertexAttribPointer(attributeLocation, 4, VertexAttribPointerType.UnsignedByte, false, stride, (IntPtr)attribute.Offset); break;
                case DXGI_FORMAT.R16G16_SINT: GL.VertexAttribIPointer(attributeLocation, 2, VertexAttribIntegerType.Short, stride, (IntPtr)attribute.Offset); break;
                case DXGI_FORMAT.R16G16B16A16_SINT: GL.VertexAttribIPointer(attributeLocation, 4, VertexAttribIntegerType.Short, stride, (IntPtr)attribute.Offset); break;
                case DXGI_FORMAT.R16G16_UNORM: GL.VertexAttribPointer(attributeLocation, 2, VertexAttribPointerType.UnsignedShort, true, stride, (IntPtr)attribute.Offset); break;
                default: throw new Exception($"Unknown attribute format {attribute.Type}");
            }
        }
    }
}
