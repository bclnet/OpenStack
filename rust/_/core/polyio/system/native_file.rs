// PORT-SOURCE: Core/OpenStack.PolyIO/System/NativeFile.cs
// PORT-SHA: d335b20802b0803a
// PORT-STATUS: done
//
// C# P/Invokes `kernel32!ReadFile` / `libc!read` to move bytes between a raw OS
// handle and a raw pointer, picking the backend at static-init time through an
// `INativeFile` interface.
//
// Rust needs none of that machinery. `std::fs::File` can be built from a raw fd
// or handle and already dispatches per-platform at compile time, so the
// interface, the two implementations, and the runtime branch all collapse into
// two functions over `&[u8]` / `&mut [u8]`.
//
// SAFETY: the caller owns the descriptor. These borrow it (`ManuallyDrop`), so
// the `File` wrapper does not close it on drop — matching the C#, which never
// owned the handle either.
//
// Two C#-side bugs, both fixed here because Rust's `io::Result` makes ignoring
// them awkward rather than easy:
//
//   1. `NativeFileWin32` discards `lpNumberOfBytesRead` and the `bool` return,
//      so a short or failed read is indistinguishable from a complete one.
//   2. `NativeFileUnix` discards `read`'s return for the same reason. Both are
//      reported here.
//
// Also note `IsUnix => () => false` is hard-coded, so the C# always selects the
// Win32 backend even on Linux — the `NativeFileUnix` branch is dead code.

use std::io::{self, Read, Write};
use std::mem::ManuallyDrop;

#[cfg(unix)]
use std::os::fd::{FromRawFd, RawFd};
#[cfg(windows)]
use std::os::windows::io::{FromRawHandle, RawHandle};

/// Platform-native file handle. C# passed these as `IntPtr`.
#[cfg(unix)]
pub type NativeHandle = RawFd;
#[cfg(windows)]
pub type NativeHandle = RawHandle;

#[cfg(unix)]
unsafe fn borrow(handle: NativeHandle) -> ManuallyDrop<std::fs::File> {
    ManuallyDrop::new(unsafe { std::fs::File::from_raw_fd(handle) })
}

#[cfg(windows)]
unsafe fn borrow(handle: NativeHandle) -> ManuallyDrop<std::fs::File> {
    ManuallyDrop::new(unsafe { std::fs::File::from_raw_handle(handle) })
}

/// C# `NativeFile.Read(IntPtr ptr, IntPtr buffer, int length)`.
///
/// Returns the number of bytes actually read, which the C# threw away.
///
/// # Safety
/// `handle` must be a valid, open, readable descriptor that outlives this call.
pub unsafe fn read(handle: NativeHandle, buffer: &mut [u8]) -> io::Result<usize> {
    let mut f = unsafe { borrow(handle) };
    f.read(buffer)
}

/// C# `NativeFile.Write(IntPtr ptr, IntPtr buffer, int length)`.
///
/// # Safety
/// `handle` must be a valid, open, writable descriptor that outlives this call.
pub unsafe fn write(handle: NativeHandle, buffer: &[u8]) -> io::Result<usize> {
    let mut f = unsafe { borrow(handle) };
    f.write(buffer)
}

// NOT PORTED: `INativeFile`, `NativeFileWin32`, `NativeFileUnix`, and
// `NativeFile.IsUnix`. The trait existed only to switch backends at runtime;
// `#[cfg]` does that at compile time with no indirection and no dead branch.
