// PORT-SOURCE: Core/OpenStack.PolyIO/ISource.cs
// PORT-SHA: 4bb05d80da4f6254
// PORT-STATUS: done
//
// C#:
//   public interface ISource {
//       Task<T> GetAsset<T>(object path, object option = default, bool throwOnError = true);
//   }
//
// Three things do not survive a literal translation:
//
//   1. `GetAsset<T>` is generic, so the trait would not be object-safe and
//      `dyn ISource` (which callers rely on) would be impossible. Split into an
//      object-safe `get_asset_any` returning `Box<dyn Any>`, plus a generic
//      `get_asset<T>` helper that downcasts.
//   2. `object path` / `object option` are untyped. `path` is modelled as an
//      enum of the shapes actually passed; `option` stays `dyn Any`.
//   3. `bool throwOnError` becomes the `Result` itself — callers that wanted
//      `false` use `.ok()`. Carrying the flag would mean returning
//      `Result<Option<T>>`, which is the same information twice.

use std::any::Any;
use std::future::Future;
use std::pin::Pin;

/// Boxed future alias — `Task<T>` with no external futures crate.
pub type BoxFuture<'a, T> = Pin<Box<dyn Future<Output = T> + Send + 'a>>;

/// The shapes passed as `object path` in the C# tree.
#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub enum AssetPath {
    /// A path or logical name.
    Name(String),
    /// A numeric file id.
    Id(u64),
}

impl From<String> for AssetPath {
    fn from(s: String) -> Self {
        AssetPath::Name(s)
    }
}

impl From<&str> for AssetPath {
    fn from(s: &str) -> Self {
        AssetPath::Name(s.to_string())
    }
}

impl From<u64> for AssetPath {
    fn from(v: u64) -> Self {
        AssetPath::Id(v)
    }
}

#[derive(Debug)]
pub enum SourceError {
    NotFound(AssetPath),
    /// `get_asset::<T>` succeeded but the asset was not a `T`.
    WrongType { requested: &'static str },
    Io(std::io::Error),
    Other(String),
}

impl std::fmt::Display for SourceError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            SourceError::NotFound(p) => write!(f, "asset not found: {p:?}"),
            SourceError::WrongType { requested } => {
                write!(f, "asset is not a {requested}")
            }
            SourceError::Io(e) => write!(f, "{e}"),
            SourceError::Other(s) => write!(f, "{s}"),
        }
    }
}

impl std::error::Error for SourceError {}

impl From<std::io::Error> for SourceError {
    fn from(e: std::io::Error) -> Self {
        SourceError::Io(e)
    }
}

/// C# `ISource`. Object-safe, so `dyn Source` works.
pub trait Source: Send + Sync {
    fn get_asset_any<'a>(
        &'a self,
        path: &'a AssetPath,
        option: Option<&'a (dyn Any + Sync)>,
    ) -> BoxFuture<'a, Result<Box<dyn Any + Send>, SourceError>>;
}

/// The generic front door — C# `GetAsset<T>`. Blanket-implemented, so every
/// `Source` gets it and it stays out of the object-safe trait.
pub trait SourceExt: Source {
    fn get_asset<'a, T: Any + Send>(
        &'a self,
        path: &'a AssetPath,
        option: Option<&'a (dyn Any + Sync)>,
    ) -> BoxFuture<'a, Result<T, SourceError>> {
        Box::pin(async move {
            let any = self.get_asset_any(path, option).await?;
            any.downcast::<T>()
                .map(|b| *b)
                .map_err(|_| SourceError::WrongType { requested: std::any::type_name::<T>() })
        })
    }
}

impl<T: Source + ?Sized> SourceExt for T {}

/// C# `IHaveSource { ISource Source { get; } }`.
pub trait HaveSource {
    fn source(&self) -> &dyn Source;
}
