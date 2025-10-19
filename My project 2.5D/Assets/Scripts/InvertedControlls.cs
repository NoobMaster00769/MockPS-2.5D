using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates a fully inverted mirror of the player character
/// Inverts ALL movements: left/right, up/down, forward/back
/// Place this on the inverted/mirrored player GameObject
/// </summary>
public class InvertedPlayerMirror : MonoBehaviour
{
    [Header("Mirror Settings")]
    [Tooltip("The original player to mirror")]
    public Transform originalPlayer;

    [Tooltip("The vertical axis line where mirroring occurs (Y position)")]
    public float mirrorAxisY = 0f;

    [Tooltip("The horizontal axis line where X mirroring occurs")]
    public float mirrorAxisX = 0f;

    [Tooltip("The depth axis line where Z mirroring occurs")]
    public float mirrorAxisZ = 0f;

    [Tooltip("Auto-find player with tag 'Player'")]
    public bool autoFindPlayer = true;

    [Header("Inversion Options")]
    [Tooltip("Invert horizontal movement (left becomes right)")]
    public bool invertX = true;

    [Tooltip("Invert vertical movement (up becomes down) - should be true")]
    public bool invertY = true;

    [Tooltip("Invert depth movement (forward becomes back)")]
    public bool invertZ = true;

    [Tooltip("Invert sprite flip direction")]
    public bool invertSpriteFlip = true;

    [Header("Animation Sync")]
    [Tooltip("Sync animations with original player")]
    public bool syncAnimations = true;

    [Tooltip("Invert forward/backward animation states")]
    public bool invertDepthAnimations = true;

    [Header("Visual Settings")]
    [Tooltip("Apply visual effect to distinguish mirror (opacity, color, etc)")]
    public bool applyMirrorEffect = true;

    [Range(0.3f, 1f)]
    [Tooltip("Alpha transparency of mirrored character")]
    public float mirrorAlpha = 0.7f;

    [Tooltip("Tint color for mirror effect")]
    public Color mirrorTint = new Color(0.8f, 0.8f, 1f, 1f);

    private SpriteRenderer mirrorSpriteRenderer;
    private SpriteRenderer originalSpriteRenderer;
    private Animator mirrorAnimator;
    private Animator originalAnimator;
    private Vector3 lastOriginalPosition;
    private bool initialized = false;

    void Start()
    {
        // Auto-find player if needed
        if (autoFindPlayer && originalPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                originalPlayer = playerObj.transform;
                Debug.Log("InvertedPlayerMirror: Auto-found player - " + playerObj.name);
            }
            else
            {
                Debug.LogError("InvertedPlayerMirror: Could not find player with 'Player' tag!");
                enabled = false;
                return;
            }
        }

        if (originalPlayer == null)
        {
            Debug.LogError("InvertedPlayerMirror: No original player assigned!");
            enabled = false;
            return;
        }

        // Set mirror axes - use middle point between current positions if both exist
        // Otherwise use original player position
        if (transform.position != Vector3.zero)
        {
            // Calculate mirror axis as midpoint between original and inverted starting positions
            mirrorAxisX = (originalPlayer.position.x + transform.position.x) / 2f;
            mirrorAxisY = (originalPlayer.position.y + transform.position.y) / 2f;
            mirrorAxisZ = (originalPlayer.position.z + transform.position.z) / 2f;
        }
        else
        {
            // Default to original player's position
            mirrorAxisX = originalPlayer.position.x;
            mirrorAxisY = originalPlayer.position.y;
            mirrorAxisZ = originalPlayer.position.z;
        }

        // Get sprite renderers
        mirrorSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalSpriteRenderer = originalPlayer.GetComponentInChildren<SpriteRenderer>();

        // Get animators
        if (syncAnimations)
        {
            mirrorAnimator = GetComponentInChildren<Animator>();
            originalAnimator = originalPlayer.GetComponentInChildren<Animator>();

            if (mirrorAnimator == null)
            {
                Debug.LogWarning("InvertedPlayerMirror: No Animator found on mirror player!");
            }
            if (originalAnimator == null)
            {
                Debug.LogWarning("InvertedPlayerMirror: No Animator found on original player!");
            }
        }

        // Apply mirror visual effect
        if (applyMirrorEffect && mirrorSpriteRenderer != null)
        {
            Color color = mirrorTint;
            color.a = mirrorAlpha;
            mirrorSpriteRenderer.color = color;
        }

        lastOriginalPosition = originalPlayer.position;

        // Position the mirror initially
        UpdateMirrorPosition();

        initialized = true;
        Debug.Log("InvertedPlayerMirror initialized successfully!");
    }

    void LateUpdate()
    {
        if (!initialized || originalPlayer == null) return;

        UpdateMirrorPosition();
        UpdateMirrorRotationAndScale();

        if (syncAnimations)
        {
            SyncAnimationParameters();
        }
    }

    void UpdateMirrorPosition()
    {
        Vector3 originalPos = originalPlayer.position;
        Vector3 newPos = transform.position;

        // Calculate inverted position based on movement from axis
        if (invertX)
        {
            // Invert X: if player goes right (+X), mirror goes left (-X)
            float distanceFromAxisX = originalPos.x - mirrorAxisX;
            newPos.x = mirrorAxisX - distanceFromAxisX;
        }
        else
        {
            newPos.x = originalPos.x;
        }

        if (invertY)
        {
            // Invert Y: if player goes up (+Y), mirror goes down (-Y)
            float distanceFromAxisY = originalPos.y - mirrorAxisY;
            newPos.y = mirrorAxisY - distanceFromAxisY;
        }
        else
        {
            newPos.y = originalPos.y;
        }

        if (invertZ)
        {
            // Invert Z: if player goes forward (into screen +Z), mirror goes back (-Z)
            // We need a Z axis point to mirror around
            float mirrorAxisZ = 0f; // You can adjust this if needed
            float distanceFromAxisZ = originalPos.z - mirrorAxisZ;
            newPos.z = mirrorAxisZ - distanceFromAxisZ;
        }
        else
        {
            newPos.z = originalPos.z;
        }

        transform.position = newPos;
        lastOriginalPosition = originalPos;
    }

    void UpdateMirrorRotationAndScale()
    {
        // Flip the entire mirror GameObject on Y axis to invert it visually (upside down)
        Vector3 scale = originalPlayer.localScale;

        if (invertY)
        {
            scale.y = -Mathf.Abs(scale.y); // Make Y scale negative to flip vertically
        }

        transform.localScale = scale;

        // Handle sprite flipping for X direction
        if (mirrorSpriteRenderer != null && originalSpriteRenderer != null)
        {
            if (invertSpriteFlip && invertX)
            {
                // Since movement is inverted, flip sprite to match direction
                mirrorSpriteRenderer.flipX = originalSpriteRenderer.flipX;
            }
            else
            {
                mirrorSpriteRenderer.flipX = !originalSpriteRenderer.flipX;
            }
        }
    }

    void SyncAnimationParameters()
    {
        if (mirrorAnimator == null || originalAnimator == null) return;

        // Sync Speed parameter
        if (HasParameter(originalAnimator, "Speed"))
        {
            float speed = originalAnimator.GetFloat("Speed");
            mirrorAnimator.SetFloat("Speed", speed);
        }

        // Sync Grounded state
        if (HasParameter(originalAnimator, "isGrounded"))
        {
            bool isGrounded = originalAnimator.GetBool("isGrounded");
            // Mirror is inverted, so grounded state remains same (grounded to ceiling)
            mirrorAnimator.SetBool("isGrounded", isGrounded);
        }

        // INVERT movement direction animations
        if (invertDepthAnimations)
        {
            // Swap forward and backward animations
            if (HasParameter(originalAnimator, "movingBackwards") && HasParameter(mirrorAnimator, "movingForward"))
            {
                bool originalMovingBack = originalAnimator.GetBool("movingBackwards");
                mirrorAnimator.SetBool("movingForward", originalMovingBack);
            }

            if (HasParameter(originalAnimator, "movingForward") && HasParameter(mirrorAnimator, "movingBackwards"))
            {
                bool originalMovingForward = originalAnimator.GetBool("movingForward");
                mirrorAnimator.SetBool("movingBackwards", originalMovingForward);
            }
        }
        else
        {
            // Don't invert - keep same direction
            if (HasParameter(originalAnimator, "movingBackwards"))
            {
                bool movingBackwards = originalAnimator.GetBool("movingBackwards");
                mirrorAnimator.SetBool("movingBackwards", movingBackwards);
            }

            if (HasParameter(originalAnimator, "movingForward"))
            {
                bool movingForward = originalAnimator.GetBool("movingForward");
                mirrorAnimator.SetBool("movingForward", movingForward);
            }
        }
    }

    // Helper function to check if animator has a parameter
    private bool HasParameter(Animator animator, string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    // Public helper methods
    public void SetMirrorAxes(float xAxis, float yAxis, float zAxis)
    {
        mirrorAxisX = xAxis;
        mirrorAxisY = yAxis;
        mirrorAxisZ = zAxis;
    }

    public void UpdateMirrorAxesToCurrentPlayer()
    {
        if (originalPlayer != null)
        {
            mirrorAxisX = originalPlayer.position.x;
            mirrorAxisY = originalPlayer.position.y;
            mirrorAxisZ = originalPlayer.position.z;
        }
    }

    public void SetOriginalPlayer(Transform player)
    {
        originalPlayer = player;
        if (player != null)
        {
            originalSpriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
            originalAnimator = player.GetComponentInChildren<Animator>();
            lastOriginalPosition = player.position;
            mirrorAxisX = player.position.x;
            mirrorAxisY = player.position.y;
            mirrorAxisZ = player.position.z;
        }
    }

    public void SetMirrorEffect(bool enable, float alpha, Color tint)
    {
        applyMirrorEffect = enable;
        mirrorAlpha = alpha;
        mirrorTint = tint;

        if (enable && mirrorSpriteRenderer != null)
        {
            Color color = tint;
            color.a = alpha;
            mirrorSpriteRenderer.color = color;
        }
        else if (mirrorSpriteRenderer != null)
        {
            mirrorSpriteRenderer.color = Color.white;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (originalPlayer == null) return;

        // Draw mirror axis lines
        Gizmos.color = Color.yellow;
        float lineLength = 50f;

        // Horizontal axis line (Y)
        Vector3 axisStartY = new Vector3(originalPlayer.position.x - lineLength, mirrorAxisY, originalPlayer.position.z);
        Vector3 axisEndY = new Vector3(originalPlayer.position.x + lineLength, mirrorAxisY, originalPlayer.position.z);
        Gizmos.DrawLine(axisStartY, axisEndY);

        // Vertical axis line (X)
        Gizmos.color = Color.red;
        Vector3 axisStartX = new Vector3(mirrorAxisX, originalPlayer.position.y - lineLength, originalPlayer.position.z);
        Vector3 axisEndX = new Vector3(mirrorAxisX, originalPlayer.position.y + lineLength, originalPlayer.position.z);
        Gizmos.DrawLine(axisStartX, axisEndX);

        // Draw connection line between original and mirror
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(originalPlayer.position, transform.position);

        // Draw position markers
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(originalPlayer.position, 0.3f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw center point
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(new Vector3(mirrorAxisX, mirrorAxisY, originalPlayer.position.z), 0.5f);
    }
}