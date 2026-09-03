// PORT-SOURCE: Core/OpenStack.Polyfills/System.Collections.Generic/KVExtensions.cs
// PORT-SHA: 81620d1bd889ef5f
// PORT-STATUS: done
//
// Typed accessors over `IDictionary<string, object>` — the loosely-typed
// key-value trees that Valve-style format parsers produce. C# casts the boxed
// `object` at each access and lets `InvalidCastException` escape.
//
// Rust has no boxed-primitive `object`, so the port introduces an explicit
// `KV` value enum. That turns every cast into a checked conversion: the
// `get_*` family returns `Option`, so a missing key and a wrong-typed value are
// both ordinary results instead of exceptions.
//
// The C# also silently returns `default(T)` for a missing key in some overloads
// and throws in others, which callers could not tell apart. Here it is uniform.

use std::collections::HashMap;

use glam::{Vec3, Vec4};

/// A loosely-typed value from a parsed key-value tree.
#[derive(Debug, Clone, PartialEq)]
pub enum KV {
    Null,
    Bool(bool),
    Int(i64),
    UInt(u64),
    Float(f64),
    Str(String),
    Array(Vec<KV>),
    Map(HashMap<String, KV>),
}

impl KV {
    /// C# `GetInt32` / `GetInt64`. Accepts an integer, or a string that parses
    /// as one, matching the C#'s tolerance for both.
    pub fn as_i64(&self) -> Option<i64> {
        match self {
            KV::Int(v) => Some(*v),
            KV::UInt(v) => i64::try_from(*v).ok(),
            KV::Float(v) => Some(*v as i64),
            KV::Bool(b) => Some(*b as i64),
            KV::Str(s) => s.trim().parse().ok(),
            _ => None,
        }
    }

    /// C# `GetUInt32` / `GetUInt64`.
    pub fn as_u64(&self) -> Option<u64> {
        match self {
            KV::UInt(v) => Some(*v),
            KV::Int(v) => u64::try_from(*v).ok(),
            KV::Float(v) if *v >= 0.0 => Some(*v as u64),
            KV::Bool(b) => Some(*b as u64),
            KV::Str(s) => s.trim().parse().ok(),
            _ => None,
        }
    }

    /// C# `GetDouble` / `GetFloat`.
    pub fn as_f64(&self) -> Option<f64> {
        match self {
            KV::Float(v) => Some(*v),
            KV::Int(v) => Some(*v as f64),
            KV::UInt(v) => Some(*v as f64),
            KV::Str(s) => s.trim().parse().ok(),
            _ => None,
        }
    }

    pub fn as_f32(&self) -> Option<f32> {
        self.as_f64().map(|v| v as f32)
    }

    pub fn as_bool(&self) -> Option<bool> {
        match self {
            KV::Bool(b) => Some(*b),
            KV::Int(v) => Some(*v != 0),
            KV::UInt(v) => Some(*v != 0),
            KV::Str(s) => match s.trim().to_ascii_lowercase().as_str() {
                "true" | "1" => Some(true),
                "false" | "0" => Some(false),
                _ => None,
            },
            _ => None,
        }
    }

    pub fn as_str(&self) -> Option<&str> {
        match self {
            KV::Str(s) => Some(s),
            _ => None,
        }
    }

    /// C# `GetArray` / `GetMap`.
    pub fn as_array(&self) -> Option<&[KV]> {
        match self {
            KV::Array(v) => Some(v),
            _ => None,
        }
    }

    /// C# `GetSub(string)`.
    pub fn as_map(&self) -> Option<&HashMap<String, KV>> {
        match self {
            KV::Map(m) => Some(m),
            _ => None,
        }
    }

    /// C# `Get<TKey, TValue>(dict, key)` — look up a child by key.
    pub fn get(&self, key: &str) -> Option<&KV> {
        self.as_map()?.get(key).filter(|v| **v != KV::Null)
    }

    /// C# `GetVector3` / `ToVector3` — three consecutive numbers.
    ///
    /// Accepts either an array of three numbers or `{x, y, z}` keys, both of
    /// which appear in the formats this parses.
    pub fn as_vec3(&self) -> Option<Vec3> {
        if let Some(a) = self.as_array() {
            if a.len() >= 3 {
                return Some(Vec3::new(
                    a[0].as_f32()?,
                    a[1].as_f32()?,
                    a[2].as_f32()?,
                ));
            }
            return None;
        }
        let m = self.as_map()?;
        Some(Vec3::new(
            m.get("x")?.as_f32()?,
            m.get("y")?.as_f32()?,
            m.get("z")?.as_f32()?,
        ))
    }

    /// C# `GetVector4` / `ToVector4`.
    pub fn as_vec4(&self) -> Option<Vec4> {
        if let Some(a) = self.as_array() {
            if a.len() >= 4 {
                return Some(Vec4::new(
                    a[0].as_f32()?,
                    a[1].as_f32()?,
                    a[2].as_f32()?,
                    a[3].as_f32()?,
                ));
            }
            return None;
        }
        let m = self.as_map()?;
        Some(Vec4::new(
            m.get("x")?.as_f32()?,
            m.get("y")?.as_f32()?,
            m.get("z")?.as_f32()?,
            m.get("w")?.as_f32()?,
        ))
    }

    /// C# `GetInt64Array` / `GetUInt64Array`.
    pub fn as_i64_array(&self) -> Option<Vec<i64>> {
        self.as_array()?.iter().map(KV::as_i64).collect()
    }

    pub fn as_u64_array(&self) -> Option<Vec<u64>> {
        self.as_array()?.iter().map(KV::as_u64).collect()
    }
}

/// C# `TryGet` — chained lookup, e.g. `root.path(["a", "b", "c"])`.
pub fn path<'a>(root: &'a KV, keys: &[&str]) -> Option<&'a KV> {
    keys.iter().try_fold(root, |node, k| node.get(k))
}

#[cfg(test)]
mod tests {
    use super::*;

    fn sample() -> KV {
        let mut inner = HashMap::new();
        inner.insert("count".into(), KV::Int(42));
        inner.insert("name".into(), KV::Str("thing".into()));
        inner.insert("scale".into(), KV::Str("2.5".into()));
        inner.insert(
            "pos".into(),
            KV::Array(vec![KV::Float(1.0), KV::Float(2.0), KV::Float(3.0)]),
        );
        let mut root = HashMap::new();
        root.insert("entity".into(), KV::Map(inner));
        KV::Map(root)
    }

    #[test]
    fn nested_lookup_by_path() {
        let kv = sample();
        assert_eq!(path(&kv, &["entity", "count"]).unwrap().as_i64(), Some(42));
        assert!(path(&kv, &["entity", "missing"]).is_none());
        assert!(path(&kv, &["nope", "count"]).is_none());
    }

    #[test]
    fn numeric_strings_convert_like_the_c_sharp() {
        let kv = sample();
        let scale = path(&kv, &["entity", "scale"]).unwrap();
        assert_eq!(scale.as_f32(), Some(2.5));
        assert_eq!(scale.as_i64(), Some(2), "string -> int truncates");
    }

    #[test]
    fn wrong_type_is_none_not_an_exception() {
        let kv = sample();
        let name = path(&kv, &["entity", "name"]).unwrap();
        assert_eq!(name.as_i64(), None);
        assert_eq!(name.as_str(), Some("thing"));
    }

    #[test]
    fn vectors_parse_from_arrays_and_from_xyz_maps() {
        let kv = sample();
        let from_array = path(&kv, &["entity", "pos"]).unwrap().as_vec3().unwrap();
        assert_eq!(from_array, Vec3::new(1.0, 2.0, 3.0));

        let mut m = HashMap::new();
        m.insert("x".to_string(), KV::Float(4.0));
        m.insert("y".to_string(), KV::Float(5.0));
        m.insert("z".to_string(), KV::Float(6.0));
        assert_eq!(KV::Map(m).as_vec3().unwrap(), Vec3::new(4.0, 5.0, 6.0));
    }

    #[test]
    fn short_arrays_do_not_produce_a_partial_vector() {
        let short = KV::Array(vec![KV::Float(1.0), KV::Float(2.0)]);
        assert!(short.as_vec3().is_none());
    }

    #[test]
    fn null_values_read_as_missing() {
        let mut m = HashMap::new();
        m.insert("k".to_string(), KV::Null);
        assert!(KV::Map(m).get("k").is_none());
    }

    #[test]
    fn integer_arrays_fail_as_a_whole_on_a_bad_element() {
        let good = KV::Array(vec![KV::Int(1), KV::Int(2)]);
        assert_eq!(good.as_i64_array(), Some(vec![1, 2]));
        let bad = KV::Array(vec![KV::Int(1), KV::Str("x".into())]);
        assert_eq!(bad.as_i64_array(), None);
    }
}
