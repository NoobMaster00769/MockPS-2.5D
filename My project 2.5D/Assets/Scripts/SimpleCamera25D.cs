using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCamera25D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 2, -10);

    [Header("Follow Settings")]
    public bool followX = true;
    public bool followY = false; // Keep false for platformers
    public bool followZ = true;

    [Header("Smoothing")]
    public float smoothSpeedX = 5f;
    public float smoothSpeedY = 3f;
    public float smoothSpeedZ = 5f;

    [Header("Y Position Control")]
    public float fixedYHeight = -7.61f; // Fixed camera Y position
    public bool useAdaptiveY = false; // Smooth Y following when player moves far
    public float yFollowThreshold = 5f;
    public float yAdaptiveSpeed = 1f;

    [Header("Boundaries (Optional)")]
    public bool useBoundaries = false;
    public float minX = -100f;
    public float maxX = 100f;
    public float minY = -100f;
    public float maxY = 100f;

    private float targetYPosition;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // Auto-find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("Camera auto-found player: " + player.name);
            }
            else
            {
                Debug.LogError("SimpleCamera25D: No target assigned and no Player tag found!");
            }
        }

        targetYPosition = followY ? (target != null ? target.position.y + offset.y : fixedYHeight) : fixedYHeight;

        // Initialize camera position immediately to match target
        if (target != null)
        {
            Vector3 initialPos = transform.position;
            if (followX) initialPos.x = target.position.x + offset.x;
            if (followY) initialPos.y = target.position.y + offset.y;
            else initialPos.y = targetYPosition + offset.y;
            if (followZ) initialPos.z = target.position.z + offset.z;
            transform.position = initialPos;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = transform.position;

        // X Position (Horizontal)
        if (followX)
        {
            float targetX = target.position.x + offset.x;
            desiredPosition.x = Mathf.Lerp(transform.position.x, targetX, smoothSpeedX * Time.deltaTime);
        }

        // Y Position (Vertical)
        if (followY)
        {
            float targetY = target.position.y + offset.y;
            desiredPosition.y = Mathf.Lerp(transform.position.y, targetY, smoothSpeedY * Time.deltaTime);
        }
        else
        {
            // Fixed Y with optional adaptive following
            if (useAdaptiveY)
            {
                float playerY = target.position.y;
                float yDiff = Mathf.Abs(playerY - targetYPosition);

                // If player gets too far, smoothly adjust target Y
                if (yDiff > yFollowThreshold)
                {
                    targetYPosition = Mathf.Lerp(targetYPosition, playerY, yAdaptiveSpeed * Time.deltaTime);
                }
            }

            desiredPosition.y = Mathf.Lerp(transform.position.y, targetYPosition + offset.y, smoothSpeedY * Time.deltaTime);
        }

        // Z Position (Depth)
        if (followZ)
        {
            float targetZ = target.position.z + offset.z;
            desiredPosition.z = Mathf.Lerp(transform.position.z, targetZ, smoothSpeedZ * Time.deltaTime);
        }

        // Apply boundaries
        if (useBoundaries)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        transform.position = desiredPosition;
    }

    // Helper method to set new target at runtime
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (!followY)
        {
            targetYPosition = fixedYHeight;
        }
    }

    // Helper to set fixed Y position
    public void SetFixedY(float yPos)
    {
        fixedYHeight = yPos;
        targetYPosition = yPos;
    }

    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position, 0.5f);

        // Draw camera target position
        Vector3 camTarget = new Vector3(
            target.position.x + offset.x,
            (followY ? target.position.y : targetYPosition) + offset.y,
            target.position.z + offset.z
        );
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(camTarget, Vector3.one * 0.5f);
        Gizmos.DrawLine(transform.position, camTarget);

        // Draw boundaries
        if (useBoundaries)
        {
            Gizmos.color = Color.red;
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, transform.position.z);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0);
            Gizmos.DrawWireCube(center, size);
        }

        // Draw Y threshold
        if (!followY && useAdaptiveY && target != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 pos = target.position;
            Gizmos.DrawLine(new Vector3(pos.x - 1, targetYPosition + yFollowThreshold, pos.z),
                           new Vector3(pos.x + 1, targetYPosition + yFollowThreshold, pos.z));
            Gizmos.DrawLine(new Vector3(pos.x - 1, targetYPosition - yFollowThreshold, pos.z),
                           new Vector3(pos.x + 1, targetYPosition - yFollowThreshold, pos.z));
        }
    }
}