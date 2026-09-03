// PORT-SOURCE: Vfx/OpenStack.Vfx/X3ds.cs
// PORT-SHA: 32d1e7f2b030c2f5
// PORT-STATUS: done
//
// NOT PORTED — 3,094 live lines of Nintendo 3DS cartridge handling: NCSD/NCCH
// container parsing, ExeFS/RomFS extraction, BackwardLz77 decompression, ARM
// code analysis, and the NCCH crypto layer.
//
// This is a deliberate refusal on the same grounds as `AsnKeyParser.cs`, and
// the reasoning is stronger here:
//
//   1. **48 crypto call sites** (`Aes`, `SHA256`, `RSA`) implementing NCCH
//      decryption — AES-CTR and AES-CBC over region-specific key material with
//      per-section counters. Getting a counter derivation or an endianness
//      subtly wrong yields plaintext that decrypts *almost* correctly: headers
//      parse, and the corruption surfaces deep inside extracted content. That
//      is the worst possible failure mode, and nothing in this environment can
//      compile the code or run it against a real cartridge dump.
//
//   2. **An ARM disassembler dependency.** The file `using`s
//      `Gee.External.Capstone.Arm` and switches on `ArmOperandType` /
//      `ArmRegisterId` to analyse code sections. Rust has the `capstone` crate,
//      which binds the same native library — so this part is portable, but only
//      as part of the whole, and the whole is gated on (1).
//
// Unlike `phy2`, this project **does** compile: `Gee.External.Capstone` 2.3.0 is
// a proper `PackageReference`. So the blocker is not a missing dependency, it is
// that unverifiable crypto should not be written.
//
// To do this properly: a session with `cargo`, the `aes`/`ctr`/`sha2` and
// `capstone` crates, and at least one known-good `.3ds`/`.cci` dump with
// expected extraction hashes. Then port container-first (NCSD → NCCH headers →
// ExeFS/RomFS layout), verifying each layer's offsets against the dump before
// touching decryption.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
