//! `openstack-platform-opengl` — 1:1 port of its .NET project.
//!
//! **This crate has never been compiled or run.** The platform registration
//! layer is a straightforward translation; the GL layer in `egin::gl_render` is
//! a reviewed translation of OpenTK calls to `glow` that has executed no draw
//! call. Treat the structure and the documented bug fixes as the value, and
//! expect to debug the GL calls against a real context.
//!
//! See PORTING.md, "Attempting the OpenGL backend against glow".

pub mod platform_open_gl;
pub mod slots;
pub mod egin;
pub mod gfx;
