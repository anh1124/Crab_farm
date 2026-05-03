using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization; // Thêm namespace này

namespace Script.Game.Cam
{
    public class ParallaxDebugLogger : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraTransform;       
        public Transform playerTransform;       

        [Header("Settings")]
        public int maxFramesToLog = 600;        
        public bool logEveryFrame = true;       
        public float positionChangeThreshold = 0.0001f; 
        public KeyCode startLogKey = KeyCode.F1;
        public KeyCode stopLogKey = KeyCode.F2;

        private List<Transform> parallaxLayers = new List<Transform>();
        private List<string> csvRows = new List<string>();
        private bool isLogging = false;
        private Vector3 lastCameraPos;
        private Vector3 lastPlayerPos;
        private string filePath;

        void Start()
        {
            if (cameraTransform == null)
                cameraTransform = UnityEngine.Camera.main?.transform;
            
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p) playerTransform = p.transform;
            }

            var allParallax = Object.FindObjectsOfType<Script.Manager.WorldManager.ParallaxLayer>();
            foreach (var p in allParallax)
            {
                parallaxLayers.Add(p.transform);
            }

            if (parallaxLayers.Count == 0)
                Debug.LogWarning("[ParallaxLogger] Không tìm thấy đối tượng nào có script ParallaxLayer!");

            filePath = Path.Combine(Application.persistentDataPath, "ParallaxLog.csv");
            lastCameraPos = cameraTransform ? cameraTransform.position : Vector3.zero;
            lastPlayerPos = playerTransform ? playerTransform.position : Vector3.zero;
        }

        void Update()
        {
            if (Input.GetKeyDown(startLogKey) && !isLogging)
                StartLogging();
            if (Input.GetKeyDown(stopLogKey) && isLogging)
                StopLogging();
        }

        void LateUpdate()
        {
            if (!isLogging || csvRows.Count >= maxFramesToLog)
            {
                if (csvRows.Count >= maxFramesToLog && isLogging) StopLogging();
                return;
            }

            bool hasChanged = false;
            if (cameraTransform && Vector3.Distance(cameraTransform.position, lastCameraPos) > positionChangeThreshold)
                hasChanged = true;

            if (!logEveryFrame && !hasChanged) return;

            StringBuilder sb = new StringBuilder();
            sb.Append(Time.frameCount).Append(",");
            // Sử dụng InvariantCulture để buộc dùng dấu chấm thập phân
            sb.Append(Time.time.ToString("F6", CultureInfo.InvariantCulture)).Append(",");
            sb.Append(Time.deltaTime.ToString("F8", CultureInfo.InvariantCulture));

            if (cameraTransform)
            {
                Vector3 camDelta = cameraTransform.position - lastCameraPos;
                sb.Append(",").Append(cameraTransform.position.x.ToString("F6", CultureInfo.InvariantCulture));
                sb.Append(",").Append(camDelta.x.ToString("F8", CultureInfo.InvariantCulture));
                lastCameraPos = cameraTransform.position;
            }
            else sb.Append(",0,0");

            if (playerTransform)
            {
                Vector3 playerDelta = playerTransform.position - lastPlayerPos;
                sb.Append(",").Append(playerTransform.position.x.ToString("F6", CultureInfo.InvariantCulture));
                sb.Append(",").Append(playerDelta.x.ToString("F8", CultureInfo.InvariantCulture));
                lastPlayerPos = playerTransform.position;
            }
            else sb.Append(",0,0");

            foreach (Transform layer in parallaxLayers)
            {
                sb.Append(",").Append(layer.name);
                sb.Append(",").Append(layer.position.x.ToString("F6", CultureInfo.InvariantCulture));
            }

            csvRows.Add(sb.ToString());
        }

        private void StartLogging()
        {
            csvRows.Clear();
            StringBuilder header = new StringBuilder();
            header.Append("Frame,Time,DeltaTime,CameraX,CameraDeltaX,PlayerX,PlayerDeltaX");
            foreach (Transform layer in parallaxLayers)
            {
                header.Append($",{layer.name}_Name,{layer.name}_X");
            }
            csvRows.Add(header.ToString());
            isLogging = true;
            Debug.Log("<color=green>[ParallaxLogger] Bắt đầu ghi log. Nhấn F2 để dừng.</color>");
        }

        private void StopLogging()
        {
            isLogging = false;
            WriteToFile();
            Debug.Log($"<color=yellow>[ParallaxLogger] Đã dừng. File lưu tại: {filePath}</color>");
        }

        private void WriteToFile()
        {
            try
            {
                File.WriteAllLines(filePath, csvRows);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Ghi file thất bại: " + e.Message);
            }
        }

        void OnDestroy()
        {
            if (isLogging) StopLogging();
        }
    }
}