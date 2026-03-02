/*
using System.Collections;
using UnityEngine;

namespace Script.Utils
{
    public class ScreenShakeController : MonoBehaviour
    {
        public static ScreenShakeController Instance { get; private set; }
        private Vector3 originalPos;
        private Coroutine _currentShake;

        void Awake()
        {
            if (Instance != null) 
            { 
                 Destroy(gameObject); 
                 return; 
            }
            Instance = this;
            originalPos = transform.localPosition;
            
#if UNITY_EDITOR
            if (transform.parent != null)
            {
                Debug.LogWarning("[ScreenShakeController] Camera hiện đang có parent. " +
                                 "Nếu dùng Cinemachine, việc thay đổi localPosition có thể xung đột. " +
                                 "Nên cân nhắc dùng CinemachineImpulse thay thế.");
            }
#endif
        }

        public void Shake(float duration, float intensity)
        {
            if (_currentShake != null)
            {
                StopCoroutine(_currentShake);
            }
            _currentShake = StartCoroutine(DoShake(duration, intensity));
        }

        IEnumerator DoShake(float duration, float intensity)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;
                transform.localPosition = originalPos + new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = originalPos;
            _currentShake = null;
        }
    }
}
*/
