using Script.Data_Model;
using Script.Interfaces;
using Script.Utils;
using TMPro;
using UnityEngine;

namespace Script.UI
{
    /// <summary>
    /// World-space HP text dùng chung: cần parent có <see cref="IHealth"/>; cập nhật / flash khi nhận đòn nếu có <see cref="IHealthReactive"/>.
    /// TMP không gán trực tiếp: dùng <see cref="hpLabelHost"/>; Awake tìm TMP trên host / con, hoặc spawn từ <see cref="PrefabData.hpLabelPrefab"/> khi thiếu.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HPText : MonoBehaviour
    {
        [Header("UI — object chứa TMP")]
        [Tooltip("Để trống = dùng chính GameObject gắn HPText. TMP có thể nằm trên object này hoặc con bên trong.")]
        [SerializeField] private GameObject hpLabelHost;

        [Tooltip("Local position sau spawn. (0,0,0) = dùng PrefabData.defaultHpTextOffset (mặc định Y=8).")]
        [SerializeField] private Vector3 spawnLocalOffset = Vector3.zero;

        [Header("Settings")]
        [SerializeField] private float defaultDisplayDuration = 3f;
        [SerializeField] private float triggerDisplayDuration = 2f;
        [SerializeField] private float fadeSpeed = 2f;
        [SerializeField] private bool useFade = true;

        private TextMeshProUGUI hpText;
        private GameObject spawnedLabelInstance;
        private IHealth health;
        private IHealthReactive healthReactive;
        private CanvasGroup canvasGroup;

        private float _showTimeRemaining;
        private bool _fading;
        private float _fadeProgress;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            ResolveHpText();

            health = GetComponentInParent<IHealth>();

            if (health == null)
            {
                Debug.LogError($"[HPText] Không tìm thấy IHealth trên parent của {gameObject.name}");
                enabled = false;
                return;
            }

            healthReactive = health as IHealthReactive;

            canvasGroup.alpha = 0f;
            UpdateHPText(health.CurrentHp, health.MaxHp);
        }

        private void OnDestroy()
        {
            if (spawnedLabelInstance != null)
                Destroy(spawnedLabelInstance);
        }

        /// <summary>
        /// TMP trên host → TMP trong con → spawn prefab (nếu có) rồi lấy TMP trong prefab.
        /// </summary>
        private void ResolveHpText()
        {
            Transform host = hpLabelHost != null ? hpLabelHost.transform : transform;

            hpText = host.GetComponent<TextMeshProUGUI>();
            if (hpText == null)
                hpText = host.GetComponentInChildren<TextMeshProUGUI>(true);

            var labelPrefab = PrefabData.Instance != null ? PrefabData.Instance.hpLabelPrefab : null;
            if (hpText == null && labelPrefab != null)
            {
                spawnedLabelInstance = Instantiate(labelPrefab, host, false);
                Vector3 offset = spawnLocalOffset;
                if (offset == Vector3.zero && PrefabData.Instance != null)
                    offset = PrefabData.Instance.defaultHpTextOffset;
                spawnedLabelInstance.transform.localPosition = offset;
                spawnedLabelInstance.transform.localRotation = Quaternion.identity;
                spawnedLabelInstance.transform.localScale = Vector3.one;
                hpText = spawnedLabelInstance.GetComponent<TextMeshProUGUI>()
                         ?? spawnedLabelInstance.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (hpText == null)
                Debug.LogWarning($"[HPText] Không tìm thấy TextMeshProUGUI trên '{host.name}' và PrefabData.hpLabelPrefab trống hoặc Instance null. Gán TMP trong hierarchy hoặc prefab trong PrefabData.", this);
        }

        private void OnEnable()
        {
            if (healthReactive == null) return;
            healthReactive.OnHealthChanged += OnHealthChangedHandler;
            healthReactive.OnTakeDamageEvent += OnTakeDamageHandler;
        }

        private void OnDisable()
        {
            if (healthReactive == null) return;
            healthReactive.OnHealthChanged -= OnHealthChangedHandler;
            healthReactive.OnTakeDamageEvent -= OnTakeDamageHandler;
        }

        private void OnHealthChangedHandler(float current, float max)
        {
            UpdateHPText(current, max);
            if (current <= 0.0001f)
                HideImmediate();
        }

        private void OnTakeDamageHandler(float _)
        {
            ShowHealth(defaultDisplayDuration);
        }

        /// <summary>
        /// Gọi từ <see cref="Script.Game.Player.PlayerInteraction"/> khi player overlap collider của structure
        /// (cùng luồng với CoinUser). Dùng khi structure không có collider trigger trên object này (ví dụ tường solid).
        /// </summary>
        public void OnPlayerProximityEnter()
        {
            ShowHealth(triggerDisplayDuration);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag(GameTags.Player))
                ShowHealth(triggerDisplayDuration);
        }

        private void Update()
        {
            if (canvasGroup == null) return;
            if (_showTimeRemaining <= 0f && !_fading) return;

            if (_showTimeRemaining > 0f)
            {
                _showTimeRemaining -= Time.deltaTime;
                if (_showTimeRemaining <= 0f)
                {
                    if (useFade)
                    {
                        _fading = true;
                        _fadeProgress = 0f;
                    }
                    else
                    {
                        canvasGroup.alpha = 0f;
                    }
                }
            }

            if (_fading)
            {
                _fadeProgress += Time.deltaTime * fadeSpeed;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(_fadeProgress));
                if (_fadeProgress >= 1f)
                {
                    _fading = false;
                    canvasGroup.alpha = 0f;
                }
            }
        }

        public void ShowHealth(float duration)
        {
            if (health == null || canvasGroup == null) return;

            // Luôn đọc lại IHealth: wall rebuild/stump có thể gán field mà không bắn OnHealthChanged,
            // và khi text vẫn đang hiện (alpha > 0) không được bỏ qua refresh.
            UpdateHPText(health.CurrentHp, health.MaxHp);

            _fadeProgress = 0f;
            canvasGroup.alpha = 1f;
            _showTimeRemaining = duration;
            if (_showTimeRemaining <= 0f)
            {
                if (useFade)
                    _fading = true;
                else
                {
                    _fading = false;
                    canvasGroup.alpha = 0f;
                }
            }
            else
                _fading = false;
        }

        public void HideImmediate()
        {
            _showTimeRemaining = 0f;
            _fading = false;
            _fadeProgress = 0f;
            canvasGroup.alpha = 0f;
        }

        private void UpdateHPText(float current, float max)
        {
            if (hpText != null)
                hpText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)} HP";
        }
    }
}
