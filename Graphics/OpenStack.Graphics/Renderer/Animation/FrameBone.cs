using System.Numerics;

namespace OpenStack.Graphics.Renderer.Animation
{
    public struct FrameBone
    {
        public Vector3 Position { get; set; }
        public Quaternion Angle { get; set; }
        public float Scale { get; set; }
    }
}
