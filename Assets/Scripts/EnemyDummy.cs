using UnityEngine;

public class EnemyDummy : MonoBehaviour
{
    [Header("Dummy HP")]
    public int hp = 30;

    [Header("Attack Player")]
    public float attackRange = 0.8f;
    public int damagePerHit = 5;
    public float attackInterval = 1f;
    public LayerMask playerLayer;

    private float attackTimer = 0f;

    private int lastHP;

    private void Start()
    {
        lastHP = hp;
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;

        Collider2D playerHit = Physics2D.OverlapCircle(
            transform.position,
            attackRange,
            playerLayer
        );

        if (playerHit != null && attackTimer <= 0f)
        {
            PlayerHealth playerHealth = playerHit.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damagePerHit);
                Debug.Log("📦 Enemy đánh Player: -" + damagePerHit);
            }

            attackTimer = attackInterval;
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp < 0)
            hp = 0;

        LogIfChanged();

        if (hp <= 0)
        {
            Debug.Log("☠️ " + gameObject.name + " đã chết");
            Destroy(gameObject);
        }
    }

    private void LogIfChanged()
    {
        if (hp != lastHP)
        {
            Debug.Log("💥 Enemy HP: " + hp);
            lastHP = hp;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}