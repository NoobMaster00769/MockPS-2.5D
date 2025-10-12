using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleTransparency : MonoBehaviour
{
    [Header("Transparency Settings")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0.3f;
    public float fadeSpeed = 5f;

    [Header("Sorting Settings")]
    [Tooltip("Sorting layer name (e.g., 'Obstacles', 'Foreground')")]
    public string sortingLayerName = "Default";
    [Tooltip("Order in layer - higher renders in front")]
    public int orderInLayer = 10;

    [Header("Detection Settings")]
    public Transform player;
    public float detectionRadius = 3f;
    public float heightTolerance = 2f;

    [Header("Optional: Auto-find Player")]
    public bool autoFindPlayer = true;

    [Header("Debug")]
    public bool showDebug = false;

    private Renderer objRenderer;
    private Material[] originalMaterials;
    private Material[] transparentMaterials;
    private bool isTransparent = false;
    private float targetAlpha = 1f;
    private float currentAlpha = 1f;

    void Start()
    {
        // Get renderer
        objRenderer = GetComponent<Renderer>();
        if (objRenderer == null)
        {
            Debug.LogError("ObstacleTransparency: No Renderer found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Auto-find player if enabled
        if (autoFindPlayer && player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("ObstacleTransparency: No player found. Make sure Player has 'Player' tag.");
            }
        }

        // Store original materials and create transparent versions
        SetupMaterials();

        // Apply sorting layer and order
        ApplySortingSettings();
    }

    void SetupMaterials()
    {
        originalMaterials = objRenderer.materials;
        transparentMaterials = new Material[originalMaterials.Length];

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            // Create a copy of the material
            transparentMaterials[i] = new Material(originalMaterials[i]);

            // Set the material to transparent mode
            SetMaterialTransparent(transparentMaterials[i]);
        }

        objRenderer.materials = transparentMaterials;
    }

    void SetMaterialTransparent(Material mat)
    {
        // Change rendering mode to Transparent
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    void ApplySortingSettings()
    {
        // Apply sorting layer and order to the renderer
        if (objRenderer != null)
        {
            objRenderer.sortingLayerName = sortingLayerName;
            objRenderer.sortingOrder = orderInLayer;

            if (showDebug)
            {
                Debug.Log("Applied sorting - Layer: " + sortingLayerName + ", Order: " + orderInLayer);
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // Check if player is behind the obstacle
        bool shouldBeTransparent = IsPlayerBehindObstacle();

        if (showDebug)
        {
            Debug.Log("Obstacle: " + gameObject.name + " | Should be transparent: " + shouldBeTransparent);
        }

        // Set target alpha
        targetAlpha = shouldBeTransparent ? transparentAlpha : 1f;

        // Smoothly lerp current alpha to target
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // Update all materials
        UpdateMaterialsAlpha(currentAlpha);

        // Update transparency state
        if (shouldBeTransparent != isTransparent)
        {
            isTransparent = shouldBeTransparent;
        }
    }

    bool IsPlayerBehindObstacle()
    {
        // Get camera position
        Camera mainCam = Camera.main;
        if (mainCam == null) return false;

        Vector3 cameraPos = mainCam.transform.position;
        Vector3 obstaclePos = transform.position;
        Vector3 playerPos = player.position;

        // 1. Check if player is close enough to obstacle (horizontal distance only for 2.5D)
        float horizontalDistance = Vector2.Distance(
            new Vector2(obstaclePos.x, obstaclePos.z),
            new Vector2(playerPos.x, playerPos.z)
        );

        if (horizontalDistance > detectionRadius)
        {
            if (showDebug) Debug.Log("Player too far: " + horizontalDistance);
            return false;
        }

        // 2. Check height difference (Y axis)
        float heightDifference = Mathf.Abs(playerPos.y - obstaclePos.y);
        if (heightDifference > heightTolerance)
        {
            if (showDebug) Debug.Log("Height difference too large: " + heightDifference);
            return false;
        }

        // 3. Check if obstacle is between camera and player (Z-axis for 2.5D)
        // In 2.5D, "behind" means player's Z is greater than obstacle's Z (further from camera)

        // For a typical 2.5D setup where camera looks at negative Z
        bool playerBehindObstacle = playerPos.z > obstaclePos.z;

        if (showDebug)
        {
            Debug.Log("Player Z: " + playerPos.z + " | Obstacle Z: " + obstaclePos.z + " | Behind: " + playerBehindObstacle);
        }

        // 4. Additional check: player should be roughly in the same X position
        float xDifference = Mathf.Abs(playerPos.x - obstaclePos.x);
        if (xDifference > detectionRadius)
        {
            if (showDebug) Debug.Log("Player not aligned horizontally");
            return false;
        }

        return playerBehindObstacle;
    }

    void UpdateMaterialsAlpha(float alpha)
    {
        foreach (Material mat in transparentMaterials)
        {
            Color color = mat.color;
            color.a = alpha;
            mat.color = color;

            // Also update main texture alpha if it exists
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
        }
    }

    // Call this from inspector or code if you change sorting settings at runtime
    public void UpdateSortingSettings()
    {
        ApplySortingSettings();
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection radius cylinder
        Gizmos.color = Color.yellow;
        Vector3 pos = transform.position;

        // Draw horizontal circle
        for (int i = 0; i < 36; i++)
        {
            float angle1 = i * 10f * Mathf.Deg2Rad;
            float angle2 = (i + 1) * 10f * Mathf.Deg2Rad;

            Vector3 p1 = pos + new Vector3(Mathf.Cos(angle1) * detectionRadius, 0, Mathf.Sin(angle1) * detectionRadius);
            Vector3 p2 = pos + new Vector3(Mathf.Cos(angle2) * detectionRadius, 0, Mathf.Sin(angle2) * detectionRadius);

            Gizmos.DrawLine(p1, p2);
        }

        // Draw height tolerance
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(pos, new Vector3(detectionRadius * 2, heightTolerance * 2, detectionRadius * 2));

        // Draw line to player if assigned
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);

            // Show Z-depth comparison
            Gizmos.color = player.position.z > transform.position.z ? Color.red : Color.blue;
            Gizmos.DrawSphere(player.position, 0.2f);
        }
    }

    void OnDestroy()
    {
        // Clean up materials
        if (transparentMaterials != null)
        {
            foreach (Material mat in transparentMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
}