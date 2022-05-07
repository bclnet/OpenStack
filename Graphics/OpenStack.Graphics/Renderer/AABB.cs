using System.Numerics;

namespace OpenStack.Graphics.Renderer
{
    public struct AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public Vector3 Size
            => Max - Min;

        public Vector3 Center
            => (Min + Max) * 0.5f;

        public AABB(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public AABB(float min_x, float min_y, float min_z, float max_x, float max_y, float max_z)
        {
            Min = new Vector3(min_x, min_y, min_z);
            Max = new Vector3(max_x, max_y, max_z);
        }

        public bool Contains(Vector3 point)
            => point.X >= Min.X && point.X < Max.X &&
            point.Y >= Min.Y && point.Y < Max.Y &&
            point.Z >= Min.Z && point.Z < Max.Z;

        public bool Intersects(AABB other)
            => other.Max.X >= Min.X && other.Min.X < Max.X &&
            other.Max.Y >= Min.Y && other.Min.Y < Max.Y &&
            other.Max.Z >= Min.Z && other.Min.Z < Max.Z;

        public bool Contains(AABB other)
            => other.Min.X >= Min.X && other.Max.X <= Max.X &&
            other.Min.Y >= Min.Y && other.Max.Y <= Max.Y &&
            other.Min.Z >= Min.Z && other.Max.Z <= Max.Z;

        public AABB Union(AABB other)
            => new AABB(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

        public AABB Translate(Vector3 offset)
            => new AABB(Min + offset, Max + offset);

        // Note: Since we're dealing with AABBs here, the resulting AABB is likely to be bigger than the original if rotation
        // and whatnot is involved. This problem compounds with multiple transformations. Therefore, endeavour to premultiply matrices
        // and only use this at the last step.
        public AABB Transform(Matrix4x4 transform)
        {
            var points = new[]
            {
                Vector4.Transform(new Vector4(Min.X, Min.Y, Min.Z, 1.0f), transform),
                Vector4.Transform(new Vector4(Max.X, Min.Y, Min.Z, 1.0f), transform),
                Vector4.Transform(new Vector4(Max.X, Max.Y, Min.Z, 1.0f), transform),
                Vector4.Transform(new Vector4(Min.X, Max.Y, Min.Z, 1.0f), transform),
                Vector4.Transform(new Vector4(Min.X, Max.Y, Max.Z, 1.0f), transform),
                Vector4.Transform(new Vector4(Min.X, Min.Y, Max.Z, 1.0f), transform),
                Vector4.Transform(new Vector4(Max.X, Min.Y, Max.Z, 1.0f), transform),
                Vector4.Transform(new Vector4(Max.X, Max.Y, Max.Z, 1.0f), transform),
            };
            var min = points[0];
            var max = points[0];
            for (var i = 1; i < points.Length; ++i)
            {
                min = Vector4.Min(min, points[i]);
                max = Vector4.Max(max, points[i]);
            }
            return new AABB(new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, max.Y, max.Z));
        }

        public override string ToString() =>
            $"AABB [({Min.X},{Min.Y},{Min.Z}) -> ({Max.X},{Max.Y},{Max.Z}))";
    }
}
