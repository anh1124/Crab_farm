using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Game.Other;
using UnityEngine;

namespace Script
{
   
    public class EntitySaveManager : MonoBehaviour
    { 
        /*
       * todo: làm tiếp những cái container khác
       */
        public static EntitySaveManager Instance { get; private set; } 
        
        public static bool isDebug = true;
        
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
            
            // Đảm bảo tất cả container nằm đúng vị trí
            ResetupAllContainers();
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
            // Tìm hoặc tạo GameObject cha tên "Container" 
            GameObject containerParent = GameObject.Find("Container");
            if (containerParent == null)
            {
                containerParent = new GameObject("Container");
                if (isDebug) Debug.Log("Created parent Container GameObject");
            }
            
            if (container == null)
            {
                container = GameObject.FindGameObjectWithTag(containerName);
                if (container == null)
                {
                    container = new GameObject(containerName);
                    container.tag = containerName;
                    if (isDebug) Debug.Log($"Created new container: {containerName}");
                }
                
                // Đảm bảo container là con của containerParent
                container.transform.SetParent(containerParent.transform);
                if (isDebug) Debug.Log($"Set {containerName} as child of Container");
            }
            else if (container.transform.parent != containerParent.transform)
            {
                // Nếu container đã tồn tại nhưng không phải con của containerParent
                container.transform.SetParent(containerParent.transform);
                if (isDebug) Debug.Log($"Moved existing {containerName} under Container parent");
            }
            
            return container;
        }
        public GameObject InstanceAndMoveObjectToContainer(GameObject prefab, Vector2 position)
        {
            try
            {
                // Kiểm tra prefab không null
                if (prefab == null)
                {
                    Debug.LogError("Prefab được truyền vào là null.");
                    return null;
                }
    
                if (isDebug) Debug.Log($"Instantiating prefab: {prefab.name} with tag: {prefab.tag} at position: {position}");
    
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
                
                // Log thông tin của instance trước khi chuyển vào container
                if (isDebug) Debug.Log($"Instance before moving to container - Name: {instance.name}, Tag: {instance.tag}");
    
                // Chuyển instance vào container
                MoveObjectToContainer(instance);
    
                // Trả về instance để có thể sử dụng sau này nếu cần
                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in InstanceAndMoveObjectToContainer: {e.Message}\nStackTrace: {e.StackTrace}");
                return null;
            }
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
            if (isDebug)
            {
                Debug.Log($"Moving object '{obj.name}' with tag '{obj.tag}' to container");
            }
            
            switch (obj.tag)
            {
                case "Player":
                    obj.transform.SetParent(playerContainer.transform, true);
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to PlayerContainer");
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
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to StructureContainer");
                    break;
                
                case "Coin":
                    obj.transform.SetParent(coinContainer.transform, true);
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to CoinContainer");
                    break;
                
                case "Tree":
                case "RabbitBush":
                    obj.transform.SetParent(natureContainer.transform, true);
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to NatureContainer");
                    break;
                
                case "Rabbit":
                    obj.transform.SetParent(animalsContainer.transform, true);
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to AnimalsContainer");
                    break;
                
                case "Enemy":
                    obj.transform.SetParent(enemyContainer.transform, true);
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to EnemyContainer");
                    break;
                
                case "Wanderer":
                case "Vagrant":
                    obj.transform.SetParent(wanderersContainer.transform, true);
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to WanderersContainer");
                    break;
                
                case "Villager":
                    obj.transform.SetParent(villagersContainer.transform, true);
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to VillagersContainer");
                    break;
                
                case "Statues":
                    obj.transform.SetParent(statuesContainer.transform, true);
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to StatuesContainer");
                    break;
                
                default:
                    obj.transform.SetParent(otherContainer.transform, true);
                    if (isDebug) Debug.Log($"Đã chuyển {obj.name} vào OtherContainer");
                    break;
            }
            
            // Log sau khi đã chuyển
            if (isDebug && obj != null && obj.transform != null && obj.transform.parent != null)
            {
                Debug.Log($"After move: {obj.name} has parent {obj.transform.parent.name}");
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
        // Phương thức để làm mới danh sách container, loại bỏ những container đã bị hủy
        private void RefreshContainersList()
        {
            if (allContainers == null)
            {
                allContainers = new List<GameObject>();
                PopulateAllContainersList();
                return;
            }
            
            // Kiểm tra và thiết lập lại các container nếu cần
            playerContainer = EnsureContainerExists("PlayerContainer", ref playerContainer);
            structureContainer = EnsureContainerExists("StructureContainer", ref structureContainer);
            coinContainer = EnsureContainerExists("CoinContainer", ref coinContainer);
            natureContainer = EnsureContainerExists("NatureContainer", ref natureContainer);
            chunkContainer = EnsureContainerExists("ChunkContainer", ref chunkContainer);
            enemyContainer = EnsureContainerExists("EnemyContainer", ref enemyContainer);
            animalsContainer = EnsureContainerExists("AnimalsContainer", ref animalsContainer);
            wanderersContainer = EnsureContainerExists("WanderersContainer", ref wanderersContainer);
            villagersContainer = EnsureContainerExists("VillagersContainer", ref villagersContainer);
            statuesContainer = EnsureContainerExists("StatuesContainer", ref statuesContainer);
            otherContainer = EnsureContainerExists("OtherContainer", ref otherContainer);
            
            // Cập nhật lại danh sách
            PopulateAllContainersList();
            
            if (isDebug) Debug.Log("Container list refreshed successfully.");
        }
        
        private GameObject EnsureContainerExists(string containerName, ref GameObject container)
        {
            if (container == null)
            {
                if (isDebug) Debug.LogWarning($"Container {containerName} is null, attempting to find or recreate it.");
                return SetupContainerHelper(containerName, ref container);
            }
            return container;
        }
        
        public void SaveAllContainer()
        {
            // Refresh danh sách container trước khi lưu
            RefreshContainersList();
            
            MoveTreesFromChunks(); // Di chuyển cây vào container trước khi save
            if (isDebug) Debug.Log($"Save path: {LoadFromSave.Instance.saveFilePath}");
            
            if (isDebug)
            {
                // Kiểm tra và log thông tin container an toàn
                foreach (var container in allContainers)
                {
                    if (container != null)
                    {
                        Debug.Log($"Saving container: {container.name}, child count: {container.transform.childCount}");
                    }
                    else
                    {
                        Debug.LogWarning("Found null container in allContainers list!");
                    }
                }
            }

            // Lọc bỏ các container null trước khi lưu
            List<GameObject> validContainers = new List<GameObject>();
            foreach (var container in allContainers)
            {
                if (container != null)
                {
                    validContainers.Add(container);
                }
                else
                {
                    Debug.LogWarning("Skipping null container during save process");
                }
            }

            // Lưu các container hợp lệ
            foreach (var container in validContainers)
            {
                SaveContainer(container);
            }
        }
        
        public void LoadAllContainer()
        {
            // Refresh danh sách container trước khi load
            RefreshContainersList();
            
            if (isDebug) Debug.Log($"Save path: {LoadFromSave.Instance.saveFilePath}");
            
            // Lọc bỏ các container null trước khi load
            List<GameObject> validContainers = new List<GameObject>();
            foreach (var container in allContainers)
            {
                if (container != null)
                {
                    validContainers.Add(container);
                }
                else
                {
                    Debug.LogWarning("Skipping null container during load process");
                }
            }
            
            // Load các container hợp lệ
            foreach (var container in validContainers)
            {
                try
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
                catch (Exception e)
                {
                    Debug.LogError($"Error loading container {container.name}: {e.Message}");
                }
            }
            Time.timeScale = 1; //dòng này fix lỗi time scale 0 khi load map
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
                PlayerController.PlayerController.Instance.player = player;

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
                PlayerController.PlayerController.Instance.SetUpControllerPlayer();
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
            PlayerController.PlayerController.Instance.player = player;
            PlayerController.PlayerController.Instance.SetUpControllerPlayer();
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
            // Lấy danh sách con an toàn để tránh lỗi khi iterable thay đổi trong quá trình lặp
            List<Transform> childrenToSave = new List<Transform>();
            foreach (Transform child in container.transform)
            {
                if (child != null)
                {
                    childrenToSave.Add(child);
                }
            }
            
            foreach (Transform child in childrenToSave)
            {
                // Kiểm tra null để tránh lỗi với objects đã bị destroy
                if (child == null || child.gameObject == null)
                {
                    continue;
                }
                
                var saveable = child.GetComponent<ISaveable>();
                if (saveable != null)
                {
                    try
                    {
                        savedObjects.Add(new SavedObject
                        {
                            prefabID = saveable.GetPrefabID(),
                            data = saveable.CaptureData()
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error saving object {child.name}: {e.Message}");
                    }
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

        // Phương thức để buộc cài đặt lại tất cả các container, đảm bảo chúng ở đúng vị trí
        public void ResetupAllContainers()
        {
            if (isDebug) Debug.Log("Forcing container re-setup...");
            
            // Tìm hoặc tạo GameObject cha
            GameObject containerParent = GameObject.Find("Container");
            if (containerParent == null)
            {
                containerParent = new GameObject("Container");
                if (isDebug) Debug.Log("Created parent Container GameObject");
            }
            
            // Xóa các container hiện tại nếu chúng không nằm ở đúng vị trí
            if (otherContainer != null && otherContainer.transform.parent != containerParent.transform)
            {
                if (isDebug) Debug.Log($"OtherContainer in wrong position, deleting it");
                GameObject tempContainer = otherContainer;
                otherContainer = null;
                Destroy(tempContainer);
            }
            
            // Đảm bảo tất cả các container được tạo lại
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
            
            // Cập nhật lại danh sách container
            PopulateAllContainersList();
            
            if (isDebug)
            {
                Debug.Log("All containers have been reset and placed under the Container parent");
                foreach (var container in allContainers)
                {
                    if (container != null)
                    {
                        Debug.Log($"Container: {container.name}, Parent: {container.transform.parent.name}");
                    }
                }
            }
        }

        // New method to find the wall farthest from town center
        public GameObject FindFarthestWall(bool isRight)
        {
            if (BuildingManager.instance == null || BuildingManager.instance.CurrentTownCenter == null)
            {
                Debug.LogWarning("No town center found to determine wall position");
                return null;
            }
            
            Vector3 townCenterPos = BuildingManager.instance.CurrentTownCenter.transform.position;
            List<GameObject> walls = GetObjectsFromContainerList(structureContainer);
            
            GameObject farthestWall = null;
            float maxDistance = 0f;
            
            foreach (GameObject obj in walls)
            {
                if (obj.CompareTag("Wall"))
                {
                    Vector3 wallPos = obj.transform.position;
                    
                    // Only check walls on the right side if isRight is true, or left side if isRight is false
                    bool isWallOnRightSide = wallPos.x > townCenterPos.x;
                    
                    if (isWallOnRightSide == isRight)
                    {
                        float distance = Mathf.Abs(wallPos.x - townCenterPos.x);
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            farthestWall = obj;
                        }
                    }
                }
            }
            
            return farthestWall;
        }
    }
}
