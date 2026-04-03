using System.Collections.Generic;
using UnityEngine;

namespace Script.Manager.WorldManager
{
    [System.Serializable]
    public class ChunkAssetLoader
    {
        [SerializeField] private string groundChunkPath = "Prefab/Chunk/ChunkGround";
        [SerializeField] private string impassableRockPath = "Prefab/Chunk/ImpassableRock";
        
        [HideInInspector] public List<GameObject> chunkPrefabsList = new List<GameObject>();
        [HideInInspector] public List<GameObject> impassableRockPrefabs = new List<GameObject>();
        
        public GameObject emptyChunk;
        public GameObject impassableRockLeft;
        public GameObject impassableRockRight;

        public void LoadAllPrefabs()
        {
            // Initialize the lists if they're null
            if (chunkPrefabsList == null)
            {
                chunkPrefabsList = new List<GameObject>();
            }
            else
            {
                chunkPrefabsList.Clear();
            }

            if (impassableRockPrefabs == null)
            {
                impassableRockPrefabs = new List<GameObject>();
            }
            else
            {
                impassableRockPrefabs.Clear();
            }

            // Load ground chunks
            LoadGroundChunks();
            
            // Load impassable rocks
            LoadImpassableRocks();
        }

        public void LoadGroundChunks()
        {
            GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(groundChunkPath);

            if (loadedPrefabs == null || loadedPrefabs.Length == 0)
            {
                Debug.LogWarning($"Không có prefabWall ground chunk nào được tìm thấy trong thư mục {groundChunkPath}. Thử các đường dẫn khác...");
                
                // Try alternative paths
                string[] alternativePaths = new string[]
                {
                    "Prefab/Chunk/ChunkGround",
                    "Art/Prefab/Chunk/ChunkGround",
                    "Art/Resources/Prefab/Chunk/ChunkGround",
                    "Chunk/ChunkGround",
                    "ChunkGround"
                };
                
                TryLoadFromAlternativePaths(alternativePaths, ref chunkPrefabsList);
            }
            else
            {
                chunkPrefabsList.AddRange(loadedPrefabs);
            }
        }

        public void LoadImpassableRocks()
        {
            GameObject[] loadedRocks = Resources.LoadAll<GameObject>(impassableRockPath);

            if (loadedRocks == null || loadedRocks.Length == 0)
            {
                Debug.LogWarning($"Không có prefabWall impassable rock nào được tìm thấy trong thư mục {impassableRockPath}. Thử các đường dẫn khác...");
                
                // Try alternative paths
                string[] alternativePaths = new string[]
                {
                    "Prefab/Chunk/ImpassableRock",
                    "Art/Prefab/Chunk/ImpassableRock",
                    "Art/Resources/Prefab/Chunk/ImpassableRock",
                    "Chunk/ImpassableRock",
                    "ImpassableRock"
                };
                
                if (TryLoadFromAlternativePaths(alternativePaths, ref impassableRockPrefabs))
                {
                    FindImpassableRockPrefabs();
                }
            }
            else
            {
                impassableRockPrefabs.AddRange(loadedRocks);
                FindImpassableRockPrefabs();
            }
        }

        private bool TryLoadFromAlternativePaths(string[] paths, ref List<GameObject> targetList)
        {
            foreach (string path in paths)
            {
                GameObject[] prefabs = Resources.LoadAll<GameObject>(path);
                
                if (prefabs != null && prefabs.Length > 0)
                {
                    targetList.AddRange(prefabs);
                    return true;
                }
            }
            
            Debug.LogError("Không thể tìm thấy prefabWall ở bất kỳ đường dẫn nào. Kiểm tra cấu trúc thư mục và định dạng prefabWall.");
            return false;
        }

        private void FindImpassableRockPrefabs()
        {
            if (impassableRockPrefabs == null) return;

            foreach (GameObject rock in impassableRockPrefabs)
            {
                if (rock == null) continue;
                
                if (rock.name.Contains("Left"))
                {
                    impassableRockLeft = rock;
                }
                else if (rock.name.Contains("Right"))
                {
                    impassableRockRight = rock;
                }
            }

            // If still not found, try direct loading
            if (impassableRockLeft == null)
            {
                impassableRockLeft = Resources.Load<GameObject>("Prefab/Chunk/ImpassableRock/ImpassableRockLeft");
                if (impassableRockLeft != null && !impassableRockPrefabs.Contains(impassableRockLeft))
                {
                    impassableRockPrefabs.Add(impassableRockLeft);
                }
            }

            if (impassableRockRight == null)
            {
                impassableRockRight = Resources.Load<GameObject>("Prefab/Chunk/ImpassableRock/ImpassableRockRight");
                if (impassableRockRight != null && !impassableRockPrefabs.Contains(impassableRockRight))
                {
                    impassableRockPrefabs.Add(impassableRockRight);
                }
            }
        }
    }
}
