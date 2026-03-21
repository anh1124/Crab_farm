using UnityEngine;
using System.Collections;
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

        [ContextMenu("Trigger AutoSave")]
        private void TriggerAutoSave()
        {
            if (WorldFileManager.Instance == null) return;
            StartCoroutine(DelayedSave());
        }

        private IEnumerator DelayedSave()
        {
            // Đợi 1 frame (Wait For End Of Frame) để tất cả các event khác hoặc 
            // hierarchy update (như destroy, instantiate con) được hoàn tất.
            yield return new WaitForEndOfFrame();

            Debug.Log($"<color=green>[AutoSave] Đang tự động lưu tại {GameTimeManager.Instance.GameHour}h00 (đã delay 1 frame)...</color>");

            // Kiểm tra số lượng object trước khi lưu
            if (EntitySaveManager.Instance != null)
            {
                if (EntitySaveManager.Instance.playerContainer != null) 
                    Debug.Log($"[AutoSave Debug] Player count trước khi lưu: {EntitySaveManager.Instance.playerContainer.transform.childCount}");
                if (EntitySaveManager.Instance.structureContainer != null) 
                    Debug.Log($"[AutoSave Debug] Structure count trước khi lưu: {EntitySaveManager.Instance.structureContainer.transform.childCount}");
            }

            float originalTimeScale = Time.timeScale;
            // Time.timeScale = 0; // Đóng băng game

            WorldFileManager.Instance.SaveCurrentMap();
            
            // Time.timeScale = originalTimeScale; // Mở lại game
            Debug.Log($"[AutoSave] Đã gọi xong WorldFileManager.Instance.SaveCurrentMap().");
        }
    }
}
