// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/Polyfill.cs
// PORT-SHA: 348ffb11dac35500
// PORT-STATUS: done
//
// The on-disk header structs shared across formats. C# marks these
// `[StructLayout(LayoutKind.Sequential)]` so they can be blitted; `#[repr(C)]`
// is the exact equivalent. They stay POD and `Copy`, as in C#.
//
// Note these are *little-endian on disk* in every current caller. Reading them
// via `BinaryReaderExt` field-by-field (rather than transmuting) keeps that
// explicit and portable to big-endian hosts.

/// C# `X_LumpON` — offset then count.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct XLumpON {
    pub offset: i32,
    pub num: i32,
}

/// C# `X_LumpNO` — count then offset.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct XLumpNO {
    pub num: i32,
    pub offset: i32,
}

/// C# `X_LumpNO2` — count then two offsets.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct XLumpNO2 {
    pub num: i32,
    pub offset: i32,
    pub offset2: i32,
}

/// C# `X_Lump2NO`.
///
/// Field-for-field identical to [`XLumpNO2`] in the C# source; both are kept so
/// the two trees stay diffable, but if that turns out to be a copy-paste slip
/// upstream, collapse them in both trees at once.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct XLump2NO {
    pub num: i32,
    pub offset: i32,
    pub offset2: i32,
}

/// C# `X_BoundBox` — axis-aligned min/max.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq)]
pub struct XBoundBox {
    /// Minimum values of X, Y, Z.
    pub min: [f32; 3],
    /// Maximum values of X, Y, Z.
    pub max: [f32; 3],
}
