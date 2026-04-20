using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Script.Controller.GUI_Controller.PlayerGui
{
    /// <summary>
    /// Component chuyên biệt để tạo hiệu ứng nhấp nháy màu vàng (gold pulse) cho một UI Button.
    /// Tác động vào ColorBlock.normalColor để tránh bị ghi đè bởi hệ thống UI state của Unity.
    /// </summary>
    public class ButtonGoldPulseEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float pulseDuration = 0.5f; // 0.5 giây cho nửa chu kỳ
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f, 0f);
        [Range(0f, 1f)] [SerializeField] private float maxAlpha = 0.85f;

        private Button _targetButton;
        private Image _maskImage;
        private Coroutine _pulseCoroutine;

        /// <summary>
        /// Khởi tạo Component với button mục tiêu và chuẩn bị lớp Mask.
        /// </summary>
        public void Setup(Button button)
        {
            _targetButton = button;
            if (_targetButton != null)
            {
                // Tìm hoặc tạo một Image con để làm mặt nạ
                Transform maskTransform = _targetButton.transform.Find("GoldPulseMask");
                if (maskTransform == null)
                {
                    GameObject maskGo = new GameObject("GoldPulseMask");
                    maskGo.transform.SetParent(_targetButton.transform, false);
                    
                    // Cấp hình RectTransform để phủ kín button
                    RectTransform rt = maskGo.AddComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;

                    _maskImage = maskGo.AddComponent<Image>();
                    
                    // Nếu button có sprite, ta copy sprite đó cho mask để khớp hình dạng
                    Image targetImg = _targetButton.GetComponent<Image>();
                    if (targetImg != null)
                    {
                        _maskImage.sprite = targetImg.sprite;
                        _maskImage.type = targetImg.type;
                    }
                }
                else
                {
                    _maskImage = maskTransform.GetComponent<Image>();
                }

                // Luôn khởi đầu với alpha = 0
                if (_maskImage != null)
                {
                    _maskImage.color = new Color(goldColor.r, goldColor.g, goldColor.b, 0f);
                    _maskImage.raycastTarget = false; // Tránh chặn click của button
                    _maskImage.gameObject.SetActive(false);
                }

                Debug.Log($"[ButtonGoldPulseEffect] Setup with overlay mask on: {_targetButton.gameObject.name}");
            }
        }

        public void StartPulse()
        {
            if (_pulseCoroutine != null) return;
            if (_maskImage == null) return;

            _maskImage.gameObject.SetActive(true);
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        public void StopPulse()
        {
            if (_pulseCoroutine == null) return;

            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;

            if (_maskImage != null)
            {
                Color c = _maskImage.color;
                c.a = 0f;
                _maskImage.color = c;
                _maskImage.gameObject.SetActive(false);
            }
        }

        private IEnumerator PulseRoutine()
        {
            float timer = 0f;

            while (true)
            {
                timer += Time.deltaTime;
                float t = Mathf.PingPong(timer / pulseDuration, 1f); 
                
                // Alpha chạy từ 0 đến maxAlpha (0.6)
                float alpha = Mathf.Lerp(0f, maxAlpha, t);

                if (_maskImage != null)
                {
                    _maskImage.color = new Color(goldColor.r, goldColor.g, goldColor.b, alpha);
                }
                yield return null;
            }
        }
    }
}

