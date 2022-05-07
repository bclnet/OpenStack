using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Graphics.Renderer
{
    public class DrawCall
    {
        public int PrimitiveType { get; set; } //: PrimitiveType
        public Shader Shader { get; set; }
        //public uint BaseVertex { get; set; }
        //public uint VertexCount { get; set; }
        public uint StartIndex { get; set; }
        public int IndexCount { get; set; }
        //public uint InstanceIndex { get; set; }   //TODO
        //public uint InstanceCount { get; set; }   //TODO
        //public float UvDensity { get; set; }     //TODO
        //public string Flags { get; set; }        //TODO
        public Vector3 TintColor { get; set; } = Vector3.One;
        //public object Material { get; set; } //: Material
        public uint VertexArrayObject { get; set; }
        public (uint Id, uint Offset) VertexBuffer { get; set; }
        public int IndexType { get; set; } //: DrawElementsType
        public (uint Id, uint Offset) IndexBuffer { get; set; }

        public static bool IsCompressedNormalTangent(IDictionary<string, object> drawCall)
        {
            if (drawCall.TryGetValue("m_bUseCompressedNormalTangent", out var z)) return (bool)z;
            else if (drawCall.TryGetValue("m_nFlags", out z))
            {
                var flags = z;
                switch (flags)
                {
                    case string flagsString: return flagsString.Contains("MESH_DRAW_FLAGS_USE_COMPRESSED_NORMAL_TANGENT");
                    case long flagsLong: return (flagsLong & 2) == 2; // TODO: enum
                }
            }
            return false;
        }

    }

    public class DrawCall<T> : DrawCall
    {
        public T Material { get; set; } //: Material
    }
}
