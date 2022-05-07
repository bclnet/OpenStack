using OpenStack.Graphics.Renderer;
using OpenTK.Graphics.OpenGL;

namespace OpenStack.Graphics.OpenGL
{
    public abstract class GLCamera : Camera
    {
        protected override void SetViewport(int x, int y, int width, int height)
            => GL.Viewport(0, 0, width, height);
    }
}
