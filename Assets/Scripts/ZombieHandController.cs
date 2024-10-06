using UnityEngine;

public class ZombieHandController : MonoBehaviour
{
    public Camera followCamera;
    private float currentX = 0f;              // X rotation value (yaw)
    private float currentY = 0f;              // Y rotation value (pitch)
    private const float Y_ANGLE_MIN = -30f;   // Minimum vertical angle
    private const float Y_ANGLE_MAX = 60f;    // Maximum vertical angle
    public float cameraFollowDistance = 5f;
    public float cameraFollowHeight = 2f;
    public float mouseSensitivity = 2f;
    public float scrollSensitivity = 2f;
    public float moveSpeed = 5f;
    private Vector3 cameraVelocity = Vector3.zero;
    public float cameraSmoothTime = 0.1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        FollowAndOrbitThrownObject();
        ControlThrownObject();
    }

    void FollowAndOrbitThrownObject()
    {
        // Get mouse input for rotation
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Clamp vertical angle (pitch) between min and max
        currentY = Mathf.Clamp(currentY, Y_ANGLE_MIN, Y_ANGLE_MAX);

        // Adjust camera distance with scroll wheel
        cameraFollowDistance -= Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;
        cameraFollowDistance = Mathf.Clamp(cameraFollowDistance, 2f, 10f); // Clamp zoom distance

        // Calculate the desired position of the camera around the object
        Vector3 direction = new Vector3(0, cameraFollowHeight, -cameraFollowDistance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 targetPosition = transform.position + rotation * direction;

        // Smoothly move the camera towards the target position
        followCamera.transform.position = Vector3.SmoothDamp(followCamera.transform.position, targetPosition, ref cameraVelocity, cameraSmoothTime);

        // Make the camera look at the thrown object
        followCamera.transform.LookAt(transform);
    }

    void ControlThrownObject()
    {
        // Movement input
        float moveHorizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float moveVertical = Input.GetAxis("Vertical");     // W/S or Up/Down

        // Calculate movement direction
        Vector3 movement = new Vector3(moveHorizontal, 0, moveVertical).normalized;
        if (movement.magnitude > 0) // Only move if there is input
        {
            // Move the object in the forward direction relative to the camera view
            Vector3 cameraForward = followCamera.transform.TransformDirection(Vector3.forward);
            Vector3 cameraRight = followCamera.transform.TransformDirection(Vector3.right);
            Vector3 desiredDirection = (cameraForward * moveVertical + cameraRight * moveHorizontal).normalized;

            // Move the throwable object
            transform.position += desiredDirection * moveSpeed * Time.deltaTime;
        }
    }
}
