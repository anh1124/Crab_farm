using UnityEngine;
using Script.Controller.PlayerController;
using Script.Controller.GUI_Controller.PlayerGui;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Manager;

namespace Script.Game.Other
{
    public partial class StartGameScrene
    {
        private void InitializePlayer()
        {
            if (BuildMenu.instance != null)
            {
                BuildMenu.instance.InitializePlayer();
            }
        }

        public void InstancePlayerAndSetCamera()
        {
            Debug.Log("👤 PHASE 5: Creating/setup player object...");

            if (PlayerController.Instance == null || EntitySaveManager.Instance == null)
            {
                Debug.LogError("Essential Player-creation systems are missing!");
                return;
            }

            GameObject player = null;
            Vector2 spawnPosition = Vector2.zero;

            // Tutorial scene: nếu player đã được gán sẵn trong PlayerController, dùng luôn
            if (PlayerController.Instance.player != null)
            {
                player = PlayerController.Instance.player;
                Debug.Log($"[StartGameScrene] Using existing player from scene: {player.name}");
            }
            else if (isTutorialScene)
            {
                Debug.LogWarning("[StartGameScrene] TutorialScene but no player assigned to PlayerController! Checking scene for player...");
                // Tìm player trong scene bằng tag hoặc component
                player = GameObject.FindGameObjectWithTag(Script.Utils.GameTags.Player);
                if (player != null)
                {
                    PlayerController.Instance.player = player;
                    Debug.Log($"[StartGameScrene] Found player in scene by tag: {player.name}");
                }
            }

            // Nếu không tìm thấy player trong scene, spawn từ PrefabData
            if (player == null)
            {
                if (PrefabData.Instance == null || PrefabData.Instance.player == null)
                {
                    Debug.LogError("PrefabData.Instance or player prefab is missing!");
                    return;
                }

                // Chỉ đọc position từ save nếu không phải tutorial scene
                if (!isTutorialScene)
                {
                    string playerPath = Path.Combine(LoadFromSave.Instance.saveFilePath, "PlayerContainer.json");

                    if (!string.IsNullOrEmpty(LoadFromSave.Instance.saveFilePath) && File.Exists(playerPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(playerPath);
                            var savedObjects = JsonConvert.DeserializeObject<List<EntitySaveManager.SavedObject>>(json,
                                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                            if (savedObjects != null && savedObjects.Count > 0)
                            {
                                var playerData = savedObjects[0].data as Script.Data_Model.Status.PlayerStatus.PlayerStatusData;
                                if (playerData != null)
                                {
                                    spawnPosition = new Vector2(playerData.positionX, playerData.positionY);
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Failed to read player position from save: {e.Message}");
                        }
                    }
                }
                else
                {
                    Debug.Log("[StartGameScrene] TutorialScene: Using default spawn position (0,0)");
                }

                player = EntitySaveManager.Instance.InstanceAndMoveObjectToContainer(PrefabData.Instance.player, spawnPosition);
                if (player == null) return;

                player.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0);
                PlayerController.Instance.player = player;
                Debug.Log($"[StartGameScrene] Spawned new player from PrefabData at {spawnPosition}");
            }

            PlayerController.Instance.SetUpControllerPlayer();

            if (PlayerGui.Instance != null)
            {
                PlayerGui.Instance.RefreshSkillManagerCache();
            }

            if (CameraFollowPlayer.Instance != null)
            {
                CameraFollowPlayer.Instance.setCameraTarget();
                CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);
            }

            // LoadAllContainer đã chạy trước khi spawn player → LoadAndSetupPlayer thoát sớm. Áp save tại đây.
            if (EntitySaveManager.Instance != null && !isTutorialScene)
                EntitySaveManager.Instance.ApplyPlayerSaveAfterSpawn();

            Debug.Log($" Player setup completed. Position: {player.transform.position}");
        }
    }
}
