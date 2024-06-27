using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocomotionTechnique : MonoBehaviour
{
    public enum SteeringMode
    {
        HeadTilt,
        ControllerDirection,
        Thumbstick
    }

    public OVRInput.Controller leftController;
    public OVRInput.Controller rightController;
    [Range(0, 10)] public float translationGain = 0.5f;
    public GameObject hmd;
    [SerializeField] private float leftTriggerValue;    
    [SerializeField] private float rightTriggerValue;
    [SerializeField] private Vector3 startPos;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool isIndexTriggerDown;
    public LineRenderer line;
    public LayerMask teleportMask;
    public float maxDistance = 10f;
    public float arcHeight = 5f;
    public float tiltSensitivity = 0.5f;
    private Vector3 destination;
    private bool isAiming = false;

    private int resulution = 20;
    // These are for the game mechanism.
    public ParkourCounter parkourCounter;
    public string stage;
    public SelectionTaskMeasure selectionTaskMeasure;
    private Vector3[] points;
    private bool can_jump =true; 
    public LocomotionMode currentMode;
    public TextMeshPro mode;
    public enum LocomotionMode
    {
        Nothing,
        Tilt,
        Hands
    }
    private void Awake()
    {
        GameObject lineRendererObj = new GameObject("ArcLineRenderer");
        lineRendererObj.transform.SetParent(this.transform);
        line = lineRendererObj.AddComponent<LineRenderer>();

        line.enabled = false;
        line.startWidth = 0.02f;
        line.endWidth = 0.01f;
        line.startColor = Color.green;
        line.endColor = Color.cyan;

        line.material = new Material(Shader.Find("Sprites/Default"));
        line.positionCount = 0;
        currentMode = LocomotionMode.Tilt;
        mode.SetText("Mode : " + currentMode) ;
        mode.color = Color.red;
    }

    void Update()
    
    {
        Debug.Log("Current Locomotion Mode: " + currentMode);
        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            ToggleLocomotionMode();
        }
        if (!can_jump) return;
        switch (currentMode)
        {
            case LocomotionMode.Tilt:
                HandleInput();
                break;
            case LocomotionMode.Nothing:
                HandleInputNothing();
                break;
            case LocomotionMode.Hands:
                HandleInputHands();
                break;
        }
        
    }
    private void ToggleLocomotionMode()
    {
        currentMode = (LocomotionMode)(((int)currentMode + 1) % System.Enum.GetValues(typeof(LocomotionMode)).Length);
        Debug.Log("Current Locomotion Mode: " + currentMode);
        mode.SetText("Mode : " + currentMode) ;
    }


    private void HandleInput()
    {
        if (!can_jump) return; // Do nothing if we are in cooldown

        leftTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, leftController);
        rightTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, rightController);

        if (rightTriggerValue >= 0.9f || leftTriggerValue >= 0.9f)
        {
            StartAiming();
        }
        else
        {
            StopAiming();
        }

        if (isAiming)
        {
            RenderArc();
            //AdjustDestinationBasedOnHeadTilt();

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
            {
                StartCoroutine(MoveAlongArc());
            }
        }

        // These are for the game mechanism.
        if (OVRInput.Get(OVRInput.Button.Two))
        {
            // Assuming you have a script or method to handle parkour respawn
            this.transform.position = parkourCounter.currentRespawnPos;
        }
    }
    private void HandleInputHands()
    {
        if (!can_jump) return; // Do nothing if we are in cooldown

        leftTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, leftController);
        rightTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, rightController);

        if (rightTriggerValue >= 0.9f || leftTriggerValue >= 0.9f)
        {
            StartAiming();
        }
        else
        {
            StopAiming();
        }

        if (isAiming)
        {
            RenderArc();
            //AdjustDestinationBasedOnHeadTilt();

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
            {
                StartCoroutine(MoveAlongArcHands());
            }
        }

        // These are for the game mechanism.
        if (OVRInput.Get(OVRInput.Button.Two))
        {
            // Assuming you have a script or method to handle parkour respawn
            this.transform.position = parkourCounter.currentRespawnPos;
        }
    }
    private void HandleInputNothing()
    {
        if (!can_jump) return; // Do nothing if we are in cooldown

        leftTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, leftController);
        rightTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, rightController);

        if (rightTriggerValue >= 0.9f || leftTriggerValue >= 0.9f)
        {
            StartAiming();
        }
        else
        {
            StopAiming();
        }

        if (isAiming)
        {
            RenderArc();
            //AdjustDestinationBasedOnHeadTilt();

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
            {
                StartCoroutine(MoveAlongArcNothing());
            }
        }

        // These are for the game mechanism.
        if (OVRInput.Get(OVRInput.Button.Two))
        {
            // Assuming you have a script or method to handle parkour respawn
            this.transform.position = parkourCounter.currentRespawnPos;
        }
    }

    private void StartAiming()
    {
        isAiming = true;
        line.enabled = true;
    }

    private void StopAiming()
    {
        isAiming = false;
        line.enabled = false;
    }

    void RenderArc()
    {
        Vector3 start = hmd.transform.position;
        Vector3 forwardDirection = hmd.transform.forward;
        Vector3 end = start + forwardDirection * maxDistance;

        Vector3 control = (start + end) / 2 + Vector3.up * arcHeight;
        points = new Vector3[20];

        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            points[i] = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
        }

        line.positionCount = points.Length;
        line.SetPositions(points);

        RaycastHit hit;
        if (Physics.Raycast(points[points.Length - 1], Vector3.down, out hit, 10f, teleportMask))
        {
            destination = hit.point;
        }
        else
        {
            destination = points[points.Length - 1];
        }

        Debug.Log("Arc End Position: " + points[points.Length - 1]);
        Debug.Log("Calculated Destination: " + destination);
    }

    IEnumerator MoveAlongArcNothing()
    {
        can_jump = false; // Disable jumping during cooldown
        DisableInput();

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = points[i + 1];
            float journey = 0f;
            float duration = 0.25f; // Adjust as needed for speed

            while (journey <= duration)
            {
                journey += Time.deltaTime;
                float percent = Mathf.Clamp01(journey / duration);
                transform.position = Vector3.Lerp(startPosition, endPosition, percent);
                yield return null;
            }
        }

        StopAiming();
        yield return new WaitForSeconds(3.0f); // Wait for the cooldown period
        EnableInput();
    }
    IEnumerator MoveAlongArcHands()
    {
        can_jump = false; // Disable jumping during cooldown
        DisableInput();

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 startPosition = transform.position;
            float journey = 0f;
            float duration = 0.25f; // Adjust as needed for speed

            while (journey <= duration)
            {
                journey += Time.deltaTime;
                float percent = Mathf.Clamp01(journey / duration);
                transform.position = Vector3.Lerp(startPosition, points[i], percent);

                // Adjust the destination based on head tilt during flight
                AdjustDestinationBasedOnControllerRotation();

                yield return null;
            }

            // Recalculate the remaining points in the arc based on the new destination
            if (i < points.Length - 1)
            {
                Vector3 currentPos = transform.position;
                Vector3 forwardDirection = hmd.transform.forward;
                Vector3 end = currentPos + forwardDirection * maxDistance;

                Vector3 control = (currentPos + end) / 2 + Vector3.up * arcHeight;
                for (int j = i + 1; j < points.Length; j++)
                {
                    float t = (j - i) / (float)(points.Length - i - 1);
                    points[j] = Mathf.Pow(1 - t, 2) * currentPos + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
                }
                line.SetPositions(points);
            }
        }

        StopAiming();
        yield return new WaitForSeconds(3.0f); // Wait for the cooldown period
        EnableInput();
    }
    IEnumerator MoveAlongArc()
    {
        can_jump = false; // Disable jumping during cooldown
        DisableInput();

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 startPosition = transform.position;
            float journey = 0f;
            float duration = 0.25f; // Adjust as needed for speed

            while (journey <= duration)
            {
                journey += Time.deltaTime;
                float percent = Mathf.Clamp01(journey / duration);
                transform.position = Vector3.Lerp(startPosition, points[i], percent);

                // Adjust the destination based on head tilt during flight
                AdjustDestinationBasedOnHeadTilt();

                yield return null;
            }

            // Recalculate the remaining points in the arc based on the new destination
            if (i < points.Length - 1)
            {
                Vector3 currentPos = transform.position;
                Vector3 forwardDirection = hmd.transform.forward;
                Vector3 end = currentPos + forwardDirection * maxDistance;

                Vector3 control = (currentPos + end) / 2 + Vector3.up * arcHeight;
                for (int j = i + 1; j < points.Length; j++)
                {
                    float t = (j - i) / (float)(points.Length - i - 1);
                    points[j] = Mathf.Pow(1 - t, 2) * currentPos + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
                }
                line.SetPositions(points);
            }
        }

        StopAiming();
        yield return new WaitForSeconds(3.0f); // Wait for the cooldown period
        EnableInput();
    }

    void AdjustDestinationBasedOnHeadTilt()
    {
        float y = destination.y;
        float tilt = hmd.transform.eulerAngles.x;
        if (tilt > 180) tilt -= 360; // Normalize tilt to -180 to 180

        if (Mathf.Abs(tilt) > tiltSensitivity)
        {
            Vector3 rightVector = Vector3.Cross(Vector3.up, hmd.transform.forward).normalized;
            // Make small adjustments to the destination along the right vector only
            Vector3 adjustment = rightVector * (tilt * tiltSensitivity * 0.001f);
            destination += adjustment;
        }
    }
    void AdjustDestinationBasedOnControllerRotation()
    {
        Vector3 leftHandPosition = OVRInput.GetLocalControllerPosition(leftController);
        Vector3 rightHandPosition = OVRInput.GetLocalControllerPosition(rightController);

        // Calculate the midpoint between the controllers
        Vector3 midpoint = (leftHandPosition + rightHandPosition) / 2;

        // Calculate the forward direction based on the controllers' positions
        Vector3 controllerForward = (rightHandPosition - leftHandPosition).normalized;
        Vector3 rightVector = Vector3.Cross(Vector3.up, controllerForward).normalized;

        // Calculate the adjustment
        float adjustmentFactor = Vector3.Distance(leftHandPosition, rightHandPosition) * 0.001f; // Scale as needed
        Vector3 adjustment = rightVector * adjustmentFactor;

        // Update the destination only along the horizontal plane
        Vector3 newDestination = destination + adjustment;
        newDestination.y = destination.y; // Ensure the y-coordinate of the destination remains unchanged

        destination = newDestination;
        Debug.Log("Adjusted Destination: " + destination);
        RenderArc();
    }

    private void DisableInput()
    {
        isAiming = false; // Stop aiming
        // Additional input disabling logic if needed
    }

    private void EnableInput()
    {
        can_jump = true; // Allow jumping after cooldown
        // Additional input enabling logic if needed
    }

    void OnTriggerEnter(Collider other)
    {
        // These are for the game mechanism.
        if (other.CompareTag("banner"))
        {
            stage = other.gameObject.name;
            parkourCounter.isStageChange = true;
        }
        else if (other.CompareTag("objectInteractionTask"))
        {
            selectionTaskMeasure.isTaskStart = true;
            selectionTaskMeasure.scoreText.text = "";
            selectionTaskMeasure.partSumErr = 0f;
            selectionTaskMeasure.partSumTime = 0f;
            // rotation: facing the user's entering direction
            float tempValueY = other.transform.position.y > 0 ? 12 : 0;
            Vector3 tmpTarget = new Vector3(hmd.transform.position.x, tempValueY, hmd.transform.position.z);
            selectionTaskMeasure.taskUI.transform.LookAt(tmpTarget);
            selectionTaskMeasure.taskUI.transform.Rotate(new Vector3(0, 180f, 0));
            selectionTaskMeasure.taskStartPanel.SetActive(true);
        }
        else if (other.CompareTag("coin"))
        {
            parkourCounter.coinCount += 1;
            this.GetComponent<AudioSource>().Play();
            other.gameObject.SetActive(false);
        }
    }
}
