using System.Collections;
using UnityEngine;

public class PlayerBlinkOnCollision : MonoBehaviour
{
    [Header("Blink Settings")]
    [SerializeField] private float blinkDuration = 0.5f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Energy System Integration")]
    [SerializeField] private bool reduceEnergyOnCollision = true;
    [Tooltip("Leave empty to auto-find EnergySystem")]
    [SerializeField] private EnergySystem energySystem;

    private SpriteRenderer spriteRenderer;
    private Coroutine blinkCoroutine;

    void Start()
    {
        // Get SpriteRenderer from child (just like your other scripts do)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("PlayerBlinkOnCollision: No SpriteRenderer found on player or children!");
            enabled = false;
            return;
        }

        // Auto-find EnergySystem if not assigned
        if (energySystem == null && reduceEnergyOnCollision)
        {
            energySystem = FindObjectOfType<EnergySystem>();
            if (energySystem == null)
            {
                Debug.LogWarning("PlayerBlinkOnCollision: No EnergySystem found! Energy reduction disabled.");
            }
            else
            {
                Debug.Log("PlayerBlinkOnCollision: Auto-found EnergySystem!");
            }
        }

        // Start invisible
        spriteRenderer.enabled = false;
        Debug.Log("PlayerBlinkOnCollision: Initialized! Player is now invisible.");
    }

    // For 3D Colliders
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("PlayerBlinkOnCollision: Collision detected with " + collision.gameObject.name);
        TriggerBlink();

        // Forward collision to energy system
        if (reduceEnergyOnCollision && energySystem != null)
        {
            energySystem.OnPlayerCollision(collision.gameObject);
        }
    }

    // For 3D Triggers
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("PlayerBlinkOnCollision: Trigger detected with " + other.gameObject.name);
        TriggerBlink();

        // Forward trigger to energy system
        if (reduceEnergyOnCollision && energySystem != null)
        {
            energySystem.OnPlayerTrigger(other.gameObject);
        }
    }

    void TriggerBlink()
    {
        if (spriteRenderer == null) return;

        // Stop any existing blink
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        float elapsed = 0f;
        Debug.Log("PlayerBlinkOnCollision: Starting blink effect!");

        // Store the original color
        Color originalColor = spriteRenderer.color;

        while (elapsed < blinkDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;

            // If sprite becomes visible, make sure it's fully visible
            if (spriteRenderer.enabled)
            {
                Color tempColor = originalColor;
                tempColor.a = 1f; // Full opacity
                spriteRenderer.color = tempColor;
            }

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        // Ensure it ends invisible
        spriteRenderer.enabled = false;
        Debug.Log("PlayerBlinkOnCollision: Blink effect finished!");
    }
}