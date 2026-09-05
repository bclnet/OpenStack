// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/IStream.cs
// PORT-SHA: d06f603e07936776
// PORT-STATUS: done

use std::io::{self, Read, Seek, Write};

/// C# `IStream { Stream GetStream(); }`.
///
/// C# returns a bare `Stream`; the Rust analogue is a boxed trait object over
/// the read+seek capabilities every current caller actually uses.
pub trait GetStream {
    fn get_stream(&self) -> io::Result<Box<dyn ReadSeek>>;
}

/// The capability set C#'s `Stream` provides to readers here.
pub trait ReadSeek: Read + Seek {}
impl<T: Read + Seek> ReadSeek for T {}

/// C# `IWriteToStream { void WriteToStream(Stream stream); }`.
pub trait WriteToStream {
    fn write_to_stream(&self, stream: &mut dyn Write) -> io::Result<()>;
}
