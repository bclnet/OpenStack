using OpenStack.Graphics.OpenGL;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenStack.GraphicsTests")]

namespace OpenStack.Graphics.Vulken
{
    // TODO: Rollout to its own dll

    /// <summary>
    /// IVulkenGraphic
    /// </summary>
    public interface IVulkenGraphic : IOpenGraphicAny<object, object, GLRenderMaterial, int, Shader>
    {
    }
}