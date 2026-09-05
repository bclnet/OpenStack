// PORT-SOURCE: Core/OpenStack.PolyIO/System.Drawing/Rectangle.cs
// PORT-SHA: 4e382e50fb2843b0
// PORT-STATUS: done
//
// The C# file is 29KB and has ONE live line — a namespace declaration. All 404
// remaining lines are commented out.
//
// The 134 `Rectangle` references elsewhere in the solution therefore resolve to
// the BCL `System.Drawing.Rectangle`, not to this file. This file is inert.
//
// A minimal integer rectangle is provided so ported call sites have something
// to bind to, matching BCL `Rectangle` semantics (x/y/width/height, with
// `right`/`bottom` exclusive). If the commented C# is ever revived, port that
// instead and reconcile.

use bytemuck::{Pod, Zeroable};

/// Integer rectangle, matching BCL `System.Drawing.Rectangle`.
#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Hash, Pod, Zeroable)]
pub struct Rectangle {
    pub x: i32,
    pub y: i32,
    pub width: i32,
    pub height: i32,
}

impl Rectangle {
    pub const EMPTY: Self = Self { x: 0, y: 0, width: 0, height: 0 };

    pub const fn new(x: i32, y: i32, width: i32, height: i32) -> Self {
        Self { x, y, width, height }
    }

    /// BCL `FromLTRB` — right and bottom are exclusive.
    pub const fn from_ltrb(left: i32, top: i32, right: i32, bottom: i32) -> Self {
        Self { x: left, y: top, width: right - left, height: bottom - top }
    }

    #[inline]
    pub const fn left(&self) -> i32 {
        self.x
    }

    #[inline]
    pub const fn top(&self) -> i32 {
        self.y
    }

    /// Exclusive, as in the BCL.
    #[inline]
    pub const fn right(&self) -> i32 {
        self.x + self.width
    }

    /// Exclusive, as in the BCL.
    #[inline]
    pub const fn bottom(&self) -> i32 {
        self.y + self.height
    }

    /// BCL `IsEmpty` — true when *any* dimension is zero or negative.
    #[inline]
    pub const fn is_empty(&self) -> bool {
        self.width <= 0 || self.height <= 0
    }

    /// BCL `Contains(int, int)`.
    #[inline]
    pub const fn contains(&self, x: i32, y: i32) -> bool {
        x >= self.x && x < self.right() && y >= self.y && y < self.bottom()
    }

    /// BCL `IntersectsWith`.
    #[inline]
    pub const fn intersects(&self, o: &Self) -> bool {
        o.x < self.right() && self.x < o.right() && o.y < self.bottom() && self.y < o.bottom()
    }

    /// BCL `Intersect` — the overlap, or `EMPTY` when there is none.
    pub fn intersect(&self, o: &Self) -> Self {
        let (l, t) = (self.x.max(o.x), self.y.max(o.y));
        let (r, b) = (self.right().min(o.right()), self.bottom().min(o.bottom()));
        if r <= l || b <= t {
            Self::EMPTY
        } else {
            Self::from_ltrb(l, t, r, b)
        }
    }

    /// BCL `Union` — the smallest rectangle covering both.
    pub fn union(&self, o: &Self) -> Self {
        Self::from_ltrb(
            self.x.min(o.x),
            self.y.min(o.y),
            self.right().max(o.right()),
            self.bottom().max(o.bottom()),
        )
    }
}

impl std::fmt::Display for Rectangle {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "{{X={},Y={},Width={},Height={}}}",
            self.x, self.y, self.width, self.height
        )
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn edges_are_exclusive_on_the_far_side() {
        let r = Rectangle::new(0, 0, 10, 10);
        assert!(r.contains(0, 0));
        assert!(r.contains(9, 9));
        assert!(!r.contains(10, 10), "right/bottom are exclusive");
    }

    #[test]
    fn intersect_returns_the_overlap() {
        let a = Rectangle::new(0, 0, 10, 10);
        let b = Rectangle::new(5, 5, 10, 10);
        assert_eq!(a.intersect(&b), Rectangle::new(5, 5, 5, 5));
    }

    #[test]
    fn disjoint_rectangles_intersect_to_empty() {
        let a = Rectangle::new(0, 0, 2, 2);
        let b = Rectangle::new(50, 50, 2, 2);
        assert!(!a.intersects(&b));
        assert!(a.intersect(&b).is_empty());
    }

    #[test]
    fn union_covers_both() {
        let a = Rectangle::new(0, 0, 2, 2);
        let b = Rectangle::new(8, 8, 2, 2);
        assert_eq!(a.union(&b), Rectangle::new(0, 0, 10, 10));
    }

    #[test]
    fn touching_edges_do_not_intersect() {
        let a = Rectangle::new(0, 0, 5, 5);
        let b = Rectangle::new(5, 0, 5, 5);
        assert!(!a.intersects(&b));
    }
}
