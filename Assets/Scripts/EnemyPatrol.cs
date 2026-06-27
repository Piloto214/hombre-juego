using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    // ============================================
    // VARIABLES PUBLICAS
    // ============================================

    [Header("Movimiento")]
    public float velocidadPatrulla = 2f;
    public float velocidadPersecucion = 4f;

    [Header("Patrulla")]
    public Transform puntoA;
    public Transform puntoB;

    [Header("Deteccion")]
    public float distanciaAtaque = 1.5f;
    public float tiempoOlvido = 3f;

    [Header("Obstaculos")]
    public LayerMask capaObstaculos;
    public float distanciaDeteccionObstaculo = 1.2f;

    // Variables privadas
    private Transform objetivoActual;
    private Transform jugadorDetectado;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    private enum Estado { Patrullando, Persiguiendo, Buscando, Alerta }
    private Estado estadoActual = Estado.Patrullando;

    private float tiempoSinVerJugador = 0f;
    private Vector2 ultimaPosicionJugador;
    private bool mirandoDerecha = true;

    private bool atacando = false;
    private bool enRetroceso = false;

    // ============================================
    // START
    // ============================================
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        objetivoActual = puntoA;
    }

    // ============================================
    // UPDATE
    // ============================================
    void Update()
    {
        switch (estadoActual)
        {
            case Estado.Patrullando: Patrullar(); break;
            case Estado.Persiguiendo: Perseguir(); break;
            case Estado.Buscando: Buscar(); break;
            case Estado.Alerta: Alerta(); break;
        }
    }

    // ============================================
    // PATRULLAR
    // ============================================
    void Patrullar()
    {
        Vector2 direccion = (objetivoActual.position - transform.position).normalized;
        Vector2 origen = (Vector2)transform.position + new Vector2(0, -0.3f);
        Vector2 dirRaycast = new Vector2(direccion.x, 0);

        RaycastHit2D hit = Physics2D.Raycast(origen, dirRaycast, distanciaDeteccionObstaculo, capaObstaculos);
        Debug.DrawRay(origen, dirRaycast * distanciaDeteccionObstaculo, Color.red);

        if (hit.collider != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            objetivoActual = (objetivoActual == puntoA) ? puntoB : puntoA;

            float retroceso = mirandoDerecha ? -1f : 1f;
            transform.position = new Vector2(transform.position.x + retroceso * 0.5f, transform.position.y);

            Voltear();
            return;
        }

        rb.linearVelocity = new Vector2(direccion.x * velocidadPatrulla, rb.linearVelocity.y);

        if (direccion.x > 0 && !mirandoDerecha) Voltear();
        else if (direccion.x < 0 && mirandoDerecha) Voltear();

        if (Vector2.Distance(transform.position, objetivoActual.position) < 2.0f)
            objetivoActual = (objetivoActual == puntoA) ? puntoB : puntoA;
    }

    // ============================================
    // PERSEGUIR
    // ============================================
    void Perseguir()
    {
        if (jugadorDetectado == null) { estadoActual = Estado.Patrullando; return; }

        if (atacando) return;

        Vector2 direccion = (jugadorDetectado.position - transform.position).normalized;
        Vector2 origen = (Vector2)transform.position + new Vector2(0, -0.3f);
        Vector2 dirRaycast = new Vector2(direccion.x, 0);

        RaycastHit2D hit = Physics2D.Raycast(origen, dirRaycast, distanciaDeteccionObstaculo, capaObstaculos);
        Debug.DrawRay(origen, dirRaycast * distanciaDeteccionObstaculo, Color.yellow);

        if (hit.collider != null && !enRetroceso)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            StartCoroutine(RetrocederYAlertar());
            return;
        }

        // SIEMPRE voltear hacia el jugador durante persecución
        if (direccion.x > 0 && !mirandoDerecha) Voltear();
        else if (direccion.x < 0 && mirandoDerecha) Voltear();

        rb.linearVelocity = new Vector2(direccion.x * velocidadPersecucion, rb.linearVelocity.y);

        float distanciaAlJugador = Vector2.Distance(transform.position, jugadorDetectado.position);

        if (distanciaAlJugador < distanciaAtaque && !atacando)
        {
            jugadorDetectado.GetComponent<PlayerHealth>()?.RecibirGolpe(transform.position);
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(PausaDespuesDeAtacar());
        }

        ultimaPosicionJugador = jugadorDetectado.position;
    }

    // ============================================
    // BUSCAR
    // ============================================
    void Buscar()
    {
        Vector2 direccion = (ultimaPosicionJugador - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direccion.x * velocidadPatrulla, rb.linearVelocity.y);

        if (Vector2.Distance(transform.position, ultimaPosicionJugador) < 0.5f)
        {
            rb.linearVelocity = Vector2.zero;
            tiempoSinVerJugador += Time.deltaTime;

            if (tiempoSinVerJugador > 1f)
            {
                estadoActual = Estado.Alerta;
                tiempoSinVerJugador = 0f;
            }
        }
    }

    // ============================================
    // ALERTA
    // ============================================
    void Alerta()
    {
        Vector2 dirUltima = (ultimaPosicionJugador - (Vector2)transform.position).normalized;

        if (dirUltima.x > 0.1f && !mirandoDerecha) Voltear();
        else if (dirUltima.x < -0.1f && mirandoDerecha) Voltear();

        rb.linearVelocity = Vector2.zero;
        tiempoSinVerJugador += Time.deltaTime;

        if (tiempoSinVerJugador > tiempoOlvido)
        {
            estadoActual = Estado.Patrullando;
            jugadorDetectado = null;
            tiempoSinVerJugador = 0f;
        }
    }

    // ============================================
    // CAMPO DE VISION
    // ============================================
    bool JugadorAlFrente(Transform jugador)
    {
        float dirEnemigo = mirandoDerecha ? 1f : -1f;
        float dirAlJugador = jugador.position.x - transform.position.x;

        // [SIGILO] Aqui se reducira el angulo de vision si el jugador esta en sigilo
        return Mathf.Sign(dirEnemigo) == Mathf.Sign(dirAlJugador);
    }

    // ============================================
    // LINEA DE VISION
    // ============================================
    bool TieneLineaDeVision(Transform jugador)
    {
        Vector2 origen = (Vector2)transform.position + new Vector2(0, -0.3f);
        Vector2 direccion = ((Vector2)jugador.position - origen).normalized;
        float distancia = Vector2.Distance(origen, jugador.position);

        RaycastHit2D hit = Physics2D.Raycast(origen, direccion, distancia, capaObstaculos);
        Debug.DrawRay(origen, direccion * distancia, hit.collider == null ? Color.green : Color.magenta);

        // [SIGILO] Aqui se reducira la distancia de deteccion si el jugador esta en sigilo
        return hit.collider == null;
    }

    // ============================================
    // REACCION AL RECIBIR GOLPE
    // ============================================
    public void AlertarPorGolpe()
    {
        if (estadoActual == Estado.Patrullando || estadoActual == Estado.Buscando)
        {
            estadoActual = Estado.Alerta;
            tiempoSinVerJugador = 0f;
            Debug.Log(gameObject.name + " alertado por golpe!");
        }
    }

    // ============================================
    // VOLTEAR
    // ============================================
    void Voltear()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = sprite.transform.localScale;
        escala.x *= -1;
        sprite.transform.localScale = escala;
    }

    // ============================================
    // CORUTINA: Pausa entre ataques
    // ============================================
    private System.Collections.IEnumerator PausaDespuesDeAtacar()
    {
        atacando = true;
        yield return new WaitForSeconds(0.8f);
        atacando = false;
    }

    // ============================================
    // CORUTINA: Retroceder y alertar
    // ============================================
    private System.Collections.IEnumerator RetrocederYAlertar()
    {
        enRetroceso = true;

        float dirRetroceso = mirandoDerecha ? -1f : 1f;
        float tiempo = 0.4f;

        while (tiempo > 0f)
        {
            rb.linearVelocity = new Vector2(dirRetroceso * velocidadPatrulla * 0.8f, rb.linearVelocity.y);
            tiempo -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        if (jugadorDetectado != null)
            ultimaPosicionJugador = jugadorDetectado.position;

        estadoActual = Estado.Alerta;
        tiempoSinVerJugador = 0f;
        enRetroceso = false;
    }

    // ============================================
    // TRIGGERS
    // ============================================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDetectado = other.transform;

        // Deteccion inicial: solo si el jugador esta al frente y hay linea de vision
        if (JugadorAlFrente(other.transform) && TieneLineaDeVision(other.transform))
        {
            estadoActual = Estado.Persiguiendo;
            tiempoSinVerJugador = 0f;
            Debug.Log("Jugador detectado al frente!");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDetectado = other.transform;

        // FIX: Si YA está persiguiendo, no importa la dirección — sigue persiguiendo
        if (estadoActual == Estado.Persiguiendo) return;

        // Solo para detección inicial desde Alerta o Patrullando
        if (JugadorAlFrente(other.transform) && TieneLineaDeVision(other.transform))
        {
            estadoActual = Estado.Persiguiendo;
            tiempoSinVerJugador = 0f;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (jugadorDetectado != null)
            ultimaPosicionJugador = jugadorDetectado.position;

        estadoActual = Estado.Buscando;
        tiempoSinVerJugador = 0f;
        Debug.Log("Jugador perdido... buscando");
    }
}