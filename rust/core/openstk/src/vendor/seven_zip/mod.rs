// mirrors dotnet folder `_LIB/SevenZip` — see PORT_MAP.tsv
//
// Vendored LZMA SDK. Nothing here is ported; use `lzma-rs` or `sevenz-rust`.
// Each file explains the reasoning.
pub mod buffer;
pub mod command_line_parser;
pub mod compress_lz;
pub mod compress_lzma;
pub mod compress_range_coder;
pub mod seven_zip;
