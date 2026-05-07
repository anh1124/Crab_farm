using UnityEngine;
using Script.Game.Other.Nature;

namespace Script.Tutorial.Core.Managers
{
    /// <summary>
    /// Quản lý các bụi cỏ thỏ (RabbitBush) trong Tutorial.
    /// Cho phép kích hoạt bụi cỏ và ép buộc sinh thỏ theo kịch bản.
    /// </summary>
    public class TutorialRabbitManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Kéo đối tượng RabbitBushBehavior trong scene vào đây.")]
        [SerializeField] private RabbitBushBehavior rabbitBush;

        /// <summary>
        /// Kích hoạt bụi cỏ (nếu đang bị tắt) và sinh ra một số lượng thỏ nhất định.
        /// </summary>
        public void SpawnRabbits(int count)
        {
            if (rabbitBush == null)
            {
                Debug.LogError("<color=red>[TUTORIAL-RABBIT]</color> LỖI: rabbitBush chưa được gán trong Inspector! Hành động sinh thỏ bị hủy.");
                return;
            }

            // Đảm bảo bụi cỏ được bật
            if (!rabbitBush.gameObject.activeSelf)
            {
                rabbitBush.gameObject.SetActive(true);
                Debug.Log($"<color=green>[TUTORIAL-RABBIT]</color> Đã kích hoạt GameObject: <b>{rabbitBush.gameObject.name}</b>");
            }

            if (count > 0)
            {
                Debug.Log($"<color=cyan>[TUTORIAL-RABBIT]</color> Bắt đầu sinh <b>{count}</b> con thỏ tại bụi cỏ.");
                // Sinh thỏ
                for (int i = 0; i < count; i++)
                {
                    rabbitBush.SpawnRabbit();
                }
            }
            else
            {
                Debug.Log("<color=yellow>[TUTORIAL-RABBIT]</color> Tham số count = 0, chỉ kích hoạt bụi cỏ mà không sinh thỏ.");
            }
        }
        
        public void HideBush()
        {
            if (rabbitBush != null)
                rabbitBush.gameObject.SetActive(false);
        }
    }
}
