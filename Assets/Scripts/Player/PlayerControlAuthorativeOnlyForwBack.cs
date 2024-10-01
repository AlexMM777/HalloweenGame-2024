using TMPro;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(ClientNetworkTransform))]
public class PlayerControlAuthorativeOnlyForwBack : NetworkBehaviour
{
    public enum PlayerState
    {
        Idle,
        Forward, Backward,
        Right, Left, 
        Running, Crouching, 
        OnFloor, AgainstWall
    }


    [Header("- - Movement - -")]
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float rotationSpeed = 1.5f;


    private CharacterController characterController;
    private GameObject characterBody;
    private Animator animator;

    [SerializeField] private Vector2 defaultInitialPlanePosition = new Vector2(-4, 4);
    [SerializeField] private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();


    [Header("- - Camera - -")]
    private GameObject playerCam;
    private GameObject camHolder;
    [SerializeField] private float mouseSensitivity = 10;
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform camPos;
    float xRotation, yRotation;
    private GameObject playerMesh;


    private void Awake()
    {
        characterController = GetComponentInChildren<CharacterController>();
        animator = gameObject.GetComponentInChildren<Animator>();
        characterBody = transform.Find("Body").gameObject;
        //playerCam = transform.Find("CamHolder/PlayerCam").gameObject;
        playerCam = GameObject.Find("PlayerMainCam");
        //camHolder = transform.Find("CamHolder").gameObject;
        camHolder = GameObject.Find("PlayerCamHolder");
        orientation = transform.Find("Body/Orientation");
        //playerMesh = transform.Find("Body/player_mesh").gameObject;
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
        if (networkPlayerState.Value == PlayerState.Forward)
        {
            animator.SetBool("isGoingForward", true);
            animator.SetBool("isRunning", false);
        }
        else if (networkPlayerState.Value == PlayerState.Running)
        {
            animator.SetBool("isRunning", true);
        }
        else if (networkPlayerState.Value == PlayerState.Backward)
        {
            animator.SetBool("isGoingBackward", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isGoingForward", false);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingLeft", false);
            animator.SetBool("isGoingRight", false);
            animator.SetBool("isCrouching", false);

        }
    }

    private void ClientInput()
    {
        //get target's rotation and transpose values
        var euler = playerCam.transform.rotation.eulerAngles;
        var rot = Quaternion.Euler(0, euler.y, 0);
        characterBody.transform.rotation = rot;




        // Y axis client rotation
        Vector3 inputRotation = new Vector3(0, Input.GetAxis("Horizontal"), 0);

        // Forward & backward direction
        Vector3 direction = characterBody.transform.TransformDirection(Vector3.forward);
        float forwardInput = Input.GetAxis("Vertical");
        if (Input.GetKey(KeyCode.LeftShift) && forwardInput > 0) forwardInput = 2;

        Vector3 inputPosition = direction * forwardInput;

        // Client is responsible for moving itself
        characterController.SimpleMove(inputPosition * walkSpeed);
        characterBody.transform.Rotate(inputRotation * rotationSpeed, Space.World);

        // Player state changes based on input
        if (forwardInput > 0 && forwardInput <=1)
        {
            UpdatePlayerStateServerRpc(PlayerState.Forward);
        }
        else if (forwardInput > 1)
        {
            UpdatePlayerStateServerRpc(PlayerState.Running);
        }
        else if (forwardInput < 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Backward);
        }
        else
        {
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        }




        // Control camera
        if (playerCam != null && camHolder != null && camPos != null)
        {
            //get mouse input
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            //rotate cam & orientation
            playerCam.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            orientation.rotation = Quaternion.Euler(0, yRotation, 0);

            //move camera to position
            camHolder.transform.position = camPos.position;
        }
        
    }

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState newState)
    {
        networkPlayerState.Value = newState;
    }
}
