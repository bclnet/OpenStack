// PORT-SOURCE: Core/OpenStack.Polyfills/System/ExtensionXAttribute.cs
// PORT-SHA: e71538f66f6b247c
// PORT-STATUS: done
//
// C# `[ExtensionX("...")]` tags an enum field with the file extension it maps
// to, read back at runtime via `GetCustomAttributes`.
//
// Rust has no attribute reflection, so the association becomes data: a lookup
// table beside the type, in the same spirit as `type_x`'s registration pattern.
// Where the enum is fixed, a `match` arm is simpler still and should be
// preferred.

/// Maps a variant to the file extension it represents.
///
/// ```
/// # use openstack_polyfills::system::extension_x_attribute::HasExtension;
/// #[derive(Clone, Copy)]
/// enum Kind { Dds, Png }
///
/// impl HasExtension for Kind {
///     fn extension(&self) -> &'static str {
///         match self { Kind::Dds => "dds", Kind::Png => "png" }
///     }
/// }
/// assert_eq!(Kind::Dds.extension(), "dds");
/// ```
pub trait HasExtension {
    /// C# `ExtensionXAttribute.Extension`, without the leading dot.
    fn extension(&self) -> &'static str;
}
