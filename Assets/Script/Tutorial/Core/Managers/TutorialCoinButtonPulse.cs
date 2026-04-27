using UnityEngine;
using Script.Controller.GUI_Controller.PlayerGui;

namespace Script.Tutorial
{
    /// <summary>
    /// Script điều phối hiệu ứng nhấp nháy cho nút bấm trong Tutorial.
    /// Script này đóng vai trò là "Proxy" để TutorialController điều khiển 
    /// thông qua các Action, và nó sẽ trực tiếp cấu hình ButtonGoldPulseEffect.
    /// </summary>
    public class TutorialCoinButtonPulse : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ButtonGoldPulseEffect goldPulseEffect;

        private void Awake()
        {
            if (goldPulseEffect == null)
                goldPulseEffect = GetComponent<ButtonGoldPulseEffect>();
        }

        /// <summary>
        /// Bắt đầu nhấp nháy với cấu hình đặc biệt cho Tutorial (Màu đen, Alpha 1).
        /// </summary>
        public void StartPulse()
        {
            if (goldPulseEffect != null)
            {
                // Ghi đè thông số: Màu đen, Alpha tối đa 1.0
                goldPulseEffect.OverridePulseSettings(Color.black, 1f);
                goldPulseEffect.StartPulse();
                Debug.Log("[TutorialCoinButtonPulse] Started Pulse with Tutorial Overrides (Black, Alpha 1)");
            }
            else
            {
                Debug.LogWarning("[TutorialCoinButtonPulse] goldPulseEffect is missing!");
            }
        }

        /// <summary>
        /// Dừng nhấp nháy.
        /// </summary>
        public void StopPulse()
        {
            if (goldPulseEffect != null)
            {
                goldPulseEffect.StopPulse();
            }
        }
    }
}
