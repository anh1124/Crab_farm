using Script.Data_Model.Interfaces;
using Script.Data_Model;
using UnityEngine;
using Newtonsoft.Json;

namespace Script.Game.Other.Nature
{   
    /// <summary>
    /// Main component for tree. Logic and data are handled by NatureTreeBehavior.
    /// </summary>
    public class NatureTreeStatus : MonoBehaviour, ISaveable
    {
        [Header("Tree Components")]
        [SerializeField] private NatureTreeBehavior behavior;
        
        public int type;
        
        private void Awake()
        {
            // Get or add required components
            if (behavior == null)
                behavior = GetComponent<NatureTreeBehavior>();
            if (behavior == null)
                behavior = gameObject.AddComponent<NatureTreeBehavior>();
        }

        private void Start()
        {
            // Auto-detect type if it's default (0), fixing headerless trees form chunks
            if (type == 0)
            {
                AutoDetectTypeFromName();
            }
        }

        private void AutoDetectTypeFromName()
        {
            if (PrefabData.Instance != null && PrefabData.Instance.treePrefabsList != null)
            {
                string cleanName = gameObject.name.Replace("(Clone)", "").Trim();
                for (int i = 0; i < PrefabData.Instance.treePrefabsList.Count; i++)
                {
                    var prefab = PrefabData.Instance.treePrefabsList[i];
                    if (prefab != null && prefab.name == cleanName)
                    {
                        type = i;
                        break;
                    }
                }
            }
        }
        
        // Public getters for external access
        public NatureTreeBehavior Behavior => behavior;
        
        // Convenience methods that delegate to behavior
        public void SetWorkProgress(int currentWorkProgress) => behavior?.SetWorkProgress(currentWorkProgress);
        public void TakeDame(int dame) => behavior?.TakeDame(dame);
        public int CurrentWorkProgress => behavior?.CurrentWorkProgress ?? 0;
        public int MaxWorkProgress => behavior?.MaxWorkProgress ?? 0;
        public bool IsReadyToChop => behavior?.IsReadyToChop ?? false;
        public string TreeType => behavior?.TreeType ?? "Default";
        public int TreeLevel => behavior?.TreeLevel ?? 1;

        public string GetPrefabID()
        {
            // ID phải khớp với key trong PrefabData
            return "tree_" + type; 
        }

        public object CaptureData()
        {
            var position = transform.position;
            return new NatureTreeStatusData
            {
                positionX = position.x,
                positionY = position.y, 
                type = this.type,
                currentWorkProgress = this.CurrentWorkProgress,
                maxWorkProgress = this.MaxWorkProgress,
                isReadyToChop = this.IsReadyToChop
            };
        }

        public void RestoreData(object data)
        {
            NatureTreeStatusData treeData = null;

            // Handle potential type mismatch if loading from different data version
            if (data is NatureTreeStatusData statusData)
            {
                treeData = statusData;
            }
            else
            {
                try 
                {
                    // Fallback: serialize and deserialize to convert properties
                    string json = JsonConvert.SerializeObject(data);
                    treeData = JsonConvert.DeserializeObject<NatureTreeStatusData>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[NatureTreeStatus] Failed to restore data: {e.Message}");
                    return;
                }
            }

            if (treeData != null)
            {
                transform.position = new Vector3(treeData.positionX, treeData.positionY, 0);
                type = treeData.type;
                
                if (behavior != null)
                {
                    behavior.SetWorkProgress(treeData.currentWorkProgress);
                    behavior.maxWorkProgress = treeData.maxWorkProgress;
                    behavior.isReadyToChop = treeData.isReadyToChop;
                }
            }
        }
        
        [System.Serializable]
        public class NatureTreeStatusData
        {
            public float positionX, positionY;
            public int type;
            public int currentWorkProgress;
            public int maxWorkProgress;
            public bool isReadyToChop;
        }
    }
}