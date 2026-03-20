using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Controller.PlayerController;
using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Data_Model.Interfaces;
using UnityEngine;
// using Script.Manager.BuildingManager; // Avoid importing this namespace here to prevent name conflict with class BuildingManager

namespace Script.Manager
{
   
    public class EntitySaveManager : MonoBehaviour
    { 
        /*
       * todo: làm tiếp những cái container khác
       */
        public static EntitySaveManager Instance { get; private set; } 
        
        public static bool isDebug = false;

        /// <summary>
        /// True khi đang trong quá trình load dữ liệu từ save.
        /// Dùng để các script khác phân biệt Destroy do hệ thống save và Destroy thật trong gameplay.
        /// </summary>
        public static bool IsLoading { get; private set; } = false;
        
        public List<GameObject> allContainers; 
        
        public GameObject playerContainer;
        public GameObject chunkContainer; //gameobject chứa các chunk được instance ra,đã có riêng 1 script để save và load chunk
        public GameObject coinContainer; 
        
        public GameObject structureContainer;//công trình của làng
        
        public GameObject natureContainer; //spawm cây,RabbitBushStatus
        public GameObject animalsContainer;//spawn thỏ
        
        public GameObject enemyContainer;//chứa portal và enemy
        
        public GameObject denizenContainer; // All denizens (Vagrant, Villager, Archer, etc.)
        public GameObject statuesContainer;
       
        public GameObject otherContainer;
        
        private void Awake()
        {
            if (!AddSingleton())
            {
                // Nếu không phải là singleton instance, không thực hiện các bước tiếp theo
                return;
            }
            
            // Thiết lập các container
            SetUp();
            
            // Đảm bảo tất cả container nằm đúng vị trí
            ResetupAllContainers();
        }
        
        /// <summary>
        /// Thiết lập singleton pattern
        /// </summary>
        /// <returns>True nếu đây là instance chính, False nếu đối tượng này sẽ bị hủy</returns>
        private bool AddSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                return true;
            }
            else
            {
                Destroy(gameObject);
                return false;
            }
        }
        
        private void SetUp()
        {
            try
            {
                playerContainer = SetupContainerHelper("PlayerContainer", ref playerContainer);
                structureContainer = SetupContainerHelper("StructureContainer", ref structureContainer);
                coinContainer = SetupContainerHelper("CoinContainer", ref coinContainer);
                
                if (coinContainer != null && coinContainer.GetComponent<Script.Game.Item.Coin.CoinPool>() == null)
                {
                    Debug.LogWarning("[EntitySaveManager] CoinPool đã được thêm tự động vào CoinContainer vì hệ thống Save/Load container chịu trách nhiệm khởi tạo node này.");
                    coinContainer.AddComponent<Script.Game.Item.Coin.CoinPool>();
                }

                natureContainer = SetupContainerHelper("NatureContainer", ref natureContainer);
                chunkContainer = SetupContainerHelper("ChunkContainer", ref chunkContainer);
                enemyContainer = SetupContainerHelper("EnemyContainer", ref enemyContainer);
                animalsContainer = SetupContainerHelper("AnimalsContainer", ref animalsContainer);
                denizenContainer = SetupContainerHelper("DenizenContainer", ref denizenContainer);
                statuesContainer = SetupContainerHelper("StatuesContainer", ref statuesContainer);
                otherContainer = SetupContainerHelper("OtherContainer", ref otherContainer);
                
                PopulateAllContainersList();
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi thiết lập các container: {e.Message}\n{e.StackTrace}");
            }
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
                denizenContainer,
                statuesContainer,
                otherContainer
            };
            
            // Loại bỏ các container null nếu có
            allContainers.RemoveAll(item => item == null);
        }
        
        private GameObject SetupContainerHelper(string containerName, ref GameObject container)
        {
            try
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
                    // Check if child exists in parent first
                    Transform existingChild = containerParent.transform.Find(containerName);
                    if (existingChild != null)
                    {
                        container = existingChild.gameObject;
                    }
                    else
                    {
                        // Fallback: Try finding by name globally (though finding in parent is preferred)
                        container = GameObject.Find(containerName);
                        
                        if (container == null)
                        {
                            container = new GameObject(containerName);
                            if (isDebug) Debug.Log($"Created new container: {containerName}");
                        }
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
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi thiết lập container {containerName}: {e.Message}");
                return null;
            }
        }
        
        public GameObject InstanceAndMoveObjectToContainer(GameObject prefab, Vector2 position)
        {
            try
            {
                // Kiểm tra prefabWall không null
                if (prefab == null)
                {
                    Debug.LogError("Prefab được truyền vào là null.");
                    return null;
                }
    
                if (isDebug) Debug.Log($"Instantiating prefabWall: {prefab.name} with tag: {prefab.tag} at position: {position}");
    
                // Tạo instance mới từ prefabWall với rotation = identity để đảm bảo không có xoay
                GameObject instance = Instantiate(prefab, position, Quaternion.identity);
    
                // Kiểm tra xem instance có khác prefabWall không
                if (instance == prefab)
                {
                    Debug.LogError("Đối tượng được sinh ra là chính prefabWall gốc, điều này không nên xảy ra.");
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
                    if (child != null && child.gameObject != null)
                    {
                        objectsList.Add(child.gameObject);
                    }
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
            if (obj == null)
            {
                Debug.LogError("Không thể chuyển null object vào container");
                return;
            }
            
            if (isDebug)
            {
                Debug.Log($"Moving object '{obj.name}' with tag '{obj.tag}' to container");
            }
            
            try
            {
                switch (obj.tag)
                {
                    case "Player":
                        if (playerContainer != null)
                            obj.transform.SetParent(playerContainer.transform, true);
                        else
                            Debug.LogError("playerContainer is null");
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
                        if (structureContainer != null)
                            obj.transform.SetParent(structureContainer.transform, true);
                        else
                            Debug.LogError("structureContainer is null");
                        if (isDebug) Debug.Log($"Set parent of {obj.name} to StructureContainer");
                        break;
                    
                    case "Coin":
                        if (coinContainer != null)
                            obj.transform.SetParent(coinContainer.transform, true);
                        else
                            Debug.LogError("coinContainer is null");
                        if (isDebug) Debug.Log($"Set parent of {obj.name} to CoinContainer");
                        break;
                    
                    case "Tree":
                    case "RabbitBushStatus":
                        if (natureContainer != null)
                            obj.transform.SetParent(natureContainer.transform, true);
                        else
                            Debug.LogError("natureContainer is null");
                        if (isDebug) Debug.Log($"Set parent of {obj.name} to NatureContainer");
                        break;
                    
                    case "RabbitStatus":
                        if (animalsContainer != null)
                            obj.transform.SetParent(animalsContainer.transform, true);
                        else
                            Debug.LogError("animalsContainer is null");
                        if (isDebug) Debug.Log($"Set parent of {obj.name} to AnimalsContainer");
                        break;
                    
                    case "Enemy":
                        if (enemyContainer != null)
                            obj.transform.SetParent(enemyContainer.transform, true);
                        else
                            Debug.LogError("enemyContainer is null");
                        if (isDebug) Debug.Log($"Set parent of {obj.name} to EnemyContainer");
                        break;
                    
                    case "Wanderer":
                    case "Vagrant":
                    case "Villager":
                    case "Archer":
                    case "Worker":
                    case "Farmer":
                        if (denizenContainer != null)
                            obj.transform.SetParent(denizenContainer.transform, true);
                        else
                            Debug.LogError("denizenContainer is null");
                        if (isDebug) Debug.Log($"Set parent of {obj.name} to DenizenContainer");
                        break;
                    
                    case "Statues":
                        if (statuesContainer != null)
                            obj.transform.SetParent(statuesContainer.transform, true);
                        else
                            Debug.LogError("statuesContainer is null");
                        if (isDebug) Debug.Log($"Set parent of {obj.name} to StatuesContainer");
                        break;
                    
                    default:
                        if (otherContainer != null)
                            obj.transform.SetParent(otherContainer.transform, true);
                        else
                            Debug.LogError("otherContainer is null");
                        if (isDebug) Debug.Log($"Đã chuyển {obj.name} vào OtherContainer");
                        break;
                }
                
                // Log sau khi đã chuyển
                if (isDebug && obj != null && obj.transform != null && obj.transform.parent != null)
                {
                    Debug.Log($"After move: {obj.name} has parent {obj.transform.parent.name}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi chuyển đối tượng {obj.name} vào container: {e.Message}");
            }
        }
        
        //chuyển toàn bộ gameobject con của nó vào container theo tag
        public void MoveChildrenToContainers(GameObject parent)
        {
            if (parent == null)
            {
                Debug.LogError("Parent object is null");
                return;
            }
            
            try
            {
                Transform containerTransform = parent.transform.Find("statuesContainer");
                if (containerTransform != null)
                {
                    List<Transform> children = new List<Transform>();
                    foreach (Transform child in containerTransform)
                    {
                        if (child != null)
                        {
                            children.Add(child);
                        }
                    }

                    foreach (Transform child in children)
                    {
                        if (child != null && child.gameObject != null)
                        {
                            MoveObjectToContainer(child.gameObject);
                        }
                    }
                }
                else
                {
                    if (isDebug) Debug.Log("statuesContainer not found in parent");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi di chuyển các đối tượng con: {e.Message}");
            }
        }
        
        // Phương thức để làm mới danh sách container, loại bỏ những container đã bị hủy
        private void RefreshContainersList()
        {
            try
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
                
                if (coinContainer != null && coinContainer.GetComponent<Script.Game.Item.Coin.CoinPool>() == null)
                {
                    Debug.LogWarning("[EntitySaveManager] CoinPool đã được thêm tự động vào CoinContainer khi Refresh.");
                    coinContainer.AddComponent<Script.Game.Item.Coin.CoinPool>();
                }

                natureContainer = EnsureContainerExists("NatureContainer", ref natureContainer);
                chunkContainer = EnsureContainerExists("ChunkContainer", ref chunkContainer);
                enemyContainer = EnsureContainerExists("EnemyContainer", ref enemyContainer);
                animalsContainer = EnsureContainerExists("AnimalsContainer", ref animalsContainer);
                denizenContainer = EnsureContainerExists("DenizenContainer", ref denizenContainer);
                statuesContainer = EnsureContainerExists("StatuesContainer", ref statuesContainer);
                otherContainer = EnsureContainerExists("OtherContainer", ref otherContainer);
                
                // Cập nhật lại danh sách
                PopulateAllContainersList();
                
                if (isDebug) Debug.Log("Container list refreshed successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi refresh container list: {e.Message}");
            }
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
            try
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
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi lưu tất cả container: {e.Message}\n{e.StackTrace}");
            }
        }
        
        public void LoadAllContainer()
        {
            IsLoading = true;
            try
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
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi load tất cả container: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                // Đảm bảo IsLoading luôn được reset dù có Exception
                IsLoading = false;
            }
        }
        
        private void LoadAndSetupPlayer()
        {
            try
            {
                string playerPath = Path.Combine(LoadFromSave.Instance.saveFilePath, "PlayerContainer.json");
                if (!File.Exists(playerPath))
                {
                    if (isDebug) Debug.LogWarning("Không tìm thấy file save của player, player sẽ dùng data mặc định");
                    return; // Không có save data, player sẽ dùng giá trị mặc định
                }

                // Kiểm tra PlayerController.Instance và player đã tồn tại chưa
                if (PlayerController.Instance == null || PlayerController.Instance.player == null)
                {
                    if (isDebug) Debug.LogWarning("PlayerController hoặc player chưa được tạo, sẽ bỏ qua load data");
                    return; // Player chưa được tạo, để StartGameScrene.InstancePlayerAndSetCamera() tạo trước
                }

                // Player đã tồn tại, chỉ restore data (không di chuyển vị trí vì đã được xử lý trong InstancePlayerAndSetCamera)
                GameObject player = PlayerController.Instance.player;

                // Load data từ file save
                var json = File.ReadAllText(playerPath);
                var savedObjects = JsonConvert.DeserializeObject<List<SavedObject>>(json,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                // Áp dụng data vào player hiện có (chỉ stats, không vị trí vì vị trí đã được set khi spawn)
                if (savedObjects != null && savedObjects.Count > 0)
                {
                    var saveable = player.GetComponent<ISaveable>();
                    if (saveable != null)
                    {
                        saveable.RestoreData(savedObjects[0].data);
                        if (isDebug) Debug.Log("Đã restore data cho player từ save (không thay đổi vị trí)");
                    }
                    else
                    {
                        Debug.LogError("Player không có component ISaveable");
                    }
                }

                if (isDebug) Debug.Log("Đã load data player thành công (vị trí đã được set khi spawn)");
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi load player data: {e.Message}\n{e.StackTrace}");
                // Không tạo player mới, để StartGameScrene.InstancePlayerAndSetCamera() xử lý
            }
        }

        private void CreateNewPlayer()
        {
            try
            {
                // Kiểm tra PrefabData.Instance tồn tại
                if (PrefabData.Instance == null || PrefabData.Instance.player == null)
                {
                    Debug.LogError("PrefabData.Instance hoặc player prefabWall là null");
                    return;
                }
                
                GameObject player = InstanceAndMoveObjectToContainer(PrefabData.Instance.player, Vector2.zero);
                if (player == null)
                {
                    Debug.LogError("Không thể tạo instance của player!");
                    return;
                }
                
                // Kiểm tra PlayerController.Instance tồn tại
                if (PlayerController.Instance == null)
                {
                    Debug.LogError("PlayerController.Instance là null, không thể thiết lập player");
                    return;
                }
                
                PlayerController.Instance.player = player;
                PlayerController.Instance.SetUpControllerPlayer();
                
                // Kiểm tra CameraFollowPlayer.Instance tồn tại
                if (CameraFollowPlayer.Instance != null)
                {
                    CameraFollowPlayer.Instance.setCameraTarget();
                    CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);
                }
                else
                {
                    Debug.LogError("CameraFollowPlayer.Instance là null, không thể thiết lập camera");
                }
                
                if (isDebug) Debug.Log("Đã tạo player mới");
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi tạo player mới: {e.Message}");
            }
        }
        
        private void SaveContainer(GameObject container)
        {
            if (container == null)
            {
                Debug.LogError("Container is null, skipping save.");
                return;
            }

            try
            {
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
                    
                    // Bỏ qua các object inactive — chúng là pool objects (VD: CoinPool preload).
                    // Coin đang nằm trong pool luôn inactive; chỉ save coin active (đang có trong thế giới).
                    if (!child.gameObject.activeSelf)
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
                
                // Kiểm tra LoadFromSave.Instance tồn tại
                if (LoadFromSave.Instance == null || string.IsNullOrEmpty(LoadFromSave.Instance.saveFilePath))
                {
                    Debug.LogError("LoadFromSave.Instance hoặc saveFilePath là null, không thể lưu container");
                    return;
                }
                
                var path = Path.Combine(LoadFromSave.Instance.saveFilePath, $"{container.name}.json");
                if (isDebug) Debug.Log($"Saving to path: {path}");
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi lưu container {container.name}: {e.Message}");
            }
        }

        private void LoadContainer(GameObject container)
        {
            if (container == null) return;

            try
            {
                // Kiểm tra LoadFromSave.Instance tồn tại
                if (LoadFromSave.Instance == null || string.IsNullOrEmpty(LoadFromSave.Instance.saveFilePath))
                {
                    Debug.LogError("LoadFromSave.Instance hoặc saveFilePath là null, không thể load container");
                    return;
                }
                
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
                //RemoveAllChildObjects(container);

                // Load và tạo object mới
                foreach (var savedObject in savedObjects)
                {
                    if (isDebug) Debug.Log($"Attempting to load prefab with ID: {savedObject.prefabID}");
                    // Kiểm tra PrefabData.Instance tồn tại
                    if (PrefabData.Instance == null)
                    {
                        Debug.LogError("PrefabData.Instance là null, không thể load prefab");
                        continue;
                    }
                    
                    var prefab = PrefabData.Instance.GetPrefabByID(savedObject.prefabID);
                    if (prefab == null)
                    {
                        // Fallback cho "stump" nếu chưa có prefab riêng: dùng "wall" làm base
                        if (savedObject.prefabID == "stump")
                        {
                            prefab = PrefabData.Instance.prefabWall;  // Hoặc PrefabData.Instance.prefabWall nếu public
                            if (prefab == null)
                            {
                                Debug.LogError($"Prefab fallback 'wall' not found for 'stump'");
                                continue;
                            }
                            if (isDebug) Debug.Log($"Using fallback 'wall' prefab for 'stump'");
                        }
                        else
                        {
                            Debug.LogError($"Prefab not found: {savedObject.prefabID}");
                            // Log all keys in the prefab dictionary for debugging (giữ nguyên)
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
                    }

                    if (isDebug) Debug.Log($"Creating instance of prefab: {prefab.name}");
                    var instance = Instantiate(prefab, container.transform);

                    // Special handling: if this is a TownCenter prefab, infer and set tier from prefabID before RestoreData
                    // (Giữ nguyên cho TownCenter, nhưng bỏ nếu prefabID không còn tier)
                    try
                    {
                        if (!string.IsNullOrEmpty(savedObject.prefabID) && savedObject.prefabID.StartsWith("town_center_"))
                        {
                            var tcs = instance.GetComponent<TownCenterStatus>();
                            if (tcs != null)
                            {
                                var suffix = savedObject.prefabID.Substring("town_center_".Length);
                                if (int.TryParse(suffix, out int parsedTier))
                                {
                                    tcs.tier = parsedTier;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[EntitySaveManager] Failed to set TownCenter tier from prefabID: {e.Message}");
                    }

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
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi load container {container.name}: {e.Message}\n{e.StackTrace}");
            }
        }
        
        public void RemoveAllChildObjects(GameObject parent)
        {
            if (parent == null)
            {
                Debug.LogError("Parent object is null");
                return;
            }
            
            try
            {
                List<Transform> childrenToDestroy = new List<Transform>();
                foreach (Transform child in parent.transform)
                {
                    if (child != null)
                    {
                        childrenToDestroy.Add(child);
                    }
                }
                
                foreach (Transform child in childrenToDestroy)
                {
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi xóa các đối tượng con: {e.Message}");
            }
        }

      
        public void MoveTreesFromChunks()
        {
            if (chunkContainer == null)
            {
                if (isDebug) Debug.LogWarning("ChunkContainer is null!");
                return;
            }

            try
            {
                // Chỉ tìm các cây trong chunkContainer
                Transform[] allObjectsInChunks = chunkContainer.GetComponentsInChildren<Transform>(true);
                if (allObjectsInChunks == null)
                {
                    Debug.LogWarning("Không tìm thấy đối tượng nào trong chunkContainer");
                    return;
                }
                
                foreach (Transform obj in allObjectsInChunks)
                {
                    if (obj != null && obj.CompareTag(Script.Utils.GameTags.Tree))
                    {
                        MoveObjectToContainer(obj.gameObject);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi di chuyển cây từ chunks: {e.Message}");
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
            
            try
            {
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
                denizenContainer = SetupContainerHelper("DenizenContainer", ref denizenContainer);
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
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi thiết lập lại các container: {e.Message}");
            }
        }

        // New method to find the wall farthest from town center
        public GameObject FindFarthestWall(bool isRight)
        {
            try
            {
                if (Building.BuildingManager.instance == null || Building.BuildingManager.instance.currentTownCenter == null)
                {
                    Debug.LogWarning("No town center found to determine wall position");
                    return null;
                }
                
                Vector3 townCenterPos = Building.BuildingManager.instance.currentTownCenter.transform.position;
                List<GameObject> walls = GetObjectsFromContainerList(structureContainer);
                
                GameObject farthestWall = null;
                float maxDistance = 0f;
                
                foreach (GameObject obj in walls)
                {
                    if (obj != null && obj.CompareTag(Script.Utils.GameTags.Wall))
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
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi tìm tường xa nhất: {e.Message}");
                return null;
            }
        }
    }
}
