# Porting OpenStack (.NET) to Rust

This workspace is a **file-for-file** port. Every `.rs` maps to exactly one
`.cs`, so a change on either side has one obvious counterpart on the other.

| | |
|---|---|
| C# projects | 32 |
| C# files mapped | 372 |
| C# LOC | 78,003 |
| Ported so far | 8 files (`openstack-polyio` I/O core) |

## How the two trees stay in sync

Three pieces:

1. **`PORT_MAP.tsv`** — one row per C# file: status, LOC, content hash, C# path,
   Rust path, crate. Regenerate with `gen_port.py` after adding or moving files.
2. **A header in every `.rs`** naming its source and the hash of that source at
   port time:
   ```rust
   // PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/BitStream.cs
   // PORT-SHA: 09d3020aa16a4f8f
   // PORT-STATUS: done
   ```
3. **`./sync-check.sh <dotnet-root>`** — compares recorded hashes against the C#
   tree and reports `STALE` / `MISSING` / `NEW` / `TODO`. Exits non-zero on
   `STALE` or `MISSING`, so it works as a CI gate on the C# repo: a PR touching a
   ported file fails until the Rust side is updated or the hash is deliberately
   bumped.

Workflow when C# changes: run `sync-check.sh`, it names the `.rs` to touch, make
the equivalent change, update `PORT-SHA` to the new hash.

## Naming

| C# | Rust |
|---|---|
| project `OpenStack.Gfx` | crate `openstack-gfx`, dir `gfx/gfx/` |
| folder `System.IO/` | module `system_io/` |
| file `Polyfill+BinaryReader.cs` | `polyfill_binary_reader.rs` |
| `ReadL32AString` | `read_l32_a_string` |
| `ReadInt32E` (big-endian) | `read_i32_be` |
| `ReadInt32X(endian)` | `read_i32_x(big)` |

Namespaces are **not** mirrored — C# reopens `System.IO` to hang extensions off
BCL types, which has no Rust analogue. The folder structure carries the grouping
instead.

Folders named `lib`, `main`, or `mod` are remapped (`_LIB/` → `vendor/`) because
they collide with Rust's special module filenames.

## Idiom translations

Counts are from the actual tree, so you know what you're in for.

### Extension methods → blanket traits (399 sites)

The single most common pattern. `public static class Polyfill` with `this
BinaryReader source` becomes a trait with a blanket impl:

```rust
pub trait BinaryReaderExt: Read + Seek {
    fn read_l32_a_string(&mut self, max: usize, big: bool) -> Result<Option<String>> { ... }
}
impl<T: Read + Seek + ?Sized> BinaryReaderExt for T {}
```

Any `Read + Seek` picks up the whole surface, exactly as any `BinaryReader` did.
Callers must `use openstack_polyio::prelude::*` — C# got this from namespace
scope, Rust needs the trait in scope explicitly.

### `partial class` → one module per file (21 sites)

`Polyfill.cs`, `Polyfill+Stream.cs`, `Polyfill+BinaryReader.cs` are three parts of
one C# class. Each becomes its own module contributing its own trait. This is
what makes the file-for-file rule survive contact with partials.

### Fluent returns → return the new state

C# `Skip()`/`Seek()`/`Align()` return `BinaryReader` for chaining. `&mut self`
chaining fights the borrow checker for no gain, so these return the resulting
position instead:

```csharp
r.Skip(4).ReadInt32()
```
```rust
r.skip(4)?; r.read_i32()?
```

### `null` returns → `Option`, `throw` → `Result`

C# returns `null` for zero-length strings and buffers, and callers branch on it.
That is `Option<T>`, not an empty `String` — collapsing the two changes behaviour.
`throwOnError: bool` parameters collapse into the `Result`; callers that passed
`false` use `.ok()`.

### `unsafe` / `MemoryMarshal` / `fixed` → explicit field reads (190 / 38 / 57 sites)

The heaviest concentration is in binary parsing, where C# blits structs straight
out of byte buffers. Don't reach for `transmute`. Read field by field through
`BinaryReaderExt`, or use `bytemuck` where the layout is genuinely POD and
`#[repr(C)]`. This is slower to write and catches the endianness and padding
assumptions the C# left implicit.

### Inheritance → composition + traits (39 abstract, 87 virtual)

Shallow, which is the good news. The deepest concrete chain is
`PartCell → ObjCell → SortCell → LandCell` in `phy2`. Pattern: base class fields
become a struct embedded as a field named `base`, base methods become a trait
with default bodies, `override` becomes a trait impl.

### Reflection → registries (148 sites, plus 16 `dynamic`)

**The hardest part of this port, concentrated in `core/openstack/src/type_x.rs`
and `manager.rs`.** `TypeX` resolves types from strings at runtime via
`Type.GetType` with assembly redirects; `RAssemblyAttribute` / `RTypeAttribute`
tag types for discovery. Rust has no runtime type graph.

Plan for it before starting `openstack-core`: replace attribute scanning with an
explicit registry — a `HashMap<&str, fn() -> Box<dyn Asset>>` populated either by
a build script or by the `inventory` crate's link-time collection. It is a real
design change, not a transcription, and it should be settled once rather than
improvised per call site.

### `async`/`Task<T>` → futures (82 sites)

`Task<T>` → `Pin<Box<dyn Future<Output = T> + Send>>`. Generic async methods
break object safety, so split: an object-safe `get_asset_any` returning
`Box<dyn Any>`, plus a generic `get_asset<T>` helper that downcasts. See
`i_source.rs`.

### `event` → callbacks or channels (60 sites)

Mostly in `gfx` and `platforms`. `Vec<Box<dyn Fn(&Args)>>` for the direct
translation; a channel where the subscriber is on another thread.

### Operator overloads → `std::ops` (135 sites)

Mechanical. Note C#'s `==` on classes is reference equality unless overloaded,
while `PartialEq` in Rust is structural — check each `IEquatable<T>` for which
one the C# actually meant.

## Deviations policy

Where Rust cannot express the C# faithfully, or where the C# is wrong, the port
says so **in a comment at the site**, and the deviation is listed here. Silent
divergence is what makes parallel maintenance fail.

Found so far while porting the I/O core:

- **`ByteXorStream.Read` ignores its `offset` argument** — it XORs `buffer[i]`
  instead of `buffer[offset + i]`, corrupting the head of the caller's buffer and
  leaving the bytes it actually read encoded. `Write` gets it right. Rust's
  `Read::read(&mut [u8])` has no offset parameter, so the bug cannot be
  expressed. **Worth fixing in the C# tree.**
- **`ByteXorStream.Write` mutates the caller's buffer in place.** `&[u8]` forbids
  that; the port copies into scratch.
- **`BitStream`'s constructor guard is `source.Length >= 0`**, always true, so it
  reads `source[1]` unconditionally and panics on a source shorter than two
  bytes. The port length-checks properly.
- **`Polyfill.ReadBytes(Stream, int)` ignores the return of `Stream.Read`**, so a
  short read silently yields a zero-padded buffer. The port uses `read_exact` and
  reports truncation.
- **The `W` ("wide") string family does not read UTF-16.** Every `BinaryReader`
  in the tree is constructed without an explicit encoding, so `ReadChars` decodes
  UTF-8. The port matches observed behaviour, not the name. If UTF-16 was
  intended, that is a C#-side bug and both trees need the same fix.
- **`Peek` restores the position even when the callback throws.** The C# leaks
  the seek on exception.
- **`X_LumpNO2` and `X_Lump2NO` are field-identical.** Both kept to preserve the
  mapping; likely a copy-paste slip upstream.

## Suggested order

Bottom-up along the dependency graph, so nothing is ported against a stub:

1. `core/polyio` — no dependencies. **In progress**: I/O core done; remaining are
   `system_numerics/*` (Vector/Matrix — consider delegating to `glam` behind the
   same API), `system_drawing/*`, `system/half_float.rs` (→ `half` crate),
   `system_io/huffman.rs`, `partial_input_stream.rs`.
2. `core/polyfills` → depends on polyio.
3. `gfx`, `sfx`, `vfx` → depend on polyfills.
4. `core/openstack` → depends on all three. **Resolve the reflection strategy
   before starting this crate.**
5. `phy2` — 27k LOC, the largest single crate, but self-contained (no project
   references), so it can proceed in parallel with everything above.
6. `platforms/*` — thin bindings over engine SDKs. Check each has a viable Rust
   binding before committing; several (Stride, Unreal, WPF) may not, and a
   C#-side shim may make more sense than a port.

`aix`, `phy`, and the `*Tests` projects are near-empty stubs.
