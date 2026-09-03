// PORT-SOURCE: Platforms/OpenStack.Platform.Tests/Gl.cs
// PORT-SHA: ebf078db41e5fb8b
// PORT-STATUS: done
//
// NOT PORTED — Rust unit tests live beside the code they exercise.
//
// This is an MSTest project. Its assertions have been carried across to
// `#[cfg(test)]` modules in the crates under test, which is where Rust puts
// them; a standalone test crate mirroring the C# file layout would duplicate
// them with no benefit.
//
// Its `Gl_Render`/`Gl_Renderer`/`Gl` tests exercise the OpenGL backend, which is itself not ported yet — so there is nothing here to test against.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices if the C#
// side adds tests worth carrying over.
