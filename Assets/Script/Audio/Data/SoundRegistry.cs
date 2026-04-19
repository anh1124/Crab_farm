using UnityEngine;
using System.Collections.Generic;

namespace Script.Audio
{
    /// <summary>
    /// ScriptableObject quản lý danh sách âm thanh theo key.
    /// Cho phép truy xuất AudioClip thông qua string key thay vì direct reference.
    ///
    /// ─── CÁCH DÙNG ────────────────────────────────────────────────────────────
    /// 1. Trong Unity Project: Create → Audio → Sound Registry
    /// 2. Đặt tên asset (ví dụ: GameSoundRegistry)
    /// 3. Trong Inspector, tạo các SoundGroup và SoundEntry:
    ///    - Group: "Enemy" → Entries: "Goblin_Hurt", "Goblin_Death"
    ///    - Group: "Player" → Entries: "Footstep_Grass", "Sword_Swing"
    /// 4. Kéo AudioClip tương ứng vào mỗi entry
    /// 5. Trong AudioManager, kéo asset này vào field SoundRegistry
    ///
    /// ─── CODE ────────────────────────────────────────────────────────────────────
    /// AudioManager.Instance.PlaySFXByKey("Goblin_Hurt", transform.position);
    /// AudioManager.Instance.PlaySFXFollowingByKey("Footstep_Grass", transform, loop: true);
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    [CreateAssetMenu(fileName = "SoundRegistry", menuName = "Audio/Sound Registry")]
    public class SoundRegistry : ScriptableObject
{
    [System.Serializable]
    public class SoundGroup
    {
        public string groupName;
        public SoundEntry[] entries;
    }

    [System.Serializable]
    public class SoundEntry
    {
        public string key;
        public AudioClip clip;
        [Tooltip("Khoảng cách tối đa để nghe to nhất (đơn vị Unity)")]
        public float minDistance = 2f;
        [Tooltip("Khoảng cách âm thanh hoàn toàn im (đơn vị Unity)")]
        public float maxDistance = 15f;
    }

    public SoundGroup[] groups;

    private Dictionary<string, AudioClip> _clipLookup;
    private Dictionary<string, SoundEntry> _entryLookup;

    /// <summary>
    /// Khởi tạo dictionary lookup từ groups.
    /// Gọi tự động khi cần, có thể gọi thủ công để pre-load.
    /// </summary>
    public void Initialize()
    {
        _clipLookup = new Dictionary<string, AudioClip>();
        _entryLookup = new Dictionary<string, SoundEntry>();
        
        foreach (var group in groups)
        {
            if (group.entries == null) continue;
            
            foreach (var entry in group.entries)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                {
                    // Nếu key trùng lặp, log warning và bỏ qua
                    if (_clipLookup.ContainsKey(entry.key))
                    {
                        Debug.LogWarning($"[SoundRegistry] Key trùng lặp: {entry.key}. Entry đầu tiên sẽ được ưu tiên.");
                    }
                    else
                    {
                        _clipLookup.Add(entry.key, entry.clip);
                        _entryLookup.Add(entry.key, entry);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Lấy AudioClip theo key.
    /// </summary>
    /// <param name="key">ID của âm thanh (ví dụ: "Goblin_Hurt")</param>
    /// <returns>AudioClip nếu tìm thấy, null nếu không</returns>
    public AudioClip GetClip(string key)
    {
        if (_clipLookup == null)
        {
            Initialize();
        }

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[SoundRegistry] Key rỗng, không thể tìm AudioClip.");
            return null;
        }

        return _clipLookup.TryGetValue(key, out var clip) ? clip : null;
    }

    /// <summary>
    /// Kiểm tra xem key có tồn tại trong registry không.
    /// </summary>
    public bool HasKey(string key)
    {
        if (_clipLookup == null) Initialize();
        return _clipLookup.ContainsKey(key);
    }

    /// <summary>
    /// Lấy SoundEntry đầy đủ theo key (bao gồm clip và khoảng cách).
    /// </summary>
    public SoundEntry GetEntry(string key)
    {
        if (_entryLookup == null) Initialize();
        
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[SoundRegistry] Key rỗng, không thể tìm SoundEntry.");
            return null;
        }

        return _entryLookup.TryGetValue(key, out var entry) ? entry : null;
    }

    /// <summary>
    /// Lấy tất cả keys có trong registry (dùng cho debug hoặc UI).
    /// </summary>
    public string[] GetAllKeys()
    {
        if (_clipLookup == null) Initialize();
        if (_clipLookup == null) return new string[0]; // Guard nếu Initialize thất bại

        var keys = new string[_clipLookup.Count];
        _clipLookup.Keys.CopyTo(keys, 0);
        return keys;
    }
}
}
