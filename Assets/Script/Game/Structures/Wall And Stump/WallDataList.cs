using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Game.Structures.Wall_And_Stump
{
    public class WallDataList : MonoBehaviour
    {
        public static WallDataList Instance { get; private set; }
        
        [System.Serializable]
        public class WallData
        {
            public int tier;
            public int upgradeCost;
            public int rebuildCost;
            public int normalMaxHp;
            public int blessedMaxHp;
            public GameObject curentPrefab;   
            public GameObject stumpPrefab;
            public GameObject nextUpgradePrefab;
            public bool maxLevel;
        }
        public List<WallData> wallList =new List<WallData>(); 
        
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
               
            }
            else
            {
                Destroy(gameObject);
            }
            //InitializeWallData();
        }

       
        public void InitializeWallData()
        {
           
            wallList.Add(new WallData
            {
                tier = 0,
                upgradeCost = 1,
                curentPrefab =  PrefabData.Instance.wallDirtMound0,
                stumpPrefab=null,
                nextUpgradePrefab=PrefabData.Instance.wallSpike1
                    
            });
            wallList.Add(new WallData
            {
                tier = 1,
                upgradeCost = 3,
                rebuildCost  = 1,
                normalMaxHp = 25,
                blessedMaxHp = 50,
                curentPrefab = PrefabData.Instance.wallSpike1,
                stumpPrefab=PrefabData.Instance.stumpSpike1,
                nextUpgradePrefab=PrefabData.Instance.wallPalisade2
            });
            wallList.Add(new WallData
            {
                tier = 2,
                upgradeCost = 5,
                rebuildCost = 1,
                normalMaxHp = 50,
                blessedMaxHp = 90,
                curentPrefab = PrefabData.Instance.wallPalisade2,
                stumpPrefab=PrefabData.Instance.stumpPalisade2,
                nextUpgradePrefab=PrefabData.Instance.wallStone3
            });
            wallList.Add(new WallData
            {
                tier = 3,
                upgradeCost = 8,
                rebuildCost = 3,
                normalMaxHp = 200,
                blessedMaxHp = 360,
                curentPrefab = PrefabData.Instance.wallStone3,
                stumpPrefab=PrefabData.Instance.stumpStone3,
                nextUpgradePrefab=PrefabData.Instance.wallTallStone4
            });
            wallList.Add(new WallData
            {
                tier = 4,
                upgradeCost = 12,
                rebuildCost = 5,
                normalMaxHp = 300,
                blessedMaxHp = 500,
                curentPrefab = PrefabData.Instance.wallTallStone4,
                stumpPrefab=PrefabData.Instance.stumpTallStone4,
                nextUpgradePrefab=PrefabData.Instance.wallIron5
            });
            wallList.Add(new WallData
            {
                tier = 5,
                upgradeCost = 0,
                rebuildCost = 6,
                normalMaxHp = 400,
                blessedMaxHp = 800,
                curentPrefab = PrefabData.Instance.wallIron5,
                stumpPrefab=PrefabData.Instance.stumpIron5,
                nextUpgradePrefab=null,
                maxLevel = true//đến đây là max cấp
            });
            //Debug.Log("Wall data initialized with count: " + wallList.Count);
        }
        public WallData GetWallDataByTier(int tier)
        {
            foreach (WallData wallData in wallList)
            {
                if (wallData.tier == tier)
                {
                    return wallData;
                }
            }
    
            Debug.LogWarning("WallData with tier " + tier + " not found.");
            return null;
        }

    }
}