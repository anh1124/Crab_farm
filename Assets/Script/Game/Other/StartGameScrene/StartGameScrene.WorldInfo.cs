using UnityEngine;
using Script.Save_Controller;
using Script.Manager;

namespace Script.Game.Other
{
    public partial class StartGameScrene
    {
        private void ValidateWorldData()
        {
            if (WorldFileManager.Instance == null)
                throw new System.Exception("WorldFileManager.Instance is null");

            if (WorldFileManager.Instance.worldInfo == null)
                Debug.LogWarning(" WorldInfo is null - using default world");
        }

        private bool InitializeActiveWorld()
        {
            if (WorldFileManager.Instance != null && !WorldFileManager.Instance.ValidateAndLoadActiveWorld())
            {
                return false; // Fail, return to menu handled by WorldFileManager
            }
            return true;
        }

        private void LoadSavedEntitiesPhased()
        {
            // Kiểm tra có nên load save không (isTutorialScene ưu tiên cao hơn)
            if (!ShouldLoadSave)
            {
                Debug.Log("[StartGameScrene] TutorialScene hoặc loadSaveFromPref = false, bỏ qua load save nhưng vẫn setup container");
                // Fallback: đảm bảo container được setup cho tutorial scene
                if (EntitySaveManager.Instance != null)
                {
                    EntitySaveManager.Instance.ResetupAllContainers();
                    Debug.Log("[StartGameScrene] Container đã được resetup cho tutorial scene");
                }
                return;
            }

            if (WorldFileManager.Instance != null && WorldFileManager.Instance.IsActiveWorldPathValid())
            {
                LoadFromSave.Instance.LoadTechAndTimeFromSave();
                LoadFromSave.Instance.LoadEntitiesFromSave();
            }
        }

        private void LoadWorldInfoIfNeeded()
        {
            if (WorldFileManager.Instance == null) return;

            string savePath = PlayerPrefs.GetString("ActiveWorldPath", "");
            if (string.IsNullOrEmpty(savePath)) return;

            try
            {
                string worldName = ExtractWorldNameFromSavePath(savePath);
                if (string.IsNullOrEmpty(worldName)) { CreateDefaultWorldInfo(); return; }

                WorldInfo loadedInfo = WorldFileManager.Instance.LoadWorldInfo(worldName);
                if (loadedInfo != null) WorldFileManager.Instance.SyncTechToPrefabData();
                else CreateDefaultWorldInfo();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Lỗi khi tải WorldInfo: {ex.Message}");
                CreateDefaultWorldInfo();
            }
        }

        private string ExtractWorldNameFromSavePath(string savePath)
        {
            if (string.IsNullOrEmpty(savePath)) return string.Empty;
            string normalizedPath = savePath.Replace('\\', '/');
            string[] parts = normalizedPath.Split('/');
            
            int savesIndex = -1;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "saves") { savesIndex = i; break; }
            }
            
            if (savesIndex >= 0 && savesIndex + 1 < parts.Length) return parts[savesIndex + 1];
            if (parts.Length >= 2) return parts[parts.Length - 2];
            return string.Empty;
        }

        private void CreateDefaultWorldInfo()
        {
            WorldInfo defaultInfo = new WorldInfo(
                name: "DefaultWorld",
                creationDate: System.DateTime.Now.ToString("O"),
                seed: 0, 9, 0, 1, 1, 1
            );
            
            WorldFileManager.Instance.worldInfo = defaultInfo;
            WorldFileManager.Instance.SyncTechToPrefabData();
        }
    }
}
