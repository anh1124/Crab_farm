using Script.Data_Model.Interfaces;
using Script.Interfaces;
using Script.Save_Controller;
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Script.Data_Model.Status
{
    /// <summary>
    /// Data container thuần túy cho Player runtime state.
    /// KHÔNG chứa logic khởi tạo — giá trị mặc định được quản lý bởi PlayerConfig.
    /// PlayerBehavior chịu trách nhiệm điều phối: load từ save → dùng Config mặc định.
    /// </summary>
    public class PlayerStatus : MonoBehaviour, ISaveable
    {
        // Flag: true nếu RestoreData() đã chạy thành công → bỏ qua ApplyConfig trong PlayerBehavior Awake
        [HideInInspector]
        public bool isDataRestoredFromSave = false;

        [Serializable]
        public class PlayerStatusData
        {
            public float maxHp;
            public float currentHp;
            public int coin;
            public float moveSpeed;
            public float positionX;
            public float positionY;
            public bool isGround;
            
            // NEW: Resource data for save/load
            public float currentMana;

            [JsonProperty("skillManager")]
            public SkillManagerSaveData skillManager;
        }
        // Event bắn ra mỗi khi số coin thay đổi — UI lắng nghe event này
        public event Action<int> OnCoinChanged;

        [Header("Resources")]
        public ResourceStat HP = new ResourceStat();
        public ResourceStat Mana = new ResourceStat();

        public int maxCoins;
        public float moveSpeed;
        public float hpRegenRate;
        public float manaRegenRate;

        [Header("Combat")]
        [Tooltip("Bonus damage cộng thêm vào mọi skill. CheatManager set 100000 khi instanceKill bật.")]
        public float damage = 0f;
        
        // TODO: Chuyển isGround, positionX, positionY về PlayerMovement.
        // Các field này là physics state, không phải player resource.
        public float positionX;
        public float positionY;
        public bool isGround;

        // Coin sử dụng property để tự động bắn event khi thay đổi
        [SerializeField] private int _coin;
        public int coin
        {
            get => _coin;
            set
            {
                if (_coin == value) return;
                _coin = value;
                OnCoinChanged?.Invoke(_coin);
            }
        }

        // Không có Awake() — PlayerBehavior.ApplyConfig() là nơi khởi tạo duy nhất
        // Không có Reset() — giá trị mặc định sống trong PlayerConfig ScriptableObject

        [ContextMenu("Trừ 2 HP")]
        public void Deduct2HP()
        {
            HP.Reduce(2f);
            Debug.Log($"[PlayerStatus] Đã trừ 2 HP. HP hiện tại: {HP.Current}/{HP.Max}");
        }

        [ContextMenu("Tăng 10 Max HP")]
        public void Add10MaxHP()
        {
            HP.SetMax(HP.Max + 10f);
            Debug.Log($"[PlayerStatus] Đã tăng 10 Max HP. Max HP mới: {HP.Max}");
        }

        [ContextMenu("Hồi đầy máu")]
        public void HealFull()
        {
            HP.Add(HP.Max);
            Debug.Log($"[PlayerStatus] Đã hồi đầy máu. HP hiện tại: {HP.Current}/{HP.Max}");
        }

        // ==========================================
        // ISaveable Implementation
        // ==========================================
        public string GetPrefabID() => "player";

        public object CaptureData()
        {
            return new PlayerStatusData
            {
                maxHp = HP.Max,
                currentHp = HP.Current,
                coin = this.coin,
                moveSpeed = this.moveSpeed,
                positionX = transform.position.x,
                positionY = transform.position.y,
                isGround = this.isGround,
                currentMana = Mana.Current,
                skillManager = SkillManagerSaveUtility.CaptureFrom(this)
            };
        }

        public void RestoreData(object data)
        {
            PlayerStatusData loaded = null;
            if (data is Newtonsoft.Json.Linq.JObject jObj)
                loaded = jObj.ToObject<PlayerStatusData>();
            else
                loaded = data as PlayerStatusData;

            if (loaded == null) return;

            // Đánh dấu đã load từ save — Awake sẽ bỏ qua ApplyConfig
            isDataRestoredFromSave = true;

            HP.SetMax(loaded.maxHp, true);
            if (loaded.currentHp < loaded.maxHp)
                HP.Reduce(loaded.maxHp - loaded.currentHp);

            this.coin = loaded.coin;
            this.moveSpeed = loaded.moveSpeed;
            this.isGround = loaded.isGround;
            transform.position = new Vector3(loaded.positionX, loaded.positionY, 0);

            // Fetch default config if possible
            var behavior = GetComponent<Script.Game.Player.PlayerBehavior>();
            float maxMana = (behavior != null && behavior.defaultConfig != null) ? behavior.defaultConfig.maxMana : 50f;
            
            Mana.SetMax(maxMana, true);
            if (loaded.currentMana < maxMana)
                Mana.Reduce(maxMana - loaded.currentMana);

            // Force refresh UI sau khi restore xong (trong trường hợp CoinUI chưa đăng ký kịp)
            OnCoinChanged?.Invoke(_coin);

            var skillMgr = GetComponent<Script.Game.PlayerCharacter.Skills.SkillManager>();
            SkillManagerSaveUtility.Restore(skillMgr, loaded.skillManager, gameObject);

            Debug.Log($"[PlayerStatus] Restored save data. HP={HP.Current}/{HP.Max}, Mana={Mana.Current}/{Mana.Max}, Coin={_coin}");
        }
    }
}
