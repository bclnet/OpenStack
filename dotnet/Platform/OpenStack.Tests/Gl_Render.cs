using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTK.Input;

namespace OpenStack.Gfx.OpenGL;

#region Camera

/// <summary>
/// TestGLCamera
/// </summary>
[TestClass]
public class TestGLCamera : GLCamera
{
    #region base
    public TestGLCamera() => SetViewport(0, 0, 100, 100);
    public override void HandleInput(MouseState mouseState, KeyboardState keyboardState) { }
    public override void GfxViewport(int x, int y, int width, int height) { }
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
    public override void GfxViewport(int x, int y, int width, int height) { }
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

#endregion

#region Model

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

#endregion

#region Scene

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

#endregion

#region Particle

#endregion