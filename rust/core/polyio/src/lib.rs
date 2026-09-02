//! `openstack-polyio` — 1:1 port of .NET project `OpenStack.PolyIO`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees can
//! be diffed and updated in parallel. Every file carries a `PORT-SOURCE` header
//! naming its C# original and a `PORT-SHA` of that file's contents at port
//! time; `./sync-check.sh` at the workspace root reports drift. See
//! `PORT_MAP.tsv` and `PORTING.md`.

pub mod i_source;
pub mod system;
pub mod system_drawing;
pub mod system_io;
pub mod system_numerics;
pub mod system_text;
pub mod type_x;
pub mod x;

/// The names worth importing together. C# got these via `global using` and
/// namespace-wide extension methods; Rust needs the traits in scope explicitly.
pub mod prelude {
    pub use crate::i_source::{AssetPath, HaveSource, Source, SourceError, SourceExt};
    pub use crate::system_io::bit_stream::BitStream;
    pub use crate::system_io::byte_xor_stream::ByteXorStream;
    pub use crate::system_io::circular_buffer::CircularBuffer;
    pub use crate::system_io::i_stream::{GetStream, ReadSeek, WriteToStream};
    pub use crate::system_io::polyfill::{XBoundBox, XLump2NO, XLumpNO, XLumpNO2, XLumpON};
    pub use crate::system_io::polyfill_binary_reader::{
        BinaryReaderExt, Origin, ReadError,
    };
    pub use crate::system_io::polyfill_stream::{StreamExt, StreamWriteExt};
}
