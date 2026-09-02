//! `openstack-phy2` — 1:1 port of .NET project `OpenStack.Phy2`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees
//! can be diffed and updated in parallel. See PORT_MAP.tsv at the workspace root.

pub mod animation;
pub mod bounding_box;
pub mod bsp;
pub mod collision;
pub mod combat;
pub mod command;
pub mod common;
pub mod cyl_sphere;
pub mod entity;
pub mod extensions;
pub mod hooks;
pub mod managers;
pub mod object_info;
pub mod part_array;
pub mod particles;
pub mod phys_obj_profile;
pub mod physics_desc;
pub mod physics_engine;
pub mod physics_globals;
pub mod physics_obj;
pub mod physics_part;
pub mod polygon;
pub mod ray;
pub mod scripts;
pub mod setup;
pub mod sound;
pub mod sphere;
pub mod sphere_path;
pub mod trajectory;
pub mod trajectory2;
pub mod transition;
pub mod util;
