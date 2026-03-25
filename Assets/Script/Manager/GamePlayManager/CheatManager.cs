using Script.Game.Other.Nature;
using Script.Game.Player;
using Script.Controller.PlayerController;
using UnityEngine;

namespace Script.Manager.GamePlayManager
{
    /// <summary>
    /// Cheat Panel tập trung cho mục đích phát triển / debug.
    /// Được yêu cầu bởi StartGameScrene (RequireComponent).
    ///
    /// ── Danh sách cheat ──────────────────────────────────────────
    ///  [Tree]
    ///   isInstanceChopTree  : Cây ngã ngay sau khi player nhét đủ coin (tạm thay Denizen Builder)
    ///
    ///  [Player – bật qua ApplyDevCheats() lúc game start]
    ///   addCurrentCoin      : Cộng đúng số coin này vào player khi khởi tạo
    ///   damageAlly          : Cho phép player đánh đồng minh
    ///
    ///  [Player – cheat stat cực lớn lúc khởi tạo (instanceXxx)]
    ///   instanceKill  → player.status.damage = 100000 (skill 1 hit kill IDamageable có HP)
    ///   infiniteHp    → HP.Max = 10000, HP đầy
    ///   infiniteMp    → Mana.Max = 10000, Mana đầy
    ///   infiniteCoin  → Coin = 10000 và maxCoins = 10000
    ///
    ///  Lưu ý về Denizen:
    ///   Denizen không dùng IDamageable thông thường — mỗi đòn đánh chỉ làm mất 1 coin/tool.
    ///   instanceKill không thể kill Denizen trong 1 hit; đây là đặc tính thiết kế.
    /// ─────────────────────────────────────────────────────────────
    /// </summary>
    public class CheatManager : MonoBehaviour
    {
        public static CheatManager Instance { get; private set; }

        // ── Tree ──────────────────────────────────────────────────
        [Header("── Tree Chop (TEMP: thay thế Denizen Builder) ──")]
        [Tooltip("Cây ngã ngay sau khi player nhét đủ coin.\nXóa cờ này khi Denizen Builder được tạo xong.")]
        public bool isInstanceChopTree = true;

        // ── Player: Áp dụng 1 lần khi ApplyDevCheats() được gọi ──
        [Header("── Player: Dev Start Cheats ──")]
        [Tooltip("Số coin cộng vào player khi bắt đầu game.")]
        public int addCurrentCoin = 50;

        [Tooltip("Cho phép player gây sát thương lên đồng minh (Denizen, v.v.)")]
        public bool damageAlly = true;

        [Tooltip("player.status.damage = 100000 → mọi skill sẽ 1-hit kill IDamageable có HP.\nKHÔNG kill được Denizen vì Denizen chỉ mất 1 coin/tool mỗi đòn.")]
        public bool instanceKill = false;

        [Tooltip("Mana.Max = 10000 và fill đầy.")]
        public bool infiniteMp = true;

        [Tooltip("HP.Max = 10000 và fill đầy.")]
        public bool infiniteHp = true;

        [Tooltip("Coin = 10000 và maxCoins = 10000.")]
        public bool infiniteCoin = false;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        // ─────────────────────────────────────────────────────────
        // Gọi bởi StartGameScrene.DevelopTest() (chỉ UNITY_EDITOR)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Áp dụng tất cả stat cheat cho player 1 lần khi game khởi động trong Editor.
        /// Gọi bởi StartGameScrene.DevelopTest().
        /// </summary>
        public void ApplyDevCheats()
        {
            if (PlayerController.Instance == null || PlayerController.Instance.playerBehavior == null)
            {
                Debug.LogWarning("[CheatManager] ApplyDevCheats: PlayerController hoặc playerBehavior chưa sẵn sàng.");
                return;
            }

            var pb  = PlayerController.Instance.playerBehavior;
            var st  = pb.status;

            // damageAlly
            PlayerController.Instance.damageAlly = damageAlly;

            // Coin cộng thêm ban đầu
            if (addCurrentCoin > 0)
                pb.Coin += addCurrentCoin;

            // infiniteHp → HP max = 10000
            if (infiniteHp)
            {
                st.HP.SetMax(10000f, refillCurrent: true);
                Debug.Log("[CheatManager] infiniteHp: HP.Max = 10000");
            }

            // infiniteMp → Mana max = 10000
            if (infiniteMp)
            {
                st.Mana.SetMax(10000f, refillCurrent: true);
                Debug.Log("[CheatManager] infiniteMp: Mana.Max = 10000");
            }

            // infiniteCoin → Coin & maxCoins = 10000
            if (infiniteCoin)
            {
                st.maxCoins = 10000;
                pb.Coin     = 10000;
                Debug.Log("[CheatManager] infiniteCoin: Coin = 10000");
            }

            // instanceKill → bonus damage cực lớn
            // Lưu ý: chỉ ảnh hưởng IDamageable có HP thật (Enemy, Wall, TownCenter).
            // Denizen shield bằng coin/tool nên vẫn không 1-hit kill được.
            if (instanceKill)
            {
                st.damage = 100000f;
                Debug.Log("[CheatManager] instanceKill: player.status.damage = 100000 (skill damage sẽ = skill.damage + 100000)");
            }

            Debug.LogWarning($"[CheatManager] Dev cheats applied — HP:{st.HP.Max}, Mana:{st.Mana.Max}, Coin:{pb.Coin}, Damage bonus:{st.damage}, damageAlly:{damageAlly}");
        }

        // ─────────────────────────────────────────────────────────
        // Tree Chop Simulation
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Được gọi bởi NatureTreeBehavior.SetChopFlag().
        /// Nếu isInstanceChopTree = true → lập tức hoàn thành toàn bộ tiến độ chặt cây.
        /// </summary>
        public void TrySimulateChop(NatureTreeBehavior tree)
        {
            if (!isInstanceChopTree) return;

            Debug.Log($"[CheatManager] isInstanceChopTree=true → Instant chop '{tree.gameObject.name}'");
            tree.AddWorkProcess(tree.MaxWorkProgress);
        }
    }
}
