using OpenStack.Graphics.Renderer;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace OpenStack.Graphics.OpenGL
{
    public class Material
    {
        public Shader Shader { get; private set; }
        public IMaterialInfo Info { get; private set; }
        public Dictionary<string, int> Textures { get; } = new Dictionary<string, int>();
        public bool IsBlended => _isTranslucent;

        float _flAlphaTestReference;
        bool _isTranslucent;
        bool _isAdditiveBlend;
        bool _isRenderBackfaces;

        public Material(IMaterialInfo info)
        {
            Info = info;
            switch (info)
            {
                case IFixedMaterialInfo p:
                    break;
                case IParamMaterialInfo p:
                    if (p.IntParams.ContainsKey("F_ALPHA_TEST") && p.IntParams["F_ALPHA_TEST"] == 1 && p.FloatParams.ContainsKey("g_flAlphaTestReference"))
                        _flAlphaTestReference = p.FloatParams["g_flAlphaTestReference"];
                    _isTranslucent = (p.IntParams.ContainsKey("F_TRANSLUCENT") && p.IntParams["F_TRANSLUCENT"] == 1) || p.IntAttributes.ContainsKey("mapbuilder.water");
                    _isAdditiveBlend = p.IntParams.ContainsKey("F_ADDITIVE_BLEND") && p.IntParams["F_ADDITIVE_BLEND"] == 1;
                    _isRenderBackfaces = p.IntParams.ContainsKey("F_RENDER_BACKFACES") && p.IntParams["F_RENDER_BACKFACES"] == 1;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(info));
            }
        }

        public void Render(Shader shader)
        {
            Shader = shader;

            // Start at 1, texture unit 0 is reserved for the animation texture
            var textureUnit = 1;
            int uniformLocation;
            foreach (var texture in Textures)
            {
                uniformLocation = Shader.GetUniformLocation(texture.Key);
                if (uniformLocation > -1)
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                    GL.BindTexture(TextureTarget.Texture2D, texture.Value);
                    GL.Uniform1(uniformLocation, textureUnit);
                    textureUnit++;
                }
            }

            switch (Info)
            {
                case IParamMaterialInfo p:
                    foreach (var param in p.FloatParams)
                    {
                        uniformLocation = Shader.GetUniformLocation(param.Key);
                        if (uniformLocation > -1)
                            GL.Uniform1(uniformLocation, param.Value);
                    }

                    foreach (var param in p.VectorParams)
                    {
                        uniformLocation = Shader.GetUniformLocation(param.Key);
                        if (uniformLocation > -1)
                            GL.Uniform4(uniformLocation, new Vector4(param.Value.X, param.Value.Y, param.Value.Z, param.Value.W));
                    }
                    break;
            }

            var alphaReference = Shader.GetUniformLocation("g_flAlphaTestReference");
            if (alphaReference > -1) GL.Uniform1(alphaReference, _flAlphaTestReference);

            if (_isTranslucent)
            {
                GL.DepthMask(false);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, _isAdditiveBlend ? BlendingFactor.One : BlendingFactor.OneMinusSrcAlpha);
            }

            if (_isRenderBackfaces) GL.Disable(EnableCap.CullFace);
        }

        public void PostRender()
        {
            if (_isTranslucent) { GL.DepthMask(true); GL.Disable(EnableCap.Blend); }
            if (_isRenderBackfaces) GL.Enable(EnableCap.CullFace);
        }
    }
}
