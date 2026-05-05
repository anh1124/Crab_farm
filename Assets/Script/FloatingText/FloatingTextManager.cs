using System.Collections.Generic;
using UnityEngine;

namespace Script.FloatingText
{
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        [SerializeField] private FloatingTextPool       _pool;
        [SerializeField] private FloatingTextRegistrySO _registry;

        private Script.Data_Model.PrefabData _prefabData;

        // Property để truy cập an toàn, tự động load fallback nếu cần
        public FloatingTextRegistrySO Registry
        {
            get
            {
                if (_registry == null)
                {
                    _registry = Resources.Load<FloatingTextRegistrySO>("FloatingText/DefaultRegistry");
                }
                return _registry;
            }
            set => _registry = value;
        }

        public FloatingTextStyleSO GetStyle(FloatingTextType type) 
        {
            var reg = Registry;
            return reg != null ? reg.Get(type) : null;
        }

        private readonly List<FloatingText> _active = new List<FloatingText>(20);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Setup(Script.Data_Model.PrefabData prefabData)
        {
            _prefabData = prefabData;
            if (_pool != null) _pool.Setup(prefabData);
        }

        public void Init()
        {
            // Tự động tìm pool nếu chưa gán
            if (_pool == null) _pool = GetComponentInChildren<FloatingTextPool>(true);
            
            // Tự tạo pool nếu vẫn không thấy (giống ArrowPool)
            if (_pool == null)
            {
                GameObject poolObj = new GameObject("FloatingTextPool_AutoCreated");
                poolObj.transform.SetParent(transform);
                _pool = poolObj.AddComponent<FloatingTextPool>();
            }

            _pool.Setup(_prefabData);
            _pool.Init();
            Debug.Log("✅ FloatingTextManager initialized via Init()");
        }

        private void Update()
        {
            if (_pool == null) return;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].ManualUpdate(Time.deltaTime);

                if (_active[i].IsFinished)
                {
                    var ft = _active[i];
                    _active.RemoveAt(i);
                    _pool.Release(ft);
                }
            }
        }

        public void Show(FloatingTextPayload payload)
        {
            var reg = Registry;
            if (reg == null)
            {
                Debug.LogWarning("[FloatingTextManager] Registry chưa được gán và không tìm thấy bản fallback trong Resources.");
                return;
            }

            var style = reg.Get(payload.Type);
            if (style == null)
            {
                Debug.LogWarning($"[FloatingTextManager] Không tìm thấy style cho {payload.Type} trong Registry.");
                return;
            }

            if (_pool == null)
            {
                Debug.LogError("[FloatingTextManager] Pool chưa được gán!");
                return;
            }

            var ft = _pool.Get();
            if (ft == null) 
            {
                Debug.LogWarning("[FloatingTextManager] Pool cạn hoặc chưa được khởi tạo!");
                return;
            }

            // Debug.Log($"[FloatingTextManager] Spawning text: {payload.Value} at {payload.WorldPosition}");
            ft.Initialize(payload, style);
            _active.Add(ft);
        }
    }
}
