// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/ByteXorStream.cs
// PORT-SHA: a89b134615be4c19
// PORT-STATUS: done
//
// C# `class ByteXorStream : Stream` overrides Read/Write/Seek to XOR every byte
// with a constant. Rust has no Stream base class, so the port implements the
// three std traits that make up the same contract: Read, Write, Seek.
//
// TWO C#-SIDE BUGS SURFACED BY THIS PORT — both worth fixing upstream:
//
//   1. `Read` XORs `buffer[i]` instead of `buffer[offset + i]`. With a non-zero
//      offset it corrupts the head of the caller's buffer and leaves the bytes
//      it actually read un-decoded. `Write` gets the same expression right.
//      Rust's `Read::read(&mut [u8])` has no offset parameter at all, so the
//      bug is not expressible here.
//
//   2. `Write` XORs the caller's buffer *in place*, mutating an argument the
//      caller still owns. Rust's `&[u8]` forbids that, so this port copies into
//      a scratch buffer. Behaviour differs from C# only for callers that were
//      (accidentally) relying on the mutation.

use std::io::{self, Read, Seek, SeekFrom, Write};

/// Wraps a stream, XOR-ing every byte that passes through with [`byte`](Self::byte).
#[derive(Debug)]
pub struct ByteXorStream<S> {
    /// C# `public Stream Stream`.
    pub stream: S,
    /// C# `public byte Byte`.
    pub byte: u8,
    /// Scratch space for `write`, so the caller's slice stays untouched.
    scratch: Vec<u8>,
}

impl<S> ByteXorStream<S> {
    /// C# `ByteXorStream(Stream stream, byte @byte)`.
    pub fn new(stream: S, byte: u8) -> Self {
        Self { stream, byte, scratch: Vec::new() }
    }

    /// Unwraps and returns the inner stream.
    pub fn into_inner(self) -> S {
        self.stream
    }
}

impl<S: Read> Read for ByteXorStream<S> {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        let n = self.stream.read(buf)?;
        for b in buf[..n].iter_mut() {
            *b ^= self.byte;
        }
        Ok(n)
    }
}

impl<S: Write> Write for ByteXorStream<S> {
    fn write(&mut self, buf: &[u8]) -> io::Result<usize> {
        self.scratch.clear();
        self.scratch.extend(buf.iter().map(|b| b ^ self.byte));
        self.stream.write(&self.scratch)
    }

    fn flush(&mut self) -> io::Result<()> {
        self.stream.flush()
    }
}

impl<S: Seek> Seek for ByteXorStream<S> {
    fn seek(&mut self, pos: SeekFrom) -> io::Result<u64> {
        self.stream.seek(pos)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn xor_roundtrips() {
        let plain = b"hello world";
        let mut out = Vec::new();
        ByteXorStream::new(&mut out, 0x5A).write_all(plain).unwrap();
        assert_ne!(out, plain);

        let mut back = Vec::new();
        ByteXorStream::new(Cursor::new(out), 0x5A)
            .read_to_end(&mut back)
            .unwrap();
        assert_eq!(back, plain);
    }

    #[test]
    fn write_does_not_mutate_caller_buffer() {
        // The C# version XORs the caller's array in place; this must not.
        let src = b"abcd";
        let mut sink = Vec::new();
        ByteXorStream::new(&mut sink, 0xFF).write_all(src).unwrap();
        assert_eq!(src, b"abcd");
    }

    #[test]
    fn partial_read_only_decodes_bytes_actually_read() {
        let mut s = ByteXorStream::new(Cursor::new(vec![0x00, 0x00]), 0xFF);
        let mut buf = [0xAAu8; 4];
        let n = s.read(&mut buf).unwrap();
        assert_eq!(n, 2);
        assert_eq!(&buf[..2], &[0xFF, 0xFF]);
        assert_eq!(&buf[2..], &[0xAA, 0xAA], "tail must be untouched");
    }
}
