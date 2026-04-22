using Script.GUI_Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.Tutorial
{
    /// <summary>
    /// Hiển thị nội dung hint. Active/Inactive do HintManager/GUI Canvas Manager điều khiển.
    /// </summary>
    public class HintPanelUI : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private ScrollRect _contentScroll;

        [Header("Controls")]
        [SerializeField] private Toggle _ignoreToggle;
        [SerializeField] private Button _closeButton;

        /// <summary>
        /// Hiển thị dữ liệu hint và gán callbacks.
        /// </summary>
        public void Show(
            HintDataSO data,
            bool isIgnored,
            System.Action<bool> onIgnoreChanged,
            System.Action onClose
        )
        {
            if (data == null) return;

            if (_titleText != null) _titleText.text = data.title;
            if (_descriptionText != null) _descriptionText.text = data.description;

            if (_contentScroll != null)
                _contentScroll.verticalNormalizedPosition = 1f;

            if (_ignoreToggle != null)
            {
                _ignoreToggle.onValueChanged.RemoveAllListeners();
                _ignoreToggle.isOn = isIgnored;
                _ignoreToggle.onValueChanged.AddListener(v => onIgnoreChanged?.Invoke(v));
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() =>
                {
                    // Gọi HandleEscapeKey để đóng hint panel và resume game đúng cách
                    if (GuiCanvasManager.Instance != null)
                        GuiCanvasManager.Instance.CloseHintAndResume();
                    else
                        onClose?.Invoke();
                });
            }
        }
    }
}

