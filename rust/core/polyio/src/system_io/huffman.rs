// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/Huffman.cs
// PORT-SHA: da751abc8e5773a4
// PORT-STATUS: done
//
// Fixed-table Huffman decoder. The 512-entry tree is a hard-coded constant in
// the C# source and was transcribed mechanically, not by hand.
//
// Table encoding: index `treepos * 2` (+ 1 when the current bit is clear)
// yields the next node. A positive value is another node index; a value <= 0 is
// a leaf whose emitted byte is its negation. -256 is the reset sentinel.

/// C# `class Huffman` — decoder state carried across calls to
/// [`decompress`](Huffman::decompress), so a stream can be fed in chunks.
#[derive(Debug, Clone)]
pub struct Huffman {
    bit_num: i32,
    value: i32,
    mask: i32,
    tree_pos: i32,
}

impl Default for Huffman {
    fn default() -> Self {
        // C# field initialiser is `int _bitNum = 8`, not 0 — starting at 8
        // forces a byte load on the first iteration.
        Self { bit_num: 8, value: 0, mask: 0, tree_pos: 0 }
    }
}

impl Huffman {
    pub fn new() -> Self {
        Self::default()
    }

    /// C# `Reset()`.
    pub fn reset(&mut self) {
        self.bit_num = 8;
        self.value = 0;
        self.mask = 0;
        self.tree_pos = 0;
    }

    /// C# `bool Decompress(Span<byte> src, Span<byte> dest, ref int size)`.
    ///
    /// Returns `Ok(written)` when the input was fully consumed, or
    /// `Err(DestinationFull)` when `dest` filled first — matching the C#
    /// `true`/`false` return, with the byte count folded into the success case
    /// instead of the `ref int size` in/out parameter.
    ///
    /// Deviation: C# calls `dest.Clear()` first, zeroing the whole buffer on
    /// every call. That is observable only for bytes past the decoded length,
    /// which no caller reads, so it is skipped here.
    pub fn decompress(&mut self, src: &[u8], dest: &mut [u8]) -> Result<usize, HuffmanError> {
        let mut dest_index = 0usize;
        let mut src = src;
        loop {
            if self.bit_num >= 8 {
                if src.is_empty() {
                    return Ok(dest_index);
                }
                self.value = src[0] as i32;
                src = &src[1..];
                self.bit_num = 0;
                self.mask = 0x80;
            }
            let idx = if self.value & self.mask != 0 {
                self.tree_pos * 2
            } else {
                self.tree_pos * 2 + 1
            };
            self.tree_pos = DEC_TREE[idx as usize];
            self.mask >>= 1;
            self.bit_num += 1;
            if self.tree_pos <= 0 {
                if self.tree_pos == -256 {
                    // Reset sentinel: realign to the next byte boundary.
                    self.bit_num = 8;
                    self.tree_pos = 0;
                    continue;
                }
                if dest_index == dest.len() {
                    return Err(HuffmanError::DestinationFull { written: dest_index });
                }
                dest[dest_index] = (-self.tree_pos) as u8;
                dest_index += 1;
                self.tree_pos = 0;
            }
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum HuffmanError {
    /// `dest` filled before the input was consumed. C# returned `false`.
    DestinationFull { written: usize },
}

impl std::fmt::Display for HuffmanError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            HuffmanError::DestinationFull { written } => {
                write!(f, "destination full after {written} bytes")
            }
        }
    }
}

impl std::error::Error for HuffmanError {}

/// C# `static readonly int[] _decTree` — 512 entries, transcribed verbatim.
#[rustfmt::skip]
static DEC_TREE: [i32; 512] = [
    1, 2, 3, 4, 5, 0, 6, 7, 8, 9, 10, 11,
    12, 13, -256, 14, 15, 16, 17, 18, 19, 20, 21, 22,
    -1, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33,
    34, 35, 36, 37, 38, 39, 40, -64, 41, 42, 43, 44,
    -6, 45, 46, 47, 48, 49, 50, 51, -119, 52, -32, 53,
    54, -14, 55, -5, 56, 57, 58, 59, 60, -2, 61, 62,
    63, 64, 65, 66, 67, 68, 69, 70, 71, 72, -51, 73,
    74, 75, 76, 77, -101, -111, -4, -97, 78, 79, -110, 80,
    81, -116, 82, 83, 84, -255, 85, 86, 87, 88, 89, 90,
    -15, -10, 91, 92, -21, 93, -117, 94, 95, 96, 97, 98,
    99, 100, -114, 101, -105, 102, -26, 103, 104, 105, 106, 107,
    108, 109, 110, 111, 112, -3, 113, -7, 114, -131, 115, -144,
    116, 117, -20, 118, 119, 120, 121, 122, 123, 124, 125, 126,
    127, 128, 129, -100, 130, -8, 131, 132, 133, 134, -120, 135,
    136, -31, 137, 138, -109, -234, 139, 140, 141, 142, 143, 144,
    -112, 145, -19, 146, 147, 148, 149, -66, 150, -145, -13, -65,
    151, 152, 153, 154, -30, 155, 156, 157, -99, 158, 159, 160,
    161, 162, -23, 163, -29, 164, -11, 165, 166, -115, 167, 168,
    169, 170, -16, 171, -34, 172, 173, -132, 174, -108, 175, -22,
    176, -9, 177, -84, -17, -37, -28, 178, 179, 180, 181, 182,
    183, 184, 185, 186, 187, -104, 188, -78, 189, -61, -79, -178,
    -59, -134, 190, -25, -83, -18, 191, -57, -67, 192, -98, 193,
    -12, -68, 194, 195, -55, -128, -24, -50, -70, 196, -94, -33,
    197, -129, -74, 198, -82, 199, -56, -87, -44, 200, -248, 201,
    -163, -81, -52, -123, 202, -113, -48, -41, -122, -40, 203, -90,
    -54, 204, -86, -192, 205, 206, 207, -130, -53, 208, -133, -45,
    209, 210, 211, -91, 212, 213, -106, -88, 214, 215, 216, 217,
    218, -49, 219, 220, 221, 222, 223, 224, 225, 226, 227, -102,
    -160, 228, -46, 229, -127, 230, -103, 231, 232, 233, -60, 234,
    235, -76, 236, -121, 237, -73, -149, 238, 239, -107, -35, 240,
    -71, -27, -69, 241, -89, -77, -62, -118, -75, -85, -72, -58,
    -63, -80, 242, -42, -150, -157, -139, -236, -126, -243, -142, -214,
    -138, -206, -240, -146, -204, -147, -152, -201, -227, -207, -154, -209,
    -153, -254, -176, -156, -165, -210, -172, -185, -195, -170, -232, -211,
    -219, -239, -200, -177, -175, -212, -244, -143, -246, -171, -203, -221,
    -202, -181, -173, -250, -184, -164, -193, -218, -199, -220, -190, -249,
    -230, -217, -169, -216, -191, -197, -47, 243, 244, 245, 246, 247,
    -148, -159, 248, 249, -92, -93, -96, -225, -151, -95, 250, 251,
    -241, 252, -161, -36, 253, 254, -135, -39, -187, -124, 255, -251,
    -162, -238, -242, -38, -43, -125, -215, -253, -140, -208, -137, -235,
    -158, -237, -136, -205, -155, -141, -228, -229, -213, -168, -224, -194,
    -196, -226, -183, -233, -231, -167, -174, -189, -252, -166, -198, -222,
    -188, -179, -223, -182, -180, -186, -245, -247,
];

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn table_is_intact() {
        assert_eq!(DEC_TREE.len(), 512);
        assert_eq!(&DEC_TREE[..6], &[1, 2, 3, 4, 5, 0]);
        assert_eq!(&DEC_TREE[508..], &[-180, -186, -245, -247]);
        // Every node index must stay inside the table.
        for (i, &v) in DEC_TREE.iter().enumerate() {
            assert!(v * 2 + 1 < 512, "node {i} -> {v} escapes the table");
        }
    }

    #[test]
    fn empty_input_writes_nothing() {
        let mut h = Huffman::new();
        let mut dest = [0u8; 16];
        assert_eq!(h.decompress(&[], &mut dest).unwrap(), 0);
    }

    #[test]
    fn full_destination_is_reported() {
        let mut h = Huffman::new();
        let mut dest = [0u8; 0];
        // Any input that decodes at least one byte must report the overflow
        // rather than writing out of bounds.
        let r = h.decompress(&[0xFF, 0xFF, 0xFF, 0xFF], &mut dest);
        assert!(matches!(r, Err(HuffmanError::DestinationFull { written: 0 })));
    }

    #[test]
    fn reset_restores_initial_state() {
        let mut h = Huffman::new();
        let mut dest = [0u8; 8];
        let _ = h.decompress(&[0xAB, 0xCD], &mut dest);
        h.reset();
        let fresh = Huffman::new();
        assert_eq!(h.bit_num, fresh.bit_num);
        assert_eq!(h.tree_pos, fresh.tree_pos);
    }
}
