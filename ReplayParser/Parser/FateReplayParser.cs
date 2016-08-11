using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FateReplayParser.Data;
using FateReplayParser.Utility;
using NLog;

namespace FateReplayParser.Parser
{
    public class FateReplayParser
    {
        private const string SUPPORTED_FILE_EXTENSION = ".w3g";
        private const string ERROR_FILE_DOES_NOT_EXIST = "Replay file does not exist: {0}";
        private const string ERROR_FILE_EXTENSION_NOT_SUPPORTED = "File extension is not a w3g replay file: {0}";
        private const string ERROR_FILE_BYTE_LENGTH_TOO_SMALL = "Replay file's byte length is too small: {0}";
        private const string ERROR_DATA_BLOCK_IS_EMPTY = "Replay Datablock is empty";
        private const int REPLAY_MINIMUM_BYTE_LENGTH = 288;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        //Unfiltered list of event calls from Fate map.
        //SyncStoredInteger calls are repeated n times of player count.
        private readonly HashSet<FRSEvent> _frsEventCallList = new HashSet<FRSEvent>(); 

        private readonly string _replayFilePath;
        private readonly FateReplayHeaderParser _fateReplayHeaderParser;
        
        public FateReplayParser(string replayFilePath)
        {
            _replayFilePath = replayFilePath;
            _fateReplayHeaderParser = new FateReplayHeaderParser();
        }

        public ReplayData ParseReplayData()
        {
            if (!File.Exists(_replayFilePath))
            {
                throw new FileNotFoundException(String.Format(ERROR_FILE_DOES_NOT_EXIST, _replayFilePath));
            }
            if (Path.GetExtension(_replayFilePath) != SUPPORTED_FILE_EXTENSION)
            {
                throw new ArgumentException(String.Format(ERROR_FILE_EXTENSION_NOT_SUPPORTED, _replayFilePath));
            }

            byte[] replayFileBytes = File.ReadAllBytes(_replayFilePath);
            if (replayFileBytes.Length < REPLAY_MINIMUM_BYTE_LENGTH)
            {
                throw new ArgumentException(String.Format(ERROR_FILE_BYTE_LENGTH_TOO_SMALL, _replayFilePath));
            }

            int currIndex = 0;
            ReplayData replayData = new ReplayData(replayFileBytes);
            //Parse until the end of replay header
            //Retrieve the current index, which is the beginning point of DataBlocks
            ReplayHeader replayHeader = _fateReplayHeaderParser.ParseReplayHeader(replayFileBytes,out currIndex);
            replayData.ReplayHeader = replayHeader;

            replayData.ReplayUrl = _replayFilePath;

            //Begin Datablock parsing
            List<byte> dataBlockBytes = new List<byte>();
            while (true)
            {
                //Each datablock is divided into 8k or less bytes.
                //CurrIndex keeps track of the current position to parse each data block.
                DataBlock dataBlock = CreateParsedDataBlock(replayFileBytes, currIndex, out currIndex);
                if (dataBlock == null)
                    break;
                replayData.AddDataBlock(dataBlock);
                dataBlockBytes.AddRange(dataBlock.DecompressedDataBlockBytes);
            }

            if (!replayData.DataBlockList.Any())
                throw new InvalidDataException(ERROR_DATA_BLOCK_IS_EMPTY);

            //Get game datetime from file name
            FileInfo replayFileInfo = new FileInfo(_replayFilePath);
            DateTime? gameDate = GetGameDateTimeFromFileName(replayFileInfo.Name) ?? DateTime.Now;
            replayData.GameDateTime = (DateTime)gameDate;

            //First block has replay game header information + action block
            ParseGameHeaderBlock(replayData.DataBlockList[0], replayData, out currIndex);
            
            //Parse main game data
            ParseGameReplayDataFromBlock(dataBlockBytes.ToArray(), replayData, currIndex);

            //Parse injected event data
            FRSEventParser.ParseEventAPI(_frsEventCallList, replayData);

            //Determine observers from game
            SetObservers(replayData.PlayerInfoList);
            return replayData;
        }

        private DateTime? GetGameDateTimeFromFileName(string replayFileName)
        {
            try
            {
                string[] spaceSeparatedFileName = replayFileName.Split(' ');
                DateTime gameDate = DateTime.Parse(spaceSeparatedFileName[1] + " " + spaceSeparatedFileName[2].Replace('-',':'));
                return gameDate;
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString);
                return null;
            }
        }
        

        private void ParseGameReplayDataFromBlock(byte[] dataBlockBytes, ReplayData replayData, int currIndex = 0)
        {
            uint accumulatedReplayTime = 0;
            
            while (currIndex < dataBlockBytes.Length)
            {
                switch (dataBlockBytes[currIndex])
                {
                    case 0x00: //End of replay
                        while (currIndex < dataBlockBytes.Length)
                        {
                            if (dataBlockBytes[currIndex] != 0x00)
                            {
                                throw new InvalidDataException(
                                    String.Format("End of replay expected, but there's still data available. CurrIndex: {0}, Input: {1}",
                                    currIndex, dataBlockBytes[currIndex]));
                            }
                            currIndex++;
                        }
                        break;
                    case 0x1A:
                        currIndex += 5; //Unknown. Skip
                        break;
                    case 0x1B:
                        currIndex += 5; //Unknown. Skip
                        break;
                    case 0x1C:
                        currIndex += 5; //Unknown. Skip
                        break;
                    case 0x1E: case 0x1F: //TimeSlot block
                        
                        currIndex++;
                        ParseTimeSlotBlock(replayData, ref currIndex, ref accumulatedReplayTime, dataBlockBytes);
                        break;
                    case 0x17: case 0x54: //Player left
                        currIndex += 14;
                        break;
                    case 0x20: //Chat message
                        currIndex++;
                        var chatMessage = GetChatMessage(replayData, ref currIndex, dataBlockBytes);
                        replayData.AddChatMessage(chatMessage);
                        break;
                    case 0x22:
                        currIndex += 6; //Checksum or Random number/seed
                        break;
                    case 0x23:
                        currIndex += 11; //Unknown
                        break;
                    case 0x2F:
                        currIndex += 9; //Game end countdown
                        break;
                    default:
                        throw new Exception(String.Format("Unexpected Action at CurrIndex : {0}, Action Input : {1}" ,currIndex,dataBlockBytes[currIndex]));
                }
            }
        }

        private void ParseTimeSlotBlock(ReplayData replayData, ref int currIndex, ref uint accumulatedReplayTime, byte[] gameData)
        {
            ushort commandByteCount = ByteUtility.ReadWord(gameData, currIndex);
            currIndex += 2;

            if (commandByteCount < 2)
                throw new InvalidDataException(String.Format("Unexpected Command Byte Count (Min. 2). Input: {0}", commandByteCount));

            accumulatedReplayTime += ByteUtility.ReadWord(gameData, currIndex);
            currIndex += 2;

            //Command Data not present if n = 2
            int commandByteEndIndex = currIndex + commandByteCount - 2;

            while (currIndex < commandByteEndIndex) //A single command block may contain multiple actions
            {
                byte playerId = gameData[currIndex];
                currIndex++;
                ushort actionBlockLength = ByteUtility.ReadWord(gameData, currIndex);
                currIndex += 2;

                ParseActionBlock(actionBlockLength, ref currIndex, gameData,replayData);
            }
        }

        private void ParseActionBlock(int actionBlockLength, ref int currIndex, byte[] gameData, ReplayData replayData)
        {
            actionBlockLength += currIndex;
            while(currIndex < actionBlockLength)
            {
                byte actionId = gameData[currIndex];
                currIndex++;

                switch (actionId)
                {
                    case 0x01: //Pause Game
                        break;
                    case 0x02: //Resume Game
                        break;
                    case 0x03: //Set game speed (Single Player)
                        currIndex++;
                        break;
                    case 0x04: //Increase game speed (Single Player)
                        break;
                    case 0x05: //Decrease game speed (Single Player)
                        break;
                    case 0x06: //Save game
                        string saveName = ByteUtility.GetNullTerminatedString(gameData, currIndex, out currIndex);
                        break;
                    case 0x07: //save game finished
                        currIndex += 4;
                        break;
                    case 0x10: //Unit/Building Ability
                        currIndex += 14;
                        break;
                    case 0x11: //Unit/Building Ability with target position
                        currIndex += 22;
                        break;
                    case 0x12: //Unit/Building ability (With target position and target object id)
                        currIndex += 30;
                        break;
                    case 0x13: //Give item to Unit / Drop item on ground
                        currIndex += 38;
                        break;
                    case 0x14: //Unit building ability (With two target positions and two item ids)
                        currIndex += 43;
                        break;
                    case 0x16:
                    case 0x17: //Change selection, assign group hotkey
                        currIndex++; //Select Mode
                        ushort selectionCount = ByteUtility.ReadWord(gameData, currIndex);
                        currIndex += 2;
                        currIndex += selectionCount*8; //n * 8 bytes (Two DWORD repeated);
                        break;
                    case 0x18: //Select Group Hotkey
                        currIndex += 2;
                        break;
                    case 0x19: //Select Subgroup
                        currIndex += 12;
                        break;
                    case 0x1A: //Pre Subselection

                        break;
                    case 0x1B: //Unknown
                        currIndex += 9;
                        break;
                    case 0x1C: //Select Ground Item
                        currIndex += 9;
                        break;
                    case 0x1D: //Cancel Hero Revival
                        currIndex += 8;
                        break;
                    case 0x1E: //Remove unit from building queue
                        currIndex += 5;
                        break;
                    case 0x21: //unknown
                        currIndex += 8;
                        break;
                    case 0x50: //Change ally options
                        currIndex += 5;
                        break;
                    case 0x51: //Transfer Resources
                        currIndex += 9;
                        break;
                    case 0x60: //Map Trigger Chat Command
                        currIndex += 8; //Two Unknown Double Words
                        List<byte> encodedChatBytes = new List<byte>();
                        while (gameData[currIndex] != 0x00)
                        {
                            encodedChatBytes.Add(gameData[currIndex]);
                            currIndex++;
                        }
                        currIndex++;
                        string chatString = Encoding.UTF8.GetString(encodedChatBytes.ToArray());
                        break;
                    case 0x61: //ESC Pressed
                        break;
                    case 0x62: //Scenario Trigger
                        currIndex += 12;
                        break;
                    case 0x66: //Choose hero skill submenu
                        break;
                    case 0x67: //Choose building submenu
                        break;
                    case 0x68: //Minimap Ping
                        currIndex += 12;
                        break;
                    case 0x69: //Continue Game (Block B)
                        currIndex += 16;
                        break;
                    case 0x6A: //Continue Game (Block A)
                        currIndex += 16;
                        break;
                    case 0x75: //Unknown
                        currIndex++;
                        break;
                    case 0x20: //Cheats
                    case 0x22:
                    case 0x23:
                    case 0x24:
                    case 0x25:
                    case 0x26:
                    case 0x29:
                    case 0x2A:
                    case 0x2B:
                    case 0x2C:
                    case 0x2F:
                    case 0x30:
                    case 0x31:
                    case 0x32:
                        break;
                    case 0x70: //SyncStoredInteger. The most important part.
                        string gameCacheName = ByteUtility.GetNullTerminatedString(gameData, currIndex, out currIndex);
                        string eventCategory = ByteUtility.GetNullTerminatedString(gameData, currIndex, out currIndex);
                        string[] eventDetailId = ByteUtility.GetNullTerminatedString(gameData, currIndex, out currIndex).Split(new[] { "/" }, StringSplitOptions.None);
                        string eventId = eventDetailId[0];
                        string eventDetail = string.Join("/",eventDetailId.Skip(1).Take(4));
                        _frsEventCallList.Add(new FRSEvent(eventId, gameCacheName,eventCategory,eventDetail));


                        break;
                    default:
                        throw new Exception(String.Format("Unexpected Action ID found at currIndex: {0} Input: {1}",
                                                          currIndex-1, actionId));
                }
            }
        }

        private void SetObservers(List<PlayerInfo> playerInfoList)
        {
            foreach (PlayerInfo player in playerInfoList)
            {
                if (String.IsNullOrEmpty(player.ServantId))
                {
                    if (player.Kills != 0 || player.Deaths != 0 || player.Assists != 0)
                        throw new InvalidDataException(String.Format("SetObservers error. No servant selected but has kills/deaths/assists: {0}",player.PlayerName));
                    player.IsObserver = true;
                }
            }
        }

        private string GetChatMessage(ReplayData replayData, ref int currIndex, byte[] gameData)
        {
            int playerId = gameData[currIndex];
            currIndex++;
            PlayerInfo playerInfo = replayData.GetPlayerInfoByPlayerReplayId(playerId);
            if (playerInfo == null)
                throw new InvalidDataException(
                    String.Format("Player Id Not found in method ParseGameReplayDataFromBlock. Input : {0}", playerId));
            ushort numberOfBytes = ByteUtility.ReadWord(gameData, currIndex);
            currIndex += 2;

            byte checkFlag = gameData[currIndex];
            currIndex++;

            uint chatMode = ByteUtility.ReadDoubleWord(gameData, currIndex);
            currIndex += 4;
            string chatMessage = String.Empty;

            //Mix Teams messes up team orientation
            //For now, not appending all or allied chat until better solution is found
            //if (chatMode == 0x00)
            //    chatMessage += "[A]";
            //else if (chatMode == 0x01)
            //    chatMessage += "[T" + (playerInfo.Team + 1) + "]";

            chatMessage += "[" + playerInfo.PlayerName + "]";
            List<byte> encodedChatMessage = new List<byte>();
            while (gameData[currIndex] != 0x00)
            {
                encodedChatMessage.Add(gameData[currIndex]);
                currIndex++;
            }
            chatMessage += Encoding.UTF8.GetString(encodedChatMessage.ToArray());
            currIndex++;
            return chatMessage;
        }

        //Precondition: datablock cannot be null
        //Parses Gamename, playerNames and the team indices of the player.
        private void ParseGameHeaderBlock(DataBlock dataBlock, ReplayData replayData, out int endIndex)
        {
            //Skip 4 (According to specification, first 4 bytes is unknown)
            int currIndex = 4;

            byte[] gameHeaderData = dataBlock.DecompressedDataBlockBytes;

            byte recordId = gameHeaderData[currIndex];
            currIndex++;
            byte playerId = gameHeaderData[currIndex];
            currIndex++;

            string playerName = ByteUtility.GetNullTerminatedString(gameHeaderData, currIndex, out currIndex);

            replayData.AddPlayerInfo(new PlayerInfo(playerName,playerId,recordId));
            
            //Custom data byte. We can safely ignore this.
            Debug.Assert(gameHeaderData[currIndex] == 0x01);
            currIndex++;
            //Null byte. Ignore this as well
            Debug.Assert(gameHeaderData[currIndex] == 0x00);
            currIndex++;

            replayData.GameName = ByteUtility.GetNullTerminatedString(gameHeaderData, currIndex, out currIndex);

            //Null byte.
            Debug.Assert(gameHeaderData[currIndex] == 0x00);
            currIndex++;
            
            //Refers to Section 4.4, 4.5 (Game Settings, Map&Creator Name)
            //We don't actually need this information, but it's kept just in case we need it in the future
            string encodedString = ByteUtility.GetReplayEncodedString(gameHeaderData, currIndex, out currIndex);

            //According to spec, this is player count but in reality, it's mapslotcount
            uint mapSlotCount = ByteUtility.ReadDoubleWord(gameHeaderData, currIndex);
            currIndex += 4;

            //Game Type byte. Safely skip (Section 4.7)
            currIndex++;

            //Private Flag. Safely Skip (Section 4.7)
            currIndex++;

            //Unknown Word. Safely Skip (Section 4.7)
            currIndex += 2;

            //Language ID. Safely Skip (Section 4.8)
            currIndex += 4;

            //Loop until we find all players
            while (gameHeaderData[currIndex] == 0x16)
            {
                recordId = gameHeaderData[currIndex];
                currIndex++;
                playerId = gameHeaderData[currIndex];
                currIndex++;

                playerName = ByteUtility.GetNullTerminatedString(gameHeaderData, currIndex, out currIndex);
                replayData.AddPlayerInfo(new PlayerInfo(playerName, playerId, recordId));

                //Custom data byte. We can safely ignore this.
                Debug.Assert(gameHeaderData[currIndex] == 0x01);
                currIndex++;

                //Skip 4 unknown bytes (Section 4.9)
                currIndex += 4;

                //Skip null byte
                currIndex++;
            }
            
            //Skip Record Id (Section 4.10, always 0x19)
            currIndex++;

            //Number of data bytes following
            ushort dataByteCount = ByteUtility.ReadWord(gameHeaderData, currIndex);
            currIndex += 2;

            //number of available slots (For fate, always 12)
            int slotCount = gameHeaderData[currIndex];
            currIndex++;

            //int slotRecordIndex = 0;
            for (int i = 0; i < slotCount; i++ )
            {
                playerId = gameHeaderData[currIndex];
                currIndex++;
                if (playerId == 0x00) //Computer player. Skip to next one
                {
                    currIndex += 8;
                    continue;
                }

                //Skip map download percent
                currIndex++;

                byte slotStatus = gameHeaderData[currIndex];
                if (slotStatus == 0x00) //Empty slot. Skip to next one.
                {
                    currIndex += 7;
                    continue;
                }
                currIndex++;

                //Skip computer player flag
                currIndex++;

                byte teamNumber = gameHeaderData[currIndex];
                PlayerInfo player = replayData.GetPlayerInfoByPlayerReplayId(playerId);
                if (player == null)
                    throw new InvalidDataException("Player Id not found! ID: " + playerId);
                player.Team = teamNumber;
                replayData.PlayerCount++;
                currIndex++;

                //Skip rest of bytes (color, raceflags, AI strength, handicap)
                currIndex += 4;
            }
            
            //Skip randomseed (Section 4.12)
            currIndex += 4;

            //Skip selectMode
            byte selectMode = gameHeaderData[currIndex];
            //For fate, Team & Race is not selectable.
            
            currIndex++;

            //Skip StartSpotCount
            currIndex++;

            endIndex = currIndex;
        }

        private DataBlock CreateParsedDataBlock(byte[] replayBytes, int currIndex, out int movedIndex)
        {
            DataBlock dataBlock = new DataBlock();
            if (currIndex >= replayBytes.Length)
            {
                movedIndex = currIndex;
                return null;
            }

            ushort compressedDataBlockSize = ByteUtility.ReadWord(replayBytes, currIndex);
            currIndex += 2;

            ushort decompressedDataBlockSize = ByteUtility.ReadWord(replayBytes, currIndex);
            currIndex += 2;

            uint checkSum = ByteUtility.ReadDoubleWord(replayBytes, currIndex);
            currIndex += 4;

            byte[] compressedDataBlockBytes = replayBytes.SubArray(currIndex, compressedDataBlockSize);
            byte[] decompressedDataBlockBytes = Ionic.Zlib.ZlibStream.UncompressBuffer(compressedDataBlockBytes);

            dataBlock.CompressedDataBlockSize = compressedDataBlockSize;
            dataBlock.DecompressedDataBlockSize = decompressedDataBlockSize;
            dataBlock.CheckSum = checkSum;
            dataBlock.CompressedDataBlockBytes = compressedDataBlockBytes;
            dataBlock.DecompressedDataBlockBytes = decompressedDataBlockBytes;

            currIndex += compressedDataBlockSize;

            movedIndex = currIndex; 
            return dataBlock;
        }
    }
}
