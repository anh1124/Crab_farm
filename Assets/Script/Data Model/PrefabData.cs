using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection; // Import thư viện I/O để ghi file
using System.Text;
using Script.Game.Structures.TownCentrer;
using Script.Game.Structures.Wall_And_Stump;
using UnityEngine.Serialization;

namespace Script.Data_Model
{
    public class PrefabData : MonoBehaviour
    {
     
        public static PrefabData Instance { get; private set; }

        public GameObject player;
        public Camera mainCamera;
        
        [Header("Player Character")]
        [Space(5)]
        public List<GameObject> PlayerCharactorList = new List<GameObject>(); 

        [Header("Wall and Stump ")]
        [Space(5)]
        #region Wall and stump
        //this not a wall
        public GameObject wallDirtMound0;
        
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
        
        
        [Space(15)] 
        [Header("TownCenter ")]
        [Space(5)]  
        #region TownCenter

        public GameObject townCenterUnlitCampfire0;
        public GameObject townCenterCampfire1;
        public GameObject townCenterVillageTents2;
        public GameObject townCenterTownHall3;
        public GameObject townCenterBigTownHall4;
        // public GameObject townCenterCastleKeep5; // Example of adding new variables
        // public GameObject townCenterIronCastle6;

        public float plazaDistance = 10f;//khoảng cách quảng trường
        //cuối cái này sẽ thêm 2 bức tường,bên trái là bow,bên phải là hammer
        
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
        public List<GameObject> treePrefabs = new List<GameObject>();
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
        
        public int treeChopCost =1;
        public int treeHp =5;//số lần cần để chặt cây




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

  
        //danh sách những thứ có coinUser và tag để hiện coin slot
        public List<string> playerCoinUserInteractable ; 
      
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            playerCoinUserInteractable = new List<string> {"Wall","Structure","Tree"}; 
            CallOtherListToGetData();
            
            // Loại bỏ tất cả các phần tử null khỏi danh sách charactor 
            PlayerCharactorList.RemoveAll(item => item == null);
            RequirePrefabCheck();
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
      
      
      
     
    }
}
