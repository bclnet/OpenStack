// PORT-SOURCE: Gfx/OpenStack.GfxTests/Gfx.cs
// PORT-SHA: 5cf900219e14fdab
// PORT-STATUS: done
//
// NOT PORTED — Rust unit tests live beside the code they exercise.
//
// This is an MSTest project. Its assertions have been carried across to
// `#[cfg(test)]` modules in the crates under test, which is where Rust puts
// them; a standalone test crate mirroring the C# file layout would duplicate
// them with no benefit.
//
// **These tests were valuable and have been mined.** The DDS header vectors from `Gfx_Texture.cs` and the camera/bone assertions from `Egin/Gfx_Render.cs` and `Egin/Gfx_Animate.cs` are now test cases in `openstack-gfx`'s `gfx_texture` and `openstack-gfx-egin`'s `egin_render`/`egin_animate` — the only external verification available anywhere in this port. See PORTING.md.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices if the C#
// side adds tests worth carrying over.
