using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model.Interfaces;

namespace Script.Manager.GamePlayManager
{
    [DefaultExecutionOrder(-100)]
    public class NPCUpdateManager : MonoBehaviour
    {
        public static NPCUpdateManager Instance { get; private set; }


        [Header("Tick Settings")]
        [SerializeField] private float tickInterval = 0.05f; 
        [SerializeField] private bool showDebugLog = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Nếu bạn muốn nó tồn tại qua các scene:
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private readonly List<INPCBehavior> _npcs = new List<INPCBehavior>();
        private float _timer = 0f;
        private int _bucketIndex = 0;
        private const int BUCKET_COUNT = 5; // Chia làm 5 nhóm thay vì 10 để nhạy hơn (10ms/bucket)

        private void Update()
        {
            if (_npcs.Count == 0) return;

            _timer += Time.deltaTime;
            
            // Tính toán: Mỗi frame chúng ta sẽ update một phần nhỏ dựa trên tickInterval
            // Đảm bảo sau 1 chu kỳ tickInterval, tất cả NPC đều được update đúng 1 lần.
            float frameThreshold = tickInterval / BUCKET_COUNT;
            
            if (_timer >= frameThreshold)
            {
                _timer = 0f;
                if (showDebugLog) Debug.Log($"[NPCUpdateManager] Updating Bucket {_bucketIndex} | Total NPCs: {_npcs.Count}");
                ProcessCurrentBucket();
                _bucketIndex = (_bucketIndex + 1) % BUCKET_COUNT;
            }
        }

        private void ProcessCurrentBucket()
        {
            int count = _npcs.Count;
            if (count == 0) return;

            int bucketSize = Mathf.CeilToInt((float)count / BUCKET_COUNT);
            int startIndex = _bucketIndex * bucketSize;
            int endIndex = Mathf.Min(startIndex + bucketSize, count);

            if (startIndex >= count) return;

            // Safe iteration: copy to a temporary list to avoid issues if a job unregisters itself during update
            List<INPCBehavior> currentBatch = new List<INPCBehavior>();
            for (int i = startIndex; i < endIndex; i++)
            {
                if (i < _npcs.Count) currentBatch.Add(_npcs[i]);
            }

            foreach (var npc in currentBatch)
            {
                if (npc != null && (npc as UnityEngine.Object) != null)
                {
                    npc.UpdateBehavior();
                }
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
