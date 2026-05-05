using System.Collections.Generic;
using UnityEngine;

namespace Script.FloatingText
{
    public class FloatingTextPool : MonoBehaviour
    {
        [SerializeField] private FloatingText _prefab;
        [SerializeField] private int          _poolSize = 20;

        private Queue<FloatingText> _queue;
        private Script.Data_Model.PrefabData _prefabData;

        public void Setup(Script.Data_Model.PrefabData prefabData)
        {
            _prefabData = prefabData;
        }

        public void Init()
        {
            if (_prefabData != null && _prefabData.floatingTextPrefab != null)
            {
                var go = _prefabData.floatingTextPrefab;
                _prefab = go.GetComponent<FloatingText>();
            }
            
            Initialize();
            Debug.Log("✅ FloatingTextPool initialized via Init()");
        }

        private void Initialize()
        {
            if (_prefab == null)
            {
                // Silent return if called from Awake without inspector assignment
                return;
            }

            if (_queue != null) return; // Prevent double init

            _queue = new Queue<FloatingText>(_poolSize);
            for (int i = 0; i < _poolSize; i++)
            {
                var ft = Instantiate(_prefab, transform);
                ft.gameObject.SetActive(false);
                _queue.Enqueue(ft);
            }
        }

        private void Awake()
        {
            // Inspector support
            if (_prefab != null)
            {
                Initialize();
            }
        }

        /// <summary>Trả về null nếu pool cạn — caller tự bỏ qua.</summary>
        public FloatingText Get()
        {
            if (_queue == null || _queue.Count == 0) return null;
            var ft = _queue.Dequeue();
            ft.gameObject.SetActive(true);
            return ft;
        }

        public void Release(FloatingText ft)
        {
            if (ft == null) return;
            ft.gameObject.SetActive(false);
            _queue.Enqueue(ft);
        }
    }
}
