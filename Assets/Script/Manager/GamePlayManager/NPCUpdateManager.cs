using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model.Interfaces;

namespace Script.Manager.GamePlayManager
{
    public class NPCUpdateManager : MonoBehaviour
    {
        public static NPCUpdateManager Instance { get; private set; }

        [Header("Tick Settings")]
        [SerializeField] private float tickInterval = 0.2f; // 5Hz mặc định

        private readonly List<INPCBehavior> _npcs = new List<INPCBehavior>();
        private float _timer = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < tickInterval) return;
            _timer = 0f;

            // Duyệt ngược để an toàn khi xóa phần tử null
            for (int i = _npcs.Count - 1; i >= 0; i--)
            {
                // Cần ép kiểu sang UnityEngine.Object vì toán tử == null của C# không hoạt động với Interface Unity bị Destroy
                var npcObj = _npcs[i] as UnityEngine.Object;
                if (npcObj == null || _npcs[i].Equals(null)) 
                { 
                    _npcs.RemoveAt(i); 
                    continue; 
                }
                
                _npcs[i].UpdateBehavior();
            }
        }

        public void Register(INPCBehavior npc)
        {
            if (npc == null || _npcs.Contains(npc)) return;
            _npcs.Add(npc);
        }

        public void Unregister(INPCBehavior npc)
        {
            _npcs.Remove(npc);
        }
    }
}
