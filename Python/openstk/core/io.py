import os

# CircularBuffer
class CircularBuffer:
    def __init__(self, size: int = 4096):
        self._buffer = bytearray(size)
        self._head: int = 0
        self._tail: int = 0
        self.length: int = 0

    def get(self, index: int) -> int: return self._buffer[(self._head + index) % len(self._buffer)]

    def clear(self) -> None:
        self._head = 0
        self._tail = 0
        self.length = 0

    def _setCapacity(self, capacity: int) -> None:
        newBuffer = bytearray(capacity)
        if self.length > 0:
            if self._head < self._tail: self._buffer.AsSpan(self._head, self.length).CopyTo(newBuffer.AsSpan())
            else: self._buffer.AsSpan(self._head, len(self._buffer) - self._head).CopyTo(newBuffer.AsSpan()); self._buffer.AsSpan(0, self._tail).CopyTo(newBuffer.AsSpan(len(self._buffer) - self._head))
        self._head = 0
        self._tail = self.length
        self._buffer = newBuffer

    def enqueue(self, buffer: bytearray, offset: int = 0, size: int = 0):
        if self.length + size >= len(self._buffer): self._setCapacity((self.length + size + 2047) & ~2047)
        if self._head < self._tail:
            rightLength = len(self._buffer) - self._tail
            if rightLength >= size: self.buffer.Slice(offset, size).CopyTo(self._buffer.AsSpan(self._tail))
            else: buffer.Slice(offset, rightLength).CopyTo(self._buffer.AsSpan(self._tail)); buffer.Slice(offset + rightLength, size - drightLength).CopyTo(self._buffer.AsSpan())
        else: buffer.Slice(offset, size).CopyTo(self._buffer.AsSpan(self._tail))
        self._tail = (self._tail + size) % len(self._buffer)
        self.length += size

# Huffman
class Huffman:
    _decTree: list[int] = [
        1,    2,
        3,    4,
        5,    0,
        6,    7,
        8,    9,
        10,   11,
        12,   13,
        -256, 14,
        15,   16,
        17,   18,
        19,   20,
        21,   22,
        -1,   23,
        24,   25,
        26,   27,
        28,   29,
        30,   31,
        32,   33,
        34,   35,
        36,   37,
        38,   39,
        40,   -64,
        41,   42,
        43,   44,
        -6,   45,
        46,   47,
        48,   49,
        50,   51,
        -119, 52,
        -32,  53,
        54,   -14,
        55,   -5,
        56,   57,
        58,   59,
        60,   -2,
        61,   62,
        63,   64,
        65,   66,
        67,   68,
        69,   70,
        71,   72,
        -51,  73,
        74,   75,
        76,   77,
        -101, -111,
        -4,   -97,
        78,   79,
        -110, 80,
        81,   -116,
        82,   83,
        84,   -255,
        85,   86,
        87,   88,
        89,   90,
        -15,  -10,
        91,   92,
        -21,  93,
        -117, 94,
        95,   96,
        97,   98,
        99,   100,
        -114, 101,
        -105, 102,
        -26,  103,
        104,  105,
        106,  107,
        108,  109,
        110,  111,
        112,  -3,
        113,  -7,
        114,  -131,
        115,  -144,
        116,  117,
        -20,  118,
        119,  120,
        121,  122,
        123,  124,
        125,  126,
        127,  128,
        129,  -100,
        130,  -8,
        131,  132,
        133,  134,
        -120, 135,
        136,  -31,
        137,  138,
        -109, -234,
        139,  140,
        141,  142,
        143,  144,
        -112, 145,
        -19,  146,
        147,  148,
        149,  -66,
        150,  -145,
        -13,  -65,
        151,  152,
        153,  154,
        -30,  155,
        156,  157,
        -99,  158,
        159,  160,
        161,  162,
        -23,  163,
        -29,  164,
        -11,  165,
        166,  -115,
        167,  168,
        169,  170,
        -16,  171,
        -34,  172,
        173,  -132,
        174,  -108,
        175,  -22,
        176,  -9,
        177,  -84,
        -17,  -37,
        -28,  178,
        179,  180,
        181,  182,
        183,  184,
        185,  186,
        187,  -104,
        188,  -78,
        189,  -61,
        -79,  -178,
        -59,  -134,
        190,  -25,
        -83,  -18,
        191,  -57,
        -67,  192,
        -98,  193,
        -12,  -68,
        194,  195,
        -55,  -128,
        -24,  -50,
        -70,  196,
        -94,  -33,
        197,  -129,
        -74,  198,
        -82,  199,
        -56,  -87,
        -44,  200,
        -248, 201,
        -163, -81,
        -52,  -123,
        202,  -113,
        -48,  -41,
        -122, -40,
        203,  -90,
        -54,  204,
        -86,  -192,
        205,  206,
        207,  -130,
        -53,  208,
        -133, -45,
        209,  210,
        211,  -91,
        212,  213,
        -106, -88,
        214,  215,
        216,  217,
        218,  -49,
        219,  220,
        221,  222,
        223,  224,
        225,  226,
        227,  -102,
        -160, 228,
        -46,  229,
        -127, 230,
        -103, 231,
        232,  233,
        -60,  234,
        235,  -76,
        236,  -121,
        237,  -73,
        -149, 238,
        239,  -107,
        -35,  240,
        -71,  -27,
        -69,  241,
        -89,  -77,
        -62,  -118,
        -75,  -85,
        -72,  -58,
        -63,  -80,
        242,  -42,
        -150, -157,
        -139, -236,
        -126, -243,
        -142, -214,
        -138, -206,
        -240, -146,
        -204, -147,
        -152, -201,
        -227, -207,
        -154, -209,
        -153, -254,
        -176, -156,
        -165, -210,
        -172, -185,
        -195, -170,
        -232, -211,
        -219, -239,
        -200, -177,
        -175, -212,
        -244, -143,
        -246, -171,
        -203, -221,
        -202, -181,
        -173, -250,
        -184, -164,
        -193, -218,
        -199, -220,
        -190, -249,
        -230, -217,
        -169, -216,
        -191, -197,
        -47,  243,
        244,  245,
        246,  247,
        -148, -159,
        248,  249,
        -92,  -93,
        -96,  -225,
        -151, -95,
        250,  251,
        -241, 252,
        -161, -36,
        253,  254,
        -135, -39,
        -187, -124,
        255,  -251,
        -162, -238,
        -242, -38,
        -43,  -125,
        -215, -253,
        -140, -208,
        -137, -235,
        -158, -237,
        -136, -205,
        -155, -141,
        -228, -229,
        -213, -168,
        -224, -194,
        -196, -226,
        -183, -233,
        -231, -167,
        -174, -189,
        -252, -166,
        -198, -222,
        -188, -179,
        -223, -182,
        -180, -186,
        -245, -247]

    def __init__(self):
        self._bitNum: int = 8
        self._value: int = 0; self._mask: int = 0; self._treePos: int = 0

    def reset(self) -> None:
        self._bitNum = 8
        self._value = 0
        self._mask = 0
        self._treePos = 0
    
    def decompress(self, src: bytearray, dest: bytearray, size: list[int]) -> bool:
        destIndex = 0
        dest.clear()
        while True:
            if self._bitNum >= 8:
                if len(src) == 0: size[0] = destIndex; return True
                self._value = src[0]
                src = src[1:]
                self._bitNum = 0
                self._mask = 0x80
            self._treePos = self._decTree[self._treePos * 2 if (self._value & self._mask) != 0 else self._treePos * 2 + 1]
            self._mask >>= 1
            self._bitNum += 1
            if self._treePos <= 0:
                if self._treePos == -256: self._bitNum = 8; self._treePos = 0; continue
                if destIndex == size: return False
                dest[destIndex] = (-self._treePos) & 0xFF; destIndex += 1
                self._treePos = 0