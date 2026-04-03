using System;
using System.Collections.Generic;

namespace Script.Data_Model
{
    [Serializable]
    public class Chunk
    {
        public int id;  // ID để xác định vị trí trái/phải trên bản đồ
        public int prefabIndex;  // Index trong mảng chunkPrefabsList
        public float positionX;
        public string chunkType;  // Loại chunk: "ground", "impassableLeft", "impassableRight"

        public Chunk(int id, int prefabIndex, float positionX, string chunkType = "ground")
        {
            this.id = id;
            this.prefabIndex = prefabIndex;
            this.positionX = positionX;
            this.chunkType = chunkType;
        }
    }

    [Serializable]
    public class MapSaveData
    {
        public int seed;
        public int currentChunkLeftId;
        public int currentChunkRightId;
        public List<Chunk> chunks;
        public string chunkContainerName;
        public bool hasImpassableRocks; // Flag to indicate if the map includes boundary rocks

        public MapSaveData(int seed, int currentChunkLeftId, int currentChunkRightId, List<Chunk> chunks,
            string chunkContainerName)
        {
            this.seed = seed;
            this.currentChunkLeftId = currentChunkLeftId;
            this.currentChunkRightId = currentChunkRightId;
            this.chunks = chunks;
            this.chunkContainerName = chunkContainerName;
            
            // Check if any chunks are impassable rocks
            this.hasImpassableRocks = chunks != null && chunks.Exists(c => c.chunkType == "impassableLeft" || c.chunkType == "impassableRight");
        }
    }
}
