using OpenStack.Gfx.Gl;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Gfx.Vk
{
    /// <summary>
    /// IVulkenGraphic
    /// </summary>
    public interface IVulkenGraphic : IOpenGraphicAny<object, object, GLRenderMaterial, int, Shader>
    {
    }
}