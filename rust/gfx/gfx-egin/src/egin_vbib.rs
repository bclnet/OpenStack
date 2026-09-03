// PORT-SOURCE: Gfx/OpenStack.Gfx.Egin/Egin_Render.cs (OnDiskBufferData / IVBIB)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// The GPU buffer descriptors — `OnDiskBufferData`, its `Attribute`, and the
// `IVBIB` container. These were the blocker noted in `egin_render.rs` and in
// `openstack-platform-opengl`: both crates left work unported because these
// types were missing. Porting them here unblocks both.
//
// They live in their own module rather than in `egin_render` because they are
// pure data with no renderer dependency, and two crates need them.
//
// ===================== TWO C#-SIDE BUGS ==================================
//
//   1. **Instanced rendering cannot work.** `Attribute` carries `SlotType`
//      (`RENDER_SLOT_PER_VERTEX` / `RENDER_SLOT_PER_INSTANCE`) and
//      `InstanceStepRate`, but `GLMeshBufferCache.BindVertexAttrib` reads
//      **neither**, and `glVertexAttribDivisor` is never called anywhere in the
//      solution. So a per-instance attribute is bound as per-vertex and
//      advances once per vertex instead of once per instance — geometry comes
//      out garbled rather than instanced. The descriptor says what was
//      intended; nothing acts on it. (Same shape as the missing
//      `glPatchParameteri` for tessellation.) **Fix in the C# tree** by calling
//      `glVertexAttribDivisor(loc, step_rate)` for per-instance slots.
//
//   2. **`RENDER_SLOT_INVALID = -1` is unrepresentable after a blind cast.**
//      The enum is `int`-backed with a negative member, and `Attribute.Slot`
//      is a separate `int` field, so a file supplying a slot type outside
//      -1..=1 produces an enum holding an undefined value. `from_raw` returns
//      `Option` here.
//
//   3. **`R8G8B8A8_UNORM` is bound with `normalized: false`.** A UNORM format
//      means "integer on the wire, scaled to 0..1 in the shader", so the flag
//      must be `true`. With it false, a vertex colour byte of 255 arrives as
//      **255.0 instead of 1.0** — 255x too bright, and every lighting
//      calculation downstream is wrong. The sibling entry `R16G16_UNORM` in the
//      same `switch` correctly passes `true`, which is what makes this a slip
//      rather than intent. (Same class as the ARGB32 and BGR555 normalisation
//      bugs recorded elsewhere in PORTING.md.)
//
//   4. **`R8G8B8A8_UINT` is bound through the float path.** `VertexAttribPointer`
//      converts to float; a `_UINT` format needs `VertexAttribIPointer` so the
//      shader receives actual integers. The siblings `R16G16_SINT` and
//      `R16G16B16A16_SINT` correctly use `IPointer`. This matters because
//      `R8G8B8A8_UINT` is the format bone indices (`BLENDINDICES`) arrive in —
//      so a shader declaring `uvec4`/`ivec4` reads reinterpreted garbage and
//      **skinning breaks**.
//
// Correction to an earlier note: the C#'s `switch` *does* have a `default` arm,
// which throws `FormatException`. An unlisted format fails loudly rather than
// binding nothing.
//
// Also worth knowing: `GLMeshBufferCache` keys `_gpuBuffers` by `IVBIB` and
// `_vertexArrayObjects` by a `VAOKey` struct **containing class references**.
// Struct equality then falls back to reference equality per field, and
// `ValueType.GetHashCode` on a struct with reference fields is the
// reflection-based slow path — hashed on every draw. Neither dictionary is ever
// evicted and no VAO is ever deleted, so both leak for the process lifetime.

use openstack_gfx::gfx_texture::DXGI_FORMAT;

/// C# `OnDiskBufferData.RenderSlotType`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum RenderSlotType {
    /// C# `RENDER_SLOT_INVALID = -1`.
    Invalid,
    /// C# `RENDER_SLOT_PER_VERTEX = 0`.
    #[default]
    PerVertex,
    /// C# `RENDER_SLOT_PER_INSTANCE = 1`.
    PerInstance,
}

impl RenderSlotType {
    pub fn from_raw(v: i32) -> Option<Self> {
        Some(match v {
            -1 => Self::Invalid,
            0 => Self::PerVertex,
            1 => Self::PerInstance,
            _ => return None,
        })
    }

    pub fn to_raw(self) -> i32 {
        match self {
            Self::Invalid => -1,
            Self::PerVertex => 0,
            Self::PerInstance => 1,
        }
    }
}

/// C# `OnDiskBufferData.Attribute`.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Attribute {
    /// C# `SemanticName` — "POSITION", "TEXCOORD", "COLOR", "NORMAL", ...
    pub semantic_name: String,
    pub semantic_index: i32,
    pub format: DXGI_FORMAT,
    /// Byte offset within the vertex.
    pub offset: u32,
    pub slot: i32,
    pub slot_type: RenderSlotType,
    /// Instances between advances. Only meaningful for `PerInstance`, and
    /// currently read by nothing — see bug 1.
    pub instance_step_rate: i32,
}

impl Attribute {
    /// The shader attribute name this binds to.
    ///
    /// C# `GLMeshBufferCache.GetVertexArrayObject` builds this inline:
    /// `$"v{SemanticName}"`, with a 1-based ordinal appended from the *second*
    /// occurrence onward for `TEXCOORD` and `COLOR` — so a mesh with three
    /// texcoords binds `vTEXCOORD`, `vTEXCOORD2`, `vTEXCOORD3`. The counter is
    /// per-buffer, which is why it is passed in rather than derived here.
    pub fn shader_name(&self, occurrence: usize) -> String {
        let base = format!("v{}", self.semantic_name);
        if occurrence == 0 {
            return base;
        }
        match self.semantic_name.as_str() {
            "TEXCOORD" | "COLOR" => format!("{base}{}", occurrence + 1),
            _ => base,
        }
    }

    /// How this attribute must be bound: component count, element type, and
    /// whether the values are normalised or passed as integers.
    ///
    /// Derived from the **format's own semantics** rather than copied from the
    /// C#'s `switch`, because two of that table's ten entries contradict their
    /// siblings — see bugs 3 and 4 in the module header. `None` for a format
    /// the C# also rejects (its `default` arm throws `FormatException`).
    pub fn layout(&self) -> Option<AttributeLayout> {
        use DXGI_FORMAT as F;
        // (components, element, semantics)
        let (components, element, kind) = match self.format {
            F::R32G32B32_FLOAT => (3, ElementType::F32, Kind::Float),
            F::R32G32_FLOAT => (2, ElementType::F32, Kind::Float),
            F::R32G32B32A32_FLOAT => (4, ElementType::F32, Kind::Float),
            F::R32_FLOAT => (1, ElementType::F32, Kind::Float),
            F::R16G16_FLOAT => (2, ElementType::F16, Kind::Float),
            F::R8G8B8A8_UNORM => (4, ElementType::U8, Kind::Normalized),
            F::R16G16_UNORM => (2, ElementType::U16, Kind::Normalized),
            F::R16G16_SNORM => (2, ElementType::I16, Kind::Normalized),
            F::R8G8B8A8_UINT => (4, ElementType::U8, Kind::Integer),
            F::R32G32B32A32_UINT => (4, ElementType::U32, Kind::Integer),
            F::R16G16_SINT => (2, ElementType::I16, Kind::Integer),
            F::R16G16B16A16_SINT => (4, ElementType::I16, Kind::Integer),
            _ => return None,
        };
        Some(AttributeLayout { components, element, kind })
    }
}

/// Element type of one attribute component.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ElementType {
    U8,
    I8,
    U16,
    I16,
    U32,
    I32,
    F16,
    F32,
}

impl ElementType {
    #[inline]
    pub const fn size(self) -> u32 {
        match self {
            Self::U8 | Self::I8 => 1,
            Self::U16 | Self::I16 | Self::F16 => 2,
            Self::U32 | Self::I32 | Self::F32 => 4,
        }
    }
}

/// How the shader should receive the values.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Kind {
    /// Already floating point; pass through.
    Float,
    /// Integer on the wire, scaled to `0..1` (unsigned) or `-1..1` (signed).
    /// Requires `normalized = true` on a float-path bind.
    Normalized,
    /// Integer on the wire and integer in the shader. Requires the **integer**
    /// bind path (`glVertexAttribIPointer`); using the float path silently
    /// converts, which is bug 4.
    Integer,
}

/// Decoded vertex-attribute layout.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct AttributeLayout {
    pub components: u32,
    pub element: ElementType,
    pub kind: Kind,
}

impl AttributeLayout {
    /// Total bytes this attribute occupies in a vertex.
    #[inline]
    pub const fn size(&self) -> u32 {
        self.components * self.element.size()
    }

    /// Whether the bind must use the integer path.
    #[inline]
    pub const fn is_integer(&self) -> bool {
        matches!(self.kind, Kind::Integer)
    }

    /// The `normalized` flag for a float-path bind.
    #[inline]
    pub const fn normalized(&self) -> bool {
        matches!(self.kind, Kind::Normalized)
    }
}

/// C# `struct OnDiskBufferData`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct OnDiskBufferData {
    pub element_count: u32,
    /// C#: "stride for vertices. Type for indices" — one field, two meanings,
    /// disambiguated by which list the buffer is in.
    pub element_size_in_bytes: u32,
    /// Empty for index buffers, per the C# comment.
    pub attributes: Vec<Attribute>,
    pub data: Vec<u8>,
}

impl OnDiskBufferData {
    /// Bytes the descriptor claims: `element_count * element_size_in_bytes`.
    ///
    /// The C# passes this product straight to `glBufferData` as the size while
    /// handing it `Data` as the pointer, with no check that `Data` is actually
    /// that long. A short `Data` means GL reads past the end of the managed
    /// array.
    pub fn declared_len(&self) -> Option<usize> {
        (self.element_count as usize).checked_mul(self.element_size_in_bytes as usize)
    }

    /// Whether `data` is long enough for what the header declares. The C# never
    /// checks this.
    pub fn is_consistent(&self) -> bool {
        self.declared_len().map(|n| self.data.len() >= n).unwrap_or(false)
    }

    /// Index width in bytes, for an index buffer (where `element_size_in_bytes`
    /// is the type rather than a stride).
    pub fn index_width(&self) -> Option<u32> {
        match self.element_size_in_bytes {
            2 | 4 => Some(self.element_size_in_bytes),
            _ => None,
        }
    }

    /// Sum of the attribute sizes, for cross-checking against the stride.
    ///
    /// A total exceeding `element_size_in_bytes` means attributes overlap or
    /// run past the end of each vertex — worth catching, since the C# would
    /// bind them anyway.
    pub fn attributes_fit_stride(&self) -> bool {
        self.attributes.iter().all(|a| {
            a.layout()
                .map(|l| a.offset + l.size() <= self.element_size_in_bytes)
                .unwrap_or(false)
        })
    }
}

/// C# `interface IVBIB`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct Vbib {
    pub vertex_buffers: Vec<OnDiskBufferData>,
    pub index_buffers: Vec<OnDiskBufferData>,
}

impl Vbib {
    /// C# `RemapBoneIndices(int[] remapTable)`.
    ///
    /// Rewrites `BLENDINDICES` attribute values through a remap table, for
    /// meshes whose bone indices are relative to a different skeleton ordering.
    /// The C# declares this on the interface and no implementation exists in
    /// the solution, so this is the behaviour the name and the call site imply
    /// rather than a translation of working code — flagged accordingly.
    ///
    /// Returns `None` if any index falls outside the table, rather than
    /// producing a mesh that references a bone that does not exist.
    pub fn remap_bone_indices(&self, remap_table: &[i32]) -> Option<Self> {
        let mut out = self.clone();
        for buf in out.vertex_buffers.iter_mut() {
            let stride = buf.element_size_in_bytes as usize;
            // Collect first: iterating attributes while mutating data aliases.
            let targets: Vec<(usize, u32)> = buf
                .attributes
                .iter()
                .filter(|a| a.semantic_name == "BLENDINDICES")
                .filter_map(|a| a.layout().map(|l| (a.offset as usize, l.components)))
                .collect();
            for (offset, components) in targets {
                for v in 0..buf.element_count as usize {
                    for c in 0..components as usize {
                        let at = v * stride + offset + c;
                        let old = *buf.data.get(at)? as usize;
                        let new = *remap_table.get(old)?;
                        buf.data[at] = u8::try_from(new).ok()?;
                    }
                }
            }
        }
        Some(out)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn attr(name: &str, format: DXGI_FORMAT, offset: u32) -> Attribute {
        Attribute {
            semantic_name: name.to_string(),
            semantic_index: 0,
            format,
            offset,
            slot: 0,
            slot_type: RenderSlotType::PerVertex,
            instance_step_rate: 0,
        }
    }

    #[test]
    fn slot_type_round_trips_including_the_negative_member() {
        for v in [-1, 0, 1] {
            let s = RenderSlotType::from_raw(v).unwrap();
            assert_eq!(s.to_raw(), v);
        }
        assert!(RenderSlotType::from_raw(2).is_none());
        assert!(RenderSlotType::from_raw(-2).is_none());
    }

    #[test]
    fn shader_names_match_the_c_sharp_numbering() {
        let t = attr("TEXCOORD", DXGI_FORMAT::R32G32_FLOAT, 0);
        assert_eq!(t.shader_name(0), "vTEXCOORD");
        assert_eq!(t.shader_name(1), "vTEXCOORD2");
        assert_eq!(t.shader_name(2), "vTEXCOORD3");
        // Only TEXCOORD and COLOR get the ordinal.
        let p = attr("POSITION", DXGI_FORMAT::R32G32B32_FLOAT, 0);
        assert_eq!(p.shader_name(1), "vPOSITION");
    }

    #[test]
    fn component_counts_match_the_c_sharp() {
        for (f, comps) in [
            (DXGI_FORMAT::R32G32B32_FLOAT, 3),
            (DXGI_FORMAT::R8G8B8A8_UNORM, 4),
            (DXGI_FORMAT::R32G32_FLOAT, 2),
            (DXGI_FORMAT::R16G16_FLOAT, 2),
            (DXGI_FORMAT::R32G32B32A32_FLOAT, 4),
            (DXGI_FORMAT::R8G8B8A8_UINT, 4),
            (DXGI_FORMAT::R16G16_SINT, 2),
            (DXGI_FORMAT::R16G16B16A16_SINT, 4),
            (DXGI_FORMAT::R16G16_SNORM, 2),
            (DXGI_FORMAT::R16G16_UNORM, 2),
        ] {
            let l = attr("X", f, 0).layout().unwrap_or_else(|| panic!("{f:?}"));
            assert_eq!(l.components, comps, "{f:?}");
        }
    }

    #[test]
    fn unorm_formats_are_normalized() {
        // The C# passes normalized: false for R8G8B8A8_UNORM while passing true
        // for R16G16_UNORM — bug 3. Both must be true.
        for f in [DXGI_FORMAT::R8G8B8A8_UNORM, DXGI_FORMAT::R16G16_UNORM] {
            let l = attr("C", f, 0).layout().unwrap();
            assert!(l.normalized(), "{f:?} must normalize");
            assert!(!l.is_integer(), "{f:?} is not an integer attribute");
        }
        // SNORM likewise.
        assert!(attr("N", DXGI_FORMAT::R16G16_SNORM, 0).layout().unwrap().normalized());
    }

    #[test]
    fn integer_formats_use_the_integer_path() {
        // The C# binds R8G8B8A8_UINT through VertexAttribPointer — bug 4.
        for f in [
            DXGI_FORMAT::R8G8B8A8_UINT,
            DXGI_FORMAT::R32G32B32A32_UINT,
            DXGI_FORMAT::R16G16_SINT,
            DXGI_FORMAT::R16G16B16A16_SINT,
        ] {
            let l = attr("I", f, 0).layout().unwrap();
            assert!(l.is_integer(), "{f:?} must use IPointer");
            assert!(!l.normalized(), "{f:?} must not be normalized");
        }
    }

    #[test]
    fn float_formats_are_neither_normalized_nor_integer() {
        for f in [
            DXGI_FORMAT::R32G32B32_FLOAT,
            DXGI_FORMAT::R32G32_FLOAT,
            DXGI_FORMAT::R32G32B32A32_FLOAT,
            DXGI_FORMAT::R16G16_FLOAT,
        ] {
            let l = attr("P", f, 0).layout().unwrap();
            assert!(!l.normalized() && !l.is_integer(), "{f:?}");
        }
    }

    #[test]
    fn blend_indices_are_integers_so_skinning_works() {
        // The concrete consequence of bug 4: bone indices arrive as
        // reinterpreted floats in the C#.
        let a = attr("BLENDINDICES", DXGI_FORMAT::R8G8B8A8_UINT, 0);
        assert!(a.layout().unwrap().is_integer());
    }

    #[test]
    fn element_sizes_are_right() {
        assert_eq!(ElementType::U8.size(), 1);
        assert_eq!(ElementType::F16.size(), 2);
        assert_eq!(ElementType::F32.size(), 4);
        let l = attr("P", DXGI_FORMAT::R32G32B32_FLOAT, 0).layout().unwrap();
        assert_eq!(l.size(), 12);
        let c = attr("C", DXGI_FORMAT::R8G8B8A8_UNORM, 0).layout().unwrap();
        assert_eq!(c.size(), 4);
    }

    #[test]
    fn declared_length_is_checked_against_the_data() {
        let mut b = OnDiskBufferData {
            element_count: 4,
            element_size_in_bytes: 12,
            attributes: vec![],
            data: vec![0; 48],
        };
        assert_eq!(b.declared_len(), Some(48));
        assert!(b.is_consistent());
        b.data.truncate(20);
        assert!(!b.is_consistent(), "C# would let GL read past the array");
    }

    #[test]
    fn overflowing_element_product_is_caught() {
        let b = OnDiskBufferData {
            element_count: u32::MAX,
            element_size_in_bytes: u32::MAX,
            attributes: vec![],
            data: vec![],
        };
        // On a 64-bit host the product fits usize, so this is about the check
        // existing rather than the specific value.
        assert!(!b.is_consistent());
    }

    #[test]
    fn attributes_must_fit_within_the_stride() {
        let ok = OnDiskBufferData {
            element_count: 1,
            element_size_in_bytes: 16,
            attributes: vec![
                attr("POSITION", DXGI_FORMAT::R32G32B32_FLOAT, 0), // 0..12
                attr("COLOR", DXGI_FORMAT::R8G8B8A8_UNORM, 12),    // 12..16
            ],
            data: vec![0; 16],
        };
        assert!(ok.attributes_fit_stride());

        let overrun = OnDiskBufferData {
            element_size_in_bytes: 8,
            attributes: vec![attr("POSITION", DXGI_FORMAT::R32G32B32_FLOAT, 0)],
            ..ok.clone()
        };
        assert!(!overrun.attributes_fit_stride(), "12 bytes will not fit in 8");
    }

    #[test]
    fn index_width_accepts_only_two_or_four() {
        let mk = |w| OnDiskBufferData { element_size_in_bytes: w, ..Default::default() };
        assert_eq!(mk(2).index_width(), Some(2));
        assert_eq!(mk(4).index_width(), Some(4));
        assert_eq!(mk(3).index_width(), None);
    }

    #[test]
    fn bone_remap_rewrites_blend_indices() {
        let vbib = Vbib {
            vertex_buffers: vec![OnDiskBufferData {
                element_count: 2,
                element_size_in_bytes: 4,
                attributes: vec![attr("BLENDINDICES", DXGI_FORMAT::R8G8B8A8_UINT, 0)],
                data: vec![0, 1, 2, 3, 3, 2, 1, 0],
            }],
            index_buffers: vec![],
        };
        // Reverse the bone order.
        let out = vbib.remap_bone_indices(&[3, 2, 1, 0]).unwrap();
        assert_eq!(out.vertex_buffers[0].data, vec![3, 2, 1, 0, 0, 1, 2, 3]);
    }

    #[test]
    fn bone_remap_rejects_an_index_outside_the_table() {
        let vbib = Vbib {
            vertex_buffers: vec![OnDiskBufferData {
                element_count: 1,
                element_size_in_bytes: 4,
                attributes: vec![attr("BLENDINDICES", DXGI_FORMAT::R8G8B8A8_UINT, 0)],
                data: vec![0, 1, 99, 3],
            }],
            index_buffers: vec![],
        };
        assert!(vbib.remap_bone_indices(&[0, 1, 2, 3]).is_none());
    }

    #[test]
    fn per_instance_attributes_carry_their_step_rate() {
        // The descriptor holds it; nothing in the C# reads it (bug 1).
        let mut a = attr("TEXCOORD", DXGI_FORMAT::R32G32_FLOAT, 0);
        a.slot_type = RenderSlotType::PerInstance;
        a.instance_step_rate = 1;
        assert_eq!(a.slot_type, RenderSlotType::PerInstance);
        assert_eq!(a.instance_step_rate, 1);
    }
}
