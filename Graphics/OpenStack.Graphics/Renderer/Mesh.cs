using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OpenStack.Graphics.Renderer
{
    public interface IMeshCollection
    {
        IEnumerable<Mesh> Meshes { get; }
    }

    public abstract class Mesh
    {
        public AABB BoundingBox { get; protected set; }
        public Vector4 Tint { get; set; } = Vector4.One;

        public List<DrawCall> DrawCallsOpaque { get; } = new List<DrawCall>();
        public List<DrawCall> DrawCallsBlended { get; } = new List<DrawCall>();

        public int? AnimationTexture { get; private set; }
        public int AnimationTextureSize { get; private set; }

        public float Time { get; private set; } = 0f;

        public IEnumerable<string> GetSupportedRenderModes() => DrawCallsOpaque
            .SelectMany(drawCall => drawCall.Shader.RenderModes)
            .Union(DrawCallsBlended.SelectMany(drawCall => drawCall.Shader.RenderModes))
            .Distinct();

        public abstract void SetRenderMode(string renderMode);

        public void SetAnimationTexture(int? texture, int animationTextureSize)
        {
            AnimationTexture = texture;
            AnimationTextureSize = animationTextureSize;
        }

        public void Update(float timeStep)
            => Time += timeStep;
    }
}
