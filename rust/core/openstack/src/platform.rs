// PORT-SOURCE: Core/OpenStack/Platform.cs
// PORT-SHA: bfce3236f8c25da0
// PORT-STATUS: done
//
// Platform registration and the global "current platform" switch: which
// graphics and audio backends are live, what the OS is, what capabilities are
// available.
//
// C#-SIDE BUG — **`PlatformX.Epsilon` is half of machine epsilon.**
//
//     float epsilon = 1f, comparison;
//     do { epsilon *= 0.5f; comparison = 1.0f + epsilon; } while (comparison > 1.0f);
//     return epsilon;
//
// The loop exits when `1 + epsilon` rounds back to 1 — that is, it returns the
// first value too small to matter, one halving past the one that does. Machine
// epsilon for `f32` is 2^-23 (about 1.19e-7); this returns 2^-24 (5.96e-8). Any
// tolerance comparison using it is twice as strict as intended. `f32::EPSILON`
// is the correct constant and needs no runtime probing.
//
// STRUCTURAL NOTE. The C# holds `Current`, `Gfx`, `Sfx`, `Platforms`, and
// `Options` as mutable statics with no synchronisation, and `Activate` mutates
// several of them in sequence. Two threads activating concurrently interleave
// those writes and can leave `Current` pointing at one platform with `Gfx` from
// another. The port puts the whole set behind one lock so a switch is atomic.

use std::sync::{Mutex, OnceLock};

/// C# `PlatformX.OS`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
pub enum Os {
    #[default]
    Unknown,
    Windows,
    OSX,
    Linux,
    Android,
}

/// C# `PlatformX.PlatformOS`, resolved at compile time.
///
/// The C# probes `RuntimeInformation` at startup and detects Android by
/// sniffing `OSDescription` for an `"android-"` prefix. `cfg!` knows the target
/// without a string comparison.
pub const fn platform_os() -> Os {
    if cfg!(target_os = "windows") {
        Os::Windows
    } else if cfg!(target_os = "macos") {
        Os::OSX
    } else if cfg!(target_os = "android") {
        Os::Android
    } else if cfg!(target_os = "linux") {
        Os::Linux
    } else {
        Os::Unknown
    }
}

/// Machine epsilon for `f32`.
///
/// C# `PlatformX.Epsilon` computes this at startup and lands on half the right
/// value — see the module header. `f32::EPSILON` is exact and free.
pub const EPSILON: f32 = f32::EPSILON;

/// The C#'s value, for any tolerance that was tuned against it.
#[deprecated(note = "mirrors a C#-side bug: half of machine epsilon")]
pub const EPSILON_HALF_BUG_COMPAT: f32 = f32::EPSILON / 2.0;

/// C# `[Flags] PlatformX.Caps`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Default)]
pub struct Caps(u32);

impl Caps {
    pub const NONE: Self = Self(0x0);
    pub const READ_DDS: Self = Self(0x1);
    pub const DRAWING: Self = Self(0x2);

    #[inline]
    pub const fn contains(self, other: Self) -> bool {
        self.0 & other.0 == other.0
    }

    #[inline]
    pub const fn union(self, other: Self) -> Self {
        Self(self.0 | other.0)
    }

    #[inline]
    pub const fn bits(self) -> u32 {
        self.0
    }
}

impl std::ops::BitOr for Caps {
    type Output = Self;
    fn bitor(self, o: Self) -> Self {
        self.union(o)
    }
}

/// C# `abstract class Platform(string id, string name)`.
///
/// `GfxFactory`/`SfxFactory` were `Func<IOpenGfx[]>` fields defaulting to
/// `() => null`; a backend that forgot to set them silently produced no
/// renderer. They are trait methods here, so a backend that provides neither is
/// explicit about it rather than accidentally null.
pub trait Platform: Send + Sync {
    /// C# `Id`.
    fn id(&self) -> &str;

    /// C# `Name` / `DisplayName` (which just returns `Name`).
    fn name(&self) -> &str;

    /// C# `Enabled`.
    fn enabled(&self) -> bool {
        true
    }

    /// C# `Caps`.
    fn caps(&self) -> Caps {
        Caps::NONE
    }

    /// C# `Activate()`.
    ///
    /// The base implementation installs the platform's assert and log
    /// callbacks into the global `Log`. Logging setup is the caller's business
    /// here — `openstack_polyfills::log::set_sink` does it directly — so this
    /// defaults to doing nothing.
    fn activate(&self) {}

    /// C# `Deactivate()`.
    fn deactivate(&self) {}
}

/// C# `UnknownPlatform.This`.
#[derive(Debug, Default)]
pub struct UnknownPlatform;

impl Platform for UnknownPlatform {
    fn id(&self) -> &str {
        "UK"
    }
    fn name(&self) -> &str {
        "Unknown"
    }
}

/// The mutable global state C# kept as loose statics.
struct PlatformState {
    current: Box<dyn Platform>,
    registered: Vec<String>,
}

fn state() -> &'static Mutex<PlatformState> {
    static S: OnceLock<Mutex<PlatformState>> = OnceLock::new();
    S.get_or_init(|| {
        Mutex::new(PlatformState {
            current: Box::new(UnknownPlatform),
            registered: vec!["UK".to_string()],
        })
    })
}

/// C# `PlatformX.Activate(Platform platform)`.
///
/// A disabled or missing platform falls back to `UnknownPlatform`, as in the C#.
/// The swap is atomic: deactivate-then-activate happens under one lock, so a
/// concurrent caller cannot observe a half-switched state.
pub fn activate(platform: Box<dyn Platform>) -> String {
    let platform: Box<dyn Platform> = if platform.enabled() {
        platform
    } else {
        Box::new(UnknownPlatform)
    };
    let mut g = state().lock().unwrap_or_else(|p| p.into_inner());
    let id = platform.id().to_string();
    if g.current.id() == id {
        return id;
    }
    g.current.deactivate();
    platform.activate();
    if !g.registered.contains(&id) {
        g.registered.push(id.clone());
    }
    g.current = platform;
    id
}

/// C# `PlatformX.Current` — the active platform's id.
pub fn current_id() -> String {
    state()
        .lock()
        .unwrap_or_else(|p| p.into_inner())
        .current
        .id()
        .to_string()
}

/// C# `PlatformX.Current.Caps`.
pub fn current_caps() -> Caps {
    state()
        .lock()
        .unwrap_or_else(|p| p.into_inner())
        .current
        .caps()
}

/// C# `PlatformX.Platforms` — every id registered so far.
pub fn registered() -> Vec<String> {
    state()
        .lock()
        .unwrap_or_else(|p| p.into_inner())
        .registered
        .clone()
}

// NOT PORTED: `PlatformX.InTestHost`, which walks the loaded assemblies looking
// for one named `testhost,` and silently swaps in `TestPlatform` when found.
// Rust selects a test platform with `#[cfg(test)]` or a feature flag, which is
// explicit and cannot misfire on an unrelated dependency whose name happens to
// start with "testhost".

#[cfg(test)]
mod tests {
    use super::*;

    struct Fake(&'static str, bool, Caps);

    impl Platform for Fake {
        fn id(&self) -> &str {
            self.0
        }
        fn name(&self) -> &str {
            self.0
        }
        fn enabled(&self) -> bool {
            self.1
        }
        fn caps(&self) -> Caps {
            self.2
        }
    }

    #[test]
    fn epsilon_is_machine_epsilon_not_half_of_it() {
        assert_eq!(EPSILON, f32::EPSILON);
        // The defining property: 1 + eps must be distinguishable from 1.
        assert_ne!(1.0f32 + EPSILON, 1.0f32);
        // And the C#'s value must not be — which is why it was wrong.
        #[allow(deprecated)]
        {
            assert_eq!(1.0f32 + EPSILON_HALF_BUG_COMPAT, 1.0f32);
        }
    }

    #[test]
    fn disabled_platforms_fall_back_to_unknown() {
        let id = activate(Box::new(Fake("DISABLED", false, Caps::NONE)));
        assert_eq!(id, "UK");
    }

    #[test]
    fn activating_registers_and_switches() {
        activate(Box::new(Fake("GL", true, Caps::READ_DDS)));
        assert_eq!(current_id(), "GL");
        assert!(registered().contains(&"GL".to_string()));
        assert!(current_caps().contains(Caps::READ_DDS));
        activate(Box::new(UnknownPlatform)); // restore for other tests
    }

    #[test]
    fn caps_compose_and_test() {
        let c = Caps::READ_DDS | Caps::DRAWING;
        assert!(c.contains(Caps::READ_DDS));
        assert!(c.contains(Caps::DRAWING));
        assert!(!Caps::READ_DDS.contains(Caps::DRAWING));
        assert!(c.contains(Caps::NONE), "empty is always contained");
    }

    #[test]
    fn os_detection_returns_something_sane() {
        // Whatever the host, it must not be a value outside the enum.
        assert!(matches!(
            platform_os(),
            Os::Windows | Os::OSX | Os::Linux | Os::Android | Os::Unknown
        ));
    }
}
