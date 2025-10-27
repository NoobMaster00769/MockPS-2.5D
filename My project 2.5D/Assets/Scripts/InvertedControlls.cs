//using UnityEngine;

// ===== InvertedPlayerMirror.cs =====
// Place this on the INVERTED player (SEPARATE FILE!)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvertedPlayerMirror : MonoBehaviour
{
    [Header("Mirror Settings")]
    public EnhancedPlayer25D originalPlayer;
    public float mirrorAxisY = 0f;
    public float mirrorAxisX = 0f;
    public float mirrorAxisZ = 0f;
    public bool autoFindPlayer = true;

    [Header("Inversion Options")]
    public bool invertX = true;
    public bool invertY = true;
    public bool invertZ = true;
    public bool invertSpriteFlip = true;

    [Header("Animation Sync")]
    public bool syncAnimations = true;
    public bool invertDepthAnimations = true;

    [Header("Visual Settings")]
    public bool applyMirrorEffect = true;
    [Range(0.3f, 1f)] public float mirrorAlpha = 0.7f;
    public Color mirrorTint = new Color(0.8f, 0.8f, 1f, 1f);

    [Header("Debug")]
    public bool showDebugInfo = true;

    private SpriteRenderer mirrorSpriteRenderer;
    private SpriteRenderer originalSpriteRenderer;
    private Animator mirrorAnimator;
    private Animator originalAnimator;
    private Vector3 lastOriginalPosition;
    private bool initialized = false;

    void Start()
    {
        if (autoFindPlayer && originalPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                originalPlayer = playerObj.GetComponent<EnhancedPlayer25D>();
                if (originalPlayer != null)
                    Debug.Log("InvertedPlayerMirror: Auto-found player");
                else
                {
                    Debug.LogError("InvertedPlayerMirror: No EnhancedPlayer25D found!");
                    enabled = false;
                    return;
                }
            }
            else
            {
                Debug.LogError("InvertedPlayerMirror: No player with 'Player' tag!");
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

        if (transform.position != Vector3.zero)
        {
            mirrorAxisX = (originalPlayer.transform.position.x + transform.position.x) / 2f;
            mirrorAxisY = (originalPlayer.transform.position.y + transform.position.y) / 2f;
            mirrorAxisZ = (originalPlayer.transform.position.z + transform.position.z) / 2f;
        }
        else
        {
            mirrorAxisX = originalPlayer.transform.position.x;
            mirrorAxisY = originalPlayer.transform.position.y;
            mirrorAxisZ = originalPlayer.transform.position.z;
        }

        Vector3 initialMirrorPos = transform.position;
        if (invertY)
        {
            float distY = originalPlayer.transform.position.y - mirrorAxisY;
            initialMirrorPos.y = mirrorAxisY - distY + 5f;
        }
        transform.position = initialMirrorPos;

        mirrorSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalSpriteRenderer = originalPlayer.GetComponentInChildren<SpriteRenderer>();

        if (syncAnimations)
        {
            mirrorAnimator = GetComponentInChildren<Animator>();
            originalAnimator = originalPlayer.GetComponentInChildren<Animator>();
        }

        if (applyMirrorEffect && mirrorSpriteRenderer != null)
        {
            Color c = mirrorTint;
            c.a = mirrorAlpha;
            mirrorSpriteRenderer.color = c;
        }

        lastOriginalPosition = originalPlayer.transform.position;
        UpdateMirrorPosition();
        initialized = true;

        Debug.Log("InvertedPlayerMirror initialized!");
    }

    void Update()
    {
        if (initialized && originalPlayer != null && syncAnimations)
        {
            SyncAnimationParameters();
        }
    }

    void LateUpdate()
    {
        if (!initialized || originalPlayer == null) return;

        UpdateMirrorPosition();
        UpdateMirrorRotationAndScale();
    }

    void UpdateMirrorPosition()
    {
        Vector3 originalPos = originalPlayer.transform.position;
        Vector3 newPos = Vector3.zero;

        if (invertX)
        {
            float distX = originalPos.x - mirrorAxisX;
            newPos.x = mirrorAxisX - distX;
        }
        else newPos.x = originalPos.x;

        if (invertY)
        {
            float distY = originalPos.y - mirrorAxisY;
            newPos.y = mirrorAxisY - distY;
        }
        else newPos.y = originalPos.y;

        if (invertZ)
        {
            float distZ = originalPos.z - mirrorAxisZ;
            newPos.z = mirrorAxisZ - distZ;
        }
        else newPos.z = originalPos.z;

        transform.position = newPos;
        lastOriginalPosition = originalPos;
    }

    void UpdateMirrorRotationAndScale()
    {
        Vector3 scale = originalPlayer.transform.localScale;
        if (invertY) scale.y = -Mathf.Abs(scale.y);
        transform.localScale = scale;

        if (mirrorSpriteRenderer != null && originalSpriteRenderer != null)
        {
            mirrorSpriteRenderer.flipX = invertSpriteFlip && invertX
                ? originalSpriteRenderer.flipX
                : !originalSpriteRenderer.flipX;
        }
    }

    void SyncAnimationParameters()
    {
        if (mirrorAnimator == null || originalAnimator == null) return;

        if (HasParameter(originalAnimator, "Speed"))
            mirrorAnimator.SetFloat("Speed", originalAnimator.GetFloat("Speed"));

        if (HasParameter(originalAnimator, "isGrounded"))
            mirrorAnimator.SetBool("isGrounded", originalAnimator.GetBool("isGrounded"));

        if (invertDepthAnimations)
        {
            if (HasParameter(originalAnimator, "movingBackwards") && HasParameter(mirrorAnimator, "movingForward"))
                mirrorAnimator.SetBool("movingForward", originalAnimator.GetBool("movingBackwards"));
            if (HasParameter(originalAnimator, "movingForward") && HasParameter(mirrorAnimator, "movingBackwards"))
                mirrorAnimator.SetBool("movingBackwards", originalAnimator.GetBool("movingForward"));
        }
        else
        {
            if (HasParameter(originalAnimator, "movingBackwards"))
                mirrorAnimator.SetBool("movingBackwards", originalAnimator.GetBool("movingBackwards"));
            if (HasParameter(originalAnimator, "movingForward"))
                mirrorAnimator.SetBool("movingForward", originalAnimator.GetBool("movingForward"));
        }
    }

    private bool HasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
            if (param.name == paramName)
                return true;
        return false;
    }

    public void SetMirrorAxes(float x, float y, float z)
    {
        mirrorAxisX = x;
        mirrorAxisY = y;
        mirrorAxisZ = z;
    }

    public void UpdateMirrorAxesToCurrentPlayer()
    {
        if (originalPlayer != null)
        {
            mirrorAxisX = originalPlayer.transform.position.x;
            mirrorAxisY = originalPlayer.transform.position.y;
            mirrorAxisZ = originalPlayer.transform.position.z;
        }
    }

    public void SetMirrorEffect(bool enable, float alpha, Color tint)
    {
        applyMirrorEffect = enable;
        mirrorAlpha = alpha;
        mirrorTint = tint;

        if (mirrorSpriteRenderer != null)
        {
            Color c = tint;
            c.a = alpha;
            mirrorSpriteRenderer.color = enable ? c : Color.white;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (originalPlayer == null) return;

        float len = 50f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(originalPlayer.transform.position.x - len, mirrorAxisY, originalPlayer.transform.position.z),
            new Vector3(originalPlayer.transform.position.x + len, mirrorAxisY, originalPlayer.transform.position.z)
        );

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(mirrorAxisX, originalPlayer.transform.position.y - len, originalPlayer.transform.position.z),
            new Vector3(mirrorAxisX, originalPlayer.transform.position.y + len, originalPlayer.transform.position.z)
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(originalPlayer.transform.position, transform.position);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(originalPlayer.transform.position, 0.3f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(new Vector3(mirrorAxisX, mirrorAxisY, originalPlayer.transform.position.z), 0.5f);
    }
}