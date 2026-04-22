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
    private SpriteRenderer sr;

    private bool isAttacking = false;
    private int currentAttackDirection = 0;

    private Vector3 sidePointBaseLocalPos;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        stamina = GetComponent<PlayerStamina>();
        inputActions = new PlayerInputActions();
        sr = GetComponent<SpriteRenderer>();

        if (attackPointSide != null)
            sidePointBaseLocalPos = attackPointSide.localPosition;
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

    private void LateUpdate()
    {
        UpdateAttackPointSideFlip();
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

        yield return new WaitForSeconds(0.08f);

        DealDamage();

        float remain = attackLockTime - 0.08f;
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

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
                return 1;

            return y > 0 ? 2 : 0;
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

        Vector2 hitCenter;
        Vector2 capsuleSize;
        CapsuleDirection2D capsuleDirection;

        switch (currentAttackDirection)
        {
            case 0: // Down
                hitCenter = currentAttackPoint.position;
                capsuleSize = new Vector2(0.95f, 1.1f);
                capsuleDirection = CapsuleDirection2D.Vertical;
                break;

            case 1: // Side
                hitCenter = GetSideAttackCenter();
                capsuleSize = new Vector2(1.2f, 0.85f);
                capsuleDirection = CapsuleDirection2D.Horizontal;
                break;

            case 2: // Up
                hitCenter = currentAttackPoint.position;
                capsuleSize = new Vector2(0.95f, 1.1f);
                capsuleDirection = CapsuleDirection2D.Vertical;
                break;

            default:
                hitCenter = currentAttackPoint.position;
                capsuleSize = new Vector2(1f, 1f);
                capsuleDirection = CapsuleDirection2D.Vertical;
                break;
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCapsuleAll(
            hitCenter,
            capsuleSize,
            capsuleDirection,
            0f,
            enemyLayer
        );

        Debug.Log("DEAL DAMAGE");
        Debug.Log("Dir = " + currentAttackDirection);
        Debug.Log("HitCenter = " + hitCenter);
        Debug.Log("Hit count = " + hitEnemies.Length);

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

    EnemyDummy dummy = enemy.GetComponentInParent<EnemyDummy>();
    if (dummy != null)
    {
        dummy.TakeDamage(attackDamage);
        continue;
    }

    EnemyMeleeAI meleeAI = enemy.GetComponentInParent<EnemyMeleeAI>();
    if (meleeAI != null)
    {
        meleeAI.TakeDamage(attackDamage);
        continue;
    }

    EnemyRangedAI rangedAI = enemy.GetComponentInParent<EnemyRangedAI>();
    if (rangedAI != null)
    {
        rangedAI.TakeDamage(attackDamage);
        continue;
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

    private Vector2 GetSideAttackCenter()
    {
        if (attackPointSide == null)
            return transform.position;

        float offsetX = Mathf.Abs(sidePointBaseLocalPos.x);
        float offsetY = sidePointBaseLocalPos.y;

        // Theo PlayerMovement hiện tại:
        // sr.flipX = true  => quay phải
        // sr.flipX = false => quay trái
        bool facingRight = sr != null && sr.flipX;

        Vector3 localOffset = new Vector3(
            facingRight ? offsetX : -offsetX,
            offsetY,
            0f
        );

        return transform.TransformPoint(localOffset);
    }

    private void UpdateAttackPointSideFlip()
    {
        if (attackPointSide == null)
            return;

        float offsetX = Mathf.Abs(sidePointBaseLocalPos.x);
        float offsetY = sidePointBaseLocalPos.y;
        float offsetZ = sidePointBaseLocalPos.z;

        // Theo PlayerMovement hiện tại:
        // sr.flipX = true  => quay phải
        // sr.flipX = false => quay trái
        bool facingRight = sr != null && sr.flipX;

        attackPointSide.localPosition = new Vector3(
            facingRight ? offsetX : -offsetX,
            offsetY,
            offsetZ
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (attackPointDown != null)
            Gizmos.DrawWireCube(attackPointDown.position, new Vector3(0.95f, 1.1f, 0f));

        if (attackPointUp != null)
            Gizmos.DrawWireCube(attackPointUp.position, new Vector3(0.95f, 1.1f, 0f));

        if (attackPointSide != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            float baseX = Mathf.Abs(attackPointSide.localPosition.x);
            float baseY = attackPointSide.localPosition.y;

            // Đồng bộ đúng với PlayerMovement:
            // flipX true = quay phải
            bool facingRight = spriteRenderer != null && spriteRenderer.flipX;

            Vector3 sideWorldPos = transform.TransformPoint(
                new Vector3(facingRight ? baseX : -baseX, baseY, 0f)
            );

            Gizmos.DrawWireCube(sideWorldPos, new Vector3(1.2f, 0.85f, 0f));
        }
    }
}