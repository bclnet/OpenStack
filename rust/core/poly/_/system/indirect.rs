// PORT-SOURCE: Core/OpenStack.Polyfills/System/Indirect.cs
// PORT-SHA: 528011e587539996
// PORT-STATUS: done

/// C# `interface Indirect<T> { T Value { get; } }` — a read-only box.
///
/// `std::ops::Deref` is the idiomatic Rust spelling, but this keeps the C#
/// shape so ported call sites read the same. Implement whichever fits.
pub trait Indirect<T> {
    fn value(&self) -> &T;
}

impl<T> Indirect<T> for T {
    fn value(&self) -> &T {
        self
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn any_value_is_its_own_indirection() {
        assert_eq!(*Indirect::<i32>::value(&42), 42);
    }
}
