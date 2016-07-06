using System;
using System.IO;
using System.Text;
using ReplayParser.Data;
using ReplayParser.Utility;
using System.Linq;

namespace ReplayParser.Parser
{
    public class FateReplayHeaderParser
    {
        private const int HEADER_SIZE = 68; //0x44
        //Zero Terminated String for "Warcraft III recorded game\0x1A\0"
        private static readonly byte[] REPLAY_HEADER_ST_BYTES = new byte[]
            {
                0x57, 0x61, 0x72, 0x63, 0x72, 0x61, 0x66, 0x74, 0x20, 0x49, 0x49, 0x49, 0x20, 0x72, 
                0x65, 0x63, 0x6f, 0x72, 0x64, 0x65, 0x64, 0x20, 0x67, 0x61, 0x6d, 0x65, 0x1a, 0x00
            };
        private const uint FILE_OFFSET_FIRST_COMPRESSED_DATA_BLOCK = 0x00000044; // Offset is 0x44 for WC3 >= 1.07
        private const uint SUPPORTED_REPLAY_HEADER_VERSION = 0x00000001; // Warcraft III Patch >= 1.07 and TFT 
        private const string SUPPORTED_WC3_CLIENT_TYPE = "PX3W"; //W3XP in little-endian format
        private const uint FLAG_MULTIPLAYER = 0x8000;
        private const uint FLAG_SINGLEPLAYER = 0x0000;

        //Error Strings
        private const string ERROR_INVALID_HEADER_START_STRING = "Replay file's header start string doesn't match. Input: {0}";
        private const string ERROR_INVALID_REPLAY_FILE_OFFSET = "Invalid Replay File Offset (Expected 0x44). Input: {0}";
        private const string ERROR_INVALID_REPLAY_HEADER_VERSION = "Invalid Replay Header Version (Supports >= 1.07. Expected 0x01). Input: {0}";
        private const string ERROR_INVALID_REPLAY_CLIENT_TYPE =
            "Invaid Replay Client Type (Supports The Frozen Throne. Expected W3XP). Input: {0}";

        private const string ERROR_INVALID_REPLAY_VERSION =
            "Invalid Replay Version (Expected between 1.00 - 2.00). Input : {0}";

        private const string ERROR_INVALID_GAME_TYPE = "Invalid game type. Only supports multiplayer games. Input: {0}";

        public FateReplayHeaderParser()
        {
        }

        public ReplayHeader ParseReplayHeader(byte[] totalReplayBytes, out int currIndex)
        {
            currIndex = 0;
            byte[] replayHeaderBytes = totalReplayBytes.SubArray(0, HEADER_SIZE);
            ReplayHeader parsedReplayHeader = new ReplayHeader(replayHeaderBytes);

            //Check if the replay file starts with the expected header string of "Warcraft III recorded game"
            byte[] replayStartHeader = replayHeaderBytes.SubArray(0, REPLAY_HEADER_ST_BYTES.Length);
            if (!replayStartHeader.SequenceEqual(REPLAY_HEADER_ST_BYTES))
            {                
                throw new InvalidDataException(String.Format(ERROR_INVALID_HEADER_START_STRING, Encoding.UTF8.GetString(replayStartHeader)));
            }
            currIndex += REPLAY_HEADER_ST_BYTES.Length; //Move 26 characters forward
            
            //Get file offset
            uint replayFileOffset = ByteUtility.ReadDoubleWord(replayHeaderBytes, currIndex);
            if (replayFileOffset != FILE_OFFSET_FIRST_COMPRESSED_DATA_BLOCK)
            {
                throw new InvalidDataException(String.Format(ERROR_INVALID_REPLAY_FILE_OFFSET,
                                               BitConverter.ToString(BitConverter.GetBytes(replayFileOffset))));
            }
            currIndex += 4;

            //Get overall size of compressed file
            uint compressedFileSize = ByteUtility.ReadDoubleWord(replayHeaderBytes, currIndex);
            parsedReplayHeader.CompressedFileSize = compressedFileSize;
            currIndex += 4;

            //Get replay version
            uint replayHeaderVersion = ByteUtility.ReadDoubleWord(replayHeaderBytes, currIndex);
            if (replayHeaderVersion != SUPPORTED_REPLAY_HEADER_VERSION)
            {
                throw new InvalidDataException(String.Format(ERROR_INVALID_REPLAY_HEADER_VERSION,
                                               BitConverter.ToString(BitConverter.GetBytes(replayHeaderVersion))));
            }
            currIndex += 4;
            
            //Get overall size of decompressed file
            uint decompressedFileSize = ByteUtility.ReadDoubleWord(replayHeaderBytes, currIndex);
            parsedReplayHeader.DecompressedFileSize = decompressedFileSize;
            currIndex += 4;

            //Get total number of compressed data blocks in file
            uint compressedDataBlockCount = ByteUtility.ReadDoubleWord(replayHeaderBytes, currIndex);
            parsedReplayHeader.CompressedDataBlockCount = (int) compressedDataBlockCount;
            currIndex += 4;

            //Start SubHeaderParsing (Version 1)
            //Get version identifier (Classic, TFT)
            string clientType = ByteUtility.ReadDoubleWordString(replayHeaderBytes, currIndex);
            if (clientType != SUPPORTED_WC3_CLIENT_TYPE)
            {
                throw new InvalidDataException(String.Format(ERROR_INVALID_REPLAY_CLIENT_TYPE,clientType));
            }
            currIndex += 4;

            //Get Client Version Number
            string replayVersion = ByteUtility.ReadDoubleWordString(replayHeaderBytes, currIndex);
            double replayVersionValue = 0;
            Double.TryParse(replayVersion, out replayVersionValue);
            //For some reason, replays generated by GHost doesn't follow the standard replay format
            //This part is commented out until I figure out what's going on

            //if (replayVersionValue < 1.0 && replayVersionValue > 2.0)
            //{
            //    throw new InvalidDataException(String.Format(ERROR_INVALID_REPLAY_VERSION, replayVersion));
            //}
            parsedReplayHeader.ReplayVersion = replayVersion;
            currIndex += 4;

            ushort buildNumber = ByteUtility.ReadWord(replayHeaderBytes, currIndex);
            parsedReplayHeader.BuildNumber = buildNumber;
            currIndex += 2;

            ushort flag = ByteUtility.ReadWord(replayHeaderBytes, currIndex);
            if (flag != FLAG_MULTIPLAYER)
            {
                //throw new InvalidDataException(String.Format(ERROR_INVALID_GAME_TYPE, BitConverter.ToString(BitConverter.GetBytes(flag))));
            }
            currIndex += 2;

            uint replayLength = ByteUtility.ReadDoubleWord(replayHeaderBytes, currIndex);
            parsedReplayHeader.ReplayLength = replayLength;
            currIndex += 4;

            uint checksum = ByteUtility.ReadDoubleWord(replayHeaderBytes, currIndex);
            parsedReplayHeader.CRC32 = checksum;
            currIndex += 4;

            return parsedReplayHeader;
        }

    }
}