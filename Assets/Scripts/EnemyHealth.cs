using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " recibio daño. Vida: " + currentHealth);

        // NUEVO: Avisar a EnemyPatrol que fue golpeado
        GetComponent<EnemyPatrol>()?.AlertarPorGolpe();

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
            Die();
    }

    // NUEVO: Para el derribo sigiloso (kill instantaneo sin alerta)
    public void InstantKill()
    {
        Die();
    }

    System.Collections.IEnumerator DamageFlash()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        Color original = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = original;
    }

    void Die()
    {
        Debug.Log(gameObject.name + " murio");
        Destroy(gameObject);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}