using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Script.Data_Model;
using Newtonsoft.Json;
using Script.Save_Controller;

namespace Script.Manager.SaveManager
{
    public class WorldSaveListManager : MonoBehaviour
    {
        // dùng trong menu scene
        public static WorldSaveListManager Instance { get; private set; }
        public string saveRootPath;

        public List<WorldInfo> WorldInfos = new List<WorldInfo>();

        private void Awake()     
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject); 
            }                     
            
            string rootPath = Application.persistentDataPath;
            saveRootPath = Path.Combine(rootPath, "saves");
        }

        public void CreateSavesFolderIfNotExists()
        {
            if (!Directory.Exists(saveRootPath))
            {
                Directory.CreateDirectory(saveRootPath);
            }
        }

        public bool CreateWorldSave(string worldName, string seedValue)
        {
            if (string.IsNullOrEmpty(worldName))
            {
                Debug.LogError("Tên thế giới để trống nên không thể tạo được");
                return false;
            }

            int seed;
            if (!int.TryParse(seedValue, out seed))
            {
                seed = 0; 
                Debug.LogWarning("seed nhập vào không thể chuyển đổi,được đặt mặc định là 0");
            }

            string worldPath = Path.Combine(saveRootPath, worldName);

            while (Directory.Exists(worldPath))
            {
                Debug.LogError("Tên thế giới đã tồn tại,không thể tạo được");
                return false;
            }

            Directory.CreateDirectory(worldPath);
            SaveWorldInfoToJson(worldName, seed, worldPath);
            
            Directory.CreateDirectory(Path.Combine(worldPath, "map"));
            
            return true;
        }

        private WorldInfo SaveWorldInfoToJson(string worldName, int seed, string worldPath)
        {
            WorldInfo newWorldInfo = new WorldInfo(
                name: worldName,
                creationDate: System.DateTime.Now.ToString("O"),
                seed: seed,
                gameHour: 7,
                gameMinute: 0,
                gameDay: 1,
                gameMonth: 1,
                gameYear: 1,
                techFireArrow: false,
                techPenetrating: false,
                techAdhesive: false,
                lastPlayedDate: System.DateTime.Now.ToString("O"),
                hasShowGuideArrow: false
            );

            string worldInfoPath = Path.Combine(worldPath, "worldInfo.json");
            string worldInfoJson = JsonConvert.SerializeObject(newWorldInfo, Formatting.Indented);
            File.WriteAllText(worldInfoPath, worldInfoJson);
            
            return newWorldInfo;
        }

        public string GetWorldFolderPath(string worldName)
        {
            string worldFolderPath = Path.Combine(saveRootPath, worldName);

            if (Directory.Exists(worldFolderPath))
            {
                return worldFolderPath; 
            }
            else
            {
                Debug.LogWarning($"World folder '{worldName}' does not exist at path: {worldFolderPath}");
                return null; 
            }
        }

        public void DeleteWorldSave(string worldName)
        {
            string worldPath = Path.Combine(saveRootPath, worldName);

            if (Directory.Exists(worldPath))
            {
                Directory.Delete(worldPath, true); 
            }
            else
            {
                Debug.LogError($"World save directory does not exist: {worldName}");
            }
        }

        public void LoadWorldInfosToChooseSavePanel()
        {
            WorldInfos.Clear();

            if (Directory.Exists(saveRootPath))
            {
                string[] directories = Directory.GetDirectories(saveRootPath);

                foreach (string dir in directories)
                {
                    string worldInfoPath = Path.Combine(dir, "worldInfo.json");

                    if (File.Exists(worldInfoPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(worldInfoPath);
                            WorldInfo worldInfo = JsonConvert.DeserializeObject<WorldInfo>(json);

                            if (worldInfo != null)
                            {
                                WorldInfos.Add(worldInfo);
                                Debug.Log($"[WorldSaveListManager] Loaded World: {worldInfo.Name} | LastPlayedDate: {worldInfo.LastPlayedDate}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"Failed to load WorldInfo from {worldInfoPath}. Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"worldInfo.json not found in directory: {dir}");
                    }
                }
            }
            else
            {
                Debug.LogError("Save root path does not exist.");
            }

            // Sort list by LastPlayedDate descending (newest first)
            WorldInfos.Sort((a, b) => {
                string dateA = string.IsNullOrEmpty(a.LastPlayedDate) ? "" : a.LastPlayedDate;
                string dateB = string.IsNullOrEmpty(b.LastPlayedDate) ? "" : b.LastPlayedDate;
                return string.Compare(dateB, dateA, System.StringComparison.Ordinal);
            });

            Debug.Log("--- Final Sorted World List ---");
            for (int i = 0; i < WorldInfos.Count; i++)
            {
                Debug.Log($"[{i}] {WorldInfos[i].Name} - last play: {WorldInfos[i].LastPlayedDate}");
            }
        }

        public int CountWorldArr()
        {
            return WorldInfos.Count;
        }

        public WorldInfo GetWorldInfoByIndex(int index)
        {
            if (index >= 1 && index <= WorldInfos.Count)
            {
                return WorldInfos[index - 1]; 
            }
            else
            {
                Debug.LogError("GetWorldInfoByIndex: Index out of range");
                return null;
            }
        }

        public void SetActiveWorldPath(string path)
        {
            PlayerPrefs.SetString("ActiveWorldPath", path);
            PlayerPrefs.Save();
        }

        public WorldInfo[] GetAllWorldInfos()
        {
            LoadWorldInfosToChooseSavePanel();
            return WorldInfos.ToArray();
        }
    }
}
