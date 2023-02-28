using OpenStack.Graphics.Renderer1;
using OpenTK.Graphics.OpenGL;
using System;

namespace OpenStack.Graphics.OpenGL.Renderer1.Renderers
{
    public class TextureRenderer : IRenderer
    {
        readonly IOpenGLGraphic _graphic;
        readonly int _texture;
        readonly Shader _shader;
        readonly int _quadVao;

        public AABB BoundingBox => new AABB(-1, -1, -1, 1, 1, 1);

        public TextureRenderer(IOpenGLGraphic graphic, int texture)
        {
            _graphic = graphic;
            _texture = texture;
            _shader = _graphic.ShaderManager.LoadPlaneShader("plane");
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

            var stride = sizeof(float) * 11;

            var positionAttributeLocation = GL.GetAttribLocation(_shader.Program, "vPOSITION");
            GL.EnableVertexAttribArray(positionAttributeLocation);
            GL.VertexAttribPointer(positionAttributeLocation, 3, VertexAttribPointerType.Float, false, stride, 0);

            var normalAttributeLocation = GL.GetAttribLocation(_shader.Program, "vNORMAL");
            GL.EnableVertexAttribArray(normalAttributeLocation);
            GL.VertexAttribPointer(normalAttributeLocation, 3, VertexAttribPointerType.Float, false, stride, sizeof(float) * 3);

            var texCoordAttributeLocation = GL.GetAttribLocation(_shader.Program, "vTEXCOORD");
            GL.EnableVertexAttribArray(texCoordAttributeLocation);
            GL.VertexAttribPointer(texCoordAttributeLocation, 2, VertexAttribPointerType.Float, false, stride, sizeof(float) * 6);

            var tangentAttributeLocation = GL.GetAttribLocation(_shader.Program, "vTANGENT");
            GL.EnableVertexAttribArray(tangentAttributeLocation);
            GL.VertexAttribPointer(tangentAttributeLocation, 3, VertexAttribPointerType.Float, false, stride, sizeof(float) * 8);

            GL.BindVertexArray(0); // Unbind VAO

            return vao;
        }

        public void Render(Camera camera, RenderPass renderPass)
        {
            GL.UseProgram(_shader.Program);
            GL.BindVertexArray(_quadVao);
            GL.EnableVertexAttribArray(0);

            if (_texture > -1)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
            }

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void Update(float frameTime) => throw new NotImplementedException();
    }
}
