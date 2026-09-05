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
