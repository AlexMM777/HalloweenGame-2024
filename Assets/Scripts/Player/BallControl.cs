using TMPro;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(ClientNetworkTransform))]
public class BallControl : NetworkBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private float flySpeed = 1f;

    //[SerializeField] private float rotationSpeed = 1.5f;

    [SerializeField] private Vector2 defaultInitialPlanePosition = new Vector2(-4, 4);

    private Rigidbody ballRigidBody;

    void Awake()
    {
        ballRigidBody = GetComponent<Rigidbody>();
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
    }

    private void ClientInput()
    {
        // Y axis client rotation
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if(vertical > 0 || vertical < 0)
        {
            ballRigidBody.AddForce(vertical > 0 ? Vector3.forward * speed : Vector3.back * speed);
        }
        if(horizontal > 0 || horizontal < 0)
        {
            ballRigidBody.AddForce(horizontal > 0 ? Vector3.right * speed : Vector3.left * speed);
        }
        if(Input.GetKey(KeyCode.Space))
        {
            ballRigidBody.AddForce(Vector3.up * flySpeed);
        }
    }
}
