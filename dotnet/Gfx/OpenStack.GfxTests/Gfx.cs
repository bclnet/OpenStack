using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenStack.Gfx;

/// <summary>
/// TestPlatformStats
/// </summary>
[TestClass]
public class TestPlatformStats
{
    [TestMethod]
    public void Test_Init()
    {
        Assert.AreEqual(0, GfxX.MaxTextureMaxAnisotropy);
    }
}
