/*
 * Copyright 2013 09 05 Unlimited Fate Works 
 * Author: l46kok
 * 
 * Data Blocks of a replay file
 * Follows Section 3.0 of W3G format standard
 * http://w3g.deepnode.de/files/w3g_format.txt
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.Data
{
    public class DataBlock
    {
        public ushort CompressedDataBlockSize { get; set; } //Excluding header
        public ushort DecompressedDataBlockSize { get; set; } //Should always be 8k
        public uint CheckSum { get; set; } // Presumably checksum
        public byte[] CompressedDataBlockBytes { get; set; } //Compressed using ZLib
        public byte[] DecompressedDataBlockBytes { get; set; }
    }
}
