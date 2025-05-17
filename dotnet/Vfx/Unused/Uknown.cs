namespace OpenStack.Vfx;

//        using FileStream input = new(root, FileMode.Open);
//        using FileStream output = new(@"E:\ArchiveLibrary\Rockstar\T_\file.dat", FileMode.Create);
//        var decoder = new SevenZip.Compression.LZMA.Decoder();
//        var properties = new byte[5];
//        if (input.Read(properties, 0, 5) != 5) throw new Exception("input .lzma is too short");
//        decoder.SetDecoderProperties(properties);
//        var outSize = 0L;
//        for (var i = 0; i < 8; i++) {
//            var v = input.ReadByte();
//            if (v < 0) throw (new Exception("Can't Read 1"));
//            outSize |= ((long)(byte)v) << (8 * i);
//        }
//        var compressedSize = input.Length - input.Position;
//        decoder.Code(input, output, compressedSize, outSize, null);
