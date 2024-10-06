using System.Collections;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class ZombieAbilities : MonoBehaviour
{
    public PostProcessVolume postProcessingVolume;
    public GameObject throwable;
    public float throwForce = 10f; // Force applied when throwing
    public float throwAngle = 45f;
    public float maxThrowAngle = 80f;  // Max throw angle when looking up
    public float minThrowAngle = 5f;
    public LineRenderer lineRenderer;
    public int numPoints = 50; // Number of points on the line
    public float timeBetweenPoints = 0.1f; // Time between each point on the arc

    public Camera followCamera;      // The camera that will follow the thrown object
    public float cameraFollowDistance = 5f; // Distance behind the object
    public float cameraFollowHeight = 2f;   // Height above the object
    public float cameraFollowSpeed = 5f;    // Speed at which the camera follows

    private Rigidbody heldObjectRb;
    private Transform heldObjectTransform;
    private bool isHoldingObject = false;
    bool hasThrown = false;
    public Camera playerCamera;

    bool isControllingHand;
    PlayerControlAuthorative playerControlAuthorative;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        playerControlAuthorative = GetComponent<PlayerControlAuthorative>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check for right-click to pick up or hold an object
        if (!hasThrown && Input.GetMouseButton(1))
        {
            AimObject();
        }
        else if (isHoldingObject)
        {
            // Release object if right-click is released
            heldObjectRb.isKinematic = false;
            isHoldingObject = false;
            throwable.SetActive(false);
            lineRenderer.enabled = false;
        }

        // Check for left-click release to throw the held object
        if (Input.GetMouseButtonUp(0) && isHoldingObject)
        {
            ThrowObject();
        }
        if(hasThrown)
        {
            FollowThrownObject();
        }

        if (isControllingHand)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                SwitchBacktoPlayer();
            }
        }
    }

    void SwitchBacktoPlayer()
    {
        isControllingHand = false;
        hasThrown = false;
        throwable.SetActive(false);
        playerControlAuthorative.enablePlayerControls();
        followCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);
    }

    void AimObject()
    {
        UpdateThrowAngle();
        throwable.SetActive(true);
        heldObjectRb = throwable.GetComponent<Rigidbody>();
        heldObjectRb.isKinematic = true; // Disable physics while holding
        heldObjectTransform = heldObjectRb.transform;
        heldObjectTransform.position = transform.GetChild(0).position + transform.GetChild(0).forward + transform.GetChild(0).up + transform.GetChild(0).right; // Position it in front of the player
        isHoldingObject = true;

        DrawArc();
        lineRenderer.enabled = true;
    }

    void ThrowObject()
    {
        playerControlAuthorative.disablePlayerControls();
        lineRenderer.enabled = false;
        heldObjectRb.isKinematic = false; // Re-enable physics

        // Calculate the throw direction and force based on the throw angle
        Vector3 throwDirection = Quaternion.AngleAxis(-throwAngle, transform.GetChild(0).right) * transform.GetChild(0).forward;
        heldObjectRb.AddForce(throwDirection * throwForce, ForceMode.Impulse);

        // Clear the held object state
        heldObjectRb = null;
        isHoldingObject = false;
        hasThrown = true;

        playerCamera.gameObject.SetActive(false);
        followCamera.gameObject.SetActive(true);

        StartCoroutine(TakeControlOfHand());
    }

    void DrawArc()
    {
        Vector3 startPoint = heldObjectTransform.position;
        Vector3 startVelocity = Quaternion.AngleAxis(-throwAngle, transform.GetChild(0).right) * transform.GetChild(0).forward * throwForce;

        lineRenderer.positionCount = numPoints;
        Vector3 currentPosition = startPoint;

        for (int i = 0; i < numPoints; i++)
        {
            lineRenderer.SetPosition(i, currentPosition);

            // Calculate the position at the next time step
            float time = i * timeBetweenPoints;
            currentPosition = startPoint + startVelocity * time + 0.5f * Physics.gravity * time * time;
        }
    }

    void UpdateThrowAngle()
    {
        // Get the camera's local rotation in degrees
        float cameraPitch = playerCamera.transform.eulerAngles.x;

        // Normalize pitch to the range -180 to 180 degrees for easier handling
        if (cameraPitch > 180) cameraPitch -= 360;

        // Map camera pitch (-90 to 90 degrees) to throw angle (minThrowAngle to maxThrowAngle)
        throwAngle = Mathf.Lerp(minThrowAngle, maxThrowAngle, (cameraPitch + 90f) / 180f);
    }

    void FollowThrownObject()
    {
        if (throwable != null)
        {
            // Calculate the target position for the camera
            Vector3 targetPosition = throwable.transform.position
                                     - throwable.transform.forward * cameraFollowDistance
                                     + Vector3.up * cameraFollowHeight;

            // Smoothly move the camera towards the target position
            followCamera.transform.position = Vector3.Lerp(followCamera.transform.position, targetPosition, cameraFollowSpeed * Time.deltaTime);

            // Make the camera look at the thrown object
            followCamera.transform.LookAt(throwable.transform);
        }
    }

    IEnumerator TakeControlOfHand()
    {
        yield return new WaitForSeconds(2.2f);
        if (heldObjectRb != null)
        {
            heldObjectRb.isKinematic = true;
        }
        isControllingHand = true;
    }
}
