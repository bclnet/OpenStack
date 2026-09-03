// PORT-SOURCE: Core/OpenStack.PolyIO/X.cs
// PORT-SHA: 34d0cdcf2f669221
// PORT-STATUS: done
//
// The C# file contains a namespace declaration and nothing else — every helper
// in it (`StripAssemblyVersion`, `SplitGenericName`, `SplitGenericType`,
// `SplitGenericTypeName`) is commented out.
//
// All four parse .NET type-name strings, so they belong with the reflection
// work in `openstack-core`'s `type_x.rs` rather than here. If any is revived on
// the C# side, port it then; there is nothing to translate today.
//
// Kept as a file so the 1:1 mapping with X.cs holds and `sync-check.sh` still
// watches it for changes.
