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
        public float maxHp = 500f;
        public float currentHp = 500f;

    }
}


