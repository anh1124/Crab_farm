using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Game.Structures.Wall_And_Stump
{
    [CreateAssetMenu(fileName = "WallTierData", menuName = "CrabFarm/Structure/Wall Data", order = 1)]
    public class WallTierData : ScriptableObject
    {
        [Header("Basic Info")]
        [Min(0)] public int tier;
        
        [Tooltip("Prefab mẫu chứa sprite + collider cho wall tier này")]
        public GameObject prefabWall;
        [Tooltip("Prefab mẫu chứa sprite + collider cho stump tier này")]
        public GameObject prefabStump;
        
        [Header("Stats")]
        [Min(1)] public int normalMaxHp;
        [Min(1)] public int blessedMaxHp;
        
        [Min(1)] public int reBuildCost;//giá xây lại
        [Min(1)] public int upgradeCost;//giá để nâng lên cấp kế tiếp
        
        [Min(1)] public int upgradeProgressMax;//công sức phải bỏ ra để xây (behavior sẽ dùng wallDataList.GetNext(data);)
        
        //sau này thêm audio như hit ,upgrade,...
        //[Header("Feedback (optional)")]
        //public AudioClip buildSfx;
        //public GameObject buildVfxPrefab;
        
        [TextArea] public string description;           
        
    }
}