using OpenStack.Graphics.Renderer1;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenStack.Graphics.OpenGL.Renderer1.Renderers
{
    public class MaterialRenderer : IRenderer
    {
        readonly IOpenGLGraphic _graphic;
        readonly GLRenderMaterial _material;
        readonly Shader _shader;
        readonly int _quadVao;

        public AABB BoundingBox => new AABB(-1, -1, -1, 1, 1, 1);

        public MaterialRenderer(IOpenGLGraphic graphic, GLRenderMaterial material)
        {
            _graphic = graphic;
            _material = material;
            _shader = _graphic.ShaderManager.LoadPlaneShader(_material.Material.ShaderName, _material.Material.GetShaderArgs());
            _quadVao = SetupQuadBuffer();
        }

        int SetupQuadBuffer()
        {
            GL.UseProgram(_shader.Program);

            // Create and bind VAO
            var vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            var vertices = new[]
            {
                // position     ; normal        ; texcoord  ; tangent
                -1f, -1f, +0f,  +0f, +0f, 1f,   +0f, +1f,   +1f, +0f, +0f,
                -1f, +1f, +0f,  +0f, +0f, 1f,   +0f, +0f,   +1f, +0f, +0f,
                +1f, -1f, +0f,  +0f, +0f, 1f,   +1f, +1f,   +1f, +0f, +0f,
                +1f, +1f, +0f,  +0f, +0f, 1f,   +1f, +0f,   +1f, +0f, +0f,
            };

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);

            var attributes = new List<(string Name, int Size)>
            {
                ("vPOSITION", 3),
                ("vNORMAL", 4),
                ("vTEXCOORD", 2),
                ("vTANGENT", 4),
                ("vBLENDINDICES", 4),
                ("vBLENDWEIGHT", 4),
            };
            var stride = sizeof(float) * attributes.Sum(x => x.Size);
            var offset = 0;
            foreach (var (Name, Size) in attributes)
            {
                var attributeLocation = GL.GetAttribLocation(_shader.Program, Name);
                GL.EnableVertexAttribArray(attributeLocation);
                GL.VertexAttribPointer(attributeLocation, Size, VertexAttribPointerType.Float, false, stride, offset);
                offset += sizeof(float) * Size;
            }

            GL.BindVertexArray(0); // Unbind VAO

            return vao;
        }

        public void Render(Camera camera, RenderPass renderPass)
        {
            GL.UseProgram(_shader.Program);
            GL.BindVertexArray(_quadVao);
            GL.EnableVertexAttribArray(0);

            var uniformLocation = _shader.GetUniformLocation("m_vTintColorSceneObject");
            if (uniformLocation > -1) GL.Uniform4(uniformLocation, Vector4.One);

            uniformLocation = _shader.GetUniformLocation("m_vTintColorDrawCall");
            if (uniformLocation > -1) GL.Uniform3(uniformLocation, Vector3.One);

            var identity = Matrix4.Identity;

            uniformLocation = _shader.GetUniformLocation("uProjectionViewMatrix");
            if (uniformLocation > -1) GL.UniformMatrix4(uniformLocation, false, ref identity);

            uniformLocation = _shader.GetUniformLocation("transform");
            if (uniformLocation > -1) GL.UniformMatrix4(uniformLocation, false, ref identity);

            _material.Render(_shader);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            _material.PostRender();

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void Update(float frameTime) => throw new NotImplementedException();
    }
}
