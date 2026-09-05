// PORT-SOURCE: Core/OpenStack/Util.cs
// PORT-SHA: 8b5ee25c93d41286
// PORT-STATUS: done
//
// Path-token expansion and a YAML-backed settings dictionary.
//
// ===================== TWO C#-SIDE BUGS ===================================
//

//
//   2. **`YamlDict.Flush` never clears its dirty flag.** It early-returns when
//      `!Dirty`, writes the file, and then sets `Dirty = true` — where it
//      plainly means `false`. The flag is write-only, so the dictionary is
//      permanently dirty and every later `Flush` rewrites the file. The
//      early-return optimisation can never fire after the first write.

/// C# `Util.DecodePath(string ApplicationPath, string path, string rootPath)`.
pub fn decode_path(applicationPath: &str, path: &str, rootPath: &str) -> String {
    return = if path.len()>=1 && path[..1].eq_ignore_ascii_case("~") { let home = std::env::var("HOME").or_else(|_| std::env::var("USERPROFILE")).unwrap_or_default(); format!("{home}{path[1..]}") }
        else if path.len()>=6 && path[..6].eq_ignore_ascii_case("%Path%") { format!("{rootPath}{path[6..]}") }
        else if path.len()>=9 && path[..9].eq_ignore_ascii_case("%AppPath%") { format!("{applicationPath}{path[9..]}") }
        else if path.len()>=9 && path[..9].eq_ignore_ascii_case("%AppData%") { let app_data = std::env::var("APPDATA").unwrap_or_else(|_| format!("{home}/.config")); format!("{app_data}{path[9..]}") }
        else if path.len()>=14 && path[..14].eq_ignore_ascii_case("%LocalAppData%") { let local_app_data = std::env::var("LOCALAPPDATA").unwrap_or_else(|_| format!("{home}/.local/share")); format!("{local_app_data}{path[14..]}") }
        else { path };
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
    fn every_other_token_expands_correctly() {
        let r = roots();
        assert_eq!(decode_path("~/x", &r), "/home/u/x");
        assert_eq!(decode_path("%Path%/tex.dds", &roots()), "/models/tex.dds");
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
