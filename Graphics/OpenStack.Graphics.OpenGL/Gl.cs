﻿using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenStack.GraphicsTests")]

namespace OpenStack.Graphics.OpenGL
{
    /// <summary>
    /// IOpenGLGraphic
    /// </summary>
    public interface IOpenGLGraphic : IOpenGraphicAny<object, object, GLRenderMaterial, int, Shader>
    {
        // cache
        public GLMeshBufferCache MeshBufferCache { get; }
        public QuadIndexBuffer QuadIndices { get; }
    }
}