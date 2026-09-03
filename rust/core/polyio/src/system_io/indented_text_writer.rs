// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/IndentedTextWriter.cs
// PORT-SHA: 91685f1f5cd73ecd
// PORT-STATUS: done
//
// A `TextWriter` that prefixes each line with N tabs — used to emit readable
// nested dumps of parsed formats.
//
// C# inherits from `System.TextWriter` and overrides ~30 `Write` overloads to
// route through one indent-aware path. Rust's `std::fmt::Write` has a single
// method, `write_str`, and everything else (`write!`, `Display`) funnels
// through it — so the whole overload set collapses into one implementation and
// `write!(w, "{x:?}")` works for any type.

use std::fmt::Write as FmtWrite;
use std::io::Write as IoWrite;

/// C# `class IndentedTextWriter`.
pub struct IndentedTextWriter<W> {
    inner: W,
    /// C# `Indent` — the current depth.
    indent: usize,
    tab: String,
    /// True at the start of a line, so the prefix is written lazily. The C#
    /// tracks the same thing with `tabsPending`.
    pending: bool,
    newline: String,
}

impl<W> IndentedTextWriter<W> {
    /// C# `IndentedTextWriter(TextWriter writer, string tabString = "    ")`.
    pub fn new(inner: W) -> Self {
        Self {
            inner,
            indent: 0,
            tab: "    ".to_string(),
            pending: true,
            newline: "\n".to_string(),
        }
    }

    pub fn with_tab(mut self, tab: impl Into<String>) -> Self {
        self.tab = tab.into();
        self
    }

    /// C# `NewLine`.
    pub fn with_newline(mut self, newline: impl Into<String>) -> Self {
        self.newline = newline.into();
        self
    }

    /// C# `Indent` getter.
    #[inline]
    pub fn indent(&self) -> usize {
        self.indent
    }

    /// C# `Indent` setter. The C# clamps a negative assignment to 0; `usize`
    /// makes that unrepresentable.
    #[inline]
    pub fn set_indent(&mut self, v: usize) {
        self.indent = v;
    }

    /// Convenience the C# lacked — callers there wrote `w.Indent++`.
    #[inline]
    pub fn push(&mut self) {
        self.indent += 1;
    }

    /// Saturates at zero rather than going negative.
    #[inline]
    pub fn pop(&mut self) {
        self.indent = self.indent.saturating_sub(1);
    }

    pub fn into_inner(self) -> W {
        self.inner
    }
}

impl<W: FmtWrite> IndentedTextWriter<W> {
    fn write_tabs(&mut self) -> std::fmt::Result {
        if self.pending {
            self.pending = false;
            for _ in 0..self.indent {
                self.inner.write_str(&self.tab)?;
            }
        }
        Ok(())
    }

    /// C# `WriteLineNoTabs(string)` — bypasses the indent for this line.
    pub fn write_line_no_tabs(&mut self, s: &str) -> std::fmt::Result {
        self.inner.write_str(s)?;
        self.inner.write_str(&self.newline)?;
        self.pending = true;
        Ok(())
    }
}

/// The single funnel every `write!` goes through.
///
/// Handles embedded newlines correctly: a multi-line string written in one call
/// gets each of its lines indented, which the C#'s per-overload routing only
/// managed for calls that happened to end at a line boundary.
impl<W: FmtWrite> FmtWrite for IndentedTextWriter<W> {
    fn write_str(&mut self, s: &str) -> std::fmt::Result {
        for (i, part) in s.split('\n').enumerate() {
            if i > 0 {
                self.inner.write_str(&self.newline)?;
                self.pending = true;
            }
            if part.is_empty() {
                continue;
            }
            self.write_tabs()?;
            self.inner.write_str(part)?;
        }
        Ok(())
    }
}

/// Same behaviour over an `io::Write` sink, for writing to a file or stdout.
pub struct IndentedIoWriter<W: IoWrite> {
    inner: W,
    indent: usize,
    tab: Vec<u8>,
    pending: bool,
    newline: Vec<u8>,
}

impl<W: IoWrite> IndentedIoWriter<W> {
    pub fn new(inner: W) -> Self {
        Self {
            inner,
            indent: 0,
            tab: b"    ".to_vec(),
            pending: true,
            newline: b"\n".to_vec(),
        }
    }

    #[inline]
    pub fn push(&mut self) {
        self.indent += 1;
    }

    #[inline]
    pub fn pop(&mut self) {
        self.indent = self.indent.saturating_sub(1);
    }

    pub fn into_inner(self) -> W {
        self.inner
    }
}

impl<W: IoWrite> IoWrite for IndentedIoWriter<W> {
    fn write(&mut self, buf: &[u8]) -> std::io::Result<usize> {
        for (i, part) in buf.split(|&b| b == b'\n').enumerate() {
            if i > 0 {
                self.inner.write_all(&self.newline)?;
                self.pending = true;
            }
            if part.is_empty() {
                continue;
            }
            if self.pending {
                self.pending = false;
                for _ in 0..self.indent {
                    self.inner.write_all(&self.tab)?;
                }
            }
            self.inner.write_all(part)?;
        }
        Ok(buf.len())
    }

    fn flush(&mut self) -> std::io::Result<()> {
        self.inner.flush()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn render(f: impl FnOnce(&mut IndentedTextWriter<String>)) -> String {
        let mut w = IndentedTextWriter::new(String::new());
        f(&mut w);
        w.into_inner()
    }

    #[test]
    fn indents_each_line() {
        let out = render(|w| {
            writeln!(w, "root").unwrap();
            w.push();
            writeln!(w, "child").unwrap();
            w.pop();
            writeln!(w, "sibling").unwrap();
        });
        assert_eq!(out, "root\n    child\nsibling\n");
    }

    #[test]
    fn embedded_newlines_are_indented_too() {
        // The C# only indents at call boundaries, so a multi-line string
        // written in one call leaves later lines flush-left.
        let out = render(|w| {
            w.push();
            write!(w, "a\nb\n").unwrap();
        });
        assert_eq!(out, "    a\n    b\n");
    }

    #[test]
    fn blank_lines_get_no_trailing_indent() {
        let out = render(|w| {
            w.push();
            writeln!(w).unwrap();
            writeln!(w, "x").unwrap();
        });
        assert_eq!(out, "\n    x\n", "no stray tabs on the empty line");
    }

    #[test]
    fn pop_saturates_at_zero() {
        let out = render(|w| {
            w.pop();
            w.pop();
            writeln!(w, "flush").unwrap();
        });
        assert_eq!(out, "flush\n");
    }

    #[test]
    fn no_tabs_variant_bypasses_indent() {
        let out = render(|w| {
            w.push();
            w.write_line_no_tabs("raw").unwrap();
            writeln!(w, "indented").unwrap();
        });
        assert_eq!(out, "raw\n    indented\n");
    }

    #[test]
    fn custom_tab_string_is_used() {
        let mut w = IndentedTextWriter::new(String::new()).with_tab("\t");
        w.push();
        writeln!(w, "x").unwrap();
        assert_eq!(w.into_inner(), "\tx\n");
    }

    #[test]
    fn io_variant_matches_the_fmt_variant() {
        let mut w = IndentedIoWriter::new(Vec::new());
        w.push();
        w.write_all(b"a\nb\n").unwrap();
        assert_eq!(w.into_inner(), b"    a\n    b\n");
    }
}
