using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Structures
{
    public  class WallAndStumpList : MonoBehaviour
    {
        [System.Serializable] 
        public class WallData 
        {
            public int tier;
    
            public  string name;
            public int buildCost;
            public int rebuilding;
             
            public bool blessing;
            public int baseHp;
            public int blessedHp;

            public GameObject stump;
            public GameObject nextUpgrade;
        }
        [System.Serializable] 
        public class StumpData 
        {
            public int tier;
        
            public  string name;
        
            public int buildingCost;

            public GameObject wall;

        }
        
        public static WallAndStumpList Instance { get; private set; }

        
        [SerializeField] private List<WallData> wallList;
        [SerializeField] private List<WallData> stumpList;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Optional: keep this object across scenes
                 DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
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
            return null;
        }

        public void SetBlessing(bool blessing)// đảo ngược blessing
        {
            foreach (WallData wallData in wallList)
            {
                if (!blessing == wallData.blessing)
                {
                    wallData.blessing = !blessing;
                }
            }
        }
    }
}
