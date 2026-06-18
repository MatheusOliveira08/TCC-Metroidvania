using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 14f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;

    public event System.Action OnJumpPerformed;
    public event System.Action OnAttackPerformed;

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
    }

    private void FixedUpdate()
    {
        CheckGrounded();
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
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void ApplyJump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            OnJumpPerformed?.Invoke();
        }

        jumpRequested = false;
    }

    private void HandleAttackInput()
    {
        Debug.Log("Atacou!");
        OnAttackPerformed?.Invoke();
    }
}
