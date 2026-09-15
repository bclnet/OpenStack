// PORT-SOURCE: Core/OpenStack.PolyIO/ISource.cs
// PORT-SHA: 4bb05d80da4f6254
// PORT-STATUS: done

use std::any::Any;
use std::future::Future;
use std::pin::Pin;
use std::io::{Error, ErrorKind};

/// Boxed future alias — `Task<T>` with no external futures crate.
pub type BoxFuture<'a, T> = Pin<Box<dyn Future<Output = T> + Send + 'a>>;

#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub enum SourcePath { Str(String), Int(u64) }
impl From<String> for SourcePath {
    fn from(s: String) -> Self { SourcePath::Str(s) }
}
impl From<&str> for SourcePath {
    fn from(s: &str) -> Self { SourcePath::Str(s.to_string()) }
}
impl From<u64> for SourcePath {
    fn from(v: u64) -> Self { SourcePath::Int(v) }
}

#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub enum SourceTag { Str(String), Int(u64) }
impl From<String> for SourceTag {
    fn from(s: String) -> Self { SourceTag::Str(s) }
}
impl From<&str> for SourceTag {
    fn from(s: &str) -> Self { SourceTag::Str(s.to_string()) }
}
impl From<u64> for SourceTag {
    fn from(v: u64) -> Self { SourceTag::Int(v) }
}

pub trait ISource: Send + Sync {
    fn getAssetAny<'a>(&'a self, path: &'a SourcePath, option: Option<&'a (dyn Any + Sync)>) -> BoxFuture<'a, Result<Box<dyn Any + Send>, Error>>;
}

/// The generic front door — C# `GetAsset<T>`. Blanket-implemented, so every
/// `Source` gets it and it stays out of the object-safe trait.
pub trait ISourceExt: ISource {
    fn getAsset<'a, T: Any + Send>(&'a self, path: &'a SourcePath, option: Option<&'a (dyn Any + Sync)>) -> BoxFuture<'a, Result<T, Error>> {
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
