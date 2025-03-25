using OpenStack.Gfx.Renders;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;

namespace OpenStack.Gfx.Gl.Renders;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : IRenderer
{
    readonly IOpenGLGfx Gfx;
    readonly Shader Shader;
    readonly object ShaderTag;
    readonly int Vao;
    public AABB BoundingBox => new(-1f, -1f, -1f, 1f, 1f, 1f);

    public TestTriRenderer(IOpenGLGfx gfx)
    {
        Gfx = gfx;
        (Shader, ShaderTag) = Gfx.ShaderManager.CreateShader("testtri");
        Vao = SetupVao();
    }

    int SetupVao()
    {
        GL.UseProgram(Shader.Program);

        // create and bind vao
        var vao = GL.GenVertexArray(); GL.BindVertexArray(vao);
        var vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        float[] vertices = [
            // xyz           :rgb
           -0.5f, -0.5f, 0.0f,  1.0f, 0.0f, 0.0f,
            0.5f, -0.5f, 0.0f,  0.0f, 1.0f, 0.0f,
            0.0f,  0.5f, 0.0f,  0.0f, 0.0f, 1.0f
        ];
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // attributes
        GL.EnableVertexAttribArray(0); GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 24, 0);
        GL.EnableVertexAttribArray(1); GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 24, 12);
        GL.BindVertexArray(0); // unbind vao
        return vao;
    }

    public void Render(Camera camera, RenderPass renderPass)
    {
        GL.UseProgram(Shader.Program);
        GL.BindVertexArray(Vao);
        GL.EnableVertexAttribArray(0);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 6);
        GL.BindVertexArray(0); // unbind vao
        GL.UseProgram(0); // unbind program
    }

    public void Update(float deltaTime) { }
}

#endregion

#region TextureRenderer

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
    public AABB BoundingBox => new(-1f, -1f, -1f, 1f, 1f, 1f);

    public TextureRenderer(IOpenGLGfx gfx, int texture, bool background = false)
    {
        Gfx = gfx;
        Texture = texture;
        (Shader, ShaderTag) = Gfx.ShaderManager.CreateShader("plane");
        Vao = SetupVao();
        Background = background;
    }

    int SetupVao()
    {
        GL.UseProgram(Shader.Program);

        // create and bind vao
        var vao = GL.GenVertexArray(); GL.BindVertexArray(vao);
        var vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        float[] vertices = [
            // position     :normal        :texcoord  :tangent
            -1f, -1f, +0f,  +0f, +0f, 1f,  +0f, +1f,  +1f, +0f, +0f,
            -1f, +1f, +0f,  +0f, +0f, 1f,  +0f, +0f,  +1f, +0f, +0f,
            +1f, -1f, +0f,  +0f, +0f, 1f,  +1f, +1f,  +1f, +0f, +0f,
            +1f, +1f, +0f,  +0f, +0f, 1f,  +1f, +0f,  +1f, +0f, +0f,
        ];
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
        int offset = 0, stride = sizeof(float) * attributes.Sum(x => x.size);
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
        GL.BindVertexArray(0); // unbind vao
        GL.UseProgram(0); // unbind program
    }

    public void Update(float deltaTime) { }
}

#endregion

#region MaterialRenderer

/// <summary>
/// MaterialRenderer
/// </summary>
public class MaterialRenderer : IRenderer
{
    readonly IOpenGLGfx Graphic;
    readonly GLRenderMaterial Material;
    readonly Shader Shader;
    readonly object ShaderTag;
    readonly int Vao;
    public AABB BoundingBox => new(-1f, -1f, -1f, 1f, 1f, 1f);

    public MaterialRenderer(IOpenGLGfx graphic, GLRenderMaterial material)
    {
        Graphic = graphic;
        Material = material;
        (Shader, ShaderTag) = Graphic.ShaderManager.CreateShader(Material.Material.ShaderName, Material.Material.GetShaderArgs());
        Vao = SetupVao();
    }

    int SetupVao()
    {
        GL.UseProgram(Shader.Program);

        // create and bind vao
        var vao = GL.GenVertexArray(); GL.BindVertexArray(vao);
        var vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        float[] vertices = [
            // position    normal            texcordr  tangent           blendindices      blendweight
            -1f, -1f, 0f,  0f, 0f, 0f, 1f,   0f, 1f,   1f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,
            -1f, +1f, 0f,  0f, 0f, 0f, 1f,   0f, 0f,   1f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,
            +1f, -1f, 0f,  0f, 0f, 0f, 1f,   1f, 1f,   1f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,
            +1f, +1f, 0f,  0f, 0f, 0f, 1f,   1f, 0f,   1f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,   0f, 0f, 0f, 0f,
        ];
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
        int offset = 0, stride = sizeof(float) * attributes.Sum(x => x.size);
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
        GL.BindVertexArray(Vao);
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
        GL.BindVertexArray(0); // unbind vao
        GL.UseProgram(0); // unbind program
    }

    public void Update(float deltaTime) { }
}

#endregion

#region ParticleGridRenderer

/// <summary>
/// ParticleGridRenderer
/// </summary>
public class ParticleGridRenderer : IRenderer
{
    readonly Shader Shader;
    readonly object ShaderTag;
    readonly int Vao;
    int VertexCount;
    public AABB BoundingBox { get; }

    public ParticleGridRenderer(IOpenGLGfx graphic, float cellWidth, int gridWidthInCells)
    {
        BoundingBox = new AABB(
            -cellWidth * 0.5f * gridWidthInCells, -cellWidth * 0.5f * gridWidthInCells, 0,
            cellWidth * 0.5f * gridWidthInCells, cellWidth * 0.5f * gridWidthInCells, 0);
        (Shader, ShaderTag) = graphic.ShaderManager.CreateShader("vrf.grid");
        Vao = SetupVao(cellWidth, gridWidthInCells);
    }

    static float[] GenerateGridVertexBuffer(float cellWidth, int gridWidthInCells)
    {
        var vertices = new List<float>();
        var width = cellWidth * gridWidthInCells;
        var color = new[] { 1.0f, 1.0f, 1.0f, 1.0f };
        for (var i = 0; i <= gridWidthInCells; i++)
        {
            vertices.AddRange([width, i * cellWidth, 0]);
            vertices.AddRange(color);
            vertices.AddRange([-width, i * cellWidth, 0]);
            vertices.AddRange(color);
        }
        for (var i = 1; i <= gridWidthInCells; i++)
        {
            vertices.AddRange([width, -i * cellWidth, 0]);
            vertices.AddRange(color);
            vertices.AddRange([-width, -i * cellWidth, 0]);
            vertices.AddRange(color);
        }
        for (var i = 0; i <= gridWidthInCells; i++)
        {
            vertices.AddRange([i * cellWidth, width, 0]);
            vertices.AddRange(color);
            vertices.AddRange([i * cellWidth, -width, 0]);
            vertices.AddRange(color);
        }
        for (var i = 1; i <= gridWidthInCells; i++)
        {
            vertices.AddRange([-i * cellWidth, width, 0]);
            vertices.AddRange(color);
            vertices.AddRange([-i * cellWidth, -width, 0]);
            vertices.AddRange(color);
        }
        return [.. vertices];
    }

    int SetupVao(float cellWidth, int gridWidthInCells)
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
        GL.UseProgram(0); // unbind program
        return vao;
    }

    public void Render(Camera camera, RenderPass renderPass)
    {
        var matrix = camera.ViewProjectionMatrix.ToOpenTK();
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.UseProgram(Shader.Program);
        GL.UniformMatrix4(Shader.GetUniformLocation("uProjectionViewMatrix"), false, ref matrix);
        GL.BindVertexArray(Vao);
        GL.EnableVertexAttribArray(0);
        GL.DrawArrays(PrimitiveType.Lines, 0, VertexCount);
        GL.BindVertexArray(0); // unbind vao
        GL.UseProgram(0); // unbind program
        GL.Disable(EnableCap.Blend);
    }

    public void Update(float deltaTime) { }
}

#endregion
