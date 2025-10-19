using UnityEngine;

/// <summary>
/// Vertical split-screen that SHARES backgrounds between both cameras.
/// Uses Camera Culling Masks and Layers instead of duplicating backgrounds.
/// </summary>
public class VerticalSplitScreenManager : MonoBehaviour
{
    [Header("Camera Setup")]
    [Tooltip("Camera for top player (inverted)")]
    public Camera topCamera;

    [Tooltip("Camera for bottom player (main)")]
    public Camera bottomCamera;

    [Header("Player References")]
    [Tooltip("Top player to follow")]
    public Transform topPlayer;

    [Tooltip("Bottom player to follow")]
    public Transform bottomPlayer;

    [Header("Split Settings")]
    [Range(0.3f, 0.7f)]
    [Tooltip("How much screen the top camera gets (0.5 = equal split)")]
    public float topScreenRatio = 0.5f;

    [Tooltip("Gap between screens in pixels")]
    public float dividerGap = 2f;

    [Header("Camera Settings")]
    [Tooltip("How much of the scene to show (higher = more zoomed out)")]
    public float orthographicSize = 10f;

    [Tooltip("Fixed Y position for top camera")]
    public float topCameraYPosition = 8f;

    [Tooltip("Fixed Y position for bottom camera")]
    public float bottomCameraYPosition = 0f;

    [Tooltip("Camera Z distance (negative = away from scene)")]
    public float cameraZDistance = -10f;

    [Header("Camera Rotation (for 2.5D depth)")]
    [Tooltip("Rotate top camera on X axis to show platform depth")]
    [Range(-15f, 15f)]
    public float topCameraXRotation = 5f;

    [Tooltip("Rotate bottom camera on X axis to show platform depth")]
    [Range(-15f, 15f)]
    public float bottomCameraXRotation = -5f;

    [Tooltip("Apply Y rotation for angled perspective")]
    [Range(-10f, 10f)]
    public float cameraYRotation = 0f;

    [Header("Camera Follow")]
    [Tooltip("Smooth speed for horizontal following")]
    public float smoothSpeed = 5f;

    [Tooltip("Horizontal offset from player")]
    public float xOffset = 0f;

    [Tooltip("Enable camera bounds")]
    public bool useCameraBounds = false;
    public float minX = -50f;
    public float maxX = 50f;

    [Header("Shared Background Settings")]
    [Tooltip("The shared background parallax system - both cameras will use this")]
    public Transform sharedBackgroundParent;

    [Tooltip("Auto-find backgrounds on start")]
    public bool autoFindBackgrounds = true;

    [Tooltip("FORCE parallax to follow bottom camera only (recommended for synchronized backgrounds)")]
    public bool forceBottomCameraParallax = true;

    [Tooltip("Parallax follows which camera? (ignored if forceBottomCameraParallax is true)")]
    public ParallaxFollowMode parallaxFollowMode = ParallaxFollowMode.BottomCamera;

    [Header("Visual")]
    public bool showDividerLine = true;
    public Color dividerColor = Color.white;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showCameraGizmos = true;

    private SimpleParallax25D[] sharedParallaxLayers;
    private bool isInitialized = false;

    public enum ParallaxFollowMode
    {
        BottomCamera,
        TopCamera,
        MidpointBetweenCameras
    }

    void Start()
    {
        SetupCameras();
        SetupSharedBackgrounds();
        isInitialized = true;

        Debug.Log("VerticalSplitScreenManager initialized with SHARED backgrounds!");
    }

    void SetupCameras()
    {
        // AUTO-CREATE CAMERAS IF MISSING
        if (topCamera == null)
        {
            GameObject topCamObj = new GameObject("TopCamera");
            topCamera = topCamObj.AddComponent<Camera>();
            Debug.Log("Created TopCamera automatically");
        }

        if (bottomCamera == null)
        {
            GameObject bottomCamObj = new GameObject("BottomCamera");
            bottomCamera = bottomCamObj.AddComponent<Camera>();
            Debug.Log("Created BottomCamera automatically");
        }

        // CONFIGURE VIEWPORTS
        UpdateViewports();

        // SET CAMERA DEPTHS
        topCamera.depth = 1;
        bottomCamera.depth = 0;

        // CONFIGURE CAMERA SETTINGS - Both render everything (no layer culling)
        topCamera.orthographic = true;
        topCamera.orthographicSize = orthographicSize;
        topCamera.cullingMask = -1; // Render everything

        bottomCamera.orthographic = true;
        bottomCamera.orthographicSize = orthographicSize;
        bottomCamera.cullingMask = -1; // Render everything

        // Set initial positions
        topCamera.transform.position = new Vector3(
            topPlayer != null ? topPlayer.position.x : 0f,
            topCameraYPosition,
            cameraZDistance
        );

        bottomCamera.transform.position = new Vector3(
            bottomPlayer != null ? bottomPlayer.position.x : 0f,
            bottomCameraYPosition,
            cameraZDistance
        );

        // Enable cameras
        topCamera.enabled = true;
        bottomCamera.enabled = true;

        // Match background colors
        topCamera.backgroundColor = bottomCamera.backgroundColor;

        // Set camera rotations for depth perception
        topCamera.transform.rotation = Quaternion.Euler(topCameraXRotation, cameraYRotation, 0);
        bottomCamera.transform.rotation = Quaternion.Euler(bottomCameraXRotation, cameraYRotation, 0);

        Debug.Log($"Cameras configured - Orthographic Size: {orthographicSize}");
        Debug.Log($"Top Y: {topCameraYPosition}, Bottom Y: {bottomCameraYPosition}");
        Debug.Log($"Top Rotation: {topCameraXRotation}°, Bottom Rotation: {bottomCameraXRotation}°");
    }

    void UpdateViewports()
    {
        float dividerOffset = dividerGap / Screen.height;

        // TOP CAMERA - upper portion
        topCamera.rect = new Rect(
            0,
            topScreenRatio + dividerOffset,
            1,
            (1f - topScreenRatio) - dividerOffset
        );

        // BOTTOM CAMERA - lower portion
        bottomCamera.rect = new Rect(
            0,
            0,
            1,
            topScreenRatio - dividerOffset
        );
    }

    void SetupSharedBackgrounds()
    {
        // Find shared background parent
        if (sharedBackgroundParent == null && autoFindBackgrounds)
        {
            // Try common background parent names
            GameObject bgObj = GameObject.Find("Background") ??
                             GameObject.Find("Backgrounds") ??
                             GameObject.Find("TopBackground") ??
                             GameObject.Find("BottomBackground");

            if (bgObj != null)
            {
                sharedBackgroundParent = bgObj.transform;
                Debug.Log($"Auto-found background parent: {bgObj.name}");
            }
        }

        // Get all parallax layers
        if (sharedBackgroundParent != null)
        {
            sharedParallaxLayers = sharedBackgroundParent.GetComponentsInChildren<SimpleParallax25D>();
            Debug.Log($"Found {sharedParallaxLayers.Length} shared parallax layers");
        }
        else
        {
            // Find all parallax in scene
            sharedParallaxLayers = FindObjectsOfType<SimpleParallax25D>();
            Debug.Log($"Found {sharedParallaxLayers.Length} parallax layers in scene");
        }

        // Configure parallax to follow the chosen camera
        ConfigureSharedParallax();
    }

    void ConfigureSharedParallax()
    {
        if (sharedParallaxLayers == null || sharedParallaxLayers.Length == 0)
        {
            Debug.LogWarning("No parallax layers found!");
            return;
        }

        Camera targetCamera = GetParallaxTargetCamera();

        foreach (var parallax in sharedParallaxLayers)
        {
            if (parallax != null)
            {
                parallax.SetTargetCamera(targetCamera);
            }
        }

        Debug.Log($"Configured {sharedParallaxLayers.Length} parallax layers to follow {targetCamera.name}");
    }

    Camera GetParallaxTargetCamera()
    {
        switch (parallaxFollowMode)
        {
            case ParallaxFollowMode.TopCamera:
                return topCamera;
            case ParallaxFollowMode.BottomCamera:
                return bottomCamera;
            case ParallaxFollowMode.MidpointBetweenCameras:
                return bottomCamera; // Default to bottom for now
            default:
                return bottomCamera;
        }
    }

    void LateUpdate()
    {
        if (!isInitialized)
            return;

        // Update camera orthographic size
        topCamera.orthographicSize = orthographicSize;
        bottomCamera.orthographicSize = orthographicSize;

        // Update camera rotations
        topCamera.transform.rotation = Quaternion.Euler(topCameraXRotation, cameraYRotation, 0);
        bottomCamera.transform.rotation = Quaternion.Euler(bottomCameraXRotation, cameraYRotation, 0);

        // Follow players ONLY on X axis
        UpdateCameraPositionXOnly(topCamera, topPlayer, topCameraYPosition);
        UpdateCameraPositionXOnly(bottomCamera, bottomPlayer, bottomCameraYPosition);

        // If using midpoint mode, update parallax camera position
        if (parallaxFollowMode == ParallaxFollowMode.MidpointBetweenCameras)
        {
            UpdateMidpointParallax();
        }
    }

    void UpdateCameraPositionXOnly(Camera cam, Transform target, float fixedY)
    {
        if (cam == null || target == null)
            return;

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

        // Set position: smooth X, fixed Y and Z
        cam.transform.position = new Vector3(smoothedX, fixedY, cameraZDistance);
    }

    void UpdateMidpointParallax()
    {
        if (sharedParallaxLayers == null || sharedParallaxLayers.Length == 0)
            return;

        // Calculate midpoint X between both cameras
        float midpointX = (topCamera.transform.position.x + bottomCamera.transform.position.x) / 2f;

        // Create virtual camera position for parallax
        Vector3 virtualCamPos = new Vector3(
            midpointX,
            (topCameraYPosition + bottomCameraYPosition) / 2f,
            cameraZDistance
        );

        // Update parallax based on virtual position
        // Note: This would require custom parallax handling
    }

    void OnGUI()
    {
        if (!showDividerLine)
            return;

        // Draw divider line
        float dividerY = Screen.height * topScreenRatio;
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

            GUI.Label(new Rect(10, dividerY - 25, 400, 20),
                $"TOP VIEW - Y: {topCameraYPosition} | Zoom: {orthographicSize}", style);

            GUI.Label(new Rect(10, dividerY + 5, 400, 20),
                $"BOTTOM VIEW - Y: {bottomCameraYPosition} | Parallax: {parallaxFollowMode}", style);
        }
    }

    // PUBLIC METHODS

    public void SetCameraRotations(float topX, float bottomX, float y = 0f)
    {
        topCameraXRotation = Mathf.Clamp(topX, -15f, 15f);
        bottomCameraXRotation = Mathf.Clamp(bottomX, -15f, 15f);
        cameraYRotation = Mathf.Clamp(y, -10f, 10f);

        if (topCamera != null)
            topCamera.transform.rotation = Quaternion.Euler(topCameraXRotation, cameraYRotation, 0);
        if (bottomCamera != null)
            bottomCamera.transform.rotation = Quaternion.Euler(bottomCameraXRotation, cameraYRotation, 0);

        Debug.Log($"Camera rotations updated - Top: {topX}°, Bottom: {bottomX}°, Y: {y}°");
    }

    public void SetOrthographicSize(float size)
    {
        orthographicSize = Mathf.Max(1f, size);
        if (topCamera != null) topCamera.orthographicSize = orthographicSize;
        if (bottomCamera != null) bottomCamera.orthographicSize = orthographicSize;
        Debug.Log($"Orthographic size set to: {orthographicSize}");
    }

    public void SetCameraHeights(float topY, float bottomY)
    {
        topCameraYPosition = topY;
        bottomCameraYPosition = bottomY;
        Debug.Log($"Camera heights updated - Top: {topY}, Bottom: {bottomY}");
    }

    public void SetSplitRatio(float ratio)
    {
        topScreenRatio = Mathf.Clamp(ratio, 0.3f, 0.7f);
        UpdateViewports();
    }

    public void SetParallaxFollowMode(ParallaxFollowMode mode)
    {
        parallaxFollowMode = mode;
        ConfigureSharedParallax();
        Debug.Log($"Parallax follow mode set to: {mode}");
    }

    public void RefreshBackgrounds()
    {
        SetupSharedBackgrounds();
        Debug.Log("Backgrounds refreshed");
    }

    void OnValidate()
    {
        if (topCamera != null && bottomCamera != null)
        {
            UpdateViewports();
            topCamera.orthographicSize = orthographicSize;
            bottomCamera.orthographicSize = orthographicSize;
            topCamera.transform.rotation = Quaternion.Euler(topCameraXRotation, cameraYRotation, 0);
            bottomCamera.transform.rotation = Quaternion.Euler(bottomCameraXRotation, cameraYRotation, 0);
        }
    }

    void OnDrawGizmos()
    {
        if (!showCameraGizmos || !Application.isPlaying)
            return;

        // Draw camera view frustums
        if (topCamera != null)
        {
            Gizmos.color = Color.cyan;
            DrawCameraGizmo(topCamera, topCameraYPosition);
        }

        if (bottomCamera != null)
        {
            Gizmos.color = Color.yellow;
            DrawCameraGizmo(bottomCamera, bottomCameraYPosition);
        }

        // Draw fixed Y lines
        Gizmos.color = Color.red;
        float lineLength = 100f;
        Gizmos.DrawLine(
            new Vector3(-lineLength, topCameraYPosition, 0),
            new Vector3(lineLength, topCameraYPosition, 0)
        );
        Gizmos.DrawLine(
            new Vector3(-lineLength, bottomCameraYPosition, 0),
            new Vector3(lineLength, bottomCameraYPosition, 0)
        );

        // Draw shared background parent
        if (sharedBackgroundParent != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(sharedBackgroundParent.position, 1f);
        }
    }

    void DrawCameraGizmo(Camera cam, float yPos)
    {
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        Vector3 center = new Vector3(cam.transform.position.x, yPos, 0);

        Vector3 topLeft = center + new Vector3(-width / 2f, height / 2f, 0);
        Vector3 topRight = center + new Vector3(width / 2f, height / 2f, 0);
        Vector3 bottomLeft = center + new Vector3(-width / 2f, -height / 2f, 0);
        Vector3 bottomRight = center + new Vector3(width / 2f, -height / 2f, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}