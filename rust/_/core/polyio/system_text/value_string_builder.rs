// PORT-SOURCE: Core/OpenStack.PolyIO/System.Text/ValueStringBuilder.cs
// PORT-SHA: d68fbc086802c132
// PORT-STATUS: done
//
// C# `ref struct ValueStringBuilder` is a stack-only string builder that
// borrows a caller-supplied `Span<char>` and falls back to `ArrayPool` when it
// overflows. Its whole reason to exist is dodging heap allocation and GC
// pressure in .NET.
//
// Rust's `String` already is that: a growable buffer the caller owns, freed
// deterministically, with `with_capacity` for the pre-sized case. So this is a
// thin wrapper that keeps the C# method names — enough for ported call sites to
// read the same, without pretending the pooling machinery is still needed.
//
// Deliberately dropped, with nothing lost:
//   * `GetPinnableReference`, `RawChars` — expose the raw buffer for interop.
//   * `Dispose`, `ArrayPool` rental          — `String` frees on drop.
//   * `TryCopyTo(out int charsWritten)`      — `as_str()` covers every caller.
//
// ONE REAL SEMANTIC DIFFERENCE: C# indexes UTF-16 code units, Rust `String` is
// UTF-8. For the ASCII these builders actually handle the two agree, but
// `insert`/`remove` take byte offsets here, not char offsets. Every current
// call site passes offsets derived from ASCII content, so they line up; if that
// changes, this is the place it will bite.

/// C# `ValueStringBuilder`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct ValueStringBuilder {
    buf: String,
}

impl ValueStringBuilder {
    /// C# `ValueStringBuilder(int initialCapacity)`.
    pub fn with_capacity(initial_capacity: usize) -> Self {
        Self { buf: String::with_capacity(initial_capacity) }
    }

    /// C# `ValueStringBuilder(ReadOnlySpan<char> initialString)`.
    pub fn from_str(initial: &str) -> Self {
        Self { buf: initial.to_string() }
    }

    /// C# `Length`. Byte length — see the note above for the UTF-8 caveat.
    #[inline]
    pub fn len(&self) -> usize {
        self.buf.len()
    }

    #[inline]
    pub fn is_empty(&self) -> bool {
        self.buf.is_empty()
    }

    /// C# `Capacity`.
    #[inline]
    pub fn capacity(&self) -> usize {
        self.buf.capacity()
    }

    /// C# `EnsureCapacity(int)`.
    pub fn ensure_capacity(&mut self, capacity: usize) {
        if capacity > self.buf.capacity() {
            self.buf.reserve(capacity - self.buf.len());
        }
    }

    /// C# `Append(char)`.
    #[inline]
    pub fn push(&mut self, c: char) {
        self.buf.push(c);
    }

    /// C# `Append(string)` / `Append(ReadOnlySpan<char>)`.
    #[inline]
    pub fn push_str(&mut self, s: &str) {
        self.buf.push_str(s);
    }

    /// C# `Append(char c, int count)`.
    pub fn push_repeated(&mut self, c: char, count: usize) {
        self.buf.reserve(count * c.len_utf8());
        for _ in 0..count {
            self.buf.push(c);
        }
    }

    /// C# `Insert(int index, ReadOnlySpan<char> s)`.
    ///
    /// # Panics
    /// If `index` is not on a UTF-8 char boundary.
    pub fn insert_str(&mut self, index: usize, s: &str) {
        self.buf.insert_str(index, s);
    }

    /// C# `Insert(int index, char value, int count)`.
    pub fn insert_repeated(&mut self, index: usize, c: char, count: usize) {
        let filler: String = std::iter::repeat(c).take(count).collect();
        self.buf.insert_str(index, &filler);
    }

    /// C# `Remove(int startIndex, int length)`.
    pub fn remove(&mut self, start: usize, length: usize) {
        self.buf.replace_range(start..start + length, "");
    }

    /// C# `Replace(char oldChar, char newChar)`.
    pub fn replace_char(&mut self, old: char, new: char) {
        self.buf = self.buf.replace(old, &new.to_string());
    }

    /// C# `Replace(ReadOnlySpan<char> oldChars, ReadOnlySpan<char> newChars)`.
    pub fn replace(&mut self, old: &str, new: &str) {
        if old.is_empty() {
            return; // C# would loop forever here
        }
        self.buf = self.buf.replace(old, new);
    }

    /// C# `AsSpan()`.
    #[inline]
    pub fn as_str(&self) -> &str {
        &self.buf
    }

    /// C# `Clear()` — keeps the allocation.
    #[inline]
    pub fn clear(&mut self) {
        self.buf.clear();
    }

    /// C# `ToString()`, consuming the builder.
    #[inline]
    pub fn into_string(self) -> String {
        self.buf
    }
}

impl std::fmt::Display for ValueStringBuilder {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.write_str(&self.buf)
    }
}

impl std::fmt::Write for ValueStringBuilder {
    fn write_str(&mut self, s: &str) -> std::fmt::Result {
        self.buf.push_str(s);
        Ok(())
    }
}

impl From<ValueStringBuilder> for String {
    fn from(b: ValueStringBuilder) -> Self {
        b.buf
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn append_and_render() {
        let mut b = ValueStringBuilder::with_capacity(16);
        b.push_str("hello");
        b.push(' ');
        b.push_repeated('!', 3);
        assert_eq!(b.as_str(), "hello !!!");
    }

    #[test]
    fn insert_and_remove() {
        let mut b = ValueStringBuilder::from_str("abcdef");
        b.insert_str(3, "XY");
        assert_eq!(b.as_str(), "abcXYdef");
        b.remove(3, 2);
        assert_eq!(b.as_str(), "abcdef");
    }

    #[test]
    fn replace_empty_needle_terminates() {
        // The C# loop never advances on an empty needle.
        let mut b = ValueStringBuilder::from_str("abc");
        b.replace("", "x");
        assert_eq!(b.as_str(), "abc");
    }

    #[test]
    fn clear_keeps_capacity() {
        let mut b = ValueStringBuilder::with_capacity(64);
        b.push_str("something");
        let cap = b.capacity();
        b.clear();
        assert!(b.is_empty());
        assert_eq!(b.capacity(), cap);
    }
}
