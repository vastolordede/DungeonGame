using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 4f;

    [Header("Dash")]
    public float dashSpeed = 10f;
    public float dashDuration = 0.12f;
    public float dashCooldown = 0.3f;
    public float doubleTapTime = 0.25f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private PlayerStamina stamina;

    // 0 = Down, 1 = Side, 2 = Up
    private int direction = 0;

    public bool IsDashing => isDashing;
public int CurrentDirection => direction;

    private enum LastPressedDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    private LastPressedDirection lastPressed = LastPressedDirection.None;

    private LastPressedDirection lastVerticalPressed = LastPressedDirection.None;
    private LastPressedDirection lastHorizontalPressed = LastPressedDirection.None;

    private Vector2 lastResolvedMoveInput = Vector2.zero;

    // Dash state
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.zero;

    // Double tap detect
    private LastPressedDirection lastTapDirection = LastPressedDirection.None;
    private float lastTapTime = -999f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        inputActions = new PlayerInputActions();
        stamina = GetComponent<PlayerStamina>();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        PlayerAttack attack = GetComponent<PlayerAttack>();
if (attack != null && attack.IsAttacking)
{
    animator.SetBool("IsMoving", false);
    return;
}
        UpdateTimers();

        if (isDashing)
        {
            animator.SetBool("IsMoving", false);
            animator.SetInteger("Direction", direction);
            return;
        }

        UpdateLastPressedDirection();
        moveInput = ResolveInputByPriority();

        bool isMoving = moveInput != Vector2.zero;
        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            UpdateFacingByMoveInput(moveInput);
        }

        animator.SetInteger("Direction", direction);
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
        }
        else
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    private void UpdateTimers()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                dashDirection = Vector2.zero;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void UpdateLastPressedDirection()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
        {
            HandleDirectionTap(LastPressedDirection.Up);
            lastPressed = LastPressedDirection.Up;
            lastVerticalPressed = LastPressedDirection.Up;
        }
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
        {
            HandleDirectionTap(LastPressedDirection.Down);
            lastPressed = LastPressedDirection.Down;
            lastVerticalPressed = LastPressedDirection.Down;
        }
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
        {
            HandleDirectionTap(LastPressedDirection.Left);
            lastPressed = LastPressedDirection.Left;
            lastHorizontalPressed = LastPressedDirection.Left;
        }
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
        {
            HandleDirectionTap(LastPressedDirection.Right);
            lastPressed = LastPressedDirection.Right;
            lastHorizontalPressed = LastPressedDirection.Right;
        }
    }

    private void HandleDirectionTap(LastPressedDirection currentTap)
    {
        if (isDashing || dashCooldownTimer > 0f)
        {
            lastTapDirection = currentTap;
            lastTapTime = Time.time;
            return;
        }

        bool isDoubleTap =
            currentTap == lastTapDirection &&
            Time.time - lastTapTime <= doubleTapTime;

        if (isDoubleTap)
        {
            StartDash(currentTap);
        }

        lastTapDirection = currentTap;
        lastTapTime = Time.time;
    }

    private void StartDash(LastPressedDirection dashInputDirection)
{
    Vector2 resolvedDashDirection = DirectionEnumToVector(dashInputDirection);
    if (resolvedDashDirection == Vector2.zero) return;

    if (stamina == null) return;
    if (!stamina.TryUseStamina(stamina.dashCost)) return;

    isDashing = true;
    dashTimer = dashDuration;
    dashCooldownTimer = dashCooldown;
    dashDirection = resolvedDashDirection;
    moveInput = Vector2.zero;
    lastResolvedMoveInput = Vector2.zero;

    UpdateFacingByMoveInput(resolvedDashDirection);

    animator.SetBool("IsMoving", false);
    animator.SetInteger("Direction", direction);
    animator.SetTrigger("Dash");
}

    private void UpdateFacingByMoveInput(Vector2 input)
    {
        if (input.x > 0f)
        {
            direction = 1;
            sr.flipX = true;   // sprite side gốc quay trái
        }
        else if (input.x < 0f)
        {
            direction = 1;
            sr.flipX = false;
        }
        else if (input.y > 0f)
        {
            direction = 2;
        }
        else if (input.y < 0f)
        {
            direction = 0;
        }
    }

    private Vector2 DirectionEnumToVector(LastPressedDirection dir)
    {
        switch (dir)
        {
            case LastPressedDirection.Up:
                return Vector2.up;
            case LastPressedDirection.Down:
                return Vector2.down;
            case LastPressedDirection.Left:
                return Vector2.left;
            case LastPressedDirection.Right:
                return Vector2.right;
            default:
                return Vector2.zero;
        }
    }

    private Vector2 ResolveInputByPriority()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        bool up = kb.wKey.isPressed || kb.upArrowKey.isPressed;
        bool down = kb.sKey.isPressed || kb.downArrowKey.isPressed;
        bool left = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
        bool right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;

        int pressedCount = 0;
        if (up) pressedCount++;
        if (down) pressedCount++;
        if (left) pressedCount++;
        if (right) pressedCount++;

        float x = 0f;
        float y = 0f;

        if (up && down)
        {
            if (lastVerticalPressed == LastPressedDirection.Up)
                y = 1f;
            else if (lastVerticalPressed == LastPressedDirection.Down)
                y = -1f;
        }
        else if (up)
        {
            y = 1f;
        }
        else if (down)
        {
            y = -1f;
        }

        if (left && right)
        {
            if (lastHorizontalPressed == LastPressedDirection.Left)
                x = -1f;
            else if (lastHorizontalPressed == LastPressedDirection.Right)
                x = 1f;
        }
        else if (left)
        {
            x = -1f;
        }
        else if (right)
        {
            x = 1f;
        }

        if (pressedCount == 0)
        {
            lastResolvedMoveInput = Vector2.zero;
            return Vector2.zero;
        }

        if (pressedCount >= 3)
        {
            if (lastResolvedMoveInput == Vector2.up && y > 0f)
                return Vector2.up;

            if (lastResolvedMoveInput == Vector2.down && y < 0f)
                return Vector2.down;

            if (lastResolvedMoveInput == Vector2.left && x < 0f)
                return Vector2.left;

            if (lastResolvedMoveInput == Vector2.right && x > 0f)
                return Vector2.right;
        }

        Vector2 resolved = Vector2.zero;

        if (x != 0f && y == 0f)
        {
            resolved = new Vector2(x, 0f);
        }
        else if (y != 0f && x == 0f)
        {
            resolved = new Vector2(0f, y);
        }
        else
        {
            switch (lastPressed)
            {
                case LastPressedDirection.Up:
                    if (y > 0f) resolved = Vector2.up;
                    break;

                case LastPressedDirection.Down:
                    if (y < 0f) resolved = Vector2.down;
                    break;

                case LastPressedDirection.Left:
                    if (x < 0f) resolved = Vector2.left;
                    break;

                case LastPressedDirection.Right:
                    if (x > 0f) resolved = Vector2.right;
                    break;
            }

            if (resolved == Vector2.zero)
            {
                if (y > 0f) resolved = Vector2.up;
                else if (y < 0f) resolved = Vector2.down;
                else if (x < 0f) resolved = Vector2.left;
                else if (x > 0f) resolved = Vector2.right;
            }
        }

        lastResolvedMoveInput = resolved;
        return resolved;
    }
}