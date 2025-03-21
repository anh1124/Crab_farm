using Script.Data_Model.Interfaces;

using UnityEngine;

namespace Script.Data_Model.Status
{
    public class TreeStatus : MonoBehaviour ,ISaveable
    {
        public int type;
        public int currentWorkProgress;
        public int maxWorkProgress;
        public bool isReadyToChop;
        public string GetPrefabID()
        {
            // ID phải khớp với key trong PrefabData
            return "tree_" + type; 
        }
        public object CaptureData()
        {
            var position = transform.position;
            return new TreeStatusData
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
            TreeStatusData treeData = (TreeStatusData)data;
            transform.position = new Vector3(treeData.positionX, treeData.positionY, 0);
            type = treeData.type;
            currentWorkProgress = treeData.currentWorkProgress;
            maxWorkProgress = treeData.maxWorkProgress;
            isReadyToChop = treeData.isReadyToChop;
        }
        // Trong lớp TreeStatus
        [System.Serializable]
        public class TreeStatusData
        {
            public float positionX, positionY;
            public int type;
            public int currentWorkProgress;
            public int maxWorkProgress;
            public bool isReadyToChop;
        }
    }
}
