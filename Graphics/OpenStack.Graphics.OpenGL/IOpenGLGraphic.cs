using OpenStack.Graphics;
using OpenStack.Graphics.OpenGL.Renderer1;
using OpenStack.Graphics.Renderer1;

namespace OpenStack
{
    public interface IOpenGLGraphic : IOpenGraphic<object, GLRenderMaterial, int, Shader>
    {
        // cache
        public GpuMeshBufferCache MeshBufferCache { get; }
        public QuadIndexBuffer QuadIndices { get; }
    }
}