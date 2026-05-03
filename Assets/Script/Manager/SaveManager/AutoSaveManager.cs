using UnityEngine;
using System.Collections;
using Script.Manager.WorldManager;
using Script.Save_Controller;

namespace Script.Manager.SaveManager
{
    public class AutoSaveManager : MonoBehaviour
    {
        private IGameTimeProvider _timeProvider;

        public void Setup(IGameTimeProvider timeProvider)
        {
            if (Utility.IsTutorialScene())
            {
                Debug.Log("[AutoSaveManager] Tutorial scene detected. AutoSave disabled.");
                return;
            }
            
            _timeProvider = timeProvider;
            if (_timeProvider != null)
            {
                _timeProvider.SubscribeToHourlyEvent(8, TriggerAutoSave);
            }
        }

        private void Start()
        {
            // Initialized via Setup
        }

        private void OnDestroy()
        {
            if (_timeProvider != null)
            {
                _timeProvider.UnsubscribeFromHourlyEvent(8, TriggerAutoSave);
            }
        }

        [ContextMenu("Trigger AutoSave")]
        private void TriggerAutoSave()
        {
            if (Utility.IsTutorialScene()) return;
            if (WorldFileManager.Instance == null) return;
            StartCoroutine(DelayedSave());
        }

        private IEnumerator DelayedSave()
        {
            // Đợi 1 frame (Wait For End Of Frame) để tất cả các event khác hoặc 
            // hierarchy update (như destroy, instantiate con) được hoàn tất.
            yield return new WaitForEndOfFrame();

            if (_timeProvider != null)
            {
                Debug.Log($"<color=green>[AutoSave] Đang tự động lưu tại {_timeProvider.GameHour}h00 (đã delay 1 frame)...</color>");
            }

            // Kiểm tra số lượng object trước khi lưu
            var entitySaveManager = FindObjectOfType<EntitySaveManager>();
            var worldFileManager = FindObjectOfType<WorldFileManager>();

            if (entitySaveManager != null)
            {
                if (entitySaveManager.playerContainer != null) 
                    Debug.Log($"[AutoSave Debug] Player count trước khi lưu: {entitySaveManager.playerContainer.transform.childCount}");
                if (entitySaveManager.structureContainer != null) 
                    Debug.Log($"[AutoSave Debug] Structure count trước khi lưu: {entitySaveManager.structureContainer.transform.childCount}");
            }

            if (worldFileManager != null)
            {
                worldFileManager.SaveCurrentMap();
            }
            
            Debug.Log($"[AutoSave] Đã gọi xong WorldFileManager.SaveCurrentMap().");
        }
    }
}
