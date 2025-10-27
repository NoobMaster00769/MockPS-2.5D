using UnityEngine;
using UnityEngine.UI;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;

    [Header("Auto Refill Settings")]
    [SerializeField] private bool autoRefillEnabled = true;
    [SerializeField] private float refillAmount = 5f;
    [SerializeField] private float refillInterval = 30f;

    [Header("Collision Settings")]
    [SerializeField] private bool reduceEnergyOnCollision = true;
    [SerializeField] private float collisionEnergyLoss = 10f;
    [SerializeField] private float collisionCooldown = 0.5f;
    [SerializeField] private LayerMask damageLayer;
    [Tooltip("Leave empty to detect all collisions, or specify tags like 'Wall', 'Obstacle'")]
    [SerializeField] private string[] damageTags = new string[] { "Wall", "Obstacle" };

    private float lastCollisionTime = 0f;

    [Header("UI References")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMPro.TextMeshProUGUI energyText;

    [Header("Follow Settings")]
    [Tooltip("The player this bar will follow (usually InvertedPlayer)")]
    [SerializeField] private Transform followTarget;

    [Tooltip("Offset from the player (positive Y goes UP in world space)")]
    [SerializeField] private Vector3 offset = new Vector3(0, 3, 0);

    [Tooltip("Is the target player inverted/upside down?")]
    [SerializeField] private bool targetIsInverted = false;

    [Header("Collision Detection")]
    [Tooltip("Should we automatically add collision detector to the player?")]
    [SerializeField] private bool autoSetupCollisionDetection = true;

    [Header("Debug")]
    public bool showDebugInfo = true;
    [Tooltip("Lock the energy bar rotation (recommended for 2.5D games)")]
    [SerializeField] private bool lockRotation = true;

    [Tooltip("Fixed rotation angles (X, Y, Z) when rotation is locked")]
    [SerializeField] private Vector3 fixedRotation = new Vector3(0, 180, 0);

    private Canvas canvas;
    private Camera mainCam;

    void Start()
    {
        currentEnergy = maxEnergy;
        UpdateUI();

        // Get canvas and camera references
        canvas = GetComponentInChildren<Canvas>();
        mainCam = Camera.main;

        // Set canvas to World Space if available
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        if (followTarget == null)
        {
            Debug.LogWarning("No follow target set! Energy bar won't move.");
        }
        else
        {
            // Automatically setup collision detection on the player
            if (autoSetupCollisionDetection && reduceEnergyOnCollision)
            {
                CollisionForwarder forwarder = followTarget.GetComponent<CollisionForwarder>();
                if (forwarder == null)
                {
                    forwarder = followTarget.gameObject.AddComponent<CollisionForwarder>();
                    Debug.Log("Added CollisionForwarder to player");
                }
                forwarder.energySystem = this;
            }

            // Make sure the player has a collider
            Collider playerCollider = followTarget.GetComponent<Collider>();
            if (playerCollider == null)
            {
                Debug.LogWarning("Follow target doesn't have a collider! Collision detection won't work.");
            }
        }

        // Set initial rotation
        if (lockRotation)
        {
            transform.rotation = Quaternion.Euler(fixedRotation);
        }
    }

    void Update()
    {
        // Energy only changes on collision now
        // Timer-based energy reduction removed
    }

    void LateUpdate()
    {
        // Follow the target player in LateUpdate for smoother tracking
        if (followTarget != null)
        {
            transform.position = followTarget.position + offset;

            // Lock rotation to prevent spinning
            if (lockRotation)
            {
                transform.rotation = Quaternion.Euler(fixedRotation);
            }
        }
    }

    void UpdateUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }

        if (energyText != null)
        {
            energyText.text = $"{currentEnergy:F0}%";
        }
    }

    void OnEnergyDepleted()
    {
        Debug.Log("Energy depleted!");
        // Add your game over logic here
    }

    // Public methods for other scripts
    public void AddEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        UpdateUI();
    }

    public void RemoveEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        UpdateUI();

        if (currentEnergy <= 0)
        {
            OnEnergyDepleted();
        }
    }

    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }

    public float GetEnergyPercentage()
    {
        return (currentEnergy / maxEnergy) * 100f;
    }

    public void SetFollowTarget(Transform newTarget)
    {
        followTarget = newTarget;
    }

    // Collision detection methods - called by CollisionForwarder
    public void OnPlayerCollision(GameObject collisionObject)
    {
        if (!reduceEnergyOnCollision) return;

        // Check cooldown
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        // Check if collision is with damage-causing object
        if (ShouldTakeDamage(collisionObject))
        {
            RemoveEnergy(collisionEnergyLoss);
            lastCollisionTime = Time.time;

            if (showDebugInfo)
            {
                Debug.Log($"Energy reduced by collision with {collisionObject.name}! Current energy: {currentEnergy}");
            }
        }
    }

    public void OnPlayerTrigger(GameObject triggerObject)
    {
        if (!reduceEnergyOnCollision) return;

        // Check cooldown
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        // Check if trigger is with damage-causing object
        if (ShouldTakeDamage(triggerObject))
        {
            RemoveEnergy(collisionEnergyLoss);
            lastCollisionTime = Time.time;

            if (showDebugInfo)
            {
                Debug.Log($"Energy reduced by trigger with {triggerObject.name}! Current energy: {currentEnergy}");
            }
        }
    }

    private bool ShouldTakeDamage(GameObject obj)
    {
        // If no tags specified, damage from everything
        if (damageTags == null || damageTags.Length == 0)
        {
            return true;
        }

        // Check if object has any of the specified tags
        foreach (string tag in damageTags)
        {
            if (obj.CompareTag(tag))
            {
                return true;
            }
        }

        return false;
    }
}

// ===== CollisionForwarder.cs =====
// This component is automatically added to the player to forward collision events to the energy system
[System.Serializable]
public class CollisionForwarder : MonoBehaviour
{
    [HideInInspector]
    public EnergySystem energySystem;

    void OnCollisionEnter(Collision collision)
    {
        if (energySystem != null)
        {
            energySystem.OnPlayerCollision(collision.gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (energySystem != null)
        {
            energySystem.OnPlayerTrigger(other.gameObject);
        }
    }
}