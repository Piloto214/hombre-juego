using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float fuerzaEmpuje = 8f;
    public float tiempoInvencible = 1f;
    public int vidas = 3;

    [Header("Respawn")]
    [SerializeField] private Transform puntoRespawn;
    [SerializeField] private float tiempoEsperaRespawn = 1f;

    private int vidasIniciales;
    private Vector3 posicionRespawnActual;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private bool puedeRecibirDaño = true;

    // Aviso publico: cualquier script (como el boss) puede suscribirse
    // para enterarse cuando el jugador reaparece.
    public delegate void JugadorRespawn();
    public static event JugadorRespawn OnRespawn;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        vidasIniciales = vidas;
        posicionRespawnActual = (puntoRespawn != null) ? puntoRespawn.position : transform.position;
    }

    public void RecibirGolpe(Vector2 posicionEnemigo, int cantidadVidas = 1)
    {
        if (!puedeRecibirDaño) return;

        vidas -= cantidadVidas;
        Debug.Log("Vidas restantes: " + vidas);

        Vector2 direccionEmpuje = ((Vector2)transform.position - posicionEnemigo).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);

        StartCoroutine(InvencibilidadTemporal());

        if (vidas <= 0)
        {
            Morir();
        }
    }

    public void ActualizarCheckpoint(Vector3 nuevaPosicion)
    {
        posicionRespawnActual = nuevaPosicion;
        Debug.Log("Checkpoint actualizado. Nuevo punto de respawn: " + nuevaPosicion);
    }

    private void Morir()
    {
        Debug.Log("GAME OVER - Reapareciendo en " + tiempoEsperaRespawn + " segundos...");
        StartCoroutine(RespawnCoroutine());
    }

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(tiempoEsperaRespawn);

        vidas = vidasIniciales;
        transform.position = posicionRespawnActual;
        rb.linearVelocity = Vector2.zero;

        // Avisamos a quien este escuchando (el boss, por ejemplo) que el jugador reapareció.
        OnRespawn?.Invoke();

        StartCoroutine(InvencibilidadTemporal());

        Debug.Log("Jugador reapareció con " + vidas + " vidas.");
    }

    private System.Collections.IEnumerator InvencibilidadTemporal()
    {
        puedeRecibirDaño = false;

        for (int i = 0; i < 5; i++)
        {
            sprite.color = new Color(1, 1, 1, 0.3f);
            yield return new WaitForSeconds(0.1f);
            sprite.color = new Color(1, 1, 1, 1f);
            yield return new WaitForSeconds(0.1f);
        }

        puedeRecibirDaño = true;
    }
}