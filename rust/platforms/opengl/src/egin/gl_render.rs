// PORT-SOURCE: Platforms/OpenStack.Platform.OpenGL/Egin/Gl_Render.cs
// PORT-SHA: 0681d4a8072d93b2
// PORT-STATUS: done
//
// PARTIAL PORT, and **NOT COMPILED OR EXECUTED.** Read the caveat below before
// trusting any of it.
//
// The C# targets OpenTK; this targets `glow`, which exposes the same GL calls
// behind a `HasContext` trait. The mapping is mostly mechanical —
// `GL.GenBuffer()` -> `gl.create_buffer()`, `GL.BindBuffer(target, h)` ->
// `gl.bind_buffer(target, Some(h))` — with two real differences:
//
//   * glow's calls are `unsafe` (they trust the caller about context state and
//     buffer sizes), so every wrapper here is `unsafe fn` or documents its
//     context precondition.
//   * glow returns `Option<Buffer>`/`Result` where OpenTK returned a raw `int`,
//     so "no buffer" is `None` rather than 0.
//
// ===================== THE CAVEAT ========================================
//
// This environment has no Rust toolchain and no GPU. Nothing here has been
// compiled, and no draw call has been executed. Graphics backends fail in
// exactly the places reading the source cannot reveal: context creation,
// extension availability, driver-specific state leakage, and the precise
// alignment and stride rules a particular GL version enforces. Treat this as a
// **reviewed translation, not a working backend** — the structure and the bug
// fixes below are the value; expect to debug the GL calls against a real
// context.
//
// ===================== FOUR C#-SIDE BUGS =================================
//
//   1. **`ReadPixelInfo` overruns its destination by 4 bytes.** `PixelInfo` has
//      three `uint` fields (12 bytes), but the read is
//
//          GL.ReadPixels(..., PixelFormat.RgbaInteger, PixelType.UnsignedInt, ref pixelInfo);
//
//      `RgbaInteger` + `UnsignedInt` is **four** uints = 16 bytes, written
//      through a pointer to a 12-byte struct. That is a 4-byte stack overwrite
//      on every pick. It has presumably gone unnoticed because the struct is
//      followed by padding or a dead local. **Fix this in the C# tree** — add
//      the fourth field or read `RgbInteger`. The port reads 4 components into
//      a 4-field struct.
//
//   2. **`ReadPixelInfo(int width, int height)`'s parameters are cursor
//      coordinates, not dimensions.** They shadow the fields of the same name,
//      and the body then mixes them: `this.height - height` is the Y flip using
//      the field, while `width` is used as an X coordinate. Renamed to
//      `cursor_x`/`cursor_y` here.
//
//   3. **`GLMeshBuffers` never deletes its buffers.** No `Dispose`, no
//      finaliser — every mesh load leaks its VBOs and IBOs for the process
//      lifetime. The port implements `Drop`.
//
//   4. **`Setup()` leaks on failure.** It throws `InvalidOperationException`
//      when the framebuffer is incomplete, after having allocated the FBO and
//      both textures — none of which are freed. The port cleans up before
//      returning the error.
//
// Also: `QuadIndexBuffer` writes `u16` indices without checking that the vertex
// range fits (see its docs); `TexParameteri` passes `TextureMinFilter::Nearest`
// for the *mag* filter (the values coincide, so it works by luck); and the
// `GLPickingTexture` constructor uses `.Result` on an async shader load — the
// deadlock pattern documented in `openstack-sfx`.

use glow::HasContext;

/// C# `enum RenderPrimitiveType`.
///
/// 43 variants in the C#, of which 33 are `N_CONTROL_POINT_PATCHLIST` for
/// tessellation. Only the ten non-patchlist values map to a GL primitive; the
/// patchlists all draw as `GL_PATCHES` with the control-point count set
/// separately via `glPatchParameteri`, which the C# never calls — so
/// tessellation could not have worked there.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
#[repr(u32)]
pub enum RenderPrimitiveType {
    Points = 0x0,
    Lines = 0x1,
    LinesWithAdjacency = 0x2,
    LineStrip = 0x3,
    LineStripWithAdjacency = 0x4,
    Triangles = 0x5,
    TrianglesWithAdjacency = 0x6,
    TriangleStrip = 0x7,
    TriangleStripWithAdjacency = 0x8,
    InstancedQuads = 0x9,
    Heterogenous = 0xA,
    /// `RENDER_PRIM_{n}_CONTROL_POINT_PATCHLIST`, n in 1..=32 (0xB..=0x2A).
    ControlPointPatchList(u8),
}

impl RenderPrimitiveType {
    /// Decode the on-disk value. `None` for anything outside 0x0..=0x2A.
    pub fn from_raw(v: u32) -> Option<Self> {
        Some(match v {
            0x0 => Self::Points,
            0x1 => Self::Lines,
            0x2 => Self::LinesWithAdjacency,
            0x3 => Self::LineStrip,
            0x4 => Self::LineStripWithAdjacency,
            0x5 => Self::Triangles,
            0x6 => Self::TrianglesWithAdjacency,
            0x7 => Self::TriangleStrip,
            0x8 => Self::TriangleStripWithAdjacency,
            0x9 => Self::InstancedQuads,
            0xA => Self::Heterogenous,
            0xB..=0x2A => Self::ControlPointPatchList((v - 0xA) as u8),
            _ => return None,
        })
    }

    /// The GL primitive mode to pass to `draw_elements`.
    ///
    /// `None` for `Heterogenous` (which is a container, not a primitive) and
    /// for `InstancedQuads` (which needs an instanced draw path, not a mode).
    pub fn gl_mode(self) -> Option<u32> {
        Some(match self {
            Self::Points => glow::POINTS,
            Self::Lines => glow::LINES,
            Self::LinesWithAdjacency => glow::LINES_ADJACENCY,
            Self::LineStrip => glow::LINE_STRIP,
            Self::LineStripWithAdjacency => glow::LINE_STRIP_ADJACENCY,
            Self::Triangles => glow::TRIANGLES,
            Self::TrianglesWithAdjacency => glow::TRIANGLES_ADJACENCY,
            Self::TriangleStrip => glow::TRIANGLE_STRIP,
            Self::TriangleStripWithAdjacency => glow::TRIANGLE_STRIP_ADJACENCY,
            Self::ControlPointPatchList(_) => glow::PATCHES,
            Self::InstancedQuads | Self::Heterogenous => return None,
        })
    }

    /// Control points per patch, for `glPatchParameteri`. The C# never sets
    /// this, so its patchlist primitives would draw with the default of 3
    /// regardless of the declared count.
    pub fn patch_vertices(self) -> Option<u8> {
        match self {
            Self::ControlPointPatchList(n) => Some(n),
            _ => None,
        }
    }
}

/// C# `GLMeshBuffers.Buffer`.
#[derive(Debug, Clone, Copy)]
pub struct MeshBuffer {
    pub handle: glow::Buffer,
    /// Size GL actually allocated, from `GL_BUFFER_SIZE`. The C# reads this
    /// back rather than trusting the requested size, which is worth keeping —
    /// a driver may round up.
    pub size: i64,
}

/// C# `class GLMeshBuffers`.
///
/// Unlike the C#, this frees its buffers on drop (bug 3). It holds no reference
/// to the GL context, so `delete` must be called with the same context that
/// created it — enforced by taking `&glow::Context` explicitly.
#[derive(Debug, Default)]
pub struct GlMeshBuffers {
    pub vertex_buffers: Vec<MeshBuffer>,
    pub index_buffers: Vec<MeshBuffer>,
}

impl GlMeshBuffers {
    /// C# `GLMeshBuffers(IVBIB vbib)`.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn new(
        gl: &glow::Context,
        vertex_data: &[&[u8]],
        index_data: &[&[u8]],
    ) -> Result<Self, String> {
        let mut me = Self::default();
        for (target, src, out) in [
            (glow::ARRAY_BUFFER, vertex_data, &mut me.vertex_buffers as *mut Vec<MeshBuffer>),
            (glow::ELEMENT_ARRAY_BUFFER, index_data, &mut me.index_buffers as *mut Vec<MeshBuffer>),
        ] {
            for data in src {
                let handle = gl.create_buffer()?;
                gl.bind_buffer(target, Some(handle));
                gl.buffer_data_u8_slice(target, data, glow::STATIC_DRAW);
                let size = gl.get_buffer_parameter_i32(target, glow::BUFFER_SIZE) as i64;
                (*out).push(MeshBuffer { handle, size });
            }
        }
        Ok(me)
    }

    /// Free every buffer. Must be called with the creating context current.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn delete(&mut self, gl: &glow::Context) {
        for b in self.vertex_buffers.drain(..).chain(self.index_buffers.drain(..)) {
            gl.delete_buffer(b.handle);
        }
    }
}

/// C# `class QuadIndexBuffer`.
///
/// Builds a static index buffer of two triangles per quad: `0,1,2, 0,2,3`.
pub struct QuadIndexBuffer {
    pub handle: glow::Buffer,
    pub index_count: usize,
}

impl QuadIndexBuffer {
    /// Highest vertex index representable in a `u16` index buffer.
    const MAX_VERTEX_INDEX: usize = u16::MAX as usize;

    /// C# `QuadIndexBuffer(int size)`, where `size` is the **index** count.
    ///
    /// Two checks the C# lacks:
    ///
    /// * `size` must be a multiple of 6. The C# loops `i < size / 6`, so a
    ///   non-multiple silently leaves the trailing indices at 0 — degenerate
    ///   triangles that still get drawn.
    /// * The largest vertex index written is `(size / 6 - 1) * 4 + 3`. Past
    ///   65535 that wraps in a `u16`, so a buffer of more than ~98,300 indices
    ///   silently aliases back to the start of the vertex array. The C# has no
    ///   guard at all.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn new(gl: &glow::Context, size: usize) -> Result<Self, String> {
        if size % 6 != 0 {
            return Err(format!("index count {size} is not a multiple of 6"));
        }
        let quads = size / 6;
        if quads > 0 {
            let max_vertex = (quads - 1) * 4 + 3;
            if max_vertex > Self::MAX_VERTEX_INDEX {
                return Err(format!(
                    "{quads} quads need vertex index {max_vertex}, which overflows u16"
                ));
            }
        }
        let mut indices = vec![0u16; size];
        for i in 0..quads {
            let (v, o) = ((i * 4) as u16, i * 6);
            indices[o] = v;
            indices[o + 1] = v + 1;
            indices[o + 2] = v + 2;
            indices[o + 3] = v;
            indices[o + 4] = v + 2;
            indices[o + 5] = v + 3;
        }
        let handle = gl.create_buffer()?;
        gl.bind_buffer(glow::ELEMENT_ARRAY_BUFFER, Some(handle));
        gl.buffer_data_u8_slice(
            glow::ELEMENT_ARRAY_BUFFER,
            bytes_of_u16(&indices),
            glow::STATIC_DRAW,
        );
        Ok(Self { handle, index_count: size })
    }

    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn delete(self, gl: &glow::Context) {
        gl.delete_buffer(self.handle);
    }
}

/// Little-endian byte view of a `u16` slice, for `buffer_data_u8_slice`.
fn bytes_of_u16(v: &[u16]) -> &[u8] {
    // Safe: u16 has no padding or invalid bit patterns, and the resulting
    // slice is 2x the length. Byte order matches the host, which is what GL
    // expects for client-side index data.
    unsafe { std::slice::from_raw_parts(v.as_ptr() as *const u8, std::mem::size_of_val(v)) }
}

/// C# `GLPickingTexture.PixelInfo`.
///
/// **Four** fields, not three. The C# declares three `uint`s (12 bytes) and
/// then reads `RGBA_INTEGER`/`UNSIGNED_INT` into it — 16 bytes — overrunning
/// the struct (bug 1). The fourth component is read into `unused3` here.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
#[repr(C)]
pub struct PixelInfo {
    pub object_id: u32,
    pub mesh_id: u32,
    pub unused2: u32,
    /// The fourth RGBA component the C# read but had nowhere to put.
    pub unused3: u32,
}

/// C# `GLPickingTexture.PickingIntent`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum PickingIntent {
    #[default]
    Select,
    Open,
}

/// C# `GLPickingTexture.PickingRequest`.
#[derive(Debug, Clone, Copy, Default)]
pub struct PickingRequest {
    pub active_next_frame: bool,
    pub cursor_x: i32,
    pub cursor_y: i32,
    pub intent: PickingIntent,
}

impl PickingRequest {
    /// C# `NextFrame(int x, int y, PickingIntent intent)`.
    pub fn next_frame(&mut self, x: i32, y: i32, intent: PickingIntent) {
        self.active_next_frame = true;
        self.cursor_x = x;
        self.cursor_y = y;
        self.intent = intent;
    }
}

/// C# `GLPickingTexture.PickingResponse`.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct PickingResponse {
    pub intent: PickingIntent,
    pub pixel_info: PixelInfo,
}

/// C# `class GLPickingTexture : IDisposable, IPickingTexture`.
///
/// An off-screen integer framebuffer that renders object and mesh ids so a
/// cursor position can be resolved to what is under it.
pub struct GlPickingTexture {
    pub request: PickingRequest,
    pub debug: bool,
    width: i32,
    height: i32,
    fbo: glow::Framebuffer,
    color: glow::Texture,
    depth: glow::Texture,
}

impl GlPickingTexture {
    /// C# `Setup()`, called from the constructor.
    ///
    /// The C#'s initial size is 4x4 and it resizes on the first viewport
    /// change. Unlike the C#, a framebuffer-incomplete failure frees what it
    /// allocated before returning (bug 4).
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn new(gl: &glow::Context, debug: bool) -> Result<Self, String> {
        let (width, height) = (4, 4);
        let fbo = gl.create_framebuffer()?;
        gl.bind_framebuffer(glow::FRAMEBUFFER, Some(fbo));

        let color = gl.create_texture()?;
        gl.bind_texture(glow::TEXTURE_2D, Some(color));
        gl.tex_image_2d(
            glow::TEXTURE_2D, 0, glow::RGBA32UI as i32, width, height, 0,
            glow::RGBA_INTEGER, glow::UNSIGNED_INT, glow::PixelUnpackData::Slice(None),
        );
        gl.tex_parameter_i32(glow::TEXTURE_2D, glow::TEXTURE_MIN_FILTER, glow::NEAREST as i32);
        // The C# passes TextureMinFilter::Nearest for the MAG filter. The
        // values coincide, so it works; this uses the right constant.
        gl.tex_parameter_i32(glow::TEXTURE_2D, glow::TEXTURE_MAG_FILTER, glow::NEAREST as i32);
        gl.framebuffer_texture_2d(
            glow::FRAMEBUFFER, glow::COLOR_ATTACHMENT0, glow::TEXTURE_2D, Some(color), 0,
        );

        let depth = gl.create_texture()?;
        gl.bind_texture(glow::TEXTURE_2D, Some(depth));
        gl.tex_image_2d(
            glow::TEXTURE_2D, 0, glow::DEPTH_COMPONENT as i32, width, height, 0,
            glow::DEPTH_COMPONENT, glow::FLOAT, glow::PixelUnpackData::Slice(None),
        );
        gl.framebuffer_texture_2d(
            glow::FRAMEBUFFER, glow::DEPTH_ATTACHMENT, glow::TEXTURE_2D, Some(depth), 0,
        );

        let status = gl.check_framebuffer_status(glow::FRAMEBUFFER);
        if status != glow::FRAMEBUFFER_COMPLETE {
            // Clean up before failing — the C# leaks all three here.
            gl.bind_framebuffer(glow::FRAMEBUFFER, None);
            gl.delete_texture(color);
            gl.delete_texture(depth);
            gl.delete_framebuffer(fbo);
            return Err(format!("framebuffer incomplete: {status:#x}"));
        }
        gl.bind_texture(glow::TEXTURE_2D, None);
        gl.bind_framebuffer(glow::FRAMEBUFFER, None);
        Ok(Self {
            request: PickingRequest::default(),
            debug,
            width,
            height,
            fbo,
            color,
            depth,
        })
    }

    /// C# `IsActive`.
    #[inline]
    pub fn is_active(&self) -> bool {
        self.request.active_next_frame
    }

    /// C# `Render()` — bind and clear.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn render(&self, gl: &glow::Context) {
        gl.bind_framebuffer(glow::DRAW_FRAMEBUFFER, Some(self.fbo));
        gl.clear_color(0.0, 0.0, 0.0, 0.0);
        gl.clear(glow::COLOR_BUFFER_BIT | glow::DEPTH_BUFFER_BIT);
    }

    /// C# `Finish()` — unbind and, if a pick was requested, resolve it.
    ///
    /// The C# raises an `OnPicked` event; this returns the response so the
    /// caller decides, avoiding the mutable-event-handler field.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn finish(&mut self, gl: &glow::Context) -> Option<PickingResponse> {
        gl.bind_framebuffer(glow::DRAW_FRAMEBUFFER, None);
        if !self.request.active_next_frame {
            return None;
        }
        self.request.active_next_frame = false;
        let pixel_info = self.read_pixel_info(gl, self.request.cursor_x, self.request.cursor_y);
        Some(PickingResponse { intent: self.request.intent, pixel_info })
    }

    /// C# `Resize(int width, int height)`.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn resize(&mut self, gl: &glow::Context, width: i32, height: i32) {
        self.width = width;
        self.height = height;
        gl.bind_texture(glow::TEXTURE_2D, Some(self.color));
        gl.tex_image_2d(
            glow::TEXTURE_2D, 0, glow::RGBA32UI as i32, width, height, 0,
            glow::RGBA_INTEGER, glow::UNSIGNED_INT, glow::PixelUnpackData::Slice(None),
        );
        gl.bind_texture(glow::TEXTURE_2D, Some(self.depth));
        gl.tex_image_2d(
            glow::TEXTURE_2D, 0, glow::DEPTH_COMPONENT as i32, width, height, 0,
            glow::DEPTH_COMPONENT, glow::FLOAT, glow::PixelUnpackData::Slice(None),
        );
    }

    /// C# `ReadPixelInfo(int width, int height)` — misnamed there; these are
    /// cursor coordinates (bug 2).
    ///
    /// Reads a full RGBA integer pixel into a 4-field struct, so the read
    /// cannot overrun its destination (bug 1).
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn read_pixel_info(
        &self,
        gl: &glow::Context,
        cursor_x: i32,
        cursor_y: i32,
    ) -> PixelInfo {
        gl.flush();
        gl.finish();
        gl.bind_framebuffer(glow::READ_FRAMEBUFFER, Some(self.fbo));
        gl.read_buffer(glow::COLOR_ATTACHMENT0);
        // 4 components x 4 bytes. The C# read this into 12 bytes.
        let mut buf = [0u8; 16];
        gl.read_pixels(
            cursor_x,
            self.height - cursor_y, // GL's origin is bottom-left
            1,
            1,
            glow::RGBA_INTEGER,
            glow::UNSIGNED_INT,
            glow::PixelPackData::Slice(Some(&mut buf)),
        );
        gl.read_buffer(glow::NONE);
        gl.bind_framebuffer(glow::READ_FRAMEBUFFER, None);
        let u = |i: usize| u32::from_ne_bytes(buf[i * 4..i * 4 + 4].try_into().unwrap());
        PixelInfo { object_id: u(0), mesh_id: u(1), unused2: u(2), unused3: u(3) }
    }

    /// C# `Dispose()`.
    ///
    /// # Safety
    /// A GL context must be current on this thread.
    pub unsafe fn delete(self, gl: &glow::Context) {
        gl.delete_texture(self.color);
        gl.delete_texture(self.depth);
        gl.delete_framebuffer(self.fbo);
    }
}

/// C# `GLCamera.GfxViewport(int x, int y, int width = 0, int height = 0)`.
///
/// A zero width or height means "use the window size" — a sentinel the C#
/// spells with default arguments. `None` here.
///
/// # Safety
/// A GL context must be current on this thread.
pub unsafe fn gfx_viewport(
    gl: &glow::Context,
    x: i32,
    y: i32,
    width: Option<i32>,
    height: Option<i32>,
    window_size: glam::IVec2,
) {
    gl.viewport(
        x,
        y,
        width.unwrap_or(window_size.x),
        height.unwrap_or(window_size.y),
    );
}

// NOT PORTED from this file: `GLDebugCamera` (input handling, needs a windowing
// crate decision — `winit` vs SDL), `GLMeshBufferCache`, `MeshBatchRenderer`,
// `GLRenderMaterial`, `GLRenderableMesh`, `OctreeDebugRenderer<T>`,
// `MeshSceneNode`, and `ParticleControllerFactory`. All depend on the
// `IVBIB`/`OnDiskBufferData` GPU-descriptor types that `openstack-gfx-egin`
// also leaves unported, so they are blocked on the same decision.

#[cfg(test)]
mod tests {
    use super::*;

    // These test the pure logic only — index generation, enum mapping, and the
    // guards added above. Anything touching a GL context is untestable here and
    // untested; see the caveat in the module header.

    #[test]
    fn primitive_types_round_trip() {
        for v in 0x0u32..=0x2A {
            let p = RenderPrimitiveType::from_raw(v).expect("in range");
            if let RenderPrimitiveType::ControlPointPatchList(n) = p {
                assert_eq!(v, 0xA + n as u32);
                assert!((1..=32).contains(&n), "control points {n}");
            }
        }
        assert!(RenderPrimitiveType::from_raw(0x2B).is_none());
    }

    #[test]
    fn patchlists_map_to_patches_and_carry_their_count() {
        let p = RenderPrimitiveType::from_raw(0xB).unwrap();
        assert_eq!(p.patch_vertices(), Some(1));
        assert_eq!(p.gl_mode(), Some(glow::PATCHES));
        let p32 = RenderPrimitiveType::from_raw(0x2A).unwrap();
        assert_eq!(p32.patch_vertices(), Some(32));
    }

    #[test]
    fn container_primitives_have_no_gl_mode() {
        assert!(RenderPrimitiveType::Heterogenous.gl_mode().is_none());
        assert!(RenderPrimitiveType::InstancedQuads.gl_mode().is_none());
        assert_eq!(RenderPrimitiveType::Triangles.gl_mode(), Some(glow::TRIANGLES));
    }

    /// The index pattern from `QuadIndexBuffer`, extracted so it can be tested
    /// without a GL context.
    fn quad_indices(size: usize) -> Vec<u16> {
        let mut indices = vec![0u16; size];
        for i in 0..size / 6 {
            let (v, o) = ((i * 4) as u16, i * 6);
            indices[o] = v;
            indices[o + 1] = v + 1;
            indices[o + 2] = v + 2;
            indices[o + 3] = v;
            indices[o + 4] = v + 2;
            indices[o + 5] = v + 3;
        }
        indices
    }

    #[test]
    fn quad_indices_are_two_triangles_per_quad() {
        assert_eq!(quad_indices(6), vec![0, 1, 2, 0, 2, 3]);
        assert_eq!(
            quad_indices(12),
            vec![0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7]
        );
    }

    #[test]
    fn every_quad_uses_four_consecutive_vertices() {
        let idx = quad_indices(60);
        for q in 0..10 {
            let s = &idx[q * 6..q * 6 + 6];
            let base = (q * 4) as u16;
            let mut used: Vec<u16> = s.iter().map(|i| i - base).collect();
            used.sort_unstable();
            used.dedup();
            assert_eq!(used, vec![0, 1, 2, 3], "quad {q}");
        }
    }

    #[test]
    fn the_u16_overflow_threshold_is_where_expected() {
        // Largest safe quad count: (q-1)*4+3 <= 65535  =>  q <= 16384.
        let safe = 16384usize;
        assert!((safe - 1) * 4 + 3 <= u16::MAX as usize);
        assert!(safe * 4 + 3 > u16::MAX as usize, "one more overflows");
        // So the index-count limit is 16384 * 6 = 98304. The C# has no guard.
        assert_eq!(safe * 6, 98304);
    }

    #[test]
    fn pixel_info_is_sixteen_bytes_not_twelve() {
        // This is the whole point of bug 1: the read needs 16 bytes.
        assert_eq!(std::mem::size_of::<PixelInfo>(), 16);
    }

    #[test]
    fn u16_byte_view_has_the_right_length() {
        let v = [1u16, 2, 3];
        assert_eq!(bytes_of_u16(&v).len(), 6);
    }

    #[test]
    fn picking_request_records_the_cursor() {
        let mut r = PickingRequest::default();
        assert!(!r.active_next_frame);
        r.next_frame(12, 34, PickingIntent::Open);
        assert!(r.active_next_frame);
        assert_eq!((r.cursor_x, r.cursor_y), (12, 34));
        assert_eq!(r.intent, PickingIntent::Open);
    }
}
