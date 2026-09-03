// PORT-SOURCE: Core/OpenStack/Util.cs
// PORT-SHA: 8b5ee25c93d41286
// PORT-STATUS: done
//
// Path-token expansion and a YAML-backed settings dictionary.
//
// ===================== TWO C#-SIDE BUGS ===================================
//
//   1. **`%ModelPath%` is sliced at the wrong offset.** The token is 11
//      characters, but the branch is `path[6..]`:
//
//          path.StartsWith("%ModelPath%", ...) ? $"{rootPath}{path[6..]}"
//
//      So `%ModelPath%/tex.dds` expands to `<rootPath>` + `ath%/tex.dds`,
//      dragging five characters of the token into the result. Every sibling
//      branch has it right — `%AppPath%` (9) uses `path[9..]`, `%AppData%` (9)
//      uses `path[9..]`, `%LocalAppData%` (14) uses `path[14..]`. **Fix this in
//      the C# tree.**
//
//   2. **`YamlDict.Flush` never clears its dirty flag.** It early-returns when
//      `!Dirty`, writes the file, and then sets `Dirty = true` — where it
//      plainly means `false`. The flag is write-only, so the dictionary is
//      permanently dirty and every later `Flush` rewrites the file. The
//      early-return optimisation can never fire after the first write.

/// Named locations a path token can expand to. The C# resolved these inline via
/// `Environment.GetFolderPath`; passing them in keeps the function pure and
/// testable, and lets a caller override for a sandbox.
#[derive(Debug, Clone, Default)]
pub struct PathRoots {
    /// `~`
    pub user_profile: String,
    /// `%AppPath%`
    pub application_path: String,
    /// `%ModelPath%`
    pub root_path: String,
    /// `%AppData%`
    pub app_data: String,
    /// `%LocalAppData%`
    pub local_app_data: String,
}

impl PathRoots {
    /// Fills from the environment, matching the C#'s `SpecialFolder` lookups.
    pub fn from_env(application_path: impl Into<String>, root_path: impl Into<String>) -> Self {
        let home = std::env::var("HOME")
            .or_else(|_| std::env::var("USERPROFILE"))
            .unwrap_or_default();
        let app_data = std::env::var("APPDATA")
            .unwrap_or_else(|_| format!("{home}/.config"));
        let local_app_data = std::env::var("LOCALAPPDATA")
            .unwrap_or_else(|_| format!("{home}/.local/share"));
        Self {
            user_profile: home,
            application_path: application_path.into(),
            root_path: root_path.into(),
            app_data,
            local_app_data,
        }
    }
}

/// C# `Util.DecodePath(string ApplicationPath, string path, string rootPath)`.
///
/// Token matching is case-insensitive, as in the C# (`OrdinalIgnoreCase`).
/// Unlike the C#, `%ModelPath%` strips its full 11 characters — see bug 1.
pub fn decode_path(path: &str, roots: &PathRoots) -> String {
    // Longest first, so `%LocalAppData%` is not shadowed by `%AppData%`. (They
    // do not actually prefix-collide, but ordering by length keeps that true if
    // a token is ever added.)
    const TOKENS: [&str; 4] = ["%LocalAppData%", "%ModelPath%", "%AppData%", "%AppPath%"];

    if let Some(rest) = path.strip_prefix('~') {
        return format!("{}{}", roots.user_profile, rest);
    }
    for tok in TOKENS {
        if path.len() >= tok.len() && path[..tok.len()].eq_ignore_ascii_case(tok) {
            let root = match tok {
                "%LocalAppData%" => &roots.local_app_data,
                "%ModelPath%" => &roots.root_path,
                "%AppData%" => &roots.app_data,
                "%AppPath%" => &roots.application_path,
                _ => unreachable!(),
            };
            return format!("{}{}", root, &path[tok.len()..]);
        }
    }
    path.to_string()
}

/// The C#'s literal `%ModelPath%` behaviour, for any caller that has been
/// working around the truncation.
#[deprecated(note = "mirrors a C#-side bug: keeps 5 characters of the token")]
pub fn decode_path_model_bug_compat(path: &str, root_path: &str) -> String {
    if path.len() >= 11 && path[..11].eq_ignore_ascii_case("%ModelPath%") {
        return format!("{}{}", root_path, &path[6..]);
    }
    path.to_string()
}

/// C# `class YamlDict : Dictionary<string, object>`.
///
/// The YAML dependency (`YamlDotNet`) is not pulled in here: the C# uses it for
/// one settings file, and the choice of `serde_yaml` vs `serde_yml` vs `toml`
/// should be made against a real caller. This models the dirty-tracking half —
/// which is where the bug was — and leaves serialisation to the caller.
#[derive(Debug, Default)]
pub struct SettingsDict {
    path: std::path::PathBuf,
    items: std::collections::HashMap<String, String>,
    dirty: bool,
}

impl SettingsDict {
    pub fn new(path: impl Into<std::path::PathBuf>) -> Self {
        Self { path: path.into(), items: Default::default(), dirty: false }
    }

    pub fn get(&self, key: &str) -> Option<&str> {
        self.items.get(key).map(String::as_str)
    }

    pub fn insert(&mut self, key: impl Into<String>, value: impl Into<String>) {
        self.items.insert(key.into(), value.into());
        self.dirty = true;
    }

    pub fn remove(&mut self, key: &str) -> bool {
        let had = self.items.remove(key).is_some();
        self.dirty |= had;
        had
    }

    /// C# `Dirty`.
    #[inline]
    pub fn is_dirty(&self) -> bool {
        self.dirty
    }

    pub fn path(&self) -> &std::path::Path {
        &self.path
    }

    /// C# `Flush()`, with the flag actually cleared — see bug 2.
    ///
    /// `serialize` renders the map; the caller supplies it so this type does not
    /// pick a YAML crate for the workspace.
    pub fn flush<F>(&mut self, serialize: F) -> std::io::Result<bool>
    where
        F: FnOnce(&std::collections::HashMap<String, String>) -> String,
    {
        if !self.dirty {
            return Ok(false);
        }
        std::fs::write(&self.path, serialize(&self.items))?;
        self.dirty = false;
        Ok(true)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn roots() -> PathRoots {
        PathRoots {
            user_profile: "/home/u".into(),
            application_path: "/opt/app".into(),
            root_path: "/models".into(),
            app_data: "/appdata".into(),
            local_app_data: "/localappdata".into(),
        }
    }

    #[test]
    fn model_path_strips_the_whole_token() {
        // The C# slices at [6..], leaving "ath%" glued to the result.
        assert_eq!(decode_path("%ModelPath%/tex.dds", &roots()), "/models/tex.dds");
    }

    #[test]
    fn the_c_sharp_behaviour_is_still_reachable_and_visibly_wrong() {
        #[allow(deprecated)]
        let got = decode_path_model_bug_compat("%ModelPath%/tex.dds", "/models");
        assert_eq!(got, "/modelsath%/tex.dds");
    }

    #[test]
    fn every_other_token_expands_correctly() {
        let r = roots();
        assert_eq!(decode_path("~/x", &r), "/home/u/x");
        assert_eq!(decode_path("%AppPath%/x", &r), "/opt/app/x");
        assert_eq!(decode_path("%AppData%/x", &r), "/appdata/x");
        assert_eq!(decode_path("%LocalAppData%/x", &r), "/localappdata/x");
    }

    #[test]
    fn token_matching_is_case_insensitive() {
        assert_eq!(decode_path("%apppath%/x", &roots()), "/opt/app/x");
        assert_eq!(decode_path("%MODELPATH%/x", &roots()), "/models/x");
    }

    #[test]
    fn local_app_data_is_not_shadowed_by_app_data() {
        // Both start with '%' and share no prefix, but the ordering matters if
        // a token is ever added; pin the behaviour.
        assert_eq!(decode_path("%LocalAppData%/x", &roots()), "/localappdata/x");
    }

    #[test]
    fn unrecognised_paths_pass_through_untouched() {
        let r = roots();
        assert_eq!(decode_path("/absolute/path", &r), "/absolute/path");
        assert_eq!(decode_path("%NotAToken%/x", &r), "%NotAToken%/x");
        assert_eq!(decode_path("", &r), "");
    }

    #[test]
    fn short_paths_do_not_panic_on_the_token_slice() {
        // A path shorter than a token must not index past its end.
        assert_eq!(decode_path("%App", &roots()), "%App");
    }

    #[test]
    fn flush_clears_the_dirty_flag() {
        // The C# sets Dirty = true here, so it can never skip a write again.
        let dir = std::env::temp_dir().join("openstack-settings-test.yaml");
        let mut s = SettingsDict::new(&dir);
        assert!(!s.is_dirty());
        s.insert("a", "1");
        assert!(s.is_dirty());
        assert!(s.flush(|_| "a: 1\n".into()).unwrap(), "first flush writes");
        assert!(!s.is_dirty(), "flag must be cleared");
        assert!(!s.flush(|_| panic!("must not serialise again")).unwrap());
        let _ = std::fs::remove_file(&dir);
    }
}
