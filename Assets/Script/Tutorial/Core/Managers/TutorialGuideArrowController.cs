using System.Collections;
using UnityEngine;
using Script.Manager;

namespace Script.Tutorial
{
    /// <summary>
    /// Controller điều phối arrow trong tutorial.
    /// Sử dụng API của ArrowGuideManager để chỉ dẫn đến các đối tượng dựa trên Tag.
    /// </summary>
    public class TutorialGuideArrowController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Giữ arrow hiển thị vô thời hạn thay vì tự biến mất.")]
        [SerializeField] private bool infiniteLifetime = true;

        private Coroutine _arrowLoopCoroutine;

        private void Awake()
        {
            if (ArrowGuideManager.Instance == null)
            {
                Debug.LogError("[TutorialGuideArrowController] ArrowGuideManager instance not found!");
            }
        }

        /// <summary>
        /// Bắt đầu hiển thị arrow chỉ về đối tượng có Tag tương ứng với vòng lặp 2s.
        /// </summary>
        public void StartGuideArrowToTag(string tag)
        {
            StopAllArrows();
            if (string.IsNullOrEmpty(tag)) return;
            _arrowLoopCoroutine = StartCoroutine(TagArrowLoop(tag));
        }

        private IEnumerator TagArrowLoop(string tag)
        {
            while (true)
            {
                // Reset arrow bằng cách ẩn để vòng sau nó bay từ Player
                ArrowGuideManager.Instance?.HideGuide();
                yield return null; // Chờ 1 frame

                GameObject targetObj = null;
                try 
                { 
                    targetObj = GameObject.FindWithTag(tag); 
                }
                catch (System.Exception)
                {
                    Debug.LogError($"[TutorialGuideArrowController] Tag '{tag}' không tồn tại trong Tag Manager!");
                    yield break;
                }

                if (targetObj != null)
                {
                    ArrowGuideManager.Instance?.SetTarget(targetObj.transform);
                    ApplyLifetimePolicy();
                }
                else
                {
                    // Nếu không thấy target, có thể ẩn guide tạm thời hoặc chờ loop sau
                    // ArrowGuideManager.Instance?.HideGuide();
                }

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

        private void ApplyLifetimePolicy()
        {
            ArrowGuideManager.Instance?.SetInfiniteLifetime(infiniteLifetime);
        }
    }
}
