using UnityEngine;

/// <summary>
/// Creates depth without parallax using atmospheric perspective, scaling, and color fading.
/// Perfect for split-screen 2.5D games where parallax doesn't work well.
/// </summary>
public class AtmosphericDepth : MonoBehaviour
{
    [Header("Depth Layer Settings")]
    [Tooltip("How far back this object is (0 = foreground, 1 = far background)")]
    [Range(0f, 1f)]
    public float depthLayer = 0.5f;

    [Header("Atmospheric Fade")]
    [Tooltip("Enable color fading based on depth")]
    public bool enableAtmosphericFade = true;

    [Tooltip("Color to fade towards (usually sky color)")]
    public Color fogColor = new Color(0.53f, 0.81f, 0.92f, 1f); // Sky blue

    [Tooltip("How strong the fade effect is")]
    [Range(0f, 1f)]
    public float fadeStrength = 0.8f;

    [Header("Scale with Depth")]
    [Tooltip("Scale object based on depth (smaller = further away)")]
    public bool enableDepthScaling = true;

    [Tooltip("Minimum scale for far objects")]
    [Range(0.3f, 1f)]
    public float minScale = 0.6f;

    [Header("Blur/Detail Reduction")]
    [Tooltip("Reduce sprite color saturation for distant objects")]
    public bool enableSaturationReduction = true;

    [Range(0f, 1f)]
    public float saturationAmount = 0.5f;

    [Header("Z Position")]
    [Tooltip("Automatically set Z position based on depth")]
    public bool autoSetZPosition = true;

    [Tooltip("Z range (further objects have higher Z)")]
    public float minZ = 0f;
    public float maxZ = 20f;

    [Header("Shadow/Contrast")]
    [Tooltip("Reduce contrast for distant objects")]
    public bool reduceBrightness = true;

    [Range(0.3f, 1f)]
    public float brightnessMultiplier = 0.8f;

    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;
    private Material materialInstance;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"AtmosphericDepth on {name}: No SpriteRenderer found!");
            enabled = false;
            return;
        }

        originalScale = transform.localScale;
        originalColor = spriteRenderer.color;

        // Create material instance to avoid affecting other sprites
        materialInstance = new Material(spriteRenderer.material);
        spriteRenderer.material = materialInstance;

        ApplyDepthEffects();
    }

    void ApplyDepthEffects()
    {
        // Apply Z position
        if (autoSetZPosition)
        {
            Vector3 pos = transform.position;
            pos.z = Mathf.Lerp(minZ, maxZ, depthLayer);
            transform.position = pos;
        }

        // Apply depth scaling
        if (enableDepthScaling)
        {
            float scaleMultiplier = Mathf.Lerp(1f, minScale, depthLayer);
            transform.localScale = originalScale * scaleMultiplier;
        }

        // Apply color effects
        UpdateColorEffects();
    }

    void UpdateColorEffects()
    {
        if (spriteRenderer == null)
            return;

        Color finalColor = originalColor;

        // Atmospheric fade - blend with fog color
        if (enableAtmosphericFade)
        {
            float fadeAmount = depthLayer * fadeStrength;
            finalColor = Color.Lerp(originalColor, fogColor, fadeAmount);
        }

        // Saturation reduction
        if (enableSaturationReduction)
        {
            float gray = (finalColor.r + finalColor.g + finalColor.b) / 3f;
            float satReduction = depthLayer * saturationAmount;
            finalColor.r = Mathf.Lerp(finalColor.r, gray, satReduction);
            finalColor.g = Mathf.Lerp(finalColor.g, gray, satReduction);
            finalColor.b = Mathf.Lerp(finalColor.b, gray, satReduction);
        }

        // Brightness reduction
        if (reduceBrightness)
        {
            float brightReduction = Mathf.Lerp(1f, brightnessMultiplier, depthLayer);
            finalColor.r *= brightReduction;
            finalColor.g *= brightReduction;
            finalColor.b *= brightReduction;
        }

        spriteRenderer.color = finalColor;
    }

    // Runtime adjustment
    public void SetDepthLayer(float depth)
    {
        depthLayer = Mathf.Clamp01(depth);
        ApplyDepthEffects();
    }

    void OnValidate()
    {
        if (Application.isPlaying && spriteRenderer != null)
        {
            ApplyDepthEffects();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.Lerp(Color.green, Color.red, depthLayer);

        if (spriteRenderer != null)
        {
            Gizmos.DrawWireCube(transform.position, spriteRenderer.bounds.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }

        // Draw depth label
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Depth: {depthLayer:F2}\nZ: {transform.position.z:F1}"
        );
#endif
    }
}

// ============================================================
// ALTERNATIVE DEPTH SYSTEM: Layered Sprites
// ============================================================

/// <summary>
/// Simple layered depth using only Z position and sorting layers.
/// Ultra-lightweight alternative when you don't want any visual effects.
/// </summary>
public class SimpleDepthLayer : MonoBehaviour
{
    [Header("Depth Settings")]
    [Range(0f, 1f)]
    public float depthLayer = 0.5f;

    public float minZ = 0f;
    public float maxZ = 20f;

    [Header("Sorting")]
    public string sortingLayerName = "Background";
    [Range(-100, 100)]
    public int orderInLayer = 0;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyDepth();
    }

    void ApplyDepth()
    {
        // Set Z position
        Vector3 pos = transform.position;
        pos.z = Mathf.Lerp(minZ, maxZ, depthLayer);
        transform.position = pos;

        // Set sorting
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = orderInLayer;
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            ApplyDepth();
    }
}