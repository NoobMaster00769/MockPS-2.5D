using UnityEngine;

public class TopCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform invertedPlayer;     // The inverted (ceiling) player
    public Transform topBackground;      // Top parallax background

    [Header("Offsets & Smoothing")]
    public Vector3 cameraOffset = new Vector3(0, -2, -10); // Usually inverted vertically
    public float smoothSpeed = 5f;
    public float parallaxFactor = 0.5f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (!invertedPlayer) return;

        // Smooth follow the inverted player
        Vector3 targetPos = invertedPlayer.position + cameraOffset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 1f / smoothSpeed);

        // Parallax
        if (topBackground)
        {
            Vector3 bgPos = topBackground.position;
            bgPos.x = transform.position.x * parallaxFactor;
            topBackground.position = bgPos;
        }
    }
}
