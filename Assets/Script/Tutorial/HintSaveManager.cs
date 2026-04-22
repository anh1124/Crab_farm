using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Script.Tutorial
{
    /// <summary>
    /// Singleton — quản lý trạng thái "đã ignore" của tutorial.
    /// Lưu theo từng world vào file Tutorials.json trong /saves/{world_name}/
    /// </summary>
    public class HintSaveManager : MonoBehaviour
    {
        [System.Serializable]
        private class TutorialSaveData
        {
            public List<string> ignoredTitles = new List<string>();
            public List<string> readTitles = new List<string>();
        }

        public static HintSaveManager Instance { get; private set; }

        private HashSet<string> _ignoredTitles = new HashSet<string>();
        private HashSet<string> _readTitles = new HashSet<string>();
        private string _currentSavePath = null;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        // ── Save / Load ────────────────────────────────────────────

        /// <summary>
        /// Load danh sách ignore từ file Tutorials.json trong thư mục save đang hoạt động.
        /// Gọi sau khi worldInfo đã được load (trong LoadFromSave.LoadWorldInfoFromSave).
        /// </summary>
        public void Load(string worldFolderPath)
        {
            _ignoredTitles.Clear();
            _readTitles.Clear();
            _currentSavePath = worldFolderPath;

            if (string.IsNullOrEmpty(worldFolderPath)) return;

            string path = GetFilePath(worldFolderPath);
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<TutorialSaveData>(json);
                if (data != null)
                {
                    _ignoredTitles = new HashSet<string>(data.ignoredTitles ?? new List<string>());
                    _readTitles = new HashSet<string>(data.readTitles ?? new List<string>());
                }

                Debug.Log($"[HintSaveManager] Loaded ignored={_ignoredTitles.Count}, read={_readTitles.Count} from {path}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HintSaveManager] Failed to load Tutorials.json: {ex.Message}");
            }
        }

        /// <summary>
        /// Lưu danh sách ignore vào file Tutorials.json.
        /// Gọi từ WorldFileManager.SaveCurrentMap.
        /// </summary>
        public void Save(string worldFolderPath)
        {
            if (string.IsNullOrEmpty(worldFolderPath)) return;

            try
            {
                string path = GetFilePath(worldFolderPath);
                var data = new TutorialSaveData
                {
                    ignoredTitles = new List<string>(_ignoredTitles),
                    readTitles = new List<string>(_readTitles)
                };
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HintSaveManager] Failed to save Tutorials.json: {ex.Message}");
            }
        }

        // ── State API ──────────────────────────────────────────────

        public bool IsIgnored(string title) => _ignoredTitles.Contains(title);

        public void SetIgnored(string title)
        {
            if (string.IsNullOrEmpty(title)) return;
            _ignoredTitles.Add(title);

            if (!string.IsNullOrEmpty(_currentSavePath))
                Save(_currentSavePath);
        }

        public void ClearIgnored(string title)
        {
            if (string.IsNullOrEmpty(title)) return;

            _ignoredTitles.Remove(title);
            if (!string.IsNullOrEmpty(_currentSavePath))
                Save(_currentSavePath);
        }

        public bool IsRead(string title) => _readTitles.Contains(title);

        public void SetRead(string title)
        {
            if (string.IsNullOrEmpty(title)) return;
            _readTitles.Add(title);

            if (!string.IsNullOrEmpty(_currentSavePath))
                Save(_currentSavePath);
        }

        public void ClearRead(string title)
        {
            if (string.IsNullOrEmpty(title)) return;

            _readTitles.Remove(title);
            if (!string.IsNullOrEmpty(_currentSavePath))
                Save(_currentSavePath);
        }

        /// <summary>Debug helper: xóa toàn bộ lịch sử ignore của world hiện tại.</summary>
        [ContextMenu("DEBUG: Clear All")]
        public void ClearAll()
        {
            _ignoredTitles.Clear();
            _readTitles.Clear();
            if (!string.IsNullOrEmpty(_currentSavePath))
                Save(_currentSavePath);
            Debug.Log("[HintSaveManager] Cleared all hint states.");
        }

        // ── private ────────────────────────────────────────────────

        private string GetFilePath(string folderPath) => Path.Combine(folderPath, "Tutorials.json");
    }

    [System.Obsolete("Renamed to HintSaveManager. Kept for backward-compatible Unity references.")]
    public class TutorialSaveManager : HintSaveManager
    {
    }
}
