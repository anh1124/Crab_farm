using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Controller.PlayerController;
using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy;
using Script.Game.Enemy.Core;
using Script.Game.Item.Coin;
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
        
        public GameObject natureContainer; //spawn cây,RabbitBushBehavior
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
                string tag = obj.tag;
                if (tag == Script.Utils.GameTags.Player)
                {
                    if (playerContainer != null)
                        obj.transform.SetParent(playerContainer.transform, true);
                    else
                        Debug.LogError("playerContainer is null");
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to PlayerContainer");
                }
                else if (tag == Script.Utils.GameTags.Wall || 
                         tag == Script.Utils.GameTags.Structure || 
                         tag == Script.Utils.GameTags.TownCenter ||
                         tag == "BowVendor" || 
                         tag == "HammerVendor" || 
                         tag == "ScytheVendor" || 
                         tag == "SwordVendor" || 
                         tag == "SpikeVendor")
                {
                    if (structureContainer != null)
                        obj.transform.SetParent(structureContainer.transform, true);
                    else
                        Debug.LogError("structureContainer is null");
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to StructureContainer");
                }
                else if (tag == Script.Utils.GameTags.Coin)
                {
                    if (coinContainer != null)
                        obj.transform.SetParent(coinContainer.transform, true);
                    else
                        Debug.LogError("coinContainer is null");
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to CoinContainer");
                }
                else if (tag == Script.Utils.GameTags.Tree || tag == "RabbitBushStatus")
                {
                    if (natureContainer != null)
                        obj.transform.SetParent(natureContainer.transform, true);
                    else
                        Debug.LogError("natureContainer is null");
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to NatureContainer");
                }
                else if (tag == Script.Utils.GameTags.Animal || tag == "RabbitStatus")
                {
                    if (animalsContainer != null)
                        obj.transform.SetParent(animalsContainer.transform, true);
                    else
                        Debug.LogError("animalsContainer is null");
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to AnimalsContainer");
                }
                else if (tag == Script.Utils.GameTags.Enemy)
                {
                    if (enemyContainer != null)
                        obj.transform.SetParent(enemyContainer.transform, true);
                    else
                        Debug.LogError("enemyContainer is null");
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to EnemyContainer");
                }
                else if (tag == "Wanderer" || 
                         tag == Script.Utils.GameTags.Vagrant || 
                         tag == Script.Utils.GameTags.Villager || 
                         tag == Script.Utils.GameTags.Archer || 
                         tag == "Worker" || 
                         tag == "Farmer")
                {
                    if (denizenContainer != null)
                        obj.transform.SetParent(denizenContainer.transform, true);
                    else
                        Debug.LogError("denizenContainer is null");
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to DenizenContainer");
                }
                else if (tag == Script.Utils.GameTags.Statues)
                {
                    if (statuesContainer != null)
                        obj.transform.SetParent(statuesContainer.transform, true);
                    else
                        Debug.LogError("statuesContainer is null");
                    if (isDebug) Debug.Log($"Set parent of {obj.name} to StatuesContainer");
                }
                else
                {
                    if (otherContainer != null)
                        obj.transform.SetParent(otherContainer.transform, true);
                    else
                        Debug.LogError("otherContainer is null");
                    if (isDebug) Debug.Log($"Đã chuyển {obj.name} vào OtherContainer (Unknown tag: {tag})");
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
                // First, check for specifically designated sub-containers like 'statuesContainer'
                Transform statuesContainerChild = parent.transform.Find("statuesContainer");
                if (statuesContainerChild != null)
                {
                    List<Transform> children = new List<Transform>();
                    foreach (Transform child in statuesContainerChild) children.Add(child);
                    foreach (Transform child in children) MoveObjectToContainer(child.gameObject);
                }

                // Also move ANY child of the parent that has a known tag (Tree, Wall, etc.)
                // This handles chunk prefabs that have trees or structures placed directly as children
                List<Transform> allChildren = new List<Transform>();
                foreach (Transform child in parent.transform) allChildren.Add(child);
                
                foreach (Transform child in allChildren)
                {
                    if (child == null || child.gameObject == null) continue;
                    
                    // If the child has a tag handled by MoveObjectToContainer, move it
                    string tag = child.tag;
                    if (tag == Script.Utils.GameTags.Tree || 
                        tag == Script.Utils.GameTags.Wall || 
                        tag == Script.Utils.GameTags.Structure || 
                        tag == Script.Utils.GameTags.TownCenter ||
                        tag == Script.Utils.GameTags.Coin ||
                        tag == Script.Utils.GameTags.Statues)
                    {
                        MoveObjectToContainer(child.gameObject);
                    }
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
                
                MoveEntitiesFromChunks(); // Di chuyển entities từ chunk vào container trước khi save
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
        
        /// <summary>
        /// Gọi sau khi player đã được Instantiate (vd. InstancePlayerAndSetCamera).
        /// LoadAllContainer chạy trước khi có player nên RestoreData bị bỏ qua — bước này áp PlayerContainer.json (HP, mana, skillManager, …).
        /// </summary>
        public void ApplyPlayerSaveAfterSpawn()
        {
            LoadAndSetupPlayer();
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
                // Quét toàn bộ ISaveable ở MỌI ĐỘ SÂU trong container (bao gồm cả trong các sub-folders của Pool)
                // Chỉ lấy những đối tượng ĐANG ACTIVE (GetComponentsInChildren(false) sẽ tự bỏ qua inactive mặc định nếu tham số là false)
                var saveables = container.GetComponentsInChildren<ISaveable>(false);
                
                foreach (var saveable in saveables)
                {
                    MonoBehaviour mb = saveable as MonoBehaviour;
                    if (mb == null || mb.gameObject == null) continue;

                    // Chỉ save những object đang hoạt động (không phải pool members đang ẩn)
                    if (!mb.gameObject.activeInHierarchy) continue;
                    
                    // Skip PhysicalItem đang trong stock của seller (đã được save bởi ItemSeller)
                    var physicalItem = mb.GetComponent<Script.Game.Item.Coin.PhysicalItem>();
                    if (physicalItem != null && physicalItem.onSellerStock)
                    {
                        continue;
                    }
                    
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
                        Debug.LogError($"Error saving object {mb.name} in {container.name}: {e.Message}");
                    }
                }


                if (isDebug) Debug.Log($"Saving {container.name} with {savedObjects.Count} objects");

                // Nếu không còn object nào, vẫn ghi file rỗng để xóa data cũ (tránh object đã destroy vẫn còn trong save)
                if (savedObjects.Count == 0)
                {
                    if (isDebug) Debug.Log($"No saveable objects found in {container.name}, saving empty list to clear old data");
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

                // [POOL INITIALIZATION] Đảm bảo Pool đã được bơm đầy trước khi Load quái vật 
                if (container == enemyContainer)
                {
                    var tel = FindObjectOfType<Script.Game.Enemy.TotalEnemyList>();
                    if (tel != null)
                        tel.InitializeEnemyData();
                            
                    var ep = FindObjectOfType<Script.Game.Enemy.EnemyPool>();
                    if (ep != null)
                        ep.InitializePools();
                }

                // Load và tạo object mới
                foreach (var savedObject in savedObjects)
                {
                    if (isDebug) Debug.Log($"Attempting to load prefab with ID: {savedObject.prefabID}");
                    
                    GameObject prefab = null;
                    bool isEnemy = false;

                    // ƯU TIÊN: Lấy trực tiếp từ IEnemyDataProvider nếu có
                    var totalEnemyList = FindObjectOfType<TotalEnemyList>();
                    if (totalEnemyList != null && totalEnemyList.ReadyToUseEnemyDataList != null)
                    {
                        var enemyData = totalEnemyList.ReadyToUseEnemyDataList.Find(d => 
                            string.Equals(d.enemyName, savedObject.prefabID, StringComparison.OrdinalIgnoreCase));
                        
                        if (enemyData != null)
                        {
                            prefab = enemyData.enemyPrefab;
                            isEnemy = true;
                        }
                    }

                    // Town Center: luôn spawn prefab tier 0; tier & UI theo dữ liệu save + TownCenterBehavior.ApplyVisualsForCurrentTier
                    if (prefab == null && PrefabData.Instance != null &&
                        !string.IsNullOrEmpty(savedObject.prefabID) &&
                        savedObject.prefabID.StartsWith("town_center_", StringComparison.Ordinal))
                    {
                        prefab = PrefabData.Instance.GetTownCenterByLevel(0);
                    }

                    // Nếu không phải Enemy hoặc không tìm thấy trong list Mob, mới dùng PrefabData
                    if (prefab == null && PrefabData.Instance != null)
                    {
                        prefab = PrefabData.Instance.GetPrefabByID(savedObject.prefabID);
                    }

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
                            continue;
                        }
                    }

                    if (isDebug) Debug.Log($"Creating instance of prefab: {prefab.name}");
                    
                    GameObject instance = null;
                    
                    var enemyPool = FindObjectOfType<EnemyPool>();
                    var coinPool = FindObjectOfType<CoinPool>();
                    var spawner = Spawner.Instance != null ? Spawner.Instance : FindObjectOfType<Spawner>();

                    // LOGIC POOLING KHI LOAD: 
                    if (isEnemy && spawner != null)
                    {
                        instance = spawner.SpawnEnemyFromLoad(savedObject.prefabID, prefab, container.transform, isDebug);
                    }
                    else if (container == coinContainer && coinPool != null)
                    {
                        instance = coinPool.GetCoin();
                    }

                    // Nếu không có pool hoặc pool không trả về được, fallback về Instantiate truyền thống
                    if (instance == null)
                    {
                        instance = Instantiate(prefab, container.transform);
                        if (isDebug) Debug.Log($"[EntitySaveManager] Created NEW entity {savedObject.prefabID} (No pool match)");
                    }
                    else
                    {
                        // QUAN TRỌNG: Không được gọi SetParent ở đây với các object lấy từ Pool!
                        // Việc ép SetParent sẽ kéo object ra khỏi cấu trúc thư mục của Pool (EnemyPool_Container/Pool_xyz)
                        // khiến người xem Hierarchy tưởng lầm là object bị rớt khỏi Pool.
                        if (isDebug) Debug.Log($"[EntitySaveManager] Retrieved pooled entity {savedObject.prefabID}");
                    }

                    // Enemy: inject trước → restore save (HP, skillManager, …) → ResetEnemy sau cùng để AI không ghi đè snapshot.
                    if (isEnemy)
                    {
                        var controller = instance.GetComponent<Script.Game.Enemy.Core.EnemyAIController>();
                        if (controller != null)
                        {
                            var targetMgr = FindObjectOfType<EnemyTargetManager>();
                            Action<GameObject> returnAction = null;
                            if (enemyPool != null)
                                returnAction = enemyPool.ReturnEnemy;
                            else
                                returnAction = (go) =>
                                {
                                    if (go != null) go.SetActive(false);
                                };
                            controller.InjectDependencies(targetMgr, returnAction);
                        }
                    }

                    var saveable = instance.GetComponent<ISaveable>();
                    if (saveable != null)
                    {
                        saveable.RestoreData(savedObject.data);
                        if (isDebug) Debug.Log($"Restored data for {instance.name}");
                    }

                    if (isEnemy)
                    {
                        var controller = instance.GetComponent<Script.Game.Enemy.Core.EnemyAIController>();
                        if (controller != null)
                        {
                            GameObject homePortal = null;
                            if (Script.Manager.GamePlayManager.EnemyPortalManager.Instance != null)
                            {
                                var portalMgr = Script.Manager.GamePlayManager.EnemyPortalManager.Instance;
                                var townCenter = Script.Utility.GetTownCenter();
                                if (townCenter != null)
                                {
                                    bool isRightSide = instance.transform.position.x > townCenter.transform.position.x;
                                    homePortal = isRightSide ? portalMgr.portalR : portalMgr.portalL;
                                }
                            }
                            controller.ResetEnemy(homePortal);
                        }
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

        /// <summary>
        /// Quét toàn bộ chunkContainer và di chuyển các đối tượng quan trọng (Tree, Wall, Structure, TownCenter, v.v.)
        /// vào đúng container của chúng trước khi lưu.
        /// Điều này đảm bảo các đối tượng được sinh ra đồng thời với chunk (khi tạo thế giới mới) 
        /// sẽ được quản lý bởi hệ thống save-load entities thay vì bị mất.
        /// </summary>
        public void MoveEntitiesFromChunks()
        {
            if (chunkContainer == null)
            {
                if (isDebug) Debug.LogWarning("ChunkContainer is null!");
                return;
            }
            
            try
            {
                // Chỉ tìm các đối tượng trong chunkContainer
                Transform[] allObjectsInChunks = chunkContainer.GetComponentsInChildren<Transform>(true);
                if (allObjectsInChunks == null)
                {
                    Debug.LogWarning("Không tìm thấy đối tượng nào trong chunkContainer");
                    return;
                }
                
                foreach (Transform obj in allObjectsInChunks)
                {
                    if (obj == null || obj.gameObject == null) continue;
                    
                    string tag = obj.tag;
                    
                    // Di chuyển nếu đối tượng có một trong các tag cần quản lý
                    if (tag == Script.Utils.GameTags.Tree || 
                        tag == Script.Utils.GameTags.Wall || 
                        tag == Script.Utils.GameTags.Structure || 
                        tag == Script.Utils.GameTags.TownCenter ||
                        tag == Script.Utils.GameTags.Statues ||
                        tag == Script.Utils.GameTags.Coin ||
                        tag == "RabbitBushStatus")
                    {
                        MoveObjectToContainer(obj.gameObject);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi di chuyển entities từ chunks: {e.Message}");
            }
        }
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
                var bm = FindObjectOfType<Script.Manager.Building.BuildingManager>();
                if (bm == null || bm.currentTownCenter == null)
                {
                    Debug.LogWarning("No town center found to determine wall position");
                    return null;
                }
                
                Vector3 townCenterPos = bm.currentTownCenter.transform.position;
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
