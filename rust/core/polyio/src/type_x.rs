// PORT-SOURCE: Core/OpenStack.PolyIO/TypeX.cs
// PORT-SHA: 74ce0c87f971da34
// PORT-STATUS: done
//
// C# `TypeX` resolves types from strings at runtime: `ScanTypes` walks every
// loaded assembly for `[RAssembly]`/`[RType]` attributes, builds name->Type
// tables, and `GetRType` feeds them to `Type.GetType` through custom assembly
// and type resolvers, with assembly-name redirects on the side.
//
// FIRST, THE USEFUL FACT: `TypeX` has **zero call sites**. `ScanTypes`,
// `GetRType`, `RAssemblyAttribute`, `RTypeAttribute`, `GetDefaultConstructor`,
// `GetAllProperties`, and `GetAllFields` are referenced nowhere outside this
// one file. Solution-wide there are 4 `Activator.CreateInstance` calls (all in
// a test harness, all with a static type argument) and 1 `Type.GetType`
// (inside this file). The runtime type graph is not actually load-bearing here.
//
// THE PORT: a registration pattern, per the project decision. Types announce
// themselves at their definition site with `register_type!`; a module-level
// `register` function collects them; the crate root wires the modules together.
// `TypeRegistry::create` then does what `Activator.CreateInstance(GetRType(s))`
// did, minus the reflection.
//
// WHY EXPLICIT REGISTRATION AND NOT LINK-TIME COLLECTION. The `inventory` crate
// would let registrations be gathered without the wiring, which reads nicer.
// It relies on life-before-main and linker section tricks, and registrations
// silently vanish when a crate ends up unreferenced, under some LTO settings,
// and on wasm — a missing asset type would then surface as a "not found" at
// runtime with nothing to grep for. Explicit registration cannot fail that way:
// if a module is not wired in, the crate does not compile. That trade is worth
// the extra line per module. Swapping in `inventory` later only means replacing
// the body of `register`.

use std::any::{Any, TypeId};
use std::collections::HashMap;
use std::sync::{OnceLock, RwLock};

/// Builds a fresh instance. C# `Activator.CreateInstance(type)` via the
/// parameterless constructor `GetDefaultConstructor` looked up.
pub type Factory = fn() -> Box<dyn Any + Send + Sync>;

/// One registered type. C# derived all of this from `[RType]` at scan time.
#[derive(Clone, Copy)]
pub struct TypeRegistration {
    /// C# `RTypeAttribute.Name` — the name formats refer to this type by.
    pub name: &'static str,
    /// C# "l-type" grouping: the namespace this name is scoped under. `None`
    /// puts the name in the flat "r-type" table.
    pub namespace: Option<&'static str>,
    /// Rust type identity, for checked downcasts.
    pub type_id: fn() -> TypeId,
    /// The Rust type's name, for error messages.
    pub rust_name: &'static str,
    pub factory: Factory,
}

impl std::fmt::Debug for TypeRegistration {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("TypeRegistration")
            .field("name", &self.name)
            .field("namespace", &self.namespace)
            .field("rust_name", &self.rust_name)
            .finish()
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum RegistryError {
    /// Two types claimed the same name.
    ///
    /// C# built these tables with `ToDictionary`, which throws on a duplicate
    /// r-type, but used `GroupBy(..).First()` for l-types and so silently kept
    /// whichever was scanned first. Both are errors here.
    Duplicate { name: String, existing: &'static str, incoming: &'static str },
    NotFound { name: String },
    /// `create_as::<T>` matched a name whose type is not `T`.
    WrongType { name: String, requested: &'static str, actual: &'static str },
}

impl std::fmt::Display for RegistryError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            RegistryError::Duplicate { name, existing, incoming } => write!(
                f,
                "type name '{name}' registered twice: {existing} and {incoming}"
            ),
            RegistryError::NotFound { name } => write!(f, "no type registered as '{name}'"),
            RegistryError::WrongType { name, requested, actual } => {
                write!(f, "'{name}' is {actual}, not {requested}")
            }
        }
    }
}

impl std::error::Error for RegistryError {}

/// C# `TypeX` + `AssemblyTag`, as one owned table instead of static mutable
/// dictionaries keyed by `Assembly`.
#[derive(Default)]
pub struct TypeRegistry {
    /// C# `AssemblyTag.RTypes` — the flat name table.
    r_types: HashMap<&'static str, TypeRegistration>,
    /// C# `AssemblyTag.LTypes` — namespace -> name -> type.
    l_types: HashMap<&'static str, HashMap<&'static str, TypeRegistration>>,
    /// C# `AssemblyRedirects` — an old name that should resolve elsewhere.
    redirects: HashMap<String, String>,
}

impl TypeRegistry {
    pub fn new() -> Self {
        Self::default()
    }

    /// C# `ScanTypes`, one type at a time and checked.
    pub fn register(&mut self, reg: TypeRegistration) -> Result<(), RegistryError> {
        let table = match reg.namespace {
            Some(ns) => self.l_types.entry(ns).or_default(),
            None => &mut self.r_types,
        };
        if let Some(existing) = table.get(reg.name) {
            return Err(RegistryError::Duplicate {
                name: reg.name.to_string(),
                existing: existing.rust_name,
                incoming: reg.rust_name,
            });
        }
        table.insert(reg.name, reg);
        Ok(())
    }

    /// Register many, failing on the first conflict.
    pub fn register_all(
        &mut self,
        regs: impl IntoIterator<Item = TypeRegistration>,
    ) -> Result<(), RegistryError> {
        for r in regs {
            self.register(r)?;
        }
        Ok(())
    }

    /// C# `AssemblyRedirects` — resolve `from` as if it were `to`.
    pub fn redirect(&mut self, from: impl Into<String>, to: impl Into<String>) {
        self.redirects.insert(from.into(), to.into());
    }

    /// C# `GetRType(assembly, typeName)`.
    ///
    /// Accepts either a bare r-type name (`"Binary_Dds"`) or a
    /// namespace-qualified l-type name (`"Formats.Valve.Binary_Vpk"`), matching
    /// the C#'s two lookup paths. Redirects are applied first.
    pub fn resolve(&self, name: &str) -> Option<&TypeRegistration> {
        let name = self.redirects.get(name).map(String::as_str).unwrap_or(name);
        if let Some(r) = self.r_types.get(name) {
            return Some(r);
        }
        // C# splits on the last '.' into (namespace, name).
        let (ns, base) = name.rsplit_once('.')?;
        self.l_types.get(ns)?.get(base)
    }

    /// C# `Activator.CreateInstance(GetRType(name))`.
    pub fn create(&self, name: &str) -> Result<Box<dyn Any + Send + Sync>, RegistryError> {
        let reg = self
            .resolve(name)
            .ok_or_else(|| RegistryError::NotFound { name: name.to_string() })?;
        Ok((reg.factory)())
    }

    /// Typed form — the common case, and the one the C# could only express by
    /// casting the `object` back afterwards.
    pub fn create_as<T: Any>(&self, name: &str) -> Result<Box<T>, RegistryError> {
        let reg = self
            .resolve(name)
            .ok_or_else(|| RegistryError::NotFound { name: name.to_string() })?;
        if (reg.type_id)() != TypeId::of::<T>() {
            return Err(RegistryError::WrongType {
                name: name.to_string(),
                requested: std::any::type_name::<T>(),
                actual: reg.rust_name,
            });
        }
        (reg.factory)()
            .downcast::<T>()
            .map_err(|_| RegistryError::WrongType {
                name: name.to_string(),
                requested: std::any::type_name::<T>(),
                actual: reg.rust_name,
            })
    }

    /// Every registered name, for diagnostics and for listing supported formats.
    pub fn names(&self) -> Vec<String> {
        let mut out: Vec<String> = self.r_types.keys().map(|s| s.to_string()).collect();
        for (ns, t) in &self.l_types {
            out.extend(t.keys().map(|n| format!("{ns}.{n}")));
        }
        out.sort();
        out
    }

    #[inline]
    pub fn len(&self) -> usize {
        self.r_types.len() + self.l_types.values().map(HashMap::len).sum::<usize>()
    }

    #[inline]
    pub fn is_empty(&self) -> bool {
        self.len() == 0
    }
}

/// Process-wide registry, for the places the C# reached for a static.
///
/// Prefer threading a `&TypeRegistry` where you can — it is testable and lets
/// two registries coexist. This exists because the C# API was static and some
/// call sites will port over more cleanly against a global.
static GLOBAL: OnceLock<RwLock<TypeRegistry>> = OnceLock::new();

pub fn global() -> &'static RwLock<TypeRegistry> {
    GLOBAL.get_or_init(|| RwLock::new(TypeRegistry::new()))
}

/// Declares a registration next to the type it describes, the way `[RType]` did.
///
/// ```ignore
/// pub struct BinaryDds { /* .. */ }
/// impl Default for BinaryDds { fn default() -> Self { Self {} } }
///
/// register_type!(BinaryDds, "Binary_Dds");                    // r-type
/// register_type!(BinaryVpk, "Binary_Vpk", ns = "Formats.Valve"); // l-type
/// ```
///
/// Each expands to a `const` the module's `register` function collects.
#[macro_export]
macro_rules! register_type {
    ($ty:ty, $name:expr) => {
        $crate::type_x::TypeRegistration {
            name: $name,
            namespace: None,
            type_id: || ::std::any::TypeId::of::<$ty>(),
            rust_name: ::std::stringify!($ty),
            factory: || ::std::boxed::Box::new(<$ty as ::std::default::Default>::default()),
        }
    };
    ($ty:ty, $name:expr, ns = $ns:expr) => {
        $crate::type_x::TypeRegistration {
            name: $name,
            namespace: ::std::option::Option::Some($ns),
            type_id: || ::std::any::TypeId::of::<$ty>(),
            rust_name: ::std::stringify!($ty),
            factory: || ::std::boxed::Box::new(<$ty as ::std::default::Default>::default()),
        }
    };
}

// NOT PORTED: `GetAllProperties`, `GetAllFields`, `GetDefaultConstructor`.
// All three enumerate members at runtime and have no call sites. Rust has no
// field reflection; anything that needs to walk a struct's fields wants a
// derive macro over a `Fields` trait, which should be designed against a real
// caller rather than speculatively.

#[cfg(test)]
mod tests {
    use super::*;

    #[derive(Debug, Default, PartialEq)]
    struct Dds {
        width: u32,
    }

    #[derive(Debug, Default, PartialEq)]
    struct Vpk;

    fn registry() -> TypeRegistry {
        let mut r = TypeRegistry::new();
        r.register(register_type!(Dds, "Binary_Dds")).unwrap();
        r.register(register_type!(Vpk, "Binary_Vpk", ns = "Formats.Valve"))
            .unwrap();
        r
    }

    #[test]
    fn creates_a_registered_type_by_name() {
        let r = registry();
        assert_eq!(*r.create_as::<Dds>("Binary_Dds").unwrap(), Dds { width: 0 });
    }

    #[test]
    fn resolves_namespace_qualified_names() {
        let r = registry();
        assert!(r.create_as::<Vpk>("Formats.Valve.Binary_Vpk").is_ok());
        // The bare name must not leak out of its namespace.
        assert!(matches!(
            r.create_as::<Vpk>("Binary_Vpk"),
            Err(RegistryError::NotFound { .. })
        ));
    }

    #[test]
    fn duplicate_registration_is_rejected() {
        // The C# silently kept the first for l-types; this reports the clash.
        let mut r = registry();
        let err = r.register(register_type!(Vpk, "Binary_Dds")).unwrap_err();
        assert!(matches!(err, RegistryError::Duplicate { .. }));
    }

    #[test]
    fn wrong_type_is_caught_at_the_downcast() {
        let r = registry();
        assert!(matches!(
            r.create_as::<Vpk>("Binary_Dds"),
            Err(RegistryError::WrongType { .. })
        ));
    }

    #[test]
    fn unknown_names_report_not_found() {
        assert!(matches!(
            registry().create("Nope"),
            Err(RegistryError::NotFound { .. })
        ));
    }

    #[test]
    fn redirects_are_applied_before_lookup() {
        let mut r = registry();
        r.redirect("Legacy_Dds", "Binary_Dds");
        assert!(r.create_as::<Dds>("Legacy_Dds").is_ok());
    }

    #[test]
    fn names_lists_both_tables_qualified() {
        assert_eq!(
            registry().names(),
            vec!["Binary_Dds", "Formats.Valve.Binary_Vpk"]
        );
    }

    #[test]
    fn each_create_returns_an_independent_instance() {
        let r = registry();
        let mut a = r.create_as::<Dds>("Binary_Dds").unwrap();
        a.width = 99;
        assert_eq!(r.create_as::<Dds>("Binary_Dds").unwrap().width, 0);
    }
}
