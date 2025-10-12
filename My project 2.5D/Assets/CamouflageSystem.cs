using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamouflageSystem : MonoBehaviour
{
    [Header("Camouflage Settings")]
    [Tooltip("Key to activate camouflage")]
    public KeyCode camouflageKey = KeyCode.E;

    [Range(0f, 1f)]
    [Tooltip("How transparent the player becomes when camouflaged")]
    public float camouflageAlpha = 0.3f;

    [Tooltip("Time to fully blend in/out")]
    public float blendDuration = 0.5f;

    [Header("Wall Detection")]
    [Tooltip("Distance to check for walls")]
    public float wallCheckDistance = 0.8f;

    [Tooltip("Which layers count as walls")]
    public LayerMask wallLayer;

    [Tooltip("Offset for wall check raycast")]
    public Vector3 wallCheckOffset = new Vector3(0, 1, 0);

    [Header("Camouflage Requirements")]
    [Tooltip("Must be touching wall to camouflage")]
    public bool requireWallContact = true;

    [Tooltip("Must be grounded to camouflage")]
    public bool requireGrounded = false;

    [Tooltip("Must be still to maintain camouflage")]
    public bool requireStillness = true;

    [Tooltip("Maximum movement speed to stay camouflaged")]
    public float maxMovementSpeed = 0.1f;

    [Header("Color Matching")]
    [Tooltip("Try to match the wall color")]
    public bool matchWallColor = true;

    [Tooltip("How quickly to transition to wall color")]
    public float colorMatchSpeed = 2f;

    [Header("Gameplay Effects")]
    [Tooltip("Disable player movement while camouflaged")]
    public bool disableMovementWhenHidden = true;

    [Tooltip("Energy/stamina system (0 = infinite)")]
    public float maxCamouflageEnergy = 100f;
    public float energyDrainRate = 10f; // per second
    public float energyRechargeRate = 20f; // per second

    [Header("Visual Effects")]
    public bool useOutlineEffect = true;
    public Color outlineColor = new Color(0.5f, 0.8f, 1f, 0.5f);
    public float outlineWidth = 0.1f;

    [Header("Audio")]
    public AudioClip camouflageActivateSound;
    public AudioClip camouflageDeactivateSound;
    public AudioClip energyDepletedSound;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Private variables
    private SpriteRenderer spriteRenderer;
    private EnhancedPlayer25D playerController;
    private Rigidbody rb;
    private AudioSource audioSource;

    private bool isCamouflaged = false;
    private bool isBlending = false;
    private bool canCamouflage = false;

    private Color originalColor;
    private Color targetColor;
    private float currentAlpha = 1f;
    private float currentEnergy;

    private Vector3 lastPosition;
    private bool isStill = true;

    private RaycastHit wallHit;
    private bool isTouchingWall = false;
    private Color wallColor = Color.white;

    // Outline effect
    private GameObject outlineObject;
    private SpriteRenderer outlineRenderer;

    void Start()
    {
        // Get components
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("CamouflageSystem: No SpriteRenderer found!");
            enabled = false;
            return;
        }

        playerController = GetComponent<EnhancedPlayer25D>();
        rb = GetComponent<Rigidbody>();

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        originalColor = spriteRenderer.color;
        currentEnergy = maxCamouflageEnergy;
        lastPosition = transform.position;

        // Create outline effect
        if (useOutlineEffect)
        {
            CreateOutlineEffect();
        }

        Debug.Log("CamouflageSystem initialized!");
    }

    void CreateOutlineEffect()
    {
        outlineObject = new GameObject("CamouflageOutline");
        outlineObject.transform.parent = spriteRenderer.transform;
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * (1f + outlineWidth);

        outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = spriteRenderer.sprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        outlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        outlineObject.SetActive(false);
    }

    void Update()
    {
        // Check if player is still
        CheckIfStill();

        // Check wall contact
        CheckWallContact();

        // Determine if camouflage is possible
        UpdateCamouflageAvailability();

        // Handle input
        HandleInput();

        // Update camouflage state
        UpdateCamouflageState();

        // Update visual effects
        UpdateVisuals();

        // Update energy
        UpdateEnergy();

        // Debug info
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    void CheckIfStill()
    {
        float distance = Vector3.Distance(transform.position, lastPosition);
        isStill = distance < maxMovementSpeed * Time.deltaTime;
        lastPosition = transform.position;
    }

    void CheckWallContact()
    {
        Vector3 checkPos = transform.position + wallCheckOffset;

        // Check in all four cardinal directions
        Vector3[] directions = new Vector3[]
        {
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back
        };

        isTouchingWall = false;

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(checkPos, dir, out wallHit, wallCheckDistance, wallLayer))
            {
                isTouchingWall = true;

                // Try to get wall color
                if (matchWallColor)
                {
                    Renderer wallRenderer = wallHit.collider.GetComponent<Renderer>();
                    if (wallRenderer != null && wallRenderer.material.HasProperty("_Color"))
                    {
                        wallColor = wallRenderer.material.color;
                    }
                }

                if (showDebugInfo)
                {
                    Debug.DrawRay(checkPos, dir * wallCheckDistance, Color.green);
                }
                break;
            }
            else if (showDebugInfo)
            {
                Debug.DrawRay(checkPos, dir * wallCheckDistance, Color.red);
            }
        }
    }

    void UpdateCamouflageAvailability()
    {
        canCamouflage = true;

        // Check wall requirement
        if (requireWallContact && !isTouchingWall)
        {
            canCamouflage = false;
        }

        // Check grounded requirement
        if (requireGrounded && playerController != null && !playerController.IsGrounded())
        {
            canCamouflage = false;
        }

        // Check energy
        if (currentEnergy <= 0)
        {
            canCamouflage = false;
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(camouflageKey))
        {
            if (!isCamouflaged && canCamouflage)
            {
                ActivateCamouflage();
            }
            else if (isCamouflaged)
            {
                DeactivateCamouflage();
            }
            else if (!canCamouflage && showDebugInfo)
            {
                Debug.Log("Cannot camouflage: " + GetCamouflageBlockReason());
            }
        }

        // Auto-deactivate if requirements no longer met
        if (isCamouflaged)
        {
            if (requireStillness && !isStill)
            {
                DeactivateCamouflage();
                if (showDebugInfo) Debug.Log("Camouflage broken: movement detected");
            }
            else if (requireWallContact && !isTouchingWall)
            {
                DeactivateCamouflage();
                if (showDebugInfo) Debug.Log("Camouflage broken: lost wall contact");
            }
            else if (currentEnergy <= 0)
            {
                DeactivateCamouflage();
                if (showDebugInfo) Debug.Log("Camouflage broken: energy depleted");
                PlaySound(energyDepletedSound);
            }
        }
    }

    string GetCamouflageBlockReason()
    {
        if (requireWallContact && !isTouchingWall) return "Not touching wall";
        if (requireGrounded && playerController != null && !playerController.IsGrounded()) return "Not grounded";
        if (currentEnergy <= 0) return "No energy";
        return "Unknown";
    }

    void ActivateCamouflage()
    {
        isCamouflaged = true;
        isBlending = true;

        // Determine target color
        if (matchWallColor && isTouchingWall)
        {
            targetColor = wallColor;
        }
        else
        {
            targetColor = originalColor;
        }

        // Disable movement if required
        if (disableMovementWhenHidden && playerController != null)
        {
            playerController.enabled = false;
            if (rb != null) rb.velocity = Vector3.zero;
        }

        PlaySound(camouflageActivateSound);

        if (showDebugInfo)
        {
            Debug.Log("Camouflage ACTIVATED");
        }
    }

    void DeactivateCamouflage()
    {
        isCamouflaged = false;
        isBlending = true;
        targetColor = originalColor;

        // Re-enable movement
        if (disableMovementWhenHidden && playerController != null)
        {
            playerController.enabled = true;
        }

        PlaySound(camouflageDeactivateSound);

        if (showDebugInfo)
        {
            Debug.Log("Camouflage DEACTIVATED");
        }
    }

    void UpdateCamouflageState()
    {
        if (!isBlending) return;

        float targetAlpha = isCamouflaged ? camouflageAlpha : 1f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / blendDuration);

        // Check if blending is complete
        if (Mathf.Approximately(currentAlpha, targetAlpha))
        {
            isBlending = false;
        }
    }

    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        // Update color
        Color displayColor = isCamouflaged && matchWallColor ?
            Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * colorMatchSpeed) :
            originalColor;

        displayColor.a = currentAlpha;
        spriteRenderer.color = displayColor;

        // Update outline
        if (outlineObject != null)
        {
            outlineObject.SetActive(isCamouflaged || isBlending);
            if (outlineRenderer != null)
            {
                Color outlineCol = outlineColor;
                outlineCol.a = currentAlpha * 0.5f;
                outlineRenderer.color = outlineCol;
                outlineRenderer.sprite = spriteRenderer.sprite;
            }
        }
    }

    void UpdateEnergy()
    {
        if (maxCamouflageEnergy <= 0) return; // Infinite energy

        if (isCamouflaged)
        {
            currentEnergy -= energyDrainRate * Time.deltaTime;
            currentEnergy = Mathf.Max(0, currentEnergy);
        }
        else
        {
            currentEnergy += energyRechargeRate * Time.deltaTime;
            currentEnergy = Mathf.Min(maxCamouflageEnergy, currentEnergy);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void DrawDebugInfo()
    {
        // This will be visible in Scene view
        if (isTouchingWall)
        {
            Debug.DrawLine(transform.position + wallCheckOffset, wallHit.point, Color.cyan);
        }
    }

    // Public methods for external access
    public bool IsCamouflaged()
    {
        return isCamouflaged;
    }

    public bool CanCamouflage()
    {
        return canCamouflage;
    }

    public float GetEnergyPercent()
    {
        if (maxCamouflageEnergy <= 0) return 1f;
        return currentEnergy / maxCamouflageEnergy;
    }

    public void ForceDeactivate()
    {
        if (isCamouflaged)
        {
            DeactivateCamouflage();
        }
    }

    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxCamouflageEnergy);
    }

    void OnDrawGizmosSelected()
    {
        // Draw wall check radius
        Gizmos.color = isTouchingWall ? Color.green : Color.red;
        Vector3 checkPos = transform.position + wallCheckOffset;

        // Draw check directions
        Gizmos.DrawWireSphere(checkPos, wallCheckDistance);

        // Draw camouflage state
        if (isCamouflaged)
        {
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("=== CAMOUFLAGE DEBUG ===");
        GUILayout.Label($"Camouflaged: {isCamouflaged}");
        GUILayout.Label($"Can Camouflage: {canCamouflage}");
        GUILayout.Label($"Touching Wall: {isTouchingWall}");
        GUILayout.Label($"Is Still: {isStill}");
        GUILayout.Label($"Alpha: {currentAlpha:F2}");

        if (maxCamouflageEnergy > 0)
        {
            float energyPercent = GetEnergyPercent();
            GUILayout.Label($"Energy: {currentEnergy:F0}/{maxCamouflageEnergy:F0} ({energyPercent:P0})");

            // Energy bar
            Rect barRect = GUILayoutUtility.GetRect(200, 20);
            GUI.Box(barRect, "");
            Rect fillRect = new Rect(barRect.x + 2, barRect.y + 2, (barRect.width - 4) * energyPercent, barRect.height - 4);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        }

        GUILayout.Label($"Press [{camouflageKey}] to toggle");

        if (!canCamouflage)
        {
            GUILayout.Label($"Blocked: {GetCamouflageBlockReason()}", GUI.skin.box);
        }

        GUILayout.EndArea();
    }
}