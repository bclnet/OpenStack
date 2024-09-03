using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenStack.Gfx.Gl;
using OpenTK.Input;

namespace OpenStack.Gfx.Gl_Camera
{
    /// <summary>
    /// TestGLCamera
    /// </summary>
    [TestClass]
    public class TestGLCamera : GLCamera
    {
        #region base
        public TestGLCamera() => SetViewport(0, 0, 100, 100);
        public override void HandleInput(MouseState mouseState, KeyboardState keyboardState) { }
        protected override void GfxSetViewport(int x, int y, int width, int height) { }
        #endregion

        [TestMethod]
        public void Test_Init()
        {
        }
        [TestMethod]
        public void Test_Event()
        {
            Event(EventType.MouseEnter, null, null);
            Event(EventType.MouseLeave, null, null);
        }
        [TestMethod]
        public void Test_SetViewport()
        {
            SetViewport(0, 0, 100, 100);
        }
    }

    /// <summary>
    /// TestGLDebugCamera
    /// </summary>
    [TestClass]
    public class TestGLDebugCamera : GLDebugCamera
    {
        #region base
        public TestGLDebugCamera()
        {
            HandleInput(new MouseState(), new KeyboardState());
            MouseOverRenderArea = true;
            SetViewport(0, 0, 100, 100);
        }
        protected override void GfxSetViewport(int x, int y, int width, int height) { }
        #endregion

        [TestMethod]
        public void Test_Init()
        {
        }
        [TestMethod]
        public void Test_Tick()
        {
            Tick(1);
        }
        [TestMethod]
        public void Test_HandleInput()
        {
            HandleInput(new MouseState(), new KeyboardState());
        }
        [TestMethod]
        public void Test_HandleInputTick()
        {
            HandleInputTick(1f);
        }
    }
}
