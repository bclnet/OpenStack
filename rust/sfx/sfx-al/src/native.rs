// PORT-SOURCE: Sfx/OpenStack.Sfx.Al/Native.cs
// PORT-SHA: f98cc3545e567421
// PORT-STATUS: done
//
// NOT PORTED — this whole crate is a vendored copy of OpenTK's OpenAL P/Invoke
// bindings: 4,660 live lines across 11 files with 134 `DllImport` declarations
// and zero logic of its own.
//
// Two reasons not to hand-translate it:
//
//   1. **Nothing in the solution calls it.** The only project referencing
//      `OpenStack.Sfx.Al` is `OpenStack.SfxTests`. There is not one call site
//      in the shipping code.
//   2. **Rust already has this.** FFI bindings are exactly what a `-sys` crate
//      is for: `openal-sys` / `alto` for OpenAL directly, or `cpal` / `rodio`
//      one level up if what is wanted is just playback. Any of them is
//      maintained, tested against real drivers, and not something to keep in
//      sync by hand across two languages — a single mistyped signature here is
//      undefined behaviour at the FFI boundary, and there are 134 chances.
//
// When the audio backend is built, implement `openstack_sfx::AudioBuilder` over
// whichever crate is chosen. That trait is the whole surface the rest of the
// codebase needs; none of it reaches into AL directly.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices if the C#
// side grows real logic here rather than more declarations.
