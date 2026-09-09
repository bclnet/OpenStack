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
use std::io::{Error, ErrorKind};

/// Boxed future alias — `Task<T>` with no external futures crate.
pub type BoxFuture<'a, T> = Pin<Box<dyn Future<Output = T> + Send + 'a>>;

/// The shapes passed as `object path` in the C# tree.
#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub enum AssetPath {
    Str(String),
    Int(u64),
}

impl From<String> for AssetPath {
    fn from(s: String) -> Self { AssetPath::Str(s) }
}

impl From<&str> for AssetPath {
    fn from(s: &str) -> Self { AssetPath::Str(s.to_string()) }
}

impl From<u64> for AssetPath {
    fn from(v: u64) -> Self { AssetPath::Int(v) }
}

/// C# `ISource`. Object-safe, so `dyn ISource` works.
pub trait ISource: Send + Sync {
    fn getAssetAny<'a>(&'a self, path: &'a AssetPath, option: Option<&'a (dyn Any + Sync)>) -> BoxFuture<'a, Result<Box<dyn Any + Send>, Error>>;
}

/// The generic front door — C# `GetAsset<T>`. Blanket-implemented, so every
/// `Source` gets it and it stays out of the object-safe trait.
pub trait ISourceExt: ISource {
    fn getAsset<'a, T: Any + Send>(&'a self, path: &'a AssetPath, option: Option<&'a (dyn Any + Sync)>) -> BoxFuture<'a, Result<T, Error>> {
        Box::pin(async move {
            let any = self.getAssetAny(path, option).await?;
            any.downcast::<T>().map(|b| *b).map_err(|_| Error::new(ErrorKind::Other, std::any::type_name::<T>()))
        })
    }
}

impl<T: ISource + ?Sized> ISourceExt for T {}

pub trait IHaveSource {
    fn source(&self) -> &dyn ISource;
}
