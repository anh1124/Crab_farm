using System.Collections;
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
        private Coroutine hideCoroutine;
        private Collider2D triggerCollider;

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

            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider == null)
                triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;

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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(GameTags.Player))
                ShowHealth(triggerDisplayDuration);
        }

        public void ShowHealth(float duration)
        {
            if (health == null || canvasGroup == null) return;

            if (canvasGroup.alpha <= 0.01f)
                UpdateHPText(health.CurrentHp, health.MaxHp);

            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);

            canvasGroup.alpha = 1f;
            hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }

        public void HideImmediate()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            canvasGroup.alpha = 0f;
        }

        private void UpdateHPText(float current, float max)
        {
            if (hpText != null)
                hpText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)} HP";
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (useFade)
            {
                float elapsed = 0f;
                float startAlpha = canvasGroup.alpha;
                while (elapsed < 1f)
                {
                    elapsed += Time.deltaTime * fadeSpeed;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed);
                    yield return null;
                }
            }

            canvasGroup.alpha = 0f;
            hideCoroutine = null;
        }
    }
}
