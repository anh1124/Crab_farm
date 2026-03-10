using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Data_Model.Status
{
    [System.Serializable]
    public class TownCenterStatus : MonoBehaviour
    {
        
        public int tier;
        public int upgradeCost;
        public bool readyToUpgrade;
        public int buildProgress;
        public int buildProgressMax;

        [Header("Health")]
        public int maxHp = 500;
        public int currentHp = 500;

    }
}


