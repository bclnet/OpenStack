# def marshalPSymbol(c: str) -> int:
#     #return 2 ** ('cxbhiq'.index(c.lower()) - 2)
#     match c.lower():
#         case 'c' | 'x' | 'b' | 's': return 1
#         case 'h': return 2
#         case 'i' | 'f': return 4
#         case 'q' | 'd': return 8
#         case _: raise Exception(f'Unknown PSymbol: {c}')

# def marshalPSize(p: str) -> int: # calcsize
#     c = p[0]; pLen = len(p)
#     if pLen == 1: return marshalPSymbol(c)
#     _ = 0; cnt = 0
#     for i in range(1 if c == '<' or c == '>' else 0, pLen):
#         c = p[i]
#         if c.isdigit(): cnt = cnt * 10 + ord(c) - 0x30; continue
#         elif cnt == 0: cnt = 1
#         size = marshalPSymbol(c)
#         _ += cnt if size <= 0 else size * cnt
#         cnt = 0
#     return _

# def marshalS(c: str) -> int:
#     #if sizeOf == size else _throw(f'Sizes are different: {sizeOf}|{size}')
#     pass

# def marshalSArray(c: str) -> int:
#     #if sizeOf == size else _throw(f'Sizes are different: {sizeOf}|{size}')
#     pass

def fixedAString(data: bytes, length: int) -> str: return data.decode('ascii').rstrip('\00')