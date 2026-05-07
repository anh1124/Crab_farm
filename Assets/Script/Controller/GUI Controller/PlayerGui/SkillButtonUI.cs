using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Script.Game.PlayerCharacter.Skills;
using Script.Game.Player;

namespace Script.Controller.GUI_Controller.PlayerGui
{
    /// <summary>
    /// Điều khiển toàn bộ visual của 1 skill button.
    /// Tự cập nhật theo trạng thái SkillManager trong Update().
    /// Tên field khớp với tên GameObject trong Hierarchy của Prefab.
    /// </summary>
    public class SkillButtonUI : MonoBehaviour
    {
        // ── Inspector References ───────────────────────────────────────────
        [Header("Visual Layers")]
        [SerializeField] private Image skillImage;      // → Skill_Image
        [SerializeField] private Image resourceMask;    // → ResourceMask (phủ Xanh/Vàng khi thiếu MP/SP)
        [SerializeField] private Image bgFrame;         // → BgFrame
        [SerializeField] private TMP_Text costText;     // → CostText
        [SerializeField] private Button button;         // → SkillButton (Root)

        [Header("Cooldown Group")]
        [SerializeField] private GameObject cooldownGroup;       // → CooldownGroup (container)
        [SerializeField] private Image cooldownRadialImage;      // → CooldownGroup/CooldownRadial
        [SerializeField] private TMP_Text cooldownText;          // → CooldownGroup/CooldownText

        [Header("Theme")]
        [SerializeField] private SkillButtonTheme theme;

        [Header("Key Hint (WebGL/PC only)")]
        [Tooltip("GameObject cha chứa cả text và ảnh nền. SetActive theo platform.")]
        [SerializeField] private GameObject keyHintContainer; // Parent object (chứa text + bg image)
        [Tooltip("TMP_Text hiển thị tên phím. Nằm bên trong keyHintContainer.")]
        [SerializeField] private TMP_Text keyHintText;        // Text label "Q", "E", "R"…

        // ── Runtime ────────────────────────────────────────────────────────
        private Script.Game.PlayerCharacter.Skills.SkillManager _skillManager;
        private PlayerBehavior _playerBehavior;
        private int _slotIndex;
        private SkillBase _lastSkillReference; // Lưu vết để nhận biết khi đổi skill

        // ── Init ───────────────────────────────────────────────────────────

        public void Init(Script.Game.PlayerCharacter.Skills.SkillManager manager, PlayerBehavior behavior, int index)
        {
            _skillManager   = manager;
            _playerBehavior = behavior;
            _slotIndex      = index;
 
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
 
            // Cập nhật trạng thái hiển thị ngay lập tức vì nếu object đang Unactive, Update() sẽ không chạy
            var skill = _skillManager?.GetSkill(_slotIndex);
            _lastSkillReference = skill;
            
            if (skill != null)
            {
                gameObject.SetActive(true);
                RefreshStaticUI();
            }
            else
            {
                gameObject.SetActive(false);
            }

            SetupKeyHint();
        }

        // ── Update ─────────────────────────────────────────────────────────

        private void Update()
        {
            if (_skillManager == null) return;

            var skill = _skillManager.GetSkill(_slotIndex);

            // Tự động cập nhật UI tĩnh (Icon/Cost) nếu phát hiện đổi skill
            if (skill != _lastSkillReference)
            {
                _lastSkillReference = skill;
                RefreshStaticUI();
            }

            // Nếu slot trống → ẩn cả nút
            if (skill == null)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            else
            {
                if (!gameObject.activeSelf) gameObject.SetActive(true);
            }

            switch (_skillManager.GetState(_slotIndex))
            {
                case SkillManager.SkillState.Cooling:
                    ApplyState_Cooling();
                    break;
                case SkillManager.SkillState.Executing:
                    ApplyState_Executing();
                    break;
                default:
                    ApplyState_Ready(skill);
                    break;
            }

            // Runtime: hiện key hint khi có keyboard input (bất kể platform)
            UpdateKeyHintVisibility();
        }

        // ── States ─────────────────────────────────────────────────────────

        private void ApplyState_Ready(SkillBase skill)
        {
            bool hasResource = true;
            Color defaultNoMana = new Color(0.2f, 0.4f, 1f, 0.6f);
            Color maskColor = theme != null ? theme.colorNoMana : defaultNoMana;

            if (_playerBehavior != null)
            {
                hasResource = _playerBehavior.CanAfford(skill.resourceType, skill.resourceCost);
                
                // Chọn màu mask dựa trên loại tài nguyên bị thiếu
                switch (skill.resourceType)
                {
                    case ResourceType.Coin:   maskColor = theme != null ? theme.colorNoCoin : new Color(1f, 0.8f, 0f, 0.6f); break;
                    case ResourceType.Health: maskColor = theme != null ? theme.colorNoHealth : new Color(1f, 0.2f, 0.2f, 0.6f); break;
                    default:                  maskColor = theme != null ? theme.colorNoMana : defaultNoMana; break;
                }
            }

            if (cooldownGroup != null) cooldownGroup.SetActive(false);

            if (!hasResource)
            {
                SetResourceMask(true, maskColor);
                if (button != null) button.interactable = false;
            }
            else
            {
                SetResourceMask(false, Color.white);
                if (button != null) button.interactable = true;
            }
        }

        private void ApplyState_Cooling()
        {
            float remaining = _skillManager.GetCooldownRemaining(_slotIndex);
            float total     = _skillManager.GetCooldownTotal(_slotIndex);

            if (button != null) button.interactable = false;
            SetResourceMask(false, Color.white);

            if (cooldownGroup != null)
            {
                cooldownGroup.SetActive(true);

                if (cooldownRadialImage != null)
                    cooldownRadialImage.fillAmount = total > 0f ? remaining / total : 0f;

                if (cooldownText != null)
                    cooldownText.text = remaining >= 1f
                        ? Mathf.Ceil(remaining).ToString()
                        : remaining.ToString("F1");
            }
        }

        private void ApplyState_Executing()
        {
            if (button != null) button.interactable = false;
            if (cooldownGroup != null) cooldownGroup.SetActive(false);
            SetResourceMask(true, theme != null ? theme.colorExecuting : new Color(0.2f, 0.2f, 0.2f, 0.5f));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private void RefreshStaticUI()
        {
            var skill = _skillManager?.GetSkill(_slotIndex);
            if (skill == null) return;

            if (skillImage != null) skillImage.sprite = skill.icon;

            if (costText != null)
            {
                costText.text  = skill.resourceCost.ToString("0");
                
                // Đổi màu chữ cost theo loại tài nguyên
                switch (skill.resourceType)
                {
                    case ResourceType.Mana:   costText.color = theme != null ? theme.textMana : Color.cyan; break;
                    case ResourceType.Coin:   costText.color = theme != null ? theme.textCoin : Color.yellow; break;
                    case ResourceType.Health: costText.color = theme != null ? theme.textHealth : Color.red; break;
                    default:                  costText.color = theme != null ? theme.textDefault : Color.white; break;
                }
            }
        }

        private void SetResourceMask(bool active, Color color)
        {
            if (resourceMask == null) return;
            resourceMask.gameObject.SetActive(active);
            resourceMask.color = color;
        }

        private void OnClick()
        {
            var instigator = _playerBehavior?.gameObject;
            _skillManager?.ExecuteSkill(_slotIndex, instigator);
        }

        // ── Key Hint ───────────────────────────────────────────────────────

        /// <summary>
        /// Ghi label phím vào text (gọi 1 lần trong Init).
        /// Visibility được cập nhật mỗi frame bởi UpdateKeyHintVisibility().
        /// </summary>
        private void SetupKeyHint()
        {
            if (keyHintText == null) return;

            var km = Script.Controller.InputManager.Instance?.KeyMap;
            if (km == null) return;

            KeyCode key = _slotIndex switch
            {
                0 => km.skill1,
                1 => km.skill2,
                2 => km.skill3,
                _ => KeyCode.None
            };
            keyHintText.text = key != KeyCode.None ? KeyCodeToLabel(key) : "";

            // Ẩn container ngay lúc đầu, UpdateKeyHintVisibility() sẽ bật khi cần
            var toggleTarget = keyHintContainer != null ? keyHintContainer : keyHintText.gameObject;
            toggleTarget.SetActive(false);
        }

        /// <summary>
        /// Gọi mỗi frame trong Update().
        /// Hiện key hint khi InputManager phát hiện có keyboard được dùng (runtime, mọi platform).
        /// </summary>
        private void UpdateKeyHintVisibility()
        {
            var toggleTarget = keyHintContainer != null ? keyHintContainer : keyHintText?.gameObject;
            if (toggleTarget == null) return;

            bool shouldShow = Script.Controller.InputManager.Instance?.AnyKeyUsed ?? false;
            if (toggleTarget.activeSelf != shouldShow)
                toggleTarget.SetActive(shouldShow);
        }

        /// <summary>
        /// Convert KeyCode sang label ngắn để hiển thị trên UI.
        /// VD: KeyCode.Q → "Q", KeyCode.Alpha1 → "1"
        /// </summary>
        private string KeyCodeToLabel(KeyCode key)
        {
            string name = key.ToString();
            // Bỏ prefix "Alpha" cho số (Alpha1 → "1")
            if (name.StartsWith("Alpha")) return name.Substring(5);
            return name;
        }
    }
}
