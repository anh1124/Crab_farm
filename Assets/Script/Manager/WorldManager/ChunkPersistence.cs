using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Data_Model;
using UnityEngine;

namespace Script.Manager.WorldManager
{
    public class ChunkPersistence
    {
        private ChunkAssetLoader assetLoader;

        public ChunkPersistence(ChunkAssetLoader assetLoader)
        {
            this.assetLoader = assetLoader;
        }

        public void SaveMapToJson(string saveFilePath, int seed, int currentChunkLeftId, int currentChunkRightId, List<Chunk> chunkList, string containerName)
        {
            if (string.IsNullOrEmpty(saveFilePath))
            {
                Debug.LogError("ChunkPersistence: saveFilePath is empty");
                return;
            }

            MapSaveData mapData = new MapSaveData(
                seed,
                currentChunkLeftId,
                currentChunkRightId,
                chunkList,
                containerName
            );

            string jsonData = JsonConvert.SerializeObject(mapData, Formatting.Indented, 
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            
            string mapPath = Path.Combine(saveFilePath, "map.json");
            File.WriteAllText(mapPath, jsonData);
            Debug.Log($"Đã lưu map vào: {mapPath}");
        }

        public MapSaveData LoadMapFromJson(string mapFilePath, GameObject container, out List<Chunk> loadedChunks)
        {
            loadedChunks = new List<Chunk>();
            if (!File.Exists(mapFilePath))
            {
                Debug.LogWarning("Không tìm thấy file map!");
                return null;
            }

            try
            {
                string jsonData = File.ReadAllText(mapFilePath);
                MapSaveData mapData = JsonConvert.DeserializeObject<MapSaveData>(jsonData,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                if (mapData == null)
                {
                    Debug.LogError("Không thể đọc dữ liệu từ map.json. File có thể bị hỏng.");
                    return null;
                }

                ClearCurrentMap(container);

                // Ensure the necessary prefabs are loaded
                if (assetLoader.chunkPrefabsList.Count == 0)
                    assetLoader.LoadGroundChunks();
                if (assetLoader.impassableRockPrefabs.Count == 0)
                    assetLoader.LoadImpassableRocks();

                if (assetLoader.emptyChunk == null)
                {
                    Debug.LogError("emptyChunk prefab is not assigned!");
                }

                foreach (var chunk in mapData.chunks)
                {
                    Vector3 position = new Vector3(chunk.positionX, 0, 0);
                    GameObject prefabToUse = assetLoader.emptyChunk;

                    if (chunk.chunkType != null)
                    {
                        switch (chunk.chunkType)
                        {
                            case "impassableLeft":
                                if (assetLoader.impassableRockLeft != null) prefabToUse = assetLoader.impassableRockLeft;
                                break;
                            case "impassableRight":
                                if (assetLoader.impassableRockRight != null) prefabToUse = assetLoader.impassableRockRight;
                                break;
                            case "ground":
                            default:
                                prefabToUse = assetLoader.emptyChunk;
                                break;
                        }
                    }

                    if (prefabToUse != null)
                    {
                        GameObject newChunkObject = UnityEngine.Object.Instantiate(prefabToUse, position, Quaternion.identity, container.transform);
                        if (newChunkObject != null)
                        {
                            // Move pre-planted trees and other objects from the chunk to their proper containers
                            if (ContainerEntityManager.Instance != null)
                                ContainerEntityManager.Instance.MoveChildrenToContainers(newChunkObject);
                        }
                    }
                }

                loadedChunks = mapData.chunks;
                return mapData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading map from JSON: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public void ClearCurrentMap(GameObject container)
        {
            if (container == null) return;
            foreach (Transform child in container.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
