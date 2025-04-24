using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenStack.Gfx.OpenGL;

/// <summary>
/// TestTextureRenderer
/// </summary>
[TestClass]
public class TestTextureRenderer() : OpenGLTextureRenderer(null, 0, default, default) {
    [TestMethod]
    public void Test_Init() {
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
public class TestMaterialRenderer() : OpenGLMaterialRenderer(null, null) {
    [TestMethod]
    public void Test_Init() {
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
/// TestGridRenderer
/// </summary>
[TestClass]
public class TestGridRenderer() : OpenGLGridRenderer(null, 1f, 1) {
    [TestMethod]
    public void Test_Init() {
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
