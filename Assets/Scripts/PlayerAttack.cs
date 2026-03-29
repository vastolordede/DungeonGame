using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public float attackRange = 0.6f;
    public int attackDamage = 10;
    public LayerMask enemyLayer;

    [Header("Attack Points")]
    public Transform attackPointDown;
    public Transform attackPointSide;
    public Transform attackPointUp;

    [Header("Attack Timing")]
    public float attackLockTime = 0.22f;

    private Animator animator;
    private PlayerMovement movement;
    private PlayerStamina stamina;
    private PlayerInputActions inputActions;

    private bool isAttacking = false;
    private int currentAttackDirection = 0;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        stamina = GetComponent<PlayerStamina>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Attack.performed -= OnAttackPerformed;
            inputActions.Disable();
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (isAttacking)
            return;

        if (movement != null && movement.IsDashing)
            return;

        if (stamina == null)
        {
            Debug.LogWarning("Không tìm thấy PlayerStamina.");
            return;
        }

        if (!stamina.TryUseStamina(stamina.attackCost))
            return;

        currentAttackDirection = GetAttackDirection();
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        animator.SetBool("IsMoving", false);
        animator.SetInteger("Direction", currentAttackDirection);

        switch (currentAttackDirection)
        {
            case 0:
                animator.Play("D_Attack", 0, 0f);
                break;
            case 1:
                animator.Play("S_Attack", 0, 0f);
                break;
            case 2:
                animator.Play("U_Attack", 0, 0f);
                break;
            default:
                animator.Play("D_Attack", 0, 0f);
                break;
        }

        yield return new WaitForSeconds(attackLockTime);

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        animator.SetInteger("Direction", currentAttackDirection);
        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            switch (currentAttackDirection)
            {
                case 0:
                    animator.Play("Player_Walk_Down", 0, 0f);
                    break;
                case 1:
                    animator.Play("Player_Walk_Side", 0, 0f);
                    break;
                case 2:
                    animator.Play("Player_Walk_Up", 0, 0f);
                    break;
            }
        }
        else
        {
            switch (currentAttackDirection)
            {
                case 0:
                    animator.Play("Player_Idle_Down", 0, 0f);
                    break;
                case 1:
                    animator.Play("Player_Idle_Side", 0, 0f);
                    break;
                case 2:
                    animator.Play("Player_Idle_Up", 0, 0f);
                    break;
            }
        }

        isAttacking = false;
    }

    private int GetAttackDirection()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 0.01f)
        {
            float x = moveInput.x;
            float y = moveInput.y;

            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                return 1; // Side
            }

            return y > 0 ? 2 : 0; // Up : Down
        }

        if (movement != null)
            return movement.CurrentDirection;

        return 0;
    }

    public void DealDamage()
    {
        Transform currentAttackPoint = GetCurrentAttackPoint();
        if (currentAttackPoint == null)
        {
            Debug.LogWarning("Chưa gán attack point.");
            return;
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            currentAttackPoint.position,
            attackRange,
            enemyLayer
        );

        if (hitEnemies.Length == 0)
        {
            Debug.Log("❌ Đánh trượt");
            return;
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.gameObject == gameObject)
                continue;

            Debug.Log("✅ Đánh trúng: " + enemy.name);

            EnemyDummy dummy = enemy.GetComponent<EnemyDummy>();
            if (dummy != null)
            {
                dummy.TakeDamage(attackDamage);
            }
        }
    }

    private Transform GetCurrentAttackPoint()
    {
        switch (currentAttackDirection)
        {
            case 0:
                return attackPointDown;
            case 1:
                return attackPointSide;
            case 2:
                return attackPointUp;
            default:
                return attackPointDown;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (attackPointDown != null)
            Gizmos.DrawWireSphere(attackPointDown.position, attackRange);

        if (attackPointSide != null)
            Gizmos.DrawWireSphere(attackPointSide.position, attackRange);

        if (attackPointUp != null)
            Gizmos.DrawWireSphere(attackPointUp.position, attackRange);
    }
}