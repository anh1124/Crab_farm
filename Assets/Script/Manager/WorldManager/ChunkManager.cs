using System;
using System.Collections.Generic;
using Script;
using Script.Controller.GUI_Controller.PlayerGui;
using Script.Data_Model;
using UnityEngine;

namespace Script.Manager.WorldManager
{
    public class ChunkManager : MonoBehaviour, IChunkProvider
    {
        [Header("Settings")]
        public int seed;
        public ChunkAssetLoader assetLoader;
        
        [Header("State")]
        [SerializeField] private List<Chunk> _chunkList = new List<Chunk>();
        public List<Chunk> chunkList => _chunkList;
        
        private ChunkFactory factory;
        private ChunkPersistence persistence;
        private EntitySaveManager _saveManager;

        public void Setup(EntitySaveManager saveManager)
        {
            _saveManager = saveManager;
        }

        private int currentChunkLeftId = 0;
        private int currentChunkRightId = 0;
        private int centerChunkId = 0;
        [SerializeField] private float chunkWidth = 30f;
        private System.Random rng;

        private void Awake()
        {
            InitializeComponents();
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
            if (factory == null) InitializeComponents();
            this.seed = Seed;
            rng = new System.Random(Seed);
            
            // Get chunk count from PrefabData
            int groundChunkCount = 3; 
            if (PrefabData.Instance != null)
            {
                groundChunkCount = PrefabData.Instance.currentCreatChunk;
            }
            
            CreateInitialChunks(groundChunkCount);
            
            // We no longer call SaveAllContainer here because the loading sequence 
            // will follow immediately after if this is a new world, or the user
            // will trigger an auto/manual save later. Calling it here causes 
            // duplication when LoadEntitiesFromSave is called later in the bootstrap.

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

            GameObject container = _saveManager.chunkContainer;

            // Create center chunk (always empty to accommodate TownCenter)
            factory.CreateGroundChunk(Vector3.zero, centerChunkId, rng, container, _chunkList, true);
            currentChunkLeftId = centerChunkId;
            currentChunkRightId = centerChunkId;

            // Spawn TownCenter level 0 at (0, 0, 0)
            GameObject townCenter = factory.SpawnInitialTownCenter();

            // Đăng ký TownCenter vào BuildingManager ngay lập tức để Utility có thể tìm thấy vị trí trung tâm
            var bm = FindObjectOfType<Script.Manager.Building.BuildingManager>();
            if (bm != null && townCenter != null)
            {
                bm.currentTownCenter = townCenter;
            }

            // Sinh portal 2 bên cho thế giới mới
            if (Script.Manager.GamePlayManager.EnemyPortalManager.Instance != null)
            {
                Script.Manager.GamePlayManager.EnemyPortalManager.Instance.SpawnStartingPortals();
            }

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
            factory.CreateImpassableRock(true, chunkWidth, ref currentChunkLeftId, ref currentChunkRightId, container, _chunkList);
            factory.CreateImpassableRock(false, chunkWidth, ref currentChunkLeftId, ref currentChunkRightId, container, _chunkList);

            _chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
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
                factory.CreateGroundChunk(new Vector3(newX, 0, 0), newId, rng, _saveManager.chunkContainer, _chunkList);
            }

            factory.CreateImpassableRock(!toRight, chunkWidth, ref currentChunkLeftId, ref currentChunkRightId, _saveManager.chunkContainer, _chunkList);
            _chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
            
            // We no longer call SaveAllContainer here to avoid lag spikes.
            // Map and entity persistence is handled by periodic auto-saves.

        }

        private void RemoveImpassableRock(bool fromRight)
        {
            string targetType = fromRight ? "impassableRight" : "impassableLeft";
            Chunk rockToRemove = chunkList.Find(c => c.chunkType == targetType);
            
            if (rockToRemove != null)
            {
                foreach (Transform child in _saveManager.chunkContainer.transform)
                {
                    if (Mathf.Approximately(rockToRemove.positionX, child.position.x))
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
                _chunkList.Remove(rockToRemove);
                
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
            if (persistence == null) InitializeComponents();
            string containerName = _saveManager.chunkContainer != null 
                ? _saveManager.chunkContainer.name 
                : "UnnamedChunkContainer";
                
            persistence.SaveMapToJson(saveFilePath, seed, currentChunkLeftId, currentChunkRightId, _chunkList, containerName);
        }

        public void LoadMapFromJson(string mapFilePath)
        {
            if (persistence == null) InitializeComponents();
            MapSaveData data = persistence.LoadMapFromJson(mapFilePath, _saveManager.chunkContainer, out List<Chunk> loadedChunks);
            
            if (data != null)
            {
                seed = data.seed;
                rng = new System.Random(seed);
                currentChunkLeftId = data.currentChunkLeftId;
                currentChunkRightId = data.currentChunkRightId;
                _chunkList = loadedChunks;
                _chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
            }
        }

        public void test() { }
    }
}