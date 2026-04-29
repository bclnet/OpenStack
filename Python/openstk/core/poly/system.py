import sys
from numpy import ndarray, nditer

# Calculates the minimum and maximum values of a 2D array.
def getExtrema(source: ndarray) -> tuple[float, float]:
    min0 = sys.float_info.max; max0 = sys.float_info.min
    for s in nditer(source): min0 = min(min0, s); max0 = max(max0, s)
    return (min0, max0)

def changeRange(x: float, min0: float, max0: float, min1: float, max1: float) -> float:
    r0 = max0 - min0; r1 = max1 - min1; p0 = (x - min0) / r0 if r0 != 0.0 else 0.0
    return min1 + (p0 * r1)