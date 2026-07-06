using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float fuerzaEmpuje = 12f;
    public float tiempoInvencible = 1f;
    public int vidas = 3;

    [Header("Respawn")]
    [SerializeField] private Transform puntoRespawn;
    [SerializeField] private float tiempoEsperaRespawn = 1f;

    [Header("Feedback de daño")]
    [SerializeField] private float duracionHitstun = 0.25f;
    [SerializeField] private float componenteVerticalMinimo = 0.5f;

    private int vidasIniciales;
    private Vector3 posicionRespawnActual;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private PlayerController controlador;
    private bool puedeRecibirDanio = true;

    // Propiedades para la UI
    public int VidasActuales => vidas;
    public int VidasMaximas => vidasIniciales;

    public delegate void JugadorRespawn();
    public static event JugadorRespawn OnRespawn;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        controlador = GetComponent<PlayerController>();

        vidasIniciales = vidas;
        posicionRespawnActual = (puntoRespawn != null) ? puntoRespawn.position : transform.position;
    }

    public void RecibirGolpe(Vector2 posicionEnemigo, int cantidadVidas = 1)
    {
        if (!puedeRecibirDanio) return;

        vidas -= cantidadVidas;
        Debug.Log("Vidas restantes: " + vidas);

        Vector2 direccionEmpuje = ((Vector2)transform.position - posicionEnemigo).normalized;
        direccionEmpuje.y = Mathf.Max(direccionEmpuje.y, componenteVerticalMinimo);
        direccionEmpuje.Normalize();

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);

        if (controlador != null)
        {
            controlador.AplicarHitstun(duracionHitstun);
        }

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

        OnRespawn?.Invoke();

        StartCoroutine(InvencibilidadTemporal());

        Debug.Log("Jugador reapareció con " + vidas + " vidas.");
    }

    private System.Collections.IEnumerator InvencibilidadTemporal()
    {
        puedeRecibirDanio = false;

        for (int i = 0; i < 5; i++)
        {
            sprite.color = new Color(1, 1, 1, 0.3f);
            yield return new WaitForSeconds(0.1f);
            sprite.color = new Color(1, 1, 1, 1f);
            yield return new WaitForSeconds(0.1f);
        }

        puedeRecibirDanio = true;
    }
}