using OpenStack.Graphics;
using OpenStack.Graphics.OpenGL.Renderer1;

namespace OpenStack
{
    public interface IOpenGLGraphic : IOpenGraphicAny<object, GLRenderMaterial, int, Shader>
    {
        // cache
        public GLMeshBufferCache MeshBufferCache { get; }
        public QuadIndexBuffer QuadIndices { get; }
    }
}