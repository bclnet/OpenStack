// PORT-SOURCE: Core/OpenStack/Platform_Test/Platform_Test.cs
// PORT-SHA: 6cc0bcef39c1321d
// PORT-STATUS: done
//
// The test platform: `TestGfxApi`, `TestGfxSprite`, `TestGfxModel`,
// `TestGfxLight`, `TestGfxTerrain`, `TestSfx`, `TestClientHost` — every member
// of all of them is `throw new NotImplementedException()` or
// `throw new NotSupportedException()`.
//
// So it is not a test double. A test double returns benign values so the code
// under test can run; this throws on contact, which means any test that touches
// graphics or audio fails regardless of whether the code is correct. Combined
// with `PlatformX.InTestHost` selecting it automatically (by sniffing assembly
// names for a `testhost,` prefix), the effect is that graphics-touching tests
// cannot pass — which is consistent with `OpenStack.SfxTests` containing one
// empty test method.
//
// Not ported. Rust test doubles go beside the tests that use them, and the
// pattern is already in the tree: `gfx`'s `CountingBuilder`, `sfx`'s
// `Counting`, and `openstack`'s `Fake` platform are all real doubles that
// return values and let assertions run.
//
// `TestClientHost.Dispose()` throws too, the same defect as
// `UnknownClientHost` and `DirectBitmap`.
