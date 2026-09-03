// PORT-SOURCE: Core/OpenStack/_LIB/SevenZip/SevenZip.cs
// PORT-SHA: ac01064f08345add
// PORT-STATUS: done
//
// NOT PORTED — `_LIB/SevenZip` is a vendored copy of the public-domain LZMA SDK
// (Igor Pavlov's reference C# implementation): 2,745 live lines across six
// files, all of it compression internals with no project-specific logic.
//
// The folder name says it: `_LIB` is third-party code kept in-tree because .NET
// had no good package for it at the time. Rust does — `lzma-rs` (pure Rust,
// LZMA/LZMA2/XZ), `sevenz-rust` (full .7z archives), or `xz2` if linking liblzma
// is acceptable.
//
// Hand-translating a range coder and its match finder is a poor use of effort:
// it is intricate, easy to get subtly wrong in ways that only surface on
// specific inputs, and the result would need the same maintenance as the
// original for no gain over a maintained crate.
//
// `openstack-vfx` already needs 7z reading for `SevenZipFileSystem`; wire both
// to the same crate when that lands.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices if the C#
// side diverges from the upstream SDK.
