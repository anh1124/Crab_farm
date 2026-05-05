using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.FloatingText
{
    [CreateAssetMenu(menuName = "FloatingText/Registry")]
    public class FloatingTextRegistrySO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public FloatingTextType    type;
            public FloatingTextStyleSO style;
        }

        [SerializeField] private List<Entry>         _entries;
        [SerializeField] private FloatingTextStyleSO _defaultStyle; // Fallback khi không tìm thấy Type

        private Dictionary<FloatingTextType, FloatingTextStyleSO> _lookup;

        private void OnEnable()
        {
            _lookup = new Dictionary<FloatingTextType, FloatingTextStyleSO>();
            if (_entries == null) return;
            foreach (var e in _entries)
                _lookup[e.type] = e.style;
        }

        public FloatingTextStyleSO Get(FloatingTextType type)
        {
            if (_lookup != null && _lookup.TryGetValue(type, out var s))
                return s;

            return _defaultStyle;
        }
    }
}
