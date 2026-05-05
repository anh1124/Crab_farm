using Script.Data_Model.Interfaces;
using Script.Data_Model;
using UnityEngine;
using Newtonsoft.Json;

namespace Script.Game.Other.Nature
{   
    [RequireComponent(typeof(NatureTreeBehavior))]
    public class NatureTreeStatus : MonoBehaviour, ISaveable
    {
        public int type;
       
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
        
        // State Properties previously in Behavior
        public int currentWorkProgress;
        public int maxWorkProgress;
        public bool isReadyToChop;
        public bool isSetupCalled = false;

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
                currentWorkProgress = this.currentWorkProgress,
                maxWorkProgress = this.maxWorkProgress,
                isReadyToChop = this.isReadyToChop
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
                
                currentWorkProgress = treeData.currentWorkProgress;
                maxWorkProgress = treeData.maxWorkProgress;
                isReadyToChop = treeData.isReadyToChop;
                isSetupCalled = true;
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