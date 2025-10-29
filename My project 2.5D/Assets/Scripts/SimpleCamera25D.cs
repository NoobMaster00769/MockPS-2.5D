using UnityEngine;

public class SimpleCamera25D : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("The main player to follow (bottom/controllable player)")]
    public Transform player;

    [Header("Camera Settings")]
    [Tooltip("How smoothly the camera follows (higher = smoother/slower)")]
    public float smoothSpeed = 5f;

    [Tooltip("Camera offset from player position")]
    public Vector3 offset = new Vector3(0, 2, -10);

    [Header("Follow Axes")]
    [Tooltip("Follow player on X axis (horizontal)")]
    public bool followX = true;

    [Tooltip("Follow player on Y axis (vertical)")]
    public bool followY = false;

    [Tooltip("Follow player on Z axis (depth)")]
    public bool followZ = false;

    [Header("Fixed Position")]
    [Tooltip("Fixed Y height (used when followY is false)")]
    public float fixedY = 2f;

    [Tooltip("Fixed Z position (used when followZ is false)")]
    public float fixedZ = -10f;

    [Header("Camera Bounds (Optional)")]
    [Tooltip("Enable to constrain camera within bounds")]
    public bool useBounds = false;
    public float minX = -100f;
    public float maxX = 100f;
    public float minY = -100f;
    public float maxY = 100f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("SimpleCamera25D: Auto-found player - " + playerObj.name);
            }
            else
            {
                Debug.LogError("SimpleCamera25D: No player assigned and couldn't find 'Player' tag!");
            }
        }

        if (player != null && showDebugInfo)
        {
            Debug.Log($"Camera initialized - Following: {player.name}, Offset: {offset}");
        }
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        // Calculate desired position starting with current position
        Vector3 desiredPosition = transform.position;

        // Apply offset and follow based on axis settings
        if (followX)
        {
            desiredPosition.x = player.position.x + offset.x;
        }

        if (followY)
        {
            desiredPosition.y = player.position.y + offset.y;
        }
        else
        {
            desiredPosition.y = fixedY;
        }

        if (followZ)
        {
            desiredPosition.z = player.position.z + offset.z;
        }
        else
        {
            desiredPosition.z = fixedZ;
        }

        // Apply bounds if enabled
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * smoothSpeed
        );

        transform.position = smoothedPosition;

        if (showDebugInfo)
        {
            Debug.DrawLine(transform.position, player.position, Color.cyan);
        }
    }

    // Public method to change follow target at runtime
    public void SetFollowTarget(Transform newTarget)
    {
        player = newTarget;
        if (showDebugInfo && newTarget != null)
        {
            Debug.Log($"Camera now following: {newTarget.name}");
        }
    }

    // Public method to update offset at runtime
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    // Snap camera to player position immediately (no smooth)
    public void SnapToPlayer()
    {
        if (player != null)
        {
            Vector3 snapPosition = transform.position;

            if (followX)
                snapPosition.x = player.position.x + offset.x;
            if (followY)
                snapPosition.y = player.position.y + offset.y;
            else
                snapPosition.y = fixedY;
            if (followZ)
                snapPosition.z = player.position.z + offset.z;
            else
                snapPosition.z = fixedZ;

            transform.position = snapPosition;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Draw line to player
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);

            // Draw target position
            Gizmos.color = Color.yellow;
            Vector3 targetPos = player.position + offset;
            Gizmos.DrawWireSphere(targetPos, 0.5f);
        }

        // Draw camera bounds if enabled
        if (useBounds)
        {
            Gizmos.color = Color.red;
            Vector3 bottomLeft = new Vector3(minX, minY, transform.position.z);
            Vector3 topRight = new Vector3(maxX, maxY, transform.position.z);
            Vector3 bottomRight = new Vector3(maxX, minY, transform.position.z);
            Vector3 topLeft = new Vector3(minX, maxY, transform.position.z);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}