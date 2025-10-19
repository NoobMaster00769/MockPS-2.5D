using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleParallax25D : MonoBehaviour
{
    [Header("Camera Selection")]
    [Tooltip("Leave empty to auto-find Main Camera")]
    public Camera targetCamera;

    [Tooltip("Auto-find camera by tag (e.g., 'MainCamera' or 'TopCamera')")]
    public string cameraTag = "";

    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    [Tooltip("0 = moves with camera (foreground), 1 = doesn't move (far background)")]
    public float parallaxStrength = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Vertical parallax effect")]
    public float verticalParallaxStrength = 0.5f;

    [Header("Infinite Scrolling")]
    public bool enableInfiniteScroll = false;
    [Tooltip("For multiple backgrounds, set this to create seamless loops")]
    public int scrollMultiplier = 2;

    [Header("Depth Settings")]
    public bool scaleWithDepth = false;
    [Range(0f, 1f)]
    public float depthScaleAmount = 0.2f;
    public float minDepth = -2f;
    public float maxDepth = 2f;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private Vector3 startPosition;
    private float spriteWidth;
    private SpriteRenderer spriteRenderer;
    private Vector3 initialScale;

    void Start()
    {
        // Find the target camera
        if (targetCamera == null)
        {
            if (!string.IsNullOrEmpty(cameraTag))
            {
                // Find camera by tag
                GameObject camObj = GameObject.FindGameObjectWithTag(cameraTag);
                if (camObj != null)
                {
                    targetCamera = camObj.GetComponent<Camera>();
                    Debug.Log($"Parallax on {gameObject.name} found camera by tag: {cameraTag}");
                }
            }

            // Fallback to main camera
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        if (targetCamera == null)
        {
            Debug.LogError($"SimpleParallax25D on {gameObject.name}: No camera found!");
            enabled = false;
            return;
        }

        cameraTransform = targetCamera.transform;
        lastCameraPosition = cameraTransform.position;
        startPosition = transform.position;
        initialScale = transform.localScale;

        // Get sprite width for infinite scrolling
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteWidth = spriteRenderer.bounds.size.x;
        }
        else
        {
            Debug.LogWarning("SimpleParallax25D: No SpriteRenderer found on " + gameObject.name);
        }

        Debug.Log($"Parallax initialized on {gameObject.name} - Camera: {targetCamera.name}, Strength: {parallaxStrength}");
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Calculate camera movement since last frame
        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        // Apply parallax - objects move LESS than the camera based on parallaxStrength
        // Higher parallaxStrength = slower movement = further away
        float moveAmountX = cameraDelta.x * (1f - parallaxStrength);
        float moveAmountY = cameraDelta.y * (1f - verticalParallaxStrength);

        // Move the parallax layer
        Vector3 newPosition = transform.position;
        newPosition.x += moveAmountX;
        newPosition.y += moveAmountY;
        transform.position = newPosition;

        // Infinite scrolling (horizontal only)
        if (enableInfiniteScroll && spriteWidth > 0)
        {
            float distanceFromStart = transform.position.x - startPosition.x;
            float scrollDistance = spriteWidth * scrollMultiplier;

            // If the sprite has moved too far, wrap it
            if (distanceFromStart > scrollDistance)
            {
                Vector3 pos = transform.position;
                pos.x -= scrollDistance;
                transform.position = pos;
                startPosition.x = pos.x;
            }
            else if (distanceFromStart < -scrollDistance)
            {
                Vector3 pos = transform.position;
                pos.x += scrollDistance;
                transform.position = pos;
                startPosition.x = pos.x;
            }
        }

        // Scale based on depth (optional pseudo-3D effect)
        if (scaleWithDepth)
        {
            float depthPercent = (transform.position.z - minDepth) / (maxDepth - minDepth);
            depthPercent = Mathf.Clamp01(depthPercent);
            float scaleMultiplier = 1f - (depthPercent * depthScaleAmount);
            transform.localScale = initialScale * scaleMultiplier;
        }

        lastCameraPosition = cameraTransform.position;
    }

    // Reset parallax to starting position
    public void ResetPosition()
    {
        transform.position = startPosition;
        if (cameraTransform != null)
        {
            lastCameraPosition = cameraTransform.position;
        }
    }

    // Adjust parallax strength at runtime
    public void SetParallaxStrength(float horizontal, float vertical)
    {
        parallaxStrength = Mathf.Clamp01(horizontal);
        verticalParallaxStrength = Mathf.Clamp01(vertical);
    }

    // Change target camera at runtime
    public void SetTargetCamera(Camera cam)
    {
        targetCamera = cam;
        if (cam != null)
        {
            cameraTransform = cam.transform;
            lastCameraPosition = cameraTransform.position;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spriteRenderer != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 pos = transform.position;
            Gizmos.DrawWireCube(pos, spriteRenderer.bounds.size);

            if (enableInfiniteScroll && spriteWidth > 0)
            {
                Gizmos.color = Color.yellow;
                float scrollDist = spriteWidth * scrollMultiplier;

                // Draw wrap boundaries
                Gizmos.DrawWireCube(new Vector3(startPosition.x + scrollDist, pos.y, pos.z),
                                   spriteRenderer.bounds.size);
                Gizmos.DrawWireCube(new Vector3(startPosition.x - scrollDist, pos.y, pos.z),
                                   spriteRenderer.bounds.size);

                // Draw start position
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(startPosition, 0.3f);
            }

            // Draw parallax info
            if (Application.isPlaying && cameraTransform != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(pos, cameraTransform.position);
            }
        }
    }
}