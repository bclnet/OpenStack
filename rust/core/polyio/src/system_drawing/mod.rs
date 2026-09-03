// mirrors dotnet folder `System.Drawing` — see PORT_MAP.tsv
//
// A note that applies to this whole folder: in the C# these files are
// overwhelmingly commented out. Live-vs-commented line counts:
//
//   BoundingBox      22 / 35        Curve       32 / 46
//   BoundingFrustum  20 / 33        Point3D     24 / 37
//   BoundingSphere   26 / 35        Ray         22 / 35
//   Rectangle         1 / 404
//
// Every `Intersects`, `Contains`, and `CreateFrom*` in the folder is commented
// out — there is not one live intersection routine. What remains is plain data
// plus `Equals`/`GetHashCode`/`ToString`. The ports match: data types with
// derived traits, and the geometry left to `glam` where callers need it.
pub mod bounding_box;
pub mod bounding_frustum;
pub mod bounding_sphere;
pub mod curve;
pub mod point3_d;
pub mod ray;
pub mod rectangle;
