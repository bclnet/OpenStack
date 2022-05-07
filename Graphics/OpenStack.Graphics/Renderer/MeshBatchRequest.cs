using System.Numerics;

namespace OpenStack.Graphics.Renderer
{
    public struct MeshBatchRequest
    {
        public Matrix4x4 Transform;
        public Mesh Mesh;
        public DrawCall Call;
        public float DistanceFromCamera;
    }
}
