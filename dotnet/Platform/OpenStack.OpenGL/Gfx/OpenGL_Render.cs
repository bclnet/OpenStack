using OpenStack.Gfx.Egin;
using OpenStack.Gfx.Egin.Particles;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OpenStack.Gfx.OpenGL;

#region OpenGLTextureRenderer

/// <summary>
/// OpenGLTextureRenderer
/// </summary>
public class OpenGLTextureRenderer : EginRenderer {
    const int FACTOR = 1;

    readonly OpenGLGfxModel Gfx;
    readonly object Obj;
    readonly Range Level;
    readonly int Tex;
    readonly Shader Shader;
    readonly object ShaderTag;
    readonly int Vao;
    public bool Background;
    public AABB BoundingBox => new(-1f, -1f, -1f, 1f, 1f, 1f);
    int FrameDelay;

    public OpenGLTextureRenderer(OpenGLGfxModel gfx, object obj, Range level, bool background = false) {
        Gfx = gfx;
        Obj = obj;
        Level = level;
        gfx.TextureManager.DeleteTexture(obj);
        Tex = gfx.TextureManager.CreateTexture(obj, level).tex;
        (Shader, ShaderTag) = gfx.ShaderManager.CreateShader("plane");
        Vao = SetupVao();
        Background = background;
    }

    public override (int, int)? GetViewport((int, int) size)
        => Obj is not ITexture o ? default
        : o.Width > 1024 || o.Height > 1024 || false ? size : (o.Width << FACTOR, o.Height << FACTOR);

    int SetupVao() {
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
        foreach (var (name, size) in attributes) {
            var location = Shader.GetAttribLocation(name);
            if (location > -1) { GL.EnableVertexAttribArray(location); GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset); }
            offset += sizeof(float) * size;
        }
        GL.BindVertexArray(0); // unbind vao
        return vao;
    }

    public override void Render(Camera camera, Pass pass) {
        if (Background) { GL.ClearColor(OpenTK.Color.White); GL.Clear(ClearBufferMask.ColorBufferBit); }
        GL.UseProgram(Shader.Program);
        GL.BindVertexArray(Vao);
        GL.EnableVertexAttribArray(0);
        if (Tex > -1) { GL.ActiveTexture(TextureUnit.Texture0); GL.BindTexture(TextureTarget.Texture2D, Tex); }
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.BindVertexArray(0); // unbind vao
        GL.UseProgram(0); // unbind program
    }

    public override void Update(float deltaTime) {
        if (Obj is not ITextureFrames obj || Gfx == null || !obj.HasFrames) return;
        FrameDelay += (int)deltaTime;
        if (FrameDelay <= obj.Fps || !obj.DecodeFrame()) return;
        FrameDelay = 0; // reset delay between frames
        Gfx.TextureManager.ReloadTexture(obj, Level);
    }
}

#endregion

#region OpenGLObjectRenderer

/// <summary>
/// OpenGLObjectRenderer
/// </summary>
public class OpenGLObjectRenderer : EginRenderer {
    public OpenGLObjectRenderer(OpenGLGfxModel gfx, object obj) {
    }
}

#endregion

#region OpenGLMaterialRenderer

/// <summary>
/// OpenGLMaterialRenderer
/// </summary>
public class OpenGLMaterialRenderer : EginRenderer {
    readonly OpenGLGfxModel Gfx;
    readonly GLRenderMaterial Material;
    readonly Shader Shader;
    readonly object ShaderTag;
    readonly int Vao;
    public AABB BoundingBox => new(-1f, -1f, -1f, 1f, 1f, 1f);

    public OpenGLMaterialRenderer(OpenGLGfxModel gfx, object obj) {
        Gfx = gfx;
        Gfx.TextureManager.DeleteTexture(obj);
        Material = Gfx.MaterialManager.CreateMaterial(obj).mat;
        (Shader, ShaderTag) = Gfx.ShaderManager.CreateShader(Material.Material.ShaderName, Material.Material.ShaderArgs);
        Vao = SetupVao();
    }

    int SetupVao() {
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
        foreach (var (name, size) in attributes) {
            var location = Shader.GetAttribLocation(name);
            if (location > -1) { GL.EnableVertexAttribArray(location); GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset); }
            offset += sizeof(float) * size;
        }
        GL.BindVertexArray(0); // unbind vao
        return vao;
    }

    public override void Render(Camera camera, Pass pass) {
        var identity = OpenTK.Matrix4.Identity;
        GL.UseProgram(Shader.Program);
        GL.BindVertexArray(Vao);
        GL.EnableVertexAttribArray(0);
        var location = Shader.GetUniformLocation("m_vTintColorSceneObject");
        if (location > -1) GL.Uniform4(location, OpenTK.Vector4.One);
        location = Shader.GetUniformLocation("m_vTintColorDrawCall");
        if (location > -1) GL.Uniform3(location, OpenTK.Vector3.One);
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
}

#endregion

#region OpenGLGridRenderer

/// <summary>
/// OpenGLGridRenderer
/// </summary>
public class OpenGLGridRenderer : EginRenderer {
    readonly Shader Shader;
    readonly object ShaderTag;
    readonly int Vao;
    int VertexCount;
    public AABB BoundingBox { get; }

    public OpenGLGridRenderer(OpenGLGfxModel gfx, float cellWidth, int gridWidthInCells) {
        BoundingBox = new AABB(
            -cellWidth * 0.5f * gridWidthInCells, -cellWidth * 0.5f * gridWidthInCells, 0,
            cellWidth * 0.5f * gridWidthInCells, cellWidth * 0.5f * gridWidthInCells, 0);
        (Shader, ShaderTag) = gfx.ShaderManager.CreateShader("vrf.grid");
        Vao = SetupVao(cellWidth, gridWidthInCells);
    }

    static float[] GenerateGridVertexBuffer(float cellWidth, int gridWidthInCells) {
        var vertices = new List<float>();
        var width = cellWidth * gridWidthInCells;
        var color = new[] { 1.0f, 1.0f, 1.0f, 1.0f };
        for (var i = 0; i <= gridWidthInCells; i++) {
            vertices.AddRange([width, i * cellWidth, 0]);
            vertices.AddRange(color);
            vertices.AddRange([-width, i * cellWidth, 0]);
            vertices.AddRange(color);
        }
        for (var i = 1; i <= gridWidthInCells; i++) {
            vertices.AddRange([width, -i * cellWidth, 0]);
            vertices.AddRange(color);
            vertices.AddRange([-width, -i * cellWidth, 0]);
            vertices.AddRange(color);
        }
        for (var i = 0; i <= gridWidthInCells; i++) {
            vertices.AddRange([i * cellWidth, width, 0]);
            vertices.AddRange(color);
            vertices.AddRange([i * cellWidth, -width, 0]);
            vertices.AddRange(color);
        }
        for (var i = 1; i <= gridWidthInCells; i++) {
            vertices.AddRange([-i * cellWidth, width, 0]);
            vertices.AddRange(color);
            vertices.AddRange([-i * cellWidth, -width, 0]);
            vertices.AddRange(color);
        }
        return [.. vertices];
    }

    int SetupVao(float cellWidth, int gridWidthInCells) {
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

    public override void Render(Camera camera, Pass pass) {
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
}

#endregion

#region OpenGLParticleRenderer

public class OpenGLParticleRenderer : EginRenderer {
    #region SpritesParticleRenderer

    public class SpritesParticleRenderer : IParticleRenderer {
        const int VertexSize = 9;
        readonly Shader Shader;
        readonly object ShaderTag;
        readonly int Vao;
        readonly int Texture;
        readonly object TextureTag;
        readonly TextureSequences TextureSequences;
        readonly float AnimationRate;
        readonly bool Additive;
        readonly float OverbrightFactor;
        readonly long OrientationType;
        float[] RawVertices;
        QuadIndexBuffer QuadIndices;
        int VertexBufferHandle;

        public SpritesParticleRenderer(IDictionary<string, object> keyValues, OpenGLGfxModel gfx) {
            (Shader, ShaderTag) = gfx.ShaderManager.CreateShader("vrf.particle.sprite");
            QuadIndices = gfx.QuadIndices;

            // The same quad is reused for all particles
            Vao = MakeVao();

            string textureName = null;
            if (keyValues.ContainsKey("m_hTexture")) textureName = keyValues.Get<string>("m_hTexture");
            else if (keyValues.ContainsKey("m_vecTexturesInput")) {
                var textures = keyValues.GetArray("m_vecTexturesInput");
                if (textures.Length > 0) textureName = textures[0].Get<string>("m_hTexture");
            }
            if (textureName != null) {
                (Texture, TextureTag) = gfx.TextureManager.CreateTexture(textureName);
                if (TextureTag is IDictionary<string, object> info)
                    TextureSequences = info.Get<TextureSequences>("sequences");
            }
            else Texture = gfx.TextureManager.DefaultTexture;

            Additive = keyValues.Get<bool>("m_bAdditive");
            OverbrightFactor = keyValues.GetFloat("m_flOverbrightFactor", 1f);
            OrientationType = keyValues.GetInt64("m_nOrientationType");
            AnimationRate = keyValues.GetFloat("m_flAnimationRate", .1f);
        }

        int MakeVao() {
            GL.UseProgram(Shader.Program);

            // Create and bind VAO
            var vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            VertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);

            var stride = sizeof(float) * VertexSize;
            var positionAttributeLocation = Shader.GetAttribLocation("aVertexPosition");
            GL.VertexAttribPointer(positionAttributeLocation, 3, VertexAttribPointerType.Float, false, stride, 0);
            var colorAttributeLocation = Shader.GetAttribLocation("aVertexColor");
            GL.VertexAttribPointer(colorAttributeLocation, 4, VertexAttribPointerType.Float, false, stride, sizeof(float) * 3);
            var uvAttributeLocation = Shader.GetAttribLocation("aTexCoords");
            GL.VertexAttribPointer(uvAttributeLocation, 2, VertexAttribPointerType.Float, false, stride, sizeof(float) * 7);

            GL.EnableVertexAttribArray(positionAttributeLocation);
            GL.EnableVertexAttribArray(colorAttributeLocation);
            GL.EnableVertexAttribArray(uvAttributeLocation);

            GL.BindVertexArray(0);

            return vao;
        }

        //static (int TextureIndex, Texture TextureData) LoadTexture(string textureName, GuiContext guiContext)
        //{
        //    var textureResource = guiContext.LoadFileByAnyMeansNecessary(textureName);
        //    return textureResource == null
        //        ? (guiContext.MaterialLoader.GetErrorTexture(), null)
        //        : (guiContext.MaterialLoader.LoadTexture(textureName), (Texture)textureResource.DataBlock);
        //}

        void EnsureSpaceForVertices(int count) {
            var numFloats = count * VertexSize;
            if (RawVertices == null) RawVertices = new float[numFloats];
            else if (RawVertices.Length < numFloats) {
                var nextSize = (count / 64 + 1) * 64 * VertexSize;
                Array.Resize(ref RawVertices, nextSize);
            }
        }

        void UpdateVertices(ParticleBag particleBag, Matrix4x4 modelViewMatrix) {
            var particles = particleBag.LiveParticles;

            // Create billboarding rotation (always facing camera)
            Matrix4x4.Decompose(modelViewMatrix, out _, out Quaternion modelViewRotation, out _);
            modelViewRotation = Quaternion.Inverse(modelViewRotation);
            var billboardMatrix = Matrix4x4.CreateFromQuaternion(modelViewRotation);

            // Update vertex buffer
            EnsureSpaceForVertices(particleBag.Count * 4);
            for (var i = 0; i < particleBag.Count; ++i) {
                // Positions
                var modelMatrix = OrientationType == 0
                    ? particles[i].GetRotationMatrix() * billboardMatrix * particles[i].GetTransformationMatrix()
                    : particles[i].GetRotationMatrix() * particles[i].GetTransformationMatrix();

                var tl = Vector4.Transform(new Vector4(-1, -1, 0, 1), modelMatrix);
                var bl = Vector4.Transform(new Vector4(-1, 1, 0, 1), modelMatrix);
                var br = Vector4.Transform(new Vector4(1, 1, 0, 1), modelMatrix);
                var tr = Vector4.Transform(new Vector4(1, -1, 0, 1), modelMatrix);

                var quadStart = i * VertexSize * 4;
                RawVertices[quadStart + 0] = tl.X;
                RawVertices[quadStart + 1] = tl.Y;
                RawVertices[quadStart + 2] = tl.Z;
                RawVertices[quadStart + VertexSize * 1 + 0] = bl.X;
                RawVertices[quadStart + VertexSize * 1 + 1] = bl.Y;
                RawVertices[quadStart + VertexSize * 1 + 2] = bl.Z;
                RawVertices[quadStart + VertexSize * 2 + 0] = br.X;
                RawVertices[quadStart + VertexSize * 2 + 1] = br.Y;
                RawVertices[quadStart + VertexSize * 2 + 2] = br.Z;
                RawVertices[quadStart + VertexSize * 3 + 0] = tr.X;
                RawVertices[quadStart + VertexSize * 3 + 1] = tr.Y;
                RawVertices[quadStart + VertexSize * 3 + 2] = tr.Z;

                // Colors
                for (var j = 0; j < 4; ++j) {
                    RawVertices[quadStart + VertexSize * j + 3] = particles[i].Color.X;
                    RawVertices[quadStart + VertexSize * j + 4] = particles[i].Color.Y;
                    RawVertices[quadStart + VertexSize * j + 5] = particles[i].Color.Z;
                    RawVertices[quadStart + VertexSize * j + 6] = particles[i].Alpha;
                }

                // UVs
                if (TextureSequences != null && TextureSequences.Count > 0 && TextureSequences[0].Frames.Count > 0) {
                    var sequence = TextureSequences[particles[i].Sequence % TextureSequences.Count];

                    var particleTime = particles[i].ConstantLifetime - particles[i].Lifetime;
                    var frame = particleTime * sequence.FramesPerSecond * AnimationRate;

                    var currentFrame = sequence.Frames[(int)Math.Floor(frame) % sequence.Frames.Count];
                    var currentImage = currentFrame.Images[0]; // TODO: Support more than one image per frame?

                    // Lerp frame coords and size
                    var subFrameTime = frame % 1.0f;
                    var offset = currentImage.CroppedMin * (1 - subFrameTime) + currentImage.UncroppedMin * subFrameTime;
                    var scale = (currentImage.CroppedMax - currentImage.CroppedMin) * (1 - subFrameTime) +
                        (currentImage.UncroppedMax - currentImage.UncroppedMin) * subFrameTime;

                    RawVertices[quadStart + VertexSize * 0 + 7] = offset.X + scale.X * 0;
                    RawVertices[quadStart + VertexSize * 0 + 8] = offset.Y + scale.Y * 1;
                    RawVertices[quadStart + VertexSize * 1 + 7] = offset.X + scale.X * 0;
                    RawVertices[quadStart + VertexSize * 1 + 8] = offset.Y + scale.Y * 0;
                    RawVertices[quadStart + VertexSize * 2 + 7] = offset.X + scale.X * 1;
                    RawVertices[quadStart + VertexSize * 2 + 8] = offset.Y + scale.Y * 0;
                    RawVertices[quadStart + VertexSize * 3 + 7] = offset.X + scale.X * 1;
                    RawVertices[quadStart + VertexSize * 3 + 8] = offset.Y + scale.Y * 1;
                }
                else {
                    RawVertices[quadStart + VertexSize * 0 + 7] = 0;
                    RawVertices[quadStart + VertexSize * 0 + 8] = 1;
                    RawVertices[quadStart + VertexSize * 1 + 7] = 0;
                    RawVertices[quadStart + VertexSize * 1 + 8] = 0;
                    RawVertices[quadStart + VertexSize * 2 + 7] = 1;
                    RawVertices[quadStart + VertexSize * 2 + 8] = 0;
                    RawVertices[quadStart + VertexSize * 3 + 7] = 1;
                    RawVertices[quadStart + VertexSize * 3 + 8] = 1;
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, particleBag.Count * VertexSize * 4 * sizeof(float), RawVertices, BufferUsageHint.DynamicDraw);
        }

        public void Render(ParticleBag particleBag, Matrix4x4 viewProjectionMatrix, Matrix4x4 modelViewMatrix) {
            if (particleBag.Count == 0) return;

            // Update vertex buffer
            UpdateVertices(particleBag, modelViewMatrix);

            // Draw it
            GL.Enable(EnableCap.Blend);
            GL.UseProgram(Shader.Program);

            if (Additive) GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            else GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindVertexArray(Vao);
            GL.EnableVertexAttribArray(0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Texture);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);

            GL.Uniform1(Shader.GetUniformLocation("uTexture"), 0); // set texture unit 0 as uTexture uniform

            var otkProjection = viewProjectionMatrix.ToOpenTK();
            GL.UniformMatrix4(Shader.GetUniformLocation("uProjectionViewMatrix"), false, ref otkProjection);

            // TODO: This formula is a guess but still seems too bright compared to valve particles
            GL.Uniform1(Shader.GetUniformLocation("uOverbrightFactor"), OverbrightFactor);

            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(false);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, QuadIndices.GLHandle);
            GL.DrawElements(BeginMode.Triangles, particleBag.Count * 6, DrawElementsType.UnsignedShort, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(true);

            GL.BindVertexArray(0);
            GL.UseProgram(0);

            if (Additive) GL.BlendEquation(BlendEquationMode.FuncAdd);

            GL.Disable(EnableCap.Blend);
        }

        public IEnumerable<string> GetSupportedRenderModes() => Shader.RenderModes;

        public void SetRenderMode(string renderMode) {
            var parameters = new Dictionary<string, bool>();
            if (renderMode != null && Shader.RenderModes.Contains(renderMode)) parameters.Add($"renderMode_{renderMode}", true);
            //_shader = guiContext.ShaderLoader.LoadShader(ShaderName, parameters);
        }
    }

    #endregion

    #region TrailsParticleRenderer

    public class TrailsParticleRenderer : IParticleRenderer {
        readonly Shader Shader;
        readonly object ShaderTag;
        readonly int QuadVao;
        readonly int Texture;
        readonly object TextureTag;

        readonly TextureSequences TextureSequences;
        readonly float AnimationRate;

        readonly bool Additive;
        readonly float OverbrightFactor;
        readonly long OrientationType;

        readonly float FinalTextureScaleU;
        readonly float FinalTextureScaleV;

        readonly float MaxLength;
        readonly float LengthFadeInTime;

        public TrailsParticleRenderer(IDictionary<string, object> keyValues, OpenGLGfxModel graphic) {
            (Shader, ShaderTag) = graphic.ShaderManager.CreateShader("vrf.particle.trail", new Dictionary<string, bool>());

            // The same quad is reused for all particles
            QuadVao = SetupQuadBuffer();

            string textureName = null;
            if (keyValues.ContainsKey("m_hTexture")) textureName = keyValues.Get<string>("m_hTexture");
            else if (keyValues.ContainsKey("m_vecTexturesInput")) {
                var textures = keyValues.GetArray("m_vecTexturesInput");
                if (textures.Length > 0) textureName = textures[0].Get<string>("m_hTexture");
            }

            if (textureName != null) {
                (Texture, TextureTag) = graphic.TextureManager.CreateTexture(textureName);
                if (TextureTag is IDictionary<string, object> info)
                    TextureSequences = info.Get<TextureSequences>("sequences");
            }
            else Texture = graphic.TextureManager.DefaultTexture;

            Additive = keyValues.Get<bool>("m_bAdditive");
            OverbrightFactor = keyValues.GetFloat("m_flOverbrightFactor", 1f);
            OrientationType = keyValues.GetInt64("m_nOrientationType");

            AnimationRate = keyValues.GetFloat("m_flAnimationRate", .1f);

            FinalTextureScaleU = keyValues.GetFloat("m_flFinalTextureScaleU", 1f);
            FinalTextureScaleV = keyValues.GetFloat("m_flFinalTextureScaleV", 1f);

            MaxLength = keyValues.GetFloat("m_flMaxLength", 2000f);
            LengthFadeInTime = keyValues.GetFloat("m_flLengthFadeInTime");
        }

        int SetupQuadBuffer() {
            GL.UseProgram(Shader.Program);

            // Create and bind VAO
            var vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            var vertices = new[]
            {
            -1.0f, -1.0f, 0.0f,
            -1.0f, 1.0f, 0.0f,
            1.0f, -1.0f, 0.0f,
            1.0f, 1.0f, 0.0f,
        };

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);

            var positionAttributeLocation = Shader.GetAttribLocation("aVertexPosition");
            GL.VertexAttribPointer(positionAttributeLocation, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindVertexArray(0); // Unbind VAO

            return vao;
        }

        //static (int TextureIndex, Texture TextureData) LoadTexture(string textureName, GuiContext guiContext)
        //{
        //    var textureResource = guiContext.LoadFileByAnyMeansNecessary(textureName);

        //    return textureResource == null
        //        ? (guiContext.MaterialLoader.GetErrorTexture(), null)
        //    : (guiContext.MaterialLoader.LoadTexture(textureName), (Texture)textureResource.DataBlock);
        //}

        public void Render(ParticleBag particleBag, Matrix4x4 viewProjectionMatrix, Matrix4x4 modelViewMatrix) {
            var particles = particleBag.LiveParticles;

            GL.Enable(EnableCap.Blend);
            GL.UseProgram(Shader.Program);

            if (Additive) GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            else GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindVertexArray(QuadVao);
            GL.EnableVertexAttribArray(0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Texture);

            GL.Uniform1(Shader.GetUniformLocation("uTexture"), 0); // set texture unit 0 as uTexture uniform

            var otkProjection = viewProjectionMatrix.ToOpenTK();
            GL.UniformMatrix4(Shader.GetUniformLocation("uProjectionViewMatrix"), false, ref otkProjection);

            // TODO: This formula is a guess but still seems too bright compared to valve particles
            GL.Uniform1(Shader.GetUniformLocation("uOverbrightFactor"), OverbrightFactor);

            var modelMatrixLocation = Shader.GetUniformLocation("uModelMatrix");
            var colorLocation = Shader.GetUniformLocation("uColor");
            var alphaLocation = Shader.GetUniformLocation("uAlpha");
            var uvOffsetLocation = Shader.GetUniformLocation("uUvOffset");
            var uvScaleLocation = Shader.GetUniformLocation("uUvScale");

            // Create billboarding rotation (always facing camera)
            Matrix4x4.Decompose(modelViewMatrix, out _, out Quaternion modelViewRotation, out _);
            modelViewRotation = Quaternion.Inverse(modelViewRotation);
            var billboardMatrix = Matrix4x4.CreateFromQuaternion(modelViewRotation);

            for (var i = 0; i < particles.Length; ++i) {
                var position = new Vector3(particles[i].Position.X, particles[i].Position.Y, particles[i].Position.Z);
                var previousPosition = new Vector3(particles[i].PositionPrevious.X, particles[i].PositionPrevious.Y, particles[i].PositionPrevious.Z);
                var difference = previousPosition - position;
                var direction = Vector3.Normalize(difference);

                var midPoint = position + 0.5f * difference;

                // Trail width = radius
                // Trail length = distance between current and previous times trail length divided by 2 (because the base particle is 2 wide)
                var length = Math.Min(MaxLength, particles[i].TrailLength * difference.Length() / 2f);
                var t = 1 - particles[i].Lifetime / particles[i].ConstantLifetime;
                var animatedLength = t >= LengthFadeInTime
                    ? length
                    : t * length / LengthFadeInTime;
                var scaleMatrix = Matrix4x4.CreateScale(particles[i].Radius, animatedLength, 1);

                // Center the particle at the midpoint between the two points
                var translationMatrix = Matrix4x4.CreateTranslation(Vector3.UnitY * animatedLength);

                // Calculate rotation matrix

                var axis = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, direction));
                var angle = (float)Math.Acos(direction.Y);
                var rotationMatrix = Matrix4x4.CreateFromAxisAngle(axis, angle);

                var modelMatrix =
                    OrientationType == 0 ? Matrix4x4.Multiply(scaleMatrix, Matrix4x4.Multiply(translationMatrix, rotationMatrix))
                    : particles[i].GetTransformationMatrix();

                // Position/Radius uniform
                var otkModelMatrix = modelMatrix.ToOpenTK();
                GL.UniformMatrix4(modelMatrixLocation, false, ref otkModelMatrix);

                if (TextureSequences != null && TextureSequences.Count > 0 && TextureSequences[0].Frames.Count > 0) {
                    var sequence = TextureSequences[0];

                    var particleTime = particles[i].ConstantLifetime - particles[i].Lifetime;
                    var frame = particleTime * sequence.FramesPerSecond * AnimationRate;

                    var currentFrame = sequence.Frames[(int)Math.Floor(frame) % sequence.Frames.Count];
                    var currentImage = currentFrame.Images[0]; // TODO: Support more than one image per frame?

                    // Lerp frame coords and size
                    var subFrameTime = frame % 1.0f;
                    var offset = currentImage.CroppedMin * (1 - subFrameTime) + currentImage.UncroppedMin * subFrameTime;
                    var scale = (currentImage.CroppedMax - currentImage.CroppedMin) * (1 - subFrameTime) +
                        (currentImage.UncroppedMax - currentImage.UncroppedMin) * subFrameTime;

                    GL.Uniform2(uvOffsetLocation, offset.X, offset.Y);
                    GL.Uniform2(uvScaleLocation, scale.X * FinalTextureScaleU, scale.Y * FinalTextureScaleV);
                }
                else {
                    GL.Uniform2(uvOffsetLocation, 1f, 1f);
                    GL.Uniform2(uvScaleLocation, FinalTextureScaleU, FinalTextureScaleV);
                }

                // Color uniform
                GL.Uniform3(colorLocation, particles[i].Color.X, particles[i].Color.Y, particles[i].Color.Z);

                GL.Uniform1(alphaLocation, particles[i].Alpha * particles[i].AlphaAlternate);

                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            }

            GL.BindVertexArray(0);
            GL.UseProgram(0);

            if (Additive) GL.BlendEquation(BlendEquationMode.FuncAdd);

            GL.Disable(EnableCap.Blend);
        }

        public IEnumerable<string> GetSupportedRenderModes() => Shader.RenderModes;

        public void SetRenderMode(string renderMode) {
            var parameters = new Dictionary<string, bool>();
            if (renderMode != null && Shader.RenderModes.Contains(renderMode)) parameters.Add($"renderMode_{renderMode}", true);
            //_shader = graphic.LoadShader(ShaderName, parameters);
        }
    }

    #endregion

    #region ParticleRenderer

    public class ParticleRenderer {
        public IEnumerable<IParticleEmitter> Emitters = [];
        public IEnumerable<IParticleInitializer> Initializers = [];
        public IEnumerable<IParticleOperator> Operators = [];
        public IEnumerable<IParticleRenderer> Renderers = [];

        public AABB BoundingBox { get; private set; }

        public Vector3 Position {
            get => _systemRenderState.GetControlPoint(0);
            set {
                _systemRenderState.SetControlPoint(0, value);
                foreach (var child in _childParticleRenderers) child.Position = value;
            }
        }

        readonly OpenGLGfxModel _gfx;
        readonly List<ParticleRenderer> _childParticleRenderers;
        bool _hasStarted = false;

        ParticleBag _particleBag;
        int _particlesEmitted = 0;
        ParticleSystemRenderState _systemRenderState;

        // TODO: Passing in position here was for testing, do it properly
        public ParticleRenderer(OpenGLGfxModel gfx, IParticleSystem particleSystem, Vector3 pos = default) {
            _gfx = gfx;
            _childParticleRenderers = [];

            _particleBag = new ParticleBag(100, true);
            _systemRenderState = new ParticleSystemRenderState();
            _systemRenderState.SetControlPoint(0, pos);

            BoundingBox = new AABB(pos + new Vector3(-32, -32, -32), pos + new Vector3(32, 32, 32));

            SetupEmitters(particleSystem.Data, particleSystem.Emitters);
            SetupInitializers(particleSystem.Initializers);
            SetupOperators(particleSystem.Operators);
            SetupRenderers(particleSystem.Renderers);

            SetupChildParticles(particleSystem.GetChildParticleNames(true));
        }

        public void Start() {
            foreach (var emitter in Emitters) emitter.Start(EmitParticle);
            foreach (var childParticleRenderer in _childParticleRenderers) childParticleRenderer.Start();
        }

        void EmitParticle() {
            var index = _particleBag.Add();
            if (index < 0) { Console.WriteLine("Out of space in particle bag"); return; }
            _particleBag.LiveParticles[index].ParticleCount = _particlesEmitted++;
            InitializeParticle(ref _particleBag.LiveParticles[index]);
        }

        void InitializeParticle(ref Particle p) {
            p.Position = _systemRenderState.GetControlPoint(0);
            foreach (var initializer in Initializers) initializer.Initialize(ref p, _systemRenderState);
        }

        public void Stop() {
            foreach (var emitter in Emitters) emitter.Stop();
            foreach (var childParticleRenderer in _childParticleRenderers) childParticleRenderer.Stop();
        }

        public void Restart() {
            Stop();
            _systemRenderState.Lifetime = 0;
            _particleBag.Clear();
            Start();

            foreach (var childParticleRenderer in _childParticleRenderers) childParticleRenderer.Restart();
        }

        public void Update(float deltaTime) {
            if (!_hasStarted) { Start(); _hasStarted = true; }

            _systemRenderState.Lifetime += deltaTime;

            foreach (var emitter in Emitters) emitter.Update(deltaTime);
            foreach (var particleOperator in Operators) particleOperator.Update(_particleBag.LiveParticles, deltaTime, _systemRenderState);

            // Remove all dead particles
            _particleBag.PruneExpired();

            var center = _systemRenderState.GetControlPoint(0);
            if (_particleBag.Count == 0) BoundingBox = new AABB(center, center);
            else {
                var minParticlePos = center;
                var maxParticlePos = center;

                var liveParticles = _particleBag.LiveParticles;
                for (var i = 0; i < liveParticles.Length; ++i) {
                    var pos = liveParticles[i].Position;
                    var radius = liveParticles[i].Radius;
                    minParticlePos = Vector3.Min(minParticlePos, pos - new Vector3(radius));
                    maxParticlePos = Vector3.Max(maxParticlePos, pos + new Vector3(radius));
                }

                BoundingBox = new AABB(minParticlePos, maxParticlePos);
            }

            foreach (var childParticleRenderer in _childParticleRenderers) {
                childParticleRenderer.Update(deltaTime);
                BoundingBox = BoundingBox.Union(childParticleRenderer.BoundingBox);
            }

            // Restart if all emitters are done and all particles expired
            if (IsFinished()) Restart();
        }

        public bool IsFinished()
            => Emitters.All(e => e.IsFinished)
            && _particleBag.Count == 0
            && _childParticleRenderers.All(r => r.IsFinished());

        public void Render(Camera camera, Pass pass) {
            if (_particleBag.Count == 0) return;
            if (pass == Pass.Translucent || pass == Pass.Both)
                foreach (var renderer in Renderers) renderer.Render(_particleBag, camera.ViewProjectionMatrix, camera.CameraViewMatrix);
            foreach (var childParticleRenderer in _childParticleRenderers) childParticleRenderer.Render(camera, Pass.Both);
        }

        public IEnumerable<string> GetSupportedRenderModes() => Renderers.SelectMany(renderer => renderer.GetSupportedRenderModes()).Distinct();

        void SetupEmitters(IDictionary<string, object> baseProperties, IEnumerable<IDictionary<string, object>> emitterData) {
            var emitters = new List<IParticleEmitter>();
            foreach (var emitterInfo in emitterData) {
                var emitterClass = emitterInfo.Get<string>("_class");
                if (ParticleControllerFactory.TryCreateEmitter(emitterClass, baseProperties, emitterInfo, out var emitter)) emitters.Add(emitter);
                else Console.WriteLine($"Unsupported emitter class '{emitterClass}'.");
            }
            Emitters = emitters;
        }

        void SetupInitializers(IEnumerable<IDictionary<string, object>> initializerData) {
            var initializers = new List<IParticleInitializer>();
            foreach (var initializerInfo in initializerData) {
                var initializerClass = initializerInfo.Get<string>("_class");
                if (ParticleControllerFactory.TryCreateInitializer(initializerClass, initializerInfo, out var initializer)) initializers.Add(initializer);
                else Console.WriteLine($"Unsupported initializer class '{initializerClass}'.");
            }
            Initializers = initializers;
        }

        void SetupOperators(IEnumerable<IDictionary<string, object>> operatorData) {
            var operators = new List<IParticleOperator>();
            foreach (var operatorInfo in operatorData) {
                var operatorClass = operatorInfo.Get<string>("_class");
                if (ParticleControllerFactory.TryCreateOperator(operatorClass, operatorInfo, out var @operator)) operators.Add(@operator);
                else Console.WriteLine($"Unsupported operator class '{operatorClass}'.");
            }
            Operators = operators;
        }

        void SetupRenderers(IEnumerable<IDictionary<string, object>> rendererData) {
            var renderers = new List<IParticleRenderer>();
            foreach (var rendererInfo in rendererData) {
                var rendererClass = rendererInfo.Get<string>("_class");
                if (ParticleControllerFactory.TryCreateRender(rendererClass, rendererInfo, _gfx, out var renderer)) renderers.Add(renderer);
                else Console.WriteLine($"Unsupported renderer class '{rendererClass}'.");
            }
            Renderers = renderers;
        }

        void SetupChildParticles(IEnumerable<string> childNames) {
            foreach (var childName in childNames) {
                var childSystem = _gfx.GetAsset<IParticleSystem>(childName).Result;
                _childParticleRenderers.Add(new ParticleRenderer(_gfx, childSystem, _systemRenderState.GetControlPoint(0)));
            }
        }
    }

    #endregion

    public OpenGLParticleRenderer(OpenGLGfxModel gfx, object obj) {
    }
}

#endregion

#region OpenGLCellRenderer

/// <summary>
/// OpenGLCellRenderer
/// </summary>
public class OpenGLCellRenderer : EginRenderer {
    public OpenGLCellRenderer(OpenGLGfxModel gfx, object obj) {
    }
}

#endregion

#region OpenGLWorldRenderer

/// <summary>
/// OpenGLWorldRenderer
/// </summary>
public class OpenGLWorldRenderer : EginRenderer {
    public OpenGLWorldRenderer(OpenGLGfxModel gfx, object obj) {
    }
}

#endregion

#region OpenGLTestTriRenderer

/// <summary>
/// OpenGLTestTriRenderer
/// </summary>
public class OpenGLTestTriRenderer : EginRenderer {
    readonly OpenGLGfxModel Gfx;
    readonly Shader Shader;
    readonly object ShaderTag;
    readonly int Vao;
    public AABB BoundingBox => new(-1f, -1f, -1f, 1f, 1f, 1f);

    public OpenGLTestTriRenderer(OpenGLGfxModel gfx, object obj) {
        Gfx = gfx;
        (Shader, ShaderTag) = Gfx.ShaderManager.CreateShader("testtri");
        Vao = SetupVao();
    }

    int SetupVao() {
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

    public override void Render(Camera camera, Pass pass) {
        GL.UseProgram(Shader.Program);
        GL.BindVertexArray(Vao);
        GL.EnableVertexAttribArray(0);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 6);
        GL.BindVertexArray(0); // unbind vao
        GL.UseProgram(0); // unbind program
    }
}

#endregion
