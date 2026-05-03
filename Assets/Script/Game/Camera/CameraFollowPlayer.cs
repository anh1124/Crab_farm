using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour {

    public static CameraFollowPlayer Instance { get; private set; } // Singleton instance

    
    public float targetDistance = 1.0f; // Distance the camera will adjust based on target movement
    public float smoothTime = 0.3f; // Time it takes for the camera to reach the target position smoothly
    public GameObject target;
    public Vector3 offset =new Vector3(0,4.75f,0); // Optional offset for camera position

    private float previousTargetX; // Stores the previous X position of the target
    private float currentTargetX;  // Stores the current X position of the target
    private Vector3 velocity = Vector3.zero; // Velocity reference used by SmoothDamp
    private int _lastLookDirection = 1;      // Lưu hướng nhìn cuối cùng (1: phải, -1: trái)

    
    private void Awake() {
        // Singleton initialization
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }
    
    // Use this for initialization
    void Start() {
        // Find the target object
        target = GameObject.FindWithTag("CameraTaget");

        if (target != null) {
            // Initialize the camera position to the target's position with an optional offset
            Vector3 startPosition = target.transform.position + offset;
            startPosition.z = -1; // Set the Z position of the camera
            transform.position = startPosition;

            // Initialize previousTargetX to the target's initial X position
            previousTargetX = target.transform.position.x;
        }
    }

    public void setCameraTarget()
    {
        target = GameObject.FindWithTag("CameraTaget");

    }
    // Update is called once per frame
    void LateUpdate() {
        if (target != null) {
            // Update the current X position of the target
            currentTargetX = target.transform.position.x;

            // Determine the target positionర్ for the camera
            Vector3 targetPosition = transform.position;

            // ✅ Thêm ngưỡng sai số (Deadzone) để tránh rung lắc khi đứng im
            float moveThreshold = 0.001f; 
            
            if (currentTargetX > previousTargetX + moveThreshold) {
                // Target is moving to the right
                targetPosition.x = target.transform.position.x + targetDistance;
                _lastLookDirection = 1;
            } else if (currentTargetX < previousTargetX - moveThreshold) {
                // Target is moving to the left
                targetPosition.x = target.transform.position.x - targetDistance;
                _lastLookDirection = -1;
            } else {
                // Khi đứng im, giữ nguyên hướng nhìn cuối cùng thay vì nhảy về giữa
                targetPosition.x = target.transform.position.x + (_lastLookDirection * targetDistance);
            }

            // Maintain the Y position of the camera with the offset
            targetPosition.y = target.transform.position.y + offset.y;
        
            // Ensure the Z position is less than or equal to -1
            targetPosition.z = Mathf.Min(-1, targetPosition.z);

            // Smoothly move the camera towards the target position using SmoothDamp
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

            // Ensure the camera's Z position remains <= -1 after smoothing
            if (transform.position.z > -1) {
                Vector3 correctedPosition = transform.position;
                correctedPosition.z = -1;
                transform.position = correctedPosition;
            }

            // Update previousTargetX for the next frame
            previousTargetX = currentTargetX;
        }
    }
    // Instantly moves the camera to the specified position
    public void TeleportCameraTo(Vector3 newPosition) {
        newPosition.z = Mathf.Min(-1, newPosition.z); // Ensure the Z position is <= -1
        transform.position = newPosition;
    }

}
