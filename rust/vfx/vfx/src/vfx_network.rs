// PORT-SOURCE: Vfx/OpenStack.Vfx/Vfx_Network.cs
// PORT-SHA: 1de2e94abd13dcbd
// PORT-STATUS: done
//
// `NetworkHost` — an `HttpClient` with a `MemoryCache` in front, used to fetch
// assets over HTTP.
//
// NOT PORTED, pending a decision. Rust has no HTTP client in `std`, so porting
// this means committing the workspace to `reqwest` (plus `tokio`) or `ureq`
// (blocking, far lighter). That is the same async-runtime choice deferred back
// at `AsyncCoroutineQueue`, and it should be made once, against a real caller.
//
// There is no such caller yet: `NetworkHost` is referenced only from
// `OpenStack.Vfx.Program`. `ureq` is the better default unless something else
// in the workspace already needs an async runtime, since nothing about this
// class benefits from one — it is request/response with a cache.
//
// ALSO NOT PORTED, and this one should simply be deleted on the C# side:
// `NetworkFileSystem` (declared in `Vfx.cs`) claims to be a network-backed
// `FileSystem` but is not networked at all —
//
//   * its constructor throws for any URI whose path has a filename, so it can
//     only ever address a directory;
//   * `Glob`, `FileExists`, and `FileInfo` all call **local** `File.Exists` /
//     the local globber, ignoring the URI entirely;
//   * `Open` unconditionally returns `null`.
//
// So it cannot open a file by any route, local or remote. It is dead weight
// that reads as a working feature.
