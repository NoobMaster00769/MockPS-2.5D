using UnityEngine;

/// <summary>
/// Enhanced parallax scrolling for 2.5D split-screen setup
/// Follows camera horizontally with parallax effect
/// Works with VerticalSplitScreenManager
/// </summary>
public class EnhancedParallax25D : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("Parallax strength (0 = static background, 1 = moves with camera)")]
    [Range(0f, 1f)]
    public float parallaxStrength = 0.5f;

    [Tooltip("Enable horizontal parallax")]
    public bool parallaxX = true;

    [Tooltip("Enable vertical parallax")]
    public bool parallaxY = false;

    [Header("Auto Depth Calculation")]
    [Tooltip("Auto-calculate parallax from Z distance")]
    public bool autoCalculateFromDepth = true;

    [Tooltip("Depth range for calculation (further = less movement)")]
    public float depthRange = 20f;

    [Header("Reference")]
    [Tooltip("Target camera to follow (set by manager)")]
    public Camera targetCamera;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showGizmos = false;

    private Vector3 previousCameraPosition;
    private float calculatedParallaxStrength;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (targetCamera == null)
        {
            // Try to find main camera as fallback
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogWarning($"EnhancedParallax25D ({gameObject.name}): No target camera assigned!");
                return;
            }
        }

        // Calculate parallax strength
        if (autoCalculateFromDepth)
        {
            CalculateParallaxFromDepth();
        }
        else
        {
            calculatedParallaxStrength = parallaxStrength;
        }

        // Store initial camera position
        previousCameraPosition = targetCamera.transform.position;
        isInitialized = true;

        if (showDebugInfo)
        {
            Debug.Log($"EnhancedParallax25D ({gameObject.name}): Initialized with camera {targetCamera.name}, strength = {calculatedParallaxStrength:F2}");
        }
    }

    void CalculateParallaxFromDepth()
    {
        if (targetCamera == null) return;

        // Calculate distance from camera
        float layerZ = transform.position.z;
        float cameraZ = targetCamera.transform.position.z;
        float distance = Mathf.Abs(layerZ - cameraZ);

        // Normalize: 0-1 based on depth range
        float normalizedDistance = Mathf.Clamp01(distance / depthRange);

        // Closer objects move more (higher parallax)
        // Further objects move less (lower parallax)
        calculatedParallaxStrength = 1f - normalizedDistance;

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Distance={distance:F1}, Parallax={calculatedParallaxStrength:F2}");
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || targetCamera == null)
        {
            return;
        }

        // Get camera movement since last frame
        Vector3 currentCameraPosition = targetCamera.transform.position;
        Vector3 cameraMovement = currentCameraPosition - previousCameraPosition;

        // Calculate parallax offset
        Vector3 parallaxOffset = Vector3.zero;

        if (parallaxX)
        {
            parallaxOffset.x = cameraMovement.x * calculatedParallaxStrength;
        }

        if (parallaxY)
        {
            parallaxOffset.y = cameraMovement.y * calculatedParallaxStrength;
        }

        // Apply parallax movement
        transform.position += parallaxOffset;

        // Update previous position
        previousCameraPosition = currentCameraPosition;

        if (showDebugInfo && parallaxOffset.magnitude > 0.001f)
        {
            Debug.Log($"{gameObject.name}: Camera moved {cameraMovement.x:F3}, BG moved {parallaxOffset.x:F3}");
        }
    }

    // PUBLIC METHODS

    /// <summary>
    /// Set the camera this parallax should follow
    /// Called by VerticalSplitScreenManager
    /// </summary>
    public void SetTargetCamera(Camera camera)
    {
        if (camera == null)
        {
            Debug.LogWarning($"EnhancedParallax25D ({gameObject.name}): Attempted to set null camera!");
            return;
        }

        targetCamera = camera;

        if (isInitialized)
        {
            previousCameraPosition = camera.transform.position;

            if (autoCalculateFromDepth)
            {
                CalculateParallaxFromDepth();
            }
        }
        else
        {
            Initialize();
        }

        if (showDebugInfo)
        {
            Debug.Log($"EnhancedParallax25D ({gameObject.name}): Target camera set to {camera.name}");
        }
    }

    /// <summary>
    /// Manually set parallax strength
    /// </summary>
    public void SetParallaxStrength(float strength)
    {
        parallaxStrength = Mathf.Clamp01(strength);
        if (!autoCalculateFromDepth)
        {
            calculatedParallaxStrength = parallaxStrength;
        }
    }

    /// <summary>
    /// Recalculate parallax strength from depth
    /// </summary>
    public void RecalculateFromDepth()
    {
        if (autoCalculateFromDepth)
        {
            CalculateParallaxFromDepth();
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Draw connection to camera
        if (targetCamera != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.5f); // Cyan
            Gizmos.DrawLine(transform.position, targetCamera.transform.position);
        }

        // Draw parallax strength indicator
        if (Application.isPlaying)
        {
            float strength = isInitialized ? calculatedParallaxStrength : parallaxStrength;
            Gizmos.color = Color.Lerp(Color.red, Color.green, strength);
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw label with strength
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"P: {strength:F2}"
            );
#endif
        }
    }

    void OnValidate()
    {
        // Update in editor when values change
        if (Application.isPlaying && autoCalculateFromDepth && targetCamera != null)
        {
            CalculateParallaxFromDepth();
        }
    }
}