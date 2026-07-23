using UnityEngine;

public class OndaSilbato : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadOnda = 4f;
    [SerializeField] private float distanciaRecorrido = 10f;

    [Header("Daño")]
    [SerializeField] private int danio = 1;

    [Header("Aturdimiento al golpear")]
    [SerializeField] private float duracionAturdimiento = 1.5f;
    [SerializeField] private float multiplicadorAturdimiento = 0.15f;

    [Header("Obstaculos")]
    [SerializeField] private LayerMask capaObstaculos;

    private float direccionX;
    private float distanciaRecorrida = 0f;
    private EnemyPatrol enemigoPadre;

    void Awake()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(1f, 0.85f, 0f, 0.75f);
    }

    // Llamado desde EnemyPatrol al instanciar la onda
    public void Inicializar(float dir, EnemyPatrol enemigo)
    {
        direccionX = dir;
        enemigoPadre = enemigo;

        if (dir < 0)
        {
            Vector3 escala = transform.localScale;
            escala.x *= -1;
            transform.localScale = escala;
        }
    }

    // Sobrecarga para compatibilidad (sin referencia al enemigo)
    public void Inicializar(float dir)
    {
        Inicializar(dir, null);
    }

    void Update()
    {
        float movimiento = velocidadOnda * Time.deltaTime;
        transform.position += new Vector3(direccionX * movimiento, 0f, 0f);
        distanciaRecorrida += movimiento;

        if (distanciaRecorrida >= distanciaRecorrido)
        {
            Destroy(gameObject);
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(direccionX, 0), 0.6f, capaObstaculos);
        if (hit.collider != null)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 1. Daño normal
        other.GetComponent<PlayerHealth>()?.RecibirGolpe(transform.position, danio);

        // 2. Aturdimiento — parpadeo amarillo y movimiento reducido
        other.GetComponent<PlayerController>()?.AplicarAturdimiento(duracionAturdimiento, multiplicadorAturdimiento);

        // 3. Notificar al enemigo que la onda conectó — activa el dash de remate
        enemigoPadre?.NotificarImpactoOnda(other.transform);

        Destroy(gameObject);
    }
}