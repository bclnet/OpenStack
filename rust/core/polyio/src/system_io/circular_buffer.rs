// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/CircularBuffer.cs
// PORT-SHA: af0a78b40bbe84f8
// PORT-STATUS: done
//
// Growable ring buffer of bytes. C# `class CircularBuffer(int size = 4096)`
// (primary constructor) -> `CircularBuffer::with_capacity` + `Default`.
//
// The C# `DequeSegment(int, out ArraySegment<byte>)` returns a window into the
// private backing array, which lets callers read (and write) buffer internals
// after the fact. The Rust equivalent is `dequeue_slice`, which borrows `&self`
// for the lifetime of the slice so the buffer cannot be mutated while a segment
// is outstanding.

/// A byte queue backed by a growable ring buffer.
#[derive(Debug, Clone)]
pub struct CircularBuffer {
    buffer: Vec<u8>,
    head: usize,
    tail: usize,
    len: usize,
}

impl Default for CircularBuffer {
    /// C# default parameter `size = 4096`.
    fn default() -> Self {
        Self::with_capacity(4096)
    }
}

impl CircularBuffer {
    /// C# `CircularBuffer(int size)`.
    pub fn with_capacity(size: usize) -> Self {
        let size = size.max(1);
        Self { buffer: vec![0; size], head: 0, tail: 0, len: 0 }
    }

    /// C# `Length` — number of queued bytes.
    #[inline]
    pub fn len(&self) -> usize {
        self.len
    }

    #[inline]
    pub fn is_empty(&self) -> bool {
        self.len == 0
    }

    /// Allocated capacity, not the queued length.
    #[inline]
    pub fn capacity(&self) -> usize {
        self.buffer.len()
    }

    /// C# `this[int index]` — indexed relative to the head.
    #[inline]
    pub fn get(&self, index: usize) -> Option<u8> {
        if index >= self.len {
            return None;
        }
        Some(self.buffer[(self.head + index) % self.buffer.len()])
    }

    /// C# `Clear()`.
    pub fn clear(&mut self) {
        self.head = 0;
        self.tail = 0;
        self.len = 0;
    }

    /// C# `SetCapacity(int capacity)` — grow and re-linearise.
    fn set_capacity(&mut self, capacity: usize) {
        let mut new_buffer = vec![0u8; capacity];
        if self.len > 0 {
            if self.head < self.tail {
                new_buffer[..self.len].copy_from_slice(&self.buffer[self.head..self.tail]);
            } else {
                let right = self.buffer.len() - self.head;
                new_buffer[..right].copy_from_slice(&self.buffer[self.head..]);
                new_buffer[right..right + self.tail].copy_from_slice(&self.buffer[..self.tail]);
            }
        }
        self.head = 0;
        self.tail = self.len;
        self.buffer = new_buffer;
    }

    /// C# `Enqueue(Span<byte> buffer, int offset, int size)`.
    ///
    /// Rust callers slice at the call site (`buf[offset..offset + size]`), so
    /// the offset/size pair collapses into one argument.
    pub fn enqueue(&mut self, data: &[u8]) {
        let size = data.len();
        if size == 0 {
            return;
        }
        // C#: grow to the next 2048 boundary once the queue would fill.
        if self.len + size >= self.buffer.len() {
            self.set_capacity((self.len + size + 2047) & !2047);
        }
        let cap = self.buffer.len();
        let right = cap - self.tail;
        if right >= size {
            self.buffer[self.tail..self.tail + size].copy_from_slice(data);
        } else {
            self.buffer[self.tail..].copy_from_slice(&data[..right]);
            self.buffer[..size - right].copy_from_slice(&data[right..]);
        }
        self.tail = (self.tail + size) % cap;
        self.len += size;
    }

    /// C# `Dequeue(Span<byte> buffer, int offset, int size)` — copies out at
    /// most `out_buf.len()` bytes and returns how many were written.
    pub fn dequeue(&mut self, out_buf: &mut [u8]) -> usize {
        let size = out_buf.len().min(self.len);
        if size == 0 {
            return 0;
        }
        let cap = self.buffer.len();
        let right = cap - self.head;
        if right >= size {
            out_buf[..size].copy_from_slice(&self.buffer[self.head..self.head + size]);
        } else {
            out_buf[..right].copy_from_slice(&self.buffer[self.head..]);
            out_buf[right..size].copy_from_slice(&self.buffer[..size - right]);
        }
        self.head = (self.head + size) % cap;
        self.len -= size;
        if self.len == 0 {
            self.head = 0;
            self.tail = 0;
        }
        size
    }

    /// C# `DequeSegment(int size, out ArraySegment<byte>)` — hands back a
    /// contiguous view without copying, truncated at the wrap point.
    ///
    /// Borrows `&self` for the slice's lifetime, so unlike the C# version the
    /// buffer cannot be mutated while the segment is alive.
    pub fn dequeue_slice(&mut self, size: usize) -> &[u8] {
        let mut size = size.min(self.len);
        if size == 0 {
            return &[];
        }
        if self.head >= self.tail {
            size = size.min(self.buffer.len() - self.head);
        }
        let start = self.head;
        self.head = (self.head + size) % self.buffer.len();
        self.len -= size;
        if self.len == 0 {
            self.head = 0;
            self.tail = 0;
        }
        &self.buffer[start..start + size]
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn fifo_order_preserved() {
        let mut b = CircularBuffer::with_capacity(8);
        b.enqueue(b"abc");
        b.enqueue(b"de");
        let mut out = [0u8; 5];
        assert_eq!(b.dequeue(&mut out), 5);
        assert_eq!(&out, b"abcde");
        assert!(b.is_empty());
    }

    #[test]
    fn wraps_around_the_ring() {
        let mut b = CircularBuffer::with_capacity(8);
        b.enqueue(b"1234");
        let mut sink = [0u8; 3];
        b.dequeue(&mut sink); // head now at 3
        b.enqueue(b"5678"); // must wrap past the end
        let mut out = [0u8; 5];
        assert_eq!(b.dequeue(&mut out), 5);
        assert_eq!(&out, b"45678");
    }

    #[test]
    fn grows_past_initial_capacity() {
        let mut b = CircularBuffer::with_capacity(4);
        let big = vec![7u8; 5000];
        b.enqueue(&big);
        assert_eq!(b.len(), 5000);
        let mut out = vec![0u8; 5000];
        assert_eq!(b.dequeue(&mut out), 5000);
        assert_eq!(out, big);
    }

    #[test]
    fn dequeue_of_more_than_queued_returns_what_exists() {
        let mut b = CircularBuffer::with_capacity(8);
        b.enqueue(b"ab");
        let mut out = [0u8; 10];
        assert_eq!(b.dequeue(&mut out), 2);
    }

    #[test]
    fn indexing_is_relative_to_head() {
        let mut b = CircularBuffer::with_capacity(8);
        b.enqueue(b"abcd");
        let mut sink = [0u8; 2];
        b.dequeue(&mut sink);
        assert_eq!(b.get(0), Some(b'c'));
        assert_eq!(b.get(2), None);
    }
}
