using System;
using System.Runtime.InteropServices;

namespace OpenStack.Gfx.OggVorbis
{
    #region Ogg

    public unsafe struct ogg_iovec_t
    {
        public void* iov_base;
        public nuint iov_len;
    }

    public unsafe struct oggpack_buffer
    {
        public nint endbyte;
        public int endbit;

        public byte* buffer;
        public byte* ptr;
        public nint storage;
    }

    // ogg_page is used to encapsulate the data in one Ogg bitstream page
    public unsafe struct ogg_page
    {
        public byte* header;
        public nint header_len;
        public byte* body;
        public nint body_len;
    }

    // ogg_stream_state contains the current encode/decode state of a logical Ogg bitstream
    public unsafe struct ogg_stream_state
    {
        public byte* body_data;         // bytes from packet bodies
        public nint body_storage;       // storage elements allocated
        public nint body_fill;          // elements stored; fill mark
        public nint body_returned;      // elements of fill returned

        public int* lacing_vals;        // The values that will go to the segment table
        public long* granule_vals;      // granulepos values for headers. Not compact this way, but it is simple coupled to the lacing fifo
        public nint lacing_storage;
        public nint lacing_fill;
        public nint lacing_packet;
        public nint lacing_returned;

        public fixed byte header[282];  // working space for header encode
        public int header_fill;

        public int e_o_s;               // set when we have buffered the last packet in the logical bitstream
        public int b_o_s;               // set after we've written the initial page of a logical bitstream
        public nint serialno;
        public nint pageno;
        public long packetno;           // sequence number for decode; the framing knows where there's a hole in the data, but we need coupling so that the codec (which is in a separate abstraction layer) also knows about the gap
        public long granulepos;
    }

    // ogg_packet is used to encapsulate the data and metadata belonging to a single raw Ogg/Vorbis packet
    public unsafe struct ogg_packet
    {
        public byte* packet;
        public nint bytes;
        public nint b_o_s;
        public nint e_o_s;

        public long granulepos;

        public long packetno;           // sequence number for decode; the framing knows where there's a hole in the data, but we need coupling so that the codec (which is in a separate abstraction layer) also knows about the gap
    }

    public unsafe struct ogg_sync_state
    {
        public byte* data;
        public int storage;
        public int fill;
        public int returned;
        public int unsynced;
        public int headerbytes;
        public int bodybytes;
    }

    public static unsafe class Ogg
    {
        const string LibraryName = "ogg";

        // Ogg BITSTREAM PRIMITIVES: bitstream
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_writeinit(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int oggpack_writecheck(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_writetrunc(oggpack_buffer* b, nint bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_writealign(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_writecopy(oggpack_buffer* b, void* source, nint bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_reset(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_writeclear(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_readinit(oggpack_buffer* b, byte* buf, int bytes);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_write(oggpack_buffer* b, nuint value, int bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpack_look(oggpack_buffer* b, int bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpack_look1(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_adv(oggpack_buffer* b, int bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpack_adv1(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpack_read(oggpack_buffer* b, int bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpack_read1(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpack_bytes(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpack_bits(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern byte* oggpack_get_buffer(oggpack_buffer* b);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_writeinit(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int oggpackB_writecheck(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_writetrunc(oggpack_buffer* b, nint bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_writealign(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_writecopy(oggpack_buffer* b, void* source, nint bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_reset(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_writeclear(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_readinit(oggpack_buffer* b, byte* buf, int bytes);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_write(oggpack_buffer* b, nuint value, int bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpackB_look(oggpack_buffer* b, int bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpackB_look1(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_adv(oggpack_buffer* b, int bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void oggpackB_adv1(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpackB_read(oggpack_buffer* b, int bits);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpackB_read1(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpackB_bytes(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint oggpackB_bits(oggpack_buffer* b);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern byte* oggpackB_get_buffer(oggpack_buffer* b);

        // Ogg BITSTREAM PRIMITIVES: encoding
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_packetin(ogg_stream_state* os, ogg_packet* op);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_iovecin(ogg_stream_state* os, ogg_iovec_t* iov, int count, nint e_o_s, long granulepos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_pageout(ogg_stream_state* os, ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_pageout_fill(ogg_stream_state* os, ogg_page* og, int nfill);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_flush(ogg_stream_state* os, ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_flush_fill(ogg_stream_state* os, ogg_page* og, int nfill);

        // Ogg BITSTREAM PRIMITIVES: decoding
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_sync_init(ogg_sync_state* oy);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_sync_clear(ogg_sync_state* oy);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_sync_reset(ogg_sync_state* oy);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_sync_destroy(ogg_sync_state* oy);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_sync_check(ogg_sync_state* oy);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern byte* ogg_sync_buffer(ogg_sync_state* oy, nint size);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_sync_wrote(ogg_sync_state* oy, nint bytes);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ogg_sync_pageseek(ogg_sync_state* oy, ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_sync_pageout(ogg_sync_state* oy, ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_pagein(ogg_stream_state* os, ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_packetout(ogg_stream_state* os, ogg_packet* op);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_packetpeek(ogg_stream_state* os, ogg_packet* op);

        // Ogg BITSTREAM PRIMITIVES: general
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_init(ogg_stream_state* os, int serialno);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_clear(ogg_stream_state* os);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_reset(ogg_stream_state* os);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_reset_serialno(ogg_stream_state* os, int serialno);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_destroy(ogg_stream_state* os);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_check(ogg_stream_state* os);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_stream_eos(ogg_stream_state* os);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern void ogg_page_checksum_set(ogg_page* og);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_page_version(ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_page_continued(ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_page_bos(ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_page_eos(ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern long ogg_page_granulepos(ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_page_serialno(ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ogg_page_pageno(ogg_page* og);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ogg_page_packets(ogg_page* og);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern void ogg_packet_clear(ogg_packet* op);
    }

    #endregion

    #region VorbisEnc

    public unsafe static partial class Vorbis
    {
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_encode_init(vorbis_info* vi, nint channels, nint rate, nint max_bitrate, nint nominal_bitrate, nint min_bitrate);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_encode_setup_managed(vorbis_info* vi, nint channels, nint rate, nint max_bitrate, nint nominal_bitrate, nint min_bitrate);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_encode_setup_vbr(vorbis_info* vi, nint channels, nint rate, float quality);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_encode_init_vbr(vorbis_info* vi, nint channels, nint rate, float base_quality);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_encode_setup_init(vorbis_info* vi);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_encode_ctl(vorbis_info* vi, int number, void* args);
    }

    [Obsolete("This is a deprecated interface. Please use vorbis_encode_ctl() with the \ref ovectl_ratemanage2_arg struct and OV_ECTL_RATEMANAGE2_GET and \ref OV_ECTL_RATEMANAGE2_SET calls in new code.")]
    public unsafe struct ovectl_ratemanage_arg
    {
        public int management_active;
        public nint bitrate_hard_min;
        public nint bitrate_hard_max;
        public double bitrate_hard_window;
        public nint bitrate_av_lo;
        public nint bitrate_av_hi;
        public nint bitrate_av_window;
        public double bitrate_av_window_center;
    }

    public unsafe struct ovectl_ratemanage2_arg
    {
        public int management_active;
        public nint bitrate_limit_min_kbps;
        public nint bitrate_limit_max_kbps;
        public nint bitrate_limit_reservoir_bits;
        public double bitrate_limit_reservoir_bias;
        public nint bitrate_average_kbps;
        public double bitrate_average_damping;
    }

    public unsafe static partial class Vorbis
    {
        public const int OV_ECTL_RATEMANAGE2_GET = 0x14;
        public const int OV_ECTL_RATEMANAGE2_SET = 0x15;
        public const int OV_ECTL_LOWPASS_GET = 0x20;
        public const int OV_ECTL_LOWPASS_SET = 0x21;
        public const int OV_ECTL_IBLOCK_GET = 0x30;
        public const int OV_ECTL_IBLOCK_SET = 0x31;
        public const int OV_ECTL_COUPLING_GET = 0x40;
        public const int OV_ECTL_COUPLING_SET = 0x41;
        [Obsolete("Please use OV_ECTL_RATEMANAGE2_SET instead.")] public const int OV_ECTL_RATEMANAGE_GET = 0x10;
        [Obsolete("Please use OV_ECTL_RATEMANAGE2_SET instead.")] public const int OV_ECTL_RATEMANAGE_SET = 0x11;
        [Obsolete("Please use OV_ECTL_RATEMANAGE2_SET instead.")] public const int OV_ECTL_RATEMANAGE_AVG = 0x12;
        [Obsolete("Please use OV_ECTL_RATEMANAGE2_SET instead.")] public const int OV_ECTL_RATEMANAGE_HARD = 0x13;
    }

    #endregion

    #region VorbisFile

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ov_callbacks
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public unsafe delegate nint ReadFuncDelegate(byte* ptr, nint size, nint nmemb, object datasource);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public unsafe delegate int SeekFuncDelegate(object datasource, long offset, int whence);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public unsafe delegate int CloseFuncDelegate(object datasource);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public unsafe delegate nint TellFuncDelegate(object datasource);

        public IntPtr read_func;
        public IntPtr seek_func;
        public IntPtr close_func;
        public IntPtr tell_func;
    }

    public unsafe static partial class Vorbis
    {
        public const int NOTOPEN = 0;
        public const int PARTOPEN = 1;
        public const int OPENED = 2;
        public const int STREAMSET = 3;
        public const int INITSET = 4;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe class OggVorbis_File
    {
        public void* datasource;         // Pointer to a FILE *, etc.
        public int seekable;
        public long offset;
        public long end;
        public ogg_sync_state oy;

        // If the FILE handle isn't seekable (eg, a pipe), only the current stream appears
        public int links;
        public long* offsets;
        public long* dataoffsets;
        public uint* serialnos;
        public long* pcmlengths;        // overloaded to maintain binary compatibility; x2 size, stores both beginning and end values
        public vorbis_info* vi;
        public vorbis_comment* vc;

        // Decoding working state local storage
        public long pcm_offset;
        public int ready_state;
        public uint current_serialno;
        public int current_link;

        public double bittrack;
        public double samptrack;

        public ogg_stream_state os;     // take physical pages, weld into a logical stream of packets
        public vorbis_dsp_state vd;     // central working state for the packet->PCM decoder
        public vorbis_block vb;         // local working space for packet->PCM decode

        public ov_callbacks callbacks;

        public void memset()
        {
            throw new NotImplementedException();
        }
    }

    public unsafe static partial class Vorbis
    {
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_clear(OggVorbis_File vf);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_fopen(string path, OggVorbis_File vf);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_open(IntPtr f, OggVorbis_File vf, byte* initial, nint ibytes);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_open_callbacks(object datasource, OggVorbis_File vf, byte* initial, nint ibytes, ov_callbacks callbacks);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_test(IntPtr f, OggVorbis_File vf, byte* initial, nint ibytes);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_test_callbacks(object datasource, OggVorbis_File vf, byte* initial, nint ibytes, ov_callbacks callbacks);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_test_open(OggVorbis_File vf);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ov_bitrate(OggVorbis_File vf, int i);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ov_bitrate_instant(OggVorbis_File vf);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ov_streams(OggVorbis_File vf);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ov_seekable(OggVorbis_File vf);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ov_serialnumber(OggVorbis_File vf, int i);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern long ov_raw_total(OggVorbis_File vf, int i);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern long ov_pcm_total(OggVorbis_File vf, int i);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern double ov_time_total(OggVorbis_File vf, int i);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_raw_seek(OggVorbis_File vf, long pos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_pcm_seek(OggVorbis_File vf, long pos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_pcm_seek_page(OggVorbis_File vf, long pos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_time_seek(OggVorbis_File vf, double pos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_time_seek_page(OggVorbis_File vf, double pos);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_raw_seek_lap(OggVorbis_File vf, long pos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_pcm_seek_lap(OggVorbis_File vf, long pos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_pcm_seek_page_lap(OggVorbis_File vf, long pos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_time_seek_lap(OggVorbis_File vf, double pos);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_time_seek_page_lap(OggVorbis_File vf, double pos);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern long ov_raw_tell(OggVorbis_File vf);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern long ov_pcm_tell(OggVorbis_File vf);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern double ov_time_tell(OggVorbis_File vf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public unsafe delegate void FilterProc(float** pcm, nint channels, nint samples, void* filter_param);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern vorbis_info* ov_info(OggVorbis_File vf, int link);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ov_read_float(OggVorbis_File vf, float*** pcm_channels, int samples, int* bitstream);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ov_read_filter(OggVorbis_File vf, byte* buffer, int length, int bigendianp, int word, int sgned, int* bitstream, FilterProc filter, void* filter_param);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern nint ov_read(OggVorbis_File vf, byte* buffer, int length, int bigendianp, int word, int sgned, int* bitstream);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_crosslap(OggVorbis_File vf1, OggVorbis_File vf2);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_halfrate(OggVorbis_File vf, int flag);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int ov_halfrate_p(OggVorbis_File vf);
    }

    #endregion

    #region Codec

    // vorbis_info contains all the setup information specific to the specific compression/decompression mode in progress(eg,
    // psychoacoustic settings, channel setup, options, codebook etc). vorbis_info and substructures are in backends.h.
    public unsafe struct vorbis_info
    {
        public int version;
        public int channels;
        public nint rate;

        // The below bitrate declarations are *hints*.
        // Combinations of the three values carry the following implications:
        //
        // all three set to the same value: implies a fixed rate bitstream
        // only nominal set: implies a VBR stream that averages the nominal bitrate.  No hard upper/lower limit
        // upper and or lower set: implies a VBR bitstream that obeys the bitrate limits. nominal may also be set to give a nominal rate.
        // none set: the coder does not care to speculate.

        public nint bitrate_upper;
        public nint bitrate_nominal;
        public nint bitrate_lower;
        public nint bitrate_window;

        public void* codec_setup;
    }

    // vorbis_dsp_state buffers the current vorbis audio analysis/synthesis state.The DSP state belongs to a specific logical bitstream
    public unsafe struct vorbis_dsp_state
    {
        public int analysisp;
        public vorbis_info* vi;

        public float** pcm;
        public float** pcmret;
        public int pcm_storage;
        public int pcm_current;
        public int pcm_returned;

        public int preextrapolate;
        public int eofflag;

        public nint lW;
        public nint W;
        public nint nW;
        public nint centerW;

        public long granulepos;
        public long sequence;

        public long glue_bits;
        public long time_bits;
        public long floor_bits;
        public long res_bits;

        public void* backend_state;
    }

    public unsafe struct vorbis_block
    {
        // necessary stream state for linking to the framing abstraction
        public float** pcm;             // this is a pointer into local storage
        public oggpack_buffer obp;

        public nint lW;
        public nint W;
        public nint nW;
        public int pcmend;
        public int mode;

        public int eofflag;
        public long granunlepos;
        public long sequence;
        public vorbis_dsp_state* vd;    // For read-only access of configuration

        // local storage to avoid remallocing; it's up to the mapping to structure it
        public void* localstore;
        public nint localtop;
        public nint localalloc;
        public nint totaluse;
        public alloc_chain* reap;

        // bitmetrics for the frame
        public nint glue_bits;
        public nint time_bits;
        public nint floor_bits;
        public nint res_bits;

        public void* @internal;
    }

    // vorbis_block is a single block of data to be processed as part of the analysis/synthesis stream; it belongs to a specific logical
    // bitstream, but is independent from other vorbis_blocks belonging to that logical bitstream.
    public unsafe struct alloc_chain
    {
        public void* ptr;
        public alloc_chain* next;
    }

    // the comments are not part of vorbis_info so that vorbis_info can be static storage
    public unsafe struct vorbis_comment
    {
        public byte** user_comments;
        public int* comment_lengths;
        public int comments;
        public byte* vendor;
    }

    // libvorbis encodes in two abstraction layers; first we perform DSP and produce a packet (see docs/analysis.txt).  The packet is then
    // coded into a framed OggSquish bitstream by the second layer (see docs/framing.txt).  Decode is the reverse process; we sync/frame
    // the bitstream and extract individual packets, then decode the packet back into PCM audio.
    // 
    // The extra framing/packetizing is used in streaming formats, such as files.  Over the net (such as with UDP), the framing and
    // packetization aren't necessary as they're provided by the transport and the streaming layer is not used

    public unsafe static partial class Vorbis
    {
        const string LibraryName = "vorbis";

        // Vorbis PRIMITIVES: general
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void vorbis_info_init(vorbis_info* vi);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void vorbis_info_clear(vorbis_info* vi);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_info_blocksize(vorbis_info* vi, int zo);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void vorbis_comment_init(vorbis_comment* vc);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void vorbis_comment_add(vorbis_comment* vc, byte* comment);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void vorbis_comment_add_tag(vorbis_comment* vc, byte* tag, byte* contents);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern byte* vorbis_comment_query(vorbis_comment* vc, byte* tag, int count);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_comment_query_count(vorbis_comment* vc, byte* tag);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void vorbis_comment_clear(vorbis_comment* vc);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_block_init(vorbis_dsp_state* v, vorbis_block* vb);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_block_clear(vorbis_block* vb);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern void vorbis_dsp_clear(vorbis_dsp_state* v);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern double vorbis_granule_time(vorbis_dsp_state* v, long granulepos);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern byte* vorbis_version_string();

        // Vorbis PRIMITIVES: analysis/DSP layer
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_analysis_init(vorbis_dsp_state* v, vorbis_info* vi);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_commentheader_out(vorbis_comment* vc, ogg_packet* op);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_analysis_headerout(vorbis_dsp_state* v, vorbis_comment* vc, ogg_packet* op, ogg_packet* op_comm, ogg_packet* op_code);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern float** vorbis_analysis_buffer(vorbis_dsp_state* v, int vals);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_analysis_wrote(vorbis_dsp_state* v, int vals);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_analysis_blockout(vorbis_dsp_state* v, vorbis_block* vb);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_analysis(vorbis_block* vb, ogg_packet* op);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_bitrate_addblock(vorbis_block* vb);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_bitrate_flushpacket(vorbis_dsp_state* vd, ogg_packet* op);

        // Vorbis PRIMITIVES: synthesis layer
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_idheader(ogg_packet* op);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_headerin(vorbis_info* vi, vorbis_comment* vc, ogg_packet* op);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_init(vorbis_dsp_state* v, vorbis_info* vi);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_restart(vorbis_dsp_state* v);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis(vorbis_block* vb, ogg_packet* op);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_trackonly(vorbis_block* vb, ogg_packet* op);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_blockin(vorbis_dsp_state* v, vorbis_block* vb);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_pcmout(vorbis_dsp_state* v, float*** pcm);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_lapout(vorbis_dsp_state* v, float*** pcm);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_read(vorbis_dsp_state* v, int samples);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern long vorbis_packet_blocksize(vorbis_info* vi, ogg_packet* op);

        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_halfrate(vorbis_info* v, int flag);
        [DllImport(LibraryName, ExactSpelling = true)] public static extern int vorbis_synthesis_halfrate_p(vorbis_info* v);

        // Vorbis ERRORS and return codes
        public const int OV_FALSE = -1;
        public const int OV_EOF = -2;
        public const int OV_HOLE = -3;

        public const int OV_EREAD = -128;
        public const int OV_EFAULT = -129;
        public const int OV_EIMPL = -130;
        public const int OV_EINVAL = -131;
        public const int OV_ENOTVORBIS = -132;
        public const int OV_EBADHEADER = -133;
        public const int OV_EVERSION = -134;
        public const int OV_ENOTAUDIO = -135;
        public const int OV_EBADPACKET = -136;
        public const int OV_EBADLINK = -137;
        public const int OV_ENOSEEK = -138;
    }

    #endregion
}