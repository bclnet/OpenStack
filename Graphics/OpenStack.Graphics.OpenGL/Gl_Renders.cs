using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;

namespace OpenStack.Graphics.OpenGL
{
    /// <summary>
    /// TextureRenderer
    /// </summary>
    public class TextureRenderer : IRenderer
    {
        readonly IOpenGLGraphic Graphic;
        readonly int Texture;
        readonly Shader Shader;
        readonly int QuadVao;
        public bool Background;
        public AABB BoundingBox => new AABB(-1f, -1f, -1f, 1f, 1f, 1f);

        public TextureRenderer(IOpenGLGraphic graphic, int texture, bool background = false)
        {
            Graphic = graphic;
            Texture = texture;
            Shader = Graphic.ShaderManager.LoadPlaneShader("plane");
            QuadVao = SetupQuadBuffer();
            Background = background;
        }

        int SetupQuadBuffer()
        {
            GL.UseProgram(Shader.Program);
            // create and bind vao
            var vao = GL.GenVertexArray(); GL.BindVertexArray(vao);
            var vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            var vertices = new[]
            {
                // position     :normal        :texcoord  :tangent
                -1f, -1f, +0f,  +0f, +0f, 1f,  +0f, +1f,  +1f, +0f, +0f,
                -1f, +1f, +0f,  +0f, +0f, 1f,  +0f, +0f,  +1f, +0f, +0f,
                +1f, -1f, +0f,  +0f, +0f, 1f,  +1f, +1f,  +1f, +0f, +0f,
                +1f, +1f, +0f,  +0f, +0f, 1f,  +1f, +0f,  +1f, +0f, +0f,
            };
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            // attributes
            GL.EnableVertexAttribArray(0);
            var attributes = new (string name, int size)[]
            {
                ("vPOSITION", 3),
                ("vNORMAL", 3),
                ("vTEXCOORD", 2),
                ("vTANGENT", 3)
            };
            var stride = sizeof(float) * attributes.Sum(x => x.size);
            var offset = 0;
            foreach (var (name, size) in attributes)
            {
                var location = GL.GetAttribLocation(Shader.Program, name);
                if (location > -1) { GL.EnableVertexAttribArray(location); GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset); }
                offset += sizeof(float) * size;
            }
            GL.BindVertexArray(0); // unbind vao
            return vao;
        }

        public void Render(Camera camera, RenderPass renderPass)
        {
            if (Background) { GL.ClearColor(OpenTK.Color.White); GL.Clear(ClearBufferMask.ColorBufferBit); }
            GL.UseProgram(Shader.Program);
            GL.BindVertexArray(QuadVao);
            GL.EnableVertexAttribArray(0);
            if (Texture > -1) { GL.ActiveTexture(TextureUnit.Texture0); GL.BindTexture(TextureTarget.Texture2D, Texture); }
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void Update(float frameTime) { }
    }

    /// <summary>
    /// MaterialRenderer
    /// </summary>
    public class MaterialRenderer : IRenderer
    {
        readonly IOpenGLGraphic Graphic;
        readonly GLRenderMaterial Material;
        readonly Shader Shader;
        readonly int QuadVao;
        public AABB BoundingBox => new AABB(-1f, -1f, -1f, 1f, 1f, 1f);

        public MaterialRenderer(IOpenGLGraphic graphic, GLRenderMaterial material)
        {
            Graphic = graphic;
            Material = material;
            Shader = Graphic.ShaderManager.LoadShader(Material.Material.ShaderName, Material.Material.GetShaderArgs());
            QuadVao = SetupQuadBuffer();
        }

        int SetupQuadBuffer()
        {
            GL.UseProgram(Shader.Program);
            // create and bind vao
            var vao = GL.GenVertexArray(); GL.BindVertexArray(vao);
            var vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            var vertices = new[]
            {
                // position    normal            texcordr  tangent           blendindices      blendweight
                -1f, -1f, 0f,  0f, 0f, 0f, 1f,   0f, 1f,   1f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,
                -1f, +1f, 0f,  0f, 0f, 0f, 1f,   0f, 0f,   1f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,
                +1f, -1f, 0f,  0f, 0f, 0f, 1f,   1f, 1f,   1f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,
                +1f, +1f, 0f,  0f, 0f, 0f, 1f,   1f, 0f,   1f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,
            };
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            // attributes
            GL.EnableVertexAttribArray(0);
            var attributes = new (string name, int size)[]
            {
                ("vPOSITION", 3),
                ("vNORMAL", 4),
                ("vTEXCOORD", 2),
                ("vTANGENT", 4),
                ("vBLENDINDICES", 4),
                ("vBLENDWEIGHT", 4),
            };
            var stride = sizeof(float) * attributes.Sum(x => x.size);
            var offset = 0;
            foreach (var (name, size) in attributes)
            {
                var location = GL.GetAttribLocation(Shader.Program, name);
                if (location > -1) { GL.EnableVertexAttribArray(location); GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset); }
                offset += sizeof(float) * size;
            }
            GL.BindVertexArray(0); // unbind vao
            return vao;
        }

        public void Render(Camera camera, RenderPass renderPass)
        {
            var identity = Matrix4.Identity;
            GL.UseProgram(Shader.Program);
            GL.BindVertexArray(QuadVao);
            GL.EnableVertexAttribArray(0);
            var uniformLocation = Shader.GetUniformLocation("m_vTintColorSceneObject");
            if (uniformLocation > -1) GL.Uniform4(uniformLocation, Vector4.One);
            uniformLocation = Shader.GetUniformLocation("m_vTintColorDrawCall");
            if (uniformLocation > -1) GL.Uniform3(uniformLocation, Vector3.One);
            uniformLocation = Shader.GetUniformLocation("uProjectionViewMatrix");
            if (uniformLocation > -1) GL.UniformMatrix4(uniformLocation, false, ref identity);
            uniformLocation = Shader.GetUniformLocation("transform");
            if (uniformLocation > -1) GL.UniformMatrix4(uniformLocation, false, ref identity);
            Material.Render(Shader);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            Material.PostRender();
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void Update(float frameTime) { }
    }

    /// <summary>
    /// ParticleGridRenderer
    /// </summary>
    public class ParticleGridRenderer : IRenderer
    {
        readonly int _vao;
        readonly Shader _shader;
        readonly int _vertexCount;
        public AABB BoundingBox { get; }

        public ParticleGridRenderer(float cellWidth, int gridWidthInCells, IOpenGLGraphic graphic)
        {
            const int STRIDE = sizeof(float) * 7;
            BoundingBox = new AABB(
                -cellWidth * 0.5f * gridWidthInCells, -cellWidth * 0.5f * gridWidthInCells, 0,
                cellWidth * 0.5f * gridWidthInCells, cellWidth * 0.5f * gridWidthInCells, 0);
            var vertices = GenerateGridVertexBuffer(cellWidth, gridWidthInCells);
            _vertexCount = vertices.Length / 3; // number of vertices in our buffer
            // shader
            _shader = graphic.LoadShader("vrf.grid");
            GL.UseProgram(_shader.Program);
            // create and bind VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            // attributes
            GL.EnableVertexAttribArray(0);
            var location = GL.GetAttribLocation(_shader.Program, "aVertexPosition");
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, STRIDE, 0);
            var colorAttributeLocation = GL.GetAttribLocation(_shader.Program, "aVertexColor");
            GL.EnableVertexAttribArray(colorAttributeLocation);
            GL.VertexAttribPointer(colorAttributeLocation, 4, VertexAttribPointerType.Float, false, STRIDE, sizeof(float) * 3);
            GL.BindVertexArray(0); // Unbind VAO
            GL.UseProgram(0);
        }

        public void Render(Camera camera, RenderPass renderPass)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.UseProgram(_shader.Program);
            var matrix = camera.ViewProjectionMatrix.ToOpenTK();
            GL.UniformMatrix4(_shader.GetUniformLocation("uProjectionViewMatrix"), false, ref matrix);
            GL.BindVertexArray(_vao);
            GL.EnableVertexAttribArray(0);
            GL.DrawArrays(PrimitiveType.Lines, 0, _vertexCount);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            GL.Disable(EnableCap.Blend);
        }

        public void Update(float frameTime) { }

        static float[] GenerateGridVertexBuffer(float cellWidth, int gridWidthInCells)
        {
            var gridVertices = new List<float>();
            var width = cellWidth * gridWidthInCells;
            var color = new[] { 1.0f, 1.0f, 1.0f, 1.0f };
            for (var i = 0; i <= gridWidthInCells; i++)
            {
                gridVertices.AddRange(new[] { width, i * cellWidth, 0 });
                gridVertices.AddRange(color);
                gridVertices.AddRange(new[] { -width, i * cellWidth, 0 });
                gridVertices.AddRange(color);
            }
            for (var i = 1; i <= gridWidthInCells; i++)
            {
                gridVertices.AddRange(new[] { width, -i * cellWidth, 0 });
                gridVertices.AddRange(color);
                gridVertices.AddRange(new[] { -width, -i * cellWidth, 0 });
                gridVertices.AddRange(color);
            }
            for (var i = 0; i <= gridWidthInCells; i++)
            {
                gridVertices.AddRange(new[] { i * cellWidth, width, 0 });
                gridVertices.AddRange(color);
                gridVertices.AddRange(new[] { i * cellWidth, -width, 0 });
                gridVertices.AddRange(color);
            }
            for (var i = 1; i <= gridWidthInCells; i++)
            {
                gridVertices.AddRange(new[] { -i * cellWidth, width, 0 });
                gridVertices.AddRange(color);
                gridVertices.AddRange(new[] { -i * cellWidth, -width, 0 });
                gridVertices.AddRange(color);
            }
            return gridVertices.ToArray();
        }
    }
}