using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Script.Tutorial;
using Script.Manager;

namespace Script.Tutorial
{
    /// <summary>
    /// Controller điều phối arrow trong tutorial.
    /// Sử dụng API của ArrowGuideManager để chỉ dẫn đến TownCenter hoặc UI Button.
    /// </summary>
    public class TutorialGuideArrowController : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("TownCenter trong Hierarchy để arrow chỉ về (kéo từ Hierarchy).")]
        [SerializeField] private Transform townCenter;

        [Header("Settings")]
        [Tooltip("Giữ arrow hiển thị vô thời hạn thay vì tự biến mất.")]
        [SerializeField] private bool infiniteLifetime = true;


        private void Awake()
        {
            // Đảm bảo ArrowGuideManager tồn tại
            if (ArrowGuideManager.Instance == null)
            {
                Debug.LogError("[TutorialGuideArrowController] ArrowGuideManager instance not found!");
            }
        }

        /// <summary>
        /// Bắt đầu hiển thị arrow chỉ về TownCenter.
        /// Nếu đang chỉ CoinButton sẽ dừng lại trước.
        private Coroutine _arrowLoopCoroutine;

        /// <summary>
        /// Bắt đầu hiển thị arrow chỉ về TownCenter với vòng lặp 2s.
        /// </summary>
        public void StartArrowToTownCenter()
        {
            StopAllArrows();

            if (townCenter == null)
            {
                Debug.LogError("[TutorialGuideArrowController] TownCenter reference is MISSING in Inspector! Logic is now centralized, please assign it.");
                return;
            }

            _arrowLoopCoroutine = StartCoroutine(TownCenterArrowLoop());
        }

        private IEnumerator TownCenterArrowLoop()
        {
            while (true)
            {
                // Reset arrow bằng cách ẩn và hiện lại
                ArrowGuideManager.Instance?.HideGuide();
                yield return null; // Chờ 1 frame

                ArrowGuideManager.Instance?.SetTarget(townCenter);
                ApplyLifetimePolicy();
                
                Debug.Log("[TutorialGuideArrowController] Arrow loop reset: Target TownCenter");
                yield return new WaitForSeconds(2f);
            }
        }

        /// <summary>
        /// Dừng arrow dựa trên tên condition (dùng để TutorialController gọi khi thỏa mãn điều kiện).
        /// </summary>
        public void StopArrowByCondition(string conditionName)
        {
            StopAllArrows();
        }

        /// <summary>
        /// Dừng tất cả arrow và ẩn guide.
        /// </summary>
        public void StopAllArrows()
        {
            if (_arrowLoopCoroutine != null)
            {
                StopCoroutine(_arrowLoopCoroutine);
                _arrowLoopCoroutine = null;
            }
            ArrowGuideManager.Instance?.HideGuide();
        }

        /// <summary>
        /// Kích hoạt chế độ hiển thị vĩnh viễn (không tự biến mất) cho tutorial.
        /// </summary>
        private void ApplyLifetimePolicy()
        {
            if (infiniteLifetime)
            {
                ArrowGuideManager.Instance?.SetInfiniteLifetime(true);
            }
            else
            {
                ArrowGuideManager.Instance?.SetInfiniteLifetime(false);
            }
        }
    }
}
