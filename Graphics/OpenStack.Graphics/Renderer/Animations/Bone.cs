using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Graphics.Renderer1.Animations
{
    public class Bone
    {
        public int Index { get; }
        public Bone Parent { get; private set; }
        public List<Bone> Children { get; } = new List<Bone>();

        public string Name { get; }

        public Vector3 Position { get; }
        public Quaternion Angle { get; }

        public Matrix4x4 BindPose { get; }
        public Matrix4x4 InverseBindPose { get; }

        public Bone(int index, string name, Vector3 position, Quaternion rotation)
        {
            Index = index;
            Name = name;

            Position = position;
            Angle = rotation;

            // Calculate matrices
            BindPose = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);

            Matrix4x4.Invert(BindPose, out var inverseBindPose);
            InverseBindPose = inverseBindPose;
        }

        public void SetParent(Bone parent)
        {
            if (!Children.Contains(parent))
            {
                Parent = parent;
                parent.Children.Add(this);
            }
        }
    }
}
