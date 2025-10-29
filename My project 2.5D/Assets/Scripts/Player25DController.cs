// ===== EnhancedPlayer25D.cs =====
// Place this on the NORMAL player
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnhancedPlayer25D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float acceleration = 50f;
    public float deceleration = 100f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpBufferTime = 0.2f;
    public float coyoteTime = 0.15f;
    public float gravity = 20f;
    public float fallGravityMultiplier = 1.5f;

    [Header("Depth Movement (2.5D)")]
    public float depthMoveSpeed = 3f;
    public float maxDepth = 2f;
    public float minDepth = -2f;
    public float depthSmoothness = 0.15f;
    public KeyCode depthForwardKey = KeyCode.W;
    public KeyCode depthBackKey = KeyCode.S;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.3f;
    public float groundCheckDistance = 0.15f;
    public LayerMask groundLayer;
    public float maxSlopeAngle = 45f;

    [Header("Visual Settings")]
    public bool flipSpriteOnMove = true;
    public float scaleChangeWithDepth = 0.3f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Transform groundCheck;
    private SpriteRenderer spriteRenderer;

    private bool isGrounded;
    private bool wasGrounded;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    private float horizontalInput;
    private float depthInput;
    private float currentVelocityX;
    private float targetDepth;
    private float verticalVelocity;

    private Vector3 initialScale;
    private bool canJump;
    private bool isMovingBackwards;
    private bool isMovingForward;

    public Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("EnhancedPlayer25D: Rigidbody component required!");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.drag = 10f;
        rb.angularDrag = 0f;
        rb.mass = 1f;

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 2f;
            capsuleCollider.radius = 0.5f;
            capsuleCollider.center = new Vector3(0, 1, 0);
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        if (anim == null)
        {
            Debug.LogWarning("EnhancedPlayer25D: No Animator component found!");
        }

        initialScale = transform.localScale;

        Transform existingGroundCheck = transform.Find("GroundCheck");
        if (existingGroundCheck != null)
        {
            groundCheck = existingGroundCheck;
        }
        else
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -capsuleCollider.height / 2, 0);
            groundCheck = groundCheckObj.transform;
        }

        targetDepth = transform.position.z;
        Debug.Log("EnhancedPlayer25D initialized successfully!");
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        depthInput = 0f;
        if (Input.GetKey(depthForwardKey)) depthInput = 1f;
        else if (Input.GetKey(depthBackKey)) depthInput = -1f;

        wasGrounded = isGrounded;
        isGrounded = CheckGrounded();

        if (isGrounded && !wasGrounded)
        {
            if (showDebugInfo) Debug.Log("Player landed");
        }

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVelocity <= 0)
            {
                verticalVelocity = -2f;
            }
            canJump = true;
        }

        if (Input.GetButtonDown("Jump"))
        {
            lastJumpPressedTime = Time.time;
        }

        bool jumpBufferActive = lastJumpPressedTime + jumpBufferTime > Time.time;
        bool coyoteTimeActive = lastGroundedTime + coyoteTime > Time.time;

        if (jumpBufferActive && coyoteTimeActive && canJump)
        {
            verticalVelocity = jumpForce;
            lastGroundedTime = 0;
            lastJumpPressedTime = 0;
            canJump = false;
        }

        if (!isGrounded)
        {
            float gravityMultiplier = verticalVelocity < 0 ? fallGravityMultiplier : 1f;
            verticalVelocity -= gravity * gravityMultiplier * Time.deltaTime;
        }

        if (Input.GetButtonUp("Jump") && verticalVelocity > 0)
        {
            verticalVelocity *= 0.5f;
        }

        if (anim != null)
        {
            anim.SetBool("isGrounded", isGrounded);
            isMovingBackwards = depthInput > 0;
            isMovingForward = depthInput < 0;
            anim.SetBool("movingBackwards", isMovingBackwards);
            anim.SetBool("movingForward", isMovingForward);
            float totalMovement = Mathf.Abs(horizontalInput) + Mathf.Abs(depthInput);
            anim.SetFloat("Speed", totalMovement > 0 ? moveSpeed : 0f);
        }

        if (flipSpriteOnMove && spriteRenderer != null && horizontalInput != 0 && !isMovingBackwards && !isMovingForward)
        {
            spriteRenderer.flipX = horizontalInput < 0;
        }

        if (depthInput != 0)
        {
            targetDepth += depthInput * depthMoveSpeed * Time.deltaTime;
            targetDepth = Mathf.Clamp(targetDepth, minDepth, maxDepth);
        }
        else
        {
            targetDepth = transform.position.z;
        }

        if (scaleChangeWithDepth > 0)
        {
            float depthPercent = (transform.position.z - minDepth) / (maxDepth - minDepth);
            depthPercent = Mathf.Clamp01(depthPercent);
            float scaleMultiplier = 1f - (depthPercent * scaleChangeWithDepth);
            transform.localScale = initialScale * scaleMultiplier;
        }
    }

    bool CheckGrounded()
    {
        if (capsuleCollider == null) return false;

        Vector3 center = capsuleCollider.bounds.center;
        float radius = capsuleCollider.radius * 0.9f;
        float distance = capsuleCollider.bounds.extents.y + groundCheckDistance;

        RaycastHit hit;

        if (Physics.Raycast(center, Vector3.down, out hit, distance, groundLayer))
        {
            if (showDebugInfo) Debug.DrawRay(center, Vector3.down * distance, Color.green);
            return true;
        }

        if (Physics.Raycast(center + transform.forward * radius * 0.5f, Vector3.down, out hit, distance, groundLayer) ||
            Physics.Raycast(center - transform.forward * radius * 0.5f, Vector3.down, out hit, distance, groundLayer) ||
            Physics.Raycast(center + transform.right * radius * 0.5f, Vector3.down, out hit, distance, groundLayer) ||
            Physics.Raycast(center - transform.right * radius * 0.5f, Vector3.down, out hit, distance, groundLayer))
        {
            return true;
        }

        if (showDebugInfo) Debug.DrawRay(center, Vector3.down * distance, Color.red);
        return false;
    }

    void FixedUpdate()
    {
        float targetVelocityX = horizontalInput * moveSpeed;

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            currentVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocityX, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            currentVelocityX = 0f;
        }

        verticalVelocity = Mathf.Clamp(verticalVelocity, -50f, 50f);
        rb.velocity = new Vector3(currentVelocityX, verticalVelocity, 0);

        if (Mathf.Abs(depthInput) > 0.01f)
        {
            Vector3 newPos = transform.position;
            newPos.z = targetDepth;
            transform.position = newPos;
        }
    }

    public void ResetToPosition(Vector3 position)
    {
        transform.position = position;
        targetDepth = position.z;
        verticalVelocity = 0;
        currentVelocityX = 0;
        rb.velocity = Vector3.zero;
    }

    // PUBLIC GETTER METHODS - For InvertedPlayerMirror
    public bool IsGrounded()
    {
        return isGrounded;
    }

    public Vector3 GetVelocity()
    {
        return new Vector3(currentVelocityX, verticalVelocity, 0);
    }

    public bool IsMovingBackwards()
    {
        return isMovingBackwards;
    }

    public float GetHorizontalInput()
    {
        return horizontalInput;
    }

    public float GetDepthInput()
    {
        return depthInput;
    }

    public float GetVerticalVelocity()
    {
        return verticalVelocity;
    }
}