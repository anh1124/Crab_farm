using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection; // Import thư viện I/O để ghi file
using System.Text;
using Script.Game.Structures.TownCenter;
using Script.Game.Structures.Wall_And_Stump;
using UnityEngine.Serialization;

namespace Script.Data_Model
{
    public class PrefabData : MonoBehaviour
    {
     
        public static PrefabData Instance { get; private set; }

        public GameObject player;
        public float playerSpeed = 7;
        public Camera mainCamera;
        
        [Header("Player Character")]
        [Space(5)]
        public List<GameObject> PlayerCharactorList = new List<GameObject>();

        [Header("Wall and Stump ")] [Space(5)]

        #region Wall and stump

        public float WallMinDistance = 10f;
        
        public GameObject wallDirtMound0;//this not a wall
        
        public GameObject wallSpike1;
        public GameObject wallPalisade2;
        public GameObject wallStone3;
        public GameObject wallTallStone4;
        public GameObject wallIron5;
        
        [Space(5)]
        public GameObject stumpSpike1;
        public GameObject stumpPalisade2;
        public GameObject stumpStone3;
        public GameObject stumpTallStone4;
        public GameObject stumpIron5;
        
      
        #endregion
        
        [Space(15)] 
        [Header("Coin ")]
        [Space(5)] 
        #region Coin
        public GameObject coinPrefab;
        
        public float coinMoveSpeed = 0.5f;
        public int fallSpeed = 15;
        public float timeMaxFloatCoin = 3f;
        #endregion
        
        
        [FormerlySerializedAs("townCenter")]
        [Space(15)] 
        [Header("TownCenter ")]
        [Space(5)]  
        #region TownCenter
        public List<GameObject> townCenterList = new List<GameObject>();
        public float plazaDistance = 10f;//khoảng cách quảng trường
        #endregion

        [Space(15)] 
        [Header("Farm ")]
        [Space(5)]  
        #region Farm

        public GameObject farmWaterStream0;
        public GameObject farmWell1;
        public GameObject farmHouse2;
        public GameObject farmPlot;

        #endregion

        [Space(15)] 
        [Header("Tower ")]
        [Space(5)]  
        #region Tower

        public bool towerFireBless;
        
        
        public GameObject towerBoulderPile0;
        public GameObject towerRockPlatForm1;
        public GameObject towerWooden2;
        public GameObject towerStone3;
        public GameObject towerTriplet4;
        public GameObject towerRoofedTriplet5;
        public GameObject towerQuadruplet6;
        
        public GameObject towerBallista7;
        
        #endregion

        [Space(15)] 
        [Header("Upgrade Tech ")]
        [Space(5)]  
        #region UpgradeTech

        public bool techFireArrow;//arrow do fire dame
        public bool techPenetrating;//breaking armor
        public bool techForge;//new struction that give sword to upgrade Squire to Vanguard
        
        public bool techAdhesive;//make wall better
        
        public GameObject upgradeHouse;
        

        #endregion

        [Space(15)] 
        [Header("OtherStruction ")]
        [Space(5)]  
        #region OtherKingdomStruction

        public GameObject siegeWorkshop;
        public GameObject bowVendor;
        public GameObject hammerVendor;
        public GameObject oilBarrelVendor;

        #endregion

        [Space(15)] 
        [Header("Subjects ")]
        [Space(5)]  
        #region Subjects
        
        public GameObject catapult;
        
        #endregion
        
        [Space(15)] 
        [Header("Subjects ")]
        [Space(5)]  
        #region Nature
        public GameObject tree;//đây là tree nguyên bản,nhưng khi lấy tree thì phải dùng hàm get random tree
        
        // Thêm danh sách prefab Tree
        [Header("Tree Prefabs")]
        public List<GameObject> treePrefabs = new();
        
        private Dictionary<string, GameObject> _prefabDict = new();
        public GameObject GetRandomTree()
        {
            if (treePrefabs.Count == 0)
            {
                Debug.LogWarning("Danh sách treePrefabs đang rỗng.");
                return null;
            }
            
            int randomIndex = UnityEngine.Random.Range(0, treePrefabs.Count);
            return treePrefabs[randomIndex];
        }
        public int treeChopCost =1;//số xu cần dể chặt cây
        public int TreeMaxWorkProgress = 5;//số lần cần để chặt cây
        




        [Space(15)] 
        public int maxDistanceFromBush = 5;
        public GameObject rabbitBush;
        public int maxRabbitLimit;
        
        [Space(15)] 
        public GameObject rabbit;
        public int rabbitHp =1;
        public int rabbitSpeed = 1;
        public int rabbitDieReward = 1;
        #endregion

        #region EnemyStruction

        [Space(15)] [Header("EnemyStruction ")] [Space(5)]

        public GameObject summoningPortal;
        
        #endregion

        [Space(15)] [Header("GUI ")] 
        public GameObject gameObjectHaveCanvas;
        public GameObject confirmationPanel;
        
        [Space(5)]  
        [Header("other ")]
        //danh sách những thứ có coinUser và tag để hiện coin slot
        public List<string> playerCoinUserInteractable ; 
      
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
            // Đăng ký các prefab tree
            for(int i = 0; i < treePrefabs.Count; i++)
            {
                _prefabDict.Add($"tree_{i}", treePrefabs[i]);
            }
            
            playerCoinUserInteractable = new List<string> {"Wall","Structure","Tree","TownCenter"}; 
            CallOtherListToGetData();
            
            // Loại bỏ tất cả các phần tử null khỏi danh sách charactor 
            PlayerCharactorList.RemoveAll(item => item == null);
            RequirePrefabCheck();
        }
        public GameObject GetPrefabByID(string id)
        {
            return _prefabDict.TryGetValue(id, out GameObject prefab) ? prefab : null;
        }
        private void RequirePrefabCheck()
        {
            AddCurrentPlayerCharactor();
            FindMainCamera();
        }
        public void FindMainCamera()
        {
            if (mainCamera == null)
            {
                // Tìm camera dựa trên tag "MainCamera"
                mainCamera = Camera.main;

                // Nếu không tìm thấy camera có tag "MainCamera"
                if (mainCamera == null)
                {
                    // Tìm tất cả các Camera trong Scene
                    Camera[] cameras = FindObjectsOfType<Camera>();

                    if (cameras.Length > 0)
                    {
                        mainCamera = cameras[0]; // Gán camera đầu tiên tìm thấy
                        Debug.LogWarning("No camera with 'MainCamera' tag found. Using the first available camera.");
                    }
                    else
                    {
                        Debug.LogError("No camera found in the scene!");
                    }
                }
                else
                {
                    Debug.Log("Main camera found and assigned.");
                }
            }
            else
            {
                Debug.Log("Main camera is already assigned.");
            }
        }

        public void AddCurrentPlayerCharactor()
        {
            if (player == null)
            {
                
                // Kiểm tra nếu danh sách rỗng
                if (PlayerCharactorList != null && PlayerCharactorList.Count > 0)
                {
                    player = PlayerCharactorList[0]; // Lấy phần tử đầu tiên trong danh sách
                }
                else
                {
                    Debug.LogWarning("PlayerCharactorList is empty or null. Cannot assign player.");
                }
            }
        }
        private void CallOtherListToGetData()
        {
            WallDataList.Instance.InitializeWallData();
            TownCenterDataList.Instance.InitializeWallData();
        }
        [System.Serializable]
        public class BuildingData
        {
            public GameObject prefab;
            public float requiredDistance;
        }

        //list này chứ thông số về khoảng cách các gameobejtc với nhau
        public List<BuildingData> buildingDataList = new List<BuildingData>();

        public BuildingData GetBuildingData(GameObject prefab)
        {
            return buildingDataList.Find(data => data.prefab == prefab);
        }

        private void Start()
        {
            if (townCenterList.Count <= 0)
            {
                Debug.LogError("thiếu dữ liệu về towncenter,hãy thêm nó vào prefabdata.towncenterlist ");
            }
        }
   


    }
}
