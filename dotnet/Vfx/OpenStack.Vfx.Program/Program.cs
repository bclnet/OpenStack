using OpenStack.Vfx.N64;
using OpenStack.Vfx.X3ds;
using System.IO;
using static System.Console;

namespace OpenStack.Vfx;

public class Program {

    public static void Main() {
        Pass0();
        //Pass1();
        //Pass2();
        //Pass3();
    }

    public static void Pass0() {
        var s = new DirectoryFileSystem(@"E:\ArchiveLibrary\Rockstar\Monster Truck Madness 64 (USA).7z", null).Next();
        //var abc = new SevenZipFileSystem(@"E:\ArchiveLibrary\Rockstar\Austin Powers - Oh, Behave! (USA).7z", null);
        //var s = new SevenZipFileSystem(null, @"E:\ArchiveLibrary\Rockstar\Monster Truck Madness 64 (USA).7z", null);
        //WriteLine(s.Glob("", null));
        //WriteLine(s.FileExists("Monster Truck Madness 64 (USA).z64"));
        //WriteLine(s.FileInfo("Monster Truck Madness 64 (USA).z64"));
        //var src = new MemoryStream();
        //s.Open("Monster Truck Madness 64 (USA).z64", null).CopyTo(src);
        //src.Position = 0;
        var src = s.Open("Monster Truck Madness 64 (USA).z64", null);
        var abc = N64Rom.Open(src);
    }

    // 3dstool -xvt0f 3ds 0.cxi "Legend of Zelda, The - Tri Force Heroes (USA) (En,Fr,Es).3ds"
    public static bool Pass1() {
        var path = @"E:\ArchiveLibrary\Nintendo+EPD\.Legend of Zelda, The - Tri Force Heroes (USA) (En,Fr,Es).zip\Legend of Zelda, The - Tri Force Heroes (USA) (En,Fr,Es).3ds";
        using var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        WriteLine(Ncsd.IsNcsdFile(s));
        var x = new Ncsd();
        //x.FileName = null;
        x.NcchFileName[0] = "0.cxi";
        x.Verbose = true;
        return x.ExtractFile(s);
    }

    // 3dstool -xvtf cxi 0.cxi --header ncchheader.bin --exh exh.bin --logo logo.bcma.lz --plain plain.bin --exefs exefs.bin --romfs romfs.bin
    public static bool Pass2() {
        var path = @"C:\_GITHUB\bclnet\GameX\OpenStack\dotnet\Base\OpenStack.Program\bin\Debug\net9.0\0.cxi";
        using var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        WriteLine(Ncch.IsCxiFile(s));

        var x = new Ncch();
        //x.FileName = null;
        x.Verbose = true;
        x.EncryptMode = Ncch.kEncryptMode.Auto;
        x.Dev = false;
        x.HeaderFileName = "ncchheader.bin";
        x.ExtendedHeaderFileName = "exh.bin";
        x.LogoRegionFileName = "logo.bcma.lz";
        x.PlainRegionFileName = "plain.bin";
        x.ExeFsFileName = "exefs.bin";
        x.RomFsFileName = "romfs.bin";
        return x.ExtractFile(s);
    }

    // 3dstool -xvtf romfs romfs.bin --romfs-dir rom
    public static bool Pass3() {
        var path = @"C:\_GITHUB\bclnet\GameX\OpenStack\dotnet\Base\OpenStack.Program\bin\Debug\net9.0\romfs.bin";
        using var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        WriteLine(RomFs.IsRomFsFile(s));

        var x = new RomFs();
        //x.FileName = null;
        x.Verbose = true;
        x.RomFsDirName = "rom";
        return x.ExtractFile(s);
    }
}
