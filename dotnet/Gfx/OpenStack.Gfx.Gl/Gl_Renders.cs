using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;

namespace OpenStack.Gfx.Gl
{
    /// <summary>
    /// TextureRenderer
    /// </summary>
    public class TextureRenderer : IRenderer
    {
        readonly IOpenGLGfx Gfx;
        readonly int Texture;
        readonly Shader Shader;
        readonly object ShaderTag;
        readonly int Vao;
        public bool Background;
        public AABB BoundingBox => new AABB(-1f, -1f, -1f, 1f, 1f, 1f);

        public TextureRenderer(IOpenGLGfx gfx, int texture, bool background = false)
        {
            Gfx = gfx;
            Texture = texture;
            (Shader, ShaderTag) = Gfx.ShaderManager.CreatePlaneShader("plane");
            Vao = SetupQuadBuffer();
            Background = background;
        }

        int SetupQuadBuffer()
        {
            GL.UseProgram(Shader.Program);
            // create and bind vao
            var vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
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
                var location = Shader.GetAttribLocation(name);
                if (location > -1) { GL.EnableVertexAttribArray(location); GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset); }
                offset += sizeof(float) * size;
            }
            GL.BindVertexArray(0); // unbind vao
            return vao;
        }

        public void Render(Camera camera, RenderPass renderPass)
        {
            if (Background) { GL.ClearColor(Color.White); GL.Clear(ClearBufferMask.ColorBufferBit); }
            GL.UseProgram(Shader.Program);
            GL.BindVertexArray(Vao);
            GL.EnableVertexAttribArray(0);
            if (Texture > -1) { GL.ActiveTexture(TextureUnit.Texture0); GL.BindTexture(TextureTarget.Texture2D, Texture); }
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void Update(float deltaTime) { }
    }

    /// <summary>
    /// MaterialRenderer
    /// </summary>
    public class MaterialRenderer : IRenderer
    {
        readonly IOpenGLGfx Graphic;
        readonly GLRenderMaterial Material;
        readonly Shader Shader;
        readonly object ShaderTag;
        readonly int QuadVao;
        public AABB BoundingBox => new AABB(-1f, -1f, -1f, 1f, 1f, 1f);

        public MaterialRenderer(IOpenGLGfx graphic, GLRenderMaterial material)
        {
            Graphic = graphic;
            Material = material;
            (Shader, ShaderTag) = Graphic.ShaderManager.CreateShader(Material.Material.ShaderName, Material.Material.GetShaderArgs());
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
                var location = Shader.GetAttribLocation(name);
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
            var location = Shader.GetUniformLocation("m_vTintColorSceneObject");
            if (location > -1) GL.Uniform4(location, Vector4.One);
            location = Shader.GetUniformLocation("m_vTintColorDrawCall");
            if (location > -1) GL.Uniform3(location, Vector3.One);
            location = Shader.GetUniformLocation("uProjectionViewMatrix");
            if (location > -1) GL.UniformMatrix4(location, false, ref identity);
            location = Shader.GetUniformLocation("transform");
            if (location > -1) GL.UniformMatrix4(location, false, ref identity);
            Material.Render(Shader);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            Material.PostRender();
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void Update(float deltaTime) { }
    }

    /// <summary>
    /// ParticleGridRenderer
    /// </summary>
    public class ParticleGridRenderer : IRenderer
    {
        readonly Shader Shader;
        readonly object ShaderTag;
        readonly int QuadVao;
        int VertexCount;
        public AABB BoundingBox { get; }

        public ParticleGridRenderer(IOpenGLGfx graphic, float cellWidth, int gridWidthInCells)
        {
            BoundingBox = new AABB(
                -cellWidth * 0.5f * gridWidthInCells, -cellWidth * 0.5f * gridWidthInCells, 0,
                cellWidth * 0.5f * gridWidthInCells, cellWidth * 0.5f * gridWidthInCells, 0);
            (Shader, ShaderTag) = graphic.ShaderManager.CreateShader("vrf.grid");
            QuadVao = SetupQuadBuffer(cellWidth, gridWidthInCells);
        }

        static float[] GenerateGridVertexBuffer(float cellWidth, int gridWidthInCells)
        {
            var vertices = new List<float>();
            var width = cellWidth * gridWidthInCells;
            var color = new[] { 1.0f, 1.0f, 1.0f, 1.0f };
            for (var i = 0; i <= gridWidthInCells; i++)
            {
                vertices.AddRange(new[] { width, i * cellWidth, 0 });
                vertices.AddRange(color);
                vertices.AddRange(new[] { -width, i * cellWidth, 0 });
                vertices.AddRange(color);
            }
            for (var i = 1; i <= gridWidthInCells; i++)
            {
                vertices.AddRange(new[] { width, -i * cellWidth, 0 });
                vertices.AddRange(color);
                vertices.AddRange(new[] { -width, -i * cellWidth, 0 });
                vertices.AddRange(color);
            }
            for (var i = 0; i <= gridWidthInCells; i++)
            {
                vertices.AddRange(new[] { i * cellWidth, width, 0 });
                vertices.AddRange(color);
                vertices.AddRange(new[] { i * cellWidth, -width, 0 });
                vertices.AddRange(color);
            }
            for (var i = 1; i <= gridWidthInCells; i++)
            {
                vertices.AddRange(new[] { -i * cellWidth, width, 0 });
                vertices.AddRange(color);
                vertices.AddRange(new[] { -i * cellWidth, -width, 0 });
                vertices.AddRange(color);
            }
            return vertices.ToArray();
        }

        int SetupQuadBuffer(float cellWidth, int gridWidthInCells)
        {
            const int STRIDE = sizeof(float) * 7;
            GL.UseProgram(Shader.Program);
            // create and bind vao
            var vao = GL.GenVertexArray(); GL.BindVertexArray(vao);
            var vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            var vertices = GenerateGridVertexBuffer(cellWidth, gridWidthInCells);
            VertexCount = vertices.Length / 3; // number of vertices in our buffer
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            // attributes
            GL.EnableVertexAttribArray(0);
            var location = Shader.GetAttribLocation("aVertexPosition");
            if (location > -1) { GL.EnableVertexAttribArray(location); GL.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, STRIDE, 0); }
            location = Shader.GetAttribLocation("aVertexColor");
            if (location > -1) { GL.EnableVertexAttribArray(location); GL.VertexAttribPointer(location, 4, VertexAttribPointerType.Float, false, STRIDE, sizeof(float) * 3); }
            GL.BindVertexArray(0); // unbind vao
            GL.UseProgram(0);
            return vao;
        }

        public void Render(Camera camera, RenderPass renderPass)
        {
            var matrix = camera.ViewProjectionMatrix.ToOpenTK();
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.UseProgram(Shader.Program);
            GL.UniformMatrix4(Shader.GetUniformLocation("uProjectionViewMatrix"), false, ref matrix);
            GL.BindVertexArray(QuadVao);
            GL.EnableVertexAttribArray(0);
            GL.DrawArrays(PrimitiveType.Lines, 0, VertexCount);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            GL.Disable(EnableCap.Blend);
        }

        public void Update(float deltaTime) { }
    }
}