using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace OpenStack.Gfx.Texture;

/// <summary>
/// TestCamera
/// </summary>
[TestClass]
public class TestDdsHeader
{
    [TestMethod]
    public void Test_Verify()
    {
        var header = new DDS_HEADER
        {
            dwSize = 124,
            dwFlags = DDSD.WIDTH | DDSD.HEIGHT,
            dwCaps = DDSCAPS.TEXTURE,
            ddspf = new DDS_PIXELFORMAT { dwSize = 32, dwFourCC = 0 }
        };
        header.Verify();
    }
    [TestMethod]
    public void Test_Read()
    {
        var stream1 = new MemoryStream(Convert.FromBase64String("RERTIHwAAAAHEAAAZAAAAGQAAACIEwAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAARFhUMQAAAAAAAAAAAAAAAAAAAAAAAAAACBBAAAAAAAAAAAAAAAAAAAAAAAABAgM="));
        var stream2 = new MemoryStream(Convert.FromBase64String("RERTIHwAAAAHEAAAZAAAAGQAAACIEwAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAARFgxMAAAAAAAAAAAAAAAAAAAAAAAAAAACBBAAAAAAAAAAAAAAAAAAAAAAABIAAAAAwAAAAAAAAABAAAAAAAAAAECAw=="));
        // start:test
        var (a1_Header, a1_HeaderDxt10, a1_Format, a1_Bytes) = DDS_HEADER.Read(new BinaryReader(stream1));
        var (a2_Header, a2_HeaderDxt10, a2_Format, a2_Bytes) = DDS_HEADER.Read(new BinaryReader(stream2));
        // stop:test
        Assert.AreEqual(10000U, a1_Header.dwWidth * a1_Header.dwHeight);
        Assert.AreEqual(10000U, a2_Header.dwWidth * a2_Header.dwHeight);
        Assert.AreEqual("AQID", Convert.ToBase64String(a1_Bytes));
        Assert.AreEqual("AQID", Convert.ToBase64String(a2_Bytes));
    }
    [TestMethod]
    public void Test_Write()
    {
        var actual1 = new MemoryStream();
        var actual2 = new MemoryStream();
        // start:test
        DDS_HEADER.Write(new BinaryWriter(actual1), new DDS_HEADER
        {
            dwSize = DDS_HEADER.SizeOf,
            dwFlags = DDSD.HEADER_FLAGS_TEXTURE,
            dwHeight = 100,
            dwWidth = 100,
            dwMipMapCount = 1,
            dwCaps = DDSCAPS.SURFACE_FLAGS_TEXTURE | DDSCAPS.SURFACE_FLAGS_MIPMAP,
            ddspf = new DDS_PIXELFORMAT { dwSize = DDS_PIXELFORMAT.SizeOf, dwFlags = DDPF.FOURCC, dwFourCC = FourCC.DXT1 },
            dwPitchOrLinearSize = 100 * 100 / 2U,
        }, null, [1, 2, 3]);
        DDS_HEADER.Write(new BinaryWriter(actual2), new DDS_HEADER
        {
            dwSize = DDS_HEADER.SizeOf,
            dwFlags = DDSD.HEADER_FLAGS_TEXTURE,
            dwHeight = 100,
            dwWidth = 100,
            dwMipMapCount = 1,
            dwCaps = DDSCAPS.SURFACE_FLAGS_TEXTURE | DDSCAPS.SURFACE_FLAGS_MIPMAP,
            ddspf = new DDS_PIXELFORMAT { dwSize = DDS_PIXELFORMAT.SizeOf, dwFlags = DDPF.FOURCC, dwFourCC = FourCC.DX10 },
            dwPitchOrLinearSize = 100 * 100 / 2U,
        }, new DDS_HEADER_DXT10
        {
            dxgiFormat = DXGI_FORMAT.BC1_UNORM_SRGB,
            resourceDimension = D3D10_RESOURCE_DIMENSION.TEXTURE2D,
            miscFlag = 0,
            arraySize = 1,
            miscFlags2 = (uint)DDS_ALPHA_MODE.ALPHA_MODE_UNKNOWN,
        }, [1, 2, 3]);
        // stop:test
        Assert.AreEqual("RERTIHwAAAAHEAAAZAAAAGQAAACIEwAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAARFhUMQAAAAAAAAAAAAAAAAAAAAAAAAAACBBAAAAAAAAAAAAAAAAAAAAAAAABAgM=", Convert.ToBase64String(actual1.ToArray()));
        Assert.AreEqual("RERTIHwAAAAHEAAAZAAAAGQAAACIEwAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAARFgxMAAAAAAAAAAAAAAAAAAAAAAAAAAACBBAAAAAAAAAAAAAAAAAAAAAAABIAAAAAwAAAAAAAAABAAAAAAAAAAECAw==", Convert.ToBase64String(actual2.ToArray()));
    }
    [TestMethod]
    public void Test_ConvertDxt3ToDtx5()
    {
    }
}
