using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Data_Model;
using Script.Game.Other.Nature;
using UnityEngine;

namespace Script
{
    public class SetUpContainer : MonoBehaviour
    {
        /*
     * todo: làm tiếp những cái container khác
     */

        public static SetUpContainer Instance { get; private set; }
        public GameObject structureContainer; // spawm các gameobject thuộc lớp Structure ở đây
        public GameObject coinContainer; //spawm coin trong này
        public GameObject natureContainer; //spawm cây trong này

        public GameObject chunkContainer; //gameobject chứa các chunk được instance ra

        //portal
        //ally
        //
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
            // Gắn StructureContainer bằng tag hoặc tạo mới nếu không tìm thấy
            if (structureContainer == null)
            {
                structureContainer = GameObject.FindGameObjectWithTag("StructureContainer");
                if (structureContainer == null)
                {
                    structureContainer = new GameObject("StructureContainer");
                    structureContainer.tag = "StructureContainer";
                }
            }

            // Gắn CoinContainer bằng tag hoặc tạo mới nếu không tìm thấy
            if (coinContainer == null)
            {
                coinContainer = GameObject.FindGameObjectWithTag("CoinContainer");
                if (coinContainer == null)
                {
                    coinContainer = new GameObject("CoinContainer");
                    coinContainer.tag = "CoinContainer";
                }
            }

            // Gắn NatureContainer bằng tag hoặc tạo mới nếu không tìm thấy
            if (natureContainer == null)
            {
                natureContainer = GameObject.FindGameObjectWithTag("NatureContainer");
                if (natureContainer == null)
                {
                    natureContainer = new GameObject("NatureContainer");
                    natureContainer.tag = "NatureContainer";
                }
            }

            // Gắn ChunkContainer bằng tag hoặc tạo mới nếu không tìm thấy
            if (chunkContainer == null)
            {
                chunkContainer = GameObject.FindGameObjectWithTag("ChunkContainer");
                if (chunkContainer == null)
                {
                    chunkContainer = new GameObject("ChunkContainer");
                    chunkContainer.tag = "ChunkContainer";
                }
            }
        }

        public List<GameObject> GetObjectsFromContainer(GameObject container)
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

        //chuyển gameobject input vào container theo tag của nó
        public void MoveObjectToContainer(GameObject obj)
        {
            if (obj.CompareTag("Tree"))
            {
                obj.transform.SetParent(natureContainer.transform, true);
            }
            else if (obj.CompareTag("Structure"))
            {
                obj.transform.SetParent(structureContainer.transform, true);
            }
            else if (obj.CompareTag("Coin"))
            {
                obj.transform.SetParent(coinContainer.transform, true);
            }
        }

        //chuyển gameobject container của gameobject input vào theo tag của nó
        public void MoveChildrenToContainers(GameObject parent)
        {
            Transform containerTransform = parent.transform.Find("Container");
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
            SaveTrees();
            SaveStructures();
        }

        public void LoadAllContainer()
        {
            LoadTrees();
            LoadStructures();
        }

        /*
     *chú ý:tree được khởi tạo khi tạo các chunk nên chỉ được load,save trong chunkmanager hoặc hàm save trong game
     */
        #region Tree
        public void SaveTrees()
        {
            // Kiểm tra container cây
            if (natureContainer == null)
            {
                Debug.LogError("NatureContainer không được thiết lập.");
                return;
            }

            // Lấy danh sách cây từ NatureContainer
            List<GameObject> treeObjects = GetObjectsFromContainer(natureContainer);
            List<NatureTree.SaveTree> saveTrees = new List<NatureTree.SaveTree>();

            // Duyệt qua từng cây và thêm vào danh sách saveTrees nếu tag là "Tree"
            foreach (GameObject treeObject in treeObjects)
            {
                if (treeObject.CompareTag("Tree")) // Kiểm tra tag là "Tree"
                {
                    NatureTree treeComponent = treeObject.GetComponent<NatureTree>();
                    if (treeComponent != null)
                    {
                        var position = treeObject.transform.position;
                        NatureTree.SaveTree saveTree = new NatureTree.SaveTree(
                            treeComponent.Type,
                            treeComponent.treeChopCost,
                            treeComponent.treeHp,
                            treeComponent.isReadyToChop,
                            position.x,
                            position.y
                        );
                        saveTrees.Add(saveTree);
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
                List<NatureTree.SaveTree> savedTrees = JsonConvert.DeserializeObject<List<NatureTree.SaveTree>>(json);

                // Duyệt qua mỗi SaveTree và khởi tạo cây
                foreach (NatureTree.SaveTree saveTree in savedTrees)
                {
                    // Lấy prefab cây dựa vào Type
                    if (saveTree.Type >= 0 && saveTree.Type < PrefabData.Instance.treePrefabs.Count)
                    {
                        GameObject treePrefab = PrefabData.Instance.treePrefabs[saveTree.Type];

                        // Khởi tạo cây ở vị trí xác định
                        Vector3 position = new Vector3(saveTree.positionX, 0, 0); // Đặt Y và Z nếu cần
                        GameObject treeObject = Instantiate(treePrefab, position, Quaternion.identity,
                            natureContainer.transform);

                        // Gán các thuộc tính cho cây
                        NatureTree treeComponent = treeObject.GetComponent<NatureTree>();
                        if (treeComponent != null)
                        {
                            treeComponent.Type = saveTree.Type;
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
        #endregion

        #region Natures
        /*
         *tree cũng được lưu trong này 
         */
        public void SaveNatures()
        {
            SaveTrees();
            SaveRabbitBush();
        }

        public void LoadNatures()
        {
            LoadTrees();
            LoadRabbitBush();
        }
        public void SaveRabbitBush()
        {
            if (natureContainer == null)
            {
                Debug.LogError("NatureContainer không được thiết lập.");
                return;
            }

            List<GameObject> rabbitBushObjects = GetObjectsFromContainer(natureContainer);
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