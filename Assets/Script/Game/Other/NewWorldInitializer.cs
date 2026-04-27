using UnityEngine;
using System.Collections;
using Script.Manager;
using Script.Manager.WorldManager;
using Script.Game.Mobs.MobsManager;
using Script.Save_Controller;
using Script.Manager.SaveManager;

namespace Script.Game.Other
{
    /// <summary>
    /// Component chuyên biệt xử lý việc thiết lập các object phụ khi phát hiện một thế giới mới (New World).
    /// Các công việc bao gồm: Mở rộng map (Add Chunk), gọi mây tiền (CoinCloud), 
    /// dọn dẹp cây vào container và thực hiện lưu bản đồ lần đầu.
    /// </summary>
    public class NewWorldInitializer : MonoBehaviour
    {
        public static NewWorldInitializer Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Bắt đầu chu trình thiết lập thế giới mới.
        /// </summary>
        public void ExecuteSetup()
        {
            Debug.Log("[NewWorldInitializer] Bắt đầu chu trình thiết lập cho thế giới mới...");
            
            // 1. Gọi mây tiền
            SpawnCoinCloud();

            // 2. Thiết lập Vagrant ban đầu
            SpawnInitialVagrant();

            // 3. Mở rộng thêm Chunk (nếu cần - theo logic người dùng yêu cầu 'add chunk')
            ExpandWorld();

            // 4. Di chuyển entities từ chunks vào container (dọn dẹp cây, đá...)
            MoveEntitiesToContainers();

            // 5. Lưu lại sau 1 giây
            StartCoroutine(DelayedInitialSave());
        }

        private void SpawnCoinCloud()
        {
            var pd = Script.Data_Model.PrefabData.Instance;
            if (pd != null && pd.coinCloudPrefab != null)
            {
                float randomX = Random.Range(-30f, 30f);
                Vector3 startPos = new Vector3(randomX, 25f, 0f);
                
                GameObject coinCloud = Instantiate(pd.coinCloudPrefab, startPos, Quaternion.identity);
                if (coinCloud != null)
                {
                    var cloudController = coinCloud.GetComponent<Script.Game.CoinCloud.CoinCloudController>();
                    if (cloudController != null)
                    {
                        cloudController.spawn = startPos;
                        cloudController.useCurrentPositionAsSpawn = true;
                        cloudController.ActivateWithTownCenterSearch();
                    }
                    Debug.Log("[NewWorldInitializer] CoinCloud spawned and activated.");
                }
            }
        }

        private void SpawnInitialVagrant()
        {
            // Sử dụng fully qualified name để tránh lỗi CS0246
            var vagrantManager = FindObjectOfType<Script.Game.Mobs.MobsManager.VagrantManager>();
            if (vagrantManager != null)
            {
                vagrantManager.SpawnInitialVagrantForNewGame();
                Debug.Log("[NewWorldInitializer] Initial Vagrant spawned.");
            }
            else
            {
                Debug.LogWarning("[NewWorldInitializer] VagrantManager not found in scene!");
            }
        }

        private void ExpandWorld()
        {
            var chunkManager = FindObjectOfType<ChunkManager>();
            if (chunkManager != null)
            {
                // Mở rộng thêm 1 chunk mỗi bên
                chunkManager.AddChunk(true);
                chunkManager.AddChunk(false);
                Debug.Log("[NewWorldInitializer] World expanded with extra chunks.");
            }
        }

        private void MoveEntitiesToContainers()
        {
            if (Script.Manager.EntitySaveManager.Instance != null)
            {
                Script.Manager.EntitySaveManager.Instance.MoveEntitiesFromChunks();
                Debug.Log("[NewWorldInitializer] Entities moved from chunks to containers.");
            }
        }

        private IEnumerator DelayedInitialSave()
        {
            yield return new WaitForSeconds(1f);
            if (Script.Save_Controller.WorldFileManager.Instance != null)
            {
                Debug.Log("[NewWorldInitializer] Performing delayed initial save for new world.");
                Script.Save_Controller.WorldFileManager.Instance.SaveCurrentMap();
            }
        }
    }
}
