using OpenStack.Gfx.Particles;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Gfx.Gl
{
    public class TrailsParticleRenderer : IParticleRenderer
    {
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

        public TrailsParticleRenderer(IDictionary<string, object> keyValues, IOpenGLGfx graphic)
        {
            (Shader, ShaderTag) = graphic.ShaderManager.CreateShader("vrf.particle.trail", new Dictionary<string, bool>());

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

            FinalTextureScaleU = keyValues.GetFloat("m_flFinalTextureScaleU", 1f);
            FinalTextureScaleV = keyValues.GetFloat("m_flFinalTextureScaleV", 1f);

            MaxLength = keyValues.GetFloat("m_flMaxLength", 2000f);
            LengthFadeInTime = keyValues.GetFloat("m_flLengthFadeInTime");
        }

        int SetupQuadBuffer()
        {
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

        public void Render(ParticleBag particleBag, Matrix4x4 viewProjectionMatrix, Matrix4x4 modelViewMatrix)
        {
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

            for (var i = 0; i < particles.Length; ++i)
            {
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

                if (TextureSequences != null && TextureSequences.Count > 0 && TextureSequences[0].Frames.Count > 0)
                {
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
                else
                {
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

        public void SetRenderMode(string renderMode)
        {
            var parameters = new Dictionary<string, bool>();
            if (renderMode != null && Shader.RenderModes.Contains(renderMode)) parameters.Add($"renderMode_{renderMode}", true);
            //_shader = graphic.LoadShader(ShaderName, parameters);
        }
    }
}
