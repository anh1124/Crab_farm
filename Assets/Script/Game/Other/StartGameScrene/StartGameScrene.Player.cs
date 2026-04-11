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

            if (PlayerController.Instance == null || EntitySaveManager.Instance == null || PrefabData.Instance == null || PrefabData.Instance.player == null)
            {
                Debug.LogError("Essential Player-creation systems are missing!");
                return;
            }

            Vector2 spawnPosition = Vector2.zero;
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

            GameObject player = EntitySaveManager.Instance.InstanceAndMoveObjectToContainer(PrefabData.Instance.player, spawnPosition);
            if (player == null) return;

            player.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0);

            PlayerController.Instance.player = player;
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
            if (EntitySaveManager.Instance != null)
                EntitySaveManager.Instance.ApplyPlayerSaveAfterSpawn();

            Debug.Log($" Player instance created at {spawnPosition} and systems setup completed");
        }
    }
}
