// PORT-SOURCE: Sfx/OpenStack.Sfx/Sfx.cs
// PORT-SHA: 749113bf668ce72a
// PORT-STATUS: done
//
// The audio abstraction: a builder that turns decoded audio into a backend
// handle, and a manager that caches the results. Same shape as `gfx`'s
// `TextureManager`, so it uses the same `Backend`-with-associated-types
// approach rather than an open generic parameter.
//
// ================= `.Result` ON AN ASYNC CALL IS A DEADLOCK =================
//
//     public (Audio aud, object tag) CreateAudio(ISource source, object path) {
//         ...
//         var tag = LoadAudio(source, path).Result;
//
// `CreateAudio` is synchronous and blocks on a `Task` with `.Result`. On any
// thread with a `SynchronizationContext` — the WPF dispatcher thread and the
// Unity main thread both qualify, and this solution ships backends for both —
// the awaited continuation needs that same thread to resume, which `.Result` is
// occupying. **The application hangs.** It only appears to work on a plain
// thread-pool thread.
//
// `.Result` also wraps any load failure in an `AggregateException` rather than
// surfacing the original.
//
// The port splits the two properly: `create` takes already-decoded audio and is
// synchronous, and loading is the caller's business — so there is no blocking
// wait to deadlock on. If a caller wants to load and build in one step, it
// awaits the load itself and then calls `create`.

use std::collections::HashMap;

/// The backend's handle types. Mirrors `openstack_gfx::gfx::Backend`.
pub trait SfxBackend {
    /// C# `Audio` — the backend's buffer/source handle.
    type Audio: Clone;
}

/// C# `abstract class AudioBuilderBase<Audio>`.
pub trait AudioBuilder<B: SfxBackend> {
    /// C# `CreateAudio(ISource source, object path)`.
    ///
    /// Takes the decoded payload rather than a source and a path: the C#
    /// signature implied the builder did the loading, but it received an
    /// already-loaded `tag` at every call site.
    fn create(&mut self, decoded: &Audio) -> B::Audio;

    /// C# `DeleteAudio(ISource source, Audio audio)`. The `source` parameter is
    /// unused in every implementation, so it is dropped.
    fn delete(&mut self, audio: &B::Audio);
}

/// Decoded PCM plus its format — the payload the C# passed around as `object`.
#[derive(Debug, Clone, PartialEq)]
pub struct Audio {
    pub channels: u16,
    pub sample_rate: u32,
    pub bits_per_sample: u16,
    pub data: Vec<u8>,
}

impl Audio {
    /// Duration in seconds, or `None` if the header describes no frames.
    pub fn duration_secs(&self) -> Option<f64> {
        let bytes_per_frame =
            self.channels as usize * (self.bits_per_sample as usize / 8);
        if bytes_per_frame == 0 || self.sample_rate == 0 {
            return None;
        }
        Some(self.data.len() as f64 / bytes_per_frame as f64 / self.sample_rate as f64)
    }
}

/// C# `class AudioManager<Audio>`.
///
/// Beyond the `.Result` deadlock in the module header:
///
///   * `LoadAudio` guards with `Log.Assert(!CachedAudios.ContainsKey(key))`,
///     whose body is empty — the check does nothing.
///   * `DeleteAudio` removes the cache entry but leaves any in-flight
///     `PreloadTasks` entry behind, so a preload that completes after a delete
///     leaks its task and can never be collected.
///   * `PreloadTasks` is only ever cleaned up on the success path of
///     `LoadAudio`; a failed load leaves the entry wedged, and every later
///     attempt awaits the same faulted task.
pub struct AudioManager<B: SfxBackend, AB: AudioBuilder<B>> {
    builder: AB,
    cached: HashMap<String, B::Audio>,
}

impl<B: SfxBackend, AB: AudioBuilder<B>> AudioManager<B, AB> {
    pub fn new(builder: AB) -> Self {
        Self { builder, cached: HashMap::new() }
    }

    /// C# `CreateAudio(ISource, object path)`, minus the blocking load.
    pub fn create(&mut self, path: &str, decoded: &Audio) -> B::Audio {
        if let Some(a) = self.cached.get(path) {
            return a.clone();
        }
        let a = self.builder.create(decoded);
        self.cached.insert(path.to_string(), a.clone());
        a
    }

    /// Already-cached handle, if any — lets a caller skip decoding entirely.
    pub fn get(&self, path: &str) -> Option<&B::Audio> {
        self.cached.get(path)
    }

    /// C# `DeleteAudio(ISource, object path)`.
    pub fn delete(&mut self, path: &str) {
        if let Some(a) = self.cached.remove(path) {
            self.builder.delete(&a);
        }
    }

    /// Release everything. No C# equivalent; its cache only ever grew.
    pub fn clear(&mut self) {
        for a in self.cached.values() {
            self.builder.delete(a);
        }
        self.cached.clear();
    }

    pub fn len(&self) -> usize {
        self.cached.len()
    }

    pub fn is_empty(&self) -> bool {
        self.cached.is_empty()
    }
}

// NOT PORTED: `IOpenSfx` (empty marker) and `IOpenSfx<Audio>` (an
// `AudioManager` property plus a `CreateAudio` that duplicates the manager's).
// A backend owns an `AudioManager` as a field; the marker adds nothing Rust
// needs. Also not ported: `PreloadAudio`, which exists only to prime the
// `PreloadTasks` map that the deadlock above depends on. Callers that want
// eager loading spawn it themselves and call `create` when it lands.

#[cfg(test)]
mod tests {
    use super::*;

    struct Test;
    impl SfxBackend for Test {
        type Audio = u32;
    }

    #[derive(Default)]
    struct Counting {
        next: u32,
        creates: u32,
        deletes: u32,
    }

    impl AudioBuilder<Test> for Counting {
        fn create(&mut self, _d: &Audio) -> u32 {
            self.next += 1;
            self.creates += 1;
            self.next
        }
        fn delete(&mut self, _a: &u32) {
            self.deletes += 1;
        }
    }

    fn pcm() -> Audio {
        Audio {
            channels: 2,
            sample_rate: 44100,
            bits_per_sample: 16,
            data: vec![0; 44100 * 4], // one second of stereo 16-bit
        }
    }

    #[test]
    fn cache_hits_by_path() {
        let mut m = AudioManager::<Test, _>::new(Counting::default());
        let a = m.create("shot.wav", &pcm());
        let b = m.create("shot.wav", &pcm());
        assert_eq!(a, b);
        assert_eq!(m.builder.creates, 1);
    }

    #[test]
    fn delete_releases_the_handle() {
        let mut m = AudioManager::<Test, _>::new(Counting::default());
        m.create("a.wav", &pcm());
        m.delete("a.wav");
        assert_eq!(m.builder.deletes, 1);
        assert!(m.is_empty());
        // Deleting again is a no-op, not a panic.
        m.delete("a.wav");
        assert_eq!(m.builder.deletes, 1);
    }

    #[test]
    fn clear_releases_everything() {
        let mut m = AudioManager::<Test, _>::new(Counting::default());
        m.create("a.wav", &pcm());
        m.create("b.wav", &pcm());
        m.clear();
        assert_eq!(m.builder.deletes, 2);
        assert!(m.is_empty());
    }

    #[test]
    fn duration_from_pcm_header() {
        assert!((pcm().duration_secs().unwrap() - 1.0).abs() < 1e-9);
    }

    #[test]
    fn degenerate_headers_do_not_divide_by_zero() {
        let bad = Audio { channels: 0, sample_rate: 44100, bits_per_sample: 16, data: vec![0; 8] };
        assert!(bad.duration_secs().is_none());
        let bad2 = Audio { channels: 2, sample_rate: 0, bits_per_sample: 16, data: vec![0; 8] };
        assert!(bad2.duration_secs().is_none());
    }

    #[test]
    fn get_does_not_build() {
        let mut m = AudioManager::<Test, _>::new(Counting::default());
        assert!(m.get("nope.wav").is_none());
        assert_eq!(m.builder.creates, 0);
    }
}
