// PORT-SOURCE: Vfx/OpenStack.Vfx.Program/Program.cs
// PORT-SHA: ed0c414069484bc7
// PORT-STATUS: done
//
// NOT PORTED — a developer scratchpad, not a program.
//
// `Main` calls `Pass0`; `Pass1` through `Pass3` are unreferenced. Every one of
// them hardcodes an absolute path from one machine:
//
//     E:\ArchiveLibrary\Rockstar\Monster Truck Madness 64 (USA).7z
//     C:\_GITHUB\bclnet\GameX\OpenStack\dotnet\...\bin\Debug\net9.0\0.cxi
//
// It cannot run anywhere else, and several passes depend on intermediate files
// (`0.cxi`, `romfs.bin`) that an earlier pass must have written into a `bin`
// directory first. This is someone's manual test harness, checked in.
//
// `Pass0` also mirrors the discarded-result pattern from `N64FileSystem`: it
// builds an `N64Rom` into `var abc` and never touches it.
//
// If a CLI for inspecting these containers is wanted, it should take paths as
// arguments and live behind `clap` — that is a new program, not a port. The
// underlying capability belongs in the library (`n64.rs` already has it), with
// the binary as a thin shell over it.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` watches it.
