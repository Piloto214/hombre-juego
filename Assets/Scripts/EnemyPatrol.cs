using UnityEngine;
using System.Collections;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadPatrulla = 2f;
    [SerializeField] private float velocidadReposicion = 2f;

    [Header("Patrulla")]
    public Transform puntoA;
    public Transform puntoB;
    [SerializeField] private float probabilidadPausa = 0.4f;
    [SerializeField] private float pausaMin = 0.8f;
    [SerializeField] private float pausaMax = 2f;

    [Header("Distancias de Combate")]
    [SerializeField] private float distanciaContacto = 1.2f;
    [SerializeField] private float distanciaTrapo = 2.5f;
    [SerializeField] private float distanciaPreferida = 4f;

    [Header("Ataque Trapo")]
    [SerializeField] private float fuerzaEmpujeTrapo = 7f;
    [SerializeField] private float tiempoTelegrafeoTrapo = 0.3f;

    [Header("Ataque Silbato")]
    [SerializeField] private float tiempoEntreOndas = 0.5f;

    [Header("Dash de Remate")]
    [SerializeField] private float velocidadDash = 10f;

    [Header("Daño por Contacto")]
    [SerializeField] private float cooldownDanioContacto = 1f;

    [Header("Tiempos de IA")]
    [SerializeField] private float tiempoReaccion = 0.7f;
    [SerializeField] private float tiempoEvaluacion = 1.2f;
    [SerializeField] private float pausaPostAtaque = 1f;
    [SerializeField] private float tiempoOlvido = 4f;

    [Header("Silbato")]
    [SerializeField] private GameObject prefabOndaSilbato;
    [SerializeField] private Transform puntoDisparo;

    [Header("Obstaculos")]
    public LayerMask capaObstaculos;
    [SerializeField] private float distanciaDeteccionObstaculo = 1.2f;

    // ============================================
    // PRIVADAS
    // ============================================
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    private enum Estado
    {
        Patrullando,
        Alerta,
        Posicionando,
        AtaqueTrapo,
        AtaqueSilbato,
        Rematando,
        Recuperando,
        Buscando,
        Vigilando
    }
    private Estado estadoActual = Estado.Patrullando;

    private Transform jugadorDetectado;
    private Vector2 ultimaPosicionJugador;
    private bool yaDetectoJugador = false;
    private bool mirandoDerecha = true;
    private bool jugadorEnRango = false;
    private bool enAtaque = false;

    private float timerEstado = 0f;
    private float timerCooldownContacto = 0f;

    private Transform objetivoActual;
    private bool esperandoEnPatrulla = false;
    private float timerPausa = 0f;

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
        if (timerCooldownContacto > 0f)
            timerCooldownContacto -= Time.deltaTime;

        switch (estadoActual)
        {
            case Estado.Patrullando: Patrullar(); break;
            case Estado.Alerta: Alertar(); break;
            case Estado.Posicionando: Posicionar(); break;
            case Estado.Rematando: Rematar(); break;
            case Estado.Recuperando: Recuperar(); break;
            case Estado.Buscando: Buscar(); break;
            case Estado.Vigilando: Vigilar(); break;
        }
    }

    // ============================================
    // PATRULLAR
    // ============================================
    void Patrullar()
    {
        if (esperandoEnPatrulla)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            timerPausa -= Time.deltaTime;

            if (timerPausa <= 0f)
            {
                esperandoEnPatrulla = false;
                if (Random.value > 0.5f) Voltear();
            }
            return;
        }

        if (puntoA == null || puntoB == null) return;

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
            transform.position = new Vector2(transform.position.x + retroceso * 0.3f, transform.position.y);
            Voltear();
            return;
        }

        rb.linearVelocity = new Vector2(direccion.x * velocidadPatrulla, rb.linearVelocity.y);

        if (direccion.x > 0 && !mirandoDerecha) Voltear();
        else if (direccion.x < 0 && mirandoDerecha) Voltear();

        if (Vector2.Distance(transform.position, objetivoActual.position) < 1.5f)
        {
            if (Random.value < probabilidadPausa)
            {
                esperandoEnPatrulla = true;
                timerPausa = Random.Range(pausaMin, pausaMax);
            }
            objetivoActual = (objetivoActual == puntoA) ? puntoB : puntoA;
        }
    }

    // ============================================
    // ALERTA
    // ============================================
    void Alertar()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (jugadorDetectado != null)
        {
            float dirX = jugadorDetectado.position.x - transform.position.x;
            if (dirX > 0 && !mirandoDerecha) Voltear();
            else if (dirX < 0 && mirandoDerecha) Voltear();
        }

        timerEstado -= Time.deltaTime;
        if (timerEstado <= 0f)
        {
            estadoActual = Estado.Posicionando;
            timerEstado = tiempoEvaluacion;
        }
    }

    // ============================================
    // POSICIONANDO
    // ============================================
    void Posicionar()
    {
        if (!jugadorEnRango || jugadorDetectado == null)
        {
            estadoActual = Estado.Buscando;
            return;
        }

        float distancia = Vector2.Distance(transform.position, jugadorDetectado.position);
        Vector2 direccion = (jugadorDetectado.position - transform.position).normalized;

        if (direccion.x > 0 && !mirandoDerecha) Voltear();
        else if (direccion.x < 0 && mirandoDerecha) Voltear();

        if (distancia < distanciaContacto && !enAtaque)
        {
            rb.linearVelocity = new Vector2(-direccion.x * velocidadReposicion, rb.linearVelocity.y);
            timerEstado = tiempoEvaluacion;
        }
        else if (distancia > distanciaPreferida)
        {
            rb.linearVelocity = new Vector2(direccion.x * velocidadReposicion, rb.linearVelocity.y);
            timerEstado = tiempoEvaluacion;
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            timerEstado -= Time.deltaTime;

            if (timerEstado <= 0f)
                DecidirAtaque(distancia);
        }

        ultimaPosicionJugador = jugadorDetectado.position;
    }

    void DecidirAtaque(float distancia)
    {
        if (enAtaque) return;

        if (distancia <= distanciaTrapo)
            StartCoroutine(EjecutarAtaqueTrapo());
        else
            StartCoroutine(EjecutarAtaqueSilbato());
    }

    // ============================================
    // REMATANDO — dash rapido tras onda conectada
    // La distancia se calcula automaticamente
    // ============================================
    void Rematar()
    {
        if (jugadorDetectado == null)
        {
            estadoActual = Estado.Posicionando;
            timerEstado = tiempoEvaluacion;
            return;
        }

        float distancia = Vector2.Distance(transform.position, jugadorDetectado.position);
        Vector2 direccion = (jugadorDetectado.position - transform.position).normalized;

        if (direccion.x > 0 && !mirandoDerecha) Voltear();
        else if (direccion.x < 0 && mirandoDerecha) Voltear();

        // Cuando llega a distancia de trapo, ejecuta el golpe
        if (distancia <= distanciaTrapo)
        {
            rb.linearVelocity = Vector2.zero;
            if (!enAtaque)
                StartCoroutine(EjecutarAtaqueTrapo());
            return;
        }

        // Dash calculado automaticamente hacia el player
        rb.linearVelocity = new Vector2(direccion.x * velocidadDash, rb.linearVelocity.y);
    }

    // ============================================
    // RECUPERANDO
    // ============================================
    void Recuperar()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        timerEstado -= Time.deltaTime;

        if (timerEstado <= 0f)
        {
            if (jugadorEnRango && jugadorDetectado != null)
            {
                estadoActual = Estado.Posicionando;
                timerEstado = tiempoEvaluacion;
            }
            else
            {
                estadoActual = Estado.Buscando;
            }
        }
    }

    // ============================================
    // BUSCAR
    // ============================================
    void Buscar()
    {
        Vector2 direccion = (ultimaPosicionJugador - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direccion.x * velocidadPatrulla, rb.linearVelocity.y);

        if (direccion.x > 0 && !mirandoDerecha) Voltear();
        else if (direccion.x < 0 && mirandoDerecha) Voltear();

        if (Vector2.Distance(transform.position, ultimaPosicionJugador) < 1f)
        {
            rb.linearVelocity = Vector2.zero;
            estadoActual = Estado.Vigilando;
            timerEstado = tiempoOlvido;
        }
    }

    // ============================================
    // VIGILANDO
    // ============================================
    void Vigilar()
    {
        rb.linearVelocity = Vector2.zero;
        timerEstado -= Time.deltaTime;

        if (Random.value < 0.004f) Voltear();

        if (timerEstado <= 0f)
        {
            estadoActual = Estado.Patrullando;
            yaDetectoJugador = false;
            jugadorDetectado = null;
        }
    }

    // ============================================
    // ATAQUE TRAPO — con empuje
    // ============================================
    IEnumerator EjecutarAtaqueTrapo()
    {
        enAtaque = true;
        estadoActual = Estado.AtaqueTrapo;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(tiempoTelegrafeoTrapo);

        if (jugadorDetectado != null)
        {
            float distancia = Vector2.Distance(transform.position, jugadorDetectado.position);
            if (distancia <= distanciaTrapo)
            {
                jugadorDetectado.GetComponent<PlayerHealth>()?.RecibirGolpe(transform.position);

                Rigidbody2D rbJugador = jugadorDetectado.GetComponent<Rigidbody2D>();
                if (rbJugador != null)
                {
                    Vector2 dirEmpuje = ((Vector2)jugadorDetectado.position - (Vector2)transform.position).normalized;
                    rbJugador.linearVelocity = new Vector2(dirEmpuje.x * fuerzaEmpujeTrapo, fuerzaEmpujeTrapo * 0.6f);
                }
            }
        }

        yield return new WaitForSeconds(0.4f);
        enAtaque = false;
        estadoActual = Estado.Recuperando;
        timerEstado = pausaPostAtaque;
    }

    // ============================================
    // ATAQUE SILBATO — pasa referencia al proyectil
    // ============================================
    IEnumerator EjecutarAtaqueSilbato()
    {
        enAtaque = true;
        estadoActual = Estado.AtaqueSilbato;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(tiempoEntreOndas);

        if (prefabOndaSilbato != null)
        {
            Vector3 spawnPos = puntoDisparo != null ? puntoDisparo.position : transform.position;
            float dirX = mirandoDerecha ? 1f : -1f;
            GameObject onda = Instantiate(prefabOndaSilbato, spawnPos, Quaternion.identity);

            // Pasa referencia a este enemigo para que notifique cuando conecte
            onda.GetComponent<OndaSilbato>()?.Inicializar(dirX, this);
        }

        yield return new WaitForSeconds(0.5f);
        enAtaque = false;
        estadoActual = Estado.Recuperando;
        timerEstado = pausaPostAtaque;
    }

    // ============================================
    // NOTIFICACION DE IMPACTO — llamado desde OndaSilbato
    // Activa el dash de remate automaticamente
    // ============================================
    public void NotificarImpactoOnda(Transform posicionPlayer)
    {
        jugadorDetectado = posicionPlayer;
        jugadorEnRango = true;
        estadoActual = Estado.Rematando;
        Debug.Log("Onda impacto! Entrando a Rematando - dash automatico");
    }

    // ============================================
    // DAÑO POR CONTACTO CORPORAL
    // ============================================
    void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (!yaDetectoJugador) return;
        if (timerCooldownContacto > 0f) return;

        collision.gameObject.GetComponent<PlayerHealth>()?.RecibirDanioContacto(transform.position);
        timerCooldownContacto = cooldownDanioContacto;
    }

    // ============================================
    // ALERTAR POR GOLPE
    // ============================================
    public void AlertarPorGolpe()
    {
        if (yaDetectoJugador) return;

        yaDetectoJugador = true;
        estadoActual = Estado.Alerta;
        timerEstado = tiempoReaccion;
    }

    // ============================================
    // ASIGNAR WAYPOINTS
    // ============================================
    public void AsignarWaypoints(Transform a, Transform b)
    {
        puntoA = a;
        puntoB = b;
        objetivoActual = puntoA;
    }

    // ============================================
    // VISION
    // ============================================
    bool JugadorAlFrente(Transform jugador)
    {
        float dirEnemigo = mirandoDerecha ? 1f : -1f;
        float dirAlJugador = jugador.position.x - transform.position.x;
        // [SIGILO] Aqui se reducira el angulo de vision
        return Mathf.Sign(dirEnemigo) == Mathf.Sign(dirAlJugador);
    }

    bool TieneLineaDeVision(Transform jugador)
    {
        Vector2 origen = (Vector2)transform.position + new Vector2(0, -0.3f);
        Vector2 direccion = ((Vector2)jugador.position - origen).normalized;
        float distancia = Vector2.Distance(origen, jugador.position);

        RaycastHit2D hit = Physics2D.Raycast(origen, direccion, distancia, capaObstaculos);
        Debug.DrawRay(origen, direccion * distancia, hit.collider == null ? Color.green : Color.magenta);
        // [SIGILO] Aqui se reducira la distancia de deteccion
        return hit.collider == null;
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
    // TRIGGERS
    // ============================================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDetectado = other.transform;
        jugadorEnRango = true;

        if (yaDetectoJugador)
        {
            if (estadoActual == Estado.Patrullando ||
                estadoActual == Estado.Vigilando ||
                estadoActual == Estado.Buscando)
            {
                estadoActual = Estado.Alerta;
                timerEstado = tiempoReaccion;
            }
            return;
        }

        if (JugadorAlFrente(other.transform) && TieneLineaDeVision(other.transform))
        {
            yaDetectoJugador = true;
            estadoActual = Estado.Alerta;
            timerEstado = tiempoReaccion;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDetectado = other.transform;
        jugadorEnRango = true;

        if (estadoActual == Estado.Posicionando ||
            estadoActual == Estado.Rematando ||
            enAtaque ||
            estadoActual == Estado.Recuperando ||
            estadoActual == Estado.Alerta) return;

        if (yaDetectoJugador)
        {
            estadoActual = Estado.Alerta;
            timerEstado = tiempoReaccion;
            return;
        }

        if (JugadorAlFrente(other.transform) && TieneLineaDeVision(other.transform))
        {
            yaDetectoJugador = true;
            estadoActual = Estado.Alerta;
            timerEstado = tiempoReaccion;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorEnRango = false;

        if (jugadorDetectado != null)
            ultimaPosicionJugador = jugadorDetectado.position;

        if (!enAtaque && estadoActual != Estado.Recuperando)
            estadoActual = Estado.Buscando;
    }
}