// PORT-SOURCE: Core/OpenStack/Client.cs
// PORT-SHA: 583752ebfbad2dfe
// PORT-STATUS: done
//
// Client-application scaffolding: a plugin host interface, a scene base class,
// and global frame timing.
//
// MOSTLY UNIMPLEMENTED ON THE C# SIDE. `Plugin` is a shell:
//
//     public static Plugin Create(string path) => default;   // always null
//     public static void OnClosing() { }                     // empty
//     public static void OnFocusGained() { }                 // empty
//     public static bool ProcessHotkeys(...) => true;         // constant
//     public static void Tick() { }                          // empty
//
// `Create` returns null unconditionally, so `Plugin.Plugins` is never populated
// and no plugin can ever load. Every hook is a no-op or a constant. The file
// also opens with `#pragma warning disable CS9113` — suppressing "parameter is
// unread" for `ClientBase`'s primary constructor, whose parameter is indeed
// never used.
//
// `ClientBase.LoadContent`/`UnloadContent` are declared `async Task` with no
// `await`, so they run synchronously and the compiler warns; and
// `IClientHost : IDisposable` is implemented by `UnknownClientHost` and
// `TestClientHost`, both of whose `Dispose()` throw
// `NotImplementedException` — so neither can appear in a `using` block.
//
// What is ported here is the part with actual semantics: the scene lifecycle
// (whose `IsDestroyed`/`IsLoaded` flags encode real state transitions) and
// frame timing. The plugin system is left out until there is something to plug
// in; `TypeRegistry` in `openstack-polyio` is the mechanism for it when that
// day comes.

/// C# `static class GlobalTime`.
///
/// The C# holds `Ticks` and `Delta` as bare mutable statics with no
/// synchronisation. Ported as a value the frame loop owns and passes down;
/// `global()` is available for callers that want the static feel.
#[derive(Debug, Clone, Copy, Default, PartialEq)]
pub struct GlobalTime {
    /// C# `Ticks`.
    pub ticks: u32,
    /// C# `Delta` — seconds since the previous frame.
    pub delta: f32,
}

impl GlobalTime {
    pub const fn new() -> Self {
        Self { ticks: 0, delta: 0.0 }
    }

    /// Advance one frame. `ticks` wraps rather than overflowing, matching the
    /// C#'s `uint` in a release build (a debug build there would throw).
    pub fn advance(&mut self, delta: f32) {
        self.ticks = self.ticks.wrapping_add(1);
        self.delta = delta;
    }
}

/// Process-wide frame time, for callers porting from `GlobalTime`'s statics.
pub fn global() -> &'static std::sync::Mutex<GlobalTime> {
    static T: std::sync::OnceLock<std::sync::Mutex<GlobalTime>> = std::sync::OnceLock::new();
    T.get_or_init(|| std::sync::Mutex::new(GlobalTime::new()))
}

/// C# `interface IClientHost : IDisposable`.
///
/// `Dispose` is not part of the trait: `Drop` runs automatically, and an
/// implementor needing teardown implements it. That also avoids reproducing the
/// throwing `Dispose` both C# implementors have.
pub trait ClientHost {
    /// C# `Run()`.
    fn run(&mut self);
}

/// C# `abstract class SceneBase : IDisposable`.
///
/// A trait plus the state it tracks. The C# mixes `virtual` methods with three
/// public mutable flags that callers were expected to read but not write;
/// `SceneState` owns them so the invariants hold.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct SceneState {
    /// C# `IsDestroyed`.
    pub is_destroyed: bool,
    /// C# `IsLoaded`.
    pub is_loaded: bool,
    /// C# `RenderedObjectsCount`.
    pub rendered_objects_count: i32,
}

/// C# `SceneBase`'s virtual members.
pub trait Scene {
    fn state(&self) -> &SceneState;
    fn state_mut(&mut self) -> &mut SceneState;

    /// C# `Load()` — sets `IsLoaded`.
    fn load(&mut self) {
        self.state_mut().is_loaded = true;
    }

    /// C# `Unload()`.
    fn unload(&mut self) {
        self.state_mut().is_loaded = false;
    }

    /// C# `Update()`.
    fn update(&mut self, _time: GlobalTime) {}

    /// C# `Draw()` — `false` to skip presenting the frame.
    fn draw(&mut self) -> bool {
        true
    }

    /// C# `Dispose()` — unload once, then mark destroyed.
    ///
    /// Idempotent, as the C# intended (`if (IsDestroyed) return;`). Kept as an
    /// explicit method rather than `Drop` because the C# is explicit about the
    /// destroy-once semantics and callers check the flag.
    fn destroy(&mut self) {
        if self.state().is_destroyed {
            return;
        }
        self.unload();
        self.state_mut().is_destroyed = true;
    }
}

// NOT PORTED: `Plugin`, `IPluginHost`, `ClientBase`. `Plugin.Create` always
// returns null and every hook is empty, so there is no behaviour to translate.
// `IPluginHost`'s 15 members are all untyped (`object ev`, `out IntPtr listPtr`)
// or raw-buffer marshalling that wants designing against a real plugin ABI
// rather than transcribing. `ClientBase`'s three members are an empty `Dispose`
// and two `async` methods with no `await`.

#[cfg(test)]
mod tests {
    use super::*;

    #[derive(Default)]
    struct TestScene {
        st: SceneState,
        loads: u32,
        unloads: u32,
    }

    impl Scene for TestScene {
        fn state(&self) -> &SceneState {
            &self.st
        }
        fn state_mut(&mut self) -> &mut SceneState {
            &mut self.st
        }
        fn load(&mut self) {
            self.loads += 1;
            self.st.is_loaded = true;
        }
        fn unload(&mut self) {
            self.unloads += 1;
            self.st.is_loaded = false;
        }
    }

    #[test]
    fn scene_lifecycle_transitions() {
        let mut s = TestScene::default();
        assert!(!s.state().is_loaded);
        s.load();
        assert!(s.state().is_loaded);
        s.destroy();
        assert!(s.state().is_destroyed);
        assert!(!s.state().is_loaded);
    }

    #[test]
    fn destroy_is_idempotent() {
        let mut s = TestScene::default();
        s.load();
        s.destroy();
        s.destroy();
        s.destroy();
        assert_eq!(s.unloads, 1, "unload must run exactly once");
    }

    #[test]
    fn draw_defaults_to_presenting() {
        let mut s = TestScene::default();
        assert!(s.draw());
    }

    #[test]
    fn time_advances_and_wraps_without_overflowing() {
        let mut t = GlobalTime::new();
        t.advance(0.016);
        assert_eq!(t.ticks, 1);
        assert!((t.delta - 0.016).abs() < 1e-6);
        t.ticks = u32::MAX;
        t.advance(0.016);
        assert_eq!(t.ticks, 0, "must wrap, not panic");
    }
}
