using TMPro;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(ClientNetworkTransform))]
public class PlayerControlAuthorative_OFFLINE : NetworkBehaviour
{
    public enum PlayerState
    {
        Idle,
        Forward, Right, Left,
        Running, RunRight, RunLeft,
        Backward, RunBackward,
        AgainstWal, Crouching
    }
    public enum CharacterType
    {
        Default, Werewolf, Vampire, Zombie, Ghost
    }

    [Header("- - Movement - -")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runningSpeed = 3f;
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
    private bool disableControls;

    // Character selection menu
    private Button defaultBtn, werewolfBtn, vampireBtn, zombieBtn, ghostBtn;
    private PlayersManager playersManager;
    private bool isWerewolf, isVampire, isZombie, isGhost; // If player is creature
    [SerializeField] private NetworkVariable<CharacterType> networkCharacterType = new NetworkVariable<CharacterType>(CharacterType.Default);
    private GameObject defaultChar, werewolfChar, vampireChar, zombieChar, ghostChar;
    [SerializeField] private float playerSpacing = 2.0f; // Distance between players
    public int locIndex = -1;
    private Transform loc0, loc1, loc2, loc3;
    public Button readyBtn;
    private bool isReady;
    public GameObject readyToggle;
    private bool inLobby;


    private void Awake()
    {
        characterController = GetComponentInChildren<CharacterController>();
        animator = gameObject.GetComponentInChildren<Animator>();
        characterBody = transform.Find("Body").gameObject;
        playerCam = GameObject.Find("PlayerMainCam");
        camHolder = GameObject.Find("PlayerCamHolder");
        orientation = transform.Find("Body/Orientation");
        camPos = transform.Find("Body/player_mesh/metarig/spine/spine.001/spine.002/spine.003/spine.004/spine.005/CamPos");


        // Character selection (Need to use prefab PlayerAuthorative_New so it works) (Can set inLobby to false so that you can go straight to playing, but haven't tested)
        inLobby = false;
        if (inLobby)
        {
            playersManager = GameObject.Find("PlayersManager").GetComponent<PlayersManager>();
            defaultChar = transform.Find("Body/player_mesh/Characters/Default").gameObject;
            werewolfChar = transform.Find("Body/player_mesh/Characters/Werewolf").gameObject; vampireChar = transform.Find("Body/player_mesh/Characters/Vampire").gameObject;
            zombieChar = transform.Find("Body/player_mesh/Characters/Zombie").gameObject; ghostChar = transform.Find("Body/player_mesh/Characters/Ghost").gameObject;
            defaultBtn = playersManager.defaultBtn;
            werewolfBtn = playersManager.werewolfBtn; vampireBtn = playersManager.vampireBtn;
            zombieBtn = playersManager.zombieBtn; ghostBtn = playersManager.ghostBtn;
            loc0 = GameObject.Find("Loc0").transform; loc1 = GameObject.Find("Loc1").transform;
            loc2 = GameObject.Find("Loc2").transform; loc3 = GameObject.Find("Loc3").transform;
            readyBtn = playersManager.readyBtn;
            playersManager.playerObjects.Add(this.gameObject);
        }
    }

    void Start()
    {
        //if (IsClient && IsOwner)
        //{
        characterBody.transform.position = new Vector3(Random.Range(defaultInitialPlanePosition.x, defaultInitialPlanePosition.y), 0, Random.Range(defaultInitialPlanePosition.x, defaultInitialPlanePosition.y));
        animator.SetBool("isOnFloor", true);

        // Character selection
        if (inLobby)
        {
            defaultBtn.onClick.AddListener(() => SetDefault());
            werewolfBtn.onClick.AddListener(() => SetWerewolf());
            vampireBtn.onClick.AddListener(() => SetVampire());
            zombieBtn.onClick.AddListener(() => SetZombie());
            ghostBtn.onClick.AddListener(() => SetGhost());

            readyBtn.onClick.AddListener(() => SetReady());
            characterController.enabled = false;
            disablePlayerControls();
        }
        //}
    }

    private void Update()
    {
        //if (IsClient && IsOwner)
        //{
        ClientInput();
        //}
        ClientAnimVisuals();

        // Character selection
        if (inLobby)
        {
            ClientCharVisuals();
            UpdateSelectCharMenuPosition();
            ClientReadyVisuals();
        }
    }

    public void disablePlayerControls()
    {
        disableControls = true;
    }
    public void enablePlayerControls()
    {
        disableControls = false;
    }

    private void ClientAnimVisuals()
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
        else if (networkPlayerState.Value == PlayerState.RunBackward)
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
        if (disableControls) { return; }

        // Get camera rotation and apply it to the player's body rotation
        var euler = playerCam.transform.rotation.eulerAngles;
        var rot = Quaternion.Euler(0, euler.y, 0);
        characterBody.transform.rotation = rot;

        // Get input for movement
        float forwardInput = Input.GetAxis("Vertical");  // Forward/backward input
        float horizontalInput = Input.GetAxis("Horizontal");  // Left/right input

        // Running logic
        bool isRunning = Input.GetKey(KeyCode.LeftShift);  // Check if Shift is held down
        float currentSpeed = isRunning ? runningSpeed : walkSpeed;  // Use runningSpeed or walkSpeed

        // Calculate movement
        Vector3 forwardDirection = characterBody.transform.forward * forwardInput;
        Vector3 rightDirection = characterBody.transform.right * horizontalInput;
        Vector3 inputPosition = (forwardDirection + rightDirection).normalized;

        // Move character using the current speed
        characterController.SimpleMove(inputPosition * currentSpeed);

        // Rotate the player body based on horizontal input (left and right rotation)
        Vector3 inputRotation = new Vector3(0, horizontalInput, 0);
        characterBody.transform.Rotate(inputRotation * rotationSpeed, Space.World);

        // Update state here and call on the server to sync
        UpdatePlayerState(DeterminePlayerState(forwardInput, horizontalInput, isRunning));

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

    private PlayerState DeterminePlayerState(float forwardInput, float horizontalInput, bool isRunning)
    {
        if (horizontalInput > 0 && isRunning) return PlayerState.RunRight;
        if (horizontalInput < 0 && isRunning) return PlayerState.RunLeft;
        if (horizontalInput > 0) return PlayerState.Right;
        if (horizontalInput < 0) return PlayerState.Left;
        if (forwardInput > 0) return (isRunning) ? PlayerState.Running : PlayerState.Forward;
        if (forwardInput < 0) return (isRunning) ? PlayerState.RunBackward : PlayerState.Backward;

        return PlayerState.Idle;
    }

    //[ServerRpc]
    public void UpdatePlayerState(PlayerState newState)
    {
        networkPlayerState.Value = newState;
    }



    #region Character Selection
    private void ClientCharVisuals()
    {
        if (networkCharacterType.Value == CharacterType.Default)
        {
            defaultChar.SetActive(true);
            werewolfChar.SetActive(false);
            vampireChar.SetActive(false);
            zombieChar.SetActive(false);
            ghostChar.SetActive(false);
        }
        else if (networkCharacterType.Value == CharacterType.Werewolf)
        {
            defaultChar.SetActive(false);
            werewolfChar.SetActive(true);
            vampireChar.SetActive(false);
            zombieChar.SetActive(false);
            ghostChar.SetActive(false);
        }
        else if (networkCharacterType.Value == CharacterType.Vampire)
        {
            defaultChar.SetActive(false);
            werewolfChar.SetActive(false);
            vampireChar.SetActive(true);
            zombieChar.SetActive(false);
            ghostChar.SetActive(false);
        }
        else if (networkCharacterType.Value == CharacterType.Zombie)
        {
            defaultChar.SetActive(false);
            werewolfChar.SetActive(false);
            vampireChar.SetActive(false);
            zombieChar.SetActive(true);
            ghostChar.SetActive(false);
        }
        else if (networkCharacterType.Value == CharacterType.Ghost)
        {
            defaultChar.SetActive(false);
            werewolfChar.SetActive(false);
            vampireChar.SetActive(false);
            zombieChar.SetActive(false);
            ghostChar.SetActive(true);
        }
    }

    private void ClientReadyVisuals()
    {
        if (isReady && readyToggle != null)
        {
            readyToggle.GetComponent<Toggle>().isOn = true;
            readyBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
        }
        else if (readyToggle != null)
        {
            readyToggle.GetComponent<Toggle>().isOn = false;
            readyBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Not Ready";
        }
    }

    void SetDefault()
    {
        if (IsOwner)
        {
            print("Default enabled.");
            if (isWerewolf) playersManager.ClearWerewolfServerRpc();
            if (isVampire) playersManager.ClearVampireServerRpc();
            if (isZombie) playersManager.ClearZombieServerRpc();
            if (isGhost) playersManager.ClearGhostServerRpc();

            isWerewolf = false;
            isVampire = false;
            isZombie = false;
            isGhost = false;

            isReady = false; // Cannot be ready if not picked character
            UpdateCharacterTypeServerRpc(CharacterType.Default);
        }
    }
    void SetWerewolf()
    {
        if (!playersManager.someoneIsWerewolf.Value && IsOwner)
        {
            print("Werewolf enabled.");
            playersManager.SetWerewolfServerRpc();
            if (isVampire) playersManager.ClearVampireServerRpc();
            if (isZombie) playersManager.ClearZombieServerRpc();
            if (isGhost) playersManager.ClearGhostServerRpc();

            isWerewolf = true;
            isVampire = false;
            isZombie = false;
            isGhost = false;

            UpdateCharacterTypeServerRpc(CharacterType.Werewolf);
        }
        else
        {
            print("ALREADY TAKEN");
        }
    }
    void SetVampire()
    {
        if (!playersManager.someoneIsVampire.Value && IsOwner)
        {
            print("Vampire enabled.");
            if (isWerewolf) playersManager.ClearWerewolfServerRpc();
            playersManager.SetVampireServerRpc();
            if (isZombie) playersManager.ClearZombieServerRpc();
            if (isGhost) playersManager.ClearGhostServerRpc();

            isWerewolf = false;
            isVampire = true;
            isZombie = false;
            isGhost = false;

            UpdateCharacterTypeServerRpc(CharacterType.Vampire);
        }
        else
        {
            print("ALREADY TAKEN");
        }
    }
    void SetZombie()
    {
        if (!playersManager.someoneIsZombie.Value && IsOwner)
        {
            print("Zombie enabled.");
            if (isWerewolf) playersManager.ClearWerewolfServerRpc();
            if (isVampire) playersManager.ClearVampireServerRpc();
            playersManager.SetZombieServerRpc();
            if (isGhost) playersManager.ClearGhostServerRpc();

            isWerewolf = false;
            isVampire = false;
            isZombie = true;
            isGhost = false;

            UpdateCharacterTypeServerRpc(CharacterType.Zombie);
        }
        else
        {
            print("ALREADY TAKEN");
        }
    }
    void SetGhost()
    {
        if (!playersManager.someoneIsGhost.Value && IsOwner)
        {
            print("Ghost enabled.");
            if (isWerewolf) playersManager.ClearWerewolfServerRpc();
            if (isVampire) playersManager.ClearVampireServerRpc();
            if (isZombie) playersManager.ClearZombieServerRpc();
            playersManager.SetGhostServerRpc();

            isWerewolf = false;
            isVampire = false;
            isZombie = false;
            isGhost = true;

            UpdateCharacterTypeServerRpc(CharacterType.Ghost);
        }
        else
        {
            print("ALREADY TAKEN");
        }
    }

    [ServerRpc]
    public void UpdateCharacterTypeServerRpc(CharacterType newCharacterType)
    {
        networkCharacterType.Value = newCharacterType;
    }

    private void SetReady()
    {
        if ((isWerewolf || isVampire || isZombie || isGhost) && !isReady) // Check that not ready and that selected character
        {
            print("Turned ready");
            isReady = true;
            playersManager.AddPlayerReady();
        }
        else if (isReady)
        {
            print("No longer ready");
            isReady = false;
            playersManager.RemovePlayerReady();
        }
        else
        {
            print("Select a character");
        }
    }

    public void UpdateSelectCharMenuPosition()
    {
        if (locIndex == 0)
        {
            readyToggle = playersManager.player0Ready;
            characterBody.transform.position = loc0.position;
        }
        if (locIndex == 1)
        {
            readyToggle = playersManager.player1Ready;
            characterBody.transform.position = loc1.position;
        }
        if (locIndex == 2)
        {
            readyToggle = playersManager.player2Ready;
            characterBody.transform.position = loc2.position;
        }
        if (locIndex == 3)
        {
            readyToggle = playersManager.player3Ready;
            characterBody.transform.position = loc3.position;
        }
    }
    #endregion
}
