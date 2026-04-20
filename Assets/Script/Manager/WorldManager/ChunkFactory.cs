using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;

namespace Script.Manager.WorldManager
{
    public class ChunkFactory
    {
        private ChunkAssetLoader assetLoader;

        public ChunkFactory(ChunkAssetLoader assetLoader)
        {
            this.assetLoader = assetLoader;
        }

        public Chunk CreateGroundChunk(Vector3 position, int id, System.Random rng, GameObject container, List<Chunk> chunkList, bool forceEmpty = false)
        {
            // Safety check
            if (assetLoader.chunkPrefabsList == null || assetLoader.chunkPrefabsList.Count == 0)
            {
                Debug.LogError("chunkPrefabsList is empty or null when trying to create chunk. Trying emergency reload...");
                assetLoader.LoadGroundChunks();
                
                if (assetLoader.chunkPrefabsList.Count == 0)
                {
                    Debug.LogWarning("Tạo chunk dự phòng vì không thể load prefabs...");
                    GameObject fallbackPrefab = assetLoader.emptyChunk;
                    if (fallbackPrefab == null)
                    {
                        fallbackPrefab = new GameObject("EmergencyChunk");
                    }

                    GameObject emergencyChunk = UnityEngine.Object.Instantiate(fallbackPrefab, position, Quaternion.identity, container.transform);
                    Chunk emergencyChunkData = new Chunk(id, 0, position.x, "ground");
                    chunkList.Add(emergencyChunkData);
                    
                    if (EntitySaveManager.Instance != null)
                    {
                        EntitySaveManager.Instance.MoveChildrenToContainers(emergencyChunk);
                    }
                    return emergencyChunkData;
                }
            }

            int prefabIndex = forceEmpty ? -1 : rng.Next(assetLoader.chunkPrefabsList.Count);
            GameObject selectedPrefab = forceEmpty ? assetLoader.emptyChunk : assetLoader.chunkPrefabsList[prefabIndex];

            if (selectedPrefab == null)
            {
                Debug.LogError($"selectedPrefab is null at index {prefabIndex}. Attempting to use fallback...");
                if (assetLoader.emptyChunk != null)
                    selectedPrefab = assetLoader.emptyChunk;
                else
                    return null;
            }

            GameObject newChunkObject = UnityEngine.Object.Instantiate(selectedPrefab, position, Quaternion.identity, container.transform);
            Chunk chunk = new Chunk(id, prefabIndex, position.x, "ground");
            chunkList.Add(chunk);

            if (EntitySaveManager.Instance != null)
            {
                EntitySaveManager.Instance.MoveChildrenToContainers(newChunkObject);
            }
            return chunk;
        }

        public void CreateImpassableRock(bool isLeft, float chunkWidth, ref int currentChunkLeftId, ref int currentChunkRightId, GameObject container, List<Chunk> chunkList)
        {
            if (isLeft && assetLoader.impassableRockLeft == null)
            {
                Debug.LogError("ImpassableRockLeft prefab not found!");
                return;
            }
            
            if (!isLeft && assetLoader.impassableRockRight == null)
            {
                Debug.LogError("ImpassableRockRight prefab not found!");
                return;
            }
            
            Vector3 position;
            int id;
            GameObject rockPrefab;
            
            if (isLeft)
            {
                position = new Vector3((currentChunkLeftId - 1) * chunkWidth, 0, 0);
                id = currentChunkLeftId - 1;
                rockPrefab = assetLoader.impassableRockLeft;
                currentChunkLeftId = id;
            }
            else
            {
                position = new Vector3((currentChunkRightId + 1) * chunkWidth, 0, 0);
                id = currentChunkRightId + 1;
                rockPrefab = assetLoader.impassableRockRight;
                currentChunkRightId = id;
            }
            
            int prefabIndex = assetLoader.impassableRockPrefabs.IndexOf(rockPrefab);
            if (prefabIndex < 0) prefabIndex = 0;
            
            GameObject rockObject = UnityEngine.Object.Instantiate(rockPrefab, position, Quaternion.identity, container.transform);
            
            string chunkType = isLeft ? "impassableLeft" : "impassableRight";
            Chunk rock = new Chunk(id, prefabIndex, position.x, chunkType);
            chunkList.Add(rock);
            
            if (EntitySaveManager.Instance != null)
            {
                EntitySaveManager.Instance.MoveChildrenToContainers(rockObject);
            }
        }

        public GameObject SpawnInitialTownCenter()
        {
            if (PrefabData.Instance != null && EntitySaveManager.Instance != null)
            {
                GameObject tcPrefab = PrefabData.Instance.GetTownCenterByLevel(0);
                if (tcPrefab != null)
                {
                    Debug.Log("Spawning initial TownCenter Tier 0 at (0,0,0)");
                    return EntitySaveManager.Instance.InstanceAndMoveObjectToContainer(tcPrefab, Vector3.zero);
                }
                else
                {
                    Debug.LogError("TownCenter Tier 0 prefab not found in PrefabData!");
                }
            }
            return null;
        }
    }
}
