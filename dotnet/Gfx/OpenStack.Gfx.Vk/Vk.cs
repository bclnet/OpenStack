using OpenStack.Gfx.Gl.Renders;
using OpenStack.Gfx.Renders;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Gfx.Vk
{
    /// <summary>
    /// IVulkenGraphic
    /// </summary>
    public interface IVulkenGfx : IOpenGfxAny<object, GLRenderMaterial, int, Shader>
    {
    }
}