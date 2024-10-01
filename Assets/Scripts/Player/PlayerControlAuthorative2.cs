using TMPro;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(ClientNetworkTransform))]
public class PlayerControlAuthorative2 : NetworkBehaviour
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
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private bool isSprinting = false;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool readyToJump = true;
    [SerializeField] private bool grounded = true;
    private float jumpForce = 6;
    private float jumpCooldown = 1.1f;
    private float playerHeight = 1.18f;
    [SerializeField] private LayerMask whatIsGround;
    //[SerializeField] private float rotationSpeed = 1.5f;



    float horizontalInput, verticalInput;
    Vector3 moveDirection;
    private Rigidbody bodyRigidBody;
    [SerializeField] private float groundDrag = 1;
    private GameObject playerMesh;




    private CharacterController characterController;
    [SerializeField] private GameObject characterBody;
    private Animator animator;

    [SerializeField] private Vector2 defaultInitialPlanePosition = new Vector2(-4, 4);
    [SerializeField] private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();

    [Header("- - KeyBinds - -")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("- - Camera - -")]
    private GameObject playerCam;
    private GameObject camHolder;
    [SerializeField] private float mouseSensitivity = 10;
    private Transform orientation;
    private Transform camPos;
    float xRotation, yRotation;

    private void Awake()
    {
        characterController = GetComponentInChildren<CharacterController>();
        animator = gameObject.GetComponentInChildren<Animator>();
        bodyRigidBody = gameObject.GetComponentInChildren<Rigidbody>();
        characterBody = transform.Find("Body").gameObject;
        playerCam = transform.Find("CamHolder/PlayerCam").gameObject;
        camHolder = transform.Find("CamHolder").gameObject;
        orientation = transform.Find("Body/Orientation");
        playerMesh = transform.Find("Body/player_mesh").gameObject;
        whatIsGround = LayerMask.GetMask("whatIsGround");
        camPos = transform.Find("Body/player_mesh/metarig/spine/spine.001/spine.002/spine.003/spine.004/spine.005/CamPos");
        bodyRigidBody.freezeRotation = true;
    }

    private void Start()
    {
        if (IsClient && IsOwner)
        {
            characterBody.transform.position = new Vector3(Random.Range(defaultInitialPlanePosition.x, defaultInitialPlanePosition.y), 0, Random.Range(defaultInitialPlanePosition.x, defaultInitialPlanePosition.y));
            animator.SetBool("isOnFloor", true);
        }
    }

    private void Update()
    {
        if (IsClient && IsOwner)
        {
            ClientInput();
        }
        ClientVisuals();
    }

    private void ClientVisuals()
    {
        /*if (networkPlayerState.Value == PlayerState.Forward)
        {
            animator.SetBool("isGoingForward", true);
        }
        else if (networkPlayerState.Value == PlayerState.Backward)
        {
            animator.SetBool("isGoingBack", true);
        }
        else
        {
            animator.SetBool("isGoingForward", false);
            animator.SetBool("isGoingBackward", false);
            animator.SetBool("isGoingLeft", false);
            animator.SetBool("isGoingRight", false);
            animator.SetBool("isCrouching", false);

        }*/

        if (!isSprinting)
        {
            animator.SetBool("isRunning", false);
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            animator.SetBool("isCrouching", true);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            animator.SetBool("isGoingForward", true);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            animator.SetBool("isGoingLeft", true);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            animator.SetBool("isGoingBackward", true);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            animator.SetBool("isGoingRight", true);
        }

        if (animator.GetBool("isGoingForward") == true || animator.GetBool("isGoingBackward") == true)
        {
            if (isSprinting)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    animator.SetBool("isRunning", true);
                }
            }
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            if (isSprinting)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                {
                    animator.SetBool("isRunning", true);
                }
            }
        }


        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            animator.SetBool("isCrouching", false);
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            animator.SetBool("isGoingForward", false);
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            animator.SetBool("isGoingLeft", false);
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            animator.SetBool("isGoingBackward", false);
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            animator.SetBool("isGoingRight", false);
        }


        //animator.SetBool("isAgainstWall", !canMove);
        animator.SetBool("isOnFloor", grounded);

        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.LeftShift))
        {
            animator.SetBool("isRunning", false);
        }
        if (!isSprinting)
        {
            animator.SetBool("isRunning", false);

        }
    }

    private void ClientInput()
    {
        /* Original Script
        // Y axis client rotation
        Vector3 inputRotation = new Vector3(0, Input.GetAxis("Horizontal"), 0);

        // Forward & backward direction
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        float forwardInput = Input.GetAxis("Vertical");
        if (Input.GetKey(KeyCode.LeftShift) && forwardInput > 0) forwardInput = 2;

        Vector3 inputPosition = direction * forwardInput;

        // Client is responsible for moving itself
        characterController.SimpleMove(inputPosition * walkSpeed);
        characterBody.transform.Rotate(inputRotation * rotationSpeed, Space.World);

        // Player state changes based on input
        if (forwardInput > 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Forward);
        }
        else if (forwardInput < 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Backward);
        }
        else
        {
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        }*/

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

        //ground check
        grounded = Physics.Raycast(characterBody.transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        //when to jump
        if (canJump)
        {
            if (Input.GetKey(jumpKey) && readyToJump && grounded)
            {
                readyToJump = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }
        //when to sprint
        if (Input.GetKey(sprintKey))
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        //calculate movement direction
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        bodyRigidBody.AddForce(moveDirection.normalized * walkSpeed * 10f * 2f, ForceMode.Force);

        Vector3 flatVel = new Vector3(bodyRigidBody.linearVelocity.x, 0f, bodyRigidBody.linearVelocity.z);

        //limit velocity if needed
        if (flatVel.magnitude > walkSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * walkSpeed;
            bodyRigidBody.linearVelocity = new Vector3(limitedVel.x, bodyRigidBody.linearVelocity.y, limitedVel.z);
        }

        //handle drag
        bodyRigidBody.linearDamping = groundDrag;

        //get target's rotation and transpose values
        var euler = playerCam.transform.rotation.eulerAngles;
        var rot = Quaternion.Euler(0, euler.y, 0);
        playerMesh.transform.rotation = rot;
    }

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState newState)
    {
        if (IsClient && IsOwner)
            networkPlayerState.Value = newState;
    }

    private void Jump()
    {
        if (IsClient && IsOwner)
        {
            //reset y velocity
            bodyRigidBody.linearVelocity = new Vector3(bodyRigidBody.linearVelocity.x, 0f, bodyRigidBody.linearVelocity.z);
            bodyRigidBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void ResetJump()
    {
        if (IsClient && IsOwner)
            readyToJump = true;
    }
}
