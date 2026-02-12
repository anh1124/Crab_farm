using UnityEngine;
using Script.Data_Model.Interfaces;
using Script.Game.Mobs.Interfaces;
using Script.Game.Item.Coin;

namespace Script.Game.Mobs.Components
{
    /// <summary>
    /// Xử lý tương tác vật lý khi nhặt xu.
    /// Ủy thác việc "Lưu Trữ" cho ICoinable (Status).
    /// Ủy thác việc "Phản ứng" cho ICoinReactor (Behavior).
    /// </summary>
    public class CoinCollector : MonoBehaviour
    {
        private ICoinable coinStorage;
        private ICoinReactor currentReactor;

        private void Awake()
        {
            // Cache bộ lưu trữ (Status) - thường không đổi trong suốt vòng đời object
            coinStorage = GetComponent<ICoinable>();
            if (coinStorage == null)
            {
                Debug.LogWarning($"[CoinCollector] {gameObject.name} không có component ICoinable (Status). Xu sẽ không được lưu.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Coin"))
            {
                HandleCoinCollision(other.gameObject);
            }
        }

        private void HandleCoinCollision(GameObject coinObj)
        {
            // Tìm reactor đang hoạt động (Behavior) - Cần tìm lại mỗi lần vì behavior có thể thay đổi (Vagrant <-> Villager)
            UpdateActiveReactor();

            Script.Utility.ProcessCoinPickup(coinObj,
                condition: () => 
                {
                    // 0. Kiểm tra Cooldown (Fix lỗi Archer destroy coin sớm)
                    var coinScript = coinObj.GetComponent<Coin>();
                    if (coinScript != null)
                    {
                        if (gameObject.CompareTag("Player"))
                        {
                            if (!coinScript.playerPickupAble) return false;
                        }
                        else 
                        {
                            // Giả định là Denizen hoặc NPC khác
                            if (!coinScript.denizensPickupAble) return false;
                        }
                    }

                    // 1. Kiểm tra xem có còn sức chứa không
                    bool hasCapacity = false;
                    if (coinStorage != null)
                    {
                        hasCapacity = coinStorage.Coin < coinStorage.MaxCoins;
                    }
        
                    if (currentReactor != null) return true; // Reactors (Vagrant) luôn nhặt
                    return hasCapacity;
                },
                onPickupSuccess: () =>
                {
                    var coinScript = coinObj.GetComponent<Coin>();

                    // 2. Thêm vào Kho chứa
                    if (coinStorage != null)
                    {
                        coinStorage.AddCoin(1); 
                        // Debug.Log($"[CoinCollector] Đã thêm 1 xu vào {gameObject.name}. Tổng: {coinStorage.Coin}");
                    }

                    // 3. Kích hoạt Phản ứng
                    if (currentReactor != null)
                    {
                        currentReactor.OnCoinPickedUp(coinScript);
                    }
                }
            );
        }

        private void UpdateActiveReactor()
        {
            // Tìm TẤT CẢ component tương tác với reactor, kiểm tra cái nào đang bật (enabled)
            // Việc này hơi tốn chi phí mỗi lần nhặt, nhưng an toàn hơn cho việc chuyển đổi behavior
            var reactors = GetComponents<ICoinReactor>();
            currentReactor = null;
            foreach (var r in reactors)
            {
                if (r is MonoBehaviour mb && mb.enabled)
                {
                    currentReactor = r;
                    break; 
                }
            }
        }
    }
}
