using OpenStack.ExtServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static OpenStack.Debug;

namespace OpenStack.Vfx.Disc;

#region FileSystem : Cue

/// <summary>
/// CueFileSystem
/// </summary>
public class CueFileSystem : FileSystem {
    public CueFileSystem(FileSystem parent, Stream stream, string basePath) {
        var res = CueDisc.Create(parent, stream, basePath);
        Console.Write("KERE");
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

class Blob_CHD(IntPtr chdFile, uint hunkSize) : Blob {
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

class Blob_ECM : Blob {
    FileStream s;

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

    public override void Dispose() { s?.Dispose(); s = null; }

    public void Load(string path) {
        s = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        // skip header
        s.Seek(4, SeekOrigin.Current);
        var logOffset = 0L;
        while (true) {
            //read block count. this format is really stupid. maybe its good for detecting non-ecm files or something.
            var b = s.ReadByte();
            if (b == -1) throw new InvalidOperationException("Mis-formed ECM file");
            var bytes = 1;
            var T = b & 3;
            var N = (b >> 2) & 0x1FL;
            var nbits = 5;
            while ((b & (1 << 7)) != 0) {
                if (bytes == 5) throw new InvalidOperationException("Mis-formed ECM file"); //if we're gonna need a 6th byte, this file is broken
                b = s.ReadByte();
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
            Index.Add(new IndexEntry(type: T, number: pos, ecmOffset: s.Position, logicalOffset: logOffset));
            switch (T) {
                case 0: s.Seek(pos, SeekOrigin.Current); logOffset += pos; break;
                case 1: s.Seek(pos * (2048 + 3), SeekOrigin.Current); logOffset += pos * 2352; break;
                case 2: s.Seek(pos * 2052, SeekOrigin.Current); logOffset += pos * 2336; break;
                case 3: s.Seek(pos * 2328, SeekOrigin.Current); logOffset += pos * 2336; break;
                default: throw new InvalidOperationException("Mis-formed ECM file");
            }
        }
        var r = new BinaryReader(s);
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
                s.Position = ie.ECMOffset + blockOffset;
                while (todo > 0) {
                    int toRead;
                    if (todo > int.MaxValue) toRead = int.MaxValue;
                    else toRead = (int)todo;
                    var done = s.Read(buffer, offset, toRead);
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
                s.Position = ie.ECMOffset + inSecSize * secNumberInBlock;
                // read and decode the sector
                switch (ie.Type) {
                    case 1: if (s.Read(Read_SectorBuf, 16, 2048) != 2048) return completed; Reconstruct(Read_SectorBuf, 1); break;
                    case 2: if (s.Read(Read_SectorBuf, 20, 2052) != 2052) return completed; Reconstruct(Read_SectorBuf, 2); break;
                    case 3: if (s.Read(Read_SectorBuf, 20, 2328) != 2328) return completed; Reconstruct(Read_SectorBuf, 3); break;
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

class Blob_RawFile : Blob {
    BufferedStream s;
    string physicalPath;
    public readonly long Offset = 0;
    public long Length;
    public string PhysicalPath {
        get => physicalPath;
        set { physicalPath = value; Length = new FileInfo(physicalPath).Length; }
    }

    public override void Dispose() { s?.Dispose(); s = null; }

    public override int Read(long byte_pos, byte[] buffer, int offset, int count) {
        const int buffersize = 2352 * 75 * 2;
        s ??= new(new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read), buffersize);
        var target = byte_pos + Offset;
        if (s.Position != target) s.Position = target;
        return s.Read(buffer, offset, count);
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

class Blob_WaveFile : Blob {
    RiffMaster RiffSource;
    long waveDataStreamPos;
    public long Length;

    public override void Dispose() { RiffSource?.Dispose(); RiffSource = null; }

    public void Load(byte[] waveData) { }
    public void Load(string wavePath) {
        var s = new FileStream(wavePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Load(s);
    }
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

class DiscContext(FileSystem system, string baseDir) {
    public FileSystem System = system;
    public CueFileResolver FileResolver = new(system, baseDir);
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

#region Disk : Cue

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

public class CueFileResolver(FileSystem system, string baseDir) {
    string BaseDir = baseDir;
    string[] BaseDirPaths = [.. system.Glob("", "")]; // list all files, so we don't scan repeatedly.
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
                    var fs = Ctx.System.Open(choice);
                    using var blob = new Blob_WaveFile();
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
            default: cfi.Type = CueFileType.Unknown; Error($"Unknown cue file type. Since it's likely an unsupported compression, this is an error: {Path.GetFileName(choice)}"); break;
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
        if (CueTracks[1].PregapLength.Sector is not (0 or 150)) Error("Track 1 specified an illegal pregap. It's being ignored and replaced with a 00:02:00 pregap");
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
    static readonly int SlowLoadAbortThreshold = 10;

    /// <summary>
    /// Whether a mount operation was aborted due to being too slow
    /// </summary>
    static bool SlowLoadAborted;

    enum BurnType { Normal, Pregap, Postgap }

    class BlobInfo {
        public Blob Blob;
        public long Length;
    }

    readonly struct TrackInfo(CueCompiler.CueTrack cueTrack) {
        public readonly CueCompiler.CueTrack CueTrack = cueTrack;
    }

    List<BlobInfo> BlobInfos = [];
    readonly List<TrackInfo> TrackInfos = [];
    readonly CueCompiler Cmp;
    DiscSession CurrentSession = new() { Number = 1 };

    internal static object Create(FileSystem parent, Stream stream, string basePath) {
        SlowLoadAborted = false;
        var cue = new CueFile(stream);
        if (cue.HasError) return null;
        var cmp = new CueCompiler(cue, new DiscContext(parent, ""));
        if (cmp.HasError) return null;
        if (cmp.LoadTime > SlowLoadAbortThreshold) { Warn("Loading terminated due to slow load threshold"); SlowLoadAborted = true; return null; }
        return new CueDisc(cmp);
    }

    internal CueDisc(CueCompiler cmp) {
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
                ss.sq.AP_Timestamp = this.Sectors.Count;
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
        foreach (var ccf in Cmp.CueFiles) {
            var bi = new BlobInfo();
            BlobInfos.Add(bi);
            Blob file_blob;
            switch (ccf.Type) {
                case CueCompiler.CueFileType.BIN:
                case CueCompiler.CueFileType.Unknown: {
                        //raw files:
                        var blob = new Blob_RawFile { PhysicalPath = ccf.FullPath };
                        DisposableResources.Add(file_blob = blob);
                        bi.Length = blob.Length;
                        break;
                    }
                case CueCompiler.CueFileType.ECM: {
                        var blob = new Blob_ECM();
                        DisposableResources.Add(file_blob = blob);
                        blob.Load(ccf.FullPath);
                        bi.Length = blob.Length;
                        break;
                    }
                case CueCompiler.CueFileType.WAVE: {
                        var blob = new Blob_WaveFile();
                        DisposableResources.Add(file_blob = blob);
                        blob.Load(ccf.FullPath);
                        bi.Length = blob.Length;
                        break;
                    }
                case CueCompiler.CueFileType.DecodeAudio: {
                        if (!FFmpegService.QueryServiceAvailable(out var version))
                            throw new Exception($"{ccf.FullPath}: No decoding service was available (make sure ffmpeg.exe is available. Even though this may be a wav, ffmpeg is used to load oddly formatted wave files.)");
                        DiscAudioDecoder dec = new();
                        var buf = dec.AcquireWaveData(ccf.FullPath);
                        var blob = new Blob_WaveFile();
                        DisposableResources.Add(file_blob = blob);
                        blob.Load(new MemoryStream(buf));
                        bi.Length = buf.Length;
                        break;
                    }
                default: throw new InvalidOperationException();
            }
            bi.Blob = new Blob_ZeroPadAdapter(file_blob, bi.Length); // wrap all the blobs with zero padding
        }
    }

    void AnalyzeTracks() {
        foreach (var cct in Cmp.CueTracks)
            TrackInfos.Add(new(cct));
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

#endregion

#region Synthesize

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
        byte mode = 255; var form = -1;
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
        if (mode != 0) {
            if ((job.Parts & ESectorSynthPart.Header16) != 0) SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, mode);
        }
        switch (mode) {
            case 1: { if ((job.Parts & ESectorSynthPart.ECMAny) != 0) SynthUtils.ECM_Mode1(job.DestBuffer2448, job.DestOffset + 0); break; }
            case 2 when form == 2: SynthUtils.EDC_Mode2_Form2(job.DestBuffer2448, job.DestOffset); break;
        }
        SynthSubchannelAsNeed(job);
    }
}

/// <summary>
/// Synthesizes RawTCOEntry A0 A1 A2 from the provided information.
/// </summary>
/// <param name="firstRecordedTrackNumber">"First Recorded Track Number" value for TOC (usually 1)</param>
/// <param name="lastRecordedTrackNumber">"Last Recorded Track Number" value for TOC</param>
/// <param name="sessionFormat">The session format for this TOC</param>
/// <param name="leadoutTimestamp">The absolute timestamp of the lead-out track</param>
class Synthesize_A0A1A2(
   int firstRecordedTrackNumber,
   int lastRecordedTrackNumber,
   DiscSessionFormat sessionFormat,
   int leadoutTimestamp) {
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
