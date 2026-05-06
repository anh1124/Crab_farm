using UnityEngine;
using Script.Manager.WorldManager;
using Script.Data_Model;
using System.Collections.Generic;
using Script.Entities.Structures.SummondPortal;
using Script.Game.Enemy;

namespace Script.Manager.GamePlayManager
{
    public partial class EnemyPortalManager : MonoBehaviour
    {
        public void OnPortalDestroyed(EnemyPortalBehavior portal)
        {
            if (portal == null) return;
            
            // Xác định bên trước khi xóa
            bool isRight = false;
            if (rightPortals.Contains(portal)) isRight = true;

            // Xóa khỏi danh sách
            leftPortals.Remove(portal);
            rightPortals.Remove(portal);

            // Sắp xếp lại và cập nhật tuyến đầu
            SortPortals();
            UpdateFrontlineStatus();
            
            // [Storm Invasion] Cộng thêm power cho wave tiếp theo
            if (EnemyWaveManager.Instance != null)
            {
                EnemyWaveManager.Instance.AddExtraPowerFromDestroyedPortal();
            }
            
            // [Storm Invasion] Kích hoạt ngay portal phía sau (nếu có)
            ActivateNextPortal(isRight);

            // [Storm Invasion] Tự động lấp đầy portal còn thiếu sau khi 1 cái bị phá
            SpawnAvailablePortal();

            Debug.Log($"[EnemyPortalManager] Portal bị phá tại x={portal.transform.position.x:F1}. Đã cập nhật danh sách, kích hoạt portal tuyến đầu mới và lấp đầy portal phía sau.");
        }
        
        /// <summary>
        /// Kích hoạt ngay portal tiếp theo trong danh sách (portal phía sau vừa lên tuyến đầu)
        /// </summary>
        private void ActivateNextPortal(bool isRightSide)
        {
            var portals = isRightSide ? rightPortals : leftPortals;
            
            if (portals.Count > 0 && portals[0] != null)
            {
                // Portal đầu tiên giờ là tuyến đầu mới - kích hoạt ngay
                portals[0].ForceActivate();
                Debug.Log($"[EnemyPortalManager] Đã kích hoạt ngay portal {(isRightSide ? "PHẢI" : "TRÁI")} phía sau tại x={portals[0].transform.position.x:F1}");
            }
        }

        [ContextMenu("Set All Portals Active")]
        public void SetAllPortalsActive()
        {
            // Active tất cả portal trong list (tuyến đầu sẽ được xử lý riêng)
            foreach (var portal in leftPortals)
            {
                if (portal != null && portal.gameObject != null)
                {
                    portal.gameObject.SetActive(true);
                    portal.ForceActivate();
                }
            }
            foreach (var portal in rightPortals)
            {
                if (portal != null && portal.gameObject != null)
                {
                    portal.gameObject.SetActive(true);
                    portal.ForceActivate();
                }
            }
            Debug.Log($"[EnemyPortalManager] Đã bật và Kích Hoạt tất cả {leftPortals.Count + rightPortals.Count} cổng.");
        }

        /// <summary>
        /// Kích hoạt ngay lập tức các portal tuyến đầu (thường gọi khi tạo world mới)
        /// </summary>
        public void ForceActivateFrontlinePortals()
        {
            if (leftPortals.Count > 0 && leftPortals[0] != null)
            {
                leftPortals[0].ForceActivate();
            }
            if (rightPortals.Count > 0 && rightPortals[0] != null)
            {
                rightPortals[0].ForceActivate();
            }
            Debug.Log("[EnemyPortalManager] Đã kích hoạt cưỡng bức 2 portal tuyến đầu.");
        }

        #region Storm Invasion System

        /// <summary>
        /// Được gọi từ EnemyPortalBehavior khi portal spawn hoặc load từ save.
        /// Đăng ký portal vào danh sách quản lý theo bên và sắp xếp lại.
        /// </summary>
        public void RegisterPortal(EnemyPortalBehavior portal)
        {
            if (portal == null) return;

            GameObject townCenter = Utility.GetTownCenter();
            float centerX = townCenter != null ? townCenter.transform.position.x : 0f;
            float portalX = portal.transform.position.x;

            // Xác định bên trái/phải
            bool isRight = portalX > centerX;

            Debug.Log($"[EnemyPortalManager] RegisterPortal: portal tại x={portalX:F1}, centerX={centerX}, isRight={isRight}");

            if (isRight)
            {
                if (!rightPortals.Contains(portal))
                {
                    rightPortals.Add(portal);
                    Debug.Log($"[EnemyPortalManager] Thêm portal vào rightPortals. Count={rightPortals.Count}");
                }
            }
            else
            {
                if (!leftPortals.Contains(portal))
                {
                    leftPortals.Add(portal);
                    Debug.Log($"[EnemyPortalManager] Thêm portal vào leftPortals. Count={leftPortals.Count}");
                }
            }

            SortPortals();
            UpdateFrontlineStatus();
        }

        /// <summary>
        /// Sắp xếp danh sách portal theo khoảng cách đến TownCenter (gần nhất đầu tiên).
        /// </summary>
        private void SortPortals()
        {
            GameObject townCenter = Utility.GetTownCenter();
            Vector3 centerPos = townCenter != null ? townCenter.transform.position : Vector3.zero;

            // Sắp xếp left portals: gần TownCenter nhất (x lớn nhất trong số âm) đầu tiên
            leftPortals.Sort((a, b) =>
            {
                float distA = Mathf.Abs(a.transform.position.x - centerPos.x);
                float distB = Mathf.Abs(b.transform.position.x - centerPos.x);
                return distA.CompareTo(distB);
            });

            // Sắp xếp right portals: gần TownCenter nhất (x nhỏ nhất trong số dương) đầu tiên
            rightPortals.Sort((a, b) =>
            {
                float distA = Mathf.Abs(a.transform.position.x - centerPos.x);
                float distB = Mathf.Abs(b.transform.position.x - centerPos.x);
                return distA.CompareTo(distB);
            });

        }

        /// <summary>
        /// Cập nhật trạng thái tuyến đầu cho tất cả portal.
        /// Chỉ portal đầu tiên mỗi bên là tuyến đầu.
        /// Khi portal mới trở thành tuyến đầu (vừa lên index 0), reset status.activeDelay.
        /// </summary>
        private void UpdateFrontlineStatus()
        {
            // Left side
            for (int i = 0; i < leftPortals.Count; i++)
            {
                bool isFront = (i == 0); // Chỉ portal đầu tiên là tuyến đầu
                bool wasFront = leftPortals[i].IsFrontline(); // Kiểm tra trạng thái cũ
                leftPortals[i].SetFrontline(isFront);
                
                // Nếu vừa trở thành tuyến đầu, reset status.activeDelay
                if (isFront && !wasFront)
                {
                    leftPortals[i].ResetToDefaultDelay();
                    Debug.Log($"[EnemyPortalManager] Left portal [{i}] tại x={leftPortals[i].transform.position.x:F1} VỪA TRỞ THÀNH TUYẾN ĐẦU - Reset delay");
                }
                
                if (isFront)
                    Debug.Log($"[EnemyPortalManager] Left portal [{i}] tại x={leftPortals[i].transform.position.x:F1} là TUYẾN ĐẦU");
            }

            // Right side
            for (int i = 0; i < rightPortals.Count; i++)
            {
                bool isFront = (i == 0);
                bool wasFront = rightPortals[i].IsFrontline();
                rightPortals[i].SetFrontline(isFront);
                
                if (isFront && !wasFront)
                {
                    rightPortals[i].ResetToDefaultDelay();
                    Debug.Log($"[EnemyPortalManager] Right portal [{i}] tại x={rightPortals[i].transform.position.x:F1} VỪA TRỞ THÀNH TUYẾN ĐẦU - Reset delay");
                }
                
                if (isFront)
                    Debug.Log($"[EnemyPortalManager] Right portal [{i}] tại x={rightPortals[i].transform.position.x:F1} là TUYẾN ĐẦU");
            }
        }

        /// <summary>
        /// Gọi portal tuyến đầu (index 0) tự trừ delay.
        /// Portal tự quyết định có active hay không dựa trên status.activeDelay.
        /// </summary>
        private void ProcessFrontlineDelays()
        {
            Debug.Log("[EnemyPortalManager] ProcessFrontlineDelays() ĐƯỢC GỌI!");
            
            int currentDay = _timeProvider != null ? _timeProvider.GetElapsedDays() : 0;
            int currentHour = _timeProvider != null ? _timeProvider.GameHour : 0;
            
            Debug.Log($"[EnemyPortalManager] === ProcessFrontlineDelays | Day {currentDay} Hour {currentHour}:00 === Left={leftPortals.Count}, Right={rightPortals.Count}");
            
            // Gọi left portal index 0 tự xử lý delay
            if (leftPortals.Count > 0 && leftPortals[0] != null)
            {
                var portal = leftPortals[0];
                Debug.Log($"[EnemyPortalManager] Gọi TickDelay cho Left[0] x={portal.transform.position.x:F1} | isFrontline={portal.IsFrontline()} activeDelay={portal.GetStatus()?.activeDelay} isReady={portal.IsReadyToSpawn}");
                leftPortals[0].TickDelay();
            }
            else
            {
                Debug.Log($"[EnemyPortalManager] [WARNING] Không có Left[0] để TickDelay! List count={leftPortals.Count}");
            }
            
            // Gọi right portal index 0 tự xử lý delay
            if (rightPortals.Count > 0 && rightPortals[0] != null)
            {
                var portal = rightPortals[0];
                Debug.Log($"[EnemyPortalManager] Gọi TickDelay cho Right[0] x={portal.transform.position.x:F1} | isFrontline={portal.IsFrontline()} activeDelay={portal.GetStatus()?.activeDelay} isReady={portal.IsReadyToSpawn}");
                rightPortals[0].TickDelay();
            }
            else
            {
                Debug.Log($"[EnemyPortalManager] [WARNING] Không có Right[0] để TickDelay! List count={rightPortals.Count}");
            }
        }

        /// <summary>
        /// Lấy danh sách portal tuyến đầu (gần TownCenter nhất mỗi bên, đang active).
        /// </summary>
        public List<EnemyPortalBehavior> GetFrontlineActivePortals()
        {
            List<EnemyPortalBehavior> frontline = new List<EnemyPortalBehavior>();

            // [DEBUG] Log chi tiết để kiểm tra tại sao portal không active
            if (leftPortals.Count > 0 && leftPortals[0] != null)
            {
                var left = leftPortals[0];
                if (left.IsReadyToSpawn)
                    frontline.Add(left);
                else
                    Debug.Log($"[EnemyPortalManager] [DEBUG] Left[0] KHÔNG READY - isReadyToSpawn={left.IsReadyToSpawn}, activeDelay={left.GetStatus()?.activeDelay}");
            }

            if (rightPortals.Count > 0 && rightPortals[0] != null)
            {
                var right = rightPortals[0];
                if (right.IsReadyToSpawn)
                    frontline.Add(right);
                else
                    Debug.Log($"[EnemyPortalManager] [DEBUG] Right[0] KHÔNG READY - isReadyToSpawn={right.IsReadyToSpawn}, activeDelay={right.GetStatus()?.activeDelay}");
            }

            Debug.Log($"[EnemyPortalManager] [DEBUG] GetFrontlineActivePortals trả về {frontline.Count} portal (Left ready={leftPortals.Count > 0 && leftPortals[0] != null && leftPortals[0].IsReadyToSpawn}, Right ready={rightPortals.Count > 0 && rightPortals[0] != null && rightPortals[0].IsReadyToSpawn})");
            return frontline;
        }

        /// <summary>
        /// Yêu cầu portal tuyến đầu di chuyển về phía TownCenter.
        /// Portal tự quyết định có di chuyển hay không (nếu chưa active thì không di chuyển).
        /// </summary>
        private void MovePortalsTowardsTownCenter()
        {
            GameObject townCenter = Utility.GetTownCenter();
            if (townCenter == null) return;

            float targetX = townCenter.transform.position.x;

            // Yêu cầu left portal index 0 di chuyển (portal tự quyết định)
            // Sử dụng thời gian di chuyển đã tính toán dựa trên khoảng cách từ TownCenter
            if (leftPortals.Count > 0 && leftPortals[0] != null)
            {
                float calculatedDuration = leftPortals[0].CalculateMoveDuration(basePortalMoveDuration, durationIncreasePer100Distance);
                leftPortals[0].TryMoveTowards(targetX, portalMoveSpeed, calculatedDuration);
                Debug.Log($"[EnemyPortalManager] Left portal di chuyển với duration={calculatedDuration:F1}s (base={basePortalMoveDuration}s + {durationIncreasePer100Distance}s/100f)");
            }

            // Yêu cầu right portal index 0 di chuyển (portal tự quyết định)
            if (rightPortals.Count > 0 && rightPortals[0] != null)
            {
                float calculatedDuration = rightPortals[0].CalculateMoveDuration(basePortalMoveDuration, durationIncreasePer100Distance);
                rightPortals[0].TryMoveTowards(targetX, portalMoveSpeed, calculatedDuration);
                Debug.Log($"[EnemyPortalManager] Right portal di chuyển với duration={calculatedDuration:F1}s (base={basePortalMoveDuration}s + {durationIncreasePer100Distance}s/100f)");
            }

            // Sắp xếp lại sau khi di chuyển (phòng khi portal vượt qua nhau)
            SortPortals();
        }



        #endregion

        /// <summary>
        /// Hàm dành riêng cho WaveManager để lấy list cổng đã sẵn sàng đẻ quái.
        /// Chỉ trả về portal tuyến đầu đang active.
        /// </summary>
        public List<EnemyPortalBehavior> GetActivePortals()
        {
            return GetFrontlineActivePortals();
        }

        /// <summary>
        /// Inject dependencies do portal tự gọi khi load từ save.
        /// </summary>
    }
}
