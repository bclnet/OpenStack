using OpenStack.Graphics;
using OpenStack.Graphics.OpenGL;
using OpenStack.Graphics.Renderer;

namespace OpenStack
{
    public interface IOpenGLGraphic : IOpenGraphic<object, Material, int, Shader>
    {
        // cache
        public GpuMeshBufferCache MeshBufferCache { get; }
        public QuadIndexBuffer QuadIndices { get; }
    }
}