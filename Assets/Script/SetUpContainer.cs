using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Data_Model;
using Script.Data_Model.SaveData;
using Script.Data_Model.Status;
using Script.Game.Other.Nature;
using UnityEngine;

namespace Script
{
    /*
     * vậy thì khi save game, 
em tạo hàm find gameobejct theo list tag, rồi chuyển từng cái gameobject cùng loại như tree vào listTrees,animal vào listAnimals
có ổn không anh,
     */
    public class SetUpContainer : MonoBehaviour
    { 
        /*
       * todo: làm tiếp những cái container khác
       */
        public static SetUpContainer Instance { get; private set; } 
        public List<GameObject> treeObjects= new List<GameObject>();
        
        public List<TreeStatusData> treeStatusDatas;

        
        public GameObject playerContainer;
        public GameObject chunkContainer; //gameobject chứa các chunk được instance ra,đã có riêng 1 script để save và load chunk
        public GameObject coinContainer; 
        
        public GameObject structureContainer;//công trình của làng
        
        public GameObject natureContainer; //spawm cây,RabbitBush
        public GameObject animalsContainer;//spawn thỏ
        
        public GameObject enemyContainer;//chứa portal và enemy
        
        public GameObject wanderersContainer;
        public GameObject villagersContainer;
        public GameObject statuesContainer;
       
        private void Awake()
        {
            AddSingleton();
            SetUp();
        }
        private void AddSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void SetUp()
        {
            playerContainer = SetupContainerHelper("PlayerContainer", ref playerContainer);
            structureContainer = SetupContainerHelper("StructureContainer", ref structureContainer);
            coinContainer = SetupContainerHelper("CoinContainer", ref coinContainer);
            natureContainer = SetupContainerHelper("NatureContainer", ref natureContainer);
            chunkContainer = SetupContainerHelper("ChunkContainer", ref chunkContainer);
            enemyContainer = SetupContainerHelper("EnemyContainer", ref enemyContainer);
            animalsContainer = SetupContainerHelper("AnimalsContainer", ref animalsContainer);
            wanderersContainer = SetupContainerHelper("WanderersContainer", ref wanderersContainer);
            villagersContainer = SetupContainerHelper("VillagersContainer", ref villagersContainer);
            statuesContainer = SetupContainerHelper("StatuesContainer", ref statuesContainer);
        }
        private GameObject SetupContainerHelper(string containerName, ref GameObject container)
        {
            if (container == null)
            {
                container = GameObject.FindGameObjectWithTag(containerName);
                if (container == null)
                {
                    container = new GameObject(containerName);
                    container.tag = containerName;
                }
            }
            return container;
        }
        public GameObject InstanceAndMoveObjectToContainer(GameObject prefab, Vector2 position)
        {
            // Kiểm tra prefab không null
            if (prefab == null)
            {
                Debug.LogError("Prefab được truyền vào là null.");
                return null;
            }

            // Tạo instance mới từ prefab
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);

            // Kiểm tra xem instance có khác prefab không
            if (instance == prefab)
            {
                Debug.LogError("Đối tượng được sinh ra là chính prefab gốc, điều này không nên xảy ra.");
            }

            // Chuyển instance vào container
            MoveObjectToContainer(instance);

            // Trả về instance để có thể sử dụng sau này nếu cần
            return instance;
        }
        public List<GameObject> GetObjectsFromContainerList(GameObject container)
        {
            List<GameObject> objectsList = new List<GameObject>();

            if (container != null)
            {
                foreach (Transform child in container.transform)
                {
                    objectsList.Add(child.gameObject);
                }
            }
            else
            {
                Debug.LogWarning("Container không tồn tại hoặc không hợp lệ!");
            }

            return objectsList;
        }
        //chuyển 1 gameobject vào container theo tag của nó
        public void MoveObjectToContainer(GameObject obj)
        {
            switch (obj.tag)
            {
                case "Player":
                    obj.transform.SetParent(playerContainer.transform, true);
                    break;
                
                case "Structure":
                    obj.transform.SetParent(structureContainer.transform, true);
                    break;
                
                case "Coin":
                    obj.transform.SetParent(coinContainer.transform, true);
                    break;
                
                case "Tree":
                case "RabbitBush":
                    obj.transform.SetParent(natureContainer.transform, true);
                    break;
                
                case "Rabbit":
                    obj.transform.SetParent(animalsContainer.transform, true);
                    break;
                
                case "Enemy":
                    obj.transform.SetParent(enemyContainer.transform, true);
                    break;
                
                case "Wanderer":
                    obj.transform.SetParent(wanderersContainer.transform, true);
                    break;
                
                case "Villager":
                    obj.transform.SetParent(villagersContainer.transform, true);
                    break;
                
                case "Statues":
                    obj.transform.SetParent(statuesContainer.transform, true);
                    break;
                
                default:
                    Debug.LogWarning("Object with unknown tag: " + obj.tag);
                    break;
            }
        }
        //chuyển toàn bộ gameobject con của nó vào container theo tag
        public void MoveChildrenToContainers(GameObject parent)
        {
            Transform containerTransform = parent.transform.Find("statuesContainer");
            if (containerTransform != null)
            {
                List<Transform> children = new List<Transform>();
                foreach (Transform child in containerTransform)
                {
                    children.Add(child);
                }

                foreach (Transform child in children)
                {
                    MoveObjectToContainer(child.gameObject);
                }
            }
        }
        
        public void SaveAllContainer()
        {
           
            SaveNatures();
            SaveStructures();
            SavePlayer();
        }
    
        public void LoadAllContainer()
        {
            //player  sẽ được instance và load riêng trong hàm load thế giới
           
            LoadNatures();
            LoadStructures();
        }
    
        public void RemoveAllChildObjects(GameObject parent)
        {
            // Kiểm tra nếu parent không null
            if (parent != null)
            {
                // Duyệt qua từng child của parent
                foreach (Transform child in parent.transform)
                {
                    // Hủy đối tượng con
                    Destroy(child.gameObject);
                }
            }
        }

        #region Player
        public void SavePlayer()
        {
            try
            {
                // Lấy PlayerStatus từ player hiện tại
                PlayerStatus status = PlayerController.Instance.player.GetComponent<PlayerStatus>();
                if (status == null)
                {
                    Debug.LogError("Không tìm thấy PlayerStatus component!");
                    return;
                }

                // Tạo đối tượng lưu trữ dữ liệu
                PlayerStatusData saveData = new PlayerStatusData(status);

                // Chuyển đổi sang JSON
                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                
                // Lưu vào file
                string filePath = Path.Combine(LoadFromSave.Instance.saveFilePath, "player.json");
                File.WriteAllText(filePath, json);

                Debug.Log("Đã lưu dữ liệu player vào " + filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Lỗi khi save player: " + ex.Message);
            }
        }

        public void LoadPlayer()
        {
            if (PlayerController.Instance == null)
            {
                Debug.LogError("PlayerController.Instance is null!");
                return;
            }
            string filePath = Path.Combine(LoadFromSave.Instance.saveFilePath, "player.json");

            // Kiểm tra file tồn tại
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("File player.json không tồn tại!");
                return;
            }

            // Đọc JSON từ file
            string json = File.ReadAllText(filePath);
            PlayerStatusData loadedData = JsonConvert.DeserializeObject<PlayerStatusData>(json);

            // Lấy PlayerStatus component
            PlayerStatus status = PlayerController.Instance.player.GetComponent<PlayerStatus>();
            if (status == null)
            {
                Debug.LogError("Không tìm thấy PlayerStatus component!");
                return;
            }
            
            // Gán dữ liệu đã load vào player
            status.MaxHp = loadedData.maxHp;
            status.CurrentHp = loadedData.currentHp;
            status.Coin = loadedData.coin;
            status.moveSpeed = loadedData.moveSpeed;
            status.transform.position = new Vector2(loadedData.positionX, loadedData.positionY);
            status.AttackDame = loadedData.attackDame;
            status.AttackSpeed = loadedData.attackSpeed;

            Debug.Log("Đã load dữ liệu player từ " + filePath);
            
        
        }
        #endregion
        
        #region Natures
        /*
         *tree ,rabbit,rabitbush
         */
        public void MoveTreesFromChunks()
        {
            if (chunkContainer == null)
            {
                Debug.LogWarning("ChunkContainer is null!");
                return;
            }

            // Chỉ tìm các cây trong chunkContainer
            Transform[] allObjectsInChunks = chunkContainer.GetComponentsInChildren<Transform>(true);
            foreach (Transform obj in allObjectsInChunks)
            {
                if (obj.CompareTag("Tree"))
                {
                    MoveObjectToContainer(obj.gameObject);
                }
            }
        }
        public void SaveNatures()
        {
            MoveTreesFromChunks();
            SaveTrees();
            SaveRabbitBush();
        }
        public void LoadNatures()
        {
            RemoveAllChildObjects(natureContainer);
            
            LoadTrees();
            LoadRabbitBush();
        }
        
        /*
         *chú ý:tree được khởi tạo khi tạo các chunk nên chỉ được load,save trong chunkmanager hoặc hàm save trong game
         */
       
        public void SaveTrees()
        {
            try 
            {
                // Tạo thư mục nếu chưa tồn tại
                Directory.CreateDirectory(LoadFromSave.Instance.saveFilePath);
                
                // Clear các list cũ
                treeObjects.Clear();
                treeStatusDatas = new List<TreeStatusData>();

                // Kiểm tra nature container
                if (natureContainer == null)
                {
                    Debug.LogError("Nature Container chưa được gán!");
                    return;
                }

                // Lấy tất cả gameobject trong nature container
                foreach (Transform child in natureContainer.transform)
                {
                    if (child.CompareTag("Tree"))
                    {
                        treeObjects.Add(child.gameObject);
                    }
                }

                Debug.Log($"Tìm thấy {treeObjects.Count} cây trong nature container");

                // Chuyển đổi từ GameObject sang TreeStatusData
                foreach (GameObject treeObject in treeObjects)
                {
                    TreeStatus treeStatus = treeObject.GetComponent<TreeStatus>();
                    if (treeStatus != null)
                    {
                        TreeStatusData treeData = new TreeStatusData()
                        {
                            positionX = treeObject.transform.position.x,
                            type = treeStatus.type,
                            currentWorkProgress = treeStatus.currentWorkProgress,
                            maxWorkProgress = treeStatus.maxWorkProgress,
                            isReadyToChop = treeStatus.isReadyToChop
                        };
                        
                        treeStatusDatas.Add(treeData);
                    }
                    else
                    {
                        Debug.LogWarning($"Tree object không có component TreeStatus: {treeObject.name}");
                    }
                }

                // Lưu vào file json
                string savePath = GetTreeSavePath();
                string json = JsonConvert.SerializeObject(treeStatusDatas, Formatting.Indented);
                File.WriteAllText(savePath, json);
                
                Debug.Log($"Đã lưu {treeStatusDatas.Count} cây vào {savePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lỗi khi save trees: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }
        // Trong phần LoadTrees
        public void LoadTrees()
        {
            treeObjects.Clear();
            ClearNatureContainer();

            if (!File.Exists(GetTreeSavePath())) return;

            string json = File.ReadAllText(GetTreeSavePath());
            List<TreeStatusData> loadedData = JsonConvert.DeserializeObject<List<TreeStatusData>>(json);

            foreach (TreeStatusData data in loadedData)
            {
                if (data.type < 0 || data.type >= PrefabData.Instance.treePrefabs.Count)
                    continue;

                // Tạo vị trí với Y = 0
                Vector3 position = new Vector3(data.positionX, 0f, 0f);
                
                GameObject newTree = Instantiate(
                    PrefabData.Instance.treePrefabs[data.type],
                    position, // Sử dụng position đã tạo
                    Quaternion.identity,
                    natureContainer.transform
                );

                TreeStatus status = newTree.GetComponent<TreeStatus>();
                if (status != null)
                {
                    status.type = data.type;
                    status.currentWorkProgress = data.currentWorkProgress;
                    status.maxWorkProgress = data.maxWorkProgress;
                    status.isReadyToChop = data.isReadyToChop;
                }

                treeObjects.Add(newTree);
            }
        }

        private string GetTreeSavePath()
        {
            // Sử dụng cùng đường dẫn với các file save khác
            return Path.Combine(LoadFromSave.Instance.saveFilePath, "trees.json");
        }
        private void ClearNatureContainer()
        {
            foreach (Transform child in natureContainer.transform)
            {
                if (child.CompareTag("Tree"))
                {
                    Destroy(child.gameObject);
                }
            }
        }
        public void SaveRabbitBush()
        {
            if (natureContainer == null)
            {
                Debug.LogError("NatureContainer không được thiết lập.");
                return;
            }

            List<GameObject> rabbitBushObjects = GetObjectsFromContainerList(natureContainer);
            List<RabbitBush.SaveRabbitBush> saveRabbitBushes = new List<RabbitBush.SaveRabbitBush>();

            foreach (GameObject rabbitBushObject in rabbitBushObjects)
            {
                if (rabbitBushObject.CompareTag("RabbitBush"))//kiểm tra lại tag
                {
                    RabbitBush rabbitBushComponent = rabbitBushObject.GetComponent<RabbitBush>();
                    if (rabbitBushComponent != null)
                    {
                        RabbitBush.SaveRabbitBush saveRabbitBush = new RabbitBush.SaveRabbitBush
                        {
                            positionX = rabbitBushObject.transform.position.x,
                            positionY = 0,
                            rabbits = new List<RabbitBush.SaveRabbit>()
                        };

                        foreach (GameObject rabbit in rabbitBushComponent.rabbitList)
                        {
                            Rabbit rabbitComponent = rabbit.GetComponent<Rabbit>();
                            if (rabbitComponent != null)
                            {
                                RabbitBush.SaveRabbit saveRabbit = new RabbitBush.SaveRabbit
                                {
                                    hp = rabbitComponent.hp,
                                    positionX = rabbit.transform.position.x,
                                    positionY = 0
                                };
                                saveRabbitBush.rabbits.Add(saveRabbit);
                            }
                        }
                        saveRabbitBushes.Add(saveRabbitBush);
                    }
                }
            }

            string json = JsonConvert.SerializeObject(saveRabbitBushes, Formatting.Indented);
            string filePath = Path.Combine(LoadFromSave.Instance.saveFilePath, "rabbitBushes.json");
            File.WriteAllText(filePath, json);

            Debug.Log("Saved rabbit bush data to " + filePath);
        }
        public void LoadRabbitBush()
        {
            string saveFilePath = LoadFromSave.Instance.saveFilePath;
            string filePath = Path.Combine(saveFilePath, "rabbitBushes.json");

            if (!File.Exists(filePath))
            {
                Debug.LogError("File rabbitBushes.json không tồn tại trong thư mục " + saveFilePath);
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                List<RabbitBush.SaveRabbitBush> savedRabbitBushes = JsonConvert.DeserializeObject<List<RabbitBush.SaveRabbitBush>>(json);

                foreach (RabbitBush.SaveRabbitBush saveRabbitBush in savedRabbitBushes)
                {
                    GameObject rabbitBushPrefab = PrefabData.Instance.rabbitBush;
                    Vector3 position = new Vector3(saveRabbitBush.positionX, saveRabbitBush.positionY, 0);
                    GameObject rabbitBushObject = Instantiate(rabbitBushPrefab, position, Quaternion.identity, natureContainer.transform);

                    RabbitBush rabbitBushComponent = rabbitBushObject.GetComponent<RabbitBush>();
                    if (rabbitBushComponent != null)
                    {
                        foreach (RabbitBush.SaveRabbit saveRabbit in saveRabbitBush.rabbits)
                        {
                            Vector3 rabbitPosition = new Vector3(saveRabbit.positionX, saveRabbit.positionY, 0);
                            GameObject rabbitObject = Instantiate(PrefabData.Instance.rabbit, rabbitPosition, Quaternion.identity);
                        
                            Rabbit rabbitComponent = rabbitObject.GetComponent<Rabbit>();
                            if (rabbitComponent != null)
                            {
                                rabbitComponent.hp = saveRabbit.hp;
                                rabbitComponent.rabbitBush = rabbitBushObject;
                            }
                            rabbitBushComponent.rabbitList.Add(rabbitObject);
                        }
                    }
                }
                Debug.Log("Loaded rabbit bush data from " + filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Lỗi khi load dữ liệu RabbitBush từ rabbitBushes.json: " + ex.Message);
            }
        }
        
        #endregion

        #region Structure
        public void SaveStructures()
        {
        }
        public void LoadStructures()
        {
        }
        #endregion
    }
}