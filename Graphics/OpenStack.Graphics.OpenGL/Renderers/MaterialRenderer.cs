using OpenStack.Graphics.Renderer;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;

namespace OpenStack.Graphics.OpenGL.Renderers
{
    public class MaterialRenderer : IRenderer
    {
        readonly IOpenGLGraphic _graphic;
        readonly Material _material;
        readonly Shader _shader;
        readonly int _quadVao;

        public AABB BoundingBox => new AABB(-1, -1, -1, 1, 1, 1);

        public MaterialRenderer(IOpenGLGraphic graphic, Material material)
        {
            _graphic = graphic;
            _material = material;
            _shader = _graphic.ShaderManager.LoadPlaneShader(_material.Info.ShaderName, _material.Info.GetShaderArgs());
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

            var uniformLocation = _shader.GetUniformLocation("m_vTintColorSceneObject");
            if (uniformLocation > -1) GL.Uniform4(uniformLocation, Vector4.One);

            uniformLocation = _shader.GetUniformLocation("m_vTintColorDrawCall");
            if (uniformLocation > -1) GL.Uniform3(uniformLocation, Vector3.One);

            var identity = Matrix4.Identity;

            uniformLocation = _shader.GetUniformLocation("projection");
            if (uniformLocation > -1) GL.UniformMatrix4(uniformLocation, false, ref identity);

            uniformLocation = _shader.GetUniformLocation("modelview");
            if (uniformLocation > -1) GL.UniformMatrix4(uniformLocation, false, ref identity);

            _material.Render(_shader);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            _material.PostRender();

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void Update(float frameTime)
            => throw new NotImplementedException();
    }
}
