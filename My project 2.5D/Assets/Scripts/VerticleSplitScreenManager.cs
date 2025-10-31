using UnityEngine;

/// <summary>
/// Vertical split-screen manager - MANUAL CAMERA VERSION
/// Assign your manually created cameras in the Inspector
/// Supports mirror background setup with parallax
/// </summary>
public class VerticalSplitScreenManager : MonoBehaviour
{
    [Header("⚙️ MANUAL CAMERA ASSIGNMENT")]
    [Tooltip("Drag your manually created TopCamera here")]
    public Camera topCamera;

    [Tooltip("Drag your manually created BottomCamera here")]
    public Camera bottomCamera;

    [Header("Player References")]
    [Tooltip("Top player to follow (InvertedPlayer)")]
    public Transform topPlayer;

    [Tooltip("Bottom player to follow (Main Player)")]
    public Transform bottomPlayer;

    [Header("Split Settings")]
    [Range(0.3f, 0.7f)]
    [Tooltip("How much screen the bottom gets (0.5 = equal split)")]
    public float bottomScreenRatio = 0.5f;

    [Tooltip("Gap between screens in pixels")]
    public float dividerGap = 2f;

    [Header("Camera Follow")]
    [Tooltip("Smooth speed for horizontal following")]
    public float smoothSpeed = 5f;

    [Tooltip("Horizontal offset from player")]
    public float xOffset = 0f;

    [Tooltip("Enable camera bounds")]
    public bool useCameraBounds = false;
    public float minX = -50f;
    public float maxX = 50f;

    [Header("Background Settings")]
    [Tooltip("Bottom level background parent")]
    public Transform bottomBackgroundParent;

    [Tooltip("Top level background parent (mirrored)")]
    public Transform topBackgroundParent;

    [Tooltip("Auto-find backgrounds on start")]
    public bool autoFindBackgrounds = true;

    [Header("Parallax Configuration")]
    [Tooltip("Which camera should bottom parallax follow?")]
    public ParallaxFollowMode bottomParallaxFollows = ParallaxFollowMode.BottomCamera;

    [Tooltip("Which camera should top parallax follow?")]
    public ParallaxFollowMode topParallaxFollows = ParallaxFollowMode.TopCamera;

    [Header("Visual")]
    public bool showDividerLine = true;
    public Color dividerColor = Color.white;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showCameraGizmos = true;

    private SimpleParallax25D[] bottomParallaxLayers;
    private SimpleParallax25D[] topParallaxLayers;
    private bool isInitialized = false;

    public enum ParallaxFollowMode
    {
        BottomCamera,
        TopCamera,
        None
    }

    void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        UpdateViewports();
        SetupBackgrounds();
        isInitialized = true;

        if (showDebugInfo)
        {
            Debug.Log("=== VerticalSplitScreenManager Initialized ===");
            Debug.Log($"Top Camera: {topCamera.name} | Position: {topCamera.transform.position}");
            Debug.Log($"Bottom Camera: {bottomCamera.name} | Position: {bottomCamera.transform.position}");
        }
    }

    bool ValidateSetup()
    {
        bool valid = true;

        if (topCamera == null)
        {
            Debug.LogError("VerticalSplitScreenManager: TopCamera not assigned! Please assign it in Inspector.");
            valid = false;
        }

        if (bottomCamera == null)
        {
            Debug.LogError("VerticalSplitScreenManager: BottomCamera not assigned! Please assign it in Inspector.");
            valid = false;
        }

        if (topPlayer == null)
        {
            Debug.LogWarning("VerticalSplitScreenManager: Top player not assigned. Camera won't follow.");
        }

        if (bottomPlayer == null)
        {
            Debug.LogWarning("VerticalSplitScreenManager: Bottom player not assigned. Camera won't follow.");
        }

        return valid;
    }

    void UpdateViewports()
    {
        if (topCamera == null || bottomCamera == null) return;

        float dividerOffset = dividerGap / Screen.height;

        // BOTTOM CAMERA - lower portion
        bottomCamera.rect = new Rect(
            0,
            0,
            1,
            bottomScreenRatio - dividerOffset
        );

        // TOP CAMERA - upper portion
        topCamera.rect = new Rect(
            0,
            bottomScreenRatio + dividerOffset,
            1,
            (1f - bottomScreenRatio) - dividerOffset
        );

        // Set camera depths
        topCamera.depth = 1;
        bottomCamera.depth = 0;

        if (showDebugInfo)
        {
            Debug.Log($"Viewports updated - Bottom: {bottomScreenRatio * 100:F1}% | Top: {(1f - bottomScreenRatio) * 100:F1}%");
        }
    }

    void SetupBackgrounds()
    {
        if (autoFindBackgrounds)
        {
            // Auto-find bottom background
            if (bottomBackgroundParent == null)
            {
                GameObject bgObj = GameObject.Find("Background");
                if (bgObj != null)
                {
                    bottomBackgroundParent = bgObj.transform;
                    Debug.Log($"Auto-found bottom background: {bgObj.name}");
                }
            }

            // Auto-find top background
            if (topBackgroundParent == null)
            {
                GameObject bgTopObj = GameObject.Find("BackgroundForTop") ??
                                      GameObject.Find("Background_TopLevel") ??
                                      GameObject.Find("BackGroundTop");
                if (bgTopObj != null)
                {
                    topBackgroundParent = bgTopObj.transform;
                    Debug.Log($"Auto-found top background: {bgTopObj.name}");
                }
            }
        }

        // Setup bottom parallax
        if (bottomBackgroundParent != null)
        {
            bottomParallaxLayers = bottomBackgroundParent.GetComponentsInChildren<SimpleParallax25D>();

            if (bottomParallaxLayers.Length > 0)
            {
                Camera targetCam = GetTargetCamera(bottomParallaxFollows);
                if (targetCam != null)
                {
                    foreach (var parallax in bottomParallaxLayers)
                    {
                        if (parallax != null)
                        {
                            parallax.SetTargetCamera(targetCam);
                        }
                    }
                    Debug.Log($"Bottom parallax: {bottomParallaxLayers.Length} layers following {targetCam.name}");
                }
            }
        }

        // Setup top parallax
        if (topBackgroundParent != null)
        {
            topParallaxLayers = topBackgroundParent.GetComponentsInChildren<SimpleParallax25D>();

            if (topParallaxLayers.Length > 0)
            {
                Camera targetCam = GetTargetCamera(topParallaxFollows);
                if (targetCam != null)
                {
                    foreach (var parallax in topParallaxLayers)
                    {
                        if (parallax != null)
                        {
                            parallax.SetTargetCamera(targetCam);
                        }
                    }
                    Debug.Log($"Top parallax: {topParallaxLayers.Length} layers following {targetCam.name}");
                }
            }
        }
    }

    Camera GetTargetCamera(ParallaxFollowMode mode)
    {
        switch (mode)
        {
            case ParallaxFollowMode.TopCamera:
                return topCamera;
            case ParallaxFollowMode.BottomCamera:
                return bottomCamera;
            case ParallaxFollowMode.None:
                return null;
            default:
                return bottomCamera;
        }
    }

    void LateUpdate()
    {
        if (!isInitialized) return;

        // Update viewports (handles window resize)
        UpdateViewports();

        // Follow players on X axis only
        if (bottomCamera != null && bottomPlayer != null)
        {
            UpdateCameraPositionXOnly(bottomCamera, bottomPlayer);
        }

        if (topCamera != null && topPlayer != null)
        {
            UpdateCameraPositionXOnly(topCamera, topPlayer);
        }
    }

    void UpdateCameraPositionXOnly(Camera cam, Transform target)
    {
        if (cam == null || target == null) return;

        // Calculate desired X position
        float desiredX = target.position.x + xOffset;

        // Apply bounds if enabled
        if (useCameraBounds)
        {
            desiredX = Mathf.Clamp(desiredX, minX, maxX);
        }

        // Smooth horizontal follow
        float currentX = cam.transform.position.x;
        float smoothedX = Mathf.Lerp(currentX, desiredX, Time.deltaTime * smoothSpeed);

        // Update position (only X changes, Y and Z stay fixed)
        Vector3 newPos = cam.transform.position;
        newPos.x = smoothedX;
        cam.transform.position = newPos;
    }

    void OnGUI()
    {
        if (!showDividerLine) return;

        // Draw divider line
        float dividerY = Screen.height * bottomScreenRatio;
        Rect dividerRect = new Rect(0, dividerY - dividerGap / 2f, Screen.width, dividerGap);

        Texture2D dividerTexture = new Texture2D(1, 1);
        dividerTexture.SetPixel(0, 0, dividerColor);
        dividerTexture.Apply();

        GUI.DrawTexture(dividerRect, dividerTexture);

        if (showDebugInfo)
        {
            GUIStyle style = new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.white },
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            string topInfo = topCamera != null ?
                $"TOP VIEW - {topCamera.name} | Rot: {topCamera.transform.rotation.eulerAngles}" :
                "TOP: NOT ASSIGNED";

            string bottomInfo = bottomCamera != null ?
                $"BOTTOM VIEW - {bottomCamera.name} | Rot: {bottomCamera.transform.rotation.eulerAngles}" :
                "BOTTOM: NOT ASSIGNED";

            GUI.Label(new Rect(10, dividerY - 30, 700, 20), topInfo, style);
            GUI.Label(new Rect(10, dividerY + 10, 700, 20), bottomInfo, style);
        }
    }

    // PUBLIC METHODS

    public void SetSplitRatio(float ratio)
    {
        bottomScreenRatio = Mathf.Clamp(ratio, 0.3f, 0.7f);
        UpdateViewports();
        Debug.Log($"Split ratio set to: {bottomScreenRatio * 100:F1}%");
    }

    public void RefreshBackgrounds()
    {
        SetupBackgrounds();
        Debug.Log("Backgrounds refreshed!");
    }

    public void SetBottomParallaxFollow(ParallaxFollowMode mode)
    {
        bottomParallaxFollows = mode;

        if (bottomParallaxLayers != null)
        {
            Camera targetCam = GetTargetCamera(mode);
            if (targetCam != null)
            {
                foreach (var parallax in bottomParallaxLayers)
                {
                    if (parallax != null) parallax.SetTargetCamera(targetCam);
                }
            }
        }

        Debug.Log($"Bottom parallax now follows: {mode}");
    }

    public void SetTopParallaxFollow(ParallaxFollowMode mode)
    {
        topParallaxFollows = mode;

        if (topParallaxLayers != null)
        {
            Camera targetCam = GetTargetCamera(mode);
            if (targetCam != null)
            {
                foreach (var parallax in topParallaxLayers)
                {
                    if (parallax != null) parallax.SetTargetCamera(targetCam);
                }
            }
        }

        Debug.Log($"Top parallax now follows: {mode}");
    }

    void OnValidate()
    {
        if (Application.isPlaying && isInitialized)
        {
            UpdateViewports();
        }
    }

    void OnDrawGizmos()
    {
        if (!showCameraGizmos) return;

        // Draw camera positions and directions
        if (bottomCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bottomCamera.transform.position, 0.8f);
            Gizmos.DrawRay(bottomCamera.transform.position, bottomCamera.transform.forward * 5f);
        }

        if (topCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(topCamera.transform.position, 0.8f);
            Gizmos.DrawRay(topCamera.transform.position, topCamera.transform.forward * 5f);
        }

        // Draw background parent positions
        if (bottomBackgroundParent != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bottomBackgroundParent.position, Vector3.one * 2f);
        }

        if (topBackgroundParent != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(topBackgroundParent.position, Vector3.one * 2f);
        }

        // Draw players
        if (bottomPlayer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bottomPlayer.position, Vector3.one * 0.5f);
        }

        if (topPlayer != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(topPlayer.position, Vector3.one * 0.5f);
        }
    }
}