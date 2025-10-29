using UnityEngine;

public class InfiniteBackgroundScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private float parallaxFactor = 0.5f; // 0 = no movement, 1 = moves with camera
    [SerializeField] private float resetThreshold = 50f; // Distance before repositioning

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Scroll Bounds (Manually Set)")]
    [SerializeField] private float backgroundWidth = 30f; // Adjust based on your background width

    private Vector3 startPosition;
    private float lastCameraX;

    void Start()
    {
        // Find main camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        startPosition = transform.position;

        if (cameraTransform != null)
        {
            lastCameraX = cameraTransform.position.x;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Calculate camera movement
        float cameraDeltaX = cameraTransform.position.x - lastCameraX;
        lastCameraX = cameraTransform.position.x;

        // Move background based on parallax factor
        Vector3 newPos = transform.position;
        newPos.x += cameraDeltaX * parallaxFactor;
        transform.position = newPos;

        // Check if we need to reposition for looping
        float offset = transform.position.x - startPosition.x;

        // If moved too far right, shift left
        if (offset > backgroundWidth)
        {
            newPos.x -= backgroundWidth * 2f;
            transform.position = newPos;
            startPosition.x -= backgroundWidth * 2f;
        }
        // If moved too far left, shift right
        else if (offset < -backgroundWidth)
        {
            newPos.x += backgroundWidth * 2f;
            transform.position = newPos;
            startPosition.x += backgroundWidth * 2f;
        }
    }
}