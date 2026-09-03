// PORT-SOURCE: Core/OpenStack/Cache.cs
// PORT-SHA: 14b816cf49d611a7
// PORT-STATUS: done
//
// The whole file is two empty class declarations:
//
//     public class FsCache { }
//     public class MemCache { }
//
// No fields, no methods, no callers. Placeholders someone intended to fill in.
// Nothing to translate.
//
// When these are implemented, note that `gfx`'s `TextureManager` and `sfx`'s
// `AudioManager` already hand-roll their own caching (with the eviction and
// static-sharing problems documented in PORTING.md) — a real `MemCache` should
// probably absorb both rather than sit alongside them.
