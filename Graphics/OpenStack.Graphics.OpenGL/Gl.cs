namespace OpenStack.Graphics.OpenGL
{
    /// <summary>
    /// IOpenGLGraphic
    /// </summary>
    public interface IOpenGLGraphic : IOpenGraphicAny<object, GLRenderMaterial, int, Shader>
    {
        // cache
        public GLMeshBufferCache MeshBufferCache { get; }
        public QuadIndexBuffer QuadIndices { get; }
    }
}