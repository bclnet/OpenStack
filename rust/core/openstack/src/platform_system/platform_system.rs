// PORT-SOURCE: Core/OpenStack/Platform_System/Platform_System.cs
// PORT-SHA: 588e870fb49c98d2
// PORT-STATUS: done
//
// A "system" audio platform. Both of `SystemAudioBuilder`'s methods are
// `throw new NotImplementedException()`, so `SystemSfx` wraps an
// `AudioManager` that cannot build or delete anything — the first
// `CreateAudio` throws.
//
// Note `SystemSfx.CreateAudio` is declared `async` but contains no `await`; it
// calls the manager's blocking `CreateAudio` (the `.Result` deadlock documented
// in `openstack-sfx`) and wraps the result in a completed task. So it presents
// an async signature over a synchronous blocking call, which is the shape most
// likely to deadlock a UI thread while looking safe.
//
// Nothing to port: there is no implementation here. When a system audio backend
// is written, implement `openstack_sfx::AudioBuilder` — that trait is the whole
// surface, and it is synchronous by design so there is no blocking wait to get
// wrong.
