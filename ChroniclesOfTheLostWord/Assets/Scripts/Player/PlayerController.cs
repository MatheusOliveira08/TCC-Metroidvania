using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 14f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool jumpRequested;
    private bool dashRequested;
    private bool isGrounded;
    private bool isDashing;
    private float dashTimer;
    private float lastDashTime = float.NegativeInfinity;
    private float lastMoveDirection = 1f;
    private float dashDirection = 1f;

    public event System.Action OnPlayerJump;
    public event System.Action OnPlayerAttack;
    public event System.Action OnPlayerDash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        moveInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                moveInput -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                moveInput += 1f;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpRequested = true;
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                HandleAttackInput();
            }

            if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame)
            {
                HandleDashInput();
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleAttackInput();
        }

        if (Gamepad.current == null)
        {
            return;
        }

        moveInput += Gamepad.current.leftStick.ReadValue().x;
        moveInput = Mathf.Clamp(moveInput, -1f, 1f);

        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            jumpRequested = true;
        }

        if (Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            HandleAttackInput();
        }

        if (Gamepad.current.rightShoulder.wasPressedThisFrame)
        {
            HandleDashInput();
        }
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        if (dashRequested)
        {
            TryStartDash();
        }

        if (isDashing)
        {
            ApplyDashMovement();
            return;
        }

        ApplyMovement();

        if (jumpRequested)
        {
            ApplyJump();
        }
    }

    private void CheckGrounded()
    {
        isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void ApplyMovement()
    {
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            lastMoveDirection = Mathf.Sign(moveInput);
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void ApplyJump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            OnPlayerJump?.Invoke();
        }

        jumpRequested = false;
    }

    private void HandleAttackInput()
    {
        Debug.Log("Atacou!");
        OnPlayerAttack?.Invoke();
    }

    private void HandleDashInput()
    {
        dashRequested = true;
    }

    private void TryStartDash()
    {
        dashRequested = false;

        if (isDashing || Time.time < lastDashTime + dashCooldown)
        {
            return;
        }

        dashDirection = Mathf.Abs(moveInput) > 0.01f ? Mathf.Sign(moveInput) : lastMoveDirection;
        lastMoveDirection = dashDirection;
        dashTimer = dashDuration;
        lastDashTime = Time.time;
        isDashing = true;
        OnPlayerDash?.Invoke();
    }

    private void ApplyDashMovement()
    {
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            isDashing = false;
        }
    }
}
