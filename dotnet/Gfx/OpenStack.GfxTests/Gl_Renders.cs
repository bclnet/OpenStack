using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenStack.Gfx.Gl;
using OpenTK.Input;
using static OpenStack.Gfx.Gl.GLCamera;

namespace OpenStack.Gfx.Gl_Renders
{
    /// <summary>
    /// TestTextureRenderer
    /// </summary>
    [TestClass]
    public class TestTextureRenderer : TextureRenderer
    {
        #region base
        public TestTextureRenderer() : base(null, 0, false) { }
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

    /// <summary>
    /// TestMaterialRenderer
    /// </summary>
    [TestClass]
    public class TestMaterialRenderer : MaterialRenderer
    {
        #region base
        public TestMaterialRenderer() : base(null, null) { }
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

    /// <summary>
    /// TestParticleGridRenderer
    /// </summary>
    [TestClass]
    public class TestParticleGridRenderer : ParticleGridRenderer
    {
        #region base
        public TestParticleGridRenderer() : base(null, 1f, 1) { }
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
