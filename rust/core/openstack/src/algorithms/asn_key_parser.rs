// PORT-SOURCE: Core/OpenStack/Algorithms/AsnKeyParser.cs
// PORT-SHA: b073712bea46cbc4
// PORT-STATUS: done
//
// A hand-rolled BER/DER decoder for RSA and DSA public/private keys —
// `AsnKeyParser` plus the `AsnParser` primitive reader (both in this one file),
// producing `RSAParameters` / `DSAParameters`.
//
// NOT PORTED, and this one is a deliberate refusal rather than a deferral.
//
// WHY. This is cryptographic input parsing. A BER length field mis-decoded by
// one byte, an integer sign bit mishandled, an OID compared with the wrong
// length — none of those announce themselves. They produce a key that is
// subtly wrong or accept a malformed structure, and the failure surfaces as a
// signature that does not verify, or worse, one that does when it should not.
// Hand-translating 201 lines of it with **no ability to compile or run a single
// test vector** in this environment would be putting a plausible-looking
// artefact where a verified one belongs. I am not willing to do that for
// crypto code.
//
// WHAT TO USE INSTEAD. Rust has this problem thoroughly solved, by crates that
// are fuzzed and audited against the actual specifications:
//
//   * `der` / `pkcs1` / `pkcs8` — RustCrypto's DER decoders and key formats.
//   * `rsa` — RSA keys with `pkcs1`/`pkcs8` import built in.
//   * `spki` — SubjectPublicKeyInfo, which is exactly what
//     `ParseRSAPublicKey` decodes (sequence, AlgorithmIdentifier with OID
//     1.2.840.113549.1.1.1, then a BIT STRING wrapping the RSAPublicKey).
//
// `RsaPublicKey::from_public_key_der(bytes)` replaces the whole file.
//
// IT ALSO HAS NO CALLERS. `AsnKeyParser` is referenced nowhere in the solution
// outside its own definition, so nothing is blocked by leaving it out.
//
// ONE C#-SIDE BUG, noted while reading it:
//
//   * `TrimLeadingZero(byte[] values)` indexes `values[0]` before checking the
//     length, so an empty array throws `IndexOutOfRangeException`. It is called
//     directly on `_parser.NextInteger()` results, so a zero-length INTEGER in
//     the input — malformed, but that is the case a parser must survive —
//     crashes instead of erroring. **Fix or delete this in the C# tree.**
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices if the
// C# side grows a caller, which would make this decision worth revisiting.
