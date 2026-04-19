using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Quản lý AudioListener - chuyển giữa camera và player pivot.
    /// 
    /// VẤN ĐỀ: AudioListener nằm trên camera, nhưng spatial audio tính dựa trên 
    /// vị trí của listener. Để mono/stereo hoạt động đúng, listener cần nằm trên player.
    /// 
    /// GIẢI PHÁP:
    /// - Khi player sống: AudioListener được gắn vào player pivot
    /// - Khi player chết: AudioListener được chuyển lại về camera
    /// 
    /// SETUP:
    /// - Gắn script này lên GameObject bất kỳ (hoặc AudioManager)
    /// - PlayerController sẽ gọi SetListenerToPlayer() khi spawn
    /// - PlayerController sẽ gọi SetListenerToCamera() khi die
    /// </summary>
    public class AudioListenerManager : MonoBehaviour
    {
        public static AudioListenerManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Camera có AudioListener gốc")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("AudioListener component (được tìm tự động nếu null)")]
        private AudioListener _audioListener;

        private Transform _originalCameraTransform;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            FindAudioListener();
        }

        private void FindAudioListener()
        {
            // Tìm AudioListener trên camera
            if (mainCamera != null)
            {
                _audioListener = mainCamera.GetComponent<AudioListener>();
                _originalCameraTransform = mainCamera.transform;
            }

            // Nếu không có camera reference, tìm AudioListener trong scene
            if (_audioListener == null)
            {
                _audioListener = FindObjectOfType<AudioListener>();
                if (_audioListener != null)
                {
                    _originalCameraTransform = _audioListener.transform.parent;
                }
            }

            if (_audioListener == null)
            {
                Debug.LogError("[AudioListenerManager] Không tìm thấy AudioListener trong scene!");
            }
        }

        /// <summary>
        /// Gắn AudioListener vào player GameObject (khi player sống).
        /// Di chuyển component từ camera sang player GameObject.
        /// Gọi từ PlayerController.SetUpControllerPlayer()
        /// </summary>
        public void SetListenerToPlayer(Transform playerTransform)
        {
            if (_audioListener == null || playerTransform == null)
            {
                Debug.LogWarning("[AudioListenerManager] Không thể chuyển AudioListener - listener hoặc player null");
                return;
            }

            GameObject playerGameObject = playerTransform.gameObject;

            // Nếu AudioListener đã ở trên player rồi thì không cần làm gì
            if (_audioListener.gameObject == playerGameObject)
            {
                Debug.Log("[AudioListenerManager] AudioListener đã ở trên player");
                return;
            }

            // Lưu reference trước khi destroy
            AudioListener listenerRef = _audioListener;

            // Xóa AudioListener từ GameObject cũ (camera)
            DestroyImmediate(listenerRef);

            // Thêm AudioListener mới lên player GameObject
            _audioListener = playerGameObject.AddComponent<AudioListener>();

            Debug.Log($"[AudioListenerManager] AudioListener đã chuyển từ camera sang player GameObject: {playerGameObject.name}");
        }

        /// <summary>
        /// Chuyển AudioListener về camera GameObject (khi player chết).
        /// Di chuyển component từ player sang camera GameObject.
        /// Gọi từ PlayerController.Die()
        /// </summary>
        public void SetListenerToCamera()
        {
            if (_audioListener == null)
            {
                Debug.LogWarning("[AudioListenerManager] Không thể chuyển AudioListener - listener null");
                return;
            }

            if (_originalCameraTransform == null)
            {
                Debug.LogWarning("[AudioListenerManager] Không tìm thấy original camera transform");
                return;
            }

            GameObject cameraGameObject = _originalCameraTransform.gameObject;

            // Nếu AudioListener đã ở trên camera rồi thì không cần làm gì
            if (_audioListener.gameObject == cameraGameObject)
            {
                Debug.Log("[AudioListenerManager] AudioListener đã ở trên camera");
                return;
            }

            // Lưu reference trước khi destroy
            AudioListener listenerRef = _audioListener;

            // Xóa AudioListener từ GameObject cũ (player)
            DestroyImmediate(listenerRef);

            // Thêm AudioListener mới lên camera GameObject
            _audioListener = cameraGameObject.AddComponent<AudioListener>();

            Debug.Log("[AudioListenerManager] AudioListener đã chuyển từ player sang camera GameObject");
        }

        // Debug: hiển thị vị trí AudioListener trong Scene view
        private void OnDrawGizmos()
        {
            if (_audioListener != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_audioListener.transform.position, 0.5f);
                Gizmos.DrawLine(_audioListener.transform.position, _audioListener.transform.position + Vector3.up * 2f);
            }
        }
    }
}
