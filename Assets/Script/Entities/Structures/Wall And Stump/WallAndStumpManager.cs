using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Script.Game.Structures.Wall_And_Stump
{
    public class WallAndStumpManager : MonoBehaviour
    {
        //TODO: kiểm tra xem script này có trong GM chưa
        public static WallAndStumpManager Instance { get; private set; }
        public GameObject townCenter;
        public List<GameObject> leftWalls = new List<GameObject>();
        public List<GameObject> rightWalls = new List<GameObject>();
        public List<GameObject> stumps = new List<GameObject>();

        private void Awake()
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

        #region Wall Management
        public void RegisterWall(GameObject wall)
        {
            if (wall == null) return;

            float wallX = wall.transform.position.x;
            townCenter = Utility.GetTownCenter();

            if (townCenter == null)
            {
                Debug.LogError("[WallAndStumpManager] Town Center not found, cannot register wall.");
                return;
            }

            float townCenterX = townCenter.transform.position.x;

            if (wallX < townCenterX)
            {
                // Add to left walls and keep sorted by distance to town center (closest first)
                leftWalls.Add(wall);
                leftWalls = leftWalls.OrderByDescending(w => w.transform.position.x).ToList();
            }
            else
            {
                // Add to right walls and keep sorted by distance to town center (closest first)
                rightWalls.Add(wall);
                rightWalls = rightWalls.OrderBy(w => w.transform.position.x).ToList();
            }
            //Debug.Log($"[WallAndStumpManager] Registered wall {wall.name}. Left Walls: {leftWalls.Count}, Right Walls: {rightWalls.Count}");
        }

        public void UnregisterWall(GameObject wall)
        {
            if (wall == null) return;

            if (leftWalls.Contains(wall))
            {
                leftWalls.Remove(wall);
            }
            else if (rightWalls.Contains(wall))
            {
                rightWalls.Remove(wall);
            }
            //Debug.Log($"[WallAndStumpManager] Unregistered wall {wall.name}. Left Walls: {leftWalls.Count}, Right Walls: {rightWalls.Count}");
        }
        #endregion

        #region Stump Management
        public void RegisterStump(GameObject stump)
        {
            if (stump == null) return;
            if (!stumps.Contains(stump))
            {
                stumps.Add(stump);
                Debug.Log($"[WallAndStumpManager] Registered stump {stump.name}. Total stumps: {stumps.Count}");
            }
        }

        public void UnregisterStump(GameObject stump)
        {
            if (stump == null) return;
            if (stumps.Contains(stump))
            {
                stumps.Remove(stump);
                Debug.Log($"[WallAndStumpManager] Unregistered stump {stump.name}. Total stumps: {stumps.Count}");
            }
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Lấy bức tường xa trung tâm thị trấn nhất (bên trái hoặc bên phải).
        /// </summary>
        /// <param name="isLeft">True để lấy tường bên trái, False để lấy tường bên phải.</param>
        /// <returns>GameObject của bức tường xa nhất, hoặc null nếu không có tường.</returns>
        public GameObject GetFarthestWall(bool isLeft)
        {
            if (isLeft)
            {
                return leftWalls.Count > 0 ? leftWalls[0] : null; // Left walls are sorted descending by x, so 0 is farthest left
            }
            else
            {
                return rightWalls.Count > 0 ? rightWalls[rightWalls.Count - 1] : null; // Right walls are sorted ascending by x, so last is farthest right
            }
        }

        /// <summary>
        /// Lấy bức tường gần nhất với một vị trí cụ thể.
        /// </summary>
        /// <param name="position">Vị trí tham chiếu.</param>
        /// <returns>GameObject của bức tường gần nhất, hoặc null nếu không có tường.</returns>
        public GameObject GetNearestWall(Vector3 position)
        {
            GameObject nearestWall = null;
            float minDistance = float.MaxValue;

            foreach (var wall in leftWalls)
            {
                if (wall == null) continue;
                float distance = Vector3.Distance(wall.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestWall = wall;
                }
            }

            foreach (var wall in rightWalls)
            {
                if (wall == null) continue;
                float distance = Vector3.Distance(wall.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestWall = wall;
                }
            }

            return nearestWall;
        }

        /// <summary>
        /// Lấy bức tường gần trung tâm thị trấn nhất (bên trái hoặc bên phải).
        /// </summary>
        /// <param name="isLeft">True để lấy tường bên trái, False để lấy tường bên phải.</param>
        /// <returns>GameObject của bức tường gần nhất, hoặc null nếu không có tường.</returns>
        public GameObject GetNearestWallToTownCenter(bool isLeft)
        {
            if (isLeft)
            {
                return leftWalls.Count > 0 ? leftWalls[leftWalls.Count - 1] : null; // Left walls are sorted descending by x, so last is closest to town center
            }
            else
            {
                return rightWalls.Count > 0 ? rightWalls[0] : null; // Right walls are sorted ascending by x, so 0 is closest to town center
            }
        }

        /// <summary>
        /// Lấy bức tường ngay phía sau bức tường hiện tại trong danh sách (gần trung tâm thị trấn hơn).
        /// </summary>
        /// <param name="currentWall">Bức tường hiện tại.</param>
        /// <returns>GameObject của bức tường phía sau, hoặc null nếu không tìm thấy hoặc là bức tường gần nhất.</returns>
        public GameObject GetWallBehind(GameObject currentWall)
        {
            if (currentWall == null) return null;

            float wallX = currentWall.transform.position.x;
            if (townCenter == null) townCenter = Utility.GetTownCenter();
            if (townCenter == null)
            {
                Debug.LogError("[WallAndStumpManager] Town Center not found, cannot get wall behind.");
                return null;
            }

            float townCenterX = townCenter.transform.position.x;

            if (wallX < townCenterX)
            {
                // Bức tường hiện tại nằm ở phía bên trái
                int index = leftWalls.IndexOf(currentWall);
                // Nếu tìm thấy bức tường hiện tại và nó không phải là bức tường gần trung tâm thị trấn nhất (index > 0)
                if (index != -1 && index > 0)
                {
                    return leftWalls[index - 1]; // Bức tường trước đó trong danh sách giảm dần (gần trung tâm thị trấn hơn)
                }
            }
            else
            {
                // Bức tường hiện tại nằm ở phía bên phải
                int index = rightWalls.IndexOf(currentWall);
                // Nếu tìm thấy bức tường hiện tại và nó không phải là bức tường gần trung tâm thị trấn nhất (index > 0)
                if (index != -1 && index > 0)
                {
                    return rightWalls[index - 1]; // Bức tường trước đó trong danh sách tăng dần (gần trung tâm thị trấn hơn)
                }
            }

            return null; // Không tìm thấy bức tường phía sau hoặc nó là bức tường gần nhất với trung tâm thị trấn
        }
        #endregion
    }
} 