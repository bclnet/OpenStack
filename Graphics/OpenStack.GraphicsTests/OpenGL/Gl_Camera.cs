using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTK.Input;

namespace OpenStack.Graphics.OpenGL
{
    /// <summary>
    /// TestGLCamera
    /// </summary>
    [TestClass]
    public class TestGLCamera : GLCamera
    {
        #region base
        public override void Tick(float deltaTime) { }
        protected override void SetViewport(int x, int y, int width, int height) { }
        #endregion

        [TestMethod]
        public void Test_Init()
        {
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
        protected override void SetViewport(int x, int y, int width, int height) { }
        #endregion

        [TestMethod]
        public void Test_Init()
        {
        }
        [TestMethod]
        public void Test_Tick()
        {
            Tick(1f);
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
