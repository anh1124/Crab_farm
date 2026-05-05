using TMPro;
using UnityEngine;

namespace Script.FloatingText
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _tmp;   // TMP 3D, KHÔNG phải TextMeshProUGUI

        // Manager đọc các field này trực tiếp — không dùng property để tránh overhead
        [HideInInspector] public float                elapsed;
        [HideInInspector] public FloatingTextStyleSO  style;
        [HideInInspector] public Vector3              spawnPosition;

        public void Initialize(FloatingTextPayload payload, FloatingTextStyleSO s)
        {
            style = s;
            elapsed = 0f;

            // Vị trí cơ sở (đã gồm defaultOffset + customOffset từ CombatUtility)
            Vector3 basePos = payload.WorldPosition;

            // Thêm random ngang từ style
            float randomX = Random.Range(-style.randomHorizontalRange, style.randomHorizontalRange);
            basePos += new Vector3(randomX, 0f, 0f);

            spawnPosition = basePos;

            _tmp.fontSize = s.fontSize;
            _tmp.color = s.color;
            
            string sign = GetSign(payload, s);

            _tmp.text = s.showValue
                ? $"{sign}{Mathf.Abs(Mathf.RoundToInt(payload.Value))}"
                : s.prefix;

            transform.position = spawnPosition;
            transform.localScale = Vector3.one;
        }

        private string GetSign(FloatingTextPayload payload, FloatingTextStyleSO s)
        {
            if (!s.showValue) return "";

            // Chỉ tự động thêm dấu + / - nếu là loại Coin
            if (payload.Type == FloatingTextType.Coin)
            {
                if (payload.Value > 0) return string.IsNullOrEmpty(s.prefix) ? "+ " : s.prefix;
                if (payload.Value < 0) return "- ";
                return ""; // value = 0
            }

            // Các loại khác (Damage, Heal...) dùng prefix mặc định từ Style
            return s.prefix;
        }

        // Gọi từ Manager.Update() — không tự drive
        public void ManualUpdate(float deltaTime)
        {
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / style.duration);

            transform.position = spawnPosition + Vector3.up * (style.moveDistance * style.moveCurve.Evaluate(t));

            Color c = _tmp.color;
            c.a = style.fadeCurve.Evaluate(t);
            _tmp.color = c;

            transform.localScale = Vector3.one * style.scaleCurve.Evaluate(t);
        }

        public bool IsFinished => elapsed >= style.duration;
    }
}
