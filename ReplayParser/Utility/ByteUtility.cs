using System;
using System.Collections.Generic;
using System.Text;

namespace FateReplayParser.Utility
{
    public static class ByteUtility
    {
        //Reverses byte order (To switch between little-endian, big-endian architecture)
        public static uint ReverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        //Reverses byte order
        public static ushort ReverseBytes(ushort value)
        {
            return (UInt16)((value >> 8) | (value << 8));
        }



        //Returns a DWORD (4 bytes) value based on the starting index
        public static uint ReadDoubleWord(byte[] bytesToRead, int startIndex)
        {
            uint dWordValue = BitConverter.ToUInt32(bytesToRead, startIndex);
            //If system architecture is little-endian, reverse the byte array
            if (!BitConverter.IsLittleEndian)
                dWordValue = ByteUtility.ReverseBytes(dWordValue);

            return dWordValue;
        }

        //Returns a WORD (2 bytes) value based on the starting index
        public static ushort ReadWord(byte[] bytesToRead, int startIndex)
        {
            ushort wordValue = BitConverter.ToUInt16(bytesToRead, startIndex);
            //If system architecture is little-endian, reverse the byte array
            if (!BitConverter.IsLittleEndian)
                wordValue = ByteUtility.ReverseBytes(wordValue);

            return wordValue;
        }

        //Blizzard does some obscure encoding to parts of their replay sections
        //See 4.3 of w3g_format.txt
        //At the moment, we don't actually need the encoded string info but this function is left
        //just in case we need it in the future
        public static string GetReplayEncodedString(byte[] bytesToRead, int startIndex, out int endIndex)
        {
            char mask = ' ';
            List<char> decodedString = new List<char>();

            int maskPos = 0;
            while (bytesToRead[startIndex] != 0)
            {
                if (maskPos % 8 == 0)
                    mask = (char)bytesToRead[startIndex];
                else
                {
                    if ((mask & (0x1 << (maskPos % 8))) == 0)
                        decodedString.Add((char)(bytesToRead[startIndex] - 1));
                    else
                        decodedString.Add((char)bytesToRead[startIndex]);
                }
                startIndex++;
                maskPos++;
            }
            startIndex++;
            endIndex = startIndex;
            return new string(decodedString.ToArray());
        }

        public static string GetNullTerminatedString(byte[] bytesToRead, int startIndex, out int endIndex)
        {
            string nullTerminatedString = String.Empty;
           
            while (bytesToRead[startIndex] != 0)
            {
                nullTerminatedString += (char)bytesToRead[startIndex];
                startIndex++;
            }
            //Skip null terminated byte
            startIndex++;
            endIndex = startIndex;
            return nullTerminatedString;
        }

        public static string ReadDoubleWordString(byte[] bytesToRead, int startIndex)
        {
            return Encoding.UTF8.GetString(bytesToRead.SubArray(startIndex, 4));
        }
    }
}
