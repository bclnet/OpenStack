using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenStack.Gl;

#region Shaders

/// <summary>
/// TestShaderLoader
/// </summary>
[TestClass]
public class TestShaderLoader : ShaderLoader
{
    #region base
    //public TestShaderLoader() : base(null) { }
    protected override string GetShaderFileByName(string name) => "";
    protected override string GetShaderSource(string name) => "";
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
/// TestShaderDebugLoader
/// </summary>
[TestClass]
public class TestShaderDebugLoader : ShaderDebugLoader
{
    #region base
    //public TestShaderDebugLoader() : base(null) { }
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
