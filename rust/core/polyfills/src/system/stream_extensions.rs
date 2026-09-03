// PORT-SOURCE: Core/OpenStack.Polyfills/System/StreamExtensions.cs
// PORT-SHA: 2d1a34c699061236
// PORT-STATUS: done
//
// C#-SIDE BUG: `CopyTo(this Stream src, Stream dest, long len)` loops
// `while (len > 0)` and subtracts the read count — but never checks for a zero
// read. When `src` hits EOF before `len` bytes, `Read` returns 0 forever,
// `len` never decreases, and the method **spins forever writing nothing**. Any
// truncated or short source hangs the caller.
//
// The port stops at EOF and reports how many bytes actually moved, so a short
// source is visible instead of fatal.

use std::io::{self, Read, Write};

pub trait CopyToLen: Read {
    /// C# `CopyTo(Stream dest, long len)` — copy exactly `len` bytes.
    ///
    /// Returns the number of bytes copied, which is less than `len` only when
    /// the source ended early.
    fn copy_to_len<W: Write>(&mut self, dest: &mut W, len: u64) -> io::Result<u64>
    where
        Self: Sized,
    {
        io::copy(&mut self.take(len), dest)
    }

    /// As above, but a short source is an error — usually what a format parser
    /// wants, since a truncated stream means the file is malformed.
    fn copy_to_len_exact<W: Write>(&mut self, dest: &mut W, len: u64) -> io::Result<()>
    where
        Self: Sized,
    {
        let n = self.copy_to_len(dest, len)?;
        if n != len {
            return Err(io::Error::new(
                io::ErrorKind::UnexpectedEof,
                format!("expected {len} bytes, source ended after {n}"),
            ));
        }
        Ok(())
    }
}

impl<T: Read> CopyToLen for T {}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn copies_the_requested_length() {
        let mut src = Cursor::new(b"hello world".to_vec());
        let mut dst = Vec::new();
        assert_eq!(src.copy_to_len(&mut dst, 5).unwrap(), 5);
        assert_eq!(dst, b"hello");
    }

    #[test]
    fn short_source_terminates_instead_of_hanging() {
        // The C# version spins forever on exactly this input.
        let mut src = Cursor::new(b"abc".to_vec());
        let mut dst = Vec::new();
        assert_eq!(src.copy_to_len(&mut dst, 1000).unwrap(), 3);
        assert_eq!(dst, b"abc");
    }

    #[test]
    fn exact_variant_reports_truncation() {
        let mut src = Cursor::new(b"abc".to_vec());
        let mut dst = Vec::new();
        let e = src.copy_to_len_exact(&mut dst, 10).unwrap_err();
        assert_eq!(e.kind(), io::ErrorKind::UnexpectedEof);
    }
}
