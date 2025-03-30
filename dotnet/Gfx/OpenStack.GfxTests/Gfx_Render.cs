using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;

namespace OpenStack.Gfx.Render;

/// <summary>
/// TestCamera
/// </summary>
[TestClass]
public class TestCamera : Camera
{
    #region base
    public TestCamera() => SetViewport(0, 0, 100, 100);
    public override void GfxViewport(int x, int y, int width, int height) { }
    #endregion

    [TestMethod]
    public void Test_Init()
    {
        Assert.AreEqual(-0.6154797f, Pitch);
        Assert.AreEqual(-2.3561945f, Yaw);
    }
    [TestMethod]
    public void Test_RecalculateMatrices()
    {
        RecalculateMatrices();
        Assert.AreEqual(-1.70710671f, ViewProjectionMatrix.M11);
        Assert.AreEqual(-0.985598564f, ViewProjectionMatrix.M12);
        Assert.AreEqual(-0.577364743f, ViewProjectionMatrix.M13);
        Assert.AreEqual(-0.5773503f, ViewProjectionMatrix.M14);
        Assert.AreEqual(0.732069254f, ViewProjectionMatrix.M43);
        Assert.AreEqual(1.7320509f, ViewProjectionMatrix.M44);
    }
    [TestMethod]
    public void Test_GetForwardVector()
    {
        var actual = GetForwardVector();
        Assert.AreEqual(-0.577350259f, actual.X);
        Assert.AreEqual(-0.577350259f, actual.Y);
        Assert.AreEqual(-0.577350259f, actual.Z);
    }
    [TestMethod]
    public void Test_GetRightVector()
    {
        var actual = GetRightVector();
        Assert.AreEqual(-0.707107f, actual.X);
        Assert.AreEqual(0.7071066f, actual.Y);
        Assert.AreEqual(0f, actual.Z);
    }
    [TestMethod]
    public void Test_SetViewport()
    {
        SetViewport(0, 0, 100, 100);
        Assert.AreEqual(1f, AspectRatio);
        Assert.AreEqual("<100, 100>", WindowSize.ToString());
        Assert.AreEqual(2.41421342f, ProjectionMatrix.M11);
    }
    [TestMethod]
    public void Test_GfxSetViewport()
    {
        GfxViewport(0, 0, 100, 100);
    }
    [TestMethod]
    public void Test_CopyFrom()
    {
        var otherCamera = new TestCamera();
        otherCamera.AspectRatio = .5f;
        // test
        CopyFrom(otherCamera);
        Assert.AreEqual(.5f, AspectRatio);
    }
    [TestMethod]
    public void Test_SetLocation()
    {
        SetLocation(new Vector3(1f, 1f, 1f));
        Assert.AreEqual("<1, 1, 1>", Location.ToString());
    }
    [TestMethod]
    public void Test_SetLocationPitchYaw()
    {
        SetLocationPitchYaw(new Vector3(1f, 1f, 1f), 2f, 3f);
        Assert.AreEqual("<1, 1, 1>", Location.ToString());
        Assert.AreEqual(2f, Pitch);
        Assert.AreEqual(3f, Yaw);
    }
    [TestMethod]
    public void Test_LookAt()
    {
        LookAt(new Vector3(.5f, .5f, .5f));
        Assert.AreEqual(-0.6154797f, Pitch);
        Assert.AreEqual(-2.3561945f, Yaw);
    }
    [TestMethod]
    public void Test_SetFromTransformMatrix()
    {
        SetFromTransformMatrix(Matrix4x4.Identity);
        Assert.AreEqual("<0, 0, 0>", Location.ToString());
        Assert.AreEqual(0f, Pitch);
        Assert.AreEqual(0f, Yaw);
    }
    [TestMethod]
    public void Test_SetScale()
    {
        SetScale(1f);
        Assert.AreEqual(1f, Scale);
    }
    [TestMethod]
    public void Test_Tick()
    {
        Tick(1);
    }
    [TestMethod]
    public void Test_ClampRotation()
    {
        ClampRotation();
    }
}
