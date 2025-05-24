using OpenStack.ExtServices;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static OpenStack.Debug;
using static OpenStack.Vfx.Disc.CueFormat;

namespace OpenStack.Vfx.Disc;

#region FileSystem : Cue

/// <summary>
/// DiscFileSystem
/// </summary>
public class DiscFileSystem : FileSystem {
    public DiscFileSystem(FileSystem vfx, string path, string basePath) {
        var disc = new DiscMount(vfx, path);
        Log("DiscFileSystem");
    }

    public override IEnumerable<string> Glob(string path, string searchPattern) {
        var matcher = CreateMatcher(searchPattern);
        return [];
    }
    //public bool FileExists(string path) => Pak.GetEntry(path) != null;
    //public (string path, long length) FileInfo(string path) { var x = Pak.GetEntry(path); return x != null ? (x.Name, x.Length) : (null, 0); }
    //public BinaryReader OpenReader(string path) => new(Pak.GetEntry(path).Open());
    //public BinaryWriter OpenWriter(string path) => throw new NotSupportedException();
    public override bool FileExists(string path) => throw new NotImplementedException();
    public override (string path, long length) FileInfo(string path) => throw new NotImplementedException();
    public override Stream Open(string path, string mode) => throw new NotImplementedException();
}

#endregion

#region Blob

abstract class Blob : IDisposable {
    public virtual void Dispose() { }
    public abstract int Read(long byte_pos, byte[] buffer, int offset, int count);
}

class Blob_CHD(FileSystem vfx, IntPtr chdFile, uint hunkSize) : Blob {
    IntPtr _chdFile = chdFile;
    readonly uint _hunkSize = hunkSize;
    readonly byte[] _hunkCache = new byte[hunkSize];
    int _currentHunk = -1;

    public override void Dispose() {
        if (_chdFile != IntPtr.Zero) { LibChd.chd_close(_chdFile); _chdFile = IntPtr.Zero; }
    }

    public override int Read(long byte_pos, byte[] buffer, int offset, int count) {
        var ret = count;
        while (count > 0) {
            var targetHunk = (uint)(byte_pos / _hunkSize);
            if (targetHunk != _currentHunk) {
                var err = LibChd.chd_read(_chdFile, targetHunk, _hunkCache);
                if (err != LibChd.chd_error.CHDERR_NONE) throw new IOException($"CHD read failed with error {err}");
                _currentHunk = (int)targetHunk;
            }
            var hunkOffset = (uint)(byte_pos - targetHunk * _hunkSize);
            var bytesToCopy = Math.Min((int)(_hunkSize - hunkOffset), count);
            Buffer.BlockCopy(_hunkCache, (int)hunkOffset, buffer, offset, bytesToCopy);
            offset += bytesToCopy;
            count -= bytesToCopy;
        }
        return ret;
    }
}

#region ECM

static class ECM {
    static readonly uint[] edc_table = new uint[256];
    static readonly byte[] mul2tab = new byte[256];
    static readonly byte[] div3tab = new byte[256];

    static ECM() {
        // edc
        var reverse_edc_poly = BitReverseX.Reverse32(0x8001801BU);
        for (var i = 0U; i < 256U; ++i) {
            var crc = i;
            for (var j = 8; j > 0; --j) { if ((crc & 1) == 1) crc = (crc >> 1) ^ reverse_edc_poly; else crc >>= 1; }
            edc_table[i] = crc;
        }
        // ecc
        for (var i = 0; i < 256; i++) { int n = i * 2, b = n & 0xFF; if (n > 0xFF) b ^= 0x1D; mul2tab[i] = (byte)b; }
        for (var i = 0; i < 256; i++) { byte x1 = (byte)i, x2 = mul2tab[i], x3 = (byte)(x2 ^ x1); div3tab[x3] = x1; }
    }

    /// <summary>
    /// Calculates ECC parity values for the specified data
    /// see annex A of yellowbook
    /// </summary>
    public static void CalcECC(byte[] data, int base_offset, int addr_offset, int addr_add, int todo, out byte p0, out byte p1) {
        byte pow_accum = 0, add_accum = 0;
        for (var i = 0; i < todo; i++) {
            addr_offset %= (1118 * 2);
            var d = data[base_offset + addr_offset];
            addr_offset += addr_add;
            add_accum ^= d;
            pow_accum ^= d;
            pow_accum = mul2tab[pow_accum];
        }
        p0 = div3tab[mul2tab[pow_accum] ^ add_accum];
        p1 = (byte)(p0 ^ add_accum);
    }

    /// <summary>
    /// handy for stashing the EDC somewhere with little endian
    /// </summary>
    public static void PokeUint(byte[] data, int offset, uint value) {
        data[offset + 0] = (byte)((value >> 0) & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    /// <summary>
    /// calculates EDC checksum for the range of data provided
    /// see section 14.3 of yellowbook
    /// </summary>
    public static uint EDC_Calc(byte[] data, int offset, int length) {
        var crc = 0U;
        for (var i = 0; i < length; i++) {
            var b = data[offset + i];
            var entry = ((int)crc ^ b) & 0xFF;
            crc = edc_table[entry] ^ (crc >> 8);
        }
        return crc;
    }

    /// <summary>
    /// returns the address from a sector. useful for saving it before zeroing it for ECC calculations
    /// </summary>
    static uint GetSectorAddress(byte[] sector, int sectorOffset) => (uint)(
        (sector[sectorOffset + 12 + 0] << 0) |
        (sector[sectorOffset + 12 + 1] << 8) |
        (sector[sectorOffset + 12 + 2] << 16));

    /// <summary>
    /// sets the address for a sector. useful for restoring it after zeroing it for ECC calculations
    /// </summary>
    static void SetSectorAddress(byte[] sector, int sectorOffset, uint address) {
        sector[sectorOffset + 12 + 0] = (byte)((address >> 0) & 0xFF);
        sector[sectorOffset + 12 + 1] = (byte)((address >> 8) & 0xFF);
        sector[sectorOffset + 12 + 2] = (byte)((address >> 16) & 0xFF);
    }

    /// <summary>
    /// populates a sector with valid ECC information.
    /// it is safe to supply the same array for sector and dest.
    /// </summary>
    public static void ECC_Populate(byte[] src, int srcOffset, byte[] dest, int destOffset, bool zeroSectorAddress) {
        var address = GetSectorAddress(src, srcOffset);
        if (zeroSectorAddress) SetSectorAddress(src, srcOffset, 0);

        // all further work takes place relative to offset 12 in the sector
        srcOffset += 12;
        destOffset += 12;

        //calculate P parity for 86 columns (twice 43 word-columns)
        byte parity0, parity1;
        for (var col = 0; col < 86; col++) {
            var offset = col;
            CalcECC(src, srcOffset, offset, 86, 24, out parity0, out parity1);
            // store the parities in the sector; theyre read for the Q parity calculations
            dest[destOffset + 1032 * 2 + col] = parity0;
            dest[destOffset + 1032 * 2 + col + 43 * 2] = parity1;
        }

        // calculate Q parity for 52 diagonals (twice 26 word-diagonals), modulo addressing is taken care of in CalcECC
        for (var d = 0; d < 26; d++)
            for (var w = 0; w < 2; w++) {
                var offset = d * 86 + w;
                CalcECC(src, srcOffset, offset, 88, 43, out parity0, out parity1);
                // store the parities in the sector; that's where theyve got to go anyway
                dest[destOffset + 1118 * 2 + d * 2 + w] = parity0;
                dest[destOffset + 1118 * 2 + d * 2 + w + 26 * 2] = parity1;
            }

        //unadjust the offset back to an absolute sector address, which SetSectorAddress expects
        srcOffset -= 12;
        SetSectorAddress(src, srcOffset, address);
    }
}

#endregion

class Blob_ECM(FileSystem vfx) : Blob {
    Stream S;

    readonly struct IndexEntry(int type, uint number, long ecmOffset, long logicalOffset) {
        public readonly long ECMOffset = ecmOffset;
        public readonly long LogicalOffset = logicalOffset;
        public readonly uint Number = number;
        public readonly int Type = type;
    }

    /// <summary>
    /// an index of blocks within the ECM file, for random-access.
    /// itll be sorted by logical ordering, so you can binary search for the address you want
    /// </summary>
    readonly List<IndexEntry> Index = [];
    /// <summary>
    /// the ECMfile-provided EDC integrity checksum. not being used right now
    /// </summary>
    int EDC;
    public long Length;

    public override void Dispose() { S?.Dispose(); S = null; }

    public void Load(string path) {
        S = vfx.Open(path);
        // skip header
        S.Seek(4, SeekOrigin.Current);
        var logOffset = 0L;
        while (true) {
            //read block count. this format is really stupid. maybe its good for detecting non-ecm files or something.
            var b = S.ReadByte();
            if (b == -1) throw new InvalidOperationException("Mis-formed ECM file");
            var bytes = 1;
            var T = b & 3;
            var N = (b >> 2) & 0x1FL;
            var nbits = 5;
            while ((b & (1 << 7)) != 0) {
                if (bytes == 5) throw new InvalidOperationException("Mis-formed ECM file"); //if we're gonna need a 6th byte, this file is broken
                b = S.ReadByte();
                bytes++;
                if (b == -1) throw new InvalidOperationException("Mis-formed ECM file");
                N |= (long)(b & 0x7F) << nbits;
                nbits += 7;
            }
            // end of blocks section
            if (N is 0xFFFF_FFFF) break;
            // the 0x8000_0000 business is confusing, but this is almost positively an error
            if (N >= 0x1_0000_0000) throw new InvalidOperationException("Mis-formed ECM file");
            var pos = (uint)N + 1;
            Index.Add(new IndexEntry(type: T, number: pos, ecmOffset: S.Position, logicalOffset: logOffset));
            switch (T) {
                case 0: S.Seek(pos, SeekOrigin.Current); logOffset += pos; break;
                case 1: S.Seek(pos * (2048 + 3), SeekOrigin.Current); logOffset += pos * 2352; break;
                case 2: S.Seek(pos * 2052, SeekOrigin.Current); logOffset += pos * 2336; break;
                case 3: S.Seek(pos * 2328, SeekOrigin.Current); logOffset += pos * 2336; break;
                default: throw new InvalidOperationException("Mis-formed ECM file");
            }
        }
        var r = new BinaryReader(S);
        EDC = r.ReadInt32();
        Length = logOffset;
    }

    public static bool IsECM(string path) {
        using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        int e = s.ReadByte(), c = s.ReadByte(), m = s.ReadByte(), o = s.ReadByte();
        return e == 'E' && c == 'C' && m == 'M' && o == 0;
    }

    /// <summary>
    /// finds the IndexEntry for the specified logical offset
    /// </summary>
    int FindInIndex(long offset, int lastReadIndex) {
        // try to avoid searching the index. check the last index we we used.
        for (var retry = 0; retry < 2; retry++) // retry 2 times
        {
            var last = Index[lastReadIndex];
            if (lastReadIndex == Index.Count - 1) {
                if (offset >= last.LogicalOffset) return lastReadIndex; // byte_pos would have to be after the last entry
            }
            else {
                var next = Index[lastReadIndex + 1];
                if (offset >= last.LogicalOffset && offset < next.LogicalOffset) return lastReadIndex;
                lastReadIndex++; // try again one sector ahead
            }
        }
        var listIndex = Index.BinarySearchLowerBound(idx => idx.LogicalOffset, offset);
        Assert(listIndex < Index.Count, "insertion point may not be after end");
        return listIndex;
    }

    static void Reconstruct(byte[] secbuf, int type) {
        // sync
        secbuf[0] = 0;
        for (var i = 1; i <= 10; i++) secbuf[i] = 0xFF;
        secbuf[11] = 0x00;
        // misc stuff
        switch (type) {
            case 1:
                secbuf[15] = 0x01; // mode 1
                for (var i = 0x814; i <= 0x81B; i++) secbuf[i] = 0x00; // reserved
                break;
            case 2:
            case 3:
                secbuf[15] = 0x02; // mode 2
                //flags - apparently CD XA specifies two copies of these 4bytes of flags. ECM didnt store the first copy; so we clone the second copy which was stored down to the spot for the first copy.
                secbuf[0x10] = secbuf[0x14];
                secbuf[0x11] = secbuf[0x15];
                secbuf[0x12] = secbuf[0x16];
                secbuf[0x13] = secbuf[0x17];
                break;
        }
        // edc
        switch (type) {
            case 1: ECM.PokeUint(secbuf, 0x810, ECM.EDC_Calc(secbuf, 0, 0x810)); break;
            case 2: ECM.PokeUint(secbuf, 0x818, ECM.EDC_Calc(secbuf, 16, 0x808)); break;
            case 3: ECM.PokeUint(secbuf, 0x92C, ECM.EDC_Calc(secbuf, 16, 0x91C)); break;
        }
        // ecc
        switch (type) {
            case 1: ECM.ECC_Populate(secbuf, 0, secbuf, 0, false); break;
            case 2: ECM.ECC_Populate(secbuf, 0, secbuf, 0, true); break;
        }
    }

    // we don't want to keep churning through this many big byte arrays while reading stuff, so we save a sector cache.
    readonly byte[] Read_SectorBuf = new byte[2352];
    int Read_LastIndex = 0;

    public override int Read(long byte_pos, byte[] buffer, int offset, int _count) {
        long remain = _count;
        var completed = 0;
        while (remain > 0) {
            var listIndex = FindInIndex(byte_pos, Read_LastIndex);
            var ie = Index[listIndex];
            Read_LastIndex = listIndex;
            if (ie.Type == 0) {
                // type 0 is special: its just a raw blob. so all we need to do is read straight out of the stream
                var blockOffset = byte_pos - ie.LogicalOffset;
                var bytesRemainInBlock = ie.Number - blockOffset;
                var todo = remain;
                if (bytesRemainInBlock < todo) todo = bytesRemainInBlock;
                S.Position = ie.ECMOffset + blockOffset;
                while (todo > 0) {
                    int toRead;
                    if (todo > int.MaxValue) toRead = int.MaxValue;
                    else toRead = (int)todo;
                    var done = S.Read(buffer, offset, toRead);
                    if (done != toRead) return completed;
                    completed += done;
                    remain -= done;
                    todo -= done;
                    offset += done;
                    byte_pos += done;
                }
            }
            else {
                // these are sector-based types. they have similar handling.
                var blockOffset = byte_pos - ie.LogicalOffset;
                // figure out which sector within the block we're in
                int outSecSize, inSecSize, outSecOffset;
                switch (ie.Type) {
                    case 1: outSecSize = 2352; inSecSize = 2048; outSecOffset = 0; break;
                    case 2: outSecSize = 2336; inSecSize = 2052; outSecOffset = 16; break;
                    case 3: outSecSize = 2336; inSecSize = 2328; outSecOffset = 16; break;
                    default: throw new InvalidOperationException();
                }
                var secNumberInBlock = blockOffset / outSecSize;
                //var secOffsetInEcm = secNumberInBlock * outSecSize;
                var bytesAskedIntoSector = blockOffset % outSecSize;
                var bytesRemainInSector = outSecSize - bytesAskedIntoSector;
                var todo = remain;
                if (bytesRemainInSector < todo) todo = bytesRemainInSector;
                // move stream to beginning of this sector in ecm
                S.Position = ie.ECMOffset + inSecSize * secNumberInBlock;
                // read and decode the sector
                switch (ie.Type) {
                    case 1: if (S.Read(Read_SectorBuf, 16, 2048) != 2048) return completed; Reconstruct(Read_SectorBuf, 1); break;
                    case 2: if (S.Read(Read_SectorBuf, 20, 2052) != 2052) return completed; Reconstruct(Read_SectorBuf, 2); break;
                    case 3: if (S.Read(Read_SectorBuf, 20, 2328) != 2328) return completed; Reconstruct(Read_SectorBuf, 3); break;
                }
                // sector is decoded to 2352 bytes. Handling doesnt depend much on type from here
                Array.Copy(Read_SectorBuf, (int)bytesAskedIntoSector + outSecOffset, buffer, offset, todo);
                var done = (int)todo;
                offset += done;
                completed += done;
                remain -= done;
                byte_pos += done;
            }
        }
        return completed;
    }
}

class Blob_Raw(FileSystem vfx) : Blob {
    const long Offset = 0;
    BufferedStream S;
    string _path;
    public long Length;
    public string Path {
        get => _path;
        set { _path = value; Length = vfx.FileInfo(_path).length; }
    }

    public override void Dispose() { S?.Dispose(); S = null; }

    public override int Read(long byte_pos, byte[] buffer, int offset, int count) {
        const int buffersize = 2352 * 75 * 2;
        S ??= new(vfx.Open(_path), buffersize);
        var target = byte_pos + Offset;
        if (S.Position != target) S.Position = target;
        return S.Read(buffer, offset, count);
    }
}

#region RiffMaster

/// <summary>
/// Parses a RIFF file into a live data structure.
/// References to large blobs remain mostly on disk in the file which RiffMaster keeps a reference too. Dispose it to close the file.
/// You can modify blobs however you want and write the file back out to a new path, if you're careful (that was the original point of this)
/// Please be sure to test round-tripping when you make any changes. This architecture is a bit tricky to use, but it works if you're careful.
/// </summary>
class RiffMaster : IDisposable {
    public void WriteFile(string fname) {
        using var s = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.Read);
        WriteStream(s);
    }

    public Stream BaseStream;

    public void LoadFile(string fname) => LoadStream(new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read));

    public void Dispose() { BaseStream?.Dispose(); BaseStream = null; }

    static string ReadTag(BinaryReader r) => string.Concat(r.ReadChar(), r.ReadChar(), r.ReadChar(), r.ReadChar());

    protected static void WriteTag(BinaryWriter w, string tag) {
        for (var i = 0; i < 4; i++) w.Write(tag[i]);
        w.Flush();
    }

    public abstract class RiffChunk {
        public string tag;

        /// <summary>
        /// writes this chunk to the stream, including padding
        /// </summary>
        public abstract void WriteStream(Stream s);

        /// <summary>
        /// distinct from a size or a length, the `volume` is the volume of bytes occupied by the chunk on disk (accounting for padding).
        /// </summary>
        public abstract long GetVolume();

        /// <summary>
        /// transforms into a derived class depending on tag
        /// </summary>
        public abstract RiffChunk Morph();
    }

    public class RiffSubchunk : RiffChunk {
        public long Position;
        public uint Length;
        public Stream Source;

        public override void WriteStream(Stream s) {
            var w = new BinaryWriter(s);
            WriteTag(w, tag);
            w.Write(Length);
            w.Flush();
            Source.Position = Position;
            Source.CopyTo(s, Length);
            // all chunks are supposed to be 16bit padded
            if (Length % 2 != 0) s.WriteByte(0);
        }

        public override long GetVolume() {
            long ret = Length;
            if (ret % 2 != 0) ret++;
            return ret;
        }

        public byte[] ReadAll() {
            var msSize = (int)Math.Min((long)int.MaxValue, Length);
            var s = new MemoryStream(msSize);
            Source.Position = Position;
            Source.CopyTo(s, Length);
            return s.ToArray();
        }

        public override RiffChunk Morph() => tag switch {
            "fmt " => new RiffSubchunk_fmt(this),
            _ => this,
        };
    }

    public class RiffSubchunk_fmt : RiffSubchunk {
        public enum FORMAT_TAG : ushort {
            WAVE_FORMAT_UNKNOWN = 0x0000,
            WAVE_FORMAT_PCM = 0x0001,
            WAVE_FORMAT_ADPCM = 0x0002,
            WAVE_FORMAT_ALAW = 0x0006,
            WAVE_FORMAT_MULAW = 0x0007,
            WAVE_FORMAT_OKI_ADPCM = 0x0010,
            WAVE_FORMAT_DIGISTD = 0x0015,
            WAVE_FORMAT_DIGIFIX = 0x0016,
            IBM_FORMAT_MULAW = 0x0101,
            IBM_FORMAT_ALAW = 0x0102,
            IBM_FORMAT_ADPCM = 0x0103,
        }
        public FORMAT_TAG format_tag;
        public ushort channels;
        public uint samplesPerSec;
        public uint avgBytesPerSec;
        public ushort blockAlign;
        public ushort bitsPerSample;

        public RiffSubchunk_fmt(RiffSubchunk origin) {
            tag = "fmt ";
            var r = new BinaryReader(new MemoryStream(origin.ReadAll()));
            format_tag = (FORMAT_TAG)r.ReadUInt16();
            channels = r.ReadUInt16();
            samplesPerSec = r.ReadUInt32();
            avgBytesPerSec = r.ReadUInt32();
            blockAlign = r.ReadUInt16();
            bitsPerSample = r.ReadUInt16();
        }

        public override void WriteStream(Stream s) { Flush(); base.WriteStream(s); }
        void Flush() {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            w.Write((ushort)format_tag);
            w.Write(channels);
            w.Write(samplesPerSec);
            w.Write(avgBytesPerSec);
            w.Write(blockAlign);
            w.Write(bitsPerSample);
            w.Flush();
            Source = s;
            Position = 0;
            Length = (uint)s.Length;
        }
        public override long GetVolume() { Flush(); return base.GetVolume(); }
    }

    public class RiffContainer : RiffChunk {
        public string type;
        public List<RiffChunk> subchunks = [];
        public RiffContainer() => tag = "LIST";
        public RiffChunk GetSubchunk(string tag, string type) {
            foreach (var rc in subchunks.Where(rc => rc.tag == tag))
                if (type == null) return rc;
                else if (rc is RiffContainer cont && cont.type == type) return cont;
            return null;
        }
        public override void WriteStream(Stream s) {
            var w = new BinaryWriter(s);
            WriteTag(w, tag);
            var size = GetVolume();
            if (size > uint.MaxValue) throw new FormatException("File too big to write out");
            w.Write((uint)size);
            WriteTag(w, type);
            w.Flush();
            foreach (var rc in subchunks) rc.WriteStream(s);
            if (size % 2 != 0) s.WriteByte(0);
        }
        public override long GetVolume() => 4 + subchunks.Sum(rc => rc.GetVolume() + 8);
        public override RiffChunk Morph() => type switch {
            "INFO" => new RiffContainer_INFO(this),
            _ => this,
        };
    }

    public class RiffContainer_INFO : RiffContainer {
        public readonly IDictionary<string, string> dictionary = new Dictionary<string, string>();
        public RiffContainer_INFO() => type = "INFO";
        public RiffContainer_INFO(RiffContainer rc) {
            subchunks = rc.subchunks;
            type = "INFO";
            foreach (var chunk in subchunks) {
                if (chunk is not RiffSubchunk rsc) throw new FormatException("Invalid subchunk of INFO list");
                dictionary[rsc.tag] = System.Text.Encoding.ASCII.GetString(rsc.ReadAll());
            }
        }
        void Flush() {
            subchunks.Clear();
            foreach (var (subchunkTag, s) in dictionary) {
                var rs = new RiffSubchunk { tag = subchunkTag, Source = new MemoryStream(Encoding.ASCII.GetBytes(s)), Position = 0 };
                rs.Length = (uint)rs.Source.Length;
                subchunks.Add(rs);
            }
        }
        public override long GetVolume() { Flush(); return base.GetVolume(); }
        public override void WriteStream(Stream s) { Flush(); base.WriteStream(s); }
    }

    public RiffContainer riff;
    long readCounter;

    RiffChunk ReadChunk(BinaryReader r) {
        RiffChunk ret;
        var tag = ReadTag(r); readCounter += 4;
        var size = r.ReadUInt32(); readCounter += 4;
        if (size > int.MaxValue) throw new FormatException("chunk too big");
        if (tag is "RIFF" or "LIST") {
            var rc = new RiffContainer { tag = tag, type = ReadTag(r) };
            readCounter += 4;
            var readEnd = readCounter - 4 + size;
            while (readEnd > readCounter) rc.subchunks.Add(ReadChunk(r));
            ret = rc.Morph();
        }
        else {
            var rsc = new RiffSubchunk { tag = tag, Source = r.BaseStream, Position = r.BaseStream.Position, Length = size };
            readCounter += size;
            r.BaseStream.Position += size;
            ret = rsc.Morph();
        }
        if (size % 2 != 0) { r.ReadByte(); readCounter += 1; }
        return ret;
    }

    public void WriteStream(Stream s) => riff.WriteStream(s);

    /// <summary>
    /// takes posession of the supplied stream
    /// </summary>
    /// <param name="s"></param>
    /// <exception cref="FormatException"></exception>
    public void LoadStream(Stream s) {
        Dispose();
        BaseStream = s;
        readCounter = 0;
        var r = new BinaryReader(s);
        var chunk = ReadChunk(r);
        if (chunk.tag != "RIFF") throw new FormatException("can't recognize riff chunk");
        riff = (RiffContainer)chunk;
    }
}

#endregion

class Blob_Wave(FileSystem vfx) : Blob {
    RiffMaster RiffSource;
    long waveDataStreamPos;
    public long Length;

    public override void Dispose() { RiffSource?.Dispose(); RiffSource = null; }

    //public void Load(byte[] waveData) { }
    public void Load(string path) => Load(vfx.Open(path));
    public void Load(Stream stream) {
        try {
            RiffSource = null;
            var rm = new RiffMaster();
            rm.LoadStream(stream);
            RiffSource = rm;
            // analyze the file to make sure its an OK wave file
            if (rm.riff.type != "WAVE") throw new Exception("Not a RIFF WAVE file");
            if (rm.riff.subchunks.Find(static chunk => chunk.tag == "fmt ") is not RiffMaster.RiffSubchunk_fmt fmt) throw new Exception("Not a valid RIFF WAVE file (missing fmt chunk");
            var dataChunks = rm.riff.subchunks.Where(chunk => chunk.tag == "data").ToList();
            if (dataChunks.Count != 1) throw new Exception("Multi-data-chunk WAVE files not supported");
            if (fmt.format_tag != RiffMaster.RiffSubchunk_fmt.FORMAT_TAG.WAVE_FORMAT_PCM) throw new Exception("Not a valid PCM WAVE file (only PCM is supported)");
            if (fmt.channels != 2 || fmt.bitsPerSample != 16 || fmt.samplesPerSec != 44100) throw new Exception("Not a CDA format WAVE file (conversion not yet supported)");
            // acquire the start of the data chunk
            var dataChunk = (RiffMaster.RiffSubchunk)dataChunks[0];
            waveDataStreamPos = dataChunk.Position;
            Length = dataChunk.Length;
        }
        catch (Exception) { Dispose(); throw; }
    }

    public override int Read(long byte_pos, byte[] buffer, int offset, int count) {
        RiffSource.BaseStream.Position = byte_pos + waveDataStreamPos;
        return RiffSource.BaseStream.Read(buffer, offset, count);
    }
}

class Blob_ZeroPadAdapter(Blob srcBlob, long srcBlobLength) : Blob {
    readonly Blob srcBlob = srcBlob;
    readonly long srcBlobLength = srcBlobLength;

    public override int Read(long byte_pos, byte[] buffer, int offset, int count) {
        var todo = count;
        var end = byte_pos + todo;
        if (end > srcBlobLength) {
            todo = checked((int)(srcBlobLength - byte_pos));
            Array.Clear(buffer, offset + todo, count - todo); // zero-fill the unused part (just for safety's sake)
        }
        srcBlob.Read(byte_pos, buffer, offset, todo);
        return count; // since it's zero padded, this never fails and always reads the requested amount
    }
}

#endregion

#region Disc

class DiscAudioDecoder {
    bool CheckForAudio(string path) => FFmpegService.QueryAudio(path).IsAudio;

    /// <summary>
    /// finds audio at a path similar to the provided path (i.e. finds Track01.mp3 for Track01.wav)
    /// </summary>
    string FindAudio(string audioPath) {
        var (dir, basePath, _) = (Path.GetDirectoryName(audioPath), Path.GetFileName(audioPath), Path.GetExtension(audioPath));
        var filePaths = new DirectoryInfo(dir!).GetFiles().Select(static fi => fi.FullName).ToArray();
        return filePaths.Where(x => audioPath.Equals(x, StringComparison.OrdinalIgnoreCase))
            .Concat(filePaths.Where(filePath => Path.GetFileNameWithoutExtension(filePath).Equals(basePath, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(CheckForAudio);
    }

    public byte[] AcquireWaveData(string audioPath) => FFmpegService.DecodeAudio(FindAudio(audioPath) ?? throw new InvalidOperationException($"Could not find source audio for: {Path.GetFileName(audioPath)}"));
}

class DiscContext(FileSystem vfx, string baseDir) {
    public FileSystem Vfx = vfx;
    public CueFileResolver FileResolver = new(vfx, baseDir);
    public DiscMountPolicy DiscMountPolicy = new();
}

public class DiscMountPolicy {
    public bool Cue_PregapContradictionModeA = true;
    public bool Cue_PregapMode2_AsXAForm2 = true;
    public bool Sbi_AsMednafen = true;

    public void SetForPSX() {
        Cue_PregapContradictionModeA = false;
        Cue_PregapMode2_AsXAForm2 = true;
        Sbi_AsMednafen = true;
    }
}

enum DiscSessionFormat {
    None = -1,
    Type00_CDROM_CDDA = 0x00,
    Type10_CDI = 0x10,
    Type20_CDXA = 0x20,
}

/// <summary>
/// encapsulates a 2 digit BCD number as used various places in the CD specs
/// </summary>
struct BCD2 : IEquatable<BCD2> {
    /// <summary>
    /// The raw BCD value. you can't do math on this number! but you may be asked to supply it to a game program.
    /// The largest number it can logically contain is 99
    /// </summary>
    public byte BCDValue;
    /// <summary>
    /// The derived decimal value. you can do math on this! the largest number it can logically contain is 99.
    /// </summary>
    public int DecimalValue {
        get => (BCDValue & 0xF) + ((BCDValue >> 4) & 0xF) * 10;
        set => BCDValue = IntToBCD(value);
    }
    /// <summary>
    /// makes a BCD2 from a decimal number. don't supply a number > 99 or you might not like the results
    /// </summary>
    public static BCD2 FromDecimal(int d) => new() { DecimalValue = d };
    public static BCD2 FromBCD(byte b) => new() { BCDValue = b };
    public static int BCDToInt(byte n) { var bcd = new BCD2 { BCDValue = n }; return bcd.DecimalValue; }
    public static byte IntToBCD(int n) { var tens = Math.DivRem(n, 10, out var ones); return (byte)((tens << 4) | ones); }
    public override string ToString() => BCDValue.ToString("X2");
    public static bool operator ==(BCD2 lhs, BCD2 rhs) => lhs.BCDValue == rhs.BCDValue;
    public static bool operator !=(BCD2 lhs, BCD2 rhs) => lhs.BCDValue != rhs.BCDValue;
    public static bool operator <(BCD2 lhs, BCD2 rhs) => lhs.BCDValue < rhs.BCDValue;
    public static bool operator >(BCD2 lhs, BCD2 rhs) => lhs.BCDValue > rhs.BCDValue;
    public static bool operator <=(BCD2 lhs, BCD2 rhs) => lhs.BCDValue <= rhs.BCDValue;
    public static bool operator >=(BCD2 lhs, BCD2 rhs) => lhs.BCDValue >= rhs.BCDValue;
    public bool Equals(BCD2 other) => BCDValue == other.BCDValue;
    public override bool Equals(object obj) => obj is BCD2 other && Equals(other);
    public override int GetHashCode() => BCDValue.GetHashCode();
}

readonly struct MSF {
    public static int ToInt(int m, int s, int f)
        => m * 60 * 75 + s * 75 + f;

    /// <summary>
    /// Checks if the string is a legit MSF. It's strict.
    /// </summary>
    public static bool IsMatch(string str) => new MSF(str).Valid;
    public readonly byte Min;
    public readonly byte Sec;
    public readonly byte Frac;
    public readonly bool Valid;
    public readonly bool Negative;

    /// <summary>
    /// creates a timestamp from a string in the form mm:ss:ff
    /// </summary>
    public MSF(string str) {
        if (!(Valid = !(str.Length != 8 ||
            (str[0] < '0' || str[0] > '9') ||
            (str[1] < '0' || str[1] > '9') ||
            (str[2] != ':') ||
            (str[3] < '0' || str[3] > '9') ||
            (str[4] < '0' || str[4] > '9') ||
            (str[5] != ':') ||
            (str[6] < '0' || str[6] > '9') ||
            (str[7] < '0' || str[7] > '9')))) return;
        Min = (byte)((str[0] - '0') * 10 + (str[1] - '0'));
        Sec = (byte)((str[3] - '0') * 10 + (str[4] - '0'));
        Frac = (byte)((str[6] - '0') * 10 + (str[7] - '0'));
    }
    /// <summary>
    /// creates timestamp from the supplied MSF
    /// </summary>
    public MSF(int m, int s, int f) {
        Valid = true;
        Min = (byte)m;
        Sec = (byte)s;
        Frac = (byte)f;
        Negative = false;
    }
    /// <summary>
    /// creates timestamp from supplied SectorNumber
    /// </summary>
    public MSF(int SectorNumber) {
        if (SectorNumber < 0) { SectorNumber = -SectorNumber; Negative = true; }
        else Negative = false;
        Valid = true;
        Min = (byte)(SectorNumber / (60 * 75));
        Sec = (byte)((SectorNumber / 75) % 60);
        Frac = (byte)(SectorNumber % 75);
    }

    /// <summary>
    /// The string representation of the MSF
    /// </summary>
    public override string ToString() => Valid ? $"{(Negative ? '-' : '+')}{Min:D2}:{Sec:D2}:{Frac:D2}" : "--:--:--";

    /// <summary>
    /// The fully multiplied out flat-address Sector number
    /// </summary>
    public int Sector => Min * 60 * 75 + Sec * 75 + Frac;
}

/// <summary>
/// Control bit flags for the Q Subchannel.
/// </summary>
[Flags]
enum EControlQ {
    None = 0,
    PRE = 1, // Pre-emphasis enabled (audio tracks only)
    DCP = 2, // Digital copy permitted
    DATA = 4, // set for data tracks, clear for audio tracks
    _4CH = 8, // Four channel audio
}

struct SubchannelQ {
    // ADR (q-Mode) is necessarily 0x01 for a RawTOCEntry
    public const int kADR = 1;
    public const EControlQ kUnknownControl = 0;

    /// <summary>
    /// ADR and CONTROL
    /// </summary>
    public byte q_status;

    /// <summary>
    /// normal track: BCD indication of the current track number
    /// leadin track: should be 0
    /// </summary>
    public BCD2 q_tno;

    /// <summary>
    /// normal track: BCD indication of the current index
    /// leadin track: 'POINT' field used to ID the TOC entry #
    /// </summary>
    public BCD2 q_index;

    /// <summary>
    /// These are the initial set of timestamps. Meaning varies:
    /// check yellowbook 22.3.3 and 22.3.4
    /// leadin track: unknown
    /// user information track: relative timestamp
    /// leadout: relative timestamp
    /// </summary>
    public BCD2 min, sec, frame;

    /// <summary>
    /// This is supposed to be zero.. but CCD format stores it, so maybe it's useful for copy protection or something
    /// </summary>
    public byte zero;

    /// <summary>
    /// These are the second set of timestamps.  Meaning varies:
    /// check yellowbook 22.3.3 and 22.3.4
    /// leadin track q-mode 1: TOC entry, absolute MSF of track
    /// user information track: absolute timestamp
    /// leadout: absolute timestamp
    /// </summary>
    public BCD2 ap_min, ap_sec, ap_frame;

    /// <summary>
    /// Don't assume this CRC is correct, in the case of some copy protections it is intended to be wrong.
    /// </summary>
    public ushort q_crc;

    /// <summary>
    /// Retrieves the initial set of timestamps (min,sec,frac) as a convenient Timestamp
    /// </summary>
    public int Timestamp {
        get => MSF.ToInt(min.DecimalValue, sec.DecimalValue, frame.DecimalValue);
        set { var ts = new MSF(value); min.DecimalValue = ts.Min; sec.DecimalValue = ts.Sec; frame.DecimalValue = ts.Frac; }
    }

    /// <summary>
    /// Retrieves the second set of timestamps (ap_min, ap_sec, ap_frac) as a convenient Timestamp.
    /// </summary>
    public int AP_Timestamp {
        get => MSF.ToInt(ap_min.DecimalValue, ap_sec.DecimalValue, ap_frame.DecimalValue);
        set { var ts = new MSF(value); ap_min.DecimalValue = ts.Min; ap_sec.DecimalValue = ts.Sec; ap_frame.DecimalValue = ts.Frac; }
    }

    /// <summary>
    /// sets the status byte from the provided adr/qmode and control values
    /// </summary>
    public void SetStatus(byte adr_qmode, EControlQ control) { q_status = ComputeStatus(adr_qmode, control); }

    /// <summary>
    /// computes a status byte from the provided adr/qmode and control values
    /// </summary>
    public static byte ComputeStatus(int adr_qmode, EControlQ control) => (byte)(adr_qmode | (((int)control) << 4));

    /// <summary>
    /// Retrives the ADR field of the q_status member (low 4 bits)
    /// </summary>
    public int ADR => q_status & 0xF;

    /// <summary>
    /// Retrieves the CONTROL field of the q_status member (high 4 bits)
    /// </summary>
    public EControlQ CONTROL => (EControlQ)((q_status >> 4) & 0xF);
}

class RawTOCEntry {
    public SubchannelQ QData;
}

class DiscTrack {
    /// <summary>
    /// The number of the track (1-indexed)
    /// </summary>
    public int Number;

    /// <summary>
    /// The Mode of the track (0 is Audio, 1 and 2 are data)
    /// This is heuristically determined.
    /// Actual sector contents may vary
    /// </summary>
    public int Mode;

    /// <summary>
    /// Is this track a Data track
    /// </summary>
    public bool IsData => !IsAudio;

    /// <summary>
    /// Is this track an Audio track
    /// </summary>
    public bool IsAudio => Mode == 0;

    /// <summary>
    /// The 'control' properties of the track expected to be found in the track's subQ.
    /// However, this is what's indicated by the disc TOC.
    /// Actual sector contents may vary.
    /// </summary>
    public EControlQ Control;

    /// <summary>
    /// The starting LBA of the track (index 1).
    /// </summary>
    public int LBA;

    /// <summary>
    /// The next track in the session. null for the leadout track of a session.
    /// </summary>
    public DiscTrack NextTrack;

    /// <summary>
    /// The Type of a track as specified in the TOC Q-Subchannel data from the control flags.
    /// Could also be 4-Channel Audio, but we'll handle that later if needed
    /// </summary>
    public enum ETrackType {
        /// <summary>
        /// The track type isn't always known.. it can take this value til its populated
        /// </summary>
        Unknown,
        /// <summary>
        /// Data track( TOC Q control 0x04 flag set )
        /// </summary>
        Data,
        /// <summary>
        /// Audio track( TOC Q control 0x04 flag clear )
        /// </summary>
        Audio,
    }
}

/// <summary>
/// Represents our best guess at what a disc drive firmware will receive by reading the TOC from the lead-in track, modeled after CCD contents and mednafen/PSX needs.
/// </summary>
class DiscTOC {
    /// <summary>
    /// The TOC specifies the first recorded track number, independently of whatever may actually be recorded
    /// </summary>
    public int FirstRecordedTrackNumber = -1;

    /// <summary>
    /// The TOC specifies the last recorded track number, independently of whatever may actually be recorded
    /// </summary>
    public int LastRecordedTrackNumber = -1;

    /// <summary>
    /// The TOC specifies the format of the session, so here it is.
    /// </summary>
    public DiscSessionFormat SessionFormat = DiscSessionFormat.None;

    /// <summary>
    /// Information about a single track in the TOC
    /// </summary>
    public struct TOCItem {
        /// <summary>
        /// [IEC10149] "the control field used in the information track"
        /// the raw TOC entries do have a control field which is supposed to match what's found in the track.
        /// Determining whether a track contains audio or data is very important.
        /// A track mode can't be safely determined from reading sectors from the actual track if it's an audio track (there's no sector header with a mode byte)
        /// </summary>
        public EControlQ Control;

        /// <summary>
        /// Whether the Control indicates that this is data
        /// </summary>
        public bool IsData => (Control & EControlQ.DATA) != 0;

        /// <summary>
        /// The location of the track (Index 1)
        /// </summary>
        public int LBA;

        /// <summary>
        /// Whether this entry exists (since the table is 101 entries long always)
        /// </summary>
        public bool Exists;
    }

    /// <summary>
    /// This is a convenient format for storing the TOC (taken from mednafen)
    /// Element 0 is the Lead-in track
    /// Element 100 is the Lead-out track
    /// </summary>
    public TOCItem[] TOCItems = new TOCItem[101];

    /// <summary>
    /// The timestamp of the leadout track. In other words, the end of the user area.
    /// </summary>
    public int LeadoutLBA => TOCItems[100].LBA;
}

class DiscSession {
    /// <summary>
    /// The DiscTOC corresponding to the RawTOCEntries.
    /// </summary>
    public DiscTOC TOC;

    /// <summary>
    /// The raw TOC entries found in the lead-in track.
    /// These aren't very useful, but they're one of the most lowest-level data structures from which other TOC-related stuff is derived
    /// </summary>
    public readonly List<RawTOCEntry> RawTOCEntries = [];

    /// <summary>
    /// The LBA of the session's leadout. In other words, for all intents and purposes, the end of the session
    /// </summary>
    public int LeadoutLBA => LeadoutTrack.LBA;

    /// <summary>
    /// The session number
    /// </summary>
    public int Number;

    /// <summary>
    /// The number of user information tracks in the session.
    /// This excludes the lead-in and lead-out tracks
    /// Use this instead of Tracks.Count
    /// </summary>
    public int InformationTrackCount => Tracks.Count - 2;

    /// <summary>
    /// All the tracks in the session.. but... Tracks[0] is the lead-in track. Tracks[1] should be "Track 1". So beware of this.
    /// For a disc with "3 tracks", Tracks.Count will be 5: it includes that lead-in track as well as the leadout track.
    /// Perhaps we should turn this into a special collection type with no Count or Length, or a method to GetTrack()
    /// </summary>
    public readonly IList<DiscTrack> Tracks = [];

    /// <summary>
    /// A reference to the first information track (Track 1)
    /// The raw TOC may have specified something different; it's not clear how this discrepancy is handled.
    /// </summary>
    public DiscTrack FirstInformationTrack => Tracks[1];

    /// <summary>
    /// A reference to the last information track on the disc.
    /// The raw TOC may have specified something different; it's not clear how this discrepancy is handled.
    /// </summary>
    public DiscTrack LastInformationTrack => Tracks[InformationTrackCount];

    /// <summary>
    /// A reference to the lead-out track.
    /// Effectively, the end of the user area of the disc.
    /// </summary>
    public DiscTrack LeadoutTrack => Tracks[Tracks.Count - 1];

    /// <summary>
    /// A reference to the lead-in track
    /// </summary>
    public DiscTrack LeadinTrack => Tracks[0];

    /// <summary>
    /// Determines which track of the session is at the specified LBA.
    /// </summary>
    public DiscTrack SeekTrack(int lba) {
        var ses = this;
        for (var i = 1; i < Tracks.Count; i++) {
            var track = ses.Tracks[i];
            // if the current track's LBA is > the requested track number, it means the previous track is the one we wanted
            if (track.LBA > lba) return ses.Tracks[i - 1];
        }
        return ses.LeadoutTrack;
    }
}

class Disc : IDisposable {
    /// <summary>
    /// This is a 1-indexed list of sessions (session 1 is at [1])
    /// </summary>
    public readonly IList<DiscSession> Sessions = [null];

    /// <summary>
    /// Session 1 of the disc, since that's all that's needed most of the time.
    /// </summary>
    public DiscSession Session1 => Sessions[1];

    /// <summary>
    /// The DiscTOC corresponding to Session1.
    /// </summary>
    public DiscTOC TOC => Session1.TOC;

    /// <summary>
    /// The name of a disc. Loosely based on the filename. Just for informational purposes.
    /// </summary>
    public string Name;

    /// <summary>
    /// Free-form optional memos about the disc
    /// </summary>
    public readonly IDictionary<string, object> Memos = new Dictionary<string, object>();

    /// <summary>
    /// Disposable resources (blobs, mostly) referenced by this disc
    /// </summary>
    internal readonly IList<IDisposable> DisposableResources = [];

    /// <summary>
    /// The sectors on the disc. Don't use this directly! Use the SectorSynthProvider instead.
    /// </summary>
    internal List<SectorSynthJob2448> Sectors = [];

    /// <summary>
    /// SectorSynthProvider instance for the disc. May be daisy-chained
    /// </summary>
    internal SectorSynthProvider SynthProvider;

    /// <summary>
    /// Parameters set during disc loading which can be referenced by the sector synthesizers
    /// </summary>
    internal SectorSynthParams SynthParams = default;

    internal Disc() { }

    public void Dispose() { foreach (var s in DisposableResources) s.Dispose(); }

    /// <summary>
    /// Easily extracts a mode1 sector range (suitable for extracting ISO FS data files)
    /// </summary>
    //public byte[] Easy_Extract_Mode1(int lba_start, int lba_count, int byteLength = -1) {
    //    int totsize = lba_count * 2048;
    //    byte[] ret = new byte[totsize];
    //    var dsr = new DiscSectorReader(this) { Policy = { DeterministicClearBuffer = false } };
    //    for (int i = 0; i < lba_count; i++) {
    //        dsr.ReadLBA_2048(lba_start + i, ret, i * 2048);
    //    }
    //    if (byteLength != -1 && byteLength != totsize) {
    //        byte[] newret = new byte[byteLength];
    //        Array.Copy(ret, newret, byteLength);
    //        return newret;
    //    }
    //    return ret;
    //}

    public static bool IsValidExtension(string extension) => extension.ToLowerInvariant() is ".ccd" or ".cdi" or ".chd" or ".cue" or ".iso" or ".toc" or ".mds" or ".nrg";
}

#endregion

#region DiscMount

class DiscMount {
    internal readonly int SlowLoadAbortThreshold = 10;
    /// <summary>
    /// Whether a mount operation was aborted due to being too slow
    /// </summary>
    internal bool SlowLoadAborted;

    /// <summary>
    /// The disc
    /// </summary>
    public Disc Disc;

    public DiscMount(FileSystem vfx, string path, bool mednaDisc = false, DiscMountPolicy discMountPolicy = null) {
        discMountPolicy ??= new DiscMountPolicy();

        if (!mednaDisc)
            switch (Path.GetExtension(path).ToLowerInvariant()) {
                case ".ccd": Disc = CcdFormat.LoadCcdToDisc(vfx, path, discMountPolicy); break;
                case ".cdi": Disc = CdiFormat.LoadCdiToDisc(vfx, path, discMountPolicy); break;
                case ".chd": Disc = ChdFormat.LoadChdToDisc(vfx, path, discMountPolicy); break;
                case ".cue": Disc = CueFormat.LoadCue(this, vfx, vfx.Open(path)); break;
                //case ".iso": LoadCue(dir, GenerateCue()); break;
                case ".toc": throw new NotSupportedException(".TOC not supported");
                case ".mds": Disc = MdsFormat.LoadMdsToDisc(vfx, path, discMountPolicy); break;
                case ".nrg": Disc = NrgFormat.LoadNrgToDisc(vfx, path, discMountPolicy); break;
            }
        if (Disc == null) return;

        // set up the lowest level synth provider
        Disc.SynthProvider = new ArraySectorSynthProvider { Sectors = Disc.Sectors, FirstLBA = -150 };

        Disc.Name = Path.GetFileName(path);

        // generate toc and session tracks:
        for (var i = 1; i < Disc.Sessions.Count; i++) {
            var session = Disc.Sessions[i];
            // 1. TOC from RawTOCEntries
            var tocSynth = new Synthesize_DiscTOCFromRawTOCEntries(session.RawTOCEntries);
            session.TOC = tocSynth.Run();
            // 2. DiscTracks from TOC
            var tracksSynth = new Synthesize_DiscTracksFromDiscTOC(Disc, session);
            tracksSynth.Run();
        }

        // insert a synth provider to take care of the leadout track
        if (!mednaDisc) {
            var ss_leadout = new SS_Leadout { SessionNumber = Disc.Sessions.Count - 1, Policy = discMountPolicy };
            new ConditionalSectorSynthProvider().Install(Disc, lba => lba >= Disc.Sessions[Disc.Sessions.Count - 1].LeadoutLBA, ss_leadout);
        }

        // apply SBI if it exists
        var sbiPath = Path.ChangeExtension(path, ".sbi");
        if (vfx.FileExists(sbiPath)) { // && SBIFormat.QuickCheckISSBI(sbiPath)) {
            var loader = new SbiLoader(vfx, sbiPath);
            loader.Apply(Disc, discMountPolicy.Sbi_AsMednafen);
        }
    }
}

#endregion

#region DiscSector

public class DiscSectorReaderPolicy {
    /// <summary>
    /// Different methods that can be used to get 2048 byte sectors
    /// </summary>
    public enum EUserData2048Mode {
        /// <summary>
        /// The contents of the sector should be inspected (mode and form) and 2048 bytes returned accordingly
        /// </summary>
        InspectSector,

        /// <summary>
        /// Read it as mode 1
        /// </summary>
        AssumeMode1,

        /// <summary>
        /// Read it as mode 2 (form 1)
        /// </summary>
        AssumeMode2_Form1,

        /// <summary>
        /// The contents of the sector should be inspected (mode) and 2048 bytes returned accordingly
        /// Mode 2 form is assumed to be 1
        /// </summary>
        InspectSector_AssumeForm1,
    }

    /// <summary>
    /// The method used to get 2048 byte sectors
    /// </summary>
    public EUserData2048Mode UserData2048Mode = EUserData2048Mode.InspectSector;

    /// <summary>
    /// Throw exceptions if 2048 byte data can't be read
    /// </summary>
    public bool ThrowExceptions2048 = true;

    /// <summary>
    /// Indicates whether subcode should be delivered deinterleaved. It isn't stored that way on actual discs. But it is in .sub files.
    /// This defaults to true because it's most likely higher-performing, and it's rarely ever wanted interleaved.
    /// </summary>
    public bool DeinterleavedSubcode = true;

    /// <summary>
    /// Indicates whether the output buffer should be cleared before returning any data.
    /// This will unfortunately involve clearing sections you didn't ask for, and clearing sections about to be filled with data from the disc.
    /// It is a waste of performance, but it will ensure reliability.
    /// </summary>
    public bool DeterministicClearBuffer = true;
}


/// <summary>
/// Main entry point for reading sectors from a disc.
/// This is not a multi-thread capable interface.
/// </summary>
class DiscSectorReader(Disc disc) {
    public DiscSectorReaderPolicy Policy = new();
    readonly byte[] buf2448 = new byte[2448];
    readonly byte[] buf12 = new byte[12];
    readonly SectorSynthJob job = new();
    readonly Disc disc = disc;

    void PrepareJob(int lba) {
        job.LBA = lba;
        job.Params = disc.SynthParams;
        job.Disc = disc;
    }

    void PrepareBuffer(byte[] buffer, int offset, int size) {
        if (Policy.DeterministicClearBuffer) Array.Clear(buffer, offset, size);
    }

    /// <summary>
    /// Reads a full 2352 bytes of user data from a sector
    /// </summary>
    public int ReadLBA_2352(int lba, byte[] buffer, int offset) {
        var sector = disc.SynthProvider.Get(lba);
        if (sector == null) return 0;
        PrepareBuffer(buffer, offset, 2352);
        PrepareJob(lba);
        job.DestBuffer2448 = buf2448;
        job.DestOffset = 0;
        job.Parts = ESectorSynthPart.User2352;
        job.Disc = disc;
        // this can't include subcode, so it's senseless to handle it here
        //if (Policy.DeinterleavedSubcode) job.Parts |= ESectorSynthPart.SubcodeDeinterleave;
        sector.Synth(job);
        Buffer.BlockCopy(buf2448, 0, buffer, offset, 2352);
        return 2352;
    }

    /// <summary>
    /// Reads the absolutely complete 2448 byte sector including all the user data and subcode
    /// </summary>
    public int ReadLBA_2448(int lba, byte[] buffer, int offset) {
        var sector = disc.SynthProvider.Get(lba);
        if (sector == null) return 0;
        PrepareBuffer(buffer, offset, 2352);
        PrepareJob(lba);
        job.DestBuffer2448 = buffer; //go straight to the caller's buffer
        job.DestOffset = offset; //go straight to the caller's buffer
        job.Parts = ESectorSynthPart.Complete2448;
        if (Policy.DeinterleavedSubcode) job.Parts |= ESectorSynthPart.SubcodeDeinterleave;
        sector.Synth(job);
        // we went straight to the caller's buffer, so no need to copy
        return 2448;
    }

    public int ReadLBA_2048_Mode1(int lba, byte[] buffer, int offset) {
        // we can read the 2048 bytes directly
        var sector = disc.SynthProvider.Get(lba);
        if (sector == null) return 0;
        PrepareBuffer(buffer, offset, 2048);
        PrepareJob(lba);
        job.DestBuffer2448 = buf2448;
        job.DestOffset = 0;
        job.Parts = ESectorSynthPart.User2048;
        sector.Synth(job);
        Buffer.BlockCopy(buf2448, 16, buffer, offset, 2048);
        return 2048;
    }

    public int ReadLBA_2048_Mode2_Form1(int lba, byte[] buffer, int offset) {
        // we can read the 2048 bytes directly but we have to get them from the mode 2 data
        var sector = disc.SynthProvider.Get(lba);
        if (sector == null) return 0;
        PrepareBuffer(buffer, offset, 2048);
        PrepareJob(lba);
        job.DestBuffer2448 = buf2448;
        job.DestOffset = 0;
        job.Parts = ESectorSynthPart.User2336;
        sector.Synth(job);
        Buffer.BlockCopy(buf2448, 24, buffer, offset, 2048);
        return 2048;
    }

    /// <summary>
    /// Reads 12 bytes of subQ data from a sector.
    /// This is necessarily deinterleaved.
    /// </summary>
    public int ReadLBA_SubQ(int lba, byte[] buffer, int offset) {
        var sector = disc.SynthProvider.Get(lba);
        if (sector == null) return 0;
        PrepareBuffer(buffer, offset, 12);
        PrepareJob(lba);
        job.DestBuffer2448 = buf2448;
        job.DestOffset = 0;
        job.Parts = ESectorSynthPart.SubchannelQ | ESectorSynthPart.SubcodeDeinterleave;
        sector.Synth(job);
        Buffer.BlockCopy(buf2448, 2352 + 12, buffer, offset, 12);
        return 12;
    }

    /// <summary>
    /// reads 2048 bytes of user data from a sector.
    /// This is only valid for Mode 1 and XA Mode 2 (Form 1) sectors.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public int ReadLBA_2048(int lba, byte[] buffer, int offset) {
        if (Policy.UserData2048Mode == DiscSectorReaderPolicy.EUserData2048Mode.AssumeMode1) return ReadLBA_2048_Mode1(lba, buffer, offset);
        else if (Policy.UserData2048Mode == DiscSectorReaderPolicy.EUserData2048Mode.AssumeMode2_Form1) return ReadLBA_2048_Mode2_Form1(lba, buffer, offset);
        else {
            // we need to determine the type of the sector. in no case do we need the ECC so build special flags here
            var sector = disc.SynthProvider.Get(lba);
            if (sector == null) return 0;
            PrepareBuffer(buffer, offset, 2048);
            PrepareJob(lba);
            job.DestBuffer2448 = buf2448;
            job.DestOffset = 0;
            job.Parts = ESectorSynthPart.Header16 | ESectorSynthPart.User2048 | ESectorSynthPart.EDC12;
            sector.Synth(job);
            // now the inspection, based on the mode
            byte mode = buf2448[15];
            if (mode == 1) {
                Buffer.BlockCopy(buf2448, 16, buffer, offset, 2048);
                return 2048;
            }
            else if (mode == 2) {
                // greenbook pg II-22
                if (Policy.UserData2048Mode != DiscSectorReaderPolicy.EUserData2048Mode.InspectSector_AssumeForm1) {
                    byte submodeByte = buf2448[18];
                    int form = ((submodeByte >> 5) & 1) + 1;
                    if (form == 2) {
                        if (Policy.ThrowExceptions2048) throw new InvalidOperationException("Unsupported scenario: reading 2048 bytes from a Mode2 Form 2 sector");
                        else return 0;
                    }
                }
                // otherwise it's OK
                Buffer.BlockCopy(buf2448, 24, buffer, offset, 2048);
                return 2048;
            }
            else {
                if (Policy.ThrowExceptions2048) throw new InvalidOperationException("Unsupported scenario: reading 2048 bytes from an unhandled sector type");
                else return 0;
            }
        }
    }

    /// <summary>
    /// Reads 12 bytes of subQ data from a sector and stores it unpacked into the provided struct
    /// </summary>
    public void ReadLBA_SubQ(int lba, out SubchannelQ sq) {
        ReadLBA_SubQ(lba, buf12, 0);
        sq.q_status = buf12[0];
        sq.q_tno.BCDValue = buf12[1];
        sq.q_index.BCDValue = buf12[2];
        sq.min.BCDValue = buf12[3];
        sq.sec.BCDValue = buf12[4];
        sq.frame.BCDValue = buf12[5];
        sq.zero = buf12[6];
        sq.ap_min.BCDValue = buf12[7];
        sq.ap_sec.BCDValue = buf12[8];
        sq.ap_frame.BCDValue = buf12[9];
        // CRC is stored inverted and big endian. so reverse
        byte hibyte = (byte)(~buf12[10]);
        byte lobyte = (byte)(~buf12[11]);
        sq.q_crc = (ushort)((hibyte << 8) | lobyte);
    }

    /// <summary>
    /// Reads the mode field from a sector
    /// If this is an audio sector, the results will be nonsense.
    /// </summary>
    public int ReadLBA_Mode(int lba) {
        var sector = disc.SynthProvider.Get(lba);
        if (sector == null) return 0;
        PrepareJob(lba);
        job.DestBuffer2448 = buf2448;
        job.DestOffset = 0;
        job.Parts = ESectorSynthPart.Header16;
        job.Disc = disc;
        sector.Synth(job);
        return buf2448[15];
    }
}

#endregion

#region Disc : CcdFormat

class CcdFormat {
    /// <summary>
    /// Represents a CCD file, faithfully. Minimal interpretation of the data happens.
    /// Currently the [TRACK] sections aren't parsed, though.
    /// </summary>
    public class CcdFile {
        /// <summary>
        /// which version CCD file this came from. We hope it shouldn't affect the semantics of anything else in here, but just in case..
        /// </summary>
        public int Version;
        /// <summary>
        /// this is probably a 0 or 1 bool
        /// </summary>
        public int DataTracksScrambled;
        /// <summary>
        /// ???
        /// </summary>
        public int CDTextLength;
        /// <summary>
        /// The [Session] sections
        /// </summary>
        public readonly List<CcdSession> Sessions = [];
        /// <summary>
        /// The [Entry] sctions
        /// </summary>
        public readonly List<CcdTocEntry> TOCEntries = [];
        /// <summary>
        /// The [TRACK] sections
        /// </summary>
        public readonly List<CcdTrack> Tracks = [];
        /// <summary>
        /// The [TRACK] sections, indexed by number
        /// </summary>
        public readonly Dictionary<int, CcdTrack> TracksByNumber = [];
    }

    /// <summary>
    /// Represents an [Entry] section from a CCD file
    /// </summary>
    public class CcdTocEntry(int entryNum) {
        /// <summary>
        /// these should be 0-indexed
        /// </summary>
        public int EntryNum = entryNum;
        /// <summary>
        /// the CCD specifies this, but it isnt in the actual disc data as such, it is encoded some other (likely difficult to extract) way and that's why CCD puts it here
        /// </summary>
        public int Session;
        /// <summary>
        /// this seems just to be the LBA corresponding to AMIN:ASEC:AFRAME (give or take 150). It's not stored on the disc, and it's redundant.
        /// </summary>
        public int ALBA;
        /// <summary>
        /// this seems just to be the LBA corresponding to PMIN:PSEC:PFRAME (give or take 150). It's not stored on the disc, and it's redundant.
        /// </summary>
        public int PLBA;
        // these correspond pretty directly to values in the Q subchannel fields
        // NOTE: they're specified as absolute MSF. That means, they're 2 seconds off from what they should be when viewed as final TOC values
        public int Control;
        public int ADR;
        public int TrackNo;
        public int Point;
        public int AMin;
        public int ASec;
        public int AFrame;
        public int Zero;
        public int PMin;
        public int PSec;
        public int PFrame;
    }

    /// <summary>
    /// Represents a [Track] section from a CCD file
    /// </summary>
    public class CcdTrack(int number) {
        /// <summary>
        /// note: this is 1-indexed
        /// </summary>
        public int Number = number;
        /// <summary>
        /// The specified data mode.
        /// </summary>
        public int Mode;
        /// <summary>
        /// The indexes specified for the track (these are 0-indexed)
        /// </summary>
        public readonly Dictionary<int, int> Indexes = new();
    }

    /// <summary>
    /// Represents a [Session] section from a CCD file
    /// </summary>
    public class CcdSession(int number) {
        /// <summary>
        /// note: this is 1-indexed.
        /// </summary>
        public int Number = number;
        // Not sure what the default should be.. ive only seen mode=2
        public int PregapMode;
        /// <summary>
        /// this is probably a 0 or 1 bool
        /// </summary>
        public int PregapSubcode;
    }

    class CcdSection : Dictionary<string, int> {
        public string Name;
        public int FetchOrDefault(int def, string key) => TryGetValue(key, out var val) ? val : def;
        public int FetchOrFail(string key) => TryGetValue(key, out var z) ? z : throw new InvalidOperationException($"Malformed or unexpected CCD format: missing required [Entry] key: {key}");
    }

    static List<CcdSection> ParseSections(Stream stream) {
        var sections = new List<CcdSection>();
        var r = new StreamReader(stream);
        CcdSection currSection = null;
        while (true) {
            var line = r.ReadLine();
            if (line is null) break;
            if (line.Length is 0) continue;
            if (line.StartsWith('[')) {
                currSection = new() { Name = line.Trim('[', ']').ToUpperInvariant() };
                sections.Add(currSection);
            }
            else {
                if (currSection == null) throw new InvalidOperationException("Malformed or unexpected CCD format: started without [");
                var parts = line.Split('=');
                if (parts.Length != 2) throw new InvalidOperationException("Malformed or unexpected CCD format: parsing item into two parts");
                if ("FLAGS".Equals(parts[0], StringComparison.OrdinalIgnoreCase))
                    // flags are a space-separated collection of symbolic constants: Skipped
                    // https://www.gnu.org/software/ccd2cue/manual/html_node/FLAGS-_0028Compact-Disc-fields_0029.html#FLAGS-_0028Compact-Disc-fields_0029
                    continue;
                currSection[parts[0].ToUpperInvariant()] = parts[1].StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? int.Parse(parts[1].Substring(2), NumberStyles.HexNumber)
                    : int.Parse(parts[1]);
            }
        }
        return sections;
    }

    static int PreParseIntegrityCheck(IReadOnlyList<CcdSection> sections) {
        switch (sections.Count) {
            case 0: throw new InvalidOperationException("Malformed CCD format: no sections");
            // we need at least a CloneCD and Disc section
            case < 2: throw new InvalidOperationException("Malformed CCD format: insufficient sections");
        }
        var ccdSection = sections[0];
        if (ccdSection.Name != "CLONECD") throw new InvalidOperationException("Malformed CCD format: confusing first section name");
        if (!ccdSection.TryGetValue("VERSION", out var version)) throw new InvalidOperationException("Malformed CCD format: missing version in CloneCD section");
        if (sections[1].Name != "DISC") throw new InvalidOperationException("Malformed CCD format: section[1] isn't [Disc]");
        return version;
    }

    public static CcdFile ParseFrom(Stream stream) {
        var r = new CcdFile();
        var sections = ParseSections(stream);
        r.Version = PreParseIntegrityCheck(sections);
        var discSection = sections[1];
        var nTocEntries = discSection["TOCENTRIES"]; //its conceivable that this could be missing
        var nSessions = discSection["SESSIONS"]; //its conceivable that this could be missing
        r.DataTracksScrambled = discSection.FetchOrDefault(0, "DATATRACKSSCRAMBLED");
        r.CDTextLength = discSection.FetchOrDefault(0, "CDTEXTLENGTH");
        if (r.DataTracksScrambled == 1) throw new InvalidOperationException($"Malformed CCD format: {nameof(r.DataTracksScrambled)}=1 not supported. Please report this, so we can understand what it means.");
        for (var i = 2; i < sections.Count; i++) {
            var section = sections[i];
            if (section.Name.StartsWith("SESSION", StringComparison.Ordinal)) {
                var sesnum = int.Parse(section.Name.Split(' ')[1]);
                var session = new CcdSession(sesnum);
                r.Sessions.Add(session);
                if (sesnum != r.Sessions.Count) throw new InvalidOperationException("Malformed CCD format: wrong session number in sequence");
                session.PregapMode = section.FetchOrDefault(0, "PREGAPMODE");
                session.PregapSubcode = section.FetchOrDefault(0, "PREGAPSUBC");
            }
            else if (section.Name.StartsWith("ENTRY", StringComparison.Ordinal)) {
                var entryNum = int.Parse(section.Name.Split(' ')[1]);
                var e = new CcdTocEntry(entryNum);
                r.TOCEntries.Add(e);
                e.Session = section.FetchOrFail("SESSION");
                e.Point = section.FetchOrFail("POINT");
                e.ADR = section.FetchOrFail("ADR");
                e.Control = section.FetchOrFail("CONTROL");
                e.TrackNo = section.FetchOrFail("TRACKNO");
                e.AMin = section.FetchOrFail("AMIN");
                e.ASec = section.FetchOrFail("ASEC");
                e.AFrame = section.FetchOrFail("AFRAME");
                e.ALBA = section.FetchOrFail("ALBA");
                e.Zero = section.FetchOrFail("ZERO");
                e.PMin = section.FetchOrFail("PMIN");
                e.PSec = section.FetchOrFail("PSEC");
                e.PFrame = section.FetchOrFail("PFRAME");
                e.PLBA = section.FetchOrFail("PLBA");
                // note: LBA 0 is Ansolute MSF 00:02:00
                if (new MSF(e.AMin, e.ASec, e.AFrame).Sector != e.ALBA + 150) throw new InvalidOperationException("Warning: inconsistency in CCD ALBA vs computed A MSF");
                if (new MSF(e.PMin, e.PSec, e.PFrame).Sector != e.PLBA + 150) throw new InvalidOperationException("Warning: inconsistency in CCD PLBA vs computed P MSF");
            }
            else if (section.Name.StartsWith("TRACK", StringComparison.Ordinal)) {
                var entryNum = int.Parse(section.Name.Split(' ')[1]);
                var track = new CcdTrack(entryNum);
                r.Tracks.Add(track);
                r.TracksByNumber[entryNum] = track;
                foreach (var (k, v) in section)
                    if (k == "MODE") track.Mode = v;
                    else if (k.StartsWith("INDEX", StringComparison.Ordinal)) track.Indexes[int.Parse(k.Split(' ')[1])] = v;
            }
        }
        return r;
    }

    class LoadResults {
        public List<RawTOCEntry> RawTOCEntries;
        public CcdFile ParsedCCDFile;
        public bool Valid;
        public InvalidOperationException FailureException;
        public string ImgPath;
        public string SubPath;
        public string CcdPath;
    }

    static LoadResults LoadCcdPath(FileSystem vfx, string path) {
        var r = new LoadResults {
            CcdPath = path,
            ImgPath = Path.ChangeExtension(path, ".img"),
            SubPath = Path.ChangeExtension(path, ".sub")
        };
        try {
            if (!vfx.FileExists(path)) throw new InvalidOperationException("Malformed CCD format: nonexistent CCD file!");
            using var s = vfx.Open(path);
            r.ParsedCCDFile = ParseFrom(s);
            r.Valid = true;
        }
        catch (InvalidOperationException ex) { r.FailureException = ex; }
        return r;
    }

    static void Dump(Disc disc, string path) {
        using (var w = new StreamWriter(path)) {
            // NOTE: IsoBuster requires the A0,A1,A2 RawTocEntries to be first or else it can't do anything with the tracks
            w.WriteLine("[CloneCD]");
            w.WriteLine("Version=3");
            w.WriteLine();
            w.WriteLine("[Disc]");
            w.WriteLine("TocEntries={0}", disc.Sessions.Sum(s => s?.RawTOCEntries.Count ?? 0));
            w.WriteLine("sessions={0}", disc.Sessions.Count - 1);
            w.WriteLine("DataTracksScrambled=0");
            w.WriteLine("CDTextLength=0"); //not supported anyway
            w.WriteLine();
            for (var i = 1; i < disc.Sessions.Count; i++) {
                var session = disc.Sessions[i];
                w.WriteLine("[Session {0}]", i);
                w.WriteLine("PreGapMode=2");
                w.WriteLine("PreGapSubC=1");
                w.WriteLine();
                for (var j = 0; j < session.RawTOCEntries.Count; j++) {
                    var e = session.RawTOCEntries[j];
                    var point = e.QData.q_index.DecimalValue;
                    if (point == 100) point = 0xA0;
                    if (point == 101) point = 0xA1;
                    if (point == 102) point = 0xA2;
                    w.WriteLine("[Entry {0}]", j);
                    w.WriteLine("Session={0}", i);
                    w.WriteLine("Point=0x{0:x2}", point);
                    w.WriteLine("ADR=0x{0:x2}", e.QData.ADR);
                    w.WriteLine("Control=0x{0:x2}", (int)e.QData.CONTROL);
                    w.WriteLine("TrackNo={0}", e.QData.q_tno.DecimalValue);
                    w.WriteLine("AMin={0}", e.QData.min.DecimalValue);
                    w.WriteLine("ASec={0}", e.QData.sec.DecimalValue);
                    w.WriteLine("AFrame={0}", e.QData.frame.DecimalValue);
                    w.WriteLine("ALBA={0}", e.QData.Timestamp - 150); //remember to adapt the absolute MSF to an LBA (this field is redundant...)
                    w.WriteLine("Zero={0}", e.QData.zero);
                    w.WriteLine("PMin={0}", e.QData.ap_min.DecimalValue);
                    w.WriteLine("PSec={0}", e.QData.ap_sec.DecimalValue);
                    w.WriteLine("PFrame={0}", e.QData.ap_frame.DecimalValue);
                    w.WriteLine("PLBA={0}", e.QData.AP_Timestamp - 150); //remember to adapt the absolute MSF to an LBA (this field is redundant...)
                    w.WriteLine();
                }
                for (var tnum = 1; tnum <= session.InformationTrackCount; tnum++) {
                    var track = session.Tracks[tnum];
                    w.WriteLine("[TRACK {0}]", track.Number);
                    w.WriteLine("MODE={0}", track.Mode);
                    w.WriteLine("INDEX 1={0}", track.LBA);
                    w.WriteLine();
                }
            }
        }
        var imgPath = Path.ChangeExtension(path, ".img");
        var subPath = Path.ChangeExtension(path, ".sub");
        var buf2448 = new byte[2448];
        var dsr = new DiscSectorReader(disc);
        using var imgFile = File.Create(imgPath);
        using var subFile = File.Create(subPath);
        var nLBA = disc.Sessions[disc.Sessions.Count - 1].LeadoutLBA;
        for (var lba = 0; lba < nLBA; lba++) {
            dsr.ReadLBA_2448(lba, buf2448, 0);
            imgFile.Write(buf2448, 0, 2352);
            subFile.Write(buf2448, 2352, 96);
        }
    }

    class SS_CCD : SectorSynthJob2448 {
        public override void Synth(SectorSynthJob job) {
            // CCD is always containing everything we'd need (unless a .sub is missing?) so don't worry about flags
            var imgBlob = (Blob)job.Disc.DisposableResources[0];
            var subBlob = (Blob)job.Disc.DisposableResources[1];
            //Read_2442(job.LBA, job.DestBuffer2448, job.DestOffset);
            // read the IMG data if needed
            if ((job.Parts & ESectorSynthPart.UserAny) != 0) imgBlob.Read(job.LBA * 2352L, job.DestBuffer2448, 0, 2352);
            // if subcode is needed, read it
            if ((job.Parts & (ESectorSynthPart.SubcodeAny)) != 0) {
                subBlob.Read(job.LBA * 96L, job.DestBuffer2448, 2352, 96);
                // subcode comes to us deinterleved; we may still need to interleave it
                if ((job.Parts & (ESectorSynthPart.SubcodeDeinterleave)) == 0) SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
            }
        }
    }

    internal static Disc LoadCcdToDisc(FileSystem vfx, string ccdPath, DiscMountPolicy discMountPolicy) {
        var loadResults = LoadCcdPath(vfx, ccdPath);
        if (!loadResults.Valid) throw loadResults.FailureException;
        var disc = new Disc();
        Blob imgBlob = null;
        var imgLen = -1L;
        // mount the IMG file
        // first check for a .ecm in place of the img
        var imgPath = loadResults.ImgPath;
        if (!vfx.FileExists(imgPath)) {
            var ecmPath = Path.ChangeExtension(imgPath, ".img.ecm");
            if (vfx.FileExists(ecmPath) && Blob_ECM.IsECM(ecmPath)) {
                var ecm = new Blob_ECM(vfx);
                ecm.Load(ecmPath);
                imgBlob = ecm;
                imgLen = ecm.Length;
            }
        }
        if (imgBlob == null) {
            if (!vfx.FileExists(loadResults.ImgPath)) throw new InvalidOperationException("Malformed CCD format: nonexistent IMG file!");
            var imgFile = new Blob_Raw(vfx) { Path = loadResults.ImgPath };
            imgLen = imgFile.Length;
            imgBlob = imgFile;
        }
        disc.DisposableResources.Add(imgBlob);

        // mount the SUB file
        if (!vfx.FileExists(loadResults.SubPath)) throw new InvalidOperationException("Malformed CCD format: nonexistent SUB file!");
        var subFile = new Blob_Raw(vfx) { Path = loadResults.SubPath };
        disc.DisposableResources.Add(subFile);
        var subLen = subFile.Length;

        // quick integrity check of file sizes
        if (imgLen % 2352 != 0) throw new InvalidOperationException("Malformed CCD format: IMG file length not multiple of 2352");
        var NumImgSectors = (int)(imgLen / 2352);
        if (subLen != NumImgSectors * 96) throw new InvalidOperationException("Malformed CCD format: SUB file length not matching IMG");

        var ccdf = loadResults.ParsedCCDFile;

        //the only instance of a sector synthesizer we'll need
        var synth = new SS_CCD();

        // create the initial session
        var curSession = 1;
        disc.Sessions.Add(new() { Number = curSession });

        // generate DiscTOCRaw items from the ones specified in the CCD file
        foreach (var entry in ccdf.TOCEntries.OrderBy(te => te.Session)) {
            if (entry.Session != curSession) {
                if (entry.Session != curSession + 1) throw new InvalidOperationException("Malformed CCD format: Session incremented more than one");
                curSession = entry.Session;
                disc.Sessions.Add(new() { Number = curSession });
            }
            var tno = BCD2.FromDecimal(entry.TrackNo); // this should actually be zero. im not sure if this is stored as BCD2 or not
            // special values taken from this: http://www.staff.uni-mainz.de/tacke/scsi/SCSI2-14.html
            var ino = BCD2.FromDecimal(entry.Point);
            ino.BCDValue = entry.Point switch {
                0xA0 or 0xA1 or 0xA2 => (byte)entry.Point,
                _ => ino.BCDValue,
            };
            var q = new SubchannelQ {
                q_status = SubchannelQ.ComputeStatus(entry.ADR, (EControlQ)(entry.Control & 0xF)),
                q_tno = tno,
                q_index = ino,
                min = BCD2.FromDecimal(entry.AMin),
                sec = BCD2.FromDecimal(entry.ASec),
                frame = BCD2.FromDecimal(entry.AFrame),
                zero = (byte)entry.Zero,
                ap_min = BCD2.FromDecimal(entry.PMin),
                ap_sec = BCD2.FromDecimal(entry.PSec),
                ap_frame = BCD2.FromDecimal(entry.PFrame),
                q_crc = 0
            };
            disc.Sessions[curSession].RawTOCEntries.Add(new() { QData = q });
        }

        // analyze the RAWTocEntries to figure out what type of track track 1 is
        var tocSynth = new Synthesize_DiscTOCFromRawTOCEntries(disc.Session1.RawTOCEntries);
        var toc = tocSynth.Run();

        // Add sectors for the mandatory track 1 pregap, which isn't stored in the CCD file
        var pregapTrackType = CueTrackType.Audio;
        if (toc.TOCItems[1].IsData)
            pregapTrackType = toc.SessionFormat switch {
                DiscSessionFormat.Type20_CDXA => CueTrackType.Mode2_2352,
                DiscSessionFormat.Type10_CDI => CueTrackType.CDI_2352,
                DiscSessionFormat.Type00_CDROM_CDDA => CueTrackType.Mode1_2352,
                _ => pregapTrackType,
            };

        // add tracks
        for (var i = 0; i < 150; i++) {
            var ss_gap = new SS_Gap() {
                Policy = discMountPolicy,
                TrackType = pregapTrackType
            };
            disc.Sectors.Add(ss_gap);
            var qRelMSF = i - 150;
            // tweak relMSF due to ambiguity/contradiction in yellowbook docs
            if (!discMountPolicy.Cue_PregapContradictionModeA) qRelMSF++;
            ss_gap.sq.SetStatus(SubchannelQ.kADR, toc.TOCItems[1].Control);
            ss_gap.sq.q_tno = BCD2.FromDecimal(1);
            ss_gap.sq.q_index = BCD2.FromDecimal(0);
            ss_gap.sq.AP_Timestamp = i;
            ss_gap.sq.Timestamp = qRelMSF;
            ss_gap.Pause = true;
        }

        // build the sectors
        for (var i = 0; i < NumImgSectors; i++) disc.Sectors.Add(synth);
        return disc;
    }
}

#endregion

#region Disc : CdiFormat

public static class CdiFormat {
    /// <summary>
    /// Represents a CDI file, faithfully. Minimal interpretation of the data happens.
    /// </summary>
    public class CdiFile {
        /// <summary>
        /// Number of sessions
        /// </summary>
        public byte NumSessions;
        /// <summary>
        /// The session blocks
        /// </summary>
        public readonly List<CdiSession> Sessions = [];
        /// <summary>
        /// The track blocks
        /// </summary>
        public readonly List<CdiTrack> Tracks = [];
        /// <summary>
        /// The disc info block
        /// </summary>
        public readonly CdiDiscInfo DiscInfo = new();
        /// <summary>
        /// Footer size in bytes
        /// </summary>
        public uint Entrypoint;
    }

    /// <summary>
    /// Represents a session block from a CDI file
    /// </summary>
    public class CdiSession {
        /// <summary>
        /// Number of tracks in session (1..99) (or 0 = no more sessions)
        /// </summary>
        public byte NumTracks;
    }

    /// <summary>
    /// Represents a track/disc info block header from a CDI track
    /// </summary>
    public class CdiTrackHeader {
        /// <summary>
        /// Number of tracks on disc (1..99)
        /// </summary>
        public byte NumTracks;
        /// <summary>
        /// Full Path/Filename (may be empty)
        /// </summary>
        public string Path;
        /// <summary>
        /// 0x0098 = CD-ROM, 0x0038 = DVD-ROM
        /// </summary>
        public ushort MediumType;
    }

    /// <summary>
    /// Represents a CD text block from a CDI track
    /// </summary>
    public class CdiCDText {
        /// <summary>
        /// A CD text block has 0-18 strings, each of variable length
        /// </summary>
        public readonly IList<string> CdTexts = [];
    }

    /// <summary>
    /// Represents a track block from a CDI file
    /// </summary>
    public class CdiTrack : CdiTrackHeader {
        /// <summary>
        /// The sector count of each index specified for the track
        /// </summary>
        public readonly IList<uint> IndexSectorCounts = [];
        /// <summary>
        /// CD text blocks
        /// </summary>
        public readonly IList<CdiCDText> CdTextBlocks = [];
        /// <summary>
        /// The specified track mode (0 = Audio, 1 = Mode1, 2 = Mode2/Mixed)
        /// </summary>
        public byte TrackMode;
        /// <summary>
        /// Session number (0-indexed)
        /// </summary>
        public uint SessionNumber;
        /// <summary>
        /// Track number (0-indexed, releative to session)
        /// </summary>
        public uint TrackNumber;
        /// <summary>
        /// Track start address
        /// </summary>
        public uint TrackStartAddress;
        /// <summary>
        /// Track length, in sectors
        /// </summary>
        public uint TrackLength;
        /// <summary>
        /// The specified read mode (0 = Mode1, 1 = Mode2, 2 = Audio, 3 = Raw+Q, 4 = Raw+PQRSTUVW)
        /// </summary>
        public uint ReadMode;
        /// <summary>
        /// Upper 4 bits of ADR/Control
        /// </summary>
        public uint Control;
        /// <summary>
        /// 12-letter/digit string (may be empty)
        /// </summary>
        public string IsrcCode;
        /// <summary>
        /// Any non-zero is valid?
        /// </summary>
        public uint IsrcValidFlag;
        /// <summary>
        /// Only present on last track of a session (0 = Audio/CD-DA, 1 = Mode1/CD-ROM, 2 = Mode2/CD-XA)
        /// </summary>
        public uint SessionType;
    }

    /// <summary>
    /// Represents a disc info block from a CDI file
    /// </summary>
    public class CdiDiscInfo : CdiTrackHeader {
        /// <summary>
        /// Total number of sectors
        /// </summary>
        public uint DiscSize;
        /// <summary>
        /// probably junk for non-ISO data discs
        /// </summary>
        public string VolumeId;
        /// <summary>
        /// 13-digit string (may be empty)
        /// </summary>
        public string Ean13Code;
        /// <summary>
        /// Any non-zero is valid?
        /// </summary>
        public uint Ean13CodeValid;
        /// <summary>
        /// CD text (for lead-in?)
        /// </summary>
        public string CdText;
    }

    public static CdiFile ParseFrom(Stream s) {
        var ret = new CdiFile();
        using var r = new BinaryReader(s);
        try {
            s.Seek(-4, SeekOrigin.End);
            ret.Entrypoint = r.ReadUInt32();
            s.Seek(-ret.Entrypoint, SeekOrigin.End);
            ret.NumSessions = r.ReadByte();
            if (ret.NumSessions == 0) throw new InvalidOperationException("Malformed CDI format: 0 sessions!");

            void ParseTrackHeader(CdiTrackHeader header) {
                s.Seek(15, SeekOrigin.Current); // unknown bytes
                header.NumTracks = r.ReadByte();
                var pathLen = r.ReadByte();
                header.Path = r.ReadF8String(pathLen);
                s.Seek(29, SeekOrigin.Current); // unknown bytes
                header.MediumType = r.ReadUInt16();
                switch (header.MediumType) {
                    case 0x0038: throw new InvalidOperationException("Malformed CDI format: DVD was specified, but this is not supported!");
                    case 0x0098: return;
                    default: throw new InvalidOperationException("Malformed CDI format: Invalid medium type!");
                }
            }

            for (var i = 0; i <= ret.NumSessions; i++) {
                var session = new CdiSession();
                s.Seek(1, SeekOrigin.Current); // unknown byte
                session.NumTracks = r.ReadByte();
                s.Seek(13, SeekOrigin.Current); // unknown bytes
                ret.Sessions.Add(session);

                // the last session block should have 0 tracks (as it indicates no more sessions)
                if (session.NumTracks == 0 && i != ret.NumSessions) throw new InvalidOperationException("Malformed CDI format: No tracks in session!");
                if (session.NumTracks + ret.Tracks.Count > 99) throw new InvalidOperationException("Malformed CDI format: More than 99 tracks on disc!");

                for (var j = 0; j < session.NumTracks; j++) {
                    var track = new CdiTrack();
                    ParseTrackHeader(track);
                    var indexes = r.ReadUInt16();
                    if (indexes < 2) throw new InvalidOperationException("Malformed CDI format: Less than 2 indexes in track!"); // We should have at least 2 indexes (one pre-gap, and one "real" one)
                    for (var k = 0; k < indexes; k++) track.IndexSectorCounts.Add(r.ReadUInt32());
                    var numCdTextBlocks = r.ReadUInt32();
                    for (var k = 0; k < numCdTextBlocks; k++) {
                        var cdTextBlock = new CdiCDText();
                        for (var l = 0; l < 18; l++) {
                            var cdTextLen = r.ReadByte();
                            if (cdTextLen > 0) cdTextBlock.CdTexts.Add(r.ReadF8String(cdTextLen));
                        }
                        track.CdTextBlocks.Add(cdTextBlock);
                    }
                    s.Seek(2, SeekOrigin.Current); // unknown bytes
                    track.TrackMode = r.ReadByte();
                    if (track.TrackMode > 2) throw new InvalidOperationException("Malformed CDI format: Invalid track mode!");
                    s.Seek(7, SeekOrigin.Current); // unknown bytes
                    track.SessionNumber = r.ReadUInt32();
                    if (track.SessionNumber != i) throw new InvalidOperationException("Malformed CDI format: Session number mismatch!");
                    track.TrackNumber = r.ReadUInt32();
                    if (track.TrackNumber != j) throw new InvalidOperationException("Malformed CDI format: Track number mismatch!");
                    track.TrackStartAddress = r.ReadUInt32();
                    track.TrackLength = r.ReadUInt32();
                    s.Seek(16, SeekOrigin.Current); // unknown bytes
                    track.ReadMode = r.ReadUInt32();
                    if (track.ReadMode > 4) throw new InvalidOperationException("Malformed CDI format: Invalid read mode!");
                    track.Control = r.ReadUInt32();
                    if ((track.Control & ~0xF) != 0) throw new InvalidOperationException("Malformed CDI format: Invalid control!");
                    s.Seek(1, SeekOrigin.Current); // unknown byte
                    var redundantTrackLen = r.ReadUInt32();
                    if (track.TrackLength != redundantTrackLen) throw new InvalidOperationException("Malformed CDI format: Track length mismatch!");
                    s.Seek(4, SeekOrigin.Current); // unknown bytes
                    track.IsrcCode = r.ReadF8String(12);
                    track.IsrcValidFlag = r.ReadUInt32();
                    if (track.IsrcValidFlag == 0) track.IsrcCode = string.Empty;
                    s.Seek(87, SeekOrigin.Current); // unknown bytes
                    track.SessionType = r.ReadByte();
                    switch (track.SessionType) {
                        case > 2: throw new InvalidOperationException("Malformed CDI format: Invalid session type!");
                        case > 0 when j != session.NumTracks - 1: throw new InvalidOperationException("Malformed CDI format: Session type was specified, but this is only supposed to be present on the last track!");
                    }
                    s.Seek(5, SeekOrigin.Current); // unknown bytes
                    var notLastTrackInSession = r.ReadByte();
                    switch (notLastTrackInSession) {
                        case 0 when j != session.NumTracks - 1: throw new InvalidOperationException("Malformed CDI format: Track was specified to be the last track of the session, but more tracks are available!");
                        case > 1: throw new InvalidOperationException("Malformed CDI format: Invalid not last track of session flag!");
                    }
                    s.Seek(5, SeekOrigin.Current); // unknown bytes
                    ret.Tracks.Add(track);
                }
            }

            ParseTrackHeader(ret.DiscInfo);
            ret.DiscInfo.DiscSize = r.ReadUInt32();
            if (ret.DiscInfo.DiscSize != ret.Tracks.Sum(t => t.TrackLength)) { } //throw new InvalidOperationException("Malformed CDI format: Disc size mismatch!");
            var volumeIdLen = r.ReadByte();
            ret.DiscInfo.VolumeId = r.ReadF8String(volumeIdLen);
            s.Seek(9, SeekOrigin.Current); // unknown bytes
            ret.DiscInfo.Ean13Code = r.ReadF8String(13);
            ret.DiscInfo.Ean13CodeValid = r.ReadUInt32();
            if (ret.DiscInfo.Ean13CodeValid == 0) ret.DiscInfo.Ean13Code = string.Empty;
            var cdTextLengh = r.ReadUInt32();
            if (cdTextLengh > int.MaxValue) throw new InvalidOperationException("Malformed CDI format: CD text too large!");
            ret.DiscInfo.CdText = r.ReadF8String((int)cdTextLengh);
            s.Seek(12, SeekOrigin.Current); // unknown bytes
            if (ret.Tracks.Exists(track => track.NumTracks != ret.Tracks.Count) || ret.DiscInfo.NumTracks != ret.Tracks.Count) throw new InvalidOperationException("Malformed CDI format: Total track number mismatch!");
            if (s.Position != s.Length - 4) { } //throw new InvalidOperationException("Malformed CDI format: Did not reach end of footer after parsing!");
            return ret;
        }
        catch (EndOfStreamException) { throw new InvalidOperationException("Malformed CDI format: Unexpected s end!"); }
    }

    public class LoadResults {
        public CdiFile ParsedCDIFile;
        public bool Valid;
        public InvalidOperationException FailureException;
        public string CdiPath;
    }

    static LoadResults LoadCdiPath(FileSystem vfx, string path) {
        var ret = new LoadResults { CdiPath = path };
        try {
            if (!vfx.FileExists(path)) throw new InvalidOperationException("Malformed CDI format: nonexistent CDI file!");
            using var s = vfx.Open(path);
            var cdif = ParseFrom(s);
            ret.ParsedCDIFile = cdif;
            ret.Valid = true;
        }
        catch (InvalidOperationException ex) { ret.FailureException = ex; }
        return ret;
    }

    internal static Disc LoadCdiToDisc(FileSystem vfx, string cdiPath, DiscMountPolicy discMountPolicy) {
        var loadResults = LoadCdiPath(vfx, cdiPath);
        if (!loadResults.Valid) throw loadResults.FailureException;

        var disc = new Disc();
        var cdi = loadResults.ParsedCDIFile;

        var blob = new Blob_Raw(vfx) { Path = cdiPath };
        disc.DisposableResources.Add(blob);

        var trackOffset = 0;
        var blobOffset = 0;
        for (var i = 0; i < cdi.NumSessions; i++) {
            var session = new DiscSession { Number = i + 1 };

            // leadin track
            // we create this only for session 2+, not session 1
            var leadinSize = i == 0 ? 0 : 4500;
            for (var j = 0; j < leadinSize; j++) {
                var cueTrackType = CueTrackType.Audio;
                if ((cdi.Tracks[trackOffset].Control & 4) != 0)
                    cueTrackType = cdi.Tracks[trackOffset + cdi.Sessions[i].NumTracks - 1].SessionType switch {
                        0 => CueTrackType.Mode1_2352,
                        1 => CueTrackType.CDI_2352,
                        2 => CueTrackType.Mode2_2352,
                        _ => cueTrackType,
                    };
                disc.Sectors.Add(new SS_Gap { Policy = discMountPolicy, TrackType = cueTrackType });
            }

            for (var j = 0; j < cdi.Sessions[i].NumTracks; j++) {
                var track = cdi.Tracks[trackOffset + j];
                RawTOCEntry EmitRawTOCEntry() {
                    var q = new SubchannelQ();
                    q.SetStatus(SubchannelQ.kADR, (EControlQ)track.Control);
                    q.q_tno = BCD2.FromDecimal(0);
                    q.q_index = BCD2.FromDecimal(trackOffset + j + 1);
                    q.Timestamp = 0;
                    q.zero = 0;
                    q.AP_Timestamp = disc.Sectors.Count;
                    q.q_crc = 0;
                    return new() { QData = q };
                }
                var sectorSize = track.ReadMode switch {
                    0 => 2048,
                    1 => 2336,
                    2 => 2352,
                    3 => 2368,
                    4 => 2448,
                    _ => throw new InvalidOperationException(),
                };
                var curIndex = 0;
                var relMSF = -track.IndexSectorCounts[0];
                var indexSectorOffset = 0U;
                for (var k = 0; k < track.TrackLength; k++) {
                    if (track.IndexSectorCounts[curIndex] == k - indexSectorOffset) {
                        indexSectorOffset += track.IndexSectorCounts[curIndex];
                        curIndex++;
                        if (track.IndexSectorCounts.Count == curIndex) throw new InvalidOperationException("Malformed CDI Format: Reached end of index list unexpectedly");
                        if (curIndex == 1) session.RawTOCEntries.Add(EmitRawTOCEntry());
                    }
                    SS_Base synth = track.ReadMode switch {
                        0 => new SS_Mode1_2048(),
                        1 => new SS_Mode2_2336(),
                        2 => new SS_2352(),
                        3 => new SS_2364_DeinterleavedQ(),
                        4 => new SS_2448_Interleaved(),
                        _ => throw new InvalidOperationException(),
                    };
                    synth.Blob = blob;
                    synth.BlobOffset = blobOffset;
                    synth.Policy = discMountPolicy;
                    const byte kADR = 1;
                    synth.sq.SetStatus(kADR, (EControlQ)track.Control);
                    synth.sq.q_tno = BCD2.FromDecimal(trackOffset + j + 1);
                    synth.sq.q_index = BCD2.FromDecimal(curIndex);
                    synth.sq.Timestamp = !discMountPolicy.Cue_PregapContradictionModeA && curIndex == 0 ? (int)relMSF + 1 : (int)relMSF;
                    synth.sq.zero = 0;
                    synth.sq.AP_Timestamp = disc.Sectors.Count;
                    synth.sq.q_crc = 0;
                    synth.Pause = curIndex == 0;
                    disc.Sectors.Add(synth);
                    blobOffset += sectorSize;
                    relMSF++;
                }
            }

            // leadout track
            var leadoutSize = i == 0 ? 6750 : 2250;
            for (var j = 0; j < leadoutSize; j++)
                disc.Sectors.Add(new SS_Leadout { SessionNumber = session.Number, Policy = discMountPolicy });

            var TOCMiscInfo = new Synthesize_A0A1A2(
                firstRecordedTrackNumber: trackOffset + 1,
                lastRecordedTrackNumber: trackOffset + cdi.Sessions[i].NumTracks,
                sessionFormat: (DiscSessionFormat)(cdi.Tracks[trackOffset + cdi.Sessions[i].NumTracks - 1].SessionType * 0x10),
                leadoutTimestamp: disc.Sectors.Count);
            TOCMiscInfo.Run(session.RawTOCEntries);
            disc.Sessions.Add(session);
            trackOffset += cdi.Sessions[i].NumTracks;
        }
        return disc;
    }
}

#endregion

#region Disc : ChdFormat

static class ChdFormat {
    /// <summary>
    /// Represents a CHD file.
    /// </summary>
    public class ChdFile {
        /// <summary>
        /// chd_file* to be used for chd_ functions
        /// </summary>
        public IntPtr File;
        /// <summary>
        /// CHD header, interpreted by chd-rs
        /// </summary>
        public LibChd.chd_header Header;
        /// <summary>
        /// CHD CD metadata for each track
        /// </summary>
        public readonly IList<ChdCdMetadata> CdMetadatas = [];
    }

    /// <summary>
    /// Results of chd_get_metadata with cdrom track metadata tags
    /// </summary>
    public class ChdCdMetadata {
        /// <summary>
        /// Track number (1..99)
        /// </summary>
        public uint Track;
        /// <summary>
        /// Indicates this is a CDI format
        /// chd_track_type doesn't have an explicit enum for this
        /// However, this is still important info for discerning the session format
        /// </summary>
        public bool IsCDI;
        /// <summary>
        /// Track type
        /// </summary>
        public LibChd.chd_track_type TrackType;
        /// <summary>
        /// Subcode type
        /// </summary>
        public LibChd.chd_sub_type SubType;
        /// <summary>
        /// Size of each sector
        /// </summary>
        public uint SectorSize;
        /// <summary>
        /// Subchannel size
        /// </summary>
        public uint SubSize;
        /// <summary>
        /// Number of frames in this track
        /// This might include pregap, if that is stored in the chd
        /// </summary>
        public uint Frames;
        /// <summary>
        /// Number of "padding" frames in this track
        /// This is done in order to maintain a multiple of 4 frames for each track
        /// These padding frames aren't representative of the actual disc anyways
        /// They're only useful to know the offset of the next track within the chd
        /// </summary>
        public uint Padding;
        /// <summary>
        /// Number of pregap sectors
        /// </summary>
        public uint Pregap;
        /// <summary>
        /// Pregap track type
        /// </summary>
        public LibChd.chd_track_type PregapTrackType;
        /// <summary>
        /// Pregap subcode type
        /// </summary>
        public LibChd.chd_sub_type PregapSubType;
        /// <summary>
        /// Indicates whether pregap is in the CHD
        /// If pregap isn't in the CHD, it needs to be generated where appropriate
        /// </summary>
        public bool PregapInChd;
        /// <summary>
        /// Number of postgap sectors
        /// </summary>
        public uint PostGap;
    }

    static LibChd.chd_track_type GetTrackType(string type) => type switch {
        "MODE1" => LibChd.chd_track_type.CD_TRACK_MODE1,
        "MODE1/2048" => LibChd.chd_track_type.CD_TRACK_MODE1,
        "MODE1_RAW" => LibChd.chd_track_type.CD_TRACK_MODE1_RAW,
        "MODE1/2352" => LibChd.chd_track_type.CD_TRACK_MODE1_RAW,
        "MODE2" => LibChd.chd_track_type.CD_TRACK_MODE2,
        "MODE2/2336" => LibChd.chd_track_type.CD_TRACK_MODE2,
        "MODE2_FORM1" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM1,
        "MODE2/2048" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM1,
        "MODE2_FORM2" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM2,
        "MODE2/2324" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM2,
        "MODE2_FORM_MIX" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX,
        "MODE2_RAW" => LibChd.chd_track_type.CD_TRACK_MODE2_RAW,
        "MODE2/2352" => LibChd.chd_track_type.CD_TRACK_MODE2_RAW,
        "CDI/2352" => LibChd.chd_track_type.CD_TRACK_MODE2_RAW,
        "AUDIO" => LibChd.chd_track_type.CD_TRACK_AUDIO,
        _ => throw new InvalidOperationException("Malformed CHD format: Invalid track type!"),
    };

    static (LibChd.chd_track_type TrackType, bool ChdContainsPregap) GetTrackTypeForPregap(string type)
        => type.Length > 0 && type[0] == 'V'
        ? (GetTrackType(type[1..]), true)
        : (GetTrackType(type), false);

    static uint GetSectorSize(LibChd.chd_track_type type) => type switch {
        LibChd.chd_track_type.CD_TRACK_MODE1 => 2048,
        LibChd.chd_track_type.CD_TRACK_MODE1_RAW => 2352,
        LibChd.chd_track_type.CD_TRACK_MODE2 => 2336,
        LibChd.chd_track_type.CD_TRACK_MODE2_FORM1 => 2048,
        LibChd.chd_track_type.CD_TRACK_MODE2_FORM2 => 2324,
        LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX => 2336,
        LibChd.chd_track_type.CD_TRACK_MODE2_RAW => 2352,
        LibChd.chd_track_type.CD_TRACK_AUDIO => 2352,
        _ => throw new InvalidOperationException("Malformed CHD format: Invalid track type!"),
    };

    static LibChd.chd_sub_type GetSubType(string type) => type switch {
        "RW" => LibChd.chd_sub_type.CD_SUB_NORMAL,
        "RW_RAW" => LibChd.chd_sub_type.CD_SUB_RAW,
        "NONE" => LibChd.chd_sub_type.CD_SUB_NONE,
        _ => throw new InvalidOperationException("Malformed CHD format: Invalid sub type!"),
    };

    static uint GetSubSize(LibChd.chd_sub_type type) => type switch {
        LibChd.chd_sub_type.CD_SUB_NORMAL => 96,
        LibChd.chd_sub_type.CD_SUB_RAW => 96,
        LibChd.chd_sub_type.CD_SUB_NONE => 0,
        _ => throw new InvalidOperationException("Malformed CHD format: Invalid sub type!"),
    };

    static readonly string[] _metadataTags = { "TRACK", "TYPE", "SUBTYPE", "FRAMES", "PREGAP", "PGTYPE", "PGSUB", "POSTGAP" };

    static ChdCdMetadata ParseMetadata2(string metadata) {
        var strs = metadata.Split(' ');
        if (strs.Length != 8) throw new InvalidOperationException("Malformed CHD format: Incorrect number of metadata tags");
        for (var i = 0; i < 8; i++) {
            var spl = strs[i].Split(':');
            if (spl.Length != 2 || _metadataTags[i] != spl[0]) throw new InvalidOperationException("Malformed CHD format: Invalid metadata tag");
            strs[i] = spl[1];
        }
        var ret = new ChdCdMetadata();
        try {
            ret.Track = uint.Parse(strs[0]);
            ret.TrackType = GetTrackType(strs[1]);
            ret.SubType = GetSubType(strs[2]);
            ret.Frames = uint.Parse(strs[3]);
            ret.Pregap = uint.Parse(strs[4]);
            (ret.PregapTrackType, ret.PregapInChd) = GetTrackTypeForPregap(strs[5]);
            ret.PregapSubType = GetSubType(strs[6]);
            ret.PostGap = uint.Parse(strs[7]);
        }
        catch (Exception ex) { throw ex as InvalidOperationException ?? new("Malformed CHD format: Metadata parsing threw an exception", ex); }
        if (ret.PregapInChd && ret.Pregap == 0) throw new InvalidOperationException("Malformed CHD format: CHD track type indicates it contained pregap data, but no pregap data is present");
        ret.IsCDI = strs[1] == "CDI/2352";
        ret.SectorSize = GetSectorSize(ret.TrackType);
        ret.SubSize = GetSubSize(ret.SubType);
        ret.Padding = (0 - ret.Frames) & 3;
        return ret;
    }

    static ChdCdMetadata ParseMetadata(string metadata) {
        var strs = metadata.Split(' ');
        if (strs.Length != 4)
            throw new InvalidOperationException("Malformed CHD format: Incorrect number of metadata tags");
        for (var i = 0; i < 4; i++) {
            var spl = strs[i].Split(':');
            if (spl.Length != 2 || _metadataTags[i] != spl[0]) throw new InvalidOperationException("Malformed CHD format: Invalid metadata tag");
            strs[i] = spl[1];
        }
        var ret = new ChdCdMetadata();
        try {
            ret.Track = uint.Parse(strs[0]);
            ret.TrackType = GetTrackType(strs[1]);
            ret.SubType = GetSubType(strs[2]);
            ret.Frames = uint.Parse(strs[3]);
        }
        catch (Exception ex) { throw ex as InvalidOperationException ?? new("Malformed CHD format: Metadata parsing threw an exception", ex); }

        ret.IsCDI = strs[1] == "CDI/2352";
        ret.SectorSize = GetSectorSize(ret.TrackType);
        ret.SubSize = GetSubSize(ret.SubType);
        ret.Padding = (0 - ret.Frames) & 3;
        return ret;
    }

    private static void ParseMetadataOld(ICollection<ChdCdMetadata> cdMetadatas, Span<byte> metadata) {
        var numTracks = BinaryPrimitives.ReadUInt32LittleEndian(metadata);
        var bigEndian = numTracks > 99; // apparently old metadata can appear as either little endian or big endian
        if (bigEndian)
            numTracks = BinaryPrimitives.ReverseEndianness(numTracks);
        if (numTracks > 99) throw new InvalidOperationException("Malformed CHD format: Invalid number of tracks");
        for (var i = 0; i < numTracks; i++) {
            var track = metadata[(4 + i * 24)..];
            var cdMetadata = new ChdCdMetadata { Track = 1U + (uint)i };
            if (bigEndian) {
                cdMetadata.TrackType = (LibChd.chd_track_type)BinaryPrimitives.ReadUInt32BigEndian(track);
                cdMetadata.SubType = (LibChd.chd_sub_type)BinaryPrimitives.ReadUInt32BigEndian(track[..4]);
                cdMetadata.SectorSize = BinaryPrimitives.ReadUInt32BigEndian(track[..8]);
                cdMetadata.SubSize = BinaryPrimitives.ReadUInt32BigEndian(track[..12]);
                cdMetadata.Frames = BinaryPrimitives.ReadUInt32BigEndian(track[..16]);
                cdMetadata.Padding = BinaryPrimitives.ReadUInt32BigEndian(track[..20]);
            }
            else {
                cdMetadata.TrackType = (LibChd.chd_track_type)BinaryPrimitives.ReadUInt32LittleEndian(track);
                cdMetadata.SubType = (LibChd.chd_sub_type)BinaryPrimitives.ReadUInt32LittleEndian(track[..4]);
                cdMetadata.SectorSize = BinaryPrimitives.ReadUInt32LittleEndian(track[..8]);
                cdMetadata.SubSize = BinaryPrimitives.ReadUInt32LittleEndian(track[..12]);
                cdMetadata.Frames = BinaryPrimitives.ReadUInt32LittleEndian(track[..16]);
                cdMetadata.Padding = BinaryPrimitives.ReadUInt32LittleEndian(track[..20]);
            }
            if (cdMetadata.SectorSize != GetSectorSize(cdMetadata.TrackType)) throw new InvalidOperationException("Malformed CHD format: Invalid sector size");
            if (cdMetadata.SubSize != GetSubSize(cdMetadata.SubType)) throw new InvalidOperationException("Malformed CHD format: Invalid sub size");
            var expectedPadding = (0 - cdMetadata.Frames) & 3;
            if (cdMetadata.Padding != expectedPadding) throw new InvalidOperationException("Malformed CHD format: Invalid padding value");
            cdMetadatas.Add(cdMetadata);
        }
    }

    public static ChdFile ParseFrom(string path) {
        var chdf = new ChdFile();
        try {
            // .NET Standard 2.0 doesn't have UnmanagedType.LPUTF8Str :(
            // (although .NET Framework has it just fine along with modern .NET)
            var nb = Encoding.UTF8.GetMaxByteCount(path.Length);
            var ptr = Marshal.AllocCoTaskMem(checked(nb + 1));
            try {
                unsafe {
                    fixed (char* c = path) {
                        var pbMem = (byte*)ptr;
                        var nbWritten = Encoding.UTF8.GetBytes(c, path.Length, pbMem!, nb);
                        pbMem[nbWritten] = 0;
                    }
                }
                var err = LibChd.chd_open(ptr, LibChd.CHD_OPEN_READ, IntPtr.Zero, out chdf.File);
                if (err != LibChd.chd_error.CHDERR_NONE) throw new InvalidOperationException($"Malformed CHD format: Failed to open chd, got error {err}");
                err = LibChd.chd_read_header(ptr, ref chdf.Header);
                if (err != LibChd.chd_error.CHDERR_NONE) throw new InvalidOperationException($"Malformed CHD format: Failed to read chd header, got error {err}");
            }
            finally { Marshal.FreeCoTaskMem(ptr); }
            if (chdf.Header.hunkbytes == 0 || chdf.Header.hunkbytes % LibChd.CD_FRAME_SIZE != 0) throw new InvalidOperationException("Malformed CHD format: Invalid hunk size");
            // chd-rs puts the correct value here for older versions of chds which don't have this, for newer chds, it is left as is, which might be invalid
            if (chdf.Header.unitbytes != LibChd.CD_FRAME_SIZE) throw new InvalidOperationException("Malformed CHD format: Invalid unit size");

            var metadataOutput = new byte[256];
            for (uint i = 0; i < 99; i++) {
                var err = LibChd.chd_get_metadata(chdf.File, LibChd.CDROM_TRACK_METADATA2_TAG, i, metadataOutput, (uint)metadataOutput.Length, out var resultLen, out _, out _);
                if (err == LibChd.chd_error.CHDERR_NONE) { var metadata = Encoding.ASCII.GetString(metadataOutput, 0, (int)resultLen).TrimEnd('\0'); chdf.CdMetadatas.Add(ParseMetadata2(metadata)); continue; }
                err = LibChd.chd_get_metadata(chdf.File, LibChd.CDROM_TRACK_METADATA_TAG, i, metadataOutput, (uint)metadataOutput.Length, out resultLen, out _, out _);
                if (err == LibChd.chd_error.CHDERR_NONE) { var metadata = Encoding.ASCII.GetString(metadataOutput, 0, (int)resultLen).TrimEnd('\0'); chdf.CdMetadatas.Add(ParseMetadata(metadata)); continue; }
                // if no more metadata, we're out of tracks
                break;
            }

            // validate track numbers
            if (chdf.CdMetadatas.Where((t, i) => t.Track != i + 1).Any()) throw new InvalidOperationException("Malformed CHD format: Invalid track number");

            if (chdf.CdMetadatas.Count == 0) {
                // if no metadata was present, we might have "old" metadata instead (which has all track info stored in one entry)
                metadataOutput = new byte[4 + 24 * 99];
                var err = LibChd.chd_get_metadata(chdf.File, LibChd.CDROM_OLD_METADATA_TAG, 0, metadataOutput, (uint)metadataOutput.Length, out var resultLen, out _, out _);
                if (err == LibChd.chd_error.CHDERR_NONE) {
                    if (resultLen != metadataOutput.Length) throw new InvalidOperationException("Malformed CHD format: Incorrect length for old metadata");
                    ParseMetadataOld(chdf.CdMetadatas, metadataOutput);
                }
            }

            if (chdf.CdMetadatas.Count == 0) throw new InvalidOperationException("Malformed CHD format: No tracks present in chd");

            // validation checks
            var chdExpectedNumSectors = 0L;
            foreach (var cdMetadata in chdf.CdMetadatas) {
                // if pregap is in the chd, then the reported frame count includes both pregap and track data
                if (cdMetadata.PregapInChd && cdMetadata.Pregap > cdMetadata.Frames) throw new InvalidOperationException("Malformed CHD format: Pregap in chd is larger than total sectors in chd track");
                chdExpectedNumSectors += cdMetadata.Frames + cdMetadata.Padding;
            }

            // pad expected sectors up to the next hunk
            var sectorsPerHunk = chdf.Header.hunkbytes / LibChd.CD_FRAME_SIZE;
            chdExpectedNumSectors = (chdExpectedNumSectors + sectorsPerHunk - 1) / sectorsPerHunk * sectorsPerHunk;

            var chdActualNumSectors = chdf.Header.hunkcount * sectorsPerHunk;
            if (chdExpectedNumSectors != chdActualNumSectors) throw new InvalidOperationException("Malformed CHD format: Mismatch in expected and actual number of sectors present");
            return chdf;
        }
        catch (Exception ex) {
            if (chdf.File != IntPtr.Zero) LibChd.chd_close(chdf.File);
            throw ex as InvalidOperationException ?? new("Malformed CHD format: An unknown exception was thrown while parsing", ex);
        }
    }

    public class LoadResults {
        public ChdFile ParsedCHDFile;
        public bool Valid;
        public Exception FailureException;
        public string ChdPath;
    }

    public static LoadResults LoadChdPath(FileSystem vfx, string path) {
        var ret = new LoadResults { ChdPath = path };
        try {
            if (!vfx.FileExists(path)) throw new InvalidOperationException("Malformed CHD format: Nonexistent CHD file!");
            ret.ParsedCHDFile = ParseFrom(path);
            ret.Valid = true;
        }
        catch (InvalidOperationException ex) { ret.FailureException = ex; }
        return ret;
    }

    /// <summary>
    /// CHD is dumb and byteswaps audio samples for some reason
    /// </summary>
    class SS_CHD_Audio : SS_Base {
        public override void Synth(SectorSynthJob job) {
            // read the sector user data
            if ((job.Parts & ESectorSynthPart.User2352) != 0) {
                Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2352);
                EndiannessUtils.MutatingByteSwap16(job.DestBuffer2448.AsSpan().Slice(job.DestOffset, 2352));
            }

            // if subcode is needed, synthesize it
            SynthSubchannelAsNeed(job);
        }
    }

    class SS_CHD_Sub(SS_Base baseSynth, uint subOffset, bool isInterleaved) : SectorSynthJob2448 {
        readonly SS_Base BaseSynth = baseSynth;
        readonly uint SubOffset = subOffset;
        readonly bool IsInterleaved = isInterleaved;

        public override void Synth(SectorSynthJob job) {
            if ((job.Parts & ESectorSynthPart.SubcodeAny) != 0) {
                BaseSynth.Blob.Read(BaseSynth.BlobOffset + SubOffset, job.DestBuffer2448, job.DestOffset + 2352, 96);
                job.Parts &= ~ESectorSynthPart.SubcodeAny;
                if ((job.Parts & ESectorSynthPart.SubcodeDeinterleave) != 0 && IsInterleaved) SynthUtils.DeinterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
                if ((job.Parts & ESectorSynthPart.SubcodeDeinterleave) == 0 && !IsInterleaved) SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
            }
            BaseSynth.Synth(job);
        }
    }

    internal static Disc LoadChdToDisc(FileSystem vfx, string chdPath, DiscMountPolicy discMountPolicy) {
        var loadResults = LoadChdPath(vfx, chdPath);
        if (!loadResults.Valid) throw loadResults.FailureException;

        var disc = new Disc();
        try {
            var chdf = loadResults.ParsedCHDFile;
            Blob blob = new Blob_CHD(vfx, chdf.File, chdf.Header.hunkbytes);
            disc.DisposableResources.Add(blob);

            // chds only support 1 session
            var session = new DiscSession { Number = 1 };
            var chdOffset = 0L;
            foreach (var cdMetadata in chdf.CdMetadatas) {
                RawTOCEntry EmitRawTOCEntry() {
                    var q = default(SubchannelQ);
                    var control = cdMetadata.TrackType != LibChd.chd_track_type.CD_TRACK_AUDIO ? EControlQ.DATA : EControlQ.None;
                    q.SetStatus(SubchannelQ.kADR, control);
                    q.q_tno = BCD2.FromDecimal(0);
                    q.q_index = BCD2.FromDecimal((int)cdMetadata.Track);
                    q.Timestamp = 0;
                    q.zero = 0;
                    q.AP_Timestamp = disc.Sectors.Count;
                    q.q_crc = 0;
                    return new() { QData = q };
                }

                static SS_Base CreateSynth(LibChd.chd_track_type trackType) => trackType switch {
                    LibChd.chd_track_type.CD_TRACK_MODE1 => new SS_Mode1_2048(),
                    LibChd.chd_track_type.CD_TRACK_MODE1_RAW => new SS_2352(),
                    LibChd.chd_track_type.CD_TRACK_MODE2 => new SS_Mode2_2336(),
                    LibChd.chd_track_type.CD_TRACK_MODE2_FORM1 => new SS_Mode2_Form1_2048(),
                    LibChd.chd_track_type.CD_TRACK_MODE2_FORM2 => new SS_Mode2_Form2_2324(),
                    LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX => new SS_Mode2_2336(),
                    LibChd.chd_track_type.CD_TRACK_MODE2_RAW => new SS_2352(),
                    LibChd.chd_track_type.CD_TRACK_AUDIO => new SS_CHD_Audio(),
                    _ => throw new InvalidOperationException(),
                };

                static CueTrackType ToCueTrackType(LibChd.chd_track_type chdTrackType, bool isCdi) => chdTrackType switch {
                    LibChd.chd_track_type.CD_TRACK_MODE1 => CueTrackType.Mode1_2048,
                    LibChd.chd_track_type.CD_TRACK_MODE1_RAW => CueTrackType.Mode1_2352,
                    LibChd.chd_track_type.CD_TRACK_MODE2 => CueTrackType.Mode2_2336,
                    LibChd.chd_track_type.CD_TRACK_MODE2_FORM1 => CueTrackType.Mode2_2336,
                    LibChd.chd_track_type.CD_TRACK_MODE2_FORM2 => CueTrackType.Mode2_2336,
                    LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX => CueTrackType.Mode2_2336,
                    LibChd.chd_track_type.CD_TRACK_MODE2_RAW when isCdi => CueTrackType.CDI_2352,
                    LibChd.chd_track_type.CD_TRACK_MODE2_RAW => CueTrackType.Mode2_2352,
                    LibChd.chd_track_type.CD_TRACK_AUDIO => CueTrackType.Audio,
                    _ => throw new InvalidOperationException(),
                };

                var pregapLength = cdMetadata.Pregap;
                // force 150 sector pregap for the first track if not present in the chd
                if (!cdMetadata.PregapInChd && cdMetadata.Track == 1) {
                    cdMetadata.PregapTrackType = cdMetadata.TrackType;
                    cdMetadata.PregapSubType = cdMetadata.SubType;
                    pregapLength = 150;
                }

                var relMSF = -pregapLength;
                for (var i = 0; i < pregapLength; i++) {
                    SS_Base synth;
                    if (cdMetadata.PregapInChd) { synth = CreateSynth(cdMetadata.PregapTrackType); synth.Blob = blob; synth.BlobOffset = chdOffset; }
                    else synth = new SS_Gap { TrackType = ToCueTrackType(cdMetadata.PregapTrackType, cdMetadata.IsCDI) };

                    synth.Policy = discMountPolicy;
                    var control = cdMetadata.PregapTrackType != LibChd.chd_track_type.CD_TRACK_AUDIO ? EControlQ.DATA : EControlQ.None;
                    synth.sq.SetStatus(SubchannelQ.kADR, control);
                    synth.sq.q_tno = BCD2.FromDecimal((int)cdMetadata.Track);
                    synth.sq.q_index = BCD2.FromDecimal(0);
                    synth.sq.Timestamp = !discMountPolicy.Cue_PregapContradictionModeA ? (int)relMSF + 1 : (int)relMSF;
                    synth.sq.zero = 0;
                    synth.sq.AP_Timestamp = disc.Sectors.Count;
                    synth.sq.q_crc = 0;
                    synth.Pause = true;

                    if (cdMetadata.PregapInChd) {
                        // wrap the base synth with our special synth if we have subcode in the chd
                        SectorSynthJob2448 chdSynth = cdMetadata.PregapSubType switch {
                            LibChd.chd_sub_type.CD_SUB_NORMAL => new SS_CHD_Sub(synth, GetSectorSize(cdMetadata.PregapTrackType), isInterleaved: false),
                            LibChd.chd_sub_type.CD_SUB_RAW => new SS_CHD_Sub(synth, GetSectorSize(cdMetadata.PregapTrackType), isInterleaved: true),
                            LibChd.chd_sub_type.CD_SUB_NONE => synth,
                            _ => throw new InvalidOperationException(),
                        };
                        disc.Sectors.Add(chdSynth);
                        chdOffset += LibChd.CD_FRAME_SIZE;
                    }
                    else disc.Sectors.Add(synth);
                    relMSF++;
                }

                session.RawTOCEntries.Add(EmitRawTOCEntry());

                var trackLength = cdMetadata.Frames;
                if (cdMetadata.PregapInChd) trackLength -= pregapLength;

                for (var i = 0; i < trackLength; i++) {
                    var synth = CreateSynth(cdMetadata.TrackType);
                    synth.Blob = blob;
                    synth.BlobOffset = chdOffset;
                    synth.Policy = discMountPolicy;
                    var control = cdMetadata.TrackType != LibChd.chd_track_type.CD_TRACK_AUDIO ? EControlQ.DATA : EControlQ.None;
                    synth.sq.SetStatus(SubchannelQ.kADR, control);
                    synth.sq.q_tno = BCD2.FromDecimal((int)cdMetadata.Track);
                    synth.sq.q_index = BCD2.FromDecimal(1);
                    synth.sq.Timestamp = (int)relMSF;
                    synth.sq.zero = 0;
                    synth.sq.AP_Timestamp = disc.Sectors.Count;
                    synth.sq.q_crc = 0;
                    synth.Pause = false;
                    SectorSynthJob2448 chdSynth = cdMetadata.SubType switch {
                        LibChd.chd_sub_type.CD_SUB_NORMAL => new SS_CHD_Sub(synth, cdMetadata.SectorSize, isInterleaved: false),
                        LibChd.chd_sub_type.CD_SUB_RAW => new SS_CHD_Sub(synth, cdMetadata.SectorSize, isInterleaved: true),
                        LibChd.chd_sub_type.CD_SUB_NONE => synth,
                        _ => throw new InvalidOperationException(),
                    };
                    disc.Sectors.Add(chdSynth);
                    chdOffset += LibChd.CD_FRAME_SIZE;
                    relMSF++;
                }

                chdOffset += cdMetadata.Padding * LibChd.CD_FRAME_SIZE;

                for (var i = 0; i < cdMetadata.PostGap; i++) {
                    var synth = new SS_Gap { TrackType = ToCueTrackType(cdMetadata.TrackType, cdMetadata.IsCDI), Policy = discMountPolicy };
                    var control = cdMetadata.TrackType != LibChd.chd_track_type.CD_TRACK_AUDIO ? EControlQ.DATA : EControlQ.None;
                    synth.sq.SetStatus(SubchannelQ.kADR, control);
                    synth.sq.q_tno = BCD2.FromDecimal((int)cdMetadata.Track);
                    synth.sq.q_index = BCD2.FromDecimal(2);
                    synth.sq.Timestamp = (int)relMSF;
                    synth.sq.zero = 0;
                    synth.sq.AP_Timestamp = disc.Sectors.Count;
                    synth.sq.q_crc = 0;
                    synth.Pause = true;
                    disc.Sectors.Add(synth);
                    relMSF++;
                }
            }

            DiscSessionFormat GuessSessionFormat() {
                foreach (var cdMetadata in chdf.CdMetadatas)
                    if (cdMetadata.IsCDI) return DiscSessionFormat.Type10_CDI;
                    else if (cdMetadata.TrackType is LibChd.chd_track_type.CD_TRACK_MODE2
                        or LibChd.chd_track_type.CD_TRACK_MODE2_FORM1
                        or LibChd.chd_track_type.CD_TRACK_MODE2_FORM2
                        or LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX
                        or LibChd.chd_track_type.CD_TRACK_MODE2_RAW) {
                        return DiscSessionFormat.Type20_CDXA;
                    }
                return DiscSessionFormat.Type00_CDROM_CDDA;
            }

            var TOCMiscInfo = new Synthesize_A0A1A2(
                firstRecordedTrackNumber: 1,
                lastRecordedTrackNumber: chdf.CdMetadatas.Count,
                sessionFormat: GuessSessionFormat(),
                leadoutTimestamp: disc.Sectors.Count);
            TOCMiscInfo.Run(session.RawTOCEntries);
            disc.Sessions.Add(session);
            return disc;
        }
        catch { disc.Dispose(); throw; }
    }

    // crc16 table taken from https://github.com/mamedev/mame/blob/26b5eb211924acbe4b78f67da8d0ae3cbe77aa6d/src/lib/util/hashing.cpp#L400C1-L434C4
    static readonly ushort[] _crc16Table = {
            0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7,
            0x8108, 0x9129, 0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF,
            0x1231, 0x0210, 0x3273, 0x2252, 0x52B5, 0x4294, 0x72F7, 0x62D6,
            0x9339, 0x8318, 0xB37B, 0xA35A, 0xD3BD, 0xC39C, 0xF3FF, 0xE3DE,
            0x2462, 0x3443, 0x0420, 0x1401, 0x64E6, 0x74C7, 0x44A4, 0x5485,
            0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE, 0xF5CF, 0xC5AC, 0xD58D,
            0x3653, 0x2672, 0x1611, 0x0630, 0x76D7, 0x66F6, 0x5695, 0x46B4,
            0xB75B, 0xA77A, 0x9719, 0x8738, 0xF7DF, 0xE7FE, 0xD79D, 0xC7BC,
            0x48C4, 0x58E5, 0x6886, 0x78A7, 0x0840, 0x1861, 0x2802, 0x3823,
            0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B,
            0x5AF5, 0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0x0A50, 0x3A33, 0x2A12,
            0xDBFD, 0xCBDC, 0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A,
            0x6CA6, 0x7C87, 0x4CE4, 0x5CC5, 0x2C22, 0x3C03, 0x0C60, 0x1C41,
            0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD, 0xAD2A, 0xBD0B, 0x8D68, 0x9D49,
            0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13, 0x2E32, 0x1E51, 0x0E70,
            0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A, 0x9F59, 0x8F78,
            0x9188, 0x81A9, 0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E, 0xE16F,
            0x1080, 0x00A1, 0x30C2, 0x20E3, 0x5004, 0x4025, 0x7046, 0x6067,
            0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C, 0xE37F, 0xF35E,
            0x02B1, 0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256,
            0xB5EA, 0xA5CB, 0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D,
            0x34E2, 0x24C3, 0x14A0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
            0xA7DB, 0xB7FA, 0x8799, 0x97B8, 0xE75F, 0xF77E, 0xC71D, 0xD73C,
            0x26D3, 0x36F2, 0x0691, 0x16B0, 0x6657, 0x7676, 0x4615, 0x5634,
            0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9, 0xB98A, 0xA9AB,
            0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x08E1, 0x3882, 0x28A3,
            0xCB7D, 0xDB5C, 0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A,
            0x4A75, 0x5A54, 0x6A37, 0x7A16, 0x0AF1, 0x1AD0, 0x2AB3, 0x3A92,
            0xFD2E, 0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B, 0x9DE8, 0x8DC9,
            0x7C26, 0x6C07, 0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0x0CC1,
            0xEF1F, 0xFF3E, 0xCF5D, 0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8,
            0x6E17, 0x7E36, 0x4E55, 0x5E74, 0x2E93, 0x3EB2, 0x0ED1, 0x1EF0,
        };

    static ushort CalcCrc16(ReadOnlySpan<byte> bytes) {
        ushort crc16 = 0xFFFF;
        foreach (var b in bytes) crc16 = (ushort)((crc16 << 8) ^ _crc16Table[(crc16 >> 8) ^ b]);
        return crc16;
    }

    class ChdHunkMapEntry {
        public uint CompressedLength;
        public long HunkOffset;
        public ushort Crc16;
    }

    static readonly byte[] _chdTag = Encoding.ASCII.GetBytes("MComprHD");

    // 8 frames is apparently the standard, but we can probably afford to go a little extra ;)
    const uint CD_FRAMES_PER_HUNK = 75; // 1 second

    static void Dump(Disc disc, string path) {
        // check if we have a multisession disc (CHD doesn't support those)
        if (disc.Sessions.Count > 2) throw new NotSupportedException("CHD does not support multisession discs");

        using var s = File.Create(path);
        using var w = new BinaryWriter(s);

        // write header
        // note CHD header has values in big endian, while BinaryWriter will write in little endian
        w.Write(_chdTag);
        w.Write(BinaryPrimitives.ReverseEndianness(LibChd.CHD_V5_HEADER_SIZE));
        w.Write(BinaryPrimitives.ReverseEndianness(LibChd.CHD_HEADER_VERSION));
        // v5 chd allows for 4 different compression types
        // we only have 1 implemented here
        w.Write(BinaryPrimitives.ReverseEndianness(LibChd.CHD_CODEC_ZSTD));
        w.Write(0);
        w.Write(0);
        w.Write(0);
        w.Write(0L); // total size of all uncompressed data (written later)
        w.Write(0L); // offset to hunk map (written later)
        w.Write(0L); // offset to first metadata (written later)
        w.Write(BinaryPrimitives.ReverseEndianness(LibChd.CD_FRAME_SIZE * CD_FRAMES_PER_HUNK)); // bytes per hunk
        w.Write(BinaryPrimitives.ReverseEndianness(LibChd.CD_FRAME_SIZE)); // bytes per sector (always CD_FRAME_SIZE)
        var blankSha1 = new byte[LibChd.CHD_SHA1_BYTES];
        w.Write(blankSha1); // SHA1 of raw data (written later)
        w.Write(blankSha1); // SHA1 of raw data + metadata (written later)
        w.Write(blankSha1); // SHA1 of raw data + metadata for parent (N/A, always 0 for us)

        // collect metadata
        var cdMetadatas = new List<ChdCdMetadata>();
        var session = disc.Session1;
        for (var i = 1; i <= session.InformationTrackCount; i++) {
            var track = session.Tracks[i];
            var firstIndexLba = track.Number == 1 ? 0 : track.LBA;
            var cdMetadata = new ChdCdMetadata {
                Track = (uint)track.Number,
                IsCDI = track.Mode == 2 && session.TOC.SessionFormat == DiscSessionFormat.Type10_CDI,
                TrackType = track.Mode switch {
                    0 => LibChd.chd_track_type.CD_TRACK_AUDIO,
                    1 => LibChd.chd_track_type.CD_TRACK_MODE1_RAW,
                    2 => LibChd.chd_track_type.CD_TRACK_MODE2_RAW,
                    _ => throw new InvalidOperationException(),
                },
                SubType = LibChd.chd_sub_type.CD_SUB_RAW,
                SectorSize = 2352,
                SubSize = 96,
                Frames = (uint)(track.NextTrack.LBA - firstIndexLba),
                Pregap = (uint)(track.LBA - firstIndexLba),
                PostGap = 0,
            };
            cdMetadata.PregapInChd = cdMetadata.Pregap > 0;
            cdMetadata.Padding = (0 - cdMetadata.Frames) & 3;
            cdMetadata.PregapTrackType = cdMetadata.TrackType;
            cdMetadatas.Add(cdMetadata);
        }

        var hunkMapEntries = new List<ChdHunkMapEntry>();
        using var sha1Inc = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        //using var zstd = new Zstd(); //: throw new NotImplementedException();
        var dsr = new DiscSectorReader(disc) { Policy = { DeinterleavedSubcode = true, DeterministicClearBuffer = true } };
        var sectorBuf = new byte[LibChd.CD_FRAME_SIZE];
        var cdLba = 0;
        uint chdLba = 0, chdPos;
        var curHunk = new byte[LibChd.CD_FRAME_SIZE * CD_FRAMES_PER_HUNK];

        void EndHunk(uint hashLen) {
            var hunkOffset = w.BaseStream.Position;
            throw new NotImplementedException();
            //using (var cstream = zstd.CreateZstdCompressionStream(w.BaseStream, 0))
            //    cstream.Write(curHunk, 0, curHunk.Length);
            hunkMapEntries.Add(new() {
                CompressedLength = (uint)(w.BaseStream.Position - hunkOffset),
                HunkOffset = hunkOffset,
                Crc16 = CalcCrc16(curHunk),
            });
            sha1Inc.AppendData(curHunk, 0, (int)hashLen);
            Array.Clear(curHunk, 0, curHunk.Length);
        }

        foreach (var cdMetadata in cdMetadatas) {
            for (var i = 0; i < cdMetadata.Frames; i++) {
                dsr.ReadLBA_2448(cdLba, sectorBuf, 0);
                // byteswapped audio samples if needed
                var trackType = i < cdMetadata.Pregap ? cdMetadata.PregapTrackType : cdMetadata.TrackType;
                if (trackType == LibChd.chd_track_type.CD_TRACK_AUDIO) EndiannessUtils.MutatingByteSwap16(sectorBuf.AsSpan()[..2352]);
                chdPos = chdLba % CD_FRAMES_PER_HUNK;
                Buffer.BlockCopy(sectorBuf, 0, curHunk, (int)(LibChd.CD_FRAME_SIZE * chdPos), (int)LibChd.CD_FRAME_SIZE);
                if (chdPos == CD_FRAMES_PER_HUNK - 1) EndHunk(CD_FRAMES_PER_HUNK * LibChd.CD_FRAME_SIZE);
                cdLba++;
                chdLba++;
            }

            for (var i = 0; i < cdMetadata.Padding; i++) {
                chdPos = chdLba % CD_FRAMES_PER_HUNK;
                if (chdPos == CD_FRAMES_PER_HUNK - 1) EndHunk(CD_FRAMES_PER_HUNK * LibChd.CD_FRAME_SIZE);
                chdLba++;
            }
        }

        // write out any remaining pending hunk
        chdPos = chdLba % CD_FRAMES_PER_HUNK;
        if (chdPos != 0) EndHunk(chdPos * LibChd.CD_FRAME_SIZE);

        static string TrackTypeStr(LibChd.chd_track_type trackType, bool isCdi) => trackType switch {
            LibChd.chd_track_type.CD_TRACK_AUDIO => "AUDIO",
            LibChd.chd_track_type.CD_TRACK_MODE1_RAW => "MODE1_RAW",
            LibChd.chd_track_type.CD_TRACK_MODE2_RAW when isCdi => "CDI/2352",
            LibChd.chd_track_type.CD_TRACK_MODE2_RAW => "MODE2_RAW",
            _ => throw new InvalidOperationException(),
        };

        var metadataOffset = w.BaseStream.Position;
        var metadataHashes = new byte[cdMetadatas.Count][];
        // write metadata
        for (var i = 0; i < cdMetadatas.Count; i++) {
            var cdMetadata = cdMetadatas[i];
            var trackType = TrackTypeStr(cdMetadata.TrackType, cdMetadata.IsCDI);
            var pgTrackType = TrackTypeStr(cdMetadata.PregapTrackType, cdMetadata.IsCDI);
            if (cdMetadata.PregapInChd) pgTrackType = $"V{pgTrackType}";
            var metadataStr = $"TRACK:{cdMetadata.Track} TYPE:{trackType} SUBTYPE:RW FRAMES:{cdMetadata.Frames} PREGAP:{cdMetadata.Pregap} PGTYPE:{pgTrackType} PGSUB:RW POSTGAP:0\0";
            var metadataBytes = Encoding.ASCII.GetBytes(metadataStr);
            w.Write(BinaryPrimitives.ReverseEndianness(LibChd.CDROM_TRACK_METADATA2_TAG));
            w.Write(LibChd.CHD_MDFLAGS_CHECKSUM);
            var chunkDataSize = new byte[3]; // 24 bit integer
            chunkDataSize[0] = (byte)((metadataBytes.Length >> 16) & 0xFF);
            chunkDataSize[1] = (byte)((metadataBytes.Length >> 8) & 0xFF);
            chunkDataSize[2] = (byte)(metadataBytes.Length & 0xFF);
            w.Write(chunkDataSize);
            w.Write(i == cdMetadatas.Count - 1
                ? 0L // last chunk
                : BinaryPrimitives.ReverseEndianness(w.BaseStream.Position + 8 + metadataBytes.Length)); // offset to next chunk
            w.Write(metadataBytes);
            throw new NotImplementedException(); //metadataHashes[i] = SHA1Checksum.Compute(metadataBytes);
        }

        var uncompressedHunkMap = new byte[hunkMapEntries.Count * 12];
        // compute uncompressed hunk map
        for (var i = 0; i < hunkMapEntries.Count; i++) {
            var hunkMapEntry = hunkMapEntries[i];
            var mapEntryOffset = i * 12;
            uncompressedHunkMap[mapEntryOffset + 0] = 0; // Codec 0
            uncompressedHunkMap[mapEntryOffset + 1] = (byte)((hunkMapEntry.CompressedLength >> 16) & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 2] = (byte)((hunkMapEntry.CompressedLength >> 8) & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 3] = (byte)(hunkMapEntry.CompressedLength & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 4] = (byte)((hunkMapEntry.HunkOffset >> 40) & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 5] = (byte)((hunkMapEntry.HunkOffset >> 32) & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 6] = (byte)((hunkMapEntry.HunkOffset >> 24) & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 7] = (byte)((hunkMapEntry.HunkOffset >> 16) & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 8] = (byte)((hunkMapEntry.HunkOffset >> 8) & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 9] = (byte)(hunkMapEntry.HunkOffset & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 10] = (byte)((hunkMapEntry.Crc16 >> 8) & 0xFF);
            uncompressedHunkMap[mapEntryOffset + 11] = (byte)(hunkMapEntry.Crc16 & 0xFF);
        }

        var hunkMapCrc16 = CalcCrc16(uncompressedHunkMap);
        var hunkMapOffset = w.BaseStream.Position;

        var firstOffset = new byte[6];
        // write hunk map header
        w.Write(0); // compressed map length (written later)
        Buffer.BlockCopy(uncompressedHunkMap, 4, firstOffset, 0, 6);
        w.Write(firstOffset); // first hunk offset
        w.Write(BinaryPrimitives.ReverseEndianness(hunkMapCrc16)); // uncompressed map crc16
        w.Write((byte)24); // num bits used to stored compression length
        w.Write((byte)0); // num bits used to stored self refs (not used)
        w.Write((byte)0); // num bits used to stored parent unit refs (not used)
        w.Write((byte)0); // reserved (should just be 0)

        // huffman map
        w.Write((byte)0x11); // makes command 0 take 1 bit
        // basic bit writing code
        byte curByte = 0;
        var curBit = 60 + hunkMapEntries.Count;
        while (curBit >= 8) { w.Write((byte)0); curBit -= 8; }

        void WriteByteBits(byte b) {
            for (var i = 0; i < 8; i++) {
                var bit = ((b >> (7 - i)) & 1) != 0;
                if (bit) curByte |= (byte)(1 << (7 - curBit));
                curBit++;
                if (curBit == 8) { w.Write(curByte); curBit = 0; curByte = 0; }
            }
        }

        for (var i = 0; i < hunkMapEntries.Count; i++) {
            var mapEntryOffset = i * 12;
            // length
            WriteByteBits(uncompressedHunkMap[mapEntryOffset + 1]);
            WriteByteBits(uncompressedHunkMap[mapEntryOffset + 2]);
            WriteByteBits(uncompressedHunkMap[mapEntryOffset + 3]);
            // crc16
            WriteByteBits(uncompressedHunkMap[mapEntryOffset + 10]);
            WriteByteBits(uncompressedHunkMap[mapEntryOffset + 11]);
        }

        // write final byte if present
        if (curBit != 0) w.Write(curByte);

        // finish everything up

        var hunkMapEnd = w.BaseStream.Position;
        w.BaseStream.Seek(hunkMapOffset, SeekOrigin.Begin);
        // hunk map length sans header
        w.Write(BinaryPrimitives.ReverseEndianness((uint)(hunkMapEnd - hunkMapOffset - 16)));
        w.BaseStream.Seek(0x20, SeekOrigin.Begin);
        w.Write(BinaryPrimitives.ReverseEndianness(chdLba * (long)LibChd.CD_FRAME_SIZE));
        w.Write(BinaryPrimitives.ReverseEndianness(hunkMapOffset));
        w.Write(BinaryPrimitives.ReverseEndianness(metadataOffset));

        var rawSha1 = sha1Inc.GetHashAndReset();
        // calc overall sha1 now (uses raw sha1 and metadata hashes)
        sha1Inc.AppendData(rawSha1);
        // apparently these are expected to be sorted with memcmp semantics
        Array.Sort(metadataHashes, static (x, y) => {
            for (var i = 0; i < x.Length; i++)
                if (x[i] < y[i]) return -1;
                else if (x[i] > y[i]) return 1;
            return 0;
        });

        // tag is hashed alongside the hash
        var metadataTag = new byte[4];
        metadataTag[0] = (byte)((LibChd.CDROM_TRACK_METADATA2_TAG >> 24) & 0xFF);
        metadataTag[1] = (byte)((LibChd.CDROM_TRACK_METADATA2_TAG >> 16) & 0xFF);
        metadataTag[2] = (byte)((LibChd.CDROM_TRACK_METADATA2_TAG >> 8) & 0xFF);
        metadataTag[3] = (byte)(LibChd.CDROM_TRACK_METADATA2_TAG & 0xFF);
        foreach (var metadataHash in metadataHashes) {
            sha1Inc.AppendData(metadataTag);
            sha1Inc.AppendData(metadataHash);
        }

        var overallSha1 = sha1Inc.GetHashAndReset();
        w.BaseStream.Seek(0x40, SeekOrigin.Begin);
        w.Write(rawSha1);
        w.Write(overallSha1);
    }
}

#endregion

#region Disk : CueFormat

class CueFormat {
    public static Disc LoadCue(DiscMount mount, FileSystem vfx, Stream stream) {
        var cue = new CueFile(stream);
        if (cue.HasError) return null;
        var cmp = new CueCompiler(cue, new DiscContext(vfx, ""));
        if (cmp.HasError) return null;
        if (cmp.LoadTime > mount.SlowLoadAbortThreshold) { mount.SlowLoadAborted = true; Warn("Loading terminated due to slow load threshold"); return null; }
        return new CueDisc(vfx, cmp);
    }

    /// <summary>
    /// CueLineParser
    /// </summary>
    /// <param name="line"></param>
    class CueLineParser(string line) {
        enum Mode { Normal, Quotable }

        int index;
        readonly string str = line;
        public bool Eof;

        public string ReadPath() => ReadToken(Mode.Quotable);
        public string ReadToken() => ReadToken(Mode.Normal);
        public string ReadLine() { var len = str.Length; var ret = str[index..len]; index = len; Eof = true; return ret; }
        string ReadToken(Mode mode) {
            if (Eof) return null;
            var isPath = mode == Mode.Quotable;
            var startIndex = index;
            bool inToken = false, inQuote = false;
            while (true) {
                var done = false;
                var c = str[index];
                var isWhiteSpace = c is ' ' or '\t';
                if (isWhiteSpace) {
                    if (inQuote) index++;
                    else {
                        if (inToken) done = true;
                        else index++;
                    }
                }
                else {
                    var startedQuote = false;
                    if (!inToken) {
                        startIndex = index;
                        if (isPath && c == '"') startedQuote = inQuote = true;
                        inToken = true;
                    }
                    switch (str[index]) {
                        case '"': index++; if (inQuote && !startedQuote) done = true; break;
                        case '\\': index++; break;
                        default: index++; break;
                    }
                }
                if (index == str.Length) { Eof = true; done = true; }
                if (done) break;
            }
            return mode == Mode.Quotable ? str[startIndex..index].Trim('"') : str[startIndex..index];
        }
    }

    /// <summary>
    /// CueFile
    /// </summary>
    class CueFile {
        #region Records

        /// <summary>
        /// Command
        /// </summary>
        public interface Command { }

        public readonly struct CATALOG(string value) : Command {
            public readonly string Value = value;
            public readonly override string ToString() => $"CATALOG: {Value}";
        }

        public readonly struct CDTEXTFILE(string path) : Command {
            public readonly string Path = path;
            public override string ToString() => $"CDTEXTFILE: {Path}";
        }

        public readonly struct FILE(string path, CueFileType type) : Command {
            public readonly string Path = path;
            public readonly CueFileType Type = type;
            public override string ToString() => $"FILE ({Type}): {Path}";
        }

        public readonly struct FLAGS(CueTrackFlags flags) : Command {
            public readonly CueTrackFlags Flags = flags;
            public override string ToString() => $"FLAGS {Flags}";
        }

        public readonly struct INDEX(int number, MSF timestamp) : Command {
            public readonly int Number = number;
            public readonly MSF Timestamp = timestamp;
            public override string ToString() => $"INDEX {Number,2} {Timestamp}";
        }

        public readonly struct ISRC(string value) : Command {
            public readonly string Value = value;
            public override string ToString() => $"ISRC: {Value}";
        }

        public readonly struct PERFORMER(string value) : Command {
            public readonly string Value = value;
            public override string ToString() => $"PERFORMER: {Value}";
        }

        public readonly struct POSTGAP(MSF length) : Command {
            public readonly MSF Length = length;
            public override string ToString() => $"POSTGAP: {Length}";
        }

        public readonly struct PREGAP(MSF length) : Command {
            public readonly MSF Length = length;
            public override string ToString() => $"PREGAP: {Length}";
        }

        public readonly struct REM(string value) : Command {
            public readonly string Value = value;
            public override string ToString() => $"REM: {Value}";
        }

        public readonly struct COMMENT(string value) : Command {
            public readonly string Value = value;
            public override string ToString() => $"COMMENT: {Value}";
        }

        public readonly struct SONGWRITER(string value) : Command {
            public readonly string Value = value;
            public override string ToString() => $"SONGWRITER: {Value}";
        }

        public readonly struct TITLE(string value) : Command {
            public readonly string Value = value;
            public override string ToString() => $"TITLE: {Value}";
        }

        public readonly struct TRACK(int number, CueTrackType type) : Command {
            public readonly int Number = number;
            public readonly CueTrackType Type = type;
            public override string ToString() => $"TRACK {Number,2} ({Type})";
        }

        public readonly struct SESSION(int number) : Command {
            public readonly int Number = number;
            public override string ToString() => $"SESSION {Number}";
        }

        #endregion

        /// <summary>
        /// The sequential list of commands parsed out of the cue file
        /// </summary>
        public List<Command> Commands = [];
        public CATALOG? Catalog;
        public ISRC? ISrc;
        public CDTEXTFILE? CDTextFile;
        public bool HasError = false;

        void Error(string message) { HasError = true; Debug.Error(message); }

        public CueFile(Stream stream, bool strict = false) {
            var s = (TextReader)new StreamReader(stream, Encoding.UTF8);
            var lineNo = 0; string z;
            while (true) {
                lineNo++;
                var line = s.ReadLine()?.Trim();
                if (line is null) break;
                else if (line.Length is 0) continue;
                var p = new CueLineParser(line);
                var key = p.ReadToken().ToUpperInvariant();
                // remove nonsense at beginning
                if (!strict)
                    while (key.Length > 0) {
                        var c = key[0];
                        if (c == ';' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) break;
                        key = key[1..];
                    }
                if (key.StartsWith(';')) { p.Eof = true; Commands.Add(new COMMENT(line)); }
                else switch (key) {
                        default: Warn($"Unknown command: {key}"); break;
                        case "CATALOG":
                            if (Catalog != null) Warn("Multiple CATALOG commands detected. Subsequent ones are ignored.");
                            else if (p.Eof) Warn("Ignoring empty CATALOG command");
                            else Commands.Add(Catalog = new CATALOG(p.ReadToken()));
                            break;
                        case "CDTEXTFILE":
                            if (CDTextFile != null) Warn("Multiple CDTEXTFILE commands detected. Subsequent ones are ignored.");
                            else if (p.Eof) Warn("Ignoring empty CDTEXTFILE command");
                            else Commands.Add(CDTextFile = new CDTEXTFILE(p.ReadPath()));
                            break;
                        case "FILE": {
                                var path = p.ReadPath();
                                CueFileType ft;
                                if (p.Eof) { Error("FILE command is missing file type."); ft = CueFileType.Unspecified; }
                                else
                                    switch (z = p.ReadToken().ToUpperInvariant()) {
                                        default: Error($"Unknown FILE type: {z}"); ft = CueFileType.Unspecified; break;
                                        case "BINARY": ft = CueFileType.BINARY; break;
                                        case "MOTOROLA": ft = CueFileType.MOTOROLA; break;
                                        case "BINARAIFF": ft = CueFileType.AIFF; break;
                                        case "WAVE": ft = CueFileType.WAVE; break;
                                        case "MP3": ft = CueFileType.MP3; break;
                                    }
                                Commands.Add(new FILE(path, ft));
                            }
                            break;
                        case "FLAGS": {
                                CueTrackFlags flags = default;
                                while (!p.Eof)
                                    switch (z = p.ReadToken().ToUpperInvariant()) {
                                        case "DATA":
                                        default: Warn($"Unknown FLAG: {z}"); break;
                                        case "DCP": flags |= CueTrackFlags.DCP; break;
                                        case "4CH": flags |= CueTrackFlags._4CH; break;
                                        case "PRE": flags |= CueTrackFlags.PRE; break;
                                        case "SCMS": flags |= CueTrackFlags.SCMS; break;
                                    }
                                if (flags == CueTrackFlags.None) Warn("Empty FLAG command");
                                Commands.Add(new FLAGS(flags));
                            }
                            break;
                        case "INDEX": {
                                if (p.Eof) { Error("Incomplete INDEX command"); break; }
                                if (!int.TryParse(z = p.ReadToken(), out var indexnum) || indexnum < 0 || indexnum > 99) { Error($"Invalid INDEX number: {z}"); break; }
                                z = p.ReadToken();
                                var ts = new MSF(z);
                                if (!ts.Valid && !strict) { z = Regex.Replace(z, "[^0-9:]", ""); ts = new MSF(z); } // try cleaning it up
                                if (!ts.Valid) { if (strict) Error($"Invalid INDEX timestamp: {z}"); break; }
                                Commands.Add(new INDEX(indexnum, ts));
                            }
                            break;
                        case "ISRC":
                            if (ISrc != null) Warn("Multiple ISRC commands detected. Subsequent ones are ignored.");
                            else if (p.Eof) Warn("Ignoring empty ISRC command");
                            else {
                                var isrc = p.ReadToken();
                                if (isrc.Length != 12) Warn($"Invalid ISRC code ignored: {isrc}");
                                else Commands.Add(ISrc = new ISRC(isrc));
                            }
                            break;
                        case "PERFORMER":
                            Commands.Add(new PERFORMER(p.ReadPath() ?? ""));
                            break;
                        case "POSTGAP":
                        case "PREGAP": {
                                z = p.ReadToken();
                                var msf = new MSF(z);
                                if (!msf.Valid) Error($"Ignoring {key} with invalid length MSF: {z}");
                                else Commands.Add(key == "POSTGAP" ? new POSTGAP(msf) : new PREGAP(msf));
                            }
                            break;
                        case "REM": {
                                var comment = p.ReadLine();
                                // if the comment starts with SESSION, interpret that as a session "command"
                                var trimmed = comment.Trim();
                                if (trimmed.StartsWith("SESSION ", StringComparison.OrdinalIgnoreCase) && int.TryParse(trimmed[8..], out var number) && number > 0) { Commands.Add(new SESSION(number)); break; }
                                Commands.Add(new REM(comment));
                                break;
                            }
                        case "SONGWRITER":
                            Commands.Add(new SONGWRITER(p.ReadPath() ?? ""));
                            break;
                        case "TITLE":
                            Commands.Add(new TITLE(p.ReadPath() ?? ""));
                            break;
                        case "TRACK": {
                                if (p.Eof) { Error("Incomplete TRACK command"); break; }
                                if (!int.TryParse(z = p.ReadToken(), out var tracknum) || tracknum is < 1 or > 99) { Error($"Invalid TRACK number: {z}"); break; }
                                CueTrackType tt;
                                switch ((z = p.ReadToken()).ToUpperInvariant()) {
                                    default: Error($"Unknown TRACK type: {z}"); tt = CueTrackType.Unknown; break;
                                    case "AUDIO": tt = CueTrackType.Audio; break;
                                    case "CDG": tt = CueTrackType.CDG; break;
                                    case "MODE1/2048": tt = CueTrackType.Mode1_2048; break;
                                    case "MODE1/2352": tt = CueTrackType.Mode1_2352; break;
                                    case "MODE2/2336": tt = CueTrackType.Mode2_2336; break;
                                    case "MODE2/2352": tt = CueTrackType.Mode2_2352; break;
                                    case "CDI/2336": tt = CueTrackType.CDI_2336; break;
                                    case "CDI/2352": tt = CueTrackType.CDI_2352; break;
                                }
                                Commands.Add(new TRACK(tracknum, tt));
                            }
                            break;
                    }
                if (!p.Eof) {
                    var remainder = p.ReadLine();
                    if (remainder.TrimStart().StartsWith(';')) Commands.Add(new COMMENT(remainder));
                    else Warn($"Unknown text at end of line after processing command: {key}");
                }
            }
        }
    }

    [Flags]
    public enum CueTrackFlags {
        None = 0,
        PRE = 1, // Pre-emphasis enabled (audio tracks only)
        DCP = 2, // Digital copy permitted
        DATA = 4, // Set automatically by cue-processing equipment, here for completeness
        _4CH = 8, // Four channel audio
        SCMS = 64, // Serial copy management system (not supported by all recorders) (??)
    }

    public enum CueFileType {
        Unspecified,
        BINARY, // Intel binary file (least significant byte first)
        MOTOROLA, // Motorola binary file (most significant byte first)
        AIFF, // Audio AIFF file
        WAVE, // Audio WAVE file
        MP3, // Audio MP3 file
    }

    public enum CueTrackType {
        Unknown,
        Audio, // Audio/Music (2352)
        CDG, // Karaoke CD+G (2448)
        Mode1_2048, // CDROM Mode1 Data (cooked)
        Mode1_2352, // CDROM Mode1 Data (raw)
        Mode2_2336, // CDROM-XA Mode2 Data (could contain form 1 or form 2)
        Mode2_2352, // CDROM-XA Mode2 Data (but there's no reason to distinguish this from Mode1_2352 other than to alert us that the entire session should be XA
        CDI_2336, // CDI Mode2 Data
        CDI_2352, // CDI Mode2 Data
    }

    public class CueFileResolver(FileSystem vfx, string baseDir) {
        string BaseDir = baseDir;
        string[] BaseDirPaths = [.. vfx.Glob("", "")]; // list all files, so we don't scan repeatedly.
        public bool CaseSensitive = false;

        /// <summary>
        /// Performs cue-intelligent logic to acquire a file requested by the cue.
        /// Returns the resulting full path(s).
        /// If there are multiple options, it returns them all.
        /// Returns the requested path first in the list (if it was found) for more simple use.
        /// Kind of an unusual design, I know. Consider them sorted by confidence.
        /// </summary>
        public List<string> Resolve(string path) {
            string interpretedAsRel = Path.Combine(BaseDir, path), targetFile = Path.GetFileName(path), targetFragment = Path.GetFileNameWithoutExtension(path), directory = Path.GetDirectoryName(path);
            var r = new List<string>();
            var comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            foreach (var filePath in BaseDirPaths) {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext is not ".iso" && (Disc.IsValidExtension(ext) || ext is ".sbi" or ".sub")) continue;
                if (ext is ".7z" or ".rar" or ".zip" or ".bz2" or ".gz") continue;
                var fragment = Path.GetFileNameWithoutExtension(filePath);
                if (fragment.Equals(targetFragment, comparison) || fragment.Equals(targetFile, comparison)) {
                    // take care to add an exact match at the beginning
                    if (filePath.Equals(interpretedAsRel, StringComparison.OrdinalIgnoreCase)) r.Insert(0, filePath);
                    else r.Add(filePath);
                }
            }
            return r;
        }
    }

    /// <summary>
    /// CueCompile
    /// </summary>
    class CueCompiler {
        #region Records

        public class CDText {
            public string Songwriter;
            public string Performer;
            public string Title;
            public string ISrc;
        }

        public readonly struct CueIndex(int number, MSF fileMSF) {
            public readonly MSF FileMSF = fileMSF;
            public readonly int Number = number;
            public override string ToString() => $"I#{Number:D2} {FileMSF}";
        }

        public enum CueFileType {
            Unknown,
            /// <summary>
            /// a raw BIN that can be mounted directly
            /// </summary>
            BIN,
            /// <summary>
            /// a raw WAV that can be mounted directly
            /// </summary>
            WAVE,
            /// <summary>
            /// an ECM file that can be mounted directly (once the index is generated)
            /// </summary>
            ECM,
            /// <summary>
            /// An encoded audio file which can be seeked on the fly, therefore roughly mounted on the fly
            /// </summary>
            SeekAudio,
            /// <summary>
            /// An encoded audio file which can't be seeked on the fly. It must be decoded to a temp buffer, or pre-discohawked
            /// </summary>
            DecodeAudio,
        }

        public class CueFile2 {
            public string FullPath;
            public CueFileType Type;
            public override string ToString() => $"{Type}: {Path.GetFileName(FullPath)}";
        }

        public class SessionInfo {
            public int FirstRecordedTrackNumber;
            public int LastRecordedTrackNumber;
            public DiscSessionFormat SessionFormat;
        }

        public class CueTrack {
            public int BlobIndex;
            public int Number;
            public int Session;
            /// <summary>
            /// A track that's final in a file gets its length from the length of the file; other tracks lengths are determined from the succeeding track
            /// </summary>
            public bool IsFinalInFile;
            /// <summary>
            /// A track that's first in a file has an implicit index 0 at 00:00:00; otherwise it has an implicit index 0 at the placement of the index 1
            /// </summary>
            public bool IsFirstInFile;
            public readonly CDText CDTextData = new();
            public MSF PregapLength, PostgapLength;
            public CueTrackFlags Flags = CueTrackFlags.None;
            public CueTrackType TrackType = CueTrackType.Unknown;
            public readonly IList<CueIndex> Indexes = [];
            public override string ToString() {
                var idx = Indexes.FirstOrDefault(x => x.Number == 1);
                if (idx.Number == 1) return $"T#{Number:D2} NO INDEX 1";
                var indexlist = string.Join("|", Indexes);
                return $"T#{Number:D2} {BlobIndex}:{idx.FileMSF} ({indexlist})";
            }
        }

        #endregion

        readonly CueFile File;
        internal readonly DiscContext Ctx;
        /// <summary>
        /// CD-Text set at the global level (before any track commands)
        /// </summary>
        public CDText GlobalCDText = new();
        /// <summary>
        /// The compiled file info
        /// </summary>
        public List<CueFile2> CueFiles = [];
        /// <summary>
        /// The compiled track info
        /// </summary>
        public List<CueTrack> CueTracks = [new()];
        /// <summary>
        /// High level session info (most of the time, this only has 1 session)
        /// </summary>
        public List<SessionInfo> SessionInfos = [null, new()];
        /// <summary>
        /// An integer between 0 and 10 indicating how costly it will be to load this disc completely.
        /// Activites like decoding non-seekable media will increase the load time.
        /// 0 - Requires no noticeable time
        /// 1 - Requires minimal processing (indexing ECM)
        /// 10 - Requires ages, decoding audio data, etc.
        /// </summary>
        public int LoadTime;
        public bool HasError = false;
        bool SessionFormatDetermined;
        CDText _cdtext;
        int _blobIndex = -1;
        int _session = 1;
        CueTrack _track;
        CueFile2 _file;
        bool _fileHasTrack;

        void Error(string message) { HasError = true; Debug.Error(message); }

        public CueCompiler(CueFile file, DiscContext ctx) {
            File = file;
            Ctx = ctx;
            _cdtext = GlobalCDText;
            foreach (var cmd in file.Commands) switch (cmd) {
                    case CueFile.CATALOG:
                    case CueFile.CDTEXTFILE: continue;
                    case CueFile.REM:
                    case CueFile.COMMENT: continue;
                    case CueFile.PERFORMER performerCmd: _cdtext.Performer = performerCmd.Value; break;
                    case CueFile.SONGWRITER songwriterCmd: _cdtext.Songwriter = songwriterCmd.Value; break;
                    case CueFile.TITLE titleCmd: _cdtext.Title = titleCmd.Value; break;
                    case CueFile.ISRC isrcCmd: _cdtext.ISrc = isrcCmd.Value; break;
                    case CueFile.FLAGS flagsCmd: // flags can only be set when a track command is running
                        if (_track == null) Warn("Ignoring invalid flag commands outside of a track command");
                        else _track.Flags |= flagsCmd.Flags;
                        break;
                    case CueFile.SESSION session:
                        if (session.Number == _session) break; // this may occur for SESSION 1 at the beginning, so we'll silence warnings from this
                        if (session.Number != _session + 1) Warn("Ignoring non-sequential session commands");
                        else { _session = session.Number; SessionInfos.Add(new()); SessionFormatDetermined = false; }
                        break;
                    case CueFile.TRACK trackCmd: OpenTrack(ref trackCmd); break;
                    case CueFile.FILE fileCmd: OpenFile(ref fileCmd); break;
                    case CueFile.INDEX indexCmd: _track.Indexes.Add(new(indexCmd.Number, indexCmd.Timestamp)); break;
                    case CueFile.PREGAP pregapCmd: _track.PregapLength = pregapCmd.Length; break;
                    case CueFile.POSTGAP postgapCmd: _track.PostgapLength = postgapCmd.Length; break;
                }
            CloseFile();
            CloseTrack();
            CreateTrack1Pregap();
            FinalAnalysis();
        }

        void OpenFile(ref CueFile.FILE f) {
            if (_file != null) CloseFile();
            _blobIndex++;
            _fileHasTrack = false;
            var options = Ctx.FileResolver.Resolve(f.Path);
            if (options.Count == 0) { CueFiles.Add(null); Error($"Couldn't resolve referenced cue file: {f.Path}; you can commonly repair the cue file yourself, or a file might be missing"); return; } // add a null entry to keep the count from being wrong later (quiets a warning)
            var choice = options[0];
            if (options.Count > 1) Warn($"Multiple options resolving referenced cue file; choosing: {Path.GetFileName(choice)}");
            var cfi = new CueFile2 { FullPath = choice };
            CueFiles.Add(cfi);
            _file = cfi;
            switch (Path.GetExtension(choice).ToLowerInvariant()) {
                case ".bin" or ".img" or ".raw":
                case ".iso": cfi.Type = CueFileType.BIN; break;
                case ".wav": {
                        var fs = Ctx.Vfx.Open(choice);
                        using var blob = new Blob_Wave(Ctx.Vfx);
                        try {
                            blob.Load(fs);
                            cfi.Type = CueFileType.WAVE;
                        }
                        catch { cfi.Type = CueFileType.DecodeAudio; }
                        break;
                    }
                case ".ape":
                case ".mp3":
                case ".mpc":
                case ".flac": cfi.Type = CueFileType.DecodeAudio; break;
                case ".ecm": {
                        cfi.Type = CueFileType.ECM;
                        if (!Blob_ECM.IsECM(choice)) { cfi.Type = CueFileType.Unknown; Error($"an ECM file was specified or detected, but it isn't a valid ECM file: {Path.GetFileName(choice)}"); }
                        break;
                    }
                default: cfi.Type = CueFileType.Unknown; Error($"Unknown cue file type. Since it'S likely an unsupported compression, this is an error: {Path.GetFileName(choice)}"); break;
            }
        }

        void UpdateDiscInfo(ref CueFile.TRACK trackCommand) {
            var sessionInfo = SessionInfos[_session];
            if (sessionInfo.FirstRecordedTrackNumber == 0) sessionInfo.FirstRecordedTrackNumber = trackCommand.Number;
            sessionInfo.LastRecordedTrackNumber = trackCommand.Number;
            if (!SessionFormatDetermined)
                switch (trackCommand.Type) {
                    case CueTrackType.Mode2_2336:
                    case CueTrackType.Mode2_2352: sessionInfo.SessionFormat = DiscSessionFormat.Type20_CDXA; SessionFormatDetermined = true; break;
                    case CueTrackType.CDI_2336:
                    case CueTrackType.CDI_2352: sessionInfo.SessionFormat = DiscSessionFormat.Type10_CDI; SessionFormatDetermined = true; break;
                }
        }

        void OpenTrack(ref CueFile.TRACK trackCommand) {
            if (_track != null) CloseTrack();
            // assert that a file is open
            if (_file == null) { Error("Track command encountered with no active file"); throw new Exception("DiscJobAbort"); }
            _track = new() {
                BlobIndex = _blobIndex,
                Session = _session,
                Number = trackCommand.Number,
                TrackType = trackCommand.Type
            };
            // spill cdtext data into this track
            _cdtext = _track.CDTextData;
            // default flags
            if (_track.TrackType != CueTrackType.Audio) _track.Flags = CueTrackFlags.DATA;
            if (!_fileHasTrack) _fileHasTrack = _track.IsFirstInFile = true;
            UpdateDiscInfo(ref trackCommand);
        }

        void CloseFile() {
            if (_track != null) _track.IsFinalInFile = true; //flag this track as the final one in the file
            _file = null;
        }

        void CloseTrack() {
            if (_track == null) return;
            // normalize: if an index 0 is missing, add it here
            if (_track.Indexes[0].Number != 0) {
                // if the first in the file, an implicit index will take its value from 00:00:00 in the file
                var fileMSF = _track.IsFirstInFile ? new(0) : _track.Indexes[0].FileMSF; // else, same MSF as index 1 will make it effectively nonexistent
                _track.Indexes.Insert(0, new(0, fileMSF));
            }
            CueTracks.Add(_track);
            _track = null;
        }

        void CreateTrack1Pregap() {
            if (CueTracks[1].PregapLength.Sector is not (0 or 150)) Error("Track 1 specified an illegal pregap. It'S being ignored and replaced with a 00:02:00 pregap");
            CueTracks[1].PregapLength = new(150);
        }

        void FinalAnalysis() {
            if (CueFiles.Count == 0) Error("Cue file doesn't specify any input files!");
            // score the cost of loading the file
            var needsCodec = false;
            LoadTime = 0;
            foreach (var cfi in CueFiles.Where(s => s is not null))
                switch (cfi.Type) {
                    case CueFileType.DecodeAudio: needsCodec = true; LoadTime = Math.Max(LoadTime, 10); break;
                    case CueFileType.SeekAudio: needsCodec = true; break;
                    case CueFileType.ECM: LoadTime = Math.Max(LoadTime, 1); break;
                }
            // check whether processing was available
            if (needsCodec && !FFmpegService.QueryServiceAvailable(out var version)) Warn("Decoding service will be required for further processing, but is not available");
        }
    }

    class CueDisc : Disc {
        enum BurnType { Normal, Pregap, Postgap }

        class BlobInfo {
            public Blob Blob;
            public long Length;
        }

        readonly struct TrackInfo(CueCompiler.CueTrack cueTrack) {
            public readonly CueCompiler.CueTrack CueTrack = cueTrack;
        }

        readonly FileSystem Vfx;
        readonly CueCompiler Cmp;
        readonly List<BlobInfo> BlobInfos = [];
        readonly List<TrackInfo> TrackInfos = [];
        DiscSession CurrentSession = new() { Number = 1 };

        internal CueDisc(FileSystem vfx, CueCompiler cmp) {
            Vfx = vfx;
            Cmp = cmp;
            var ctx = Cmp.Ctx;
            // generation state
            var _blobIndex = -1;
            var _blobMSF = -1;
            BlobInfo _blobInfo = null;
            long _blobOffset = -1L;

            // mount all input files
            MountBlobs();

            // we cannot determine the length of all the tracks without knowing the length of the files, now that the files are mounted, we can figure the track lengths
            AnalyzeTracks();

            // loop from track 1 to 99 (track 0 isnt handled yet, that's way distant work)
            for (var t = 1; t < TrackInfos.Count; t++) {
                var ti = TrackInfos[t];
                var ct = ti.CueTrack;
                if (CurrentSession.Number != ct.Session) {
                    if (ct.Session != CurrentSession.Number + 1) throw new InvalidOperationException("Track session change was not incremental");
                    CloseSession();
                    CurrentSession = new() { Number = ct.Session };
                }

                // setup track pregap processing: per "Example 05" on digitalx.org, pregap can come from index specification and pregap command
                var specifiedPregapLength = ct.PregapLength.Sector;
                var impliedPregapLength = ct.Indexes[1].FileMSF.Sector - ct.Indexes[0].FileMSF.Sector;
                var totalPregapLength = specifiedPregapLength + impliedPregapLength;

                // from now on we'll track relative timestamp and increment it continually
                var relMSF = -totalPregapLength;

                // generate sectors for this track.

                // advance to the next file if needed
                if (_blobIndex != ct.BlobIndex) { _blobIndex = ct.BlobIndex; _blobOffset = 0; _blobMSF = 0; _blobInfo = BlobInfos[_blobIndex]; }

                // work until the next track is reached, or the end of the current file is reached, depending on the track type
                var _index = 0;
                while (true) {
                    bool trackDone = false, generateGap = false;
                    if (specifiedPregapLength > 0) { generateGap = true; specifiedPregapLength--; } // if burning through a specified pregap, count it down
                    else
                        // if burning through the file, select the appropriate index by inspecting the next index and seeing if we've reached it
                        while (true) {
                            if (_index == ct.Indexes.Count - 1) break;
                            if (_blobMSF >= ct.Indexes[_index + 1].FileMSF.Sector) { _index++; if (_index == 1) EmitRawTOCEntry(ct); } // WE ARE NOW AT INDEX 1: generate the RawTOCEntry for this track
                            else break;
                        }

                    // select the track type for the subQ
                    var qTrack = ti; var qRelMSF = relMSF;
                    if (_index == 0) {
                        if (!ctx.DiscMountPolicy.Cue_PregapContradictionModeA) qRelMSF++; // tweak relMSF due to ambiguity/contradiction in yellowbook docs
                        if (t != 1 && ct.TrackType != CueTrackType.Audio && TrackInfos[t - 1].CueTrack.TrackType == CueTrackType.Audio)
                            if (relMSF < -150) qTrack = TrackInfos[t - 1];
                    }

                    // generate the right kind of sector synth for this track
                    SS_Base ss;
                    if (generateGap) {
                        ss = new SS_Gap { TrackType = qTrack.CueTrack.TrackType };
                    }
                    else {
                        int sectorSize;
                        switch (qTrack.CueTrack.TrackType) {
                            case CueTrackType.Audio:
                            case CueTrackType.CDI_2352:
                            case CueTrackType.Mode1_2352:
                            case CueTrackType.Mode2_2352: ss = new SS_2352(); sectorSize = 2352; break;
                            case CueTrackType.Mode1_2048: ss = new SS_Mode1_2048(); sectorSize = 2048; break;
                            case CueTrackType.CDI_2336:
                            case CueTrackType.Mode2_2336: ss = new SS_Mode2_2336(); sectorSize = 2336; break;
                            default: throw new InvalidOperationException($"Not supported: {ct.TrackType}");
                        }
                        ss.Blob = _blobInfo!.Blob;
                        ss.BlobOffset = _blobOffset;
                        _blobOffset += sectorSize;
                        _blobMSF++;
                    }
                    ss.Policy = ctx.DiscMountPolicy;
                    // setup subQ
                    ss.sq.SetStatus(SubchannelQ.kADR, (EControlQ)(int)qTrack.CueTrack.Flags);
                    ss.sq.q_tno = BCD2.FromDecimal(ct.Number);
                    ss.sq.q_index = BCD2.FromDecimal(_index);
                    ss.sq.AP_Timestamp = Sectors.Count;
                    ss.sq.Timestamp = qRelMSF;
                    // setup subP
                    if (_index == 0) ss.Pause = true;
                    Sectors.Add(ss);
                    relMSF++;
                    if (ct.IsFinalInFile) {
                        if (_blobOffset >= _blobInfo!.Length) trackDone = true; // sometimes, break when the file is exhausted
                    }
                    else {
                        if (_blobMSF >= TrackInfos[t + 1].CueTrack.Indexes[0].FileMSF.Sector) trackDone = true; // other times, break when the track is done
                    }
                    if (trackDone) break;
                }

                // gen postgap sectors
                var specifiedPostgapLength = ct.PostgapLength.Sector;
                for (var s = 0; s < specifiedPostgapLength; s++) {
                    var ss = new SS_Gap { TrackType = ct.TrackType };
                    // subq
                    ss.sq.SetStatus(SubchannelQ.kADR, (EControlQ)(int)ct.Flags);
                    ss.sq.q_tno = BCD2.FromDecimal(ct.Number);
                    ss.sq.q_index = BCD2.FromDecimal(_index);
                    ss.sq.AP_Timestamp = Sectors.Count;
                    ss.sq.Timestamp = relMSF;
                    ss.Pause = true;
                    Sectors.Add(ss);
                    relMSF++;
                }
            }
            CloseSession();
        }

        void MountBlobs() {
            foreach (var s in Cmp.CueFiles) {
                var bi = new BlobInfo();
                BlobInfos.Add(bi);
                Blob padBlob;
                switch (s.Type) {
                    case CueCompiler.CueFileType.BIN:
                    case CueCompiler.CueFileType.Unknown: {
                            var blob = new Blob_Raw(Vfx) { Path = s.FullPath };
                            DisposableResources.Add(padBlob = blob);
                            bi.Length = blob.Length;
                            break;
                        }
                    case CueCompiler.CueFileType.ECM: {
                            var blob = new Blob_ECM(Vfx);
                            DisposableResources.Add(padBlob = blob);
                            blob.Load(s.FullPath);
                            bi.Length = blob.Length;
                            break;
                        }
                    case CueCompiler.CueFileType.WAVE: {
                            var blob = new Blob_Wave(Vfx);
                            DisposableResources.Add(padBlob = blob);
                            blob.Load(s.FullPath);
                            bi.Length = blob.Length;
                            break;
                        }
                    case CueCompiler.CueFileType.DecodeAudio: {
                            if (!FFmpegService.QueryServiceAvailable(out var version))
                                throw new Exception($"{s.FullPath}: No decoding service was available (make sure ffmpeg.exe is available. Even though this may be a wav, ffmpeg is used to load oddly formatted wave files.)");
                            DiscAudioDecoder dec = new();
                            var buf = dec.AcquireWaveData(s.FullPath);
                            var blob = new Blob_Wave(Vfx);
                            DisposableResources.Add(padBlob = blob);
                            blob.Load(new MemoryStream(buf));
                            bi.Length = buf.Length;
                            break;
                        }
                    default: throw new InvalidOperationException();
                }
                bi.Blob = new Blob_ZeroPadAdapter(padBlob, bi.Length); // wrap all the blobs with zero padding
            }
        }

        void AnalyzeTracks() {
            foreach (var s in Cmp.CueTracks)
                TrackInfos.Add(new(s));
        }

        void EmitRawTOCEntry(CueCompiler.CueTrack cct) {
            SubchannelQ toc_sq = default;
            toc_sq.SetStatus(SubchannelQ.kADR, (EControlQ)(int)cct.Flags);
            toc_sq.q_tno.BCDValue = 0;
            toc_sq.q_index = BCD2.FromDecimal(cct.Number);
            toc_sq.min = BCD2.FromDecimal(0);
            toc_sq.sec = BCD2.FromDecimal(0);
            toc_sq.frame = BCD2.FromDecimal(0);
            toc_sq.AP_Timestamp = Sectors.Count;
            CurrentSession.RawTOCEntries.Add(new() { QData = toc_sq });
        }

        void CloseSession() {
            // add RawTOCEntries A0 A1 A2 to round out the TOC
            var sessionInfo = Cmp.SessionInfos[CurrentSession.Number];
            var TOCMiscInfo = new Synthesize_A0A1A2(
                firstRecordedTrackNumber: sessionInfo.FirstRecordedTrackNumber,
                lastRecordedTrackNumber: sessionInfo.LastRecordedTrackNumber,
                sessionFormat: sessionInfo.SessionFormat,
                leadoutTimestamp: Sectors.Count);
            TOCMiscInfo.Run(CurrentSession.RawTOCEntries);
            Sessions.Add(CurrentSession);
            CurrentSession = null;
        }
    }
}

#endregion

#region Disc : MdsFormat

class MdsFormat {
    /// <summary>
    /// A loose representation of an Alcohol 120 .mds file (with a few extras)
    /// </summary>
    class AFile {
        /// <summary>
        /// Full path to the MDS file
        /// </summary>
        public string MDSPath;
        /// <summary>
        /// MDS Header
        /// </summary>
        public AHeader Header = new();
        /// <summary>
        /// List of MDS session blocks
        /// </summary>
        public readonly IList<ASession> Sessions = [];
        /// <summary>
        /// List of track blocks
        /// </summary>
        public readonly IList<ATrack> Tracks = [];
        /// <summary>
        /// Current parsed session objects
        /// </summary>
        public List<Session> ParsedSession = [];
        /// <summary>
        /// Calculated MDS TOC entries (still to be parsed into BizHawk)
        /// </summary>
        public readonly IList<ATocEntry> TOCEntries = [];
    }

    class AHeader {
        /// <summary>
        /// Standard alcohol 120% signature - usually "MEDIA DESCRIPTOR"
        /// </summary>
        public string Signature;                // 16 bytes
        /// <summary>
        /// Alcohol version?
        /// </summary>
        public (byte, byte) Version;                  // 2 bytes
        /// <summary>
        /// The medium type
        /// * 0x00  -   CD
        /// * 0x01  -   CD-R
        /// * 0x02  -   CD-RW
        /// * 0x10  -   DVD
        /// * 0x12  -   DVD-R
        /// </summary>
        public int Medium;
        /// <summary>
        /// Number of sessions
        /// </summary>
        public int SessionCount;
        /// <summary>
        /// Burst Cutting Area length
        /// </summary>
        public int BCALength;
        /// <summary>
        /// Burst Cutting Area data offset
        /// </summary>
        public long BCAOffset;
        /// <summary>
        /// Offset to disc (DVD?) structures
        /// </summary>
        public long StructureOffset;
        /// <summary>
        /// Offset to the first session block
        /// </summary>
        public long SessionOffset;
        /// <summary>
        /// Data Position Measurement offset
        /// </summary>
        public long DPMOffset;
        /// <summary>
        /// Parse mds stream for the header
        /// </summary>
        public AHeader Parse(Stream s) {
            var r = new BinaryReader(s);
            Signature = Encoding.ASCII.GetString(r.ReadBytes(16));
            Version = (r.ReadByte(), r.ReadByte());
            Medium = r.ReadInt16();
            SessionCount = r.ReadInt16();
            r.Skip(4);
            BCALength = r.ReadInt16();
            r.Skip(8);
            BCAOffset = r.ReadInt32();
            r.Skip(24);
            StructureOffset = r.ReadInt32();
            r.Skip(12);
            SessionOffset = r.ReadInt32();
            DPMOffset = r.ReadInt32();
            return this;
        }
    }

    /// <summary>
    /// MDS session block representation
    /// </summary>
    class ASession {
        public int SessionStart;        // Session's start address
        public int SessionEnd;          // Session's end address
        public int SessionNumber;       // Session number
        public byte AllBlocks;          // Number of all data blocks
        public byte NonTrackBlocks;     // Number of lead-in data blocks
        public int FirstTrack;          // First track in session
        public int LastTrack;           // Last track in session
        public long TrackOffset;       // Offset of lead-in+regular track data blocks
    }

    /// <summary>
    /// Representation of an MDS track block
    /// For convenience (and extra confusion) this also holds the track extrablock, filename(footer) block infos
    /// as well as the calculated image filepath as specified in the MDS file
    /// </summary>
    class ATrack {
        /// <summary>
        /// The specified data mode (only lower 3 bits are actually meaningful)
        /// 0x00    -   None (no data)
        /// 0x02    -   DVD (when header specifies DVD, Mode1 otherwise)
        /// 0xA9    -   Audio
        /// 0xAA    -   Mode1
        /// 0xAB    -   Mode2
        /// 0xAC    -   Mode2 Form1
        /// 0xAD    -   Mode2 Form2
        /// </summary>
        public byte Mode;               // Track mode
        /// <summary>
        /// Subchannel mode for the track (0x00 = None, 0x08 = Interleaved)
        /// </summary>
        public byte SubMode;            // Subchannel mode
        // These are the fields from Sub-channel Q information, which are also returned in full TOC by READ TOC/PMA/ATIP command
        public int ADR_Control;         // Adr/Ctl
        public int TrackNo;             // Track number field
        public int Point;               // Point field (= track number for track entries)
        public int AMin;                // Min
        public int ASec;                // Sec
        public int AFrame;              // Frame
        public int Zero;                // Zero
        public int PMin;                // PMin
        public int PSec;                // PSec
        public int PFrame;              // PFrame
        //
        public long ExtraOffset;        // Start offset of this track's extra block
        public int SectorSize;          // Sector size
        public long PLBA;               // Track start sector (PLBA)
        public ulong StartOffset;       // Track start offset (from beginning of MDS file)
        public long Files;              // Number of filenames for this track
        public long FooterOffset;       // Start offset of footer (from beginning of MDS file)
        /// <summary>
        /// Track extra block
        /// </summary>
        public readonly ATrackExtra ExtraBlock = new();
        /// <summary>
        /// List of footer(filename) blocks for this track
        /// </summary>
        public List<AFooter> FooterBlocks = [];
        /// <summary>
        /// List of the calculated full paths to this track's image file
        /// The MDS file itself may contain a filename, or just an *.extension
        /// </summary>
        public List<string> ImageFileNamePaths = [];
        public int BlobIndex;
    }

    /// <summary>
    /// Extra track block
    /// </summary>
    class ATrackExtra {
        public long Pregap;            // Number of sectors in pregap
        public long Sectors;           // Number of sectors in track
    }

    /// <summary>
    /// Footer (filename) block - potentially one for every track
    /// </summary>
    class AFooter {
        public long FilenameOffset;    // Start offset of image filename string (from beginning of mds file)
        public long WideChar;          // Seems to be set to 1 if widechar filename is used
    }

    /// <summary>
    /// Represents a parsed MDS TOC entry
    /// </summary>
    class ATocEntry(int entryNum) {
        /// <summary>
        /// these should be 0-indexed
        /// </summary>
        public int EntryNum = entryNum;
        /// <summary>
        /// 1-indexed - the session that this entry belongs to
        /// </summary>
        public int Session;
        ///// <summary>
        ///// this seems just to be the LBA corresponding to AMIN:ASEC:AFRAME (give or take 150). It's not stored on the disc, and it's redundant.
        ///// </summary>
        //public int ALBA;
        /// <summary>
        /// this seems just to be the LBA corresponding to PMIN:PSEC:PFRAME (give or take 150).
        /// </summary>
        public int PLBA;
        // these correspond pretty directly to values in the Q subchannel fields
        // NOTE: they're specified as absolute MSF. That means, they're 2 seconds off from what they should be when viewed as final TOC values
        public int ADR_Control;
        public int TrackNo;
        public int Point;
        public int AMin;
        public int ASec;
        public int AFrame;
        public int Zero;
        public int PMin;
        public int PSec;
        public int PFrame;
        /// <summary>
        /// Lower 3 bits of ATrack Mode
        /// Upper 5 bits are meaningless (see mirage_parser_mds_convert_track_mode)
        /// 0x0 - None or Mode2 (Depends on sector size)
        /// 0x1 - Audio
        /// 0x2 - DVD or Mode1 (Depends on medium)
        /// 0x3 - Mode2
        /// 0x4 - Mode2 Form1
        /// 0x5 - Mode2 Form2
        /// 0x6 - UNKNOWN
        /// 0x7 - Mode2
        /// </summary>
        public int TrackMode;
        public int SectorSize;
        public long TrackOffset;
        /// <summary>
        /// List of the calculated full paths to this track's image file
        /// The MDS file itself may contain a filename, or just an *.extension
        /// </summary>
        public List<string> ImageFileNamePaths = [];
        /// <summary>
        /// Track extra block
        /// </summary>
        public ATrackExtra ExtraBlock = new();
        public int BlobIndex;
    }

    static AFile Parse(Stream s, string path) {
        var isDvd = false;
        var file = new AFile { MDSPath = path };
        s.Seek(0, SeekOrigin.Begin);
        // check whether the header in the mds file is long enough
        if (s.Length < 88) throw new InvalidOperationException("Malformed MDS format: The descriptor file does not appear to be long enough.");
        // parse header
        file.Header = file.Header.Parse(s);
        // check version to make sure this is only v1.x
        // currently NO support for version 2.x
        if (file.Header.Version.Item1 > 1) throw new InvalidOperationException($"MDS Parse Error: Only MDS version 1.x is supported!\nDetected version: {file.Header.Version.Item1}.{file.Header.Version.Item2}");

        isDvd = file.Header.Medium is 0x10 or 0x12;
        if (isDvd) throw new InvalidOperationException("DVD Detected. Not currently supported!");

        // parse sessions
        var sessions = new Dictionary<int, ASession>();

        var r = new BinaryReader(s);
        s.Seek(file.Header.SessionOffset, SeekOrigin.Begin);
        for (var se = 0; se < file.Header.SessionCount; se++) {
            var session = new ASession {
                SessionStart = r.ReadInt32(),
                SessionEnd = r.ReadInt32(),
                SessionNumber = r.ReadInt16(),
                AllBlocks = r.ReadByte(),
                NonTrackBlocks = r.ReadByte(),
                FirstTrack = r.ReadInt16(),
                LastTrack = r.ReadInt16(),
                TrackOffset = r.Skip(4).ReadInt32(),
            };
            sessions.Add(session.SessionNumber, session);
        }

        var footerOffset = 0L;

        // parse track blocks
        var aTracks = new Dictionary<int, ATrack>();

        // iterate through each session block
        foreach (var session in sessions.Values) {
            s.Seek(session.TrackOffset, SeekOrigin.Begin);
            // iterate through every block specified in each session
            for (var bl = 0; bl < session.AllBlocks; bl++) {
                var trackHeader = new byte[80];
                var track = new ATrack();
                var bytesRead = s.Read(trackHeader, offset: 0, count: trackHeader.Length);
                Assert(bytesRead == trackHeader.Length, "reached end-of-file while reading track header");
                track.Mode = r.ReadByte();
                track.SubMode = r.ReadByte();
                track.ADR_Control = r.ReadByte();
                track.TrackNo = r.ReadByte();
                track.Point = r.ReadByte();
                track.AMin = r.ReadByte();
                track.ASec = r.ReadByte();
                track.AFrame = r.ReadByte();
                track.Zero = r.ReadByte();
                track.PMin = r.ReadByte();
                track.PSec = r.ReadByte();
                track.PFrame = r.ReadByte();
                track.ExtraOffset = r.ReadInt32();
                track.SectorSize = r.ReadInt16();
                r.Skip(18);
                track.PLBA = r.ReadInt32();
                track.StartOffset = r.ReadUInt64();
                track.Files = r.ReadInt32();
                track.FooterOffset = r.ReadInt32();
                r.Skip(24);
                var currPos = s.Position;
                // Only CDs have extra blocks - for DVDs ExtraOffset = track length
                if (track.ExtraOffset > 0 && !isDvd) {
                    s.Seek(track.ExtraOffset, SeekOrigin.Begin);
                    track.ExtraBlock.Pregap = r.ReadInt32();
                    track.ExtraBlock.Sectors = r.ReadInt32();
                    s.Seek(currPos, SeekOrigin.Begin);
                }
                else if (isDvd) track.ExtraBlock.Sectors = track.ExtraOffset;

                // read the footer/filename block for this track
                currPos = s.Position;
                var numOfFilenames = track.Files;
                for (var fi = 1L; fi <= numOfFilenames; fi++) {
                    // skip leadin/out info tracks
                    if (track.FooterOffset == 0) continue;

                    s.Seek(track.FooterOffset, SeekOrigin.Begin);
                    var f = new AFooter {
                        FilenameOffset = r.ReadInt32(),
                        WideChar = r.ReadInt32(),
                    };
                    track.FooterBlocks.Add(f);
                    track.FooterBlocks = track.FooterBlocks.Distinct().ToList();

                    // parse the filename string
                    var fileName = "*.mdf";
                    if (f.FilenameOffset > 0) {
                        // filename offset is present
                        s.Seek(f.FilenameOffset, SeekOrigin.Begin);
                        byte[] fname = numOfFilenames == 1
                            ? file.Header.DPMOffset == 0
                                ? new byte[s.Length - s.Position] // filename is in the remaining space to EOF
                                : new byte[file.Header.DPMOffset - s.Position] // filename is in the remaining space to EOF + dpm offset
                            : new byte[6]; // looks like each filename string is 6 bytes with a trailing \0

                        // read the filename
                        var bytesRead2 = s.Read(fname, offset: 0, count: fname.Length);
                        Assert(bytesRead2 == fname.Length, "reached end-of-file while reading track filename");

                        // if widechar is 1 filename is stored using 16-bit, otherwise 8-bit is used
                        fileName = f.WideChar == 1
                            ? Encoding.Unicode.GetString(fname).TrimEnd('\0')
                            : Encoding.Default.GetString(fname).TrimEnd('\0');
                    }
                    //else { } // assume an MDF file with the same name as the MDS

                    var (dir, fileNoExt) = (Path.GetDirectoryName(file.MDSPath), Path.GetFileNameWithoutExtension(file.MDSPath));
                    fileName = f.FilenameOffset is 0 || "*.mdf".Equals(fileName, StringComparison.OrdinalIgnoreCase)
                        ? $@"{dir}\{fileNoExt}.mdf"
                        : $@"{dir}\{fileName}";
                    track.ImageFileNamePaths.Add(fileName);
                    track.ImageFileNamePaths = track.ImageFileNamePaths.Distinct().ToList();
                }

                s.Position = currPos;

                var point = track.Point;
                // each session has its own 0xA0/0xA1/0xA3 track so this can't be used directly as a key
                if (point is 0xA0 or 0xA1 or 0xA2) point |= session.SessionNumber << 8;

                aTracks.Add(point, track);
                file.Tracks.Add(track);

                if (footerOffset == 0) footerOffset = track.FooterOffset;
            }
        }

        // build custom session object
        file.ParsedSession = [];
        foreach (var t in sessions.Values) {
            var session = new Session();
            if (!aTracks.TryGetValue(t.FirstTrack, out var startTrack)) break;
            if (!aTracks.TryGetValue(t.LastTrack, out var endTrack)) break;
            session.StartSector = startTrack.PLBA;
            session.StartTrack = t.FirstTrack;
            session.SessionSequence = t.SessionNumber;
            session.EndSector = endTrack.PLBA + endTrack.ExtraBlock.Sectors - 1;
            session.EndTrack = t.LastTrack;
            file.ParsedSession.Add(session);
        }

        // now build the TOC object
        foreach (var se in file.ParsedSession) {
            ATocEntry CreateTOCEntryFromTrack(ATrack track) => new(track.Point) {
                ADR_Control = track.ADR_Control,
                AFrame = track.AFrame,
                AMin = track.AMin,
                ASec = track.ASec,
                BlobIndex = track.BlobIndex,
                EntryNum = track.TrackNo,
                ExtraBlock = track.ExtraBlock,
                ImageFileNamePaths = track.ImageFileNamePaths,
                PFrame = track.PFrame,
                PLBA = Convert.ToInt32(track.PLBA),
                PMin = track.PMin,
                Point = track.Point,
                PSec = track.PSec,
                TrackMode = track.Mode & 0x7,
                SectorSize = track.SectorSize,
                Session = se.SessionSequence,
                TrackOffset = Convert.ToInt64(track.StartOffset),
                Zero = track.Zero,
            };
            void AddAXTrack(int x) {
                if (aTracks.TryGetValue(se.SessionSequence << 8 | 0xA0 | x, out var axTrack)) file.TOCEntries.Add(CreateTOCEntryFromTrack(axTrack));
            }
            // add in the 0xA0/0xA1/0xA2 tracks
            AddAXTrack(0);
            AddAXTrack(1);
            AddAXTrack(2);
            // add in the rest of the tracks
            foreach (var t in aTracks.Where(a => se.StartTrack <= a.Key && a.Key <= se.EndTrack).OrderBy(a => a.Key).Select(a => a.Value))
                file.TOCEntries.Add(CreateTOCEntryFromTrack(t));
        }
        return file;
    }

    /// <summary>
    /// Custom session object
    /// </summary>
    class Session {
        public long StartSector;
        public int StartTrack;
        public int SessionSequence;
        public long EndSector;
        public int EndTrack;
    }

    class LoadResults {
        public List<RawTOCEntry> RawTOCEntries;
        public AFile ParsedMDSFile;
        public bool Valid;
        public InvalidOperationException FailureException;
        public string MdsPath;
    }

    static LoadResults LoadMdsPath(FileSystem vfx, string path) {
        var ret = new LoadResults { MdsPath = path };
        try {
            if (!vfx.FileExists(path)) throw new InvalidOperationException("Malformed MDS format: nonexistent MDS file!");
            using var s = vfx.Open(path);
            var mdsf = Parse(s, path);
            ret.ParsedMDSFile = mdsf;
            ret.Valid = true;
        }
        catch (InvalidOperationException ex) {
            ret.FailureException = ex;
        }

        return ret;
    }

    static Dictionary<int, Blob> MountBlobs(FileSystem vfx, AFile mdsf, Disc disc) {
        var blobIndex = new Dictionary<int, Blob>();
        var count = 0;
        foreach (var track in mdsf.Tracks) {
            foreach (var file in track.ImageFileNamePaths.Distinct()) {
                if (!vfx.FileExists(file)) throw new InvalidOperationException($"Malformed MDS format: nonexistent image file: {file}");
                // mount the file
                var blob = new Blob_Raw(vfx) { Path = file };
                var dupe = false;
                foreach (var re in disc.DisposableResources) if (re.ToString() == blob.ToString()) dupe = true;
                if (!dupe) {
                    // wrap in zeropadadapter
                    disc.DisposableResources.Add(blob);
                    blobIndex[count++] = blob;
                }
            }
        }
        return blobIndex;
    }

    static RawTOCEntry EmitRawTOCEntry(ATocEntry entry) {
        var tno = BCD2.FromDecimal(entry.TrackNo);
        var ino = BCD2.FromDecimal(entry.Point);
        ino.BCDValue = entry.Point switch {
            0xA0 or 0xA1 or 0xA2 => (byte)entry.Point,
            _ => ino.BCDValue,
        };
        // get ADR & Control from ADR_Control byte
        var adrc = Convert.ToByte(entry.ADR_Control);
        var Control = adrc & 0x0F;
        var ADR = adrc >> 4;
        var q = new SubchannelQ {
            q_status = SubchannelQ.ComputeStatus(ADR, (EControlQ)(Control & 0xF)),
            q_tno = tno,
            q_index = ino,
            min = BCD2.FromDecimal(entry.AMin),
            sec = BCD2.FromDecimal(entry.ASec),
            frame = BCD2.FromDecimal(entry.AFrame),
            zero = (byte)entry.Zero,
            ap_min = BCD2.FromDecimal(entry.PMin),
            ap_sec = BCD2.FromDecimal(entry.PSec),
            ap_frame = BCD2.FromDecimal(entry.PFrame),
            q_crc = 0, //meaningless
        };
        return new() { QData = q };
    }

    internal static Disc LoadMdsToDisc(FileSystem vfx, string mdsPath, DiscMountPolicy discMountPolicy) {
        var loadResults = LoadMdsPath(vfx, mdsPath);
        if (!loadResults.Valid) throw loadResults.FailureException;
        var disc = new Disc();
        // load all blobs
        var BlobIndex = MountBlobs(vfx, loadResults.ParsedMDSFile, disc);
        var mdsf = loadResults.ParsedMDSFile;
        // generate DiscTOCRaw items from the ones specified in the MDS file
        var curSession = 1;
        disc.Sessions.Add(new() { Number = curSession });
        foreach (var entry in mdsf.TOCEntries) {
            if (entry.Session != curSession) {
                if (entry.Session != curSession + 1) throw new InvalidOperationException("Session incremented more than one!");
                curSession = entry.Session;
                disc.Sessions.Add(new() { Number = curSession });
            }
            disc.Sessions[curSession].RawTOCEntries.Add(EmitRawTOCEntry(entry));
        }
        // analyze the RAWTocEntries to figure out what type of track track 1 is
        var tocSynth = new Synthesize_DiscTOCFromRawTOCEntries(disc.Session1.RawTOCEntries);
        var toc = tocSynth.Run();

        // now build the sectors
        var currBlobIndex = 0;
        foreach (var session in mdsf.ParsedSession) {
            // leadin track
            var leadinSize = session.SessionSequence == 1 ? 0 : 4500;
            for (var i = 0; i < leadinSize; i++) {
                var pregapTrackType = CueTrackType.Audio;
                if (toc.TOCItems[1].IsData) {
                    pregapTrackType = toc.SessionFormat switch {
                        DiscSessionFormat.Type20_CDXA => CueTrackType.Mode2_2352,
                        DiscSessionFormat.Type10_CDI => CueTrackType.CDI_2352,
                        DiscSessionFormat.Type00_CDROM_CDDA => CueTrackType.Mode1_2352,
                        _ => pregapTrackType,
                    };
                }
                disc.Sectors.Add(new SS_Gap() { Policy = discMountPolicy, TrackType = pregapTrackType });
            }

            for (var i = session.StartTrack; i <= session.EndTrack; i++) {
                var relMSF = -1;
                var track = mdsf.TOCEntries.FirstOrDefault(t => t.Point == i);
                if (track == null) break;
                // ignore the info entries
                if (track.Point is 0xA0 or 0xA1 or 0xA2) continue;
                // get the blob(s) for this track
                var tr = mdsf.TOCEntries.FirstOrDefault(a => a.Point == i) ?? throw new InvalidOperationException("BLOB Error!");
                if (tr.ImageFileNamePaths.Count == 0) throw new InvalidOperationException("BLOB Error!");

                // check for track pregap and create if necessary
                if (track.ExtraBlock.Pregap > 0) {
                    var pregapTrackType = CueTrackType.Audio;
                    if (toc.TOCItems[1].IsData) {
                        pregapTrackType = toc.SessionFormat switch {
                            DiscSessionFormat.Type20_CDXA => CueTrackType.Mode2_2352,
                            DiscSessionFormat.Type10_CDI => CueTrackType.CDI_2352,
                            DiscSessionFormat.Type00_CDROM_CDDA => CueTrackType.Mode1_2352,
                            _ => pregapTrackType,
                        };
                    }
                    for (var pre = 0; pre < track.ExtraBlock.Pregap; pre++) {
                        relMSF++;
                        var ss_gap = new SS_Gap() { Policy = discMountPolicy, TrackType = pregapTrackType };
                        disc.Sectors.Add(ss_gap);
                        var qRelMSF = pre - Convert.ToInt32(track.ExtraBlock.Pregap);
                        // tweak relMSF due to ambiguity/contradiction in yellowbook docs
                        if (!discMountPolicy.Cue_PregapContradictionModeA) qRelMSF++;
                        ss_gap.sq.SetStatus(SubchannelQ.kADR, toc.TOCItems[1].Control);
                        ss_gap.sq.q_tno = BCD2.FromDecimal(1);
                        ss_gap.sq.q_index = BCD2.FromDecimal(0);
                        ss_gap.sq.AP_Timestamp = pre;
                        ss_gap.sq.Timestamp = qRelMSF;
                        ss_gap.Pause = true;
                    }
                }

                // create track sectors
                var currBlobOffset = track.TrackOffset;
                for (var sector = session.StartSector; sector <= session.EndSector; sector++) {
                    // get the current blob from the BlobIndex
                    var currBlob = (Blob_Raw)BlobIndex[currBlobIndex];
                    var currBlobLength = currBlob.Length;
                    if (sector == currBlobLength)
                        currBlobIndex++;
                    var mdfBlob = (Blob)disc.DisposableResources[currBlobIndex];

                    SS_Base sBase = track.SectorSize switch {
                        2352 when track.TrackMode is 1 => new SS_2352(),
                        2048 when track.TrackMode is 2 => new SS_Mode1_2048(),
                        2336 when track.TrackMode is 0 or 3 or 7 => new SS_Mode2_2336(),
                        2048 when track.TrackMode is 4 => new SS_Mode2_Form1_2048(),
                        2324 when track.TrackMode is 5 => new SS_Mode2_Form2_2324(),
                        2328 when track.TrackMode is 5 => new SS_Mode2_Form2_2328(),
                        2048 => new SS_Mode1_2048(),
                        2336 => new SS_Mode2_2336(),
                        2352 => new SS_2352(),
                        2448 => new SS_2448_Interleaved(),
                        _ => throw new InvalidOperationException($"Not supported: Sector Size {track.SectorSize}, Track Mode {track.TrackMode}"),
                    };
                    sBase.Policy = discMountPolicy;
                    // configure blob
                    sBase.Blob = mdfBlob;
                    sBase.BlobOffset = currBlobOffset;

                    currBlobOffset += track.SectorSize;

                    // add subchannel data
                    relMSF++;
                    var ino = BCD2.FromDecimal(track.Point);
                    ino.BCDValue = track.Point switch {
                        0xA0 or 0xA1 or 0xA2 => (byte)track.Point,
                        _ => ino.BCDValue,
                    };
                    // get ADR & Control from ADR_Control byte
                    var adrc = Convert.ToByte(track.ADR_Control);
                    var Control = adrc & 0x0F;
                    var ADR = adrc >> 4;
                    sBase.sq = new SubchannelQ {
                        q_status = SubchannelQ.ComputeStatus(ADR, (EControlQ)(Control & 0xF)),
                        q_tno = BCD2.FromDecimal(track.Point),
                        q_index = ino,
                        AP_Timestamp = disc.Sectors.Count,
                        Timestamp = relMSF - Convert.ToInt32(track.ExtraBlock.Pregap),
                    };
                    disc.Sectors.Add(sBase);
                }
            }

            // leadout track, first leadout is 6750 sectors, later ones are 2250 sectors
            var leadoutSize = session.SessionSequence == 1 ? 6750 : 2250;
            for (var i = 0; i < leadoutSize; i++)
                disc.Sectors.Add(new SS_Leadout { SessionNumber = session.SessionSequence, Policy = discMountPolicy });
        }
        return disc;
    }
}

#endregion

#region Disc : NrgFormat

static class NrgFormat {
    /// <summary>
    /// Represents a NRG file, faithfully. Minimal interpretation of the data happens.
    /// May represent either a v1 or v2 NRG file
    /// </summary>
    class NrgFile {
        /// <summary>
        /// File ID
        /// "NERO" for V1, "NER5" for V2
        /// </summary>
        public string FileID;
        /// <summary>
        /// Offset to first chunk size in bytes
        /// </summary>
        public long FileOffset;
        /// <summary>
        /// The CUES/CUEX chunks
        /// </summary>
        public readonly IList<NrgCue> Cues = [];
        /// <summary>
        /// The DAOI/DAOX chunks
        /// </summary>
        public readonly IList<NrgDaoTrackInfo> DAOTrackInfos = [];
        /// <summary>
        /// The TINF/ETNF/ETN2 chunks
        /// </summary>
        public readonly IList<NrgTaoTrackInfo> TAOTrackInfos = [];
        /// <summary>
        /// The RELO chunks
        /// </summary>
        public readonly IList<NrgRelo> RELOs = [];
        /// <summary>
        /// The TOCT chunks
        /// </summary>
        public readonly IList<NrgToct> TOCTs = [];
        /// <summary>
        /// The SINF chunks
        /// </summary>
        public readonly IList<NrgSessionInfo> SessionInfos = [];
        /// <summary>
        /// The CDTX chunk
        /// </summary>
        public NrgCdText CdText;
        /// <summary>
        /// The MTYP chunk
        /// </summary>
        public NrgMediaType MediaType;
        /// <summary>
        /// The AFNM chunk
        /// </summary>
        public NrgFilenames Filenames;
        /// <summary>
        /// The VOLM chunk
        /// </summary>
        public NrgVolumeName VolumeName;
        /// <summary>
        /// The END! chunk
        /// </summary>
        public NrgEnd End;
    }

    /// <summary>
    /// Represents a generic chunk from a NRG file
    /// </summary>
    abstract class NrgChunk {
        /// <summary>
        /// The chunk ID
        /// </summary>
        public string ChunkID;
        /// <summary>
        /// The chunk size in bytes
        /// </summary>
        public int ChunkSize;
    }

    /// <summary>
    /// Represents a track index in CUES/CUEX chunk
    /// </summary>
    class NrgTrackIndex {
        /// <summary>
        /// ADR/Control byte (LSBs = ADR, MSBs = Control)
        /// </summary>
        public byte ADRControl;
        /// <summary>
        /// Track number (00 = leadin, 01-99 = track n, AA = leadout)
        /// </summary>
        public BCD2 Track;
        /// <summary>
        /// Index (00 = pregap, 01+ = actual track)
        /// </summary>
        public BCD2 Index;
        /// <summary>
        /// LBA for the location of this track index, starts at -150
        /// </summary>
        public int LBA;
    }

    /// <summary>
    /// Represents a CUES/CUEX chunk from a NRG file
    /// </summary>
    class NrgCue : NrgChunk {
        /// <summary>
        /// All of the track indices for this session
        /// Don't trust index0's LBA, it's probably wrong
        /// </summary>
        public readonly IList<NrgTrackIndex> TrackIndices = [];
    }

    /// <summary>
    /// Represents a track in a DAOI/DAOX chunk
    /// </summary>
    class NegDaoTrack {
        /// <summary>
        /// 12-letter/digit string (may be empty)
        /// </summary>
        public string Isrc;
        /// <summary>
        /// Sector size (depends on Mode)
        /// Note: some files will have all tracks use the same sector size
        /// So if you have different modes on tracks, this will be the largest mode size
        /// Of course, this means sectors on the file may just have padding
        /// </summary>
        public ushort SectorSize;
        /// <summary>
        /// 00 = Mode1 / 2048 byte sectors
        /// 02 = Mode2 Form1 / 2048 byte sectors
        /// 03 = Mode2 / 2336 byte sectors
        /// (nb: no$ reports this is Form1, libmirage reports this is Form2, doesn't matter with 2336 bytes anyways)
        /// 05 = Mode1 / 2352 byte sectors
        /// 06 = Mode2 / 2352 byte sectors
        /// 07 = Audio / 2352 byte sectors
        /// 0F = Mode1 / 2448 byte sectors
        /// 10 = Audio / 2448 byte sectors
        /// 11 = Mode2 / 2448 byte sectors
        /// </summary>
        public byte Mode;
        /// <summary>
        /// File offset to this track's pregap (index 0)
        /// </summary>
        public long PregapFileOffset;
        /// <summary>
        /// File offset to this track's actual data (index 1)
        /// </summary>
        public long TrackStartFileOffset;
        /// <summary>
        /// File offset to the end of this track (equal to next track pregap)
        /// </summary>
        public long TrackEndFileOffset;
    }

    /// <summary>
    /// Represents a DAOI/DAOX chunk from a NRG file
    /// </summary>
    class NrgDaoTrackInfo : NrgChunk {
        /// <summary>
        /// 13-digit ASCII string (may be empty)
        /// </summary>
        public string Ean13CatalogNumber;
        /// <summary>
        /// Disk type (0x00 = Mode1 or Audio, 0x10 = CD-I (?), 0x20 = XA/Mode2)
        /// </summary>
        public byte DiskType;
        /// <summary>
        /// First track, non-BCD (1-99)
        /// </summary>
        public byte FirstTrack;
        /// <summary>
        /// Last track, non-BCD (1-99)
        /// </summary>
        public byte LastTrack;
        /// <summary>
        /// All of the tracks for this chunk
        /// </summary>
        public readonly IList<NegDaoTrack> Tracks = new List<NegDaoTrack>();
    }

    /// <summary>
    /// Represents a track in a TINF/ETNF/ETN2 chunk
    /// </summary>
    class NrgTaoTrack {
        /// <summary>
        /// File offset to this track's data (presumably the start of the pregap)
        /// </summary>
        public long TrackFileOffset;
        /// <summary>
        /// Track length in bytes
        /// </summary>
        public ulong TrackLength;
        /// <summary>
        /// Same meaning as NRGDAOTrack's Mode
        /// </summary>
        public int Mode;
        /// <summary>
        /// Starting LBA for this track on the disc
        /// Not present for TINF chunks
        /// </summary>
        public int? StartLBA;
    }

    /// <summary>
    /// Represents a TINF/ETNF/ETN2 chunk
    /// </summary>
    class NrgTaoTrackInfo : NrgChunk {
        /// <summary>
        /// All of the tracks for this chunk
        /// </summary>
        public readonly IList<NrgTaoTrack> Tracks = [];
    }

    /// <summary>
    /// Represents a RELO chunk
    /// </summary>
    class NrgRelo : NrgChunk {
    }

    /// <summary>
    /// Represents a TOCT chunk
    /// </summary>
    class NrgToct : NrgChunk {
        /// <summary>
        /// Disk type (0x00 = Mode1 or Audio, 0x10 = CD-I (?), 0x20 = XA/Mode2)
        /// </summary>
        public byte DiskType;
    }

    /// <summary>
    /// Represents a SINF chunk
    /// </summary>
    class NrgSessionInfo : NrgChunk {
        /// <summary>
        /// Number of tracks in session
        /// </summary>
        public uint TrackCount;
    }

    /// <summary>
    /// Represents a CDTX chunk
    /// </summary>
    class NrgCdText : NrgChunk {
        /// <summary>
        /// Raw 18-byte CD text packs
        /// </summary>
        public readonly IList<byte[]> CdTextPacks = [];
    }

    /// <summary>
    /// Represents a MTYP chunk
    /// </summary>
    class NrgMediaType : NrgChunk {
        /// <summary>
        /// Media Type
        /// </summary>
        public uint MediaType;
    }

    /// <summary>
    /// Represents a AFNM chunk
    /// </summary>
    class NrgFilenames : NrgChunk {
        /// <summary>
        /// Filenames where the image originally came from
        /// </summary>
        public IList<string> Filenames = [];
    }

    /// <summary>
    /// Represents a VOLM chunk
    /// </summary>
    class NrgVolumeName : NrgChunk {
        /// <summary>
        /// Volume Name
        /// </summary>
        public string VolumeName;
    }

    /// <summary>
    /// Represents a END! chunk
    /// </summary>
    class NrgEnd : NrgChunk {
        // Chunk size should always be 0
    }

    static NrgCue ParseCueChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // CUES/CUEX is always a multiple of 8
        if (chunkSize % 8 != 0) throw new InvalidOperationException("Malformed NRG format: CUE chunk was not a multiple of 8!");
        else if (chunkSize == 0) throw new InvalidOperationException("Malformed NRG format: 0 sized CUE chunk!");
        var v2 = chunkID == "CUEX";
        var ret = new NrgCue { ChunkID = chunkID, ChunkSize = chunkSize };
        for (var i = 0; i < chunkSize; i += 8) {
            var trackIndex = new NrgTrackIndex {
                ADRControl = chunkData[i + 0],
                Track = BCD2.FromBCD(chunkData[i + 1]),
                Index = BCD2.FromBCD(chunkData[i + 2]),
                // chunkData[i + 3] is probably padding
            };
            trackIndex.LBA = v2
                ? BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 4, sizeof(int)))
                : MSF.ToInt(chunkData[i + 5], chunkData[i + 6], chunkData[i + 7]) - 150;
            ret.TrackIndices.Add(trackIndex);
        }
        return ret;
    }

    static NrgDaoTrackInfo ParseDaoChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // base DAOI/DAOX is 22 bytes
        if (chunkSize < 22) throw new InvalidOperationException("Malformed NRG format: DAO chunk is less than 22 bytes!");
        var ret = new NrgDaoTrackInfo {
            ChunkID = chunkID,
            ChunkSize = chunkSize,
            // chunkData[0..3] is usually a duplicate of chunkSize
            Ean13CatalogNumber = Encoding.ASCII.GetString(chunkData.Slice(4, 13)).TrimEnd('\0'),
            // chunkData[17] is probably padding
            DiskType = chunkData[18],
            // chunkData[19] is said to be "_num_sessions" by libmirage with a question mark as a comment
            // however, for a 2 session disc (DAOX), this seems to be 0 for the first session, then 1 for the next one
            // others report that this byte is "always 1" (presumably with single session discs)
            FirstTrack = chunkData[20],
            LastTrack = chunkData[21],
        };
        var v2 = chunkID == "DAOX";
        var ntracks = ret.LastTrack - ret.FirstTrack + 1;
        if (ntracks <= 0 || ret.FirstTrack is < 0 or > 99 || ret.LastTrack is < 0 or > 99) throw new InvalidOperationException("Malformed NRG format: Corrupt track numbers in DAO chunk!");
        // each track is 30 (DAOI) or 42 (DAOX) bytes
        if (chunkSize - 22 != ntracks * (v2 ? 42 : 30)) throw new InvalidOperationException("Malformed NRG format: DAO chunk size does not match number of tracks!");
        for (var i = 22; i < chunkSize; i += v2 ? 42 : 30) {
            var track = new NegDaoTrack {
                Isrc = Encoding.ASCII.GetString(chunkData.Slice(i, 12)).TrimEnd('\0'),
                SectorSize = BinaryPrimitives.ReadUInt16BigEndian(chunkData.Slice(i + 12, sizeof(ushort))),
                Mode = chunkData[i + 14],
            };
            if (v2) {
                track.PregapFileOffset = BinaryPrimitives.ReadInt64BigEndian(chunkData.Slice(i + 18, sizeof(long)));
                track.TrackStartFileOffset = BinaryPrimitives.ReadInt64BigEndian(chunkData.Slice(i + 26, sizeof(long)));
                track.TrackEndFileOffset = BinaryPrimitives.ReadInt64BigEndian(chunkData.Slice(i + 34, sizeof(long)));
                if (track.PregapFileOffset < 0 || track.TrackStartFileOffset < 0 || track.TrackEndFileOffset < 0) throw new InvalidOperationException("Malformed NRG format: Negative file offsets in DAOX chunk!");
            }
            else {
                track.PregapFileOffset = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 18, sizeof(uint)));
                track.TrackStartFileOffset = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 22, sizeof(uint)));
                track.TrackEndFileOffset = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 26, sizeof(uint)));
            }
            ret.Tracks.Add(track);
        }
        return ret;
    }

    static NrgTaoTrackInfo ParseEtnChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // TINF is always a multiple of 12
        // ETNF is always a multiple of 20
        // ETN2 is always a multiple of 32
        var trackSize = chunkID switch {
            "TINF" => 12,
            "ETNF" => 20,
            "ETN2" => 32,
            _ => throw new InvalidOperationException(),
        };
        if (chunkSize % trackSize != 0) throw new InvalidOperationException($"Malformed NRG format: {chunkID} chunk was not a multiple of {trackSize}!");
        var ret = new NrgTaoTrackInfo { ChunkID = chunkID, ChunkSize = chunkSize };
        for (var i = 0; i < chunkSize; i += trackSize) {
            var track = new NrgTaoTrack();
            if (chunkID == "ETN2") {
                track.TrackFileOffset = BinaryPrimitives.ReadInt64BigEndian(chunkData.Slice(i + 0, sizeof(long)));
                track.TrackLength = BinaryPrimitives.ReadUInt64BigEndian(chunkData.Slice(i + 8, sizeof(ulong)));
                track.Mode = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 16, sizeof(int)));
                track.StartLBA = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 20, sizeof(int)));
                // chunkData[24..31] is unknown
                if (track.TrackFileOffset < 0) throw new InvalidOperationException("Malformed NRG format: Negative file offset in ETN2 chunk!");
            }
            else {
                track.TrackFileOffset = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 0, sizeof(uint)));
                track.TrackLength = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 4, sizeof(uint)));
                track.Mode = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 8, sizeof(int)));
                // not available in TINF chunks
                if (chunkID == "ETNF") {
                    track.StartLBA = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 12, sizeof(int)));
                    // chunkData[16..19] is unknown
                }
            }
            ret.Tracks.Add(track);
        }
        return ret;
    }

    static NrgRelo ParseReloChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // RELO seems to be only 4 bytes large (although they're always all 0?)
        if (chunkSize != 4) throw new InvalidOperationException("Malformed NRG format: RELO chunk was not 4 bytes!");
        return new() { ChunkID = chunkID, ChunkSize = chunkSize };
    }

    static NrgToct ParseToctChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // TOCT is always 2 bytes large
        if (chunkSize != 2) throw new InvalidOperationException("Malformed NRG format: TOCT chunk was not 2 bytes!");
        return new() { ChunkID = chunkID, ChunkSize = chunkSize, DiskType = chunkData[0] };
    }

    static NrgSessionInfo ParseSinfChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // SINF is always 4 bytes large
        if (chunkSize != 4) throw new InvalidOperationException("Malformed NRG format: SINF chunk was not 4 bytes!");
        return new() { ChunkID = chunkID, ChunkSize = chunkSize, TrackCount = BinaryPrimitives.ReadUInt32BigEndian(chunkData) };
    }

    static NrgCdText ParseCdtxChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // CDTX is always a multiple of 18
        if (chunkSize % 18 != 0) throw new InvalidOperationException("Malformed NRG format: CDTX chunk was not a multiple of 18!");
        var ret = new NrgCdText { ChunkID = chunkID, ChunkSize = chunkSize };
        for (var i = 0; i < chunkSize; i += 18)
            ret.CdTextPacks.Add(chunkData.Slice(i, 18).ToArray());
        return ret;
    }

    static NrgMediaType ParseMtypChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // MTYP is always 4 bytes large
        if (chunkSize != 4) throw new InvalidOperationException("Malformed NRG format: MTYP chunk was not 4 bytes!");
        return new() { ChunkID = chunkID, ChunkSize = chunkSize, MediaType = BinaryPrimitives.ReadUInt32BigEndian(chunkData) };
    }

    static NrgFilenames ParseAfnmChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // AFNM just contains list of null terminated strings
        if (chunkSize == 0 || chunkData[chunkSize - 1] != 0) throw new InvalidOperationException("Malformed NRG format: Missing null terminator in AFNM chunk!");
        var ret = new NrgFilenames { ChunkID = chunkID, ChunkSize = chunkSize };
        for (var i = 0; i < chunkSize;) {
            var j = 0;
            while (chunkData[i + j] != 0) j++;
            ret.Filenames.Add(Encoding.ASCII.GetString(chunkData.Slice(i, j)));
            i += j + 1;
        }
        return ret;
    }

    static NrgVolumeName ParseVolmChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // VOLM just contains a null terminated string
        if (chunkSize == 0 || chunkData[chunkSize - 1] != 0) throw new InvalidOperationException("Malformed NRG format: Missing null terminator in VOLM chunk!");
        return new() { ChunkID = chunkID, ChunkSize = chunkSize, VolumeName = Encoding.ASCII.GetString(chunkData).TrimEnd('\0') };
    }

    static NrgEnd ParseEndChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData) {
        // END! is always 0 bytes large
        if (chunkSize != 0) throw new InvalidOperationException("Malformed NRG format: END! chunk was not 0 bytes!");
        return new() { ChunkID = chunkID, ChunkSize = chunkSize };
    }

    static NrgFile ParseFrom(Stream stream) {
        var nrgf = new NrgFile();
        using var r = new BinaryReader(stream);
        try {
            stream.Seek(-12, SeekOrigin.End);
            nrgf.FileID = r.ReadF8String(4);
            if (nrgf.FileID == "NER5") {
                nrgf.FileOffset = r.ReadInt64();
                if (BitConverter.IsLittleEndian) nrgf.FileOffset = BinaryPrimitives.ReverseEndianness(nrgf.FileOffset);
                // suppose technically you can interpret this as ulong
                // but streams seek with long, and a CD can't be millions of TB anyways
                if (nrgf.FileOffset < 0) throw new InvalidOperationException("Malformed NRG format: Chunk file offset was negative!");
            }
            else {
                nrgf.FileID = r.ReadF8String(4);
                if (nrgf.FileID != "NERO") throw new InvalidOperationException("Malformed NRG format: Could not find NERO/NER5 signature!");
                nrgf.FileOffset = r.ReadUInt32();
                if (BitConverter.IsLittleEndian) nrgf.FileOffset = BinaryPrimitives.ReverseEndianness(nrgf.FileOffset);
            }
            stream.Seek(nrgf.FileOffset, SeekOrigin.Begin);

            void AssertIsV1() { if (nrgf.FileID != "NERO") throw new InvalidOperationException("Malformed NRG format: Found V1 chunk in a V2 file!"); }
            void AssertIsV2() { if (nrgf.FileID != "NER5") throw new InvalidOperationException("Malformed NRG format: Found V2 chunk in a V1 file!"); }

            while (nrgf.End is null) {
                var chunkID = r.ReadF8String(4);
                var chunkSize = r.ReadInt32();
                if (BitConverter.IsLittleEndian) chunkSize = BinaryPrimitives.ReverseEndianness(chunkSize);

                // can interpret this as uint rather
                // but chunks should never reach 2 GB anyways
                if (chunkSize < 0) throw new InvalidOperationException("Malformed NRG format: Chunk size was negative!");
                var chunkData = r.ReadBytes(chunkSize);
                if (chunkData.Length != chunkSize) throw new InvalidOperationException("Malformed NRG format: Unexpected stream end!");
                switch (chunkID) {
                    case "CUES": AssertIsV1(); nrgf.Cues.Add(ParseCueChunk(chunkID, chunkSize, chunkData)); break;
                    case "CUEX": AssertIsV2(); nrgf.Cues.Add(ParseCueChunk(chunkID, chunkSize, chunkData)); break;
                    case "DAOI": AssertIsV1(); nrgf.DAOTrackInfos.Add(ParseDaoChunk(chunkID, chunkSize, chunkData)); break;
                    case "DAOX": AssertIsV2(); nrgf.DAOTrackInfos.Add(ParseDaoChunk(chunkID, chunkSize, chunkData)); break;
                    case "TINF":
                    case "ETNF": AssertIsV1(); nrgf.TAOTrackInfos.Add(ParseEtnChunk(chunkID, chunkSize, chunkData)); break;
                    case "ETN2": AssertIsV2(); nrgf.TAOTrackInfos.Add(ParseEtnChunk(chunkID, chunkSize, chunkData)); break;
                    case "RELO": AssertIsV2(); nrgf.RELOs.Add(ParseReloChunk(chunkID, chunkSize, chunkData)); break;
                    case "TOCT": AssertIsV2(); nrgf.TOCTs.Add(ParseToctChunk(chunkID, chunkSize, chunkData)); break;
                    case "SINF": nrgf.SessionInfos.Add(ParseSinfChunk(chunkID, chunkSize, chunkData)); break;
                    case "CDTX": AssertIsV2(); if (nrgf.CdText is not null) throw new InvalidOperationException("Malformed NRG format: Found multiple CD text chunks!"); nrgf.CdText = ParseCdtxChunk(chunkID, chunkSize, chunkData); break;
                    case "MTYP": if (nrgf.MediaType is not null) throw new InvalidOperationException("Malformed NRG format: Found multiple media type chunks!"); nrgf.MediaType = ParseMtypChunk(chunkID, chunkSize, chunkData); break;
                    case "AFNM": if (nrgf.Filenames is not null) throw new InvalidOperationException("Malformed NRG format: Found multiple filenames chunks!"); nrgf.Filenames = ParseAfnmChunk(chunkID, chunkSize, chunkData); break;
                    case "VOLM": if (nrgf.VolumeName is not null) throw new InvalidOperationException("Malformed NRG format: Found multiple volume name chunks!"); nrgf.VolumeName = ParseVolmChunk(chunkID, chunkSize, chunkData); break;
                    case "END!": nrgf.End = ParseEndChunk(chunkID, chunkSize, chunkData); break;
                    default: Console.WriteLine($"Unknown NRG chunk {chunkID} encountered"); break;
                }
            }

            // sanity checks

            // SessionInfos will be empty if there is only 1 session
            var nsessions = Math.Max(nrgf.SessionInfos.Count, 1);
            if (nrgf.Cues.Count != nsessions) throw new InvalidOperationException("Malformed NRG format: CUE chunk count does not match session count!");
            if (nrgf.DAOTrackInfos.Count > 0) {
                if (nrgf.TAOTrackInfos.Count is not 0 || nrgf.RELOs.Count is not 0 || nrgf.TOCTs.Count is not 0) throw new InvalidOperationException("Malformed NRG format: DAO and TAO chunks both present on file!");
                else if (nrgf.DAOTrackInfos.Count != nsessions) throw new InvalidOperationException("Malformed NRG format: DAO chunk count does not match session count!");
            }
            else {
                if (nrgf.TAOTrackInfos.Count != nsessions) throw new InvalidOperationException("Malformed NRG format: TAO chunk count does not match session count!");
                else if (nrgf.TOCTs.Count != nsessions) throw new InvalidOperationException("Malformed NRG format: TOCT chunk count does not match session count!");
            }
            return nrgf;
        }
        catch (EndOfStreamException) { throw new InvalidOperationException("Malformed NRG format: Unexpected stream end!"); }
    }

    class LoadResult {
        public NrgFile ParsedNRGFile;
        public bool Valid;
        public InvalidOperationException FailureException;
        public string NrgPath;
    }

    static LoadResult LoadNrgPath(FileSystem vfx, string path) {
        var ret = new LoadResult { NrgPath = path };
        try {
            if (!vfx.FileExists(path)) throw new InvalidOperationException("Malformed NRG format: nonexistent NRG file!");
            using var s = vfx.Open(path);
            var nrgf = ParseFrom(s);
            ret.ParsedNRGFile = nrgf;
            ret.Valid = true;
        }
        catch (InvalidOperationException ex) { ret.FailureException = ex; }
        return ret;
    }

    internal static Disc LoadNrgToDisc(FileSystem vfx, string nrgPath, DiscMountPolicy discMountPolicy) {
        var loadResults = LoadNrgPath(vfx, nrgPath);
        if (!loadResults.Valid) throw loadResults.FailureException;
        var disc = new Disc();
        var nrgf = loadResults.ParsedNRGFile;
        Blob blob = new Blob_Raw(vfx) { Path = nrgPath };
        disc.DisposableResources.Add(blob);
        // SessionInfos will be empty if there is only 1 session
        var nsessions = Math.Max(nrgf.SessionInfos.Count, 1);
        var dao = nrgf.DAOTrackInfos.Count > 0; // tao otherwise
        for (var i = 0; i < nsessions; i++) {
            var session = new DiscSession { Number = i + 1 };
            int startTrack, endTrack;
            DiscSessionFormat sessionFormat;
            if (dao) {
                startTrack = nrgf.DAOTrackInfos[i].FirstTrack;
                endTrack = nrgf.DAOTrackInfos[i].LastTrack;
                sessionFormat = (DiscSessionFormat)nrgf.DAOTrackInfos[i].DiskType;
            }
            else {
                startTrack = 1 + nrgf.TAOTrackInfos.Take(i).Sum(t => t.Tracks.Count);
                endTrack = startTrack + nrgf.TAOTrackInfos[i].Tracks.Count - 1;
                sessionFormat = (DiscSessionFormat)nrgf.TOCTs[i].DiskType;
            }

            var TOCMiscInfo = new Synthesize_A0A1A2(
                firstRecordedTrackNumber: startTrack,
                lastRecordedTrackNumber: endTrack,
                sessionFormat: sessionFormat,
                leadoutTimestamp: nrgf.Cues[i].TrackIndices.First(t => t.Track.BCDValue == 0xAA).LBA + 150);
            TOCMiscInfo.Run(session.RawTOCEntries);

            foreach (var trackIndex in nrgf.Cues[i].TrackIndices)
                if (trackIndex.Track.BCDValue is not (0 or 0xAA) && trackIndex.Index.BCDValue == 1) {
                    var q = default(SubchannelQ);
                    q.q_status = trackIndex.ADRControl;
                    q.q_tno = BCD2.FromBCD(0);
                    q.q_index = trackIndex.Track;
                    q.Timestamp = 0;
                    q.zero = 0;
                    q.AP_Timestamp = trackIndex.LBA + 150;
                    q.q_crc = 0;
                    session.RawTOCEntries.Add(new() { QData = q });
                }

            // leadin track
            var leadinSize = i == 0 ? 0 : 4500;
            var isData = (session.RawTOCEntries.First(t => t.QData.q_index.DecimalValue == startTrack).QData.ADR & 4) != 0;
            for (var j = 0; j < leadinSize; j++) {
                var cueTrackType = CueTrackType.Audio;
                if (isData)
                    cueTrackType = sessionFormat switch {
                        DiscSessionFormat.Type00_CDROM_CDDA => CueTrackType.Mode1_2352,
                        DiscSessionFormat.Type10_CDI => CueTrackType.CDI_2352,
                        DiscSessionFormat.Type20_CDXA => CueTrackType.Mode2_2352,
                        _ => cueTrackType,
                    };
                disc.Sectors.Add(new SS_Gap { Policy = discMountPolicy, TrackType = cueTrackType });
            }

            static SS_Base CreateSynth(int mode) => mode switch {
                0x00 => new SS_Mode1_2048(),
                0x02 => new SS_Mode2_Form1_2048(),
                0x03 => new SS_Mode2_2336(),
                0x05 or 0x06 or 0x07 => new SS_2352(),
                0x0F or 0x10 or 0x11 => new SS_2448_Interleaved(),
                _ => throw new InvalidOperationException($"Invalid mode {mode}"),
            };

            if (dao) {
                var tracks = nrgf.DAOTrackInfos[i].Tracks;
                for (var j = 0; j < tracks.Count; j++) {
                    var track = nrgf.DAOTrackInfos[i].Tracks[j];
                    var relMSF = -(track.TrackStartFileOffset - track.PregapFileOffset) / track.SectorSize;
                    var trackNumBcd = BCD2.FromDecimal(startTrack + j);
                    var cueIndexes = nrgf.Cues[i].TrackIndices.Where(t => t.Track == trackNumBcd).ToArray();

                    // do the pregap
                    var pregapCueIndex = cueIndexes[0];
                    for (var k = track.PregapFileOffset; k < track.TrackStartFileOffset; k += track.SectorSize) {
                        var synth = CreateSynth(track.Mode);
                        synth.Blob = blob;
                        synth.BlobOffset = k;
                        synth.Policy = discMountPolicy;
                        synth.sq.q_status = pregapCueIndex.ADRControl;
                        synth.sq.q_tno = trackNumBcd;
                        synth.sq.q_index = BCD2.FromBCD(0);
                        synth.sq.Timestamp = !discMountPolicy.Cue_PregapContradictionModeA ? (int)relMSF + 1 : (int)relMSF;
                        synth.sq.zero = 0;
                        synth.sq.AP_Timestamp = disc.Sectors.Count;
                        synth.sq.q_crc = 0;
                        synth.Pause = true;
                        disc.Sectors.Add(synth);
                        relMSF++;
                    }

                    // actual data
                    var curIndex = 1;
                    for (var k = track.TrackStartFileOffset; k < track.TrackEndFileOffset; k += track.SectorSize) {
                        if (curIndex + 1 != cueIndexes.Length && disc.Sectors.Count == cueIndexes[curIndex + 1].LBA + 150) curIndex++;
                        var synth = CreateSynth(track.Mode);
                        synth.Blob = blob;
                        synth.BlobOffset = k;
                        synth.Policy = discMountPolicy;
                        synth.sq.q_status = cueIndexes[curIndex].ADRControl;
                        synth.sq.q_tno = trackNumBcd;
                        synth.sq.q_index = cueIndexes[curIndex].Index;
                        synth.sq.Timestamp = (int)relMSF;
                        synth.sq.zero = 0;
                        synth.sq.AP_Timestamp = disc.Sectors.Count;
                        synth.sq.q_crc = 0;
                        synth.Pause = false;
                        disc.Sectors.Add(synth);
                        relMSF++;
                    }
                }
            }
            else throw new NotSupportedException("TAO not supported yet!"); // TAO

            // leadout track
            var leadoutSize = i == 0 ? 6750 : 2250;
            for (var j = 0; j < leadoutSize; j++)
                disc.Sectors.Add(new SS_Leadout { SessionNumber = session.Number, Policy = discMountPolicy });
            disc.Sessions.Add(session);
        }
        return disc;
    }
}

#endregion

#region Sbi

/// <summary>
/// Loads SBI files into an internal representation.
/// </summary>
class SbiLoader {
    const uint MAGIC = 0x00494253; //: SBI\0

    public class SubQPatchData {
        /// <summary>
        /// a list of patched ABAs
        /// </summary>
        public readonly IList<int> Abas = [];

        /// <summary>
        /// 12 values (Q subchannel data) for every patched ABA; -1 means unpatched
        /// </summary>
        public short[] Subq;
    }

    /// <summary>
    /// The resulting interpreted data
    /// </summary>
    public SubQPatchData Sbi;

    public SbiLoader(FileSystem vfx, string path) {
        using var s = vfx.Open(path);
        var r = new BinaryReader(s);
        var sig = r.ReadUInt32();
        if (sig != MAGIC) throw new InvalidOperationException("BAD MAGIC");
        var ret = new SubQPatchData();
        var bytes = new List<short>();
        // read records until done
        while (true) {
            if (s.Position == s.Length) break;
            if (s.Position + 4 > s.Length) throw new InvalidOperationException("Broken record");
            var ts = new MSF(BCD2.BCDToInt(r.ReadByte()), BCD2.BCDToInt(r.ReadByte()), BCD2.BCDToInt(r.ReadByte()));
            ret.Abas.Add(ts.Sector);
            int type = r.ReadByte();
            switch (type) {
                case 1: // Q0..Q9
                    if (s.Position + 10 > s.Length) throw new InvalidOperationException("Broken record");
                    for (var i = 0; i <= 9; i++) bytes.Add(r.ReadByte());
                    for (var i = 10; i <= 11; i++) bytes.Add(-1);
                    break;
                case 2: // Q3..Q5
                    if (s.Position + 3 > s.Length) throw new InvalidOperationException("Broken record");
                    for (var i = 0; i <= 2; i++) bytes.Add(-1);
                    for (var i = 3; i <= 5; i++) bytes.Add(r.ReadByte());
                    for (var i = 6; i <= 11; i++) bytes.Add(-1);
                    break;
                case 3: // Q7..Q9
                    if (s.Position + 3 > s.Length) throw new InvalidOperationException("Broken record");
                    for (var i = 0; i <= 6; i++) bytes.Add(-1);
                    for (var i = 7; i <= 9; i++) bytes.Add(r.ReadByte());
                    for (var i = 10; i <= 11; i++) bytes.Add(-1);
                    break;
                default: throw new InvalidOperationException("Broken record");
            }
        }
        ret.Subq = bytes.ToArray();
        Sbi = ret;
    }

    /// <summary>
    /// applies an SBI file to the disc
    /// </summary>
    public void Apply(Disc disc, bool asMednafen) {
        // save this, it's small, and we'll want it for disc processing a/b checks
        disc.Memos["sbi"] = Sbi;

        var dsr = new DiscSectorReader(disc);

        var n = Sbi.Abas.Count;
        var b = 0;
        for (var i = 0; i < n; i++) {
            var lba = Sbi.Abas[i] - 150;

            // create a synthesizer which can return the patched data
            var ss_patchq = new SS_PatchQ { Original = disc.Sectors[lba + 150] };
            var subQbuf = ss_patchq.Buffer_SubQ;

            // read the old subcode
            dsr.ReadLBA_SubQ(lba, subQbuf, 0);

            // insert patch
            disc.Sectors[lba + 150] = ss_patchq;

            // apply SBI patch
            for (var j = 0; j < 12; j++) {
                var patch = Sbi.Subq[b++];
                if (patch == -1) continue;
                subQbuf[j] = (byte)patch;
            }

            // Apply mednafen hack
            if (asMednafen) { SynthUtils.SubQ_SynthChecksum(subQbuf, 0); subQbuf[10] ^= 0xFF; subQbuf[11] ^= 0xFF; }
        }
    }
}

#endregion

#region Synth

/// <summary>
/// Indicates which part of a sector are needing to be synthesized.
/// Sector synthesis may create too much data, but this is a hint as to what's needed
/// </summary>
[Flags]
enum ESectorSynthPart {
    /// <summary>
    /// The data sector header is required. There's no header for audio tracks/sectors.
    /// </summary>
    Header16 = 1,
    /// <summary>
    /// The main 2048 user data bytes are required
    /// </summary>
    User2048 = 2,
    /// <summary>
    /// The 276 bytes of error correction are required
    /// </summary>
    ECC276 = 4,
    /// <summary>
    /// The 12 bytes preceding the ECC section are required (usually EDC and zero but also userdata sometimes)
    /// </summary>
    EDC12 = 8,
    /// <summary>
    /// The entire possible 276+12=288 bytes of ECM data is required (ECC276|EDC12)
    /// </summary>
    ECM288Complete = (ECC276 | EDC12),
    /// <summary>
    /// An alias for ECM288Complete
    /// </summary>
    ECMAny = ECM288Complete,
    /// <summary>
    /// A mode2 userdata section is required: the main 2048 user bytes AND the ECC and EDC areas
    /// </summary>
    User2336 = (User2048 | ECM288Complete),
    /// <summary>
    /// The complete sector userdata (2352 bytes) is required
    /// </summary>
    UserComplete = (Header16 | User2048 | ECM288Complete),
    /// <summary>
    /// An alias for UserComplete
    /// </summary>
    UserAny = UserComplete,
    /// <summary>
    /// An alias for UserComplete
    /// </summary>
    User2352 = UserComplete,
    /// <summary>
    /// SubP is required
    /// </summary>
    SubchannelP = 16,
    /// <summary>
    /// SubQ is required
    /// </summary>
    SubchannelQ = 32,
    /// <summary>
    /// Subchannels R-W (all except for P and Q)
    /// </summary>
    Subchannel_RSTUVW = (64 | 128 | 256 | 512 | 1024 | 2048),
    /// <summary>
    /// Complete subcode is required
    /// </summary>
    SubcodeComplete = (SubchannelP | SubchannelQ | Subchannel_RSTUVW),
    /// <summary>
    /// Any of the subcode might be required (just another way of writing SubcodeComplete)
    /// </summary>
    SubcodeAny = SubcodeComplete,
    /// <summary>
    /// The subcode should be deinterleaved
    /// </summary>
    SubcodeDeinterleave = 4096,
    /// <summary>
    /// The 100% complete sector is required including 2352 bytes of userdata and 96 bytes of subcode
    /// </summary>
    Complete2448 = SubcodeComplete | User2352,
}

class SectorSynthJob {
    public int LBA;
    public ESectorSynthPart Parts;
    public byte[] DestBuffer2448;
    public int DestOffset;
    public SectorSynthParams Params;
    public Disc Disc;
}

/// <summary>
/// sector synthesis
/// </summary>
abstract class SectorSynthJob2448 {
    /// <summary>
    /// Synthesizes a sctor with the given job parameters
    /// </summary>
    public abstract void Synth(SectorSynthJob job);
}

/// <summary>
/// When creating a disc, this is set with a callback that can deliver an ISectorSynthJob2448 for the given LBA
/// </summary>
abstract class SectorSynthProvider {
    /// <summary>
    /// Retrieves an SectorSynthJob2448 for the given LBA
    /// </summary>
    public abstract SectorSynthJob2448 Get(int lba);
}

/// <summary>
/// an SectorSynthProvider that just returns a value from an array of pre-made sectors
/// </summary>
class ArraySectorSynthProvider : SectorSynthProvider {
    public List<SectorSynthJob2448> Sectors = [];
    public int FirstLBA;

    public override SectorSynthJob2448 Get(int lba) {
        var index = lba - FirstLBA;
        if (index < 0) return null;
        return index >= Sectors.Count ? null : Sectors[index];
    }
}

/// <summary>
/// an ISectorSynthProvider that just returns a fixed synthesizer
/// </summary>
class SimpleSectorSynthProvider : SectorSynthProvider {
    public SectorSynthJob2448 SS;
    public override SectorSynthJob2448 Get(int lba) => SS;
}

/// <summary>
/// Returns 'Patch' synth if the provided condition is met
/// </summary>
class ConditionalSectorSynthProvider : SectorSynthProvider {
    Func<int, bool> Condition;
    SectorSynthJob2448 Patch;
    SectorSynthProvider Parent;

    public void Install(Disc disc, Func<int, bool> condition, SectorSynthJob2448 patch) {
        Parent = disc.SynthProvider;
        disc.SynthProvider = this;
        Condition = condition;
        Patch = patch;
    }

    public override SectorSynthJob2448 Get(int lba) => Condition(lba) ? Patch : Parent.Get(lba);
}

/// <summary>
/// Generic parameters for sector synthesis.
/// To cut down on resource utilization, these can be stored in a disc and are tightly coupled to
/// the SectorSynths that have been setup for it
/// </summary>
struct SectorSynthParams {
    //public MednaDisc MednaDisc;
}

#region CRC16_CCITT

static class CRC16_CCITT {
    static readonly ushort[] table = new ushort[256];
    static CRC16_CCITT() {
        for (ushort i = 0; i < 256; ++i) {
            ushort value = 0;
            var temp = (ushort)(i << 8);
            for (byte j = 0; j < 8; ++j) {
                if (((value ^ temp) & 0x8000) != 0) value = (ushort)((value << 1) ^ 0x1021);
                else value <<= 1;
                temp <<= 1;
            }
            table[i] = value;
        }
    }

    public static ushort Calculate(byte[] data, int offset, int length) {
        ushort Result = 0;
        for (var i = 0; i < length; i++) {
            var b = data[offset + i];
            var index = (b ^ ((Result >> 8) & 0xFF));
            Result = (ushort)((Result << 8) ^ table[index]);
        }
        return Result;
    }
}

#endregion

static class SynthUtils {
    /// <summary>
    /// Calculates the checksum of the provided Q subchannel buffer and emplaces it
    /// </summary>
    /// <param name="buf12">12 byte Q subchannel buffer: input and output buffer for operation</param>
    /// <param name="offset">location within buffer of Q subchannel</param>
    public static ushort SubQ_SynthChecksum(byte[] buf12, int offset) {
        var crc16 = CRC16_CCITT.Calculate(buf12, offset, 10);
        // CRC is stored inverted and big endian
        buf12[offset + 10] = (byte)(~(crc16 >> 8));
        buf12[offset + 11] = (byte)(~(crc16));
        return crc16;
    }

    /// <summary>
    /// Calculates the checksum of the provided Q subchannel buffer
    /// </summary>
    public static ushort SubQ_CalcChecksum(byte[] buf12, int offset) => CRC16_CCITT.Calculate(buf12, offset, 10);

    /// <summary>
    /// Serializes the provided SubchannelQ structure into a buffer
    /// Returns the crc, calculated or otherwise.
    /// </summary>
    public static ushort SubQ_Serialize(byte[] buf12, int offset, ref SubchannelQ sq) {
        buf12[offset + 0] = sq.q_status;
        buf12[offset + 1] = sq.q_tno.BCDValue;
        buf12[offset + 2] = sq.q_index.BCDValue;
        buf12[offset + 3] = sq.min.BCDValue;
        buf12[offset + 4] = sq.sec.BCDValue;
        buf12[offset + 5] = sq.frame.BCDValue;
        buf12[offset + 6] = sq.zero;
        buf12[offset + 7] = sq.ap_min.BCDValue;
        buf12[offset + 8] = sq.ap_sec.BCDValue;
        buf12[offset + 9] = sq.ap_frame.BCDValue;
        return SubQ_SynthChecksum(buf12, offset);
    }

    /// <summary>
    /// Synthesizes the typical subP data into the provided buffer depending on the indicated pause flag
    /// </summary>
    public static void SubP(byte[] buf12, int offset, bool pause) {
        var val = (byte)(pause ? 0xFF : 0x00);
        for (var i = 0; i < 12; i++) buf12[offset + i] = val;
    }

    /// <summary>
    /// Synthesizes a data sector header
    /// </summary>
    public static void SectorHeader(byte[] buf16, int offset, int lba, byte mode) {
        buf16[offset + 0] = 0x00;
        for (var i = 1; i < 11; i++) buf16[offset + i] = 0xFF;
        buf16[offset + 11] = 0x00;
        var ts = new MSF(lba + 150);
        buf16[offset + 12] = BCD2.IntToBCD(ts.Min);
        buf16[offset + 13] = BCD2.IntToBCD(ts.Sec);
        buf16[offset + 14] = BCD2.IntToBCD(ts.Frac);
        buf16[offset + 15] = mode;
    }

    /// <summary>
    /// Synthesizes a Mode2 sector subheader
    /// </summary>
    public static void SectorSubHeader(byte[] buffer8, int offset, byte form) {
        // see mirage_sector_generate_subheader
        for (var i = 0; i < 8; i++) buffer8[offset + i] = 0;
        if (form == 2) {
            // these are just 0 in form 1
            buffer8[offset + 2] = 0x20;
            buffer8[offset + 5] = 0x20;
        }
    }

    /// <summary>
    /// Synthesizes the EDC checksum for a Mode 1 data sector (and puts it in place)
    /// </summary>
    public static void EDC_Mode1(byte[] buf2352, int offset) {
        var edc = ECM.EDC_Calc(buf2352, offset, 2064);
        ECM.PokeUint(buf2352, offset + 2064, edc);
    }

    /// <summary>
    /// Synthesizes the EDC checksum for a Mode 2 Form 1 data sector (and puts it in place)
    /// </summary>
    public static void EDC_Mode2_Form1(byte[] buf2352, int offset) {
        var edc = ECM.EDC_Calc(buf2352, offset + 16, 2048 + 8);
        ECM.PokeUint(buf2352, offset + 2072, edc);
    }

    /// <summary>
    /// Synthesizes the EDC checksum for a Mode 2 Form 2 data sector (and puts it in place)
    /// </summary>
    public static void EDC_Mode2_Form2(byte[] buf2352, int offset) {
        var edc = ECM.EDC_Calc(buf2352, offset + 16, 2324 + 8);
        ECM.PokeUint(buf2352, offset + 2348, edc);
    }

    /// <summary>
    /// Synthesizes the complete ECM data (EDC + ECC) for a Mode 1 data sector (and puts it in place)
    /// Make sure everything else in the sector header and userdata is done before calling this
    /// </summary>
    public static void ECM_Mode1(byte[] buf2352, int offset) {
        EDC_Mode1(buf2352, offset); // EDC
        for (var i = 0; i < 8; i++) buf2352[offset + 2068 + i] = 0; // reserved, zero
        ECM.ECC_Populate(buf2352, offset, buf2352, offset, false); // ECC
    }

    /// <summary>
    /// Synthesizes the complete ECM data (Subheader + EDC + ECC) for a Mode 2 Form 1 data sector (and puts it in place)
    /// Make sure everything else in the sector header and userdata is done before calling this
    /// </summary>
    public static void ECM_Mode2_Form1(byte[] buf2352, int offset) {
        SectorSubHeader(buf2352, offset + 16, 1); // Subheader
        EDC_Mode2_Form1(buf2352, offset); // EDC
        ECM.ECC_Populate(buf2352, offset, buf2352, offset, false); // ECC
    }

    /// <summary>
    /// Synthesizes the complete ECM data (Subheader + EDC) for a Mode 2 Form 2 data sector (and puts it in place)
    /// Make sure everything else in the userdata is done before calling this
    /// </summary>
    public static void ECM_Mode2_Form2(byte[] buf2352, int offset) {
        SectorSubHeader(buf2352, offset + 16, 2); // Subheader
        EDC_Mode2_Form2(buf2352, offset); // EDC
        // note that Mode 2 Form 2 does not have ECC
    }

    /// <summary>
    /// Converts the useful (but unrealistic) deinterleaved subchannel data into the useless (but realistic) interleaved format.
    /// in_buf and out_buf should not overlap
    /// </summary>
    public static void InterleaveSubcode(byte[] src, int srcOffset, byte[] dst, int dstOffset) {
        for (var d = 0; d < 12; d++)
            for (var bitpoodle = 0; bitpoodle < 8; bitpoodle++) {
                var rawb = 0;
                for (var ch = 0; ch < 8; ch++) rawb |= ((src[ch * 12 + d + srcOffset] >> (7 - bitpoodle)) & 1) << (7 - ch);
                dst[(d << 3) + bitpoodle + dstOffset] = (byte)rawb;
            }
    }

    /// <summary>
    /// Converts the useless (but realistic) interleaved subchannel data into a useful (but unrealistic) deinterleaved format.
    /// in_buf and out_buf should not overlap
    /// </summary>
    public static void DeinterleaveSubcode(byte[] src, int srcOffset, byte[] dst, int dstOffset) {
        for (var i = 0; i < 96; i++) dst[i] = 0;
        for (var ch = 0; ch < 8; ch++)
            for (var i = 0; i < 96; i++) dst[(ch * 12) + (i >> 3) + dstOffset] |= (byte)(((src[i + srcOffset] >> (7 - ch)) & 0x1) << (7 - (i & 0x7)));
    }

    /// <summary>
    /// Converts the useful (but unrealistic) deinterleaved data into the useless (but realistic) interleaved subchannel format.
    /// </summary>
    public static unsafe void InterleaveSubcodeInplace(byte[] buf, int bufOffset) {
        var b = stackalloc byte[96];
        for (var i = 0; i < 96; i++) b[i] = 0;
        for (var d = 0; d < 12; d++)
            for (var bitpoodle = 0; bitpoodle < 8; bitpoodle++) {
                var rawb = 0;
                for (var ch = 0; ch < 8; ch++) rawb |= ((buf[ch * 12 + d + bufOffset] >> (7 - bitpoodle)) & 1) << (7 - ch);
                b[(d << 3) + bitpoodle] = (byte)rawb;
            }
        for (var i = 0; i < 96; i++) buf[i + bufOffset] = b[i];
    }

    /// <summary>
    /// Converts the useless (but realistic) interleaved subchannel data into a useful (but unrealistic) deinterleaved format.
    /// </summary>
    public static unsafe void DeinterleaveSubcodeInplace(byte[] buf, int bufOffset) {
        var b = stackalloc byte[96];
        for (var i = 0; i < 96; i++) b[i] = 0;
        for (var ch = 0; ch < 8; ch++)
            for (var i = 0; i < 96; i++) b[(ch * 12) + (i >> 3)] |= (byte)(((buf[i + bufOffset] >> (7 - ch)) & 0x1) << (7 - (i & 0x7)));
        for (var i = 0; i < 96; i++) buf[i + bufOffset] = b[i];
    }
}

#endregion

#region Synth : Jobs

class SS_PatchQ : SectorSynthJob2448 {
    public SectorSynthJob2448 Original;
    public readonly byte[] Buffer_SubQ = new byte[12];

    public override void Synth(SectorSynthJob job) {
        Original.Synth(job);
        if ((job.Parts & ESectorSynthPart.SubchannelQ) == 0) return;
        // apply patched subQ
        for (var i = 0; i < 12; i++) job.DestBuffer2448[2352 + 12 + i] = Buffer_SubQ[i];
    }
}

class SS_Leadout : SectorSynthJob2448 {
    public int SessionNumber;
    public DiscMountPolicy Policy;

    public override void Synth(SectorSynthJob job) {
        var ses = job.Disc.Sessions[SessionNumber];
        var lba_relative = job.LBA - ses.LeadoutTrack.LBA;
        // data is zero
        var ts = lba_relative;
        var ats = job.LBA;
        SubchannelQ sq = default;
        sq.SetStatus(SubchannelQ.kADR, ses.LeadoutTrack.Control | (EControlQ)(((int)ses.LastInformationTrack.Control) & 4));
        sq.q_tno.BCDValue = 0xAA;
        sq.q_index.BCDValue = 0x01;
        sq.Timestamp = ts;
        sq.AP_Timestamp = ats;
        sq.zero = 0;
        var TrackType = CueTrackType.Audio;
        if (ses.LeadoutTrack.IsData)
            TrackType = job.Disc.TOC.SessionFormat is DiscSessionFormat.Type20_CDXA or DiscSessionFormat.Type10_CDI
                ? CueTrackType.Mode2_2352
                : CueTrackType.Mode1_2352;
        var ss_gap = new SS_Gap {
            Policy = Policy,
            sq = sq,
            TrackType = TrackType,
            Pause = true
        };
        ss_gap.Synth(job);
    }
}

abstract class SS_Base : SectorSynthJob2448 {
    public Blob Blob;
    public long BlobOffset;
    public DiscMountPolicy Policy;
    public SubchannelQ sq; // subQ data
    public bool Pause; // subP data

    protected void SynthSubchannelAsNeed(SectorSynthJob job) {
        if ((job.Parts & ESectorSynthPart.SubchannelP) != 0) SynthUtils.SubP(job.DestBuffer2448, job.DestOffset + 2352, Pause); // synth P if needed
        if ((job.Parts & ESectorSynthPart.SubchannelQ) != 0) SynthUtils.SubQ_Serialize(job.DestBuffer2448, job.DestOffset + 2352 + 12, ref sq); // synth Q if needed
        if ((job.Parts & ESectorSynthPart.Subchannel_RSTUVW) != 0) Array.Clear(job.DestBuffer2448, job.DestOffset + 2352 + 12 + 12, (12 * 6)); // clear R-W if needed
        // subcode has been generated deinterleaved; we may still need to interleave it
        if ((job.Parts & ESectorSynthPart.SubcodeAny) != 0 && (job.Parts & ESectorSynthPart.SubcodeDeinterleave) == 0) SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
    }
}

/// <summary>
/// Represents a Mode1 2048-byte sector
/// </summary>
class SS_Mode1_2048 : SS_Base {
    public override void Synth(SectorSynthJob job) {
        var ecm = (job.Parts & ESectorSynthPart.ECMAny) != 0;
        if (ecm) job.Parts |= ESectorSynthPart.User2048 | ESectorSynthPart.Header16; // ecm needs these parts for synth
        // read the sector user data
        if ((job.Parts & ESectorSynthPart.User2048) != 0) Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 16, 2048);
        if ((job.Parts & ESectorSynthPart.Header16) != 0) SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 1);
        if (ecm) SynthUtils.ECM_Mode1(job.DestBuffer2448, job.DestOffset + 0);
        SynthSubchannelAsNeed(job);
    }
}

/// <summary>
/// Represents a Mode2 2336-byte sector
/// </summary>
class SS_Mode2_2336 : SS_Base {
    public override void Synth(SectorSynthJob job) {
        // read the sector sector user data + ECM data
        if ((job.Parts & ESectorSynthPart.User2336) != 0) Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 16, 2336);
        if ((job.Parts & ESectorSynthPart.Header16) != 0) SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 2);
        SynthSubchannelAsNeed(job); // if subcode is needed, synthesize it
    }
}

/// <summary>
/// Represents a 2352-byte sector of any sort
/// </summary>
class SS_2352 : SS_Base {
    public override void Synth(SectorSynthJob job) {
        // read the sector user data
        if ((job.Parts & ESectorSynthPart.User2352) != 0) Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2352);
        SynthSubchannelAsNeed(job); // if subcode is needed, synthesize it
    }
}

/// <summary>
/// Encodes a pre-gap sector
/// </summary>
class SS_Gap : SS_Base {
    public CueTrackType TrackType;

    public override void Synth(SectorSynthJob job) {
        Array.Clear(job.DestBuffer2448, job.DestOffset, 2352);
        byte mode; var form = -1;
        switch (TrackType) {
            case CueTrackType.Audio: mode = 0; break;
            case CueTrackType.CDI_2352:
            case CueTrackType.Mode1_2352: mode = 1; break;
            case CueTrackType.CDI_2336:
            case CueTrackType.Mode2_2336:
            case CueTrackType.Mode2_2352:
                mode = 2;
                if (Policy.Cue_PregapMode2_AsXAForm2) {
                    job.DestBuffer2448[job.DestOffset + 12 + 6] = 0x20;
                    job.DestBuffer2448[job.DestOffset + 12 + 10] = 0x20;
                }
                form = 2; //no other choice right now really
                break;
            case CueTrackType.Mode1_2048: mode = 1; Pause = true; break;
            default: throw new InvalidOperationException($"Not supported: {TrackType}");
        }
        // audio has no sector header but the others do
        if (mode != 0)
            if ((job.Parts & ESectorSynthPart.Header16) != 0) SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, mode);
        switch (mode) {
            case 1: if ((job.Parts & ESectorSynthPart.ECMAny) != 0) SynthUtils.ECM_Mode1(job.DestBuffer2448, job.DestOffset + 0); break;
            case 2 when form == 2: SynthUtils.EDC_Mode2_Form2(job.DestBuffer2448, job.DestOffset); break;
        }
        SynthSubchannelAsNeed(job);
    }
}

/// <summary>
/// Represents a Mode2 Form1 2048-byte sector
/// Only used by NRG, MDS, and CHD
/// </summary>
class SS_Mode2_Form1_2048 : SS_Base {
    public override void Synth(SectorSynthJob job) {
        var ecm = (job.Parts & ESectorSynthPart.ECMAny) != 0;
        if (ecm) {
            // ecm needs these parts for synth
            job.Parts |= ESectorSynthPart.User2048;
            job.Parts |= ESectorSynthPart.Header16;
        }
        // read the sector user data
        if ((job.Parts & ESectorSynthPart.User2048) != 0) Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 24, 2048);
        if ((job.Parts & ESectorSynthPart.Header16) != 0) SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 2);
        if (ecm) SynthUtils.ECM_Mode2_Form1(job.DestBuffer2448, job.DestOffset);
        SynthSubchannelAsNeed(job);
    }
}

/// <summary>
/// Represents a Mode2 Form1 2324-byte sector
/// Only used by MDS and CHD
/// </summary>
class SS_Mode2_Form2_2324 : SS_Base {
    public override void Synth(SectorSynthJob job) {
        // read the sector userdata (note: ECC data is now userdata in this regard)
        if ((job.Parts & ESectorSynthPart.User2336) != 0) {
            Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 24, 2324);
            // only needs userdata for synth
            SynthUtils.ECM_Mode2_Form2(job.DestBuffer2448, job.DestOffset);
        }
        if ((job.Parts & ESectorSynthPart.Header16) != 0) SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 2);
        SynthSubchannelAsNeed(job);
    }
}

/// <summary>
/// Represents a Mode2 Form1 2328-byte sector
/// Only used by MDS
/// </summary>
class SS_Mode2_Form2_2328 : SS_Base {
    public override void Synth(SectorSynthJob job) {
        // read the sector userdata (note: ECC data is now userdata in this regard)
        if ((job.Parts & ESectorSynthPart.User2336) != 0) {
            Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 24, 2328);
            // only subheader needs to be synthed
            SynthUtils.SectorSubHeader(job.DestBuffer2448, job.DestOffset + 16, 2);
        }
        if ((job.Parts & ESectorSynthPart.Header16) != 0) SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 2);
        SynthSubchannelAsNeed(job);
    }
}

/// <summary>
/// Represents a full 2448-byte sector with interleaved subcode
/// Only used by MDS, NRG, and CDI
/// </summary>
class SS_2448_Interleaved : SS_Base {
    public override void Synth(SectorSynthJob job) {
        // all subcode is present and interleaved, just read it all
        Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2448);
        // deinterleave it if needed
        if ((job.Parts & ESectorSynthPart.SubcodeDeinterleave) != 0) SynthUtils.DeinterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
    }
}

/// <summary>
/// Represents a 2364-byte (2352 + 12) sector with deinterleaved Q subcode
/// Only used by CDI
/// </summary>
class SS_2364_DeinterleavedQ : SS_Base {
    public override void Synth(SectorSynthJob job) {
        if ((job.Parts & ESectorSynthPart.User2352) != 0) Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2352);
        if ((job.Parts & ESectorSynthPart.SubchannelP) != 0) SynthUtils.SubP(job.DestBuffer2448, job.DestOffset + 2352, Pause);
        // Q is present in the blob and non-interleaved
        if ((job.Parts & ESectorSynthPart.SubchannelQ) != 0) Blob.Read(BlobOffset + 2352, job.DestBuffer2448, job.DestOffset + 2352 + 12, 12);
        // clear R-W if needed
        if ((job.Parts & ESectorSynthPart.Subchannel_RSTUVW) != 0) Array.Clear(job.DestBuffer2448, job.DestOffset + 2352 + 12 + 12, 12 * 6);
        // subcode has been generated deinterleaved; we may still need to interleave it
        if ((job.Parts & ESectorSynthPart.SubcodeAny) != 0 && (job.Parts & ESectorSynthPart.SubcodeDeinterleave) == 0) SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
    }
}

#endregion

#region Synth : Jobs2

/// <summary>
/// Synthesizes RawTCOEntry A0 A1 A2 from the provided information.
/// </summary>
/// <param name="firstRecordedTrackNumber">"First Recorded Track Number" value for TOC (usually 1)</param>
/// <param name="lastRecordedTrackNumber">"Last Recorded Track Number" value for TOC</param>
/// <param name="sessionFormat">The session format for this TOC</param>
/// <param name="leadoutTimestamp">The absolute timestamp of the lead-out track</param>
class Synthesize_A0A1A2(int firstRecordedTrackNumber, int lastRecordedTrackNumber, DiscSessionFormat sessionFormat, int leadoutTimestamp) {
    readonly int FirstRecordedTrackNumber = firstRecordedTrackNumber;
    readonly int LastRecordedTrackNumber = lastRecordedTrackNumber;
    readonly DiscSessionFormat SessionFormat = sessionFormat;
    readonly int LeadoutTimestamp = leadoutTimestamp;

    /// <summary>appends the new entries to the provided list</summary>
    /// <exception cref="InvalidOperationException"><see cref="SessionFormat"/> is <see cref="SessionFormat.None"/> or a non-member</exception>
    public void Run(List<RawTOCEntry> entries) {
        SubchannelQ sq = default;
        sq.SetStatus(SubchannelQ.kADR, SubchannelQ.kUnknownControl);
        // first recorded track number:
        sq.q_index.BCDValue = 0xA0;
        sq.ap_min.DecimalValue = FirstRecordedTrackNumber;
        sq.ap_sec.DecimalValue = SessionFormat switch {
            DiscSessionFormat.Type00_CDROM_CDDA => 0x00,
            DiscSessionFormat.Type10_CDI => 0x10,
            DiscSessionFormat.Type20_CDXA => 0x20,
            _ => throw new InvalidOperationException("Invalid SessionFormat"),
        };
        sq.ap_frame.DecimalValue = 0;
        entries.Insert(0, new() { QData = sq });

        // last recorded track number:
        sq.q_index.BCDValue = 0xA1;
        sq.ap_min.DecimalValue = LastRecordedTrackNumber;
        sq.ap_sec.DecimalValue = 0;
        sq.ap_frame.DecimalValue = 0;
        entries.Insert(1, new() { QData = sq });

        // leadout
        sq.q_index.BCDValue = 0xA2;
        sq.AP_Timestamp = LeadoutTimestamp;
        entries.Insert(2, new() { QData = sq });
    }
}

/// <summary>
/// Synthesizes the TOC from a set of raw entries.
/// </summary>
class Synthesize_DiscTOCFromRawTOCEntries(IReadOnlyList<RawTOCEntry> entries) {
    readonly IReadOnlyList<RawTOCEntry> Entries = entries;

    public DiscTOC Run() {
        var r = new DiscTOC();
        r.TOCItems[0].LBA = 0; //arguably could be -150, but let's not just yet
        r.TOCItems[0].Control = 0;
        r.TOCItems[0].Exists = false;
        int minFoundTrack = 100, maxFoundTrack = 1;
        foreach (var te in Entries) {
            var q = te.QData;
            var point = q.q_index.DecimalValue;
            // see ECMD-394 page 5-14 for info about point = 0xA0, 0xA1, 0xA2
            switch (point) {
                case 0x00: Log("unexpected POINT=00 in lead-in Q-channel"); break;
                case 255: throw new InvalidOperationException("point == 255");
                case <= 99:
                    minFoundTrack = Math.Min(minFoundTrack, point);
                    maxFoundTrack = Math.Max(maxFoundTrack, point);
                    r.TOCItems[point].LBA = q.AP_Timestamp - 150;
                    r.TOCItems[point].Control = q.CONTROL;
                    r.TOCItems[point].Exists = true;
                    break;
                // 0xA0 bcd
                case 100: {
                        r.FirstRecordedTrackNumber = q.ap_min.DecimalValue;
                        if (q.ap_frame.DecimalValue != 0) Log("PFRAME should be 0 for POINT=0xA0");
                        switch (q.ap_sec.DecimalValue) {
                            case 0x00: r.SessionFormat = DiscSessionFormat.Type00_CDROM_CDDA; break;
                            case 0x10: r.SessionFormat = DiscSessionFormat.Type10_CDI; break;
                            case 0x20: r.SessionFormat = DiscSessionFormat.Type20_CDXA; break;
                            default: Log("Unrecognized session format: PSEC should be one of {0x00,0x10,0x20} for POINT=0xA0"); break;
                        }
                        break;
                    }
                // 0xA1 bcd
                case 101: {
                        r.LastRecordedTrackNumber = q.ap_min.DecimalValue;
                        if (q.ap_sec.DecimalValue != 0) Log("PSEC should be 0 for POINT=0xA1");
                        if (q.ap_frame.DecimalValue != 0) Log("PFRAME should be 0 for POINT=0xA1");
                        break;
                    }
                // 0xA2 bcd
                case 102:
                    r.TOCItems[100].LBA = q.AP_Timestamp - 150;
                    r.TOCItems[100].Control = 0;
                    r.TOCItems[100].Exists = true;
                    break;
            }
        }
        if (r.FirstRecordedTrackNumber == -1) { r.FirstRecordedTrackNumber = minFoundTrack == 100 ? 1 : minFoundTrack; }
        if (r.LastRecordedTrackNumber == -1) { r.LastRecordedTrackNumber = maxFoundTrack; }
        if (r.SessionFormat == DiscSessionFormat.None) r.SessionFormat = DiscSessionFormat.Type00_CDROM_CDDA;
        return r;
    }
}

class Synthesize_DiscTracksFromDiscTOC(Disc disc, DiscSession session) {
    readonly Disc Disc = disc;
    readonly DiscSession Session = session;
    DiscTOC TOCRaw => Session.TOC;
    IList<DiscTrack> Tracks => Session.Tracks;

    public void Run() {
        var dsr = new DiscSectorReader(Disc) { Policy = { DeterministicClearBuffer = false } };
        // add a lead-in track
        Tracks.Add(new() {
            Number = 0,
            Control = EControlQ.None,
            LBA = -new MSF(99, 99, 99).Sector
        });
        // add tracks
        for (var i = TOCRaw.FirstRecordedTrackNumber; i <= TOCRaw.LastRecordedTrackNumber; i++) {
            var item = TOCRaw.TOCItems[i];
            var track = new DiscTrack {
                Number = i,
                Control = item.Control,
                LBA = item.LBA
            };
            Tracks.Add(track);
            // determine the mode by a hardcoded heuristic: check mode of first sector
            track.Mode = !item.IsData ? 0 : dsr.ReadLBA_Mode(track.LBA);
        }
        // add lead-out track
        Tracks.Add(new() {
            Number = 0xA0,
            Control = Tracks[Tracks.Count - 1].Control,
            Mode = Tracks[Tracks.Count - 1].Mode,
            LBA = TOCRaw.LeadoutLBA,
        });
        // link track list
        for (var i = 0; i < Tracks.Count - 1; i++)
            Tracks[i].NextTrack = Tracks[i + 1];
        // fix lead-in track type
        Tracks[0].Control = Tracks[1].Control;
        Tracks[0].Mode = Tracks[1].Mode;
    }
}

#endregion
