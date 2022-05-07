using OpenStack.Graphics.Renderer;
using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Graphics.Renderer
{
    /// <summary>
    /// IMeshInfo
    /// </summary>
    public interface IMeshInfo
    {
        IDictionary<string, object> Data { get; }

        IVBIB VBIB { get; }

        Vector3 MinBounds { get; }
        Vector3 MaxBounds { get; }
    }
}