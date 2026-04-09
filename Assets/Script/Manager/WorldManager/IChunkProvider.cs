using System.Collections.Generic;
using Script.Data_Model;

namespace Script.Manager.WorldManager
{
    public interface IChunkProvider
    {
        void GenerateFirstMap(int seed);
        void LoadMapFromJson(string mapFilePath);
        void SaveMapToJson(string saveFilePath);
        void AddChunk(bool toRight);
        List<Chunk> chunkList { get; }
    }
}
