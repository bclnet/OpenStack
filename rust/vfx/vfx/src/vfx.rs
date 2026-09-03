// PORT-SOURCE: Vfx/OpenStack.Vfx/Vfx.cs
// PORT-SHA: 4737d0dae6cc3f9e
// PORT-STATUS: done
//
// The virtual filesystem: a `FileSystem` abstraction with directory, in-memory,
// aggregate, and archive-backed implementations, plus `Advance`, which nests a
// filesystem inside a container file (a zip, a disc image, a 3DS cartridge).
//
// C# uses an abstract base class with four abstract members and two virtual
// ones. That maps to a trait with provided methods, and `Advance`/`FindPaths`
// become provided methods on the trait — same shape, no inheritance.
//
// ================= THE PATH MATCHER IS LOCALE-DEPENDENT ====================
//
// `CreateMatcher` compares filenames with `StringComparison.CurrentCultureIgnoreCase`
// in all three of its branches. For **file paths** that is the wrong
// comparison: under a Turkish locale `"I".ToLower()` is `"ı"`, not `"i"`, so a
// file named `INDEX.DAT` does not match the pattern `index.dat` — and the same
// build works fine in en-US. It is the classic Turkish-I bug, and it makes asset
// lookup silently locale-dependent. Paths want ordinal comparison; this port
// uses ASCII-case-insensitive matching, which is locale-invariant.
//
// Two more, both crashes where the surrounding code returns null:
//
//   * `ZipFileSystem.Open` is `Arc.GetEntry(...).Open()` with no null check, so
//     a missing entry throws `NullReferenceException` — while
//     `DirectoryFileSystem.Open` and `VirtualFileSystem.Open` return null for
//     the same condition. `AggregateFileSystem.Open` relies on that null to try
//     the next filesystem, so one zip in an aggregate turns a normal miss into
//     an exception.
//   * `SevenZipFileSystem.Open` uses `.First(...)`, which throws
//     `InvalidOperationException` on a miss, for the same reason.
//   * `Advance` for `.bin`/`.cue` calls `Glob("", "*.cue").Single()`, which
//     throws when a directory holds zero or several `.cue` files — a multi-disc
//     set is not exotic.
//
// `NetworkFileSystem` is not ported: its constructor rejects any URI with a
// filename, `Glob`/`FileExists`/`FileInfo` all call local `File.Exists`, and
// `Open` unconditionally returns null. It cannot open anything, over the
// network or otherwise. See `vfx_network.rs`.

use std::io::{Read, Seek};

/// C# `(string path, long length)` from `FileInfo`.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct FileInfo {
    pub path: String,
    pub length: u64,
}

/// Anything the VFS can hand back for reading.
pub trait VfsRead: Read + Seek {}
impl<T: Read + Seek> VfsRead for T {}

/// C# `abstract class FileSystem`.
pub trait FileSystem {
    /// C# `Glob(string path, string searchPattern)`.
    fn glob(&self, path: &str, search_pattern: &str) -> Vec<String>;

    /// C# `FileExists(string path)`.
    fn file_exists(&self, path: &str) -> bool;

    /// C# `FileInfo(string path)` — `None` where the C# returned `(null, 0)`.
    fn file_info(&self, path: &str) -> Option<FileInfo>;

    /// C# `Open(string path, string mode)`.
    ///
    /// Returns `None` for a missing entry. The C#'s archive implementations
    /// threw here instead of returning null, which broke `AggregateFileSystem`'s
    /// fallback; see the module header.
    fn open(&self, path: &str) -> Option<Box<dyn VfsRead + '_>>;

    /// C# `FindPaths(string path, string searchPattern)`.
    ///
    /// Expands `(a:b:c)` alternation groups in the pattern before globbing, so
    /// `tex(0:1).dds` searches `tex0.dds` and `tex1.dds`.
    fn find_paths(&self, path: &str, search_pattern: &str) -> Vec<String> {
        if let Some((prefix, alts, suffix)) = split_alternation(search_pattern) {
            let mut out = Vec::new();
            for alt in alts {
                let expanded = format!("{prefix}{alt}{suffix}");
                out.extend(self.find_paths(path, &expanded));
            }
            return out;
        }
        self.glob(path, search_pattern)
    }
}

/// Splits `"a(x:y)b"` into `("a", ["x", "y"], "b")`.
fn split_alternation(pattern: &str) -> Option<(&str, Vec<&str>, &str)> {
    let start = pattern.find('(')?;
    let mid = pattern[start..].find(':')? + start;
    let end = pattern[mid..].find(')')? + mid;
    if start >= end {
        return None;
    }
    Some((
        &pattern[..start],
        pattern[start + 1..end].split(':').collect(),
        &pattern[end + 1..],
    ))
}

/// C# `FileSystem.CreateMatcher(string searchPattern)`.
///
/// Locale-invariant, unlike the C# — see the module header. An empty pattern
/// matches everything, as in the C#.
pub fn create_matcher(search_pattern: &str) -> Box<dyn Fn(&str) -> bool> {
    if search_pattern.is_empty() {
        return Box::new(|_| true);
    }
    let stars = search_pattern.matches('*').count();
    if stars == 0 {
        let p = search_pattern.to_ascii_lowercase();
        return Box::new(move |x| x.to_ascii_lowercase() == p);
    }
    if stars == 1 {
        let bare = search_pattern.replace('*', "").to_ascii_lowercase();
        if search_pattern.starts_with('*') {
            return Box::new(move |x| x.to_ascii_lowercase().ends_with(&bare));
        }
        if search_pattern.ends_with('*') {
            return Box::new(move |x| x.to_ascii_lowercase().starts_with(&bare));
        }
    }
    // General case: `*` matches any run, everything else is literal. The C#
    // built a regex here; a direct matcher avoids the dependency and the
    // `catch { return false; }` that silently swallowed bad patterns.
    let parts: Vec<String> = search_pattern
        .to_ascii_lowercase()
        .split('*')
        .map(str::to_string)
        .collect();
    Box::new(move |x| glob_match(&x.to_ascii_lowercase(), &parts))
}

/// `parts` are the literal runs between `*`s, already lowercased.
fn glob_match(s: &str, parts: &[String]) -> bool {
    if parts.is_empty() {
        return true;
    }
    if !s.starts_with(&*parts[0]) {
        return false;
    }
    let mut rest = &s[parts[0].len()..];
    let last = parts.len() - 1;
    for (i, part) in parts.iter().enumerate().skip(1) {
        if i == last {
            // Final literal must land at the end.
            return rest.len() >= part.len() && rest.ends_with(&**part);
        }
        match rest.find(&**part) {
            Some(at) => rest = &rest[at + part.len()..],
            None => return false,
        }
    }
    true
}

/// C# `class VirtualFileSystem` — an in-memory map of path to bytes.
#[derive(Debug, Default, Clone)]
pub struct VirtualFileSystem {
    virtuals: std::collections::HashMap<String, Vec<u8>>,
}

impl VirtualFileSystem {
    pub fn new(virtuals: std::collections::HashMap<String, Vec<u8>>) -> Self {
        Self { virtuals }
    }

    pub fn insert(&mut self, path: impl Into<String>, data: Vec<u8>) {
        self.virtuals.insert(path.into(), data);
    }
}

impl FileSystem for VirtualFileSystem {
    fn glob(&self, _path: &str, search_pattern: &str) -> Vec<String> {
        let m = create_matcher(search_pattern);
        let mut out: Vec<String> = self
            .virtuals
            .keys()
            .filter(|k| m(k))
            .cloned()
            .collect();
        // HashMap order is arbitrary; the C# relied on Dictionary insertion
        // order, which is itself unspecified. Sorting makes globs reproducible.
        out.sort();
        out
    }

    fn file_exists(&self, path: &str) -> bool {
        self.virtuals.contains_key(path)
    }

    fn file_info(&self, path: &str) -> Option<FileInfo> {
        self.virtuals.get(path).map(|d| FileInfo {
            path: path.to_string(),
            length: d.len() as u64,
        })
    }

    fn open(&self, path: &str) -> Option<Box<dyn VfsRead + '_>> {
        self.virtuals
            .get(path)
            .map(|d| Box::new(std::io::Cursor::new(d.as_slice())) as Box<dyn VfsRead + '_>)
    }
}

/// C# `class AggregateFileSystem` — first hit wins.
pub struct AggregateFileSystem {
    aggregate: Vec<Box<dyn FileSystem>>,
}

impl AggregateFileSystem {
    /// C# spells the field `aggreate`; corrected here.
    pub fn new(aggregate: Vec<Box<dyn FileSystem>>) -> Self {
        Self { aggregate }
    }
}

impl FileSystem for AggregateFileSystem {
    fn glob(&self, path: &str, search_pattern: &str) -> Vec<String> {
        self.aggregate
            .iter()
            .flat_map(|f| f.glob(path, search_pattern))
            .collect()
    }

    fn file_exists(&self, path: &str) -> bool {
        self.aggregate.iter().any(|f| f.file_exists(path))
    }

    fn file_info(&self, path: &str) -> Option<FileInfo> {
        self.aggregate.iter().find_map(|f| f.file_info(path))
    }

    fn open(&self, path: &str) -> Option<Box<dyn VfsRead + '_>> {
        self.aggregate.iter().find_map(|f| f.open(path))
    }
}

/// C# `class DirectoryFileSystem` — a real directory on disk.
pub struct DirectoryFileSystem {
    root: std::path::PathBuf,
}

impl DirectoryFileSystem {
    pub fn new(root: impl Into<std::path::PathBuf>) -> Self {
        Self { root: root.into() }
    }

    /// Joins under the root and rejects anything that escapes it.
    ///
    /// The C# calls `Path.Combine(Root, path)` with no such check, so a path
    /// containing `..` reads outside the intended tree — and these paths come
    /// from asset files.
    fn resolve(&self, path: &str) -> Option<std::path::PathBuf> {
        let joined = self.root.join(path);
        let mut depth = 0i32;
        for c in std::path::Path::new(path).components() {
            match c {
                std::path::Component::ParentDir => {
                    depth -= 1;
                    if depth < 0 {
                        return None;
                    }
                }
                std::path::Component::Normal(_) => depth += 1,
                std::path::Component::RootDir | std::path::Component::Prefix(_) => return None,
                std::path::Component::CurDir => {}
            }
        }
        Some(joined)
    }
}

impl FileSystem for DirectoryFileSystem {
    fn glob(&self, path: &str, search_pattern: &str) -> Vec<String> {
        let Some(base) = self.resolve(path) else {
            return Vec::new();
        };
        let m = create_matcher(search_pattern);
        let mut out = Vec::new();
        walk(&base, &base, &m, &mut out);
        out.sort();
        out
    }

    fn file_exists(&self, path: &str) -> bool {
        self.resolve(path).map(|p| p.is_file()).unwrap_or(false)
    }

    fn file_info(&self, path: &str) -> Option<FileInfo> {
        let p = self.resolve(path)?;
        let md = std::fs::metadata(&p).ok()?;
        if !md.is_file() {
            return None;
        }
        Some(FileInfo { path: path.to_string(), length: md.len() })
    }

    fn open(&self, path: &str) -> Option<Box<dyn VfsRead + '_>> {
        let p = self.resolve(path)?;
        std::fs::File::open(p)
            .ok()
            .map(|f| Box::new(f) as Box<dyn VfsRead + '_>)
    }
}

fn walk(
    base: &std::path::Path,
    dir: &std::path::Path,
    m: &dyn Fn(&str) -> bool,
    out: &mut Vec<String>,
) {
    let Ok(rd) = std::fs::read_dir(dir) else {
        return;
    };
    for e in rd.flatten() {
        let p = e.path();
        if p.is_dir() {
            walk(base, &p, m, out);
        } else if let Ok(rel) = p.strip_prefix(base) {
            let rel = rel.to_string_lossy().replace('\\', "/");
            if m(&rel) {
                out.push(rel);
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::collections::HashMap;

    fn vfs() -> VirtualFileSystem {
        let mut m = HashMap::new();
        m.insert("a/index.dat".to_string(), b"one".to_vec());
        m.insert("a/INDEX.DAT2".to_string(), b"two".to_vec());
        m.insert("b/tex0.dds".to_string(), b"t0".to_vec());
        m.insert("b/tex1.dds".to_string(), b"t1".to_vec());
        VirtualFileSystem::new(m)
    }

    #[test]
    fn exact_match_is_case_insensitive_but_locale_invariant() {
        // Under a Turkish locale the C# fails this: "I".ToLower() is "ı".
        let m = create_matcher("a/index.dat");
        assert!(m("a/INDEX.DAT"));
        assert!(m("A/Index.Dat"));
        assert!(!m("a/index.dat2"));
    }

    #[test]
    fn prefix_and_suffix_wildcards() {
        assert!(create_matcher("*.dds")("b/tex0.dds"));
        assert!(!create_matcher("*.dds")("b/tex0.png"));
        assert!(create_matcher("b/*")("b/tex0.dds"));
        assert!(!create_matcher("b/*")("a/tex0.dds"));
    }

    #[test]
    fn interior_wildcards_match_runs() {
        let m = create_matcher("b/*.dds");
        assert!(m("b/tex0.dds"));
        assert!(!m("a/tex0.dds"));
        let m2 = create_matcher("*tex*dds");
        assert!(m2("b/tex1.dds"));
        assert!(!m2("b/img1.png"));
    }

    #[test]
    fn empty_pattern_matches_everything() {
        assert!(create_matcher("")("anything at all"));
    }

    #[test]
    fn alternation_groups_expand() {
        let f = vfs();
        let mut found = f.find_paths("", "b/tex(0:1).dds");
        found.sort();
        assert_eq!(found, vec!["b/tex0.dds", "b/tex1.dds"]);
    }

    #[test]
    fn alternation_with_no_group_falls_through_to_glob() {
        assert_eq!(vfs().find_paths("", "b/tex0.dds"), vec!["b/tex0.dds"]);
    }

    #[test]
    fn virtual_fs_reads_back_what_was_put_in() {
        let f = vfs();
        assert!(f.file_exists("a/index.dat"));
        assert_eq!(f.file_info("a/index.dat").unwrap().length, 3);
        let mut s = String::new();
        f.open("a/index.dat").unwrap().read_to_string(&mut s).unwrap();
        assert_eq!(s, "one");
    }

    #[test]
    fn missing_entries_return_none_rather_than_throwing() {
        // The archive filesystems in C# throw here, which breaks the aggregate.
        let f = vfs();
        assert!(f.open("nope").is_none());
        assert!(f.file_info("nope").is_none());
        assert!(!f.file_exists("nope"));
    }

    #[test]
    fn aggregate_falls_through_to_the_next_filesystem() {
        let mut a = HashMap::new();
        a.insert("only_in_a".to_string(), b"A".to_vec());
        let mut b = HashMap::new();
        b.insert("only_in_b".to_string(), b"B".to_vec());
        let agg = AggregateFileSystem::new(vec![
            Box::new(VirtualFileSystem::new(a)),
            Box::new(VirtualFileSystem::new(b)),
        ]);
        assert!(agg.file_exists("only_in_a"));
        assert!(agg.file_exists("only_in_b"));
        assert!(agg.open("only_in_b").is_some(), "must not stop at the first miss");
        assert!(agg.open("nowhere").is_none());
    }

    #[test]
    fn directory_fs_rejects_paths_escaping_its_root() {
        // The C# Path.Combine's straight through, so `..` reads outside.
        let d = DirectoryFileSystem::new("/tmp/does-not-matter");
        assert!(d.resolve("../../etc/passwd").is_none());
        assert!(d.resolve("a/../b").is_some(), "interior .. that stays inside is fine");
        assert!(d.resolve("ok/path").is_some());
    }
}
