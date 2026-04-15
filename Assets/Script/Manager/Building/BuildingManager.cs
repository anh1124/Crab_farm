using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Script.Data_Model.Status;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Manager.Building
{
    
    //đảm bảo script này có trong scene
    //đảm bảo script towncenterbehavior khi chạy sẽ tự đăng ký vào đây
    
    //script này sẽ đọc data từ json chỉ định,
    //giới hạn số lượng và cấp độ tối đa mà 1 công trình có thể xây đối với từng cấp nhà chính

    #region Helper structure
    // Enum khai báo các loại công trình trong game
    public enum BuildingType
    {
        TownCenter,
        Wall,
        HammerVendor,
        BowVendor,
        // Mở rộng thêm các công trình nếu cần
    }
    
    // Lớp lưu thông tin số lượng công trình tối đa có thể xây ở một cấp
    [Serializable]
    public class StructureInfo
    {
        public BuildingType buildingType;
        public int maxQuantity;
        public int maxTier;//tier tối đa của công trình (nếu công trình có thể nâng cấp)
    }
    
    // Lớp lưu danh sách cấu hình công trình cho một cấp của TownCenter
    [Serializable]
    public class TownCenterBuildingLimits
    {
        public int townCenterLevel;
        public List<StructureInfo> buildAvailableStructuresList;
    }

    #endregion
    [RequireComponent(typeof(BuildingRegistry))]
    public class BuildingManager : MonoBehaviour, IBuildingProvider
    {
        
        [Header("Config File Settings")]
        [Tooltip("Tên file config trong thư mục Resources/DataConfig/Structure")]
        public string configFileName = "TownCenterBuildingLimits";

        [Header("Town Center Build Configs")]
        [SerializeField]
        //danh sách config được load từ json ra
        private List<TownCenterBuildingLimits> townCenterConfigs = new List<TownCenterBuildingLimits>();


        // Public property to access townCenterConfigs from other classes
        public List<TownCenterBuildingLimits> TownCenterConfigs => townCenterConfigs;

        
        [Header("Available Structures (Debug)")]
        [SerializeField]
        [Header("Danh sách công trình có thể xây hiện tại")]
        private List<StructureInfo> availableStructures = new List<StructureInfo>();
        
        public GameObject currentTownCenter { get; set; }
       
        //singleton
        private void Awake()
        {
            LoadTownCenterConfigsFromJson();
        }
        
        private void Start()
        {
            // Cập nhật danh sách công trình khả dụng ngay lập tức nếu TownCenter đã tồn tại
            // Nếu TownCenter đã tồn tại (do thứ tự Start)
            UpdateAvailableBuildings();
        } // Đọc dữ liệu từ JSON
        
        //load config mới theo cấp towncenter hiện tại
        private void LoadTownCenterConfigsFromJson()
        {
            try 
            {
                // nạp từ đường dẫn mới: Resources/DataConfig/Structure/[configFileName]
                string fullPath = $"DataConfig/Structure/{configFileName}";
                TextAsset jsonTextAsset = Resources.Load<TextAsset>(fullPath);
                
                if (jsonTextAsset != null)
                {
                    // parse JSON trực tiếp để xử lý khi tên thuộc tính không khớp
                    var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonTextAsset.text);
                    if (jsonObject != null && jsonObject.ContainsKey("townCenterConfigs"))
                    {
                        var configsArray = JsonConvert.DeserializeObject<List<object>>(jsonObject["townCenterConfigs"].ToString());
                        townCenterConfigs = new List<TownCenterBuildingLimits>();

                        foreach (var configObj in configsArray)
                        {
                            var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(configObj.ToString());
                            int level = Convert.ToInt32(configDict["townCenterLevel"]);

                            var config = new TownCenterBuildingLimits
                            {
                                townCenterLevel = level,
                                buildAvailableStructuresList = new List<StructureInfo>()
                            };
                            
                            // tìm key "availableStructures" trong JSON (tên đúng từ TownCenterBuildingLimits.json)
                            string structureKey = configDict.ContainsKey("availableStructures") ? "availableStructures" :
                                                  configDict.ContainsKey("structures") ? "structures" : 
                                                  configDict.ContainsKey("build_Available_Structures_List") ? "build_Available_Structures_List" : null;
                            
                            if (structureKey != null && configDict.ContainsKey(structureKey))
                            {
                                var structures = JsonConvert.DeserializeObject<List<object>>(configDict[structureKey].ToString());
                                foreach (var structObj in structures)
                                {
                                    var structDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(structObj.ToString());
                                    var structInfo = new StructureInfo
                                    {
                                        buildingType = Enum.Parse<BuildingType>(structDict["buildingType"].ToString()),
                                        maxQuantity = Convert.ToInt32(structDict["maxQuantity"])
                                    };
                                    
                                    // kiểm tra cả "isMaxTier" và "maxLevel" để hỗ trợ các định dạng JSON khác nhau
                                    if (structDict.ContainsKey("isMaxTier"))
                                    {
                                        structInfo.maxTier = Convert.ToInt32(structDict["isMaxTier"]);
                                    }
                                    else if (structDict.ContainsKey("maxLevel"))
                                    {
                                        structInfo.maxTier = Convert.ToInt32(structDict["maxLevel"]);
                                    }
                                    else if (structDict.ContainsKey("maxTier"))
                                    {
                                        structInfo.maxTier = Convert.ToInt32(structDict["maxTier"]);
                                    }
                                    // mặc định maxTier là 0 nếu không được chỉ định
                                    
                                    config.buildAvailableStructuresList.Add(structInfo);
                                }
                            }
                            
                            townCenterConfigs.Add(config);
                        }
                        
                    }
                   
                }
                else
                {
                    // sử dụng đường dẫn trực tiếp nếu không tìm thấy trong Resources
                    Debug.LogError($"Không tìm thấy file cấu hình {configFileName} trong Resources/DataConfig. Sử dụng cấu hình dự phòng.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading TownCenter configs: {e.Message}\n{e.StackTrace}");
            }
        }
        public void UpdateAvailableBuildings()
        {
            // xóa danh sách hiện tại
            availableStructures.Clear();
            
            // kiểm tra currentTownCenter có null không
            if (currentTownCenter == null)
            {
                Debug.LogWarning("[BuildingManager] currentTownCenter is null. Chưa có TownCenter được xây dựng.");
                return;
            }
            
            if (townCenterConfigs == null || townCenterConfigs.Count == 0)
            {
                Debug.LogError("Không có cấu hình TownCenter để cập nhật.");
                return;
            }

            // tìm config cho tier hiện tại của TownCenter
            TownCenterBuildingLimits currentConfig = null;
            int currentTownCenterLevel = currentTownCenter.GetComponent<TownCenterStatus>().tier;

            foreach (var config in townCenterConfigs)
            {
                if (config.townCenterLevel == currentTownCenterLevel)
                {
                    currentConfig = config;
                    break;
                }
            }
            
            if (currentConfig != null)
            {
                // thêm tất cả công trình khả dụng cho tier này
                if (currentConfig.buildAvailableStructuresList != null)
                {
                    availableStructures.AddRange(currentConfig.buildAvailableStructuresList);
                    Debug.Log($"[UpdateAvailableBuildings] Cập nhật build rule cho cấp TownCenter {currentTownCenterLevel}:");
                    foreach (var structureInfo in availableStructures)
                    {
                        int builtCount = CountBuiltBuildings(structureInfo.buildingType);
                        Debug.Log($"- {structureInfo.buildingType}: {builtCount}/{structureInfo.maxQuantity}");
                    }
                }
                else
                {
                    Debug.LogError($"Không có công trình khả dụng được định nghĩa cho town center tier {currentTownCenterLevel}");
                }
                    Debug.Log("BuildingManager đã thông báo BuildMenu cập nhật danh sách công trình khả dụng");
            }
            else
            {
                Debug.LogWarning($"Buidldingmanager error, currentConfig = null");
            }
        }

        //sau này làm manager quản lý tường rồi thì phải lấy data từ nó ,không dùng cái đếm tag
        public int CountBuiltBuildings(BuildingType type)
        {
            try
            {
                string tag = type.ToString();
                GameObject[] foundObjects = GameObject.FindGameObjectsWithTag(tag);
                
                if (foundObjects == null) return 0;
                
                int count = 0;
                foreach (var obj in foundObjects)
                {
                    bool counted = false;

                    switch (type)
                    {
                        case BuildingType.Wall:
                            if (obj.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>() != null)
                            {
                                counted = true;
                            }
                            break;
                            
                        case BuildingType.TownCenter:
                            if (obj.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>() != null)
                            {
                                counted = true;
                            }
                            break;

                        default:
                            // Fallback chung cho các công trình chưa được định nghĩa rõ component
                            counted = true; 
                            break;
                    }

                    if (counted)
                    {
                        count++;
                    }
                }
                
                return count;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error counting buildings of type {type}: {e.Message}");
                return 0;
            }
        }
        
        // hàm này nên sử dụng trong trigger
        //ví dụ player trigger với wall,hàm này sẽ được wall gọi.
        //nếu hàm này trả về true thì sẽ hiện coinslot cho update,còn không thì tắt coinslot đi
        public bool CanBuildingBeUpgraded(BuildingType buildingType, int currentTier)
        {
            // TỰ ĐỘNG CẬP NHẬT nếu danh sách đang trống nhưng đã có TownCenter
            if ((availableStructures == null || availableStructures.Count == 0) && currentTownCenter != null)
            {
                UpdateAvailableBuildings();
            }

            // kiểm tra đã load được config chưa
            if (townCenterConfigs == null || townCenterConfigs.Count == 0 || availableStructures == null || availableStructures.Count == 0)
            {
                Debug.LogWarning("No town center configurations available for upgrade check");
                return false;
            }

            // kiểm tra struct này đã được khai báo trong structureInfo chưa
            StructureInfo structureInfo = availableStructures.Find(s => s.buildingType == buildingType);
            if (structureInfo == null)
            {
                int tier = currentTownCenter != null ? currentTownCenter.GetComponent<TownCenterStatus>().tier : 0;
                Debug.LogWarning($"Building type {buildingType} is not available at town center tier {tier}");
                return false;
            }

            // kiểm tra xem có dữ liệu về tier tối đa được phép nâng cấp không
            if (structureInfo.maxTier <= 0)
            {
                Debug.Log($"Building type {buildingType} doesn't have a valid isMaxTier ({structureInfo.maxTier})");
                return false;
            }

            // kiểm tra xem tier tiếp theo có vượt quá tier tối đa được phép không
            int nextTier = currentTier + 1;
            if (nextTier > structureInfo.maxTier)
            {
                int townCenterTier = currentTownCenter != null ? currentTownCenter.GetComponent<TownCenterStatus>().tier : 0;
                //Debug.Log($"Building type {buildingType} at tier {currentTier} cannot be upgraded. Max tier is {structureInfo.maxTier}, town center tier is {townCenterTier}");
                return false;
            }

           // Debug.Log($"Building type {buildingType} at tier {currentTier} can be upgraded to tier {nextTier}, max allowed tier is {structureInfo.maxTier}");
            return true;
        }
        
        // lấy tier tối đa được phép của một loại công trình cụ thể tại cấp town center hiện tại
        
        //hàm này trả về cấp công trình cao nhất có thể nâng cấp của buildingType truyền vào
        //hiện tại chỉ dùng cho wallbehavior debug
        public int GetMaxAllowedTier(BuildingType buildingType)
        {
            // TỰ ĐỘNG CẬP NHẬT nếu danh sách đang trống nhưng đã có TownCenter
            if ((availableStructures == null || availableStructures.Count == 0) && currentTownCenter != null)
            {
                UpdateAvailableBuildings();
            }

            if (townCenterConfigs == null || availableStructures == null || availableStructures.Count == 0)
            {
                return 0;
            }
            
            StructureInfo structureInfo = availableStructures.Find(s => s.buildingType == buildingType);
            
            // nếu không tìm thấy công trình trong danh sách khả dụng, trả về 0
            if (structureInfo == null)
            {
                int tier = currentTownCenter != null ? currentTownCenter.GetComponent<TownCenterStatus>().tier : 0;
                Debug.LogWarning($"Loại công trình {buildingType} không được tìm thấy trong danh sách khả dụng cho town center tier {tier}");
                return 0;
            }
            
            int townCenterTier = currentTownCenter != null ? currentTownCenter.GetComponent<TownCenterStatus>().tier : 0;
            //Debug.Log($"Town center tier {townCenterTier} cho phép {buildingType} up to tier {structureInfo.maxTier}");
            
            return structureInfo.maxTier;
        }
        
        
        // cập nhật tất cả công trình trong scene với giới hạn nâng cấp mới
        private void UpdateAllStructureLimits()
        {
            Debug.Log("BuildingManager đã gọi UpdateAllStructureLimits?? hàm này chưa có gì cả");
        }
    }
}
