using UnityEngine;

public class EnemyRangedAI : MonoBehaviour
{
    private enum State
    {
        Idle,
        Approach,
        Attack,
        Retreat,
        Return
    }

    [Header("Enemy HP")]
    public int hp = 30;
    private int lastHP;

    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Detect / Range Control")]
    public float detectRange = 8f;
    public float preferredAttackDistance = 4.5f;
    public float tooCloseDistance = 1.5f;
    public float loseTargetRange = 10f;
    public float maxChaseDistanceFromSpawn = 7f;
    public float stopReturnDistance = 0.05f;

    [Header("Attack")]
    public int damagePerShot = 5;
    public float attackWindup = 0.4f;
    public float attackCooldown = 0.35f;
    public int shotsPerBurst = 2;
    public float attackExitRange = 5.5f;

    [Header("Retreat")]
    public float retreatTargetDistance = 3.5f;
    public float retreatLockDuration = 0.8f;

    [Header("Projectile")]
public GameObject fireballPrefab;
public Transform firePoint;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    private Vector2 spawnPosition;
    private Vector2 moveDirection = Vector2.zero;

    private State currentState = State.Idle;

    // 0 = Down, 1 = Side, 2 = Up
    private int currentDirection = 0;

    private State lastLoggedState;
    private bool firstStateLog = true;

    private float attackWindupTimer = 0f;
    private float attackCooldownTimer = 0f;
    private float retreatLockTimer = 0f;

    private bool hasResolvedShotThisCycle = false;
    private int shotsDoneInBurst = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        spawnPosition = transform.position;
        lastHP = hp;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (hp <= 0)
            return;

        if (attackWindupTimer > 0f)
            attackWindupTimer -= Time.deltaTime;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (retreatLockTimer > 0f)
            retreatLockTimer -= Time.deltaTime;

        if (player == null)
        {
            moveDirection = Vector2.zero;
            currentState = State.Idle;
            UpdateAnimator();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float distanceToSpawn = Vector2.Distance(transform.position, spawnPosition);

        switch (currentState)
        {
            case State.Idle:
                UpdateIdle(distanceToPlayer);
                break;

            case State.Approach:
                UpdateApproach(distanceToPlayer, distanceToSpawn);
                break;

            case State.Attack:
                UpdateAttack(distanceToPlayer, distanceToSpawn);
                break;

            case State.Retreat:
                UpdateRetreat(distanceToPlayer, distanceToSpawn);
                break;

            case State.Return:
                UpdateReturn(distanceToPlayer);
                break;
        }

        UpdateAnimator();
        LogState();
    }

    private void FixedUpdate()
    {
        if (hp <= 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveDirection * moveSpeed;
    }

    private void UpdateIdle(float distanceToPlayer)
    {
        moveDirection = Vector2.zero;

        if (distanceToPlayer <= detectRange)
        {
            currentState = State.Approach;
        }
    }

    private void UpdateApproach(float distanceToPlayer, float distanceToSpawn)
    {
        if (distanceToPlayer > loseTargetRange || distanceToSpawn > maxChaseDistanceFromSpawn)
        {
            moveDirection = Vector2.zero;
            retreatLockTimer = retreatLockDuration;
            currentState = State.Return;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (toPlayer.sqrMagnitude <= 0.0001f)
        {
            moveDirection = Vector2.zero;
            return;
        }

        Vector2 dir = toPlayer.normalized;
        UpdateDirection(dir);

        if (distanceToPlayer <= tooCloseDistance)
        {
            EnterRetreatState();
            return;
        }

        if (distanceToPlayer <= preferredAttackDistance)
        {
            EnterAttackState();
            return;
        }

        moveDirection = dir;
    }

    private void EnterAttackState()
    {
        currentState = State.Attack;
        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        hasResolvedShotThisCycle = false;
        shotsDoneInBurst = 0;

        if (attackWindupTimer <= 0f && attackCooldownTimer <= 0f)
            attackWindupTimer = attackWindup;
    }

    private void ExitAttackState(State nextState)
    {
        hasResolvedShotThisCycle = false;
        moveDirection = Vector2.zero;
        currentState = nextState;
    }

    private void UpdateAttack(float distanceToPlayer, float distanceToSpawn)
    {
        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (toPlayer.sqrMagnitude > 0.0001f)
            UpdateDirection(toPlayer.normalized);

        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        // Trong Attack thì không được bỏ cast giữa chừng.
        // Phải bắn đủ shotsPerBurst phát rồi mới xét đổi state.
        if (shotsDoneInBurst < shotsPerBurst)
        {
            if (!hasResolvedShotThisCycle)
            {
                if (attackWindupTimer > 0f)
                    return;

                DoAttack();
                hasResolvedShotThisCycle = true;
                attackCooldownTimer = attackCooldown;
                return;
            }

            if (attackCooldownTimer > 0f)
                return;

            shotsDoneInBurst++;
            hasResolvedShotThisCycle = false;

            if (shotsDoneInBurst < shotsPerBurst)
            {
                attackWindupTimer = attackWindup;
            }

            return;
        }

        // Chỉ sau khi bắn đủ burst mới quyết định state tiếp theo
        if (distanceToPlayer > loseTargetRange || distanceToSpawn > maxChaseDistanceFromSpawn)
        {
            retreatLockTimer = retreatLockDuration;
            ExitAttackState(State.Return);
            return;
        }

        if (distanceToPlayer <= tooCloseDistance)
        {
            EnterRetreatState();
            return;
        }

        if (distanceToPlayer > attackExitRange)
        {
            ExitAttackState(State.Approach);
            return;
        }

        // Nếu player vẫn trong vùng bắn đẹp thì bắt đầu burst mới
        hasResolvedShotThisCycle = false;
        shotsDoneInBurst = 0;
        attackWindupTimer = attackWindup;
    }

    private void EnterRetreatState()
    {
        currentState = State.Retreat;
        retreatLockTimer = retreatLockDuration;
        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateRetreat(float distanceToPlayer, float distanceToSpawn)
    {
        if (distanceToPlayer > loseTargetRange || distanceToSpawn > maxChaseDistanceFromSpawn)
        {
            moveDirection = Vector2.zero;
            currentState = State.Return;
            return;
        }

        Vector2 awayFromPlayer = rb.position - (Vector2)player.position;

        if (awayFromPlayer.sqrMagnitude <= 0.0001f)
            awayFromPlayer = Vector2.down;

        Vector2 dir = awayFromPlayer.normalized;
        UpdateDirection(dir);
        moveDirection = dir;

        // Trong khoảng lock thì bắt buộc chạy tiếp để giữ nhịp, tránh khựng cast
        if (retreatLockTimer > 0f)
            return;

        // Đủ xa để quay lại bắn
        if (distanceToPlayer >= retreatTargetDistance &&
            distanceToPlayer <= preferredAttackDistance)
        {
            EnterAttackState();
            return;
        }

        // Lùi quá xa thì quay lại dí vào vùng bắn đẹp
        if (distanceToPlayer > preferredAttackDistance)
        {
            currentState = State.Approach;
            return;
        }
    }

    private void UpdateReturn(float distanceToPlayer)
    {
        float distanceToSpawn = Vector2.Distance(transform.position, spawnPosition);

        if (retreatLockTimer <= 0f &&
            distanceToPlayer <= detectRange &&
            distanceToSpawn <= maxChaseDistanceFromSpawn)
        {
            currentState = State.Approach;
            return;
        }

        Vector2 toSpawn = spawnPosition - rb.position;
        float distanceBack = toSpawn.magnitude;

        if (distanceBack <= stopReturnDistance)
        {
            rb.position = spawnPosition;
            moveDirection = Vector2.zero;
            currentState = State.Idle;
            return;
        }

        Vector2 dir = toSpawn.normalized;
        UpdateDirection(dir);
        moveDirection = dir;
    }

    private void DoAttack()
{
    animator.SetTrigger("Attack");

    if (fireballPrefab == null || firePoint == null || player == null)
    {
        Debug.Log("Missing fireballPrefab / firePoint / player");
        return;
    }

    Vector2 shootDir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;

    GameObject bullet = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);

    EnemyProjectile projectile = bullet.GetComponent<EnemyProjectile>();
    if (projectile != null)
    {
        projectile.Init(shootDir, damagePerShot);
    }

    Debug.Log("Enemy ranged cast shot: -" + damagePerShot);
}

    private void UpdateDirection(Vector2 dir)
    {
        float x = dir.x;
        float y = dir.y;

        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            currentDirection = 1;
            sr.flipX = x > 0;
        }
        else
        {
            sr.flipX = false;
            currentDirection = y > 0 ? 2 : 0;
        }
    }

    private void UpdateAnimator()
    {
        bool isMoving = moveDirection.sqrMagnitude > 0.001f;
        animator.SetBool("IsMoving", isMoving);
        animator.SetInteger("Direction", currentDirection);
    }

    public void TakeDamage(int damage)
    {
        if (hp <= 0)
            return;

        hp -= damage;
        if (hp < 0)
            hp = 0;

        LogIfChanged();

        if (hp <= 0)
        {
            moveDirection = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            Destroy(gameObject);
        }
    }

    private void LogIfChanged()
    {
        if (hp != lastHP)
        {
            Debug.Log("Enemy HP: " + hp);
            lastHP = hp;
        }
    }

    private void LogState()
    {
        if (firstStateLog || lastLoggedState != currentState)
        {
            Debug.Log("Enemy State -> " + currentState);
            lastLoggedState = currentState;
            firstStateLog = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, preferredAttackDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? (Vector3)spawnPosition : transform.position;
        Gizmos.DrawWireSphere(center, maxChaseDistanceFromSpawn);
    }
}