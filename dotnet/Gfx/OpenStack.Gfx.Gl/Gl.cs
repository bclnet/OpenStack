using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Gfx.Gl
{
    /// <summary>
    /// IOpenGLGfx
    /// </summary>
    public interface IOpenGLGfx : IOpenGfxAny<object, GLRenderMaterial, int, Shader>
    {
        // cache
        public GLMeshBufferCache MeshBufferCache { get; }
        public QuadIndexBuffer QuadIndices { get; }
    }
}