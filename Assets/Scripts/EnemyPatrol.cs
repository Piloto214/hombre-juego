using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
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

    // FLAG CLAVE: Recuerda si ya detectó al jugador alguna vez
    private bool yaDetectoJugador = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        objetivoActual = puntoA;
    }

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
            // RESET: Ahora sí puede ser sorprendido de nuevo
            yaDetectoJugador = false;
            tiempoSinVerJugador = 0f;
            Debug.Log("Enemigo olvido al jugador, volviendo a patrullar");
        }
    }

    bool JugadorAlFrente(Transform jugador)
    {
        float dirEnemigo = mirandoDerecha ? 1f : -1f;
        float dirAlJugador = jugador.position.x - transform.position.x;
        // [SIGILO] Aqui se reducira el angulo de vision si el jugador esta en sigilo
        return Mathf.Sign(dirEnemigo) == Mathf.Sign(dirAlJugador);
    }

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

    public void AlertarPorGolpe()
    {
        if (estadoActual == Estado.Patrullando || estadoActual == Estado.Buscando)
        {
            yaDetectoJugador = true;
            estadoActual = Estado.Alerta;
            tiempoSinVerJugador = 0f;
            Debug.Log(gameObject.name + " alertado por golpe!");
        }
    }

    void Voltear()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = sprite.transform.localScale;
        escala.x *= -1;
        sprite.transform.localScale = escala;
    }

    private System.Collections.IEnumerator PausaDespuesDeAtacar()
    {
        atacando = true;
        yield return new WaitForSeconds(0.8f);
        atacando = false;
    }

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDetectado = other.transform;

        // Si ya lo había detectado antes, perseguir sin importar dirección
        if (yaDetectoJugador)
        {
            estadoActual = Estado.Persiguiendo;
            tiempoSinVerJugador = 0f;
            Debug.Log("Jugador redetectado - persiguiendo sin importar dirección");
            return;
        }

        // Primera detección: solo si está al frente y hay línea de visión
        if (JugadorAlFrente(other.transform) && TieneLineaDeVision(other.transform))
        {
            yaDetectoJugador = true;
            estadoActual = Estado.Persiguiendo;
            tiempoSinVerJugador = 0f;
            Debug.Log("Jugador detectado al frente por primera vez!");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDetectado = other.transform;

        // Si ya está persiguiendo, no interrumpir
        if (estadoActual == Estado.Persiguiendo) return;

        // Si ya lo detectó antes, retomar persecución
        if (yaDetectoJugador)
        {
            estadoActual = Estado.Persiguiendo;
            tiempoSinVerJugador = 0f;
            return;
        }

        // Primera detección desde Alerta o Patrullando
        if (JugadorAlFrente(other.transform) && TieneLineaDeVision(other.transform))
        {
            yaDetectoJugador = true;
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
        Debug.Log("Jugador salió del rango - buscando");
    }
}