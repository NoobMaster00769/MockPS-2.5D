using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Automatically organizes background sprites into depth layers.
/// Removes parallax and applies depth effects instead.
/// </summary>
public class DepthLayerOrganizer : MonoBehaviour
{
    [Header("Scene Analysis")]
    [Tooltip("Background parent object containing all layers")]
    public Transform backgroundParent;

    [Tooltip("Auto-organize on start")]
    public bool autoOrganizeOnStart = false;

    [Header("Depth Settings")]
    [Tooltip("Sky blue color for atmospheric fade")]
    public Color atmosphericColor = new Color(0.53f, 0.81f, 0.92f, 1f);

    [Tooltip("Use atmospheric perspective")]
    public bool useAtmosphericFade = true;

    [Tooltip("Use depth scaling")]
    public bool useDepthScaling = true;

    [Header("Layer Presets")]
    [Tooltip("Predefined depth layers (name contains these strings)")]
    public DepthPreset[] depthPresets = new DepthPreset[]
    {
        new DepthPreset("Sky", 1.0f, 20f),
        new DepthPreset("Cloud", 0.9f, 18f),
        new DepthPreset("Mountain", 0.8f, 15f),
        new DepthPreset("Tree", 0.5f, 10f),
        new DepthPreset("Grass", 0.2f, 5f),
        new DepthPreset("Ground", 0f, 0f)
    };

    [Header("Actions")]
    public bool organizeNow = false;
    public bool removeAllParallax = false;
    public bool showAnalysis = false;

    [System.Serializable]
    public class DepthPreset
    {
        public string nameContains;
        public float depthLayer;
        public float zPosition;

        public DepthPreset(string name, float depth, float z)
        {
            nameContains = name;
            depthLayer = depth;
            zPosition = z;
        }
    }

    void Start()
    {
        if (autoOrganizeOnStart)
        {
            OrganizeBackgroundLayers();
        }
    }

    void Update()
    {
        if (organizeNow)
        {
            organizeNow = false;
            OrganizeBackgroundLayers();
        }

        if (removeAllParallax)
        {
            removeAllParallax = false;
            RemoveAllParallaxComponents();
        }

        if (showAnalysis)
        {
            showAnalysis = false;
            AnalyzeBackgroundLayers();
        }
    }

    [ContextMenu("Organize Background Layers")]
    public void OrganizeBackgroundLayers()
    {
        Debug.Log("=== Organizing Background Layers ===");

        if (backgroundParent == null)
        {
            // Try to find background parent
            GameObject bg = GameObject.Find("Background") ?? GameObject.Find("Backgrounds");
            if (bg != null)
                backgroundParent = bg.transform;
            else
            {
                Debug.LogError("No background parent found!");
                return;
            }
        }

        // Remove all existing parallax
        RemoveAllParallaxComponents();

        // Get all sprite renderers
        SpriteRenderer[] sprites = backgroundParent.GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"Found {sprites.Length} background sprites");

        int organized = 0;

        foreach (SpriteRenderer sprite in sprites)
        {
            GameObject obj = sprite.gameObject;
            string objName = obj.name.ToLower();

            // Find matching preset
            DepthPreset matchedPreset = null;
            foreach (DepthPreset preset in depthPresets)
            {
                if (objName.Contains(preset.nameContains.ToLower()))
                {
                    matchedPreset = preset;
                    break;
                }
            }

            if (matchedPreset != null)
            {
                // Apply depth settings
                SetupDepthLayer(obj, matchedPreset);
                organized++;
                Debug.Log($"✓ {obj.name} → Depth: {matchedPreset.depthLayer:F2}, Z: {matchedPreset.zPosition}");
            }
            else
            {
                Debug.LogWarning($"⚠ {obj.name} - No matching preset found");
            }
        }

        Debug.Log($"=== Organized {organized}/{sprites.Length} layers ===");
    }

    void SetupDepthLayer(GameObject obj, DepthPreset preset)
    {
        // Remove old components
        AtmosphericDepth oldAtmo = obj.GetComponent<AtmosphericDepth>();
        if (oldAtmo != null)
        {
            if (Application.isPlaying)
                Destroy(oldAtmo);
            else
                DestroyImmediate(oldAtmo);
        }

        // Add atmospheric depth component
        AtmosphericDepth depth = obj.AddComponent<AtmosphericDepth>();
        depth.depthLayer = preset.depthLayer;
        depth.enableAtmosphericFade = useAtmosphericFade;
        depth.fogColor = atmosphericColor;
        depth.fadeStrength = 0.7f;
        depth.enableDepthScaling = useDepthScaling;
        depth.minScale = 0.7f;
        depth.autoSetZPosition = true;
        depth.minZ = 0f;
        depth.maxZ = 20f;

        // Set position
        Vector3 pos = obj.transform.position;
        pos.z = preset.zPosition;
        obj.transform.position = pos;
    }

    [ContextMenu("Remove All Parallax")]
    public void RemoveAllParallaxComponents()
    {
        SimpleParallax25D[] allParallax = FindObjectsOfType<SimpleParallax25D>();

        Debug.Log($"Removing {allParallax.Length} parallax components...");

        foreach (SimpleParallax25D parallax in allParallax)
        {
            if (Application.isPlaying)
                Destroy(parallax);
            else
                DestroyImmediate(parallax);
        }

        Debug.Log("✓ All parallax removed");
    }

    [ContextMenu("Analyze Background Layers")]
    public void AnalyzeBackgroundLayers()
    {
        if (backgroundParent == null)
        {
            GameObject bg = GameObject.Find("Background") ?? GameObject.Find("Backgrounds");
            if (bg != null)
                backgroundParent = bg.transform;
        }

        if (backgroundParent == null)
        {
            Debug.LogError("No background parent found!");
            return;
        }

        SpriteRenderer[] sprites = backgroundParent.GetComponentsInChildren<SpriteRenderer>();

        Debug.Log("=== BACKGROUND ANALYSIS ===");
        Debug.Log($"Total layers: {sprites.Length}");
        Debug.Log("");

        foreach (SpriteRenderer sprite in sprites)
        {
            SimpleParallax25D parallax = sprite.GetComponent<SimpleParallax25D>();
            AtmosphericDepth atmo = sprite.GetComponent<AtmosphericDepth>();

            string status = "❌ No depth system";
            if (parallax != null)
                status = $"⚠ Parallax (strength {parallax.parallaxStrength})";
            if (atmo != null)
                status = $"✓ Atmospheric Depth (layer {atmo.depthLayer:F2})";

            Debug.Log($"{sprite.gameObject.name}:");
            Debug.Log($"  Position: {sprite.transform.position}");
            Debug.Log($"  Status: {status}");
            Debug.Log("");
        }

        Debug.Log("=========================");
    }

    [ContextMenu("Quick Setup Guide")]
    public void ShowQuickSetupGuide()
    {
        Debug.Log("=== QUICK SETUP GUIDE ===");
        Debug.Log("");
        Debug.Log("STEP 1: Organize your background sprites under one parent");
        Debug.Log("  GameObject -> Create Empty -> Name it 'Background'");
        Debug.Log("  Drag all background sprites into it");
        Debug.Log("");
        Debug.Log("STEP 2: Name your sprites appropriately:");
        Debug.Log("  - Sky_1, Sky_2 (furthest back)");
        Debug.Log("  - Cloud_1, Cloud_2");
        Debug.Log("  - Mountain_Far, Mountain_Near");
        Debug.Log("  - Tree_Background");
        Debug.Log("  - Grass_Foreground");
        Debug.Log("");
        Debug.Log("STEP 3: Assign 'Background' parent to this script");
        Debug.Log("");
        Debug.Log("STEP 4: Click 'Organize Now' checkbox");
        Debug.Log("");
        Debug.Log("DONE! Your backgrounds now have depth without parallax!");
        Debug.Log("=========================");
    }
}