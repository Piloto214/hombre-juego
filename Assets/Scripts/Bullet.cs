using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 0.8f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Shoot(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        if (other.CompareTag("DetectorEnemigo")) return;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        MiniBossController miniBoss = other.GetComponentInParent<MiniBossController>();
        if (miniBoss != null)
        {
            miniBoss.RecibirDanio(damage);
            Destroy(gameObject);
            return;
        }

        PropVida prop = other.GetComponent<PropVida>();
        if (prop != null)
        {
            prop.RecibirGolpe(damage);
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}