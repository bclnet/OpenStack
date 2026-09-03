// mirrors dotnet folder `ExtServices` — see PORT_MAP.tsv
//
// Both files here bridge to external code (an FFmpeg process, native libchdr).
// Neither is ported; each explains what to use instead.
pub mod ffmpeg_service;
pub mod lib_chd;
