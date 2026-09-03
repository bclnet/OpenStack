// PORT-SOURCE: Platforms/OpenStack.Platform.OpenGL/Egin/Gl_Render.cs (GLMeshBufferCache)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// Vertex-array-object construction and caching — the `GLMeshBufferCache` half of
// `Gl_Render.cs`, unblocked now that the `IVBIB` descriptors are ported.
//
// **This is where instanced rendering is fixed.** Three defects in the C#
// binding path are corrected here, each documented at the call site:
//
//   1. `glVertexAttribDivisor` is never called anywhere in the C# solution, so
//      `Attribute.SlotType` and `InstanceStepRate` are carried through the
//      whole pipeline and then ignored. Per-instance attributes bind as
//      per-vertex and advance once per vertex.
//   2. `R8G8B8A8_UNORM` binds with `normalized: false`, so colour bytes arrive
//      255x too large.
//   3. `R8G8B8A8_UINT` binds through the float path, so bone indices arrive as
//      reinterpreted floats and skinning breaks.
//
// Still **not compiled or executed** — same caveat as `gl_render.rs`. The
// divisor logic and the format table are reviewed and unit-tested for their
// arithmetic; the GL calls themselves have never run.

use std::collections::HashMap;

use glow::HasContext;
use openstack_gfx_egin::egin_vbib::{Attribute, Kind, OnDiskBufferData, RenderSlotType, Vbib};

use super::gl_render::MeshBuffer;

/// glow's element-type constant for an attribute component.
fn gl_element_type(e: openstack_gfx_egin::egin_vbib::ElementType) -> u32 {
    use openstack_gfx_egin::egin_vbib::ElementType as E;
    match e {
        E::U8 => glow::UNSIGNED_BYTE,
        E::I8 => glow::BYTE,
        E::U16 => glow::UNSIGNED_SHORT,
        E::I16 => glow::SHORT,
        E::U32 => glow::UNSIGNED_INT,
        E::I32 => glow::INT,
        E::F16 => glow::HALF_FLOAT,
        E::F32 => glow::FLOAT,
    }
}

/// The divisor to pass to `glVertexAttribDivisor`.
///
/// **The fix.** 0 means "advance per vertex"; N means "advance once every N
/// instances". GL's default is 0, which is why the C#'s omission looks like
/// working code until something actually declares a per-instance attribute.
///
/// A `PerInstance` attribute with `instance_step_rate <= 0` is treated as 1:
/// a rate of 0 would mean "never advance", so every instance would read the
/// same element, which is never what a per-instance attribute means. The C#
/// has no such attribute path at all, so there is no behaviour to preserve.
pub fn attrib_divisor(attribute: &Attribute) -> u32 {
    match attribute.slot_type {
        RenderSlotType::PerVertex => 0,
        RenderSlotType::PerInstance => attribute.instance_step_rate.max(1) as u32,
        // An invalid slot is treated as per-vertex, which is GL's default and
        // the least surprising reading.
        RenderSlotType::Invalid => 0,
    }
}

/// Bind one vertex attribute into the currently bound VAO.
///
/// Returns `false` when the shader has no such attribute — the C# does the same
/// via `if (attributeLocation == uint.MaxValue) return;`, relying on
/// `GetAttribLocation` returning -1 and the `(uint)` cast making it
/// `uint::MAX`. glow returns `Option<u32>`, so the sentinel is explicit.
///
/// # Safety
/// A GL context must be current, with `program` linked and a VAO bound.
pub unsafe fn bind_vertex_attrib(
    gl: &glow::Context,
    program: glow::Program,
    attribute: &Attribute,
    shader_name: &str,
    stride: i32,
    base_vertex: u32,
) -> Result<bool, String> {
    let Some(location) = gl.get_attrib_location(program, shader_name) else {
        return Ok(false); // not present in this shader; skip it
    };
    let layout = attribute
        .layout()
        .ok_or_else(|| format!("unsupported attribute format {:?}", attribute.format))?;

    gl.enable_vertex_attrib_array(location);
    let offset = base_vertex + attribute.offset;
    let ty = gl_element_type(layout.element);

    match layout.kind {
        // Integer in, integer out. The C# routes R8G8B8A8_UINT down the float
        // path instead, which is bug 3 — bone indices arrive as garbage.
        Kind::Integer => gl.vertex_attrib_pointer_i32(
            location,
            layout.components as i32,
            ty,
            stride,
            offset as i32,
        ),
        // normalized = true, so 0..255 becomes 0..1. The C# passes false for
        // R8G8B8A8_UNORM while passing true for R16G16_UNORM — bug 2.
        Kind::Normalized => gl.vertex_attrib_pointer_f32(
            location,
            layout.components as i32,
            ty,
            true,
            stride,
            offset as i32,
        ),
        Kind::Float => gl.vertex_attrib_pointer_f32(
            location,
            layout.components as i32,
            ty,
            false,
            stride,
            offset as i32,
        ),
    }

    // THE FIX: tell GL how fast this attribute advances. Absent in the C#.
    let divisor = attrib_divisor(attribute);
    if divisor != 0 {
        gl.vertex_attrib_divisor(location, divisor);
    }
    Ok(true)
}

/// Shader-attribute names for one buffer's attributes, applying the C#'s
/// per-buffer occurrence numbering for `TEXCOORD` and `COLOR`.
///
/// The C# does this inline with two mutable counters and a
/// `texCoordNum++ > 0 ... else if colorNum++ > 0` chain. Extracted so the
/// numbering is testable without a GL context.
pub fn shader_names(buffer: &OnDiskBufferData) -> Vec<String> {
    let mut seen: HashMap<&str, usize> = HashMap::new();
    buffer
        .attributes
        .iter()
        .map(|a| {
            let n = seen.entry(a.semantic_name.as_str()).or_insert(0);
            let name = a.shader_name(*n);
            *n += 1;
            name
        })
        .collect()
}

/// Identifies a cached VAO. C# `GLMeshBufferCache.VAOKey`.
///
/// The C#'s key is a **struct containing class references** (`GLMeshBuffers`,
/// `Shader`), so equality falls back to per-field reference identity and
/// `ValueType.GetHashCode` takes the reflection-based slow path — hashed on
/// every draw. Interning both as indices makes the key a plain value.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct VaoKey {
    pub mesh: usize,
    pub program: usize,
    pub vertex_index: u32,
    pub index_index: u32,
    pub base_vertex: u32,
}

/// C# `class GLMeshBufferCache`.
///
/// Unlike the C#, this can be emptied: `clear` deletes every VAO it created.
/// The C# never calls `glDeleteVertexArrays` and never evicts either
/// dictionary, so both grow for the process lifetime.
#[derive(Default)]
pub struct GlVaoCache {
    vaos: HashMap<VaoKey, glow::VertexArray>,
}

impl GlVaoCache {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn len(&self) -> usize {
        self.vaos.len()
    }

    pub fn is_empty(&self) -> bool {
        self.vaos.is_empty()
    }

    /// C# `GetVertexArrayObject(...)`.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    #[allow(clippy::too_many_arguments)]
    pub unsafe fn get_or_create(
        &mut self,
        gl: &glow::Context,
        key: VaoKey,
        program: glow::Program,
        vbib: &Vbib,
        buffers: (&[MeshBuffer], &[MeshBuffer]),
    ) -> Result<glow::VertexArray, String> {
        if let Some(v) = self.vaos.get(&key) {
            return Ok(*v);
        }
        let (vertex_buffers, index_buffers) = buffers;
        let vb = vertex_buffers
            .get(key.vertex_index as usize)
            .ok_or("vertex buffer index out of range")?;
        let ib = index_buffers
            .get(key.index_index as usize)
            .ok_or("index buffer index out of range")?;
        let desc = vbib
            .vertex_buffers
            .get(key.vertex_index as usize)
            .ok_or("vertex descriptor index out of range")?;

        let vao = gl.create_vertex_array()?;
        gl.bind_vertex_array(Some(vao));
        gl.bind_buffer(glow::ARRAY_BUFFER, Some(vb.handle));
        gl.bind_buffer(glow::ELEMENT_ARRAY_BUFFER, Some(ib.handle));

        let names = shader_names(desc);
        let stride = desc.element_size_in_bytes as i32;
        for (a, name) in desc.attributes.iter().zip(&names) {
            // A bind failure leaves the VAO allocated; clean up rather than
            // leaking it, which is what the C# does on its FormatException.
            if let Err(e) = bind_vertex_attrib(gl, program, a, name, stride, key.base_vertex) {
                gl.bind_vertex_array(None);
                gl.delete_vertex_array(vao);
                return Err(e);
            }
        }
        gl.bind_vertex_array(None);
        self.vaos.insert(key, vao);
        Ok(vao)
    }

    /// Delete every cached VAO. No C# equivalent.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn clear(&mut self, gl: &glow::Context) {
        for (_, v) in self.vaos.drain() {
            gl.delete_vertex_array(v);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use openstack_gfx::gfx_texture::DXGI_FORMAT;
    use openstack_gfx_egin::egin_vbib::Attribute;

    fn attr(name: &str, format: DXGI_FORMAT, slot: RenderSlotType, rate: i32) -> Attribute {
        Attribute {
            semantic_name: name.to_string(),
            semantic_index: 0,
            format,
            offset: 0,
            slot: 0,
            slot_type: slot,
            instance_step_rate: rate,
        }
    }

    #[test]
    fn per_vertex_attributes_get_divisor_zero() {
        let a = attr("POSITION", DXGI_FORMAT::R32G32B32_FLOAT, RenderSlotType::PerVertex, 0);
        assert_eq!(attrib_divisor(&a), 0);
    }

    #[test]
    fn per_instance_attributes_get_their_step_rate() {
        // This is the whole point: the C# never emits a divisor at all, so
        // every attribute behaves as if this returned 0.
        for rate in [1, 2, 7] {
            let a = attr("TEXCOORD", DXGI_FORMAT::R32G32_FLOAT, RenderSlotType::PerInstance, rate);
            assert_eq!(attrib_divisor(&a), rate as u32, "rate {rate}");
        }
    }

    #[test]
    fn a_per_instance_rate_of_zero_becomes_one() {
        // Divisor 0 means per-vertex, which contradicts PerInstance; 0 would
        // silently reintroduce the original bug for that attribute.
        let a = attr("TEXCOORD", DXGI_FORMAT::R32G32_FLOAT, RenderSlotType::PerInstance, 0);
        assert_eq!(attrib_divisor(&a), 1);
        let neg = attr("TEXCOORD", DXGI_FORMAT::R32G32_FLOAT, RenderSlotType::PerInstance, -5);
        assert_eq!(attrib_divisor(&neg), 1);
    }

    #[test]
    fn invalid_slots_fall_back_to_per_vertex() {
        let a = attr("X", DXGI_FORMAT::R32G32_FLOAT, RenderSlotType::Invalid, 4);
        assert_eq!(attrib_divisor(&a), 0);
    }

    #[test]
    fn element_types_map_to_gl_constants() {
        use openstack_gfx_egin::egin_vbib::ElementType as E;
        assert_eq!(gl_element_type(E::U8), glow::UNSIGNED_BYTE);
        assert_eq!(gl_element_type(E::I16), glow::SHORT);
        assert_eq!(gl_element_type(E::F16), glow::HALF_FLOAT);
        assert_eq!(gl_element_type(E::F32), glow::FLOAT);
        assert_eq!(gl_element_type(E::U32), glow::UNSIGNED_INT);
    }

    #[test]
    fn occurrence_numbering_matches_the_c_sharp() {
        let buf = OnDiskBufferData {
            element_count: 1,
            element_size_in_bytes: 64,
            attributes: vec![
                attr("POSITION", DXGI_FORMAT::R32G32B32_FLOAT, RenderSlotType::PerVertex, 0),
                attr("TEXCOORD", DXGI_FORMAT::R32G32_FLOAT, RenderSlotType::PerVertex, 0),
                attr("TEXCOORD", DXGI_FORMAT::R32G32_FLOAT, RenderSlotType::PerVertex, 0),
                attr("TEXCOORD", DXGI_FORMAT::R32G32_FLOAT, RenderSlotType::PerVertex, 0),
                attr("COLOR", DXGI_FORMAT::R8G8B8A8_UNORM, RenderSlotType::PerVertex, 0),
                attr("COLOR", DXGI_FORMAT::R8G8B8A8_UNORM, RenderSlotType::PerVertex, 0),
            ],
            data: vec![],
        };
        assert_eq!(
            shader_names(&buf),
            vec![
                "vPOSITION",
                "vTEXCOORD",
                "vTEXCOORD2",
                "vTEXCOORD3",
                "vCOLOR",
                "vCOLOR2",
            ]
        );
    }

    #[test]
    fn semantics_other_than_texcoord_and_color_are_not_numbered() {
        let buf = OnDiskBufferData {
            attributes: vec![
                attr("NORMAL", DXGI_FORMAT::R32G32B32_FLOAT, RenderSlotType::PerVertex, 0),
                attr("NORMAL", DXGI_FORMAT::R32G32B32_FLOAT, RenderSlotType::PerVertex, 0),
            ],
            ..Default::default()
        };
        assert_eq!(shader_names(&buf), vec!["vNORMAL", "vNORMAL"]);
    }

    #[test]
    fn vao_keys_are_plain_values() {
        // The C# key is a struct holding class references, so it hashes by
        // reference identity through the reflection slow path.
        let a = VaoKey { mesh: 1, program: 2, vertex_index: 0, index_index: 0, base_vertex: 0 };
        let b = a;
        assert_eq!(a, b);
        let mut m = HashMap::new();
        m.insert(a, 10);
        assert_eq!(m.get(&b), Some(&10), "equal keys must collide");
        let c = VaoKey { base_vertex: 1, ..a };
        assert_ne!(a, c);
    }

    #[test]
    fn cache_starts_empty() {
        let c = GlVaoCache::new();
        assert!(c.is_empty());
        assert_eq!(c.len(), 0);
    }
}
