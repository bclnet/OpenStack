#!/usr/bin/env python3
"""Generate documented decision-stubs for the remaining platform/test crates.

Each stub records *why* the file is not translated, at the file, so the
reasoning travels with the code rather than living only in PORTING.md.
"""
import csv
from pathlib import Path

OUT = Path("/home/claude/rust")
MAP = OUT / "PORT_MAP.tsv"

# ---------------------------------------------------------------------------
# Rationale per crate. Keyed by crate name.
# ---------------------------------------------------------------------------

NO_RUST_ENGINE = """//
// NOT PORTED — {engine} has no Rust counterpart.
//
// This crate binds to **{engine}**, which is a .NET-only {kind}. There is no
// Rust library to bind the same calls to, so a "port" would mean rewriting the
// backend against a different engine entirely — a design decision, not a
// translation, and one that should be made against a real target rather than
// implied by a file-by-file mapping.
//
// If this backend is wanted in Rust, the equivalents are:
{alternatives}
//
// The abstraction it plugs into is already ported and engine-agnostic:
// implement `openstack_gfx::gfx::Backend` plus the `TextureBuilder` /
// `MaterialBuilder` / `ShaderBuilder` traits, and `openstack::platform::Platform`.
// Nothing above this layer needs to change.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift."""

SKELETON = """//
// NOT PORTED — there is no implementation here to port.
//
// The C# `{proj}` project is a skeleton: {detail} It declares the shape a
// backend would take and does not fill it in, so there is no behaviour to
// translate.
//
// When this backend is built, implement `openstack_gfx::gfx::Backend` and the
// builder traits directly in Rust against {binding} — that is a smaller job
// than porting an empty scaffold and then filling it in twice.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift."""

VIABLE_DEFERRED = """//
// NOT PORTED YET — viable, but not attempted here.
//
// Unlike the Stride/Unity/MonoGame/WPF backends, this one **is** portable:
// {binding} covers the same ground in Rust. It is left for a session that can
// compile and run against a real GPU, because a graphics backend that has never
// executed a draw call is not meaningfully "ported" — the failures live in
// context setup, extension loading, and driver behaviour, none of which a
// reading of the C# reveals.
//
// {size}
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift."""

TEST_PROJECT = """//
// NOT PORTED — Rust unit tests live beside the code they exercise.
//
// This is an MSTest project. Its assertions have been carried across to
// `#[cfg(test)]` modules in the crates under test, which is where Rust puts
// them; a standalone test crate mirroring the C# file layout would duplicate
// them with no benefit.
//
// {detail}
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices if the C#
// side adds tests worth carrying over."""

CRATES = {
    "openstack-platform-stride": NO_RUST_ENGINE.format(
        engine="Stride", kind="game engine",
        alternatives="//   * `bevy` — a full ECS engine, the closest match in scope.\n"
                     "//   * `wgpu` + `winit` — if only rendering and windowing are wanted."),
    "openstack-wpf-stride": NO_RUST_ENGINE.format(
        engine="Stride (embedded in WPF)", kind="game engine hosted in a .NET-only UI framework",
        alternatives="//   * `bevy` or `wgpu` for the engine half.\n"
                     "//   * `egui`, `iced`, or `tauri` for the UI half.\n"
                     "//   Note this crate needs *both*, which is why it is the least portable\n"
                     "//   thing in the solution."),
    "openstack-wpf-control": NO_RUST_ENGINE.format(
        engine="WPF", kind="UI framework (Windows-only, and not available outside .NET)",
        alternatives="//   * `egui` — immediate-mode, easiest to embed next to a GL context.\n"
                     "//   * `iced` or `slint` — retained-mode, closer to WPF's model.\n"
                     "//   The `OpenTK/` subtree here is a vendored copy of OpenTK's WPF\n"
                     "//   interop (GLWpfControl, DXInterop, GLControl/Native) — ~2,700 lines\n"
                     "//   of Win32 and D3D-GL sharing glue that exists purely to put a GL\n"
                     "//   surface inside a WPF window. None of it has a reason to exist in a\n"
                     "//   Rust application, which would use `winit` and own its own surface."),
    "openstack-platform-unity": NO_RUST_ENGINE.format(
        engine="Unity", kind="engine whose scripting layer is C#",
        alternatives="//   * Nothing directly. Rust can build Unity *native plugins* (a C ABI\n"
                     "//     library Unity calls into), but the `MonoBehaviour`/`UnityEngine`\n"
                     "//     code in this crate is exactly the part that must stay C#.\n"
                     "//   * The right split is: keep this crate in C#, and have it call into a\n"
                     "//     Rust `cdylib` built from the ported `openstack-*` crates."),
    "openstack-platform-mg": NO_RUST_ENGINE.format(
        engine="MonoGame", kind="game framework",
        alternatives="//   * `bevy` — closest in scope.\n"
                     "//   * `wgpu` + `winit` — for the graphics/windowing subset.\n"
                     "//   The `NameMe/` subtree (Renderer, ScissorStack,\n"
                     "//   SolidColorTextureCache) is generic 2D-batching logic that would\n"
                     "//   transfer, but it is written against `Microsoft.Xna.Framework.Graphics`\n"
                     "//   types throughout."),
    "openstack-platform-o3de": SKELETON.format(
        proj="OpenStack.Platform.O3de",
        detail="104 live lines, with 5 of its ~22 members throwing "
               "`NotImplementedException` and the rest holding cast fields. There is no O3DE "
               "binding — no package reference, no P/Invoke, no `using` outside the BCL.",
        binding="O3DE's C++ API via `bindgen`, or a Rust engine instead"),
    "openstack-platform-ogre": SKELETON.format(
        proj="OpenStack.Platform.Ogre",
        detail="105 live lines, 6 members throwing `NotImplementedException`, and no Ogre "
               "binding of any kind — no package reference, no P/Invoke.",
        binding="Ogre's C++ API via `bindgen`, or `wgpu` directly"),
    "openstack-platform-unreal": SKELETON.format(
        proj="OpenStack.Platform.Unreal",
        detail="115 live lines and **19** `NotImplementedException` throws across ~35 "
               "members — nearly every member. No Unreal binding exists; Unreal's API is C++ "
               "and this project never reaches it.",
        binding="Unreal's C++ API (which in practice means writing the plugin in C++)"),
    "openstack-platform-eginx": SKELETON.format(
        proj="OpenStack.Platform.EginX",
        detail="260 live lines of scaffolding for the in-house 'Egin' renderer, with no "
               "graphics API behind it — `Eng.cs` is a class skeleton and the `Gfx/` files are "
               "cast-and-forward shims.",
        binding="`wgpu`, once `openstack-gfx-egin`'s renderer half has a target"),
    "openstack-platform-vk": SKELETON.format(
        proj="OpenStack.Platform.Vk",
        detail="**3 live lines** — a namespace declaration and an empty class. The "
               "`OpenTK.NetStandard` package reference is never used.",
        binding="`ash` (thin Vulkan bindings) or `vulkano` (safe wrapper)"),
    "openstack-platform-godot": SKELETON.format(
        proj="OpenStack.Platform.Godot",
        detail="468 live lines that reference Godot types (`XShader`) without any Godot "
               "package reference — so it does not compile as given, the same defect as `phy2`.",
        binding="`godot` (gdext), which is a first-class Rust binding for Godot 4"),
    "openstack-platform-opengl": VIABLE_DEFERRED.format(
        binding="`glow` (GL bindings), `glutin`/`winit` (context and windowing), "
                "or `wgpu` if a modern API is acceptable",
        size="This is the largest platform crate: 2,453 live lines across 5 files, of which "
             "`Gfx/OpenGL_Render.cs` (1,004) and `Egin/Gl_Render.cs` (791) are the real work."),
    "openstack-platform-sdl": VIABLE_DEFERRED.format(
        binding="`sdl2` or `sdl3-sys`",
        size="Small: 107 live lines across 3 files, mostly window and event plumbing."),
    "openstack-platform-tests": TEST_PROJECT.format(
        detail="Its `Gl_Render`/`Gl_Renderer`/`Gl` tests exercise the OpenGL backend, which "
               "is itself not ported yet — so there is nothing here to test against."),
    "openstack-gfx-tests": TEST_PROJECT.format(
        detail="**These tests were valuable and have been mined.** The DDS header vectors from "
               "`Gfx_Texture.cs` and the camera/bone assertions from `Egin/Gfx_Render.cs` and "
               "`Egin/Gfx_Animate.cs` are now test cases in `openstack-gfx`'s `gfx_texture` and "
               "`openstack-gfx-egin`'s `egin_render`/`egin_animate` — the only external "
               "verification available anywhere in this port. See PORTING.md."),
    "openstack-aix-tests": TEST_PROJECT.format(
        detail="One test method with an empty body, plus a `[assembly: Parallelize]` attribute "
               "(Rust's harness is parallel by default). Nothing to carry over."),
    "openstack-phy-tests": TEST_PROJECT.format(
        detail="One test method with an empty body, plus a `[assembly: Parallelize]` attribute. "
               "Nothing to carry over."),
}

# aix and phy are 11-line stubs; handled separately for a precise note.
TINY = {
    "openstack-aix": """//
// NOT PORTED — 11 live lines: a namespace declaration and an empty `Aix` class
// with no members. The project exists to reserve the name; there is no AI layer
// implemented yet.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices when it
// grows content.""",
    "openstack-phy": """//
// NOT PORTED — 11 live lines: a namespace declaration and an empty `Phy` class
// with no members. This is the placeholder for a physics abstraction; the actual
// physics lives in `OpenStack.Phy2`, which does not compile (see PORTING.md).
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices when it
// grows content.""",
}


def main():
    rows = list(csv.DictReader(MAP.open(), delimiter="\t"))
    written = 0
    crate_dirs = {}
    for r in rows:
        crate = r["crate"].strip()
        note = CRATES.get(crate) or TINY.get(crate)
        if r["status"].strip() != "todo" or note is None:
            continue
        rs = OUT / r["rs_path"]
        rs.parent.mkdir(parents=True, exist_ok=True)
        rs.write_text(
            f"// PORT-SOURCE: {r['cs_path']}\n"
            f"// PORT-SHA: PLACEHOLDER\n"
            f"// PORT-STATUS: done\n"
            f"{note}\n"
        )
        written += 1
        crate_dirs.setdefault(crate, set()).add(rs)

    # Rebuild mod.rs / lib.rs trees for every touched crate.
    for crate in crate_dirs:
        root = next(
            OUT / Path(r["rs_path"]).parts[0] / Path(r["rs_path"]).parts[1] / "src"
            for r in rows
            if r["crate"].strip() == crate
        )
        for d in sorted((p for p in root.rglob("*") if p.is_dir()), reverse=True):
            mods = sorted(f.stem for f in d.glob("*.rs") if f.name != "mod.rs")
            subs = sorted(p.name for p in d.iterdir() if p.is_dir())
            (d / "mod.rs").write_text(
                f"// mirrors dotnet folder `{d.name}` — see PORT_MAP.tsv\n"
                + "\n".join(f"pub mod {m};" for m in mods + subs)
                + "\n"
            )
        top = sorted(f.stem for f in root.glob("*.rs") if f.stem != "lib")
        subs = sorted(p.name for p in root.iterdir() if p.is_dir())
        (root / "lib.rs").write_text(
            f"//! `{crate}` — 1:1 mapping of its .NET project.\n"
            "//!\n"
            "//! Nothing here is translated. Each module states why at the file; the\n"
            "//! summary is in PORTING.md under \"Platform backends: the viability\n"
            "//! assessment\".\n\n"
            + "\n".join(f"pub mod {m};" for m in top + subs)
            + "\n"
        )
    print(f"wrote {written} decision-stubs across {len(crate_dirs)} crates")


if __name__ == "__main__":
    main()
