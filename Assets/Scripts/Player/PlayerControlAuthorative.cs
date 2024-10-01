using TMPro;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(ClientNetworkTransform))]
public class PlayerControlAuthorative : NetworkBehaviour
{
    public enum PlayerState
    {
        Idle, 
        Forward, Right, Left, 
        Running, RunRight, RunLeft,
        Backward, RunBackward,
        AgainstWal, Crouching,
    }

    [Header("- - Movement - -")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 3f;


    private CharacterController characterController;
    private GameObject characterBody;
    private Animator animator;

    [SerializeField] private Vector2 defaultInitialPlanePosition = new Vector2(-4, 4);
    [SerializeField] private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();


    [Header("- - Camera - -")]
    private GameObject playerCam;
    private GameObject camHolder;
    [SerializeField] private float mouseSensitivity = 10;
    private Transform orientation;
    private Transform camPos;
    float xRotation, yRotation;
    private GameObject playerMesh;


    private void Awake()
    {
        characterController = GetComponentInChildren<CharacterController>();
        animator = gameObject.GetComponentInChildren<Animator>();
        characterBody = transform.Find("Body").gameObject;
        playerCam = GameObject.Find("PlayerMainCam");
        camHolder = GameObject.Find("PlayerCamHolder");
        orientation = transform.Find("Body/Orientation");
        camPos = transform.Find("Body/player_mesh/metarig/spine/spine.001/spine.002/spine.003/spine.004/spine.005/CamPos");
    }

    void Start()
    {
        if (IsClient && IsOwner)
        {
            characterBody.transform.position = new Vector3(Random.Range(defaultInitialPlanePosition.x, defaultInitialPlanePosition.y), 0, Random.Range(defaultInitialPlanePosition.x, defaultInitialPlanePosition.y));
            animator.SetBool("isOnFloor", true);
        }
    }

    void Update()
    {
        if (IsClient && IsOwner)
        {
            ClientInput();
        }
        ClientVisuals();
    }

    private void ClientVisuals()
    {
        if (networkPlayerState.Value == PlayerState.RunRight)
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isGoingForward", true);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingRight", true);
        }
        else if (networkPlayerState.Value == PlayerState.RunLeft)
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isGoingForward", true);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingLeft", true);
        }
        else if (networkPlayerState.Value == PlayerState.Right)
        {
            animator.SetBool("isGoingRight", true);
            animator.SetBool("isRunning", false);
            animator.SetBool("isGoingForward", true);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingLeft", false);
        }
        else if (networkPlayerState.Value == PlayerState.Left)
        {
            animator.SetBool("isGoingLeft", true);
            animator.SetBool("isRunning", false);
            animator.SetBool("isGoingForward", true);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingRight", false);
        }
        else if (networkPlayerState.Value == PlayerState.Forward)
        {
            animator.SetBool("isGoingForward", true);
            animator.SetBool("isRunning", false);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingLeft", false);
            animator.SetBool("isGoingRight", false);
        }
        else if (networkPlayerState.Value == PlayerState.Running)
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isGoingForward", true);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingLeft", false);
            animator.SetBool("isGoingRight", false);
        }
        else if (networkPlayerState.Value == PlayerState.Backward)
        {
            animator.SetBool("isGoingBackward", true);
            animator.SetBool("isRunning", false);
            animator.SetBool("isGoingForward", false);
            animator.SetBool("isGoingLeft", false);
            animator.SetBool("isGoingRight", false);
        }
        else if (networkPlayerState.Value == PlayerState.RunBackward)  // <-- Add this block
        {
            animator.SetBool("isGoingBackward", true);
            animator.SetBool("isRunning", true);
            animator.SetBool("isGoingForward", false);
            animator.SetBool("isGoingLeft", false);
            animator.SetBool("isGoingRight", false);
        }
        else
        {
            // Reset all animation states if idle
            animator.SetBool("isRunning", false);
            animator.SetBool("isGoingForward", false);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingLeft", false);
            animator.SetBool("isGoingRight", false);
        }
    }



    private void ClientInput()
    {
        // Get camera rotation and apply it to the player's body rotation
        var euler = playerCam.transform.rotation.eulerAngles;
        var rot = Quaternion.Euler(0, euler.y, 0);
        characterBody.transform.rotation = rot;

        // Get input for both forward/backward (Vertical) and left/right (Horizontal) movement
        float forwardInput = Input.GetAxis("Vertical");  // Forward/backward input
        float horizontalInput = Input.GetAxis("Horizontal");  // Left/right input

        // Determine if the player is running (with Left Shift)
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // Apply running speed when Left Shift is held
        if (isRunning && forwardInput > 0) forwardInput = 2; // Running forward
        if (isRunning && horizontalInput != 0) horizontalInput = 2 * Mathf.Sign(horizontalInput); // Running left or right

        // New logic to handle running backward
        if (isRunning && forwardInput < 0)
        {
            forwardInput = -2; // Running backward
        }

        // Compute the movement direction based on the forward and right direction of the player
        Vector3 forwardDirection = characterBody.transform.forward * forwardInput;
        Vector3 rightDirection = characterBody.transform.right * horizontalInput;

        // Combine the forward and right direction into a single movement vector
        Vector3 inputPosition = (forwardDirection + rightDirection).normalized;

        // Client is responsible for moving itself
        characterController.SimpleMove(inputPosition * walkSpeed);

        // Rotate the player body based on horizontal input (left and right rotation)
        Vector3 inputRotation = new Vector3(0, horizontalInput, 0);
        characterBody.transform.Rotate(inputRotation * rotationSpeed, Space.World);

        // Prioritize horizontal movement over forward/backward movement
        if (horizontalInput > 0 && isRunning)
        {
            UpdatePlayerStateServerRpc(PlayerState.RunRight); // Run right
        }
        else if (horizontalInput < 0 && isRunning)
        {
            UpdatePlayerStateServerRpc(PlayerState.RunLeft); // Run left
        }
        else if (horizontalInput > 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Right); // Walk right
        }
        else if (horizontalInput < 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Left); // Walk left
        }
        else if (forwardInput > 0 && forwardInput <= 1)
        {
            UpdatePlayerStateServerRpc(PlayerState.Forward); // Walk forward
        }
        else if (forwardInput > 1)
        {
            UpdatePlayerStateServerRpc(PlayerState.Running); // Run forward
        }
        else if (forwardInput < 0 && forwardInput >= -1)
        {
            UpdatePlayerStateServerRpc(PlayerState.Backward); // Walk backward
        }
        else if (forwardInput < -1)
        {
            UpdatePlayerStateServerRpc(PlayerState.RunBackward); // Run backward
        }
        else
        {
            UpdatePlayerStateServerRpc(PlayerState.Idle); // Idle
        }

        // Control camera
        if (playerCam != null && camHolder != null && camPos != null)
        {
            // Get mouse input for camera movement
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            // Rotate camera and orientation based on input
            playerCam.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            orientation.rotation = Quaternion.Euler(0, yRotation, 0);

            // Move camera to the camera position on the player
            camHolder.transform.position = camPos.position;
        }
    }





    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState newState)
    {
        networkPlayerState.Value = newState;
    }
}
