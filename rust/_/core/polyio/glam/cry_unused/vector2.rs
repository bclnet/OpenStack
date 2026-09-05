// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Cry+Unused/Vector2.cs
// PORT-SHA: a22e13f5ac73f24a
// PORT-STATUS: done
//
// Entirely commented out in the C# — 52 commented lines and one live line
// (a namespace declaration). The folder name says the rest: Crytek structs
// kept for reference, not compiled.
//
// The type names inside (`Vector3`, `Matrix3x3`, ...) collide with the live
// BCL and polyio types, which is presumably why they were commented out rather
// than deleted. Nothing to translate; if any is revived, port it *and* resolve
// the name clash against `system_numerics` in both trees.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` watches it.
