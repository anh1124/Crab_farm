using UnityEngine;
using Script.Game.PlayerCharacter.Skills;

namespace Script.Controller
{
    /// <summary>
    /// Trung gian nhận input từ cả 2 nguồn: Keyboard và Mobile UI Button.
    ///
    /// Kiến trúc:
    ///   - Movement: lưu state MoveLeft/MoveRight (sustained). PlayerController.FixedUpdate đọc.
    ///   - Coin/Skills từ Keyboard: gọi action method trực tiếp trong Update()
    ///     → tránh vấn đề timing FixedUpdate vs one-frame event.
    ///   - Coin/Skills từ Mobile UI: PlayerGui gọi trực tiếp PlayerController (không qua đây).
    ///   - AnyKeyUsed: flag runtime để SkillButtonUI biết khi nào hiện key hint.
    ///
    /// Setup:
    ///   1. Tạo GameObject "InputManager" trong scene, gắn script này.
    ///   2. Kéo asset KeyMapConfig vào field keyMap (hoặc để trống, sẽ tự Resources.Load).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Key Binding Config")]
        [Tooltip("Kéo KeyMapConfig asset vào đây. Nếu để trống sẽ tự load từ Resources/Config/KeyMapConfig")]
        public KeyMapConfig keyMap;

        /// <summary>Expose KeyMap để SkillButtonUI đọc tên phím cho key hint.</summary>
        public KeyMapConfig KeyMap => keyMap;

        // ── Movement State (sustained — read by PlayerController.FixedUpdate) ───
        /// <summary>Tổng hợp từ keyboard VÀ mobile. True khi đang giữ Left.</summary>
        public bool MoveLeft  { get; private set; }
        /// <summary>Tổng hợp từ keyboard VÀ mobile. True khi đang giữ Right.</summary>
        public bool MoveRight { get; private set; }

        // Internal: tách riêng để OR đúng cách
        private bool _kbMoveLeft;
        private bool _kbMoveRight;
        private bool _mobileMoveLeft;
        private bool _mobileMoveRight;

        /// <summary>
        /// True nếu có phím keyboard nào đó đã được bấm trong session này.
        /// Các script UI như UIKeyHint hay SkillButtonUI sẽ tự đọc cờ này.
        /// </summary>
        public bool AnyKeyUsed { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (keyMap == null)
                keyMap = Resources.Load<KeyMapConfig>("Config/KeyMapConfig");

            if (keyMap == null)
                Debug.LogError("[InputManager] Không tìm thấy KeyMapConfig! Tạo asset tại Assets/Resources/Config/KeyMapConfig");
        }

        // ── Update: Đọc Raw Hardware Input (bỏ qua IME/Telex hoàn toàn) ──────
        //
        // Input.GetKeyDown/GetKeyUp nhận KeyCode trực tiếp từ phần cứng,
        // TRƯỚC khi OS hay bộ gõ Telex/VNI can thiệp → không bao giờ bị lỗi IME.
        // OnGUI + Event.current đã được XÓA vì lý do này.
        //
        private void Update()
        {
            if (keyMap == null) return;

            // ── Movement: Phím giữ (sustained state) ──────────────────────────

            // Keyboard move left
            if (Input.GetKeyDown(keyMap.moveLeft) || Input.GetKeyDown(keyMap.moveLeftAlt))
                _kbMoveLeft = true;
            if (Input.GetKeyUp(keyMap.moveLeft) || Input.GetKeyUp(keyMap.moveLeftAlt))
                _kbMoveLeft = false;

            // Keyboard move right
            if (Input.GetKeyDown(keyMap.moveRight) || Input.GetKeyDown(keyMap.moveRightAlt))
                _kbMoveRight = true;
            if (Input.GetKeyUp(keyMap.moveRight) || Input.GetKeyUp(keyMap.moveRightAlt))
                _kbMoveRight = false;

            // Gamepad / joystick axis
            float h = Input.GetAxisRaw("Horizontal");
            bool axisLeft = h < -0.1f;
            bool axisRight = h > 0.1f;

            // Tổng hợp movement cho FixedUpdate
            MoveLeft  = _mobileMoveLeft  || _kbMoveLeft || axisLeft;
            MoveRight = _mobileMoveRight || _kbMoveRight || axisRight;

            if (_kbMoveLeft || _kbMoveRight)
            {
                SetAnyKeyUsed();
                // Debug.Log($"[InputManager] Movement Updated: KBLeft={_kbMoveLeft}, KBRight={_kbMoveRight}, MobileLeft={_mobileMoveLeft}, MobileRight={_mobileMoveRight}");
            }

            // ── Actions: Phím nhấn một lần (one-shot) ─────────────────────────

            var pc = PlayerController.PlayerController.Instance;

            if (pc != null)
            {
                if (Input.GetKeyDown(keyMap.useCoin)) { Debug.Log("[InputManager] UseCoin Down"); pc.UseCoinDownAction(); SetAnyKeyUsed(); }
                if (Input.GetKeyUp(keyMap.useCoin))   { Debug.Log("[InputManager] UseCoin Up"); pc.UseCoinUpAction();   SetAnyKeyUsed(); }
            }

            // ── Skills ────────────────────────────────────────────────────────

            if (pc?.playerBehavior != null)
            {
                var sm = pc.SkillManager as SkillManager;
                var instigator = pc.playerBehavior.gameObject;
                if (sm != null)
                {
                    if (Input.GetKeyDown(keyMap.skill1)) { Debug.Log("[InputManager] Skill 1 KeyDown"); sm.ExecuteSkill(0, instigator); SetAnyKeyUsed(); }
                    if (Input.GetKeyDown(keyMap.skill2)) { Debug.Log("[InputManager] Skill 2 KeyDown"); sm.ExecuteSkill(1, instigator); SetAnyKeyUsed(); }
                    if (Input.GetKeyDown(keyMap.skill3)) { Debug.Log("[InputManager] Skill 3 KeyDown"); sm.ExecuteSkill(2, instigator); SetAnyKeyUsed(); }
                }
            }

            // ── UI ────────────────────────────────────────────────────────────

            if (!Script.GUI_Controller.GameOverHandler.IsActive)
            {
                if (Input.GetKeyDown(keyMap.openPauseMenu))
                {
                    Script.GUI_Controller.GuiCanvasManager.Instance?.HandleEscapeKey();
                    SetAnyKeyUsed();
                }

                if (Input.GetKeyDown(keyMap.openBuildMenu) ||
                    Input.GetKeyDown(keyMap.accept)        ||
                    Input.GetKeyDown(keyMap.cancel))
                {
                    SetAnyKeyUsed();
                }
            }
        }

        private void SetAnyKeyUsed()
        {
            if (AnyKeyUsed) return;
            AnyKeyUsed = true;
        }

        // ── Mobile Movement Hooks (được gọi từ PlayerGui UI buttons) ──────────

        /// <summary>Gọi khi mobile Left button PointerDown/Up.</summary>
        public void SetMoveLeft(bool value)
        {
            Debug.Log($"[InputManager] Mobile MoveLeft: {value}");
            _mobileMoveLeft = value;
            MoveLeft = _mobileMoveLeft || _kbMoveLeft;
        }

        /// <summary>Gọi khi mobile Right button PointerDown/Up.</summary>
        public void SetMoveRight(bool value)
        {
            Debug.Log($"[InputManager] Mobile MoveRight: {value}");
            _mobileMoveRight = value;
            MoveRight = _mobileMoveRight || _kbMoveRight;
        }
    }
}
