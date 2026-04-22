using System.Collections.Generic;
using Script.Game.Player;
using Script.Interfaces;
using UnityEngine;

namespace Script.Tutorial
{
    /// <summary>
    /// Khi player vào trigger, yêu cầu HintManager spawn buttons.
    /// Khi player rời, yêu cầu HintManager clear buttons.
    /// 
    /// Collider2D Require
    /// </summary>
    public class HintTrigger : MonoBehaviour, IPlayerInteractable
    {
        [Header("Hint Data (gán trực tiếp)")]
        [SerializeField] private List<HintDataSO> _hintDataList;

        private bool _hasSpawned = false;

        public void OnPlayerEnter(PlayerBehavior player)
        {
            if (_hasSpawned) return;
            if (_hintDataList == null || _hintDataList.Count == 0) return;
            if (!CanInteract()) return;

            if (HintManager.Instance != null)
            {
                HintManager.Instance.SpawnHintButtons(_hintDataList);
                _hasSpawned = true;
            }
        }

        public void OnPlayerExit(PlayerBehavior player)
        {
            if (HintManager.Instance != null)
            {
                HintManager.Instance.ClearHintButtons();
            }
            _hasSpawned = false;
        }

        public bool CanInteract()
        {
            if (_hintDataList == null || _hintDataList.Count == 0) return false;

            // Có ít nhất 1 hint chưa bị ignore
            foreach (var data in _hintDataList)
            {
                if (data == null) continue;
                if (!IsIgnored(data.title)) return true;
            }
            return false;
        }

        private bool IsIgnored(string title)
        {
            return HintSaveManager.Instance != null && 
                   HintSaveManager.Instance.IsIgnored(title);
        }
    }

    [System.Obsolete("Renamed to HintTrigger. Kept for backward-compatible Unity references.")]
    public class TutorialHintInteractable : HintTrigger
    {
    }
}
