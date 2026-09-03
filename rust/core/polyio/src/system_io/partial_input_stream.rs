// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/PartialInputStream.cs
// PORT-SHA: 0686506f55227849
// PORT-STATUS: done
//
// A read-only window onto a subsection of a larger stream: positions are
// reported relative to the window, and reads are clamped to it.
//
// CONCURRENCY. The C# takes `lock (_baseStream)` on every read, because several
// PartialInputStreams over the same file are expected to interleave. A lock on
// the shared stream is the wrong tool for that — it serialises readers without
// making the seek-then-read pair atomic against anything that touches the
// stream *outside* a PartialInputStream.
//
// Rust makes the choice explicit instead of implicit:
//
//   * `PartialInputStream<S>` owns its `S` outright — no lock, no sharing. This
//     is what a single-threaded caller wants and costs nothing.
//   * For the shared case, wrap the source in `Arc<Mutex<S>>` at the call site;
//     `PartialInputStream<SharedSource<S>>` below does exactly the C# thing,
//     but the sharing is visible in the type rather than buried in a method.

use std::io::{self, Read, Seek, SeekFrom};
use std::sync::{Arc, Mutex};

/// C# `class PartialInputStream : Stream`.
#[derive(Debug)]
pub struct PartialInputStream<S> {
    base: S,
    start: u64,
    length: u64,
    /// Absolute position in the underlying stream.
    read_pos: u64,
    end: u64,
}

impl<S: Read + Seek> PartialInputStream<S> {
    /// C# `PartialInputStream(Stream source, long start, long length)`.
    pub fn new(base: S, start: u64, length: u64) -> Self {
        Self { base, start, length, read_pos: start, end: start + length }
    }

    /// C# `Length` — the window's length, not the underlying stream's.
    #[inline]
    pub fn len(&self) -> u64 {
        self.length
    }

    #[inline]
    pub fn is_empty(&self) -> bool {
        self.length == 0
    }

    /// Bytes left in the window.
    #[inline]
    pub fn remaining(&self) -> u64 {
        self.end.saturating_sub(self.read_pos)
    }

    pub fn into_inner(self) -> S {
        self.base
    }
}

impl<S: Read + Seek> Read for PartialInputStream<S> {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        let avail = self.remaining();
        if avail == 0 {
            return Ok(0);
        }
        let want = (buf.len() as u64).min(avail) as usize;
        // C# guards this seek with `if (Position != _readPos)` to spare stream
        // implementations that drop their buffer on every seek. Kept, since the
        // same is true of a BufReader here.
        if self.base.stream_position()? != self.read_pos {
            self.base.seek(SeekFrom::Start(self.read_pos))?;
        }
        let n = self.base.read(&mut buf[..want])?;
        self.read_pos += n as u64;
        Ok(n)
    }
}

impl<S: Read + Seek> Seek for PartialInputStream<S> {
    /// Window-relative, so position 0 is `start` in the underlying stream.
    fn seek(&mut self, pos: SeekFrom) -> io::Result<u64> {
        let target = match pos {
            SeekFrom::Start(o) => o as i64,
            SeekFrom::Current(o) => (self.read_pos - self.start) as i64 + o,
            SeekFrom::End(o) => self.length as i64 + o,
        };
        if target < 0 {
            return Err(io::Error::new(
                io::ErrorKind::InvalidInput,
                "seek before the start of the window",
            ));
        }
        // Seeking past the end is allowed (as on a real stream); reads there
        // simply return 0.
        self.read_pos = self.start + target as u64;
        Ok(target as u64)
    }
}

/// Shares one seekable source between several windows — the case the C# `lock`
/// was reaching for, made explicit.
#[derive(Debug, Clone)]
pub struct SharedSource<S>(pub Arc<Mutex<S>>);

impl<S> SharedSource<S> {
    pub fn new(inner: S) -> Self {
        Self(Arc::new(Mutex::new(inner)))
    }
}

impl<S: Read + Seek> Read for SharedSource<S> {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        let mut g = self
            .0
            .lock()
            .map_err(|_| io::Error::new(io::ErrorKind::Other, "source mutex poisoned"))?;
        g.read(buf)
    }
}

impl<S: Read + Seek> Seek for SharedSource<S> {
    fn seek(&mut self, pos: SeekFrom) -> io::Result<u64> {
        let mut g = self
            .0
            .lock()
            .map_err(|_| io::Error::new(io::ErrorKind::Other, "source mutex poisoned"))?;
        g.seek(pos)
    }
}

// NOT PORTED: C# `Write`, `SetLength`, and `Flush` all throw
// `NotSupportedException`. Rust expresses "not writable" by not implementing
// `Write`, so there is nothing to carry over.

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    fn window() -> PartialInputStream<Cursor<Vec<u8>>> {
        PartialInputStream::new(Cursor::new((0u8..20).collect::<Vec<_>>()), 5, 8)
    }

    #[test]
    fn reads_only_the_window() {
        let mut w = window();
        let mut out = Vec::new();
        w.read_to_end(&mut out).unwrap();
        assert_eq!(out, (5u8..13).collect::<Vec<_>>());
    }

    #[test]
    fn read_past_the_end_is_clamped_not_an_error() {
        let mut w = window();
        let mut buf = [0u8; 100];
        assert_eq!(w.read(&mut buf).unwrap(), 8);
        assert_eq!(w.read(&mut buf).unwrap(), 0);
    }

    #[test]
    fn seek_is_window_relative() {
        let mut w = window();
        w.seek(SeekFrom::Start(2)).unwrap();
        let mut b = [0u8; 1];
        w.read_exact(&mut b).unwrap();
        assert_eq!(b[0], 7, "window offset 2 is absolute offset 7");
    }

    #[test]
    fn seek_from_end_lands_inside_the_window() {
        let mut w = window();
        w.seek(SeekFrom::End(-1)).unwrap();
        let mut b = [0u8; 1];
        w.read_exact(&mut b).unwrap();
        assert_eq!(b[0], 12);
    }

    #[test]
    fn seek_before_start_is_rejected() {
        assert!(window().seek(SeekFrom::Start(0)).is_ok());
        assert!(window().seek(SeekFrom::End(-100)).is_err());
    }

    #[test]
    fn two_windows_can_share_one_source() {
        let src = SharedSource::new(Cursor::new((0u8..20).collect::<Vec<_>>()));
        let mut a = PartialInputStream::new(src.clone(), 0, 4);
        let mut b = PartialInputStream::new(src, 10, 4);
        let (mut x, mut y) = ([0u8; 4], [0u8; 4]);
        // Interleaved, to prove each window keeps its own cursor.
        a.read_exact(&mut x[..2]).unwrap();
        b.read_exact(&mut y[..2]).unwrap();
        a.read_exact(&mut x[2..]).unwrap();
        b.read_exact(&mut y[2..]).unwrap();
        assert_eq!(x, [0, 1, 2, 3]);
        assert_eq!(y, [10, 11, 12, 13]);
    }
}
