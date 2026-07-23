using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float fuerzaEmpuje = 12f;
    public float tiempoInvencible = 1f;
    public int vidas = 3;

    [Header("Respawn")]
    [SerializeField] private Transform puntoRespawn;
    [SerializeField] private float tiempoEsperaRespawn = 1f;

    [Header("Feedback de danio")]
    [SerializeField] private float duracionHitstun = 0.25f;
    [SerializeField] private float componenteVerticalMinimo = 0.5f;

    [Header("Daño por Contacto")]
    [SerializeField] private float fuerzaSaltoContacto = 8f;

    private int vidasIniciales;
    private Vector3 posicionRespawnActual;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private PlayerController controlador;
    private bool puedeRecibirDanio = true;

    public delegate void JugadorRespawn();
    public static event JugadorRespawn OnRespawn;

    public int VidasActuales => vidas;
    public int VidasMaximas => vidasIniciales;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        controlador = GetComponent<PlayerController>();

        vidasIniciales = vidas;
        posicionRespawnActual = (puntoRespawn != null) ? puntoRespawn.position : transform.position;
    }

    // ============================================
    // DAÑO NORMAL — parpadeo blanco/gris
    // Golpe de trapo, bala, etc.
    // ============================================
    public void RecibirGolpe(Vector2 posicionEnemigo, int cantidadVidas = 1, float fuerzaEmpujeOverride = -1f)
    {
        if (!puedeRecibirDanio) return;

        vidas -= cantidadVidas;
        Debug.Log("Vidas restantes: " + vidas);

        Vector2 direccionEmpuje = ((Vector2)transform.position - posicionEnemigo).normalized;
        direccionEmpuje.y = Mathf.Max(direccionEmpuje.y, componenteVerticalMinimo);
        direccionEmpuje.Normalize();

        float fuerzaUsada = (fuerzaEmpujeOverride > 0f) ? fuerzaEmpujeOverride : fuerzaEmpuje;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direccionEmpuje * fuerzaUsada, ForceMode2D.Impulse);

        controlador?.AplicarHitstun(duracionHitstun);

        // Parpadeo blanco / gris — daño normal
        StartCoroutine(ParpadeoNormal());

        if (vidas <= 0) Morir();
    }

    // ============================================
    // DAÑO POR CONTACTO CORPORAL — parpadeo rojo + salto mediano
    // Se llama desde EnemyPatrol.OnCollisionStay2D
    // ============================================
    public void RecibirDanioContacto(Vector2 posicionEnemigo)
    {
        if (!puedeRecibirDanio) return;

        vidas -= 1;
        Debug.Log("Daño por contacto! Vidas restantes: " + vidas);

        // Salto mediano — hacia arriba y levemente alejado del enemigo
        Vector2 dirEmpuje = ((Vector2)transform.position - posicionEnemigo).normalized;
        dirEmpuje.y = 1f;
        dirEmpuje.Normalize();

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dirEmpuje * fuerzaSaltoContacto, ForceMode2D.Impulse);

        controlador?.AplicarHitstun(duracionHitstun);

        // Parpadeo rojo — visualmente distinto al golpe normal
        StartCoroutine(ParpadeoRojo());

        if (vidas <= 0) Morir();
    }

    // ============================================
    // CHECKPOINT
    // ============================================
    public void ActualizarCheckpoint(Vector3 nuevaPosicion)
    {
        posicionRespawnActual = nuevaPosicion;
        Debug.Log("Checkpoint actualizado: " + nuevaPosicion);
    }

    // ============================================
    // MORIR
    // ============================================
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

        StartCoroutine(ParpadeoNormal());
        Debug.Log("Jugador reapareciendo con " + vidas + " vidas.");
    }

    // ============================================
    // PARPADEO NORMAL — blanco / gris semitransparente
    // ============================================
    private System.Collections.IEnumerator ParpadeoNormal()
    {
        puedeRecibirDanio = false;

        for (int i = 0; i < 5; i++)
        {
            sprite.color = new Color(0.7f, 0.7f, 0.7f, 0.4f);
            yield return new WaitForSeconds(0.1f);
            sprite.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(0.1f);
        }

        puedeRecibirDanio = true;
    }

    // ============================================
    // PARPADEO ROJO — daño por contacto corporal
    // Mas rapido y mas flashes para que se distinga
    // ============================================
    private System.Collections.IEnumerator ParpadeoRojo()
    {
        puedeRecibirDanio = false;

        for (int i = 0; i < 7; i++)
        {
            sprite.color = new Color(1f, 0f, 0f, 1f);
            yield return new WaitForSeconds(0.07f);
            sprite.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(0.07f);
        }

        puedeRecibirDanio = true;
    }
}