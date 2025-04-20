using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using UnityEngine;

namespace Script
{
   
    public class SetUpContainer : MonoBehaviour
    { 
        /*
       * todo: làm tiếp những cái container khác
       */
        public static SetUpContainer Instance { get; private set; } 
        
        public static bool isDebug = false;
        
        public List<GameObject> allContainers; 
        
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
       
        public GameObject otherContainer;
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
            otherContainer = SetupContainerHelper("OtherContainer", ref otherContainer);
            
            PopulateAllContainersList(); 
        }
        private void PopulateAllContainersList()
        {
            allContainers = new List<GameObject>
            {
                playerContainer,
                natureContainer,
                structureContainer,
                coinContainer,
                enemyContainer,
                animalsContainer,
                wanderersContainer,
                villagersContainer,
                statuesContainer,
                otherContainer
            };
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

            // Tạo instance mới từ prefab với rotation = identity để đảm bảo không có xoay
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);

            // Kiểm tra xem instance có khác prefab không
            if (instance == prefab)
            {
                Debug.LogError("Đối tượng được sinh ra là chính prefab gốc, điều này không nên xảy ra.");
            }
            
            // Đảm bảo rotation luôn là identity (không xoay)
            instance.transform.rotation = Quaternion.identity;
            if (isDebug) Debug.Log($"Instance created with rotation: {instance.transform.rotation.eulerAngles}");

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
                case "Wall":
                case "Structure":
                case "TownCenter":
                case "BowVendor":
                case "HammerVendor":
                case "ScytheVendor":
                case "SwordVendor":
                case "SpikeVendor":
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
                    obj.transform.SetParent(otherContainer.transform, true);
                    if (isDebug) Debug.Log($"Đã chuyển {obj.name} vào OtherContainer");
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
            MoveTreesFromChunks(); // Di chuyển cây vào container trước khi save
            if (isDebug) Debug.Log($"Save path: {LoadFromSave.Instance.saveFilePath}");
            
            if (isDebug)
            {
                foreach (var container in allContainers)
                {
                    Debug.Log($"Saving container: {container.name}, child count: {container.transform.childCount}");
                }
            }

            foreach (var container in allContainers)
            {
                SaveContainer(container);
            }
        }
        public void LoadAllContainer()
        {
            if (isDebug) Debug.Log($"Save path: {LoadFromSave.Instance.saveFilePath}");
            foreach (var container in allContainers)
            {
                if (container == playerContainer)
                {
                    LoadAndSetupPlayer(); // Hàm mới để xử lý riêng player
                }
                else
                {
                    LoadContainer(container);
                }
            }
            Time.timeScale = 1;//dòng này fix lỗi time scale 0 khi load map
        }
        private void LoadAndSetupPlayer()
        {
            string playerPath = Path.Combine(LoadFromSave.Instance.saveFilePath, "PlayerContainer.json");
            if (!File.Exists(playerPath))
            {
                if (isDebug) Debug.LogWarning("Không tìm thấy file save của player, tạo player mới");
                CreateNewPlayer();
                return;
            }

            try
            {
                // Instance player mới từ prefab
                GameObject player = InstanceAndMoveObjectToContainer(PrefabData.Instance.player, Vector2.zero);
                if (player == null)
                {
                    Debug.LogError("Không thể tạo instance của player!");
                    return;
                }

                // Gán player vào PlayerController
                PlayerController.Instance.player = player;

                // Load data từ file save
                var json = File.ReadAllText(playerPath);
                var savedObjects = JsonConvert.DeserializeObject<List<SavedObject>>(json,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                // Áp dụng data vào player
                if (savedObjects != null && savedObjects.Count > 0)
                {
                    var saveable = player.GetComponent<ISaveable>();
                    saveable?.RestoreData(savedObjects[0].data);
                }

                // Setup các thành phần liên quan
                PlayerController.Instance.SetUpControllerPlayer();
                CameraFollowPlayer.Instance.setCameraTarget();
                CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);

                if (isDebug) Debug.Log("Đã load và setup player thành công");
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi load player: {e.Message}");
                CreateNewPlayer();
            }
        }

        private void CreateNewPlayer()
        {
            GameObject player = InstanceAndMoveObjectToContainer(PrefabData.Instance.player, Vector2.zero);
            PlayerController.Instance.player = player;
            PlayerController.Instance.SetUpControllerPlayer();
            CameraFollowPlayer.Instance.setCameraTarget();
            CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);
            if (isDebug) Debug.Log("Đã tạo player mới");
        }
        
        private void SaveContainer(GameObject container)
        {
            if (container == null)
            {
                Debug.LogError("Container is null, skipping save.");
                return;
            }

            var savedObjects = new List<SavedObject>();
            foreach (Transform child in container.transform)
            {
                var saveable = child.GetComponent<ISaveable>();
                if (saveable != null)
                {
                    savedObjects.Add(new SavedObject
                    {
                        prefabID = saveable.GetPrefabID(),
                        data = saveable.CaptureData()
                    });
                }
            }

            if (isDebug) Debug.Log($"Saving {container.name} with {savedObjects.Count} objects");

            if (savedObjects.Count == 0)
            {
                if (isDebug) Debug.LogWarning($"No saveable objects found in {container.name}");
            }

            var json = JsonConvert.SerializeObject(savedObjects, Formatting.Indented,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            
            var path = Path.Combine(LoadFromSave.Instance.saveFilePath, $"{container.name}.json");
            if (isDebug) Debug.Log($"Saving to path: {path}");
            File.WriteAllText(path, json);
        }

        private void LoadContainer(GameObject container)
        {
            if (container == null) return;

            var path = Path.Combine(LoadFromSave.Instance.saveFilePath, $"{container.name}.json");
            if (!File.Exists(path))
            {
                if (isDebug) Debug.LogWarning($"File {path} không tồn tại!");
                return;
            }
            // Đọc và log nội dung JSON
            var json = File.ReadAllText(path);
            if (isDebug) Debug.Log($"JSON của {container.name}: {json}");

            var savedObjects = JsonConvert.DeserializeObject<List<SavedObject>>(json, 
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            if (savedObjects == null)
            {
                Debug.LogError($"Không thể deserialize JSON của {container.name}!");
                return;
            }

            if (isDebug) Debug.Log($"{container.name} cần load {savedObjects.Count} object.");

            // Xóa toàn bộ object cũ trong container
            foreach (Transform child in container.transform)
            {
                Destroy(child.gameObject);
            }

            // Load và tạo object mới
            json = File.ReadAllText(path);
            savedObjects = JsonConvert.DeserializeObject<List<SavedObject>>(json, 
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            foreach (Transform child in container.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var savedObject in savedObjects)
            {
                if (isDebug) Debug.Log($"Attempting to load prefab with ID: {savedObject.prefabID}");
                var prefab = PrefabData.Instance.GetPrefabByID(savedObject.prefabID);
                if (prefab == null)
                {
                    Debug.LogError($"Prefab not found: {savedObject.prefabID}");
                    // Log all keys in the prefab dictionary for debugging
                    if (isDebug)
                    {
                        var prefabDictType = PrefabData.Instance.GetType().GetField("_prefabDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (prefabDictType != null)
                        {
                            var prefabDict = prefabDictType.GetValue(PrefabData.Instance) as Dictionary<string, GameObject>;
                            if (prefabDict != null)
                            {
                                Debug.Log("Available prefab IDs:");
                                foreach (var key in prefabDict.Keys)
                                {
                                    Debug.Log($"- {key}");
                                }
                            }
                        }
                    }
                    continue;
                }

                if (isDebug) Debug.Log($"Creating instance of prefab: {prefab.name}");
                var instance = Instantiate(prefab, container.transform);
                var saveable = instance.GetComponent<ISaveable>();
                if (saveable != null)
                {
                    saveable.RestoreData(savedObject.data);
                    if (isDebug) Debug.Log($"Restored data for {instance.name}");
                }
                else
                {
                    Debug.LogError($"ISaveable not found on {instance.name}");
                }
            }
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

      
        public void MoveTreesFromChunks()
        {
            if (chunkContainer == null)
            {
                if (isDebug) Debug.LogWarning("ChunkContainer is null!");
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
        /*
         *chú ý:tree được khởi tạo khi tạo các chunk nên chỉ được load,save trong chunkmanager hoặc hàm save trong game
         */
        [System.Serializable]
        public class SavedObject
        {
            public string prefabID;
            public object data;
        }


      
    }
}
