//! `openstack-polyio` — 1:1 port of .NET project `OpenStack.PolyIO`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees can
//! be diffed and updated in parallel. Every file carries a `PORT-SOURCE` header
//! naming its C# original and a `PORT-SHA` of that file's contents at port
//! time; `./sync-check.sh` at the workspace root reports drift. See
//! `PORT_MAP.tsv` and `PORTING.md`.
//!
//! Numerics delegate to `glam` and struct blitting to `bytemuck`, per the
//! project decision recorded in `PORTING.md`.

pub mod i_source;
pub mod system;
pub mod system_drawing;
pub mod system_io;
pub mod system_numerics;
pub mod system_text;
pub mod type_x;
pub mod x;

/// The names worth importing together. C# got these via `global using` and
/// namespace-wide extension methods; Rust needs the traits in scope explicitly.
pub mod prelude {
    pub use crate::i_source::{AssetPath, HaveSource, Source, SourceError, SourceExt};

    pub use crate::system_io::bit_stream::BitStream;
    pub use crate::system_io::byte_xor_stream::ByteXorStream;
    pub use crate::system_io::circular_buffer::CircularBuffer;
    pub use crate::system_io::huffman::{Huffman, HuffmanError};
    pub use crate::system_io::i_stream::{GetStream, ReadSeek, WriteToStream};
    pub use crate::system_io::partial_input_stream::{PartialInputStream, SharedSource};
    pub use crate::system_io::polyfill::{XBoundBox, XLump2NO, XLumpNO, XLumpNO2, XLumpON};
    pub use crate::system_io::polyfill_binary_reader::{BinaryReaderExt, Origin, ReadError};
    pub use crate::system_io::polyfill_binary_writer::BinaryWriterExt;
    pub use crate::system_io::indented_text_writer::{IndentedIoWriter, IndentedTextWriter};
    pub use crate::system_io::polyfill_stream::{StreamExt, StreamWriteExt};

    pub use crate::system::half_float::HalfFloat;
    pub use crate::system_text::value_string_builder::ValueStringBuilder;

    // Numerics: glam types, re-exported under the names the C# used.
    pub use crate::system_numerics::matrix2x2::Matrix2x2;
    pub use crate::system_numerics::matrix3x3::Matrix3x3;
    pub use crate::system_numerics::matrix3x4::Matrix3x4;
    pub use crate::system_numerics::matrix4x3::Matrix4x3;
    pub use crate::system_numerics::polyfill::{Int2, Int3};
    pub use glam::{IVec2, IVec3, IVec4, Mat3, Mat4, Quat, Vec2, Vec3, Vec4};

    pub use crate::system_drawing::bounding_box::BoundingBox;
    pub use crate::system_drawing::bounding_frustum::{BoundingFrustum, Plane};
    pub use crate::system_drawing::bounding_sphere::BoundingSphere;
    pub use crate::system_drawing::curve::{Curve, Curve3, CurveKey};
    pub use crate::system_drawing::point3_d::Point3D;
    pub use crate::system_drawing::ray::Ray;
    pub use crate::system_drawing::rectangle::Rectangle;
}
