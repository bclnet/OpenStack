using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

namespace OpenStack.Gfx.Gfx_Bitmap
{
    /// <summary>
    /// TestDirectBitmap
    /// </summary>
    [TestClass]
    public class TestDirectBitmap : DirectBitmap
    {
        public TestDirectBitmap() : base(100, 100) { }

        [TestMethod]
        public void Test_Init()
        {
            Assert.AreEqual(100, Width);
            Assert.AreEqual(100, Height);
        }
        [TestMethod]
        public void Test_SetPixel()
        {
            SetPixel(0, 0, Color.Aqua);
        }
        [TestMethod]
        public void Test_GetPixel()
        {
            var actual = GetPixel(0, 0);
            Assert.AreEqual(0, actual.R);
            Assert.AreEqual(0, actual.G);
            Assert.AreEqual(0, actual.B);
            Assert.AreEqual(0, actual.A);
        }
        [TestMethod]
        public void Test_Save()
        {
            Save("path");
        }
    }
}
