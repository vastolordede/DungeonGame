using UnityEngine;

public class EnemyMeleeAI : MonoBehaviour
{
    private enum State
    {
        Idle,
        Chase,
        Attack,
        Return
    }

    [Header("Enemy HP")]
    public int hp = 30;
    private int lastHP;

    [Header("Target")]
    public Transform player;
    public LayerMask playerLayer;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Detect / Chase")]
    public float detectRange = 5f;
    public float attackStartDistance = 0.75f;
    public float loseTargetRange = 7f;
    public float maxChaseDistanceFromSpawn = 6f;
    public float stopReturnDistance = 0.05f;

    [Header("Attack")]
    public int damagePerHit = 5;
    public float attackHitRange = 0.55f;
    public float attackWindup = 0.75f;
    public float attackCooldown = 0.25f;
    public float attackExitRange = 1.0f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    private Vector2 spawnPosition;
    private Vector2 moveDirection = Vector2.zero;

    private State currentState = State.Idle;

    // 0 = Down, 1 = Side, 2 = Up
    private int currentDirection = 0;

    
    
    private bool hasResolvedAttackThisState = false;
    private State lastLoggedState;
private bool firstStateLog = true;
private float attackWindupTimer = 0f;
private float attackCooldownTimer = 0f;

private float returnLockDuration = 2f;
private float returnLockTimer = 0f;

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
        if (returnLockTimer > 0f)
    returnLockTimer -= Time.deltaTime;

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

            case State.Chase:
                UpdateChase(distanceToPlayer, distanceToSpawn);
                break;

            case State.Attack:
                UpdateAttack(distanceToPlayer, distanceToSpawn);
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

    private void EnterAttackState()
{
    currentState = State.Attack;
    
    hasResolvedAttackThisState = false;

    if (attackWindupTimer <= 0f && attackCooldownTimer <= 0f)
        attackWindupTimer = attackWindup;

    moveDirection = Vector2.zero;
    rb.linearVelocity = Vector2.zero;
}

    private void ExitAttackState(State nextState)
{
   
    hasResolvedAttackThisState = false;
    moveDirection = Vector2.zero;
    currentState = nextState;
}

    private void UpdateIdle(float distanceToPlayer)
    {
        moveDirection = Vector2.zero;

        if (distanceToPlayer <= detectRange)
        {
            currentState = State.Chase;
        }
    }

    private void UpdateChase(float distanceToPlayer, float distanceToSpawn)
{
    if (distanceToPlayer > loseTargetRange || distanceToSpawn > maxChaseDistanceFromSpawn)
{
    moveDirection = Vector2.zero;
    returnLockTimer = returnLockDuration;
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

    if (distanceToPlayer <= attackStartDistance)
    {
        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        EnterAttackState();
        return;
    }

    moveDirection = dir;
}

    private void UpdateAttack(float distanceToPlayer, float distanceToSpawn)
{
    Vector2 toPlayer = (Vector2)player.position - rb.position;

    if (toPlayer.sqrMagnitude > 0.0001f)
        UpdateDirection(toPlayer.normalized);

    moveDirection = Vector2.zero;
    rb.linearVelocity = Vector2.zero;

    // Nếu đòn đang trong quá trình windup thì phải cho nó hoàn thành
    if (!hasResolvedAttackThisState)
    {
        if (attackWindupTimer > 0f)
            return;

        DoAttack();
        hasResolvedAttackThisState = true;
        attackWindupTimer = attackWindup;
        attackCooldownTimer = attackCooldown;
        return;
    }

    // Đã vung đòn rồi thì phải chờ recovery/cooldown xong
    if (attackCooldownTimer > 0f)
        return;

    // Sau khi đòn đã hoàn thành + cooldown xong mới quyết định state tiếp theo
    if (distanceToPlayer > loseTargetRange || distanceToSpawn > maxChaseDistanceFromSpawn)
{
    returnLockTimer = returnLockDuration;
    ExitAttackState(State.Return);
    return;
}

    if (distanceToPlayer > attackExitRange)
    {
        ExitAttackState(State.Chase);
        return;
    }

    // Nếu player vẫn còn trong tầm thì bắt đầu vòng đánh mới
    hasResolvedAttackThisState = false;
    attackWindupTimer = attackWindup;
}

    private void DoAttack()
    {
        animator.SetTrigger("Attack");

        Collider2D playerHit = Physics2D.OverlapCircle(
            transform.position,
            attackHitRange,
            playerLayer
        );

        if (playerHit != null)
        {
            PlayerHealth playerHealth = playerHit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damagePerHit);
                Debug.Log("Enemy hit Player: -" + damagePerHit);
            }
        }
    }

    private void UpdateReturn(float distanceToPlayer)
    {
        

        float distanceToSpawn = Vector2.Distance(transform.position, spawnPosition);

if (returnLockTimer <= 0f &&
    distanceToPlayer <= detectRange &&
    distanceToSpawn <= maxChaseDistanceFromSpawn)
{
    currentState = State.Chase;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackStartDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackHitRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? (Vector3)spawnPosition : transform.position;
        Gizmos.DrawWireSphere(center, maxChaseDistanceFromSpawn);
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
}