// PORT-SOURCE: Core/OpenStack.Polyfills/Poly2.1/Half.cs
// PORT-SHA: 3d6d7a7948fe44ca
// PORT-STATUS: done
//
// 360 live lines (plus 240 commented) reimplementing IEEE 754 binary16 for
// netstandard2.1, which predates .NET's built-in `System.Half`. That is a
// polyfill for a missing BCL type — precisely the kind of file that has no
// reason to exist in Rust.
//
// Re-exports the same `half::f16` that `openstack-polyio`'s `system/half_float.rs`
// uses, so the two C# half types (`Half` here, `HalfFloat` there — a genuine
// duplication on the C# side) converge on one Rust type instead of two.
//
// If the C# ever consolidates those, this file disappears with no Rust change.

pub use half::f16 as Half;

pub use openstack_polyio::system::half_float::{
    from_binary_stream, from_f32, from_f32_truncate, to_binary_stream, to_f32, to_f64,
};

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn is_the_same_type_polyio_exposes() {
        // Guards against the two C# half implementations drifting into two
        // different Rust types.
        let v: Half = from_f32(1.5);
        let w: openstack_polyio::system::half_float::HalfFloat = v;
        assert_eq!(to_f32(w), 1.5);
    }
}
