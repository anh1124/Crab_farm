using Script.Data_Model.Status;
using Script.GUI_Controller;
using UnityEngine;

namespace Script.Game.Player
{
    /// <summary>
    /// Chịu trách nhiệm duy nhất: khởi tạo các UI liên quan đến Player.
    /// Tách khỏi PlayerBehavior để tránh God Object.
    /// 
    /// Gắn Script này lên cùng GameObject với PlayerBehavior.
    /// Sẽ tự động lấy PlayerStatus và tìm GuiCanvasManager để Setup các UI.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class PlayerUISetup : MonoBehaviour
    {
        private PlayerStatus status;

        private void Awake()
        {
            status = GetComponent<PlayerStatus>();
        }

        private void Start()
        {
            var canvasManager = FindObjectOfType<GuiCanvasManager>();
            if (canvasManager == null)
            {
                Debug.LogWarning("[PlayerUISetup] Không tìm thấy GuiCanvasManager trong Scene!");
                return;
            }

            canvasManager.SetupResourceBars(status);
            SetupCoinUI(canvasManager);
        }

        private void SetupCoinUI(GuiCanvasManager canvasManager)
        {
            if (canvasManager.coinUI == null)
            {
                Debug.LogWarning("[PlayerUISetup] CoinUI chưa được gán vào GuiCanvasManager!");
                return;
            }

            canvasManager.coinUI.Setup(status);
        }
    }
}
