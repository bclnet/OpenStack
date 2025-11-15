using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static OpenStack.Debug;
using static System.Buffers.Binary.BinaryPrimitives;

namespace OpenStack.Vfx.N64;

#region FileSystem : N64

/// <summary>
/// N64FileSystem
/// </summary>
public class N64FileSystem : FileSystem {
    public N64FileSystem(FileSystem vfx, string path, string basePath) {
        var disc = new N64Rom(vfx, path);
        Log("N64FileSystem");
    }

    public override bool FileExists(string path) => throw new NotImplementedException();
    public override (string path, long length) FileInfo(string path) => throw new NotImplementedException();
    public override IEnumerable<string> Glob(string path, string searchPattern) => [];
    public override Stream Open(string path, string mode) => throw new NotImplementedException();
}

#endregion

#region N64Rom

unsafe class N64Rom {
    #region Headers

    [StructLayout(LayoutKind.Sequential)]
    public struct RomHeader {
        public byte Init_PI_BSB_DOM1_LAT_REG;   /* 0x00 */
        public byte Init_PI_BSB_DOM1_PGS_REG;   /* 0x01 */
        public byte Init_PI_BSB_DOM1_PWD_REG;   /* 0x02 */
        public byte Init_PI_BSB_DOM1_PGS_REG2;  /* 0x03 */
        public uint ClockRate;                  /* 0x04 */
        public uint PC;                         /* 0x08 */
        public uint Release;                    /* 0x0C */
        public uint CRC1;                       /* 0x10 */
        public uint CRC2;                       /* 0x14 */
        public fixed uint Unknown[2];           /* 0x18 */
        public fixed sbyte Name[20];            /* 0x20 */
        public uint Unknown2;                   /* 0x34 */
        public uint ManufacturerID;             /* 0x38 */
        public ushort CartridgeID;              /* 0x3C - Game serial number  */
        public byte CountryCode;                /* 0x3E */
        public byte Version;                    /* 0x3F */
    }

    public enum IMAGE { Z64, V64, N64 }
    public enum SYSTEM { NTSC, PAL, MPAL }

    const uint Z64_MAGIC = 0x40123780; //0x80371240;
    const uint V64_MAGIC = 0x12408037; //0x37804012;
    const uint N64_MAGIC = 0x90371240; //0x40123780;

    #endregion

    //public bool WordsLittleEndian;
    public int RomSize;
    public byte[] Image;
    public IMAGE ImageType;
    public SYSTEM SystemType;
    public RomHeader Header;
    public string Name;
    public byte[] Md5;
    const bool Verbose = false;

    public N64Rom(FileSystem vfx, string path) {
        using var src = vfx.Open(path);
        byte[] image; using (var s = new MemoryStream()) { src.CopyTo(s); s.Position = 0; image = s.ToArray(); }
        fixed (byte* _image = image) {
            var size = RomSize = image.Length;
            if (!IsValidRom(_image, size)) throw new Exception("not a valid ROM image");
            var newImage = Image = new byte[size]; // allocate new buffer for ROM and copy into this buffer
            fixed (byte* _newImage = newImage) {
                SwapCopyRom(_newImage, _image, size, out ImageType); // ROM is now in N64 native (big endian) byte order
                Header = Marshal.PtrToStructure<RomHeader>((IntPtr)_newImage);
            }
            using var md5 = System.Security.Cryptography.MD5.Create();
            Md5 = md5.ComputeHash(newImage);
        }
        SystemType = CountryCodeToSystemType(Header.CountryCode);
        Header.Name[20] = (sbyte)'\0';
        fixed (sbyte* _name = Header.Name) Name = new string(_name).Trim();
        // display
        Log($"Name: {Name}");
        Log($"MD5: {Util.ToHexString(Md5)}");
        Log($"CRC: {ReverseEndianness(Header.CRC1):X08} {ReverseEndianness(Header.CRC2):X08}");
        Log($"Imagetype: {ImageToString(ImageType)}");
        Log($"Rom size: {RomSize} bytes (or {RomSize / 1024 / 1024} Mb or {RomSize / 1024 / 1024 * 8} Megabits)");
        if (Verbose) Log($"ClockRate = {ReverseEndianness(Header.ClockRate):Center}");
        Log($"Version: {ReverseEndianness(Header.Release):Center}");
        Log($"Manufacturer: {(ReverseEndianness(Header.ManufacturerID) == (byte)'N' ? "Nintendo" : ReverseEndianness(Header.ManufacturerID))}");
        if (Verbose) Log($"CartridgeID: {ReverseEndianness(Header.CartridgeID)}");
        Log($"Country: {CountryCodeToString(ReverseEndianness(Header.CountryCode))}");
        if (Verbose) Log($"PC = {ReverseEndianness(Header.PC)}");
    }

    static bool IsValidRom(byte* src, int size) {
        var magic = *(uint*)src;
        return (magic == Z64_MAGIC) || (magic == V64_MAGIC && size % 2 == 0) || (magic == N64_MAGIC && size % 4 == 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ushort m64p_swap16(ushort x) => (ushort)(
        ((x & 0x00FF) << 8) |
        ((x & 0xFF00) >> 8));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint m64p_swap32(uint x) =>
        ((x & 0x000000FF) << 24) |
        ((x & 0x0000FF00) << 8) |
        ((x & 0x00FF0000) >> 8) |
        ((x & 0xFF000000) >> 24);
    static void SwapCopyRom(void* dst, void* src, int len, out IMAGE image) {
        var magic = *(uint*)src;
        if (magic == V64_MAGIC) {
            ushort* src16 = (ushort*)src, dst16 = (ushort*)dst;
            image = IMAGE.V64;
            // .v64 images have byte-swapped half-words (16-bit)
            for (var i = 0; i < len; i += 2) *dst16++ = m64p_swap16(*src16++);
        }
        else if (magic == N64_MAGIC) {
            uint* src32 = (uint*)src, dst32 = (uint*)dst;
            image = IMAGE.N64;
            // .n64 images have byte-swapped words (32-bit)
            for (var i = 0; i < len; i += 4) *dst32++ = m64p_swap32(*src32++);
        }
        else {
            image = IMAGE.Z64;
            Unsafe.CopyBlock(dst, src, (uint)len);
        }
    }

    static SYSTEM CountryCodeToSystemType(ushort countryCode) => countryCode switch {
        0x44 or 0x46 or 0x49 or 0x50 or 0x53 or 0x55 or 0x58 or 0x59 => SYSTEM.PAL, // PAL codes
        0x37 or 0x41 or 0x45 or 0x4a => SYSTEM.NTSC, // NTSC codes
        _ => SYSTEM.NTSC, // Fallback for unknown codes
    };

    static string CountryCodeToString(ushort countryCode) => countryCode switch {
        0 => "Demo",
        '7' => "Beta",
        0x41 => "USA/Japan",
        0x44 => "Germany",
        0x45 => "USA",
        0x46 => "France",
        'I' => "Italy",
        0x4A => "Japan",
        'S' => "Spain",
        0x55 or 0x59 => $"Australia (0x{countryCode:X02}",
        0x50 or 0x58 or 0x20 or 0x21 or 0x38 or 0x70 => $"Europe (0x{countryCode:X02}",
        _ => $"Unknown (0x{countryCode:X02}",
    };

    static string ImageToString(IMAGE imageType) => imageType switch {
        IMAGE.Z64 => ".z64 (native)",
        IMAGE.V64 => ".v64 (byteswapped)",
        IMAGE.N64 => ".n64 (wordswapped)",
        _ => "",
    };

    
}

#endregion