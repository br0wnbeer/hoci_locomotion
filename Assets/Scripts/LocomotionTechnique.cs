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
    public Transform right; 
    
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
    private bool hitDetected;
    private Vector3 inintial_start;
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
        
        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            ToggleLocomotionMode();
        }
        if (!can_jump) return;
        inintial_start = transform.position;
        switch (currentMode)
        {
            case LocomotionMode.Tilt:
                HandleInput();
                break;
            case LocomotionMode.Nothing:
                HandleInputNothing();
                break;
            case LocomotionMode.Hands:
                //Vector3 fixedPosition = hmd.transform.position + hmd.transform.forward * 0.5f; // 0.5 meters in front of the camera
                //right.transform.position = fixedPosition;
               
                HandleInputHands();
                break;
        }
        
    }
   

    private void ToggleLocomotionMode()
    {
        currentMode = (LocomotionMode)(((int)currentMode + 1) % System.Enum.GetValues(typeof(LocomotionMode)).Length);
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
        Vector3 end ;
        if (Physics.Raycast(start, hmd.transform.forward, out RaycastHit initialHit, maxDistance, teleportMask))
        {
            Debug.LogWarning("Render Arc on terrain");
            end = initialHit.point;
        }
        else
        {
            Debug.LogWarning("render Arc outside Terrain");
            end = start + hmd.transform.forward * maxDistance;
        }
        Vector3 control = (start + end) / 2 + Vector3.up * arcHeight;
        int numPoints = 20;
        points = new Vector3[numPoints];
        hitDetected = false;

        for (int i = 0; i < numPoints; i++)
        {
            float t = i / (float)(numPoints - 1);
            points[i] = CalculateQuadraticBezierPoint(t, start, control, end);

            // Perform raycast to check for collisions
            if (i > 0)
            {
                Vector3 direction = (points[i] - points[i - 1]).normalized;
                float distance = Vector3.Distance(points[i], points[i - 1]);
                if (Physics.Raycast(points[i - 1], direction, out RaycastHit hit, distance, teleportMask))
                {
                    if (IsValidSurface(hit))
                    {
                        destination = hit.point;
                        hitDetected = true;
                        break;
                    }
                }
            }
        }

        if (!hitDetected)
        {
            for (int i = numPoints - 1; i >= 0; i--)
            {
                if (Physics.Raycast(points[i], Vector3.down, out RaycastHit hit, 10f, teleportMask))
                {
                    if (IsValidSurface(hit))
                    {
                        destination = hit.point;
                        hitDetected = true;
                        break;
                    }
                }
            }
        }

        if (!hitDetected)
        {
            CalculateDestination();
        }
        line.positionCount = hitDetected ? points.Length : numPoints;
        line.SetPositions(points);
    }

    private bool IsValidSurface(RaycastHit hit)
    {
        float angle = Vector3.Angle(hit.normal, Vector3.up);
        return angle <= 40.0f;
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * p0 +
               2f * oneMinusT * t * p1 +
               t * t * p2;
    }
    private void CalculateDestination()
    {
        if (!hitDetected)
        {
            Vector3 endPoint = points[points.Length - 1];
            if (Physics.Raycast(endPoint, Vector3.down, out RaycastHit hit, 10f, teleportMask))
            {
                if (IsValidSurface(hit))
                {
                    destination = hit.point;
                }
                else
                {
                    destination = endPoint;
                }
            }
            else
            {
                destination = endPoint;
            }
        }
    }
    
    IEnumerator MoveAlongArcNothing()
    {
        can_jump = false; // Disable jumping during cooldown
        DisableInput();

        for (int i = 0; i < points.Length - 2; i++)
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
                
                if (Physics.Raycast(startPosition, points[i], out RaycastHit hit,
                        (startPosition - points[i]).sqrMagnitude, teleportMask))
                {
                    
                    
                        destination = hit.point;
                        hitDetected = true;
                       
                }
                transform.position = Vector3.Lerp(startPosition, points[i], percent);
                
                AdjustDestinationBasedOnControllerRotation();
                
                

                yield return null;
            }

            // Recalculate the remaining points in the arc based on the new destination
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
                
                if (Physics.Raycast(startPosition, points[i], out RaycastHit hit,
                        (startPosition - points[i]).sqrMagnitude, teleportMask))
                {
                  
                    {
                        destination = hit.point;
                        hitDetected = true;
                        
                    }
                }
                transform.position = Vector3.Lerp(startPosition, points[i], percent);
                // Adjust the destination based on head tilt during flight
               
                AdjustDestinationBasedOnHeadTilt();
               
                

                yield return null;
            }

            // Recalculate the remaining points in the arc based on the new destination
        }

        StopAiming();
        yield return new WaitForSeconds(3.0f); // Wait for the cooldown period
        EnableInput();
    }

    void AdjustDestinationBasedOnHeadTilt()
    {
        
        
        Vector3 endAdjusted = inintial_start + new Vector3(hmd.transform.forward.x, 0, hmd.transform.forward.z) * (maxDistance + 10* ((hmd.transform.forward.y +1) / 2 ));
        Vector3 controlAdjusted = (inintial_start + endAdjusted) / 2 + Vector3.up * arcHeight;
       Debug.LogWarning("Current Adjustment Y : " +hmd.transform.forward.y  );
        for (int i = 0; i < 20; i++)
        {
            float adjustedT = i / (float)(20 - 1);
            points[i] = CalculateQuadraticBezierPoint(adjustedT, inintial_start, controlAdjusted, endAdjusted) + new Vector3(0, hmd.transform.forward.y ,0) * 3;

            if (i > 0)
            {
                Vector3 direction = (points[i] - points[i - 1]).normalized;
                float distance = Vector3.Distance(points[i], points[i - 1]);
                if (Physics.Raycast(points[i - 1], direction, out RaycastHit hit, distance, teleportMask))
                {
                    if (IsValidSurface(hit))
                    {
                        destination = hit.point;
                        hitDetected = true;
                        break;
                    }
                }
            }
        }

        line.positionCount = points.Length;
        line.SetPositions(points);
    }
    void AdjustDestinationBasedOnControllerRotation()
    {
        
        Vector3 endAdjusted = inintial_start + new Vector3(right.transform.forward.x, 0, right.transform.forward.z) * (maxDistance + 10* ((right.transform.forward.y +1) / 2 ));
        Vector3 controlAdjusted = (inintial_start + endAdjusted) / 2 + Vector3.up * arcHeight;
        Debug.LogWarning("Current Adjustment Y : " +hmd.transform.forward.y  );
        for (int i = 0; i < 20; i++)
        {
            float adjustedT = i / (float)(20 - 1);
            points[i] = CalculateQuadraticBezierPoint(adjustedT, inintial_start, controlAdjusted, endAdjusted) + new Vector3(0,right.transform.forward.y ,0) * 3;

            if (i > 0)
            {
                Vector3 direction = (points[i] - points[i - 1]).normalized;
                float distance = Vector3.Distance(points[i], points[i - 1]);
                if (Physics.Raycast(points[i - 1], direction, out RaycastHit hit, distance, teleportMask))
                {
                    if (IsValidSurface(hit))
                    {
                        destination = hit.point;
                        hitDetected = true;
                        break;
                    }
                }
            }
        }

        line.positionCount = points.Length;
        line.SetPositions(points);
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
