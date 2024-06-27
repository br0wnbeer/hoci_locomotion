using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;

public class ParabolicRay : MonoBehaviour
{
    public GameObject controller;  // Assign your VR controller GameObject here
    public LineRenderer lineRenderer;
    public int resolution = 20;
    public float[] velocitySteps = new float[] { 5f, 7f, 9f, 11f, 13f, 15f };  // Predefined velocity values
    public float angle = 45f;
    public float gravity = 9.8f;  // Positive gravity for downward force
    public LayerMask teleportMask;  // Assign the layers you want to hit for teleportation
    public Transform player;  // Reference to the player/camera rig
    public GameObject teleportIndicator;  // Assign a GameObject to indicate the teleport point
    public float teleportDuration = 1f;  // Duration of the smooth teleportation
    public Transform HMD;  // Assign your HMD Transform here

    private bool isAiming = false;
    private Vector3 hitPoint;
    private float initialVelocity;
    private List<Vector3> arcPositions = new List<Vector3>();

    void Start()
    {
        // Ensure LineRenderer is initialized
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // LineRenderer settings
        lineRenderer.positionCount = resolution;
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;
        initialVelocity = 15.0f;

        // Ensure teleport indicator is disabled initially
        if (teleportIndicator != null)
        {
            teleportIndicator.SetActive(false);
        }
    }

    void Update()
    {
        // Aiming input
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            isAiming = true;
            lineRenderer.enabled = true;
        }
        // Teleport input
        else if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            isAiming = false;

            if (hitPoint != Vector3.zero)
            {
                StartCoroutine(SmoothTeleport());
            }

            // Disable teleport indicator when not aiming
            if (teleportIndicator != null)
            {
                teleportIndicator.SetActive(false);
            }
        }

        // Adjust range using thumbstick
        float thumbstickValue = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).y;
        AdjustVelocity(thumbstickValue);

        // Render the arc if aiming
        if (isAiming)
        {
            RenderArc();
        }
    }

    void AdjustVelocity(float thumbstickValue)
    {
        int index = Mathf.FloorToInt((thumbstickValue + 1) / 2 * (velocitySteps.Length - 1));
        index = Mathf.Clamp(index, 0, velocitySteps.Length - 1);
        initialVelocity = velocitySteps[index];
    }

    void RenderArc()
    {
        arcPositions.Clear();
        Vector3[] positions = new Vector3[resolution];
        float radianAngle = Mathf.Deg2Rad * angle;
        float maxDistance = (initialVelocity * initialVelocity * Mathf.Sin(2 * radianAngle)) / gravity;
        bool hitDetected = false;
        hitPoint = Vector3.zero;  // Reset hitPoint each frame

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            positions[i] = CalculateArcPoint(t, maxDistance);
            arcPositions.Add(positions[i]);

            if (!hitDetected && i > 0)
            {
                if (Physics.Raycast(positions[i - 1], positions[i] - positions[i - 1], out RaycastHit hit, Vector3.Distance(positions[i - 1], positions[i]), teleportMask))
                {
                    positions[i] = hit.point;
                    hitPoint = hit.point;
                    lineRenderer.positionCount = i + 1;
                    hitDetected = true;

                    // Update teleport indicator position
                    if (teleportIndicator != null)
                    {
                        teleportIndicator.transform.position = hitPoint;
                        teleportIndicator.SetActive(true);
                    }

                    break;
                }
            }
        }

        // If no hit detected, disable teleport indicator
        if (!hitDetected && teleportIndicator != null)
        {
            teleportIndicator.SetActive(false);
        }

        lineRenderer.SetPositions(positions);
    }

    Vector3 CalculateArcPoint(float t, float maxDistance)
    {
        float x = t * maxDistance;
        float y = x * Mathf.Tan(Mathf.Deg2Rad * angle) - ((gravity * x * x) / (2 * initialVelocity * initialVelocity * Mathf.Cos(Mathf.Deg2Rad * angle) * Mathf.Cos(Mathf.Deg2Rad * angle)));
        return controller.transform.position + controller.transform.forward * x + controller.transform.up * y;
    }

    IEnumerator SmoothTeleport()
    {
        lineRenderer.enabled = true;  // Ensure the line renderer is enabled during teleport

        int segmentCount = arcPositions.Count - 1;
        float segmentDuration = teleportDuration / segmentCount;
        float elapsedTime = 0f;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 start = arcPositions[i];
            Vector3 end = arcPositions[i + 1];
            elapsedTime = 0f;

            while (elapsedTime < segmentDuration)
            {
                float t = elapsedTime / segmentDuration;
                player.position = Vector3.Lerp(start, end, t);

                // Adjust the landing position dynamically using HMD orientation
                AdjustArcWithHeadTilt();
                end = arcPositions[i + 1];  // Update the end position with new arc data

                RenderArc();  // Draw the arc during teleport

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure the player reaches the end of this segment
            player.position = end;
        }

        // Ensure the player reaches the final position
        player.position = arcPositions[arcPositions.Count - 1];

        lineRenderer.enabled = false;  // Disable the line renderer after teleport
    }

    void AdjustArcWithHeadTilt()
    {
        // Get the HMD rotation
        Quaternion headRotation = HMD.rotation;
        Vector3 headDirection = headRotation * Vector3.forward;

        // Adjust the controller forward direction based on the head direction
        controller.transform.forward = headDirection;

        // Recalculate the arc positions
        RenderArc();
    }
}
