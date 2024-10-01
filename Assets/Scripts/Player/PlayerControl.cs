using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class PlayerControl : NetworkBehaviour
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
    //[SerializeField] private float runSpeed = 3f;
    //[SerializeField] private bool isSprinting = false;
    [SerializeField] private float rotationSpeed = 1.5f;

    [Header("- - Other - -")]
    [SerializeField] private Vector2 defaultInitialPlanePosition = new Vector2(-4, 4);
    [SerializeField] private NetworkVariable<Vector3> networkPositionDirection = new NetworkVariable<Vector3>();
    [SerializeField] private NetworkVariable<Vector3> networkRotationDirection = new NetworkVariable<Vector3>();
    [SerializeField] private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();
    [SerializeField] private NetworkVariable<float> forwardBackPosition = new NetworkVariable<float>();
    [SerializeField] private NetworkVariable<float> leftRightPosition = new NetworkVariable<float>();

    [Header("- - KeyBinds - -")]
    //[SerializeField] private KeyCode jumpKey = KeyCode.Space;
    //[SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    private CharacterController characterController;
    private Animator animator;

    // client caching
    private Vector3 oldInputPosition;
    private Vector3 oldInputRotation;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = gameObject.GetComponentInChildren<Animator>(); ;
    }

    private void Start()
    {
        if (IsClient && IsOwner)
        {
            transform.position = new Vector3(Random.Range(defaultInitialPlanePosition.x, defaultInitialPlanePosition.y), 0, Random.Range(defaultInitialPlanePosition.x, defaultInitialPlanePosition.y));
        }
    }

    private void Update()
    {
        if (IsClient && IsOwner)
        {
            ClientInput();
        }

        ClientMoveAndRotate();
        ClientVisuals();
    }

    private void ClientInput()
    {
        // Player position and rotation input
        Vector3 inputRotation = new Vector3(0, Input.GetAxis("Horizontal"), 0);

        Vector3 direction = transform.TransformDirection(Vector3.forward);
        float forwardInput = Input.GetAxis("Vertical");
        Vector3 inputPosition = direction * forwardInput;

        if ((oldInputPosition != inputPosition) || (oldInputRotation != inputPosition))
        {
            oldInputRotation = inputRotation;
            oldInputPosition = inputPosition;

            UpdateClientPositionAndRotationServerRpc(inputPosition * walkSpeed, inputRotation * rotationSpeed);
        }
        
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
        }
    }

    private void ClientMoveAndRotate()
    {
        if(networkPositionDirection.Value != Vector3.zero)
        {
            characterController.SimpleMove(networkPositionDirection.Value);
        }
        if (networkRotationDirection.Value != Vector3.zero)
        {
            transform.Rotate(networkRotationDirection.Value);
        }
    }

    private void ClientVisuals()
    {
        if (networkPlayerState.Value == PlayerState.Forward)
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

        }
    }

    [ServerRpc]
    public void UpdateClientPositionAndRotationServerRpc(Vector3 newPositionDirection, Vector3 newRotationDirection)
    {
        networkPositionDirection.Value = newPositionDirection;
        networkRotationDirection.Value = newRotationDirection;
    }

    [ServerRpc]

    public void UpdatePlayerStateServerRpc(PlayerState newState)
    {
        networkPlayerState.Value = newState;
    }
}
