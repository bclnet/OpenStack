using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenStack.Gfx.Gl.Scenes
{
    /// <summary>
    /// TestOctreeDebugRenderer
    /// </summary>
    [TestClass]
    public class TestOctreeDebugRenderer : OctreeDebugRenderer<object>
    {
        #region base
        public TestOctreeDebugRenderer() : base(null, null, false) { }
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
