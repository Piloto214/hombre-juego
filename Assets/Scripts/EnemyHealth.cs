using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    private Rigidbody2D rb;

    [Header("Empuje al recibir golpe")]
    [SerializeField] private float duracionEmpuje = 0.15f;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    // Sobrecarga simple: mantiene compatibilidad con cualquier llamada existente
    // que solo pase el daño, sin empuje.
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero, 0f);
    }

    // Version completa: daño + empuje configurable.
    // direccionEmpuje debe venir normalizada (o Vector2.zero para no empujar).
    // fuerzaEmpuje es la velocidad aplicada en esa dirección.
    public void TakeDamage(int damage, Vector2 direccionEmpuje, float fuerzaEmpuje)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " recibio dano. Vida: " + currentHealth);

        // Avisar a EnemyPatrol que fue golpeado
        GetComponent<EnemyPatrol>()?.AlertarPorGolpe();

        StartCoroutine(DamageFlash());

        if (fuerzaEmpuje > 0f && direccionEmpuje != Vector2.zero && rb != null)
        {
            StartCoroutine(AplicarEmpuje(direccionEmpuje, fuerzaEmpuje));
        }

        if (currentHealth <= 0)
            Die();
    }

    // Empuje temporal: bloquea el control de EnemyPatrol durante la duración
    // del empuje para que la patrulla no sobreescriba la velocidad de inmediato
    // (mismo problema/solucion que ya resolviste en PlayerController con
    // bloqueoExterno). Si EnemyPatrol no tiene un metodo de bloqueo, este
    // GetComponent simplemente no hace nada (revisar EnemyPatrol.cs).
    private System.Collections.IEnumerator AplicarEmpuje(Vector2 direccion, float fuerza)
    {
        EnemyPatrol patrol = GetComponent<EnemyPatrol>();
        patrol?.BloquearControl(duracionEmpuje);

        rb.linearVelocity = direccion * fuerza;

        yield return new WaitForSeconds(duracionEmpuje);

        // No forzamos velocidad a cero aqui: dejamos que EnemyPatrol retome
        // el control normalmente una vez termine el bloqueo temporal.
    }

    // ============================================
    // ATURDIMIENTO (golpe cargado del player)
    // Solo aplica al Vieneviene, nunca al boss.
    // ============================================
    public void AplicarAturdimiento(float duracion)
    {
        StartCoroutine(AturdimientoCoroutine(duracion));
    }

    private System.Collections.IEnumerator AturdimientoCoroutine(float duracion)
    {
        EnemyPatrol patrol = GetComponent<EnemyPatrol>();
        patrol?.BloquearControl(duracion);

        if (rb != null) rb.linearVelocity = Vector2.zero;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        Color colorOriginal = sr.color;
        float tiempo = duracion;

        // Parpadeo amarillo — mismo color que usa el player para aturdimiento,
        // para mantener el lenguaje visual consistente.
        while (tiempo > 0f)
        {
            sr.color = new Color(1f, 0.85f, 0f, 1f);
            yield return new WaitForSeconds(0.08f);
            sr.color = colorOriginal;
            yield return new WaitForSeconds(0.08f);
            tiempo -= 0.16f;
        }

        sr.color = colorOriginal;
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