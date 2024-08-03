using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenStack.Gfx.Gl;
using OpenTK.Input;
using static OpenStack.Gfx.Gl.GLCamera;

namespace OpenStack.Gfx.Gl_Render
{
    /// <summary>
    /// TestGLMeshBuffers
    /// </summary>
    [TestClass]
    public class TestGLMeshBuffers : GLMeshBuffers
    {
        #region base
        public TestGLMeshBuffers() : base(null) { }
        #endregion

        [TestMethod]
        public void Test_Init()
        {
            //Assert.AreEqual(0, Pitch);
        }
        //[TestMethod]
        //public void Test_Event()
        //{
        //    Event(EventType.MouseEnter, null, null);
        //    Event(EventType.MouseLeave, null, null);
        //}
        //[TestMethod]
        //public void Test_SetViewport()
        //{
        //    SetViewport(0, 0, 100, 100);
        //}
    }
}
