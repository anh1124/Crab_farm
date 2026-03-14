using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Script.Manager.WorldManager;

namespace Script.Editor
{
    /// <summary>
    /// Tool tạo toàn bộ Sky hierarchy trong scene.
    /// Menu: Tools → Setup CelestialManager
    ///
    /// Hierarchy được tạo:
    ///   Sky
    ///    ├── Background  (SpriteRenderer — cần assign sprite trắng)
    ///    ├── Sun         (SpriteRenderer)
    ///    ├── Moon        (SpriteRenderer)
    ///    └── Waypoints
    ///         ├── WP_0  (chân trời trái)
    ///         ├── WP_1
    ///         ├── WP_2  (đỉnh)
    ///         ├── WP_3
    ///         └── WP_4  (chân trời phải)
    /// </summary>
    public class CelestialSetupTool : EditorWindow
    {
        [MenuItem("Tools/Setup CelestialManager")]
        public static void SetupCelestialManager()
        {
            // Guard: không tạo trùng
            CelestialManager existing = Object.FindObjectOfType<CelestialManager>();
            if (existing != null)
            {
                Debug.LogWarning($"[CelestialSetupTool] CelestialManager đã tồn tại trên object: '{existing.gameObject.name}'. Hủy setup.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Guard: cần camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[CelestialSetupTool] Không tìm thấy Main Camera trong scene. Hãy đặt tag 'MainCamera' cho camera trước.");
                return;
            }

            // Đảm bảo Sorting Layer "Sky" tồn tại
            EnsureSortingLayer("Sky");

            // ── Tạo Sky root ──────────────────────────────────────────────────────
            GameObject skyRoot = new GameObject("Sky");
            skyRoot.transform.position = new Vector3(
                mainCam.transform.position.x,
                0f,
                0f
            );

            CelestialManager manager = skyRoot.AddComponent<CelestialManager>();

            // Tạo một GameObject trống làm điểm neo mục tiêu mặc định
            GameObject targetObj = new GameObject("CelestialTarget");
            // Không đặt làm child của Sky để nó có thể di chuyển độc lập hoặc bám theo Player
            targetObj.transform.position = skyRoot.transform.position;
            manager.backgroundFollowTarget = targetObj.transform;
            manager.celestialFollowTarget = targetObj.transform;

            // ── Background ────────────────────────────────────────────────────────
            // Background cần sprite màu TRẮNG để SpriteRenderer.color tint đúng.
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(skyRoot.transform);
            bgObj.transform.localPosition = new Vector3(0f, 0f, 10f); // đẩy về sau
            SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
            bgRenderer.sortingLayerName = "Sky";
            bgRenderer.sortingOrder = 0;
            bgRenderer.color = Color.white;
            manager.backgroundRenderer = bgRenderer;

            // ── Sun ───────────────────────────────────────────────────────────────
            GameObject sunObj = new GameObject("Sun");
            sunObj.transform.SetParent(skyRoot.transform);
            sunObj.transform.localPosition = new Vector3(-manager.waypoints.Count, 5f, 9f);
            SpriteRenderer sunRenderer = sunObj.AddComponent<SpriteRenderer>();
            sunRenderer.sortingLayerName = "Sky";
            sunRenderer.sortingOrder = 2;
            manager.sunRenderer = sunRenderer;

            // ── Moon ──────────────────────────────────────────────────────────────
            GameObject moonObj = new GameObject("Moon");
            moonObj.transform.SetParent(skyRoot.transform);
            moonObj.transform.localPosition = new Vector3(5f, 5f, 9f);
            SpriteRenderer moonRenderer = moonObj.AddComponent<SpriteRenderer>();
            moonRenderer.sortingLayerName = "Sky";
            moonRenderer.sortingOrder = 2;
            manager.moonRenderer = moonRenderer;

            // ── Waypoints ─────────────────────────────────────────────────────────
            // Tạo 5 waypoint theo hình cung nửa ellipse.
            // Người dùng có thể kéo chỉnh lại trong scene view.
            //
            // Hình dung (nhìn từ trên):
            //   WP0              WP4
            //      WP1    WP3
            //          WP2
            // Thực ra là arc từ trái → phải, WP2 là đỉnh cao nhất.
            GameObject waypointsRoot = new GameObject("Waypoints");
            waypointsRoot.transform.SetParent(skyRoot.transform);
            waypointsRoot.transform.localPosition = Vector3.zero;

            // Định nghĩa arc: 5 điểm theo hình bán ellipse
            // radiusX theo hướng ngang, radiusY theo hướng dọc
            float rX = 20f;
            float rY = 10f;
            int waypointCount = 5;
            List<Transform> wps = new List<Transform>();

            for (int i = 0; i < waypointCount; i++)
            {
                // Góc từ π (trái) → 0 (phải), chiều từ trái sang phải
                float t = i / (float)(waypointCount - 1);       // 0 → 1
                float angle = Mathf.PI - t * Mathf.PI;          // π → 0

                float x = Mathf.Cos(angle) * rX;                // -rX → +rX
                float y = Mathf.Sin(angle) * rY;                //  0  → rY → 0

                GameObject wp = new GameObject($"WP_{i}");
                wp.transform.SetParent(waypointsRoot.transform);
                wp.transform.localPosition = new Vector3(x, y, 9f);
                wps.Add(wp.transform);
            }

            manager.waypoints = wps;

            // ── Focus inspector ───────────────────────────────────────────────────
            Selection.activeGameObject = skyRoot;
            Undo.RegisterCreatedObjectUndo(skyRoot, "Setup CelestialManager");

            Debug.Log(
                "[CelestialSetupTool] Sky hierarchy đã được tạo!\n" +
                "Việc cần làm:\n" +
                "  1. Assign sprite TRẮNG cho Background SpriteRenderer\n" +
                "  2. Assign sprite mặt trời cho Sun SpriteRenderer\n" +
                "  3. Assign sprite mặt trăng cho Moon SpriteRenderer\n" +
                "  4. Kéo chỉnh các WP_x trong Scene View để tạo arc theo ý muốn\n" +
                "  5. Thêm 'Sky' vào Sorting Layers nếu chưa có (đã cố gắng tự thêm)"
            );
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Utility: Tự thêm Sorting Layer nếu chưa có
        // ──────────────────────────────────────────────────────────────────────────

        private static void EnsureSortingLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );

            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayers == null) return;

            // Kiểm tra đã tồn tại chưa
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                SerializedProperty layer = sortingLayers.GetArrayElementAtIndex(i);
                if (layer.FindPropertyRelative("name").stringValue == layerName) return;
            }

            // Thêm mới
            sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
            SerializedProperty newLayer = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
            newLayer.FindPropertyRelative("name").stringValue = layerName;
            newLayer.FindPropertyRelative("uniqueID").intValue = layerName.GetHashCode();

            tagManager.ApplyModifiedProperties();
            Debug.Log($"[CelestialSetupTool] Đã thêm Sorting Layer '{layerName}'.");
        }
    }
}
