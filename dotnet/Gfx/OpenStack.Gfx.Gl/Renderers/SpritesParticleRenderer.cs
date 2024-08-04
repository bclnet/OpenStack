using OpenStack.Gfx.Particles;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Gfx.Gl
{
    public class SpritesParticleRenderer : IParticleRenderer
    {
        const int VertexSize = 9;
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
        float[] RawVertices;
        QuadIndexBuffer QuadIndices;
        int VertexBufferHandle;

        public SpritesParticleRenderer(IDictionary<string, object> keyValues, IOpenGLGfx graphic)
        {
            (Shader, ShaderTag) = graphic.ShaderManager.CreateShader("vrf.particle.sprite");
            QuadIndices = graphic.QuadIndices;

            // The same quad is reused for all particles
            QuadVao = SetupQuadBuffer();

            string textureName = null;
            if (keyValues.ContainsKey("m_hTexture")) textureName = keyValues.Get<string>("m_hTexture");
            else if (keyValues.ContainsKey("m_vecTexturesInput"))
            {
                var textures = keyValues.GetArray("m_vecTexturesInput");
                if (textures.Length > 0) textureName = textures[0].Get<string>("m_hTexture");
            }
            if (textureName != null)
            {
                (Texture, TextureTag) = graphic.TextureManager.CreateTexture(textureName);
                if (TextureTag is IDictionary<string, object> info)
                    TextureSequences = info.Get<TextureSequences>("sequences");
            }
            else Texture = graphic.TextureManager.DefaultTexture;

            Additive = keyValues.Get<bool>("m_bAdditive");
            OverbrightFactor = keyValues.GetFloat("m_flOverbrightFactor", 1f);
            OrientationType = keyValues.GetInt64("m_nOrientationType");
            AnimationRate = keyValues.GetFloat("m_flAnimationRate", .1f);
        }

        int SetupQuadBuffer()
        {
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

        void EnsureSpaceForVertices(int count)
        {
            var numFloats = count * VertexSize;
            if (RawVertices == null) RawVertices = new float[numFloats];
            else if (RawVertices.Length < numFloats)
            {
                var nextSize = (count / 64 + 1) * 64 * VertexSize;
                Array.Resize(ref RawVertices, nextSize);
            }
        }

        void UpdateVertices(ParticleBag particleBag, Matrix4x4 modelViewMatrix)
        {
            var particles = particleBag.LiveParticles;

            // Create billboarding rotation (always facing camera)
            Matrix4x4.Decompose(modelViewMatrix, out _, out Quaternion modelViewRotation, out _);
            modelViewRotation = Quaternion.Inverse(modelViewRotation);
            var billboardMatrix = Matrix4x4.CreateFromQuaternion(modelViewRotation);

            // Update vertex buffer
            EnsureSpaceForVertices(particleBag.Count * 4);
            for (var i = 0; i < particleBag.Count; ++i)
            {
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
                for (var j = 0; j < 4; ++j)
                {
                    RawVertices[quadStart + VertexSize * j + 3] = particles[i].Color.X;
                    RawVertices[quadStart + VertexSize * j + 4] = particles[i].Color.Y;
                    RawVertices[quadStart + VertexSize * j + 5] = particles[i].Color.Z;
                    RawVertices[quadStart + VertexSize * j + 6] = particles[i].Alpha;
                }

                // UVs
                if (TextureSequences != null && TextureSequences.Count > 0 && TextureSequences[0].Frames.Count > 0)
                {
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
                else
                {
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

        public void Render(ParticleBag particleBag, Matrix4x4 viewProjectionMatrix, Matrix4x4 modelViewMatrix)
        {
            if (particleBag.Count == 0) return;

            // Update vertex buffer
            UpdateVertices(particleBag, modelViewMatrix);

            // Draw it
            GL.Enable(EnableCap.Blend);
            GL.UseProgram(Shader.Program);

            if (Additive) GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            else GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindVertexArray(QuadVao);
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

        public void SetRenderMode(string renderMode)
        {
            var parameters = new Dictionary<string, bool>();
            if (renderMode != null && Shader.RenderModes.Contains(renderMode)) parameters.Add($"renderMode_{renderMode}", true);
            //_shader = guiContext.ShaderLoader.LoadShader(ShaderName, parameters);
        }
    }
}
