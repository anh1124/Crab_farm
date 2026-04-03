using System;
using System.Collections.Generic;
using Script;
using Script.Controller.GUI_Controller.PlayerGui;
using Script.Data_Model;
using UnityEngine;

namespace Script.Manager.WorldManager
{
    public class ChunkManager : MonoBehaviour
    {
        public static ChunkManager Instance { get; private set; }

        [Header("Settings")]
        public int seed;
        public ChunkAssetLoader assetLoader;
        
        [Header("State")]
        [HideInInspector] public List<Chunk> chunkList = new List<Chunk>();
        
        private ChunkFactory factory;
        private ChunkPersistence persistence;

        private int currentChunkLeftId = 0;
        private int currentChunkRightId = 0;
        private int centerChunkId = 0;
        [SerializeField] private float chunkWidth = 30f;
        private System.Random rng;

        private void Awake()
        {
            AddSingleton();
            InitializeComponents();
        }

        private void AddSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeComponents()
        {
            if (assetLoader == null)
            {
                assetLoader = new ChunkAssetLoader();
            }
            assetLoader.LoadAllPrefabs();
            
            factory = new ChunkFactory(assetLoader);
            persistence = new ChunkPersistence(assetLoader);
        }

        private void Start()
        {
            PlayerGui.Instance.OnSkill3PressedACT += test;
        }

        public void GenerateFirstMap(int Seed)
        {
            this.seed = Seed;
            rng = new System.Random(Seed);
            
            // Get chunk count from PrefabData
            int groundChunkCount = 3; 
            if (PrefabData.Instance != null)
            {
                groundChunkCount = PrefabData.Instance.currentCreatChunk;
            }
            
            CreateInitialChunks(groundChunkCount);
            
            // Save entity containers after world generation.
            // Trees are now initialized in Awake() so currentWorkProgress=8 is correct at this point.
            if (EntitySaveManager.Instance != null)
            {
                EntitySaveManager.Instance.SaveAllContainer();
            }
        }

        private void CreateInitialChunks(int count)
        {
            int totalGroundChunks = count;
            if (totalGroundChunks % 2 == 0) totalGroundChunks--;
            int sideChunks = totalGroundChunks / 2;

            if (EntitySaveManager.Instance == null || EntitySaveManager.Instance.chunkContainer == null)
            {
                Debug.LogError("EntitySaveManager or chunkContainer is null!");
                return;
            }

            GameObject container = EntitySaveManager.Instance.chunkContainer;

            // Create center chunk (always empty to accommodate TownCenter)
            factory.CreateGroundChunk(Vector3.zero, centerChunkId, rng, container, chunkList, true);
            currentChunkLeftId = centerChunkId;
            currentChunkRightId = centerChunkId;

            // Spawn TownCenter level 0 at (0, 0, 0)
            factory.SpawnInitialTownCenter();

            // Create ground chunks to left and right
            for (int i = 1; i <= sideChunks; i++)
            {
                bool forceEmpty = (i == 1); // Tạo chunk trống ở sát chunk trung tâm (1 bên trái, 1 bên phải)
                factory.CreateGroundChunk(new Vector3(-i * chunkWidth, 0, 0), centerChunkId - i, rng, container, chunkList, forceEmpty);
                currentChunkLeftId = centerChunkId - i;

                factory.CreateGroundChunk(new Vector3(i * chunkWidth, 0, 0), centerChunkId + i, rng, container, chunkList, forceEmpty);
                currentChunkRightId = centerChunkId + i;
            }

            // Add boundary rocks
            factory.CreateImpassableRock(true, chunkWidth, ref currentChunkLeftId, ref currentChunkRightId, container, chunkList);
            factory.CreateImpassableRock(false, chunkWidth, ref currentChunkLeftId, ref currentChunkRightId, container, chunkList);

            chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
        }

        public void AddChunk(bool toRight)
        {
            if (chunkList.Count == 0) return;

            RemoveImpassableRock(toRight);
            
            float newX;
            int newId;

            if (toRight)
            {
                newX = FindRightmostGroundChunkX() + chunkWidth;
                newId = currentChunkRightId + 1;
                currentChunkRightId = newId;
            }
            else
            {
                newX = FindLeftmostGroundChunkX() - chunkWidth;
                newId = currentChunkLeftId - 1;
                currentChunkLeftId = newId;
            }

            bool chunkExists = chunkList.Exists(c => Mathf.Approximately(c.positionX, newX) && c.chunkType == "ground");
            if (!chunkExists)
            {
                factory.CreateGroundChunk(new Vector3(newX, 0, 0), newId, rng, EntitySaveManager.Instance.chunkContainer, chunkList);
            }

            factory.CreateImpassableRock(!toRight, chunkWidth, ref currentChunkLeftId, ref currentChunkRightId, EntitySaveManager.Instance.chunkContainer, chunkList);
            chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
            
            EntitySaveManager.Instance.SaveAllContainer();
        }

        private void RemoveImpassableRock(bool fromRight)
        {
            string targetType = fromRight ? "impassableRight" : "impassableLeft";
            Chunk rockToRemove = chunkList.Find(c => c.chunkType == targetType);
            
            if (rockToRemove != null)
            {
                foreach (Transform child in EntitySaveManager.Instance.chunkContainer.transform)
                {
                    if (Mathf.Approximately(rockToRemove.positionX, child.position.x))
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
                chunkList.Remove(rockToRemove);
                
                if (fromRight) currentChunkRightId--;
                else currentChunkLeftId++;
            }
        }

        private float FindRightmostGroundChunkX()
        {
            float maxX = float.MinValue;
            foreach (var chunk in chunkList)
            {
                if (chunk.chunkType == "ground" && chunk.positionX > maxX) maxX = chunk.positionX;
            }
            return maxX;
        }

        private float FindLeftmostGroundChunkX()
        {
            float minX = float.MaxValue;
            foreach (var chunk in chunkList)
            {
                if (chunk.chunkType == "ground" && chunk.positionX < minX) minX = chunk.positionX;
            }
            return minX;
        }

        public void SaveMapToJson(string saveFilePath)
        {
            string containerName = EntitySaveManager.Instance.chunkContainer != null 
                ? EntitySaveManager.Instance.chunkContainer.name 
                : "UnnamedChunkContainer";
                
            persistence.SaveMapToJson(saveFilePath, seed, currentChunkLeftId, currentChunkRightId, chunkList, containerName);
        }

        public void LoadMapFromJson(string mapFilePath)
        {
            MapSaveData data = persistence.LoadMapFromJson(mapFilePath, EntitySaveManager.Instance.chunkContainer, out List<Chunk> loadedChunks);
            
            if (data != null)
            {
                seed = data.seed;
                rng = new System.Random(seed);
                currentChunkLeftId = data.currentChunkLeftId;
                currentChunkRightId = data.currentChunkRightId;
                chunkList = loadedChunks;
                chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
            }
        }

        public void test() { }
    }
}