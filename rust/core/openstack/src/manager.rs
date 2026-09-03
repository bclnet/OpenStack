// PORT-SOURCE: Core/OpenStack/Manager.cs
// PORT-SHA: d562394db202ebac
// PORT-STATUS: done
//
// World streaming: `CellManager` keeps a window of cells loaded around the
// player, builds each through a `CellBuilder`, and cancels/destroys cells that
// fall out of range. `CellBuilder<Object, Material, Texture, Shader>` is the
// generic that closes over a backend's handle types — the same shape `gfx` uses,
// so this reuses `openstack_gfx::gfx::Backend`'s associated types.
//
// ===================== FOUR C#-SIDE BUGS ==================================
//
//   1. **`BeginCellByName` files every named cell under `Int3.Zero`.**
//
//          var cell = BuildCell(record); Cells[Int3.Zero] = cell;
//
//      So loading two cells by name silently evicts the first — the dictionary
//      key has nothing to do with the cell. Worse, `UpdateCells` then measures
//      Chebyshev distance from `(0,0,0)` for that entry, so a named interior
//      cell is destroyed or hidden based on a coordinate it never had. The port
//      keys by the record's own `grid_id`.
//
//   2. **`modelObj != null` is always true for a value-type backend.** `Object`
//      is an *unconstrained* type parameter, so `Object modelObj = default;`
//      followed by `if (modelObj != null)` compares a boxed value against null.
//      If a backend uses a struct handle — a Unity instance id, a `u32` GPU
//      index, anything not a class — the check passes even when the model
//      failed to load, and `GfxApi.Attach` runs against `default`. Rust's
//      `Option<B::Object>` makes the two states distinct.
//
//   3. **`TerrainLayers` is `static` on a generic class.** Same defect as
//      `gfx`'s `TextureManager`: one cache per closed generic type, shared by
//      every instance, never evicted. Two builders over different sources hand
//      each other's terrain layers back. It is an instance field here.
//
//   4. **`UpdateCells` scans the whole square once per ring.** The loop is
//      `for r in 0..=radius { for x in minX..=maxX { for y in minY..=maxY {
//      if (d == r) ... } } }`, so it makes `radius + 1` full passes over
//      `(2*radius+1)^2` positions to visit each once — 726 iterations for
//      radius 5 where 121 would do. Correct, just wasteful; the port walks each
//      ring directly and keeps the same near-to-far ordering.
//
// Also: `GfxApi`/`GfxModel`/`GfxLight`/`GfxTerrain` are initialised by unchecked
// casts on array indices (`(IOpenGfxApi<..>)gfx[GfX.XApi]`), so a short or
// mis-ordered `gfx` array throws `IndexOutOfRangeException` or
// `InvalidCastException` at construction. They are typed `Option` fields here.

use std::collections::HashMap;

use openstack_gfx::gfx::{Backend, GfxAttach};
use openstack_polyio::system_numerics::polyfill::Int3;
use openstack_polyio::system_numerics::vector3::Vec3;

/// C# `CellManager.ICellXref` — one placed object in a cell.
pub trait CellXref {
    fn name(&self) -> &str;
    fn scale(&self) -> Option<f32>;
    fn position(&self) -> Vec3;
    fn euler_angles(&self) -> Vec3;
}

/// C# `CellManager.ICellXrefModel`.
pub trait CellXrefModel {
    fn model_path(&self) -> &str;
}

/// C# `CellManager.ICell`.
pub trait Cell {
    fn id(&self) -> u32;
    fn is_interior(&self) -> bool;
    fn grid_id(&self) -> Int3;
    fn name(&self) -> &str;
    fn ambient_light(&self) -> Option<u32>;
    fn xrefs(&self) -> &[Box<dyn CellXref>];
}

/// C# `CellManager.ILand` — terrain for one exterior cell.
pub trait Land {
    fn grid_id(&self) -> Int3;
    fn vtex(&self) -> &[u32];
    fn height_offset(&self) -> f32;
    fn heights(&self) -> Option<&[i8]>;
}

/// C# `CellManager.ILtex`.
pub trait Ltex {
    fn intv(&self) -> i64;
    fn path(&self) -> &str;
}

/// C# `CellManager.ILigh`.
pub trait Ligh {
    fn radius(&self) -> f32;
    fn light_color(&self) -> u32;
}

/// C# `CellManager.IQuery` — the world database.
pub trait Query {
    fn meter_in_units(&self) -> f32;
    fn cell_length_in_meters(&self) -> f32;
    /// C# `Radius` — `[0]` is the load radius, `[1]` the visible radius.
    ///
    /// The C# indexes this array directly in the constructor
    /// (`query.Radius[0]`, `query.Radius[1]`), so a shorter array throws there.
    /// Split into two methods so the shape cannot be wrong.
    fn load_radius(&self) -> i32;
    fn visible_radius(&self) -> i32;
    fn world(&self) -> i32;
    fn set_world(&mut self, world: i32);
    fn get_cell_id(&self, point: Vec3) -> Int3;
    fn find_cell(&self, cell: Int3) -> Option<Box<dyn Cell>>;
    fn find_cell_by_name(&self, name: &str) -> Option<Box<dyn Cell>>;
    fn find_land(&self, cell: Int3) -> Option<Box<dyn Land>>;
    fn find_ltex(&self, index: i32) -> Option<Box<dyn Ltex>>;
}

/// C# `abstract class CellBuilder`.
///
/// The C# pairs an untyped abstract base (`(object, object) CreateContainers`)
/// with a generic subclass that casts back — an artefact of needing a
/// non-generic handle for `CellManager` to hold. Rust parameterises
/// `CellManager` by `B: Backend` instead, so one trait suffices and no cast
/// is needed anywhere.
pub trait CellBuilder<B: Backend> {
    /// C# `CreateContainers(string name)` -> `(cell object, objects object)`.
    fn create_containers(&mut self, name: &str) -> (B::Object, B::Object);

    /// C# `SetVisible(object src, bool visible)`.
    fn set_visible(&mut self, src: &B::Object, visible: bool);

    /// C# `Destroy(object src)`.
    fn destroy(&mut self, src: &B::Object);

    /// C# `Coroutine(...)` — one step of cell construction; `false` when done.
    ///
    /// The C# returns `IAsyncEnumerator<object>` and the queue awaits
    /// `MoveNextAsync`. Kept synchronous and steppable: the async-runtime
    /// decision is still open (see PORTING.md), and cell building is CPU work
    /// interleaved with the frame, not I/O waiting.
    fn step(&mut self, handle: &mut CellBuild<B>) -> bool;

    /// C# `GfxAttach` pass-through, used when attaching a light to a model.
    fn attach(&mut self, _mode: GfxAttach, _child: &B::Object, _parent: &B::Object) {}
}

/// In-progress construction state for one cell. Replaces the C#'s
/// `IAsyncEnumerator<object>` handle.
pub struct CellBuild<B: Backend> {
    pub obj: B::Object,
    pub objects_obj: B::Object,
    pub grid_id: Int3,
    pub is_interior: bool,
    /// How many `step` calls have run. Builders use this to resume.
    pub progress: usize,
    pub done: bool,
}

/// C# `CellManager.Cell`.
pub struct LoadedCell<B: Backend> {
    pub build: CellBuild<B>,
    pub record: Box<dyn Cell>,
}

/// C# `class CellManager(IQuery, AsyncCoroutineQueue, CellBuilder)`.
pub struct CellManager<B: Backend, Q: Query, CB: CellBuilder<B>> {
    query: Q,
    builder: CB,
    cells: HashMap<Int3, LoadedCell<B>>,
    radius: i32,
    radius2: i32,
}

impl<B: Backend, Q: Query, CB: CellBuilder<B>> CellManager<B, Q, CB> {
    pub fn new(query: Q, builder: CB) -> Self {
        let radius = query.load_radius();
        let radius2 = query.visible_radius();
        Self { query, builder, cells: HashMap::new(), radius, radius2 }
    }

    pub fn query(&self) -> &Q {
        &self.query
    }

    pub fn loaded_len(&self) -> usize {
        self.cells.len()
    }

    /// C# `BeginCell(Int3 point)`.
    pub fn begin_cell(&mut self, point: Int3) -> bool {
        let Some(record) = self.query.find_cell(point) else {
            return false;
        };
        let cell = self.build_cell(record);
        self.cells.insert(point, cell);
        true
    }

    /// C# `BeginCellByName(string name)`.
    ///
    /// Keys by the record's own `grid_id`, not `Int3::ZERO` — see bug 1.
    pub fn begin_cell_by_name(&mut self, name: &str) -> bool {
        let Some(record) = self.query.find_cell_by_name(name) else {
            return false;
        };
        let key = record.grid_id();
        let cell = self.build_cell(record);
        self.cells.insert(key, cell);
        true
    }

    /// C# `BuildCell(ICell cell)`.
    fn build_cell(&mut self, record: Box<dyn Cell>) -> LoadedCell<B> {
        // C# `Debug.Assert(cell != null)` — unrepresentable here.
        let name = if record.is_interior() {
            format!("cell {}", record.name())
        } else {
            format!("cell {}", record.grid_id())
        };
        let (obj, objects_obj) = self.builder.create_containers(&name);
        LoadedCell {
            build: CellBuild {
                obj,
                objects_obj,
                grid_id: record.grid_id(),
                is_interior: record.is_interior(),
                progress: 0,
                done: false,
            },
            record,
        }
    }

    /// C# `UpdateCells(Vector3 position, bool immediate, int radius)`.
    ///
    /// Walks rings outward from the player so nearby cells load first, visiting
    /// each position once — see bug 4.
    pub fn update_cells(&mut self, position: Vec3, immediate: bool, radius: Option<i32>) {
        let radius = radius.unwrap_or(self.radius);
        let point = self.query.get_cell_id(position);
        let world = self.query.world();

        // Evict anything outside the square, collecting first so the map is not
        // mutated mid-iteration (the C# does the same).
        let out_of_range: Vec<Int3> = self
            .cells
            .keys()
            .filter(|s| {
                (s.x - point.x).abs() > radius || (s.y - point.y).abs() > radius
            })
            .copied()
            .collect();
        for s in out_of_range {
            self.destroy_cell(s);
        }

        // Ring by ring, nearest first.
        for r in 0..=radius {
            for p in ring(point, r, world) {
                if self.cells.contains_key(&p) {
                    continue;
                }
                if self.begin_cell(p) && immediate {
                    self.finish_cell(p);
                }
            }
        }

        // Visibility by Chebyshev distance.
        let keys: Vec<Int3> = self.cells.keys().copied().collect();
        for p in keys {
            let d = (point.x - p.x).abs().max((point.y - p.y).abs());
            let visible = d <= self.radius2;
            if let Some(c) = self.cells.get(&p) {
                let obj = &c.build.objects_obj;
                // Clone the handle so the builder borrow does not alias the map.
                let obj = obj.clone();
                self.builder.set_visible(&obj, visible);
            }
        }
    }

    /// Advance every in-progress cell by one step. Replaces the C#'s
    /// `AsyncCoroutineQueue.Run` for this workload.
    ///
    /// Each cell is removed from the map, stepped, and reinserted. That is what
    /// keeps `&mut self.builder` from aliasing `&mut self.cells`, and it also
    /// means a builder is free to look at the manager's other cells if it ever
    /// needs to.
    pub fn step_cells(&mut self) -> usize {
        let keys: Vec<Int3> = self
            .cells
            .iter()
            .filter(|(_, c)| !c.build.done)
            .map(|(k, _)| *k)
            .collect();
        let mut stepped = 0;
        for k in keys {
            if let Some(mut c) = self.cells.remove(&k) {
                let more = self.builder.step(&mut c.build);
                c.build.done = !more;
                self.cells.insert(k, c);
                stepped += 1;
            }
        }
        stepped
    }

    /// C# `Queue.WaitFor(cell.Task)` — run one cell to completion.
    pub fn finish_cell(&mut self, point: Int3) {
        let Some(mut c) = self.cells.remove(&point) else { return };
        while !c.build.done {
            let more = self.builder.step(&mut c.build);
            c.build.done = !more;
        }
        self.cells.insert(point, c);
    }

    /// C# `DestroyCell(Int3 point)`.
    ///
    /// Returns whether a cell was actually there. The C# logs
    /// `Log.Error("Tried to destroy a cell that is not created.")` on a miss —
    /// through the `Log` whose sink defaults to null and throws, per the
    /// `polyfills` findings.
    pub fn destroy_cell(&mut self, point: Int3) -> bool {
        match self.cells.remove(&point) {
            Some(c) => {
                self.builder.destroy(&c.build.obj);
                true
            }
            None => false,
        }
    }

    /// C# `DestroyAllCells()`.
    pub fn destroy_all_cells(&mut self) {
        let cells = std::mem::take(&mut self.cells);
        for (_, c) in cells {
            self.builder.destroy(&c.build.obj);
        }
    }
}

/// Positions at exactly Chebyshev distance `r` from `center`, in the `world` plane.
fn ring(center: Int3, r: i32, world: i32) -> Vec<Int3> {
    if r == 0 {
        return vec![Int3::new(center.x, center.y, world)];
    }
    let mut out = Vec::with_capacity((8 * r) as usize);
    for x in (center.x - r)..=(center.x + r) {
        out.push(Int3::new(x, center.y - r, world));
        out.push(Int3::new(x, center.y + r, world));
    }
    for y in (center.y - r + 1)..(center.y + r) {
        out.push(Int3::new(center.x - r, y, world));
        out.push(Int3::new(center.x + r, y, world));
    }
    out
}

/// C# `interface IDatabase`.
///
/// Both members take and return `object`, so nothing about the interface is
/// checkable. It has no implementors in the solution; ported as a generic trait
/// so an implementor states its own types.
pub trait Database {
    type Src;
    type Converted;
    type Queried;
    fn convert(&self, src: Self::Src) -> Self::Converted;
    fn query(&self, src: Self::Src) -> Self::Queried;
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn ring_zero_is_the_center_only() {
        let c = Int3::new(3, 4, 0);
        assert_eq!(ring(c, 0, 7), vec![Int3::new(3, 4, 7)]);
    }

    #[test]
    fn ring_has_eight_r_positions_and_all_at_distance_r() {
        let c = Int3::new(0, 0, 0);
        for r in 1..=4 {
            let ps = ring(c, r, 0);
            assert_eq!(ps.len(), (8 * r) as usize, "ring {r} size");
            for p in &ps {
                let d = p.x.abs().max(p.y.abs());
                assert_eq!(d, r, "{p:?} not at distance {r}");
            }
        }
    }

    #[test]
    fn rings_cover_the_square_exactly_once() {
        // This is the property the C#'s radius+1 nested passes achieve by
        // brute force; verify the direct walk matches.
        let c = Int3::new(2, -3, 0);
        let radius = 4;
        let mut seen = std::collections::HashSet::new();
        for r in 0..=radius {
            for p in ring(c, r, 0) {
                assert!(seen.insert((p.x, p.y)), "{p:?} visited twice");
            }
        }
        let expected = ((2 * radius + 1) * (2 * radius + 1)) as usize;
        assert_eq!(seen.len(), expected, "must cover the full square");
    }

    #[test]
    fn rings_are_ordered_nearest_first() {
        let c = Int3::new(0, 0, 0);
        let mut last = -1;
        for r in 0..=3 {
            for p in ring(c, r, 0) {
                let d = p.x.abs().max(p.y.abs());
                assert!(d >= last, "ordering regressed");
                last = d;
            }
        }
    }

    #[test]
    fn ring_uses_the_world_plane_for_z() {
        for p in ring(Int3::new(0, 0, 0), 2, 9) {
            assert_eq!(p.z, 9);
        }
    }
}
