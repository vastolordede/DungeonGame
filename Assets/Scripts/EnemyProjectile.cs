using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed = 6f;
    public float lifeTime = 3f;

    private Vector2 moveDirection = Vector2.zero;
    private int damage = 0;

    public void Init(Vector2 dir, int dmg)
    {
        moveDirection = dir.normalized;
        damage = dmg;

        Destroy(gameObject, lifeTime);

        // lật sprite theo hướng bay ngang
        if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y))
        {
            Vector3 scale = transform.localScale;
            scale.x = moveDirection.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log("Enemy fireball hit Player: -" + damage);
            }

            Destroy(gameObject);
            return;
        }

        // nếu muốn chạm tường thì tự hủy
        if (!other.isTrigger && !other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}