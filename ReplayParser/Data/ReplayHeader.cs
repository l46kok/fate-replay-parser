/*
 * Copyright 2013 09 01 Unlimited Fate Works 
 * Author: l46kok
 * 
 * Header data struct for a given replay
 * See http://w3g.deepnode.de/files/w3g_format.txt for format information
 * Only stores Version 1 replays (Warcraft III patch >= 1.07)
 * 
 */


namespace FateReplayParser.Data
{
    public class ReplayHeader
    {

        //Total bytes
        private byte[] _replayHeaderBytes;

        public uint CompressedFileSize { get; set; }
        public uint DecompressedFileSize { get; set; }
        public int CompressedDataBlockCount { get; set; }
        public string ReplayVersion { get; set; }
        public ushort BuildNumber { get; set; }
        public uint ReplayLength { get; set; } // In milliseconds
        public uint CRC32 { get; set; }
        public ReplayHeader(byte[] replayHeaderBytes)
        {
            _replayHeaderBytes = replayHeaderBytes;
        }
    }
}
