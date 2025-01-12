using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Data_Model;
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

        public List<GameObject> treeObjects;
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
            // Tạo instance mới từ prefab
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
    
            // Chuyển instance (không phải prefab) vào container
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
        }

        public void LoadAllContainer()
        {
            //player  sẽ được instance riêng trong hàm load
            //ở đây,nếu ko tìm dc json thì tự tạo mới,còn nếu tìm dc thì load từ đó ra
            LoadNatures();
            LoadStructures();
        }

      

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
            
            LoadTrees();
            LoadRabbitBush();
        }
        
        /*
         *chú ý:tree được khởi tạo khi tạo các chunk nên chỉ được load,save trong chunkmanager hoặc hàm save trong game
         */
       
        public void SaveTrees()
        {
            // Kiểm tra container cây
            if (natureContainer == null)
            {
                Debug.LogError("NatureContainer không được thiết lập.");
                return;
            }

            // Lấy danh sách cây từ NatureContainer
            treeObjects = GetObjectsFromContainerList(natureContainer);
            List<NatureTree.TreeStatus> saveTrees = new List<NatureTree.TreeStatus>();

            // Duyệt qua từng cây và thêm vào danh sách saveTrees nếu tag là "Tree"
            foreach (GameObject treeObject in treeObjects)
            {
                if (treeObject.CompareTag("Tree")) // Kiểm tra tag là "Tree"
                {
                    NatureTree treeComponent = treeObject.GetComponent<NatureTree>();
                    if (treeComponent != null)
                    {
                        var position = treeObject.transform.position;
                        NatureTree.TreeStatus treeStatus = new NatureTree.TreeStatus(
                            treeComponent.Type,
                            treeComponent.treeChopCost,
                            treeComponent.treeHp,
                            treeComponent.isReadyToChop,
                            position.x,
                            position.y
                        );
                        saveTrees.Add(treeStatus);
                    }
                }
            }

            // Chuyển đổi danh sách saveTrees thành JSON và lưu vào file
            string json = JsonConvert.SerializeObject(saveTrees, Formatting.Indented);
            string filePath = Path.Combine(LoadFromSave.Instance.saveFilePath, "trees.json");
            File.WriteAllText(filePath, json);

            Debug.Log("Saved tree data to " + filePath);
        }
        public void LoadTrees()
        {
            string saveFilePath = LoadFromSave.Instance.saveFilePath;
            string filePath = Path.Combine(saveFilePath, "trees.json");

            // Kiểm tra nếu file JSON tồn tại
            if (!File.Exists(filePath))
            {
                Debug.LogError("File trees.json không tồn tại trong thư mục " + saveFilePath);
                Debug.LogError("hãy xóa file map đi để tạo lại file tree ");
                return;
            }

            try
            {
                // Đọc dữ liệu từ file JSON
                string json = File.ReadAllText(filePath);
                List<NatureTree.TreeStatus> savedTrees = JsonConvert.DeserializeObject<List<NatureTree.TreeStatus>>(json);

                // Duyệt qua mỗi SaveTree và khởi tạo cây
                foreach (NatureTree.TreeStatus saveTree in savedTrees)
                {
                    // Lấy prefab cây dựa vào Type
                    if (saveTree.type >= 0 && saveTree.type < PrefabData.Instance.treePrefabs.Count)
                    {
                        GameObject treePrefab = PrefabData.Instance.treePrefabs[saveTree.type];

                        // Khởi tạo cây ở vị trí xác định
                        Vector3 position = new Vector3(saveTree.positionX, 0, 0); // Đặt Y và Z nếu cần
                        GameObject treeObject = Instantiate(treePrefab, position, Quaternion.identity,
                            natureContainer.transform);

                        // Gán các thuộc tính cho cây
                        NatureTree treeComponent = treeObject.GetComponent<NatureTree>();
                        if (treeComponent != null)
                        {
                            treeComponent.Type = saveTree.type;
                            treeComponent.treeChopCost = saveTree.TreeChopCost;
                            treeComponent.treeHp = saveTree.TreeHp;
                            treeComponent.isReadyToChop = saveTree.IsReadyToChop;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Type cây không hợp lệ hoặc vượt quá danh sách prefab.");
                    }
                }

                Debug.Log("Load dữ liệu cây từ " + filePath + " thành công.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Lỗi khi load dữ liệu cây từ trees.json: " + ex.Message);
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