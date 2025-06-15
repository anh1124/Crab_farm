using UnityEngine;
using Script.Game.Other;
using Script.Data_Model;
using Script.Game.Mobs.MobsManager;
using Script.Data_Model.Status;
using Script.Game.Mobs.Subjects;
using Script.Game.Enemy.Interfaces;
using Script.Game.Structures.Wall_And_Stump;

namespace Script
{
    public static class Utility 
    {
        public static bool isDebug = true;
        
        public static bool IsAlly(GameObject obj)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.allyTags == null)
            {
                Debug.LogWarning("PrefabData.Instance hoặc allyTags là null. Không thể kiểm tra đồng minh.");
                return false; // Mặc định không phải đồng minh nếu dữ liệu thiếu
            }
            return PrefabData.Instance.allyTags.Contains(obj.tag);
        }

        /// <summary>
        /// Xử lý việc gây sát thương cho các đối tượng bị va chạm trong một cuộc tấn công.
        /// </summary>
        /// <param name="instigator">Đối tượng gây ra sát thương (ví dụ: Player).</param>
        /// <param name="hits">Mảng các Collider2D bị va chạm.</param>
        /// <param name="count">Số lượng đối tượng va chạm hợp lệ trong mảng hits.</param>
        /// <param name="damageAmount">Lượng sát thương gây ra.</param>
        /// <param name="allowDamageAlly">True nếu cho phép gây sát thương lên đồng minh.</param>
        public static void ProcessAttackHits(GameObject instigator, Collider2D[] hits, int count, int damageAmount, bool allowDamageAlly)
        {
            Debug.Log($"[Utility.ProcessAttackHits] {instigator.name} bắt đầu xử lý {count} đối tượng va chạm. Damage: {damageAmount}. Damage Ally: {allowDamageAlly}.");

            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null) // Kiểm tra nếu phần tử collider trong mảng là null
                {
                    Debug.LogWarning($"[Utility.ProcessAttackHits] Collider tại vị trí {i} trong mảng hits là null. Bỏ qua.");
                    continue; // Bỏ qua phần tử này và tiếp tục vòng lặp
                }

                GameObject hitObject = hits[i].gameObject;
                if (hitObject == null) // Kiểm tra nếu GameObject của collider là null
                {
                    Debug.LogWarning($"[Utility.ProcessAttackHits] GameObject của collider {hits[i].name} tại vị trí {i} là null. Bỏ qua.");
                    continue; // Bỏ qua phần tử này
                }

                if (hitObject == instigator) // Mới: Bỏ qua nếu đối tượng va chạm chính là đối tượng gây tấn công
                {
                    Debug.Log($"[Utility.ProcessAttackHits] Bỏ qua đối tượng gây tấn công {hitObject.name} (Tag: {hitObject.tag}).");
                    continue;
                }

                IDamageable target = hitObject.GetComponent<IDamageable>();

                if (target != null) // Nếu đối tượng có thể nhận sát thương
                {
                    bool isTargetAlly = IsAlly(hitObject);

                    if (!allowDamageAlly && isTargetAlly) // Nếu không cho phép tấn công đồng minh VÀ đối tượng là đồng minh
                    {
                        Debug.Log($"[Utility.ProcessAttackHits] Bỏ qua đồng minh {hitObject.name} (Tag: {hitObject.tag}).");
                        continue; // Bỏ qua đối tượng này
                    }

                    // Kiểm tra tier của Wall
                    if (hitObject.CompareTag("Wall"))
                    {
                        Wall wall = hitObject.GetComponent<Wall>();
                        if (wall != null)
                        {
                            if (wall.tier == 0)
                            {
                                Debug.Log($"[Utility.ProcessAttackHits] Wall {wall.name} (Tier 0) không nhận sát thương.");
                                continue; // Wall Tier 0 không nhận sát thương
                            }
                            else
                            {
                                Debug.Log($"[Utility.ProcessAttackHits] Wall {wall.name} (Tier {wall.tier}) nhận sát thương {damageAmount} từ {instigator.name}.");
                                target.TakeDamage((int)damageAmount, instigator);
                            }
                        }
                        else
                        {
                             Debug.LogWarning($"[Utility.ProcessAttackHits] Đối tượng {hitObject.name} có tag 'Wall' nhưng không có script Wall. Bỏ qua.");
                        }
                    }
                    else // Đối tượng không phải Wall hoặc Wall không phải Tier 0
                    {
                        Debug.Log($"[Utility.ProcessAttackHits] {hitObject.name} (Tag: {hitObject.tag}) nhận sát thương {damageAmount} từ {instigator.name}.");
                        target.TakeDamage((int)damageAmount, instigator);
                    }
                }
                else
                {
                    Debug.Log($"[Utility.ProcessAttackHits] Đối tượng {hitObject.name} (Tag: {hitObject.tag}) không implement IDamageable. Bỏ qua.");
                }
            }
        }

        /// <summary>
        /// In ra thông tin debug về các GameObject đã va chạm.
        /// </summary>
        /// <param name="colliderName">Tên của collider đã thực hiện va chạm.</param>
        /// <param name="hits">Mảng các Collider2D bị va chạm.</param>
        /// <param name="count">Số lượng đối tượng va chạm hợp lệ trong mảng hits.</param>
        public static void LogCollidedObjects(string colliderName, Collider2D[] hits, int count)
        {
            if (isDebug)
            {
                string logMessage = @$"[Utility.LogCollidedObjects] Collider ""{colliderName}"" đã va chạm với {count} đối tượng: ";
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (hits[i] != null)
                        {
                            logMessage += $"{hits[i].gameObject.name} (Tag: {hits[i].gameObject.tag}), ";
                        }
                    }
                    Debug.Log(logMessage.TrimEnd(',', ' '));
                }
                else
                {
                    Debug.Log(@$"[Utility.LogCollidedObjects] Collider ""{colliderName}"" không va chạm với đối tượng nào.");
                }
            }
        }

        public static void RotateObjectTowardsTownCenter(GameObject obj, Vector3 referencePos, Vector3 townCenterPos)
        {
            Vector3 scale = obj.transform.localScale;
            
            // Calculate direction from reference position to town center
            Vector3 directionFromTownCenter = referencePos - townCenterPos;
            
            // We want objects to face RIGHT by default (x positive)
            // If object is to the left of town center, face right (scale.x positive)
            // If object is to the right of town center, face left (scale.x negative)
            if (directionFromTownCenter.x < 0)
            {
                // Object is to the LEFT of town center - should face RIGHT
                scale.x =-1f * Mathf.Abs(scale.x); // Ensure positive scale (face right)
            }
            else
            {
                // Object is to the RIGHT of town center - should face LEFT
                scale.x =  Mathf.Abs(scale.x); // Ensure negative scale (face left)
            }
            
            if (isDebug)
            {
                Debug.Log($"Utility.RotateObjectTowardsTownCenter: Object {obj.name} at pos {referencePos}, town center at {townCenterPos}, direction {directionFromTownCenter}, scale.x={scale.x}");
            }
            
            obj.transform.localScale = scale;
            
            // Ensure Z rotation is always 0
            obj.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// Gets a random X position between town center and nearest walls (plaza area)
        /// </summary>
        public static float GetRandomPlazaPos()
        {
            if (BuildingManager.instance == null || BuildingManager.instance.CurrentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return 0f;
            }

            float townCenterX = BuildingManager.instance.CurrentTownCenter.transform.position.x;
            
            // Find nearest walls on both sides
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            float nearestLeftWall = float.MinValue;
            float nearestRightWall = float.MaxValue;

            foreach (GameObject wall in walls)
            {
                float wallX = wall.transform.position.x;
                if (wallX < townCenterX && wallX > nearestLeftWall)
                {
                    nearestLeftWall = wallX;
                }
                if (wallX > townCenterX && wallX < nearestRightWall)
                {
                    nearestRightWall = wallX;
                }
            }

            // If no walls found, use a default range
            if (nearestLeftWall == float.MinValue) nearestLeftWall = townCenterX - 5f;
            if (nearestRightWall == float.MaxValue) nearestRightWall = townCenterX + 5f;

            return Random.Range(nearestLeftWall, nearestRightWall);
        }

        /// <summary>
        /// Gets a random X position between farthest walls (kingdom safe area)
        /// </summary>
        public static float GetRandomKingdomSafePos()
        {
            if (BuildingManager.instance == null || BuildingManager.instance.CurrentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return 0f;
            }

            float townCenterX = BuildingManager.instance.CurrentTownCenter.transform.position.x;
            
            // Find farthest walls on both sides
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            float farthestLeftWall = townCenterX;
            float farthestRightWall = townCenterX;

            foreach (GameObject wall in walls)
            {
                float wallX = wall.transform.position.x;
                if (wallX < farthestLeftWall)
                {
                    farthestLeftWall = wallX;
                }
                if (wallX > farthestRightWall)
                {
                    farthestRightWall = wallX;
                }
            }

            // If no walls found, use a default range
            if (farthestLeftWall == townCenterX) farthestLeftWall = townCenterX - 15f;
            if (farthestRightWall == townCenterX) farthestRightWall = townCenterX + 15f;

            return Random.Range(farthestLeftWall, farthestRightWall);
        }

        /// <summary>
        /// Trả về vị trí của bức tường trái hoặc phải gần nhất theo Vector2.
        /// </summary>
        /// <param name="getLeftWall">True để lấy vị trí tường trái, False để lấy vị trí tường phải.</param>
        /// <returns>Vị trí Vector2 của bức tường (X của tường, Y của trung tâm thị trấn).</returns>
        public static Vector2 GetWallPosition(bool getLeftWall)
        {
            if (BuildingManager.instance == null || BuildingManager.instance.CurrentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return Vector2.zero; // Hoặc một giá trị mặc định phù hợp
            }

            float townCenterX = BuildingManager.instance.CurrentTownCenter.transform.position.x;
            float townCenterY = BuildingManager.instance.CurrentTownCenter.transform.position.y;
            
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            float wallX = getLeftWall ? float.MinValue : float.MaxValue;
            float defaultOffset = getLeftWall ? -15f : 15f; // Default offset if no walls are found

            foreach (GameObject wall in walls)
            {
                float currentWallX = wall.transform.position.x;
                if (getLeftWall)
                {
                    if (currentWallX < townCenterX && currentWallX > wallX)
                    {
                        wallX = currentWallX;
                    }
                }
                else // getRightWall
                {
                    if (currentWallX > townCenterX && currentWallX < wallX)
                    {
                        wallX = currentWallX;
                    }
                }
            }

            // If no wall found, use a default position relative to the town center
            if ((getLeftWall && wallX == float.MinValue) || (!getLeftWall && wallX == float.MaxValue))
            {
                wallX = townCenterX + defaultOffset;
            }

            return new Vector2(wallX, townCenterY);
        }

        /// <summary>
        /// Di chuyển một GameObject đến vị trí mục tiêu với tốc độ cho trước (chỉ di chuyển theo trục X)
        /// </summary>
        /// <param name="objectToMove">GameObject cần di chuyển</param>
        /// <param name="target">Vị trí đích</param>
        /// <param name="speed">Tốc độ di chuyển</param>
        /// <returns>True nếu đã đến đích (khoảng cách < 0.1f), False nếu chưa</returns>
        public static bool MoveTowardsTarget(GameObject objectToMove, Vector3 target, float speed)
        {
            FlipDirection(objectToMove, target);
            
            Vector2 currentPosition = objectToMove.transform.position;
            // Chỉ di chuyển theo trục X, giữ nguyên Y và Z
            float newX = Mathf.MoveTowards(currentPosition.x, target.x, speed * Time.deltaTime);
            objectToMove.transform.position = new Vector3(newX, objectToMove.transform.position.y, objectToMove.transform.position.z);

            // Kiểm tra xem đã đến đích chưa (chỉ xét khoảng cách theo X)
            return Mathf.Abs(objectToMove.transform.position.x - target.x) < 0.1f;
        }

        /// <summary>
        /// Xoay hướng GameObject dựa vào vị trí mục tiêu
        /// </summary>
        /// <param name="objectToFlip">GameObject cần xoay hướng</param>
        /// <param name="target">Vị trí mục tiêu</param>
        public static void FlipDirection(GameObject objectToFlip, Vector3 target)
        {
            Vector3 currentScale = objectToFlip.transform.localScale;
            objectToFlip.transform.localScale = new Vector3(
                target.x < objectToFlip.transform.position.x ? -1 : 1,
                currentScale.y,
                currentScale.z
            );
        }

        public static void TransformToVillager(GameObject archerObject, int currentCoins)
        {
            if (archerObject == null || !archerObject.activeInHierarchy) return;

            if (PrefabData.Instance != null && PrefabData.Instance.village != null)
            {
                // Rơi cung
                if (PrefabData.Instance.Bow != null)
                {
                    GameObject.Instantiate(PrefabData.Instance.Bow, archerObject.transform.position, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("Bow prefab is missing in PrefabData. Cannot drop bow.");
                }

                Vector3 position = archerObject.transform.position;
                Quaternion rotation = archerObject.transform.rotation;
                Vector3 scale = archerObject.transform.localScale;
                
                // Dừng tất cả coroutines trước khi hủy
                // Lưu ý: Coroutine chỉ có thể dừng từ MonoBehaviour. Hàm static không thể StopAllCoroutines trực tiếp.
                // Nếu có coroutines đang chạy trên ArcherController, chúng sẽ tự dừng khi GameObject bị Destroy.

                if (Spawner.Instance != null)
                {
                    GameObject villager = Spawner.Instance.SpawnMob(position, PrefabData.Instance.village);
                    if (villager != null)
                    {
                        villager.transform.rotation = rotation;
                        villager.transform.localScale = scale;
                        VillagerController villagerController = villager.GetComponent<VillagerController>();
                        if (villagerController != null)
                        {
                            // Truyền số xu còn lại bằng cách truy cập VillageStatus
                            VillageStatus villageStatus = villager.GetComponent<VillageStatus>();
                            if (villageStatus != null)
                            {
                                villageStatus.CurrentCoins = currentCoins;
                            }
                            else
                            {
                                Debug.LogWarning("[Utility] Villager does not have VillageStatus component.");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("[Utility] Spawner instance not found! Cannot spawn Villager.");
                    // Fallback: Nếu Spawner không tìm thấy, tạo trực tiếp nhưng vẫn log lỗi
                    GameObject villagerInstance = GameObject.Instantiate(PrefabData.Instance.village, archerObject.transform.position, Quaternion.identity);
                    VillagerController villagerController = villagerInstance.GetComponent<VillagerController>();
                    if (villagerController != null)
                    {
                        VillageStatus villageStatus = villagerInstance.GetComponent<VillageStatus>();
                        if (villageStatus != null)
                        {
                            villageStatus.CurrentCoins = currentCoins;
                        }
                        else
                        {
                            Debug.LogWarning("[Utility] Villager does not have VillageStatus component.");
                        }
                    }
                }
                
                GameObject.Destroy(archerObject);
            }
            else
            {
                Debug.LogError("[Utility] Villager prefab not found in PrefabData! Cannot transform Archer to Villager.");
            }
        }
        public static GameObject GetTownCenter()
        {
            if (BuildingManager.instance == null || BuildingManager.instance.CurrentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return null;
            }
            return BuildingManager.instance.CurrentTownCenter.gameObject; 
        }
    }
}
