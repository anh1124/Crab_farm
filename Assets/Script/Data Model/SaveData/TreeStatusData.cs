using System;
using Script.Data_Model.Status;
using UnityEngine.Serialization;

namespace Script.Data_Model.SaveData
{
    [Serializable]
    public class TreeStatusData 
    {
        public float positionX;
        public int type;
        public int currentWorkProgress;
        public int maxWorkProgress;
        public bool isReadyToChop;
        
        public TreeStatusData()
        {
        }
        public TreeStatusData(TreeStatus status)
        {
            positionX = status.positionX;
            type = status.type;
            currentWorkProgress = status.currentWorkProgress;
            maxWorkProgress = status.maxWorkProgress;
            isReadyToChop = status.isReadyToChop;
        }
    }
   
  
}
