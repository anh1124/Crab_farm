using UnityEngine;
using Script.Manager.WorldManager;
using Script.Save_Controller;

namespace Script.Manager.SaveManager
{
    public class AutoSaveManager : MonoBehaviour
    {
        private void Start()
        {
            if (GameTimeManager.Instance != null)
            {
        
                GameTimeManager.Instance.SubscribeToHourlyEvent(8, TriggerAutoSave);
            }
        }

        private void TriggerAutoSave()
        {
            if (WorldFileManager.Instance == null) return;
            
            Debug.Log($"<color=green>[AutoSave] Đang tự động lưu tại {GameTimeManager.Instance.GameHour}h00...</color>");

            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0; // Đóng băng game

            WorldFileManager.Instance.SaveCurrentMap();
            
            Time.timeScale = originalTimeScale; // Mở lại game
            Debug.Log("[AutoSave] Đã lưu thành công!");
        }
    }
}
