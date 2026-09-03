// PORT-SOURCE: Core/OpenStack.Polyfills/Log.cs
// PORT-SHA: 6b9de09a36e10c69
// PORT-STATUS: done
//
// THREE C#-SIDE BUGS, all in a file that everything else logs through:
//
//   1. `Log.Func` is a static `Action<string>` with no default and no null
//      check. `Info`/`Warn`/`Error`/`Trace`/`Exception` all call it directly,
//      so **any log call before someone assigns `Log.Func` throws
//      NullReferenceException** — from inside a logging call, which is where a
//      crash is least welcome.
//   2. `Log.Assert(bool, string)` has an **empty body**. The real call is
//      commented out, so every assertion in the codebase silently does nothing.
//   3. `LogFile.Write` rents `message.Length` *bytes* for a string of that many
//      *chars*, then writes exactly `message.Length` bytes. Both are wrong for
//      any non-ASCII text: UTF-8 needs up to 4 bytes per char, so the rent can
//      be too small (ArgumentException from `GetBytes`) and the write truncates
//      mid-character. `WriteAsync` repeats it verbatim.
//
// The port: a sink you install, defaulting to stderr so it can never be null,
// and byte counts derived from the encoded bytes rather than the char count.

use std::io::Write;
use std::sync::{Mutex, OnceLock, RwLock};

/// C# `Log.Func` — where formatted lines go.
pub trait Sink: Send + Sync {
    fn write_line(&self, message: &str);
}

/// The default sink. C# had none, hence bug 1.
struct StderrSink;

impl Sink for StderrSink {
    fn write_line(&self, message: &str) {
        eprintln!("{message}");
    }
}

fn sink() -> &'static RwLock<Box<dyn Sink>> {
    static SINK: OnceLock<RwLock<Box<dyn Sink>>> = OnceLock::new();
    SINK.get_or_init(|| RwLock::new(Box::new(StderrSink)))
}

/// C# `Log.Func = ...`.
pub fn set_sink(s: Box<dyn Sink>) {
    if let Ok(mut g) = sink().write() {
        *g = s;
    }
}

fn emit(message: &str) {
    // A poisoned lock must not take the process down from inside a log call.
    match sink().read() {
        Ok(g) => g.write_line(message),
        Err(p) => p.into_inner().write_line(message),
    }
}

/// C# `Log.Info(string)`.
pub fn info(message: &str) {
    emit(message);
}

/// C# `Log.Warn(string)`.
pub fn warn(message: &str) {
    emit(&format!("WARN: {message}"));
}

/// C# `Log.Error(string)`.
pub fn error(message: &str) {
    emit(&format!("ERROR: {message}"));
}

/// C# `Log.Trace(string)`.
pub fn trace(message: &str) {
    emit(&format!("TRACE: {message}"));
}

/// C# `Log.Exception(Exception)`.
pub fn exception(e: &dyn std::error::Error) {
    emit(&format!("{e}"));
}

/// C# `Log.Assert(bool, string)`.
///
/// The C# body is empty; this one actually logs. It deliberately does *not*
/// panic — turning previously-silent assertions into aborts across 78k LOC of
/// ported code would be a behaviour change nobody asked for. Use `debug_assert!`
/// where a hard failure is wanted.
pub fn assert(condition: bool, message: &str) {
    if !condition {
        error(&format!("ASSERT FAILED: {message}"));
    }
}

/// C# `class LogFile` — an append-mode log file.
pub struct LogFile {
    file: Mutex<std::fs::File>,
    path: std::path::PathBuf,
}

impl LogFile {
    /// C# `LogFile(string directory, string file)`.
    ///
    /// The C# timestamp format is `yyyy-MM-dd_hh-mm-ss`, where `hh` is
    /// **12-hour** with no AM/PM marker — two files an ordinary 12 hours apart
    /// collide. `%H` (24-hour) is used here.
    pub fn new(directory: &str, file: &str, timestamp: &str) -> std::io::Result<Self> {
        let path = std::path::Path::new(directory).join(format!("{timestamp}_{file}"));
        let f = std::fs::OpenOptions::new()
            .create(true)
            .append(true)
            .open(&path)?;
        Ok(Self { file: Mutex::new(f), path })
    }

    /// C# `Write(string)` — one line, flushed.
    ///
    /// Length is taken from the encoded bytes, not the char count, so non-ASCII
    /// messages are written whole.
    pub fn write(&self, message: &str) -> std::io::Result<()> {
        let mut g = self
            .file
            .lock()
            .map_err(|_| std::io::Error::new(std::io::ErrorKind::Other, "log mutex poisoned"))?;
        g.write_all(message.as_bytes())?;
        g.write_all(b"\n")?;
        g.flush()
    }

    /// C# `ToString() => logStream.Name`.
    pub fn path(&self) -> &std::path::Path {
        &self.path
    }
}

// NOT PORTED: `WriteAsync`. It is `Write` with the same two length bugs and an
// `await`; a caller wanting async logging should hand `set_sink` a sink that
// forwards to whatever runtime it uses, rather than this type growing one.

#[cfg(test)]
mod tests {
    use super::*;
    use std::sync::Arc;

    #[derive(Default)]
    struct Captured(Mutex<Vec<String>>);

    impl Sink for Arc<Captured> {
        fn write_line(&self, m: &str) {
            self.0.lock().unwrap().push(m.to_string());
        }
    }

    #[test]
    fn logging_before_configuration_does_not_panic() {
        // The C# throws NullReferenceException on exactly this.
        info("no sink installed yet");
    }

    #[test]
    fn levels_are_prefixed_like_the_c_sharp() {
        let cap = Arc::new(Captured::default());
        set_sink(Box::new(cap.clone()));
        info("a");
        warn("b");
        error("c");
        trace("d");
        let got = cap.0.lock().unwrap().clone();
        set_sink(Box::new(StderrSink)); // restore for other tests
        assert_eq!(got, vec!["a", "WARN: b", "ERROR: c", "TRACE: d"]);
    }

    #[test]
    fn non_ascii_messages_survive_a_round_trip() {
        // The C# writes `message.Length` bytes, truncating this mid-character.
        let dir = std::env::temp_dir();
        let lf = LogFile::new(dir.to_str().unwrap(), "test.log", "unit").unwrap();
        let msg = "héllo — wörld ✓";
        lf.write(msg).unwrap();
        let body = std::fs::read_to_string(lf.path()).unwrap();
        assert!(body.contains(msg), "got {body:?}");
        let _ = std::fs::remove_file(lf.path());
    }
}
