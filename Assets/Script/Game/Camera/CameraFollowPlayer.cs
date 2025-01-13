using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour {

    public static CameraFollowPlayer Instance { get; private set; } // Singleton instance

    
    public float targetDistance = 1.0f; // Distance the camera will adjust based on target movement
    public float smoothTime = 0.3f; // Time it takes for the camera to reach the target position smoothly
    public GameObject target;
    public Vector3 offset ; // Optional offset for camera position

    private float previousTargetX; // Stores the previous X position of the target
    private float currentTargetX;  // Stores the current X position of the target
    private Vector3 velocity = Vector3.zero; // Velocity reference used by SmoothDamp

    
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

        offset = new Vector3(0,4.5f,0);
    }
    // Update is called once per frame
    void FixedUpdate() {
        if (target != null) {
            // Update the current X position of the target
            currentTargetX = target.transform.position.x;

            // Determine the target position for the camera
            Vector3 targetPosition = transform.position;

            if (currentTargetX > previousTargetX) {
                // Target is moving to the right
                targetPosition.x = target.transform.position.x + targetDistance;
            } else if (currentTargetX < previousTargetX) {
                // Target is moving to the left
                targetPosition.x = target.transform.position.x - targetDistance;
            }

            // Maintain the Y position of the camera with the offset
            targetPosition.y = target.transform.position.y + offset.y;
            targetPosition.z = -1; // Ensure the Z position is fixed

            // Smoothly move the camera towards the target position using SmoothDamp
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

            // Update previousTargetX for the next frame
            previousTargetX = currentTargetX;
        }
    }
}
