3dstool -xvt0f 3ds 0.cxi "Legend of Zelda, The - Tri Force Heroes (USA) (En,Fr,Es).3ds"
INFO: ncsd header is not extract
save: 0.cxi
INFO: partition 1 is not extract
INFO: partition 2 is not extract
INFO: partition 6 is not extract
INFO: partition 7 is not extract

3dstool -xvtf cxi 0.cxi --header ncchheader.bin --exh exh.bin --logo logo.bcma.lz --plain plain.bin --exefs exefs.bin --romfs romfs.bin
save: ncchheader.bin
save: exh.bin
save: logo.bcma.lz
save: plain.bin
save: exefs.bin
save: romfs.bin