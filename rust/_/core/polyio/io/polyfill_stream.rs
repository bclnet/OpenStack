// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/Polyfill+Stream.cs
// PORT-SHA: 8ce91c2771e1bc25
// PORT-STATUS: done
//
// C# extension methods on `Stream` -> a blanket-implemented trait, same shape
// as `BinaryReaderExt`.

use std::io::{self, Read, Seek, SeekFrom, Write};

pub trait StreamExt: Read + Seek {
    /// C# `ReadAllBytes()` — reads the whole stream from position 0 and
    /// restores the original position.
    fn read_all_bytes(&mut self) -> io::Result<Vec<u8>> {
        let saved = self.stream_position()?;
        self.seek(SeekFrom::Start(0))?;
        let mut out = Vec::new();
        let r = self.read_to_end(&mut out);
        // Restore even on failure, so a failed read cannot strand the cursor.
        self.seek(SeekFrom::Start(saved))?;
        r?;
        Ok(out)
    }

    /// C# `ReadBytes(int count)`.
    ///
    /// The C# version calls `Stream.Read` once and ignores the return value, so
    /// a short read silently yields a zero-padded buffer. This uses
    /// `read_exact` and reports the truncation instead.
    fn read_bytes(&mut self, count: usize) -> io::Result<Vec<u8>> {
        let mut v = vec![0u8; count];
        self.read_exact(&mut v)?;
        Ok(v)
    }
}

impl<T: Read + Seek + ?Sized> StreamExt for T {}

pub trait StreamWriteExt: Write {
    /// C# `WriteBytes(byte[] data)`.
    #[inline]
    fn write_bytes(&mut self, data: &[u8]) -> io::Result<()> {
        self.write_all(data)
    }

    /// C# `WriteBytes(BinaryReader r, int count)` — copy `count` bytes across.
    fn write_bytes_from<R: Read>(&mut self, src: &mut R, count: u64) -> io::Result<u64> {
        io::copy(&mut src.take(count), self)
    }
}

impl<T: Write + ?Sized> StreamWriteExt for T {}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn read_all_bytes_restores_position() {
        let mut c = Cursor::new(b"abcdef".to_vec());
        c.seek(SeekFrom::Start(4)).unwrap();
        assert_eq!(c.read_all_bytes().unwrap(), b"abcdef");
        assert_eq!(c.stream_position().unwrap(), 4);
    }

    #[test]
    fn short_read_is_an_error_not_zero_padding() {
        let mut c = Cursor::new(b"ab".to_vec());
        assert!(c.read_bytes(8).is_err());
    }
}
