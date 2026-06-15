using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    // ============================================
    // VARIABLES PUBLICAS (configurables en Unity)
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

    private enum Estado { Patrullando, Persiguiendo, Atacando, Buscando, Alerta }
    private Estado estadoActual = Estado.Patrullando;

    private float tiempoSinVerJugador = 0f;
    private Vector2 ultimaPosicionJugador;
    private bool mirandoDerecha = true;

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
            case Estado.Patrullando:
                Patrullar();
                break;
            case Estado.Persiguiendo:
                Perseguir();
                break;
            case Estado.Buscando:
                Buscar();
                break;
            case Estado.Alerta:
                Alerta();
                break;
        }
    }

    // ============================================
    // PATRULLAR: Caminar entre puntoA y puntoB
    // ============================================
    void Patrullar()
    {
        Vector2 direccion = (objetivoActual.position - transform.position).normalized;

        // Detectar obstaculo adelante
        Vector2 origenRaycast = (Vector2)transform.position + new Vector2(0, -0.3f);
        Vector2 direccionRaycast = new Vector2(direccion.x, 0);

        RaycastHit2D hit = Physics2D.Raycast(origenRaycast, direccionRaycast, distanciaDeteccionObstaculo, capaObstaculos);

        Debug.DrawRay(origenRaycast, direccionRaycast * distanciaDeteccionObstaculo, Color.red);

        if (hit.collider != null)
        {
            // Obstaculo detectado - Detenerse, retroceder, voltear
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (objetivoActual == puntoA)
                objetivoActual = puntoB;
            else
                objetivoActual = puntoA;

            // Retroceder para despegar del obstaculo
            float direccionRetroceso = mirandoDerecha ? -1f : 1f;
            transform.position = new Vector2(
                transform.position.x + (direccionRetroceso * 0.5f),
                transform.position.y
            );

            Voltear();
            return;
        }

        // Moverse normal
        rb.linearVelocity = new Vector2(direccion.x * velocidadPatrulla, rb.linearVelocity.y);

        // Voltear sprite
        if (direccion.x > 0 && !mirandoDerecha)
            Voltear();
        else if (direccion.x < 0 && mirandoDerecha)
            Voltear();

        // Llegamos al punto?
        float distanciaAlPunto = Vector2.Distance(transform.position, objetivoActual.position);
        if (distanciaAlPunto < 2.0f)
        {
            if (objetivoActual == puntoA)
                objetivoActual = puntoB;
            else
                objetivoActual = puntoA;
        }
    }

    // ============================================
    // PERSEGUIR: Correr hacia el jugador
    // ============================================
    void Perseguir()
    {
        if (jugadorDetectado == null)
        {
            estadoActual = Estado.Patrullando;
            return;
        }

        Vector2 direccion = (jugadorDetectado.position - transform.position).normalized;

        // Detectar obstaculo al perseguir
        Vector2 origenRaycast = (Vector2)transform.position + new Vector2(0, -0.3f);
        Vector2 direccionRaycast = new Vector2(direccion.x, 0);

        RaycastHit2D hit = Physics2D.Raycast(origenRaycast, direccionRaycast, distanciaDeteccionObstaculo, capaObstaculos);

        Debug.DrawRay(origenRaycast, direccionRaycast * distanciaDeteccionObstaculo, Color.yellow);

        if (hit.collider != null)
        {
            // Obstaculo bloquea la vista - Retroceder y ponerse en alerta
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // Iniciar retroceso y alerta
            StartCoroutine(RetrocederYAlertar());
            return;
        }

        // Perseguir normal
        rb.linearVelocity = new Vector2(direccion.x * velocidadPersecucion, rb.linearVelocity.y);

        if (direccion.x > 0 && !mirandoDerecha)
            Voltear();
        else if (direccion.x < 0 && mirandoDerecha)
            Voltear();

        // Atacar si estamos cerca
        float distanciaAlJugador = Vector2.Distance(transform.position, jugadorDetectado.position);
        if (distanciaAlJugador < distanciaAtaque)
        {
            PlayerHealth saludJugador = jugadorDetectado.GetComponent<PlayerHealth>();
            if (saludJugador != null)
            {
                saludJugador.RecibirGolpe(transform.position);
            }

            rb.linearVelocity = Vector2.zero;
            StartCoroutine(PausaDespuesDeAtacar());
        }

        ultimaPosicionJugador = jugadorDetectado.position;
    }

    // ============================================
    // RETROCEDER Y ALERTAR: Corutina para retroceder y quedarse en guardia
    // ============================================
    private System.Collections.IEnumerator RetrocederYAlertar()
    {
        // Calcular direccion de retroceso (opuesto a donde mira)
        float direccionRetroceso = mirandoDerecha ? -1f : 1f;

        // Retroceder durante 0.4 segundos
        float tiempoRetrocediendo = 0.4f;
        while (tiempoRetrocediendo > 0f)
        {
            rb.linearVelocity = new Vector2(direccionRetroceso * velocidadPatrulla * 0.8f, rb.linearVelocity.y);
            tiempoRetrocediendo -= Time.deltaTime;
            yield return null;
        }

        // Detenerse
        rb.linearVelocity = Vector2.zero;

        // Guardar ultima posicion del jugador y cambiar a Alerta
        if (jugadorDetectado != null)
        {
            ultimaPosicionJugador = jugadorDetectado.position;
        }

        estadoActual = Estado.Alerta;
        tiempoSinVerJugador = 0f;
        Debug.Log("Enemigo retrocedio y esta en ALERTA");
    }

    // ============================================
    // ALERTA: El jugador esta cerca pero oculto
    // ============================================
    void Alerta()
    {
        // Mirar hacia la ultima posicion conocida del jugador
        Vector2 direccionUltimaVista = (ultimaPosicionJugador - (Vector2)transform.position).normalized;

        // Voltear hacia donde vio al jugador por ultima vez
        if (direccionUltimaVista.x > 0.1f && !mirandoDerecha)
            Voltear();
        else if (direccionUltimaVista.x < -0.1f && mirandoDerecha)
            Voltear();

        // Quedarse completamente quieto en guardia
        rb.linearVelocity = Vector2.zero;

        // Contar tiempo en alerta
        tiempoSinVerJugador += Time.deltaTime;

        // Si pasa mucho tiempo sin ver al jugador, volver a patrullar
        if (tiempoSinVerJugador > tiempoOlvido)
        {
            Debug.Log("Tiempo de alerta agotado, volviendo a patrullar");
            estadoActual = Estado.Patrullando;
            jugadorDetectado = null;
            tiempoSinVerJugador = 0f;
        }
    }

    // ============================================
    // BUSCAR: Ir a ultima posicion conocida
    // ============================================
    void Buscar()
    {
        Vector2 direccion = (ultimaPosicionJugador - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direccion.x * velocidadPatrulla, rb.linearVelocity.y);

        float distancia = Vector2.Distance(transform.position, ultimaPosicionJugador);
        if (distancia < 0.5f)
        {
            tiempoSinVerJugador += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;

            if (tiempoSinVerJugador > 1f)
            {
                estadoActual = Estado.Patrullando;
                tiempoSinVerJugador = 0f;
            }
        }
    }

    // ============================================
    // VOLTear: Girar el sprite izquierda/derecha
    // ============================================
    void Voltear()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = sprite.transform.localScale;
        escala.x *= -1;
        sprite.transform.localScale = escala;
    }

    // ============================================
    // CORUTINA: PAUSA DESPUES DE ATACAR
    // ============================================
    private System.Collections.IEnumerator PausaDespuesDeAtacar()
    {
        estadoActual = Estado.Buscando;
        yield return new WaitForSeconds(0.8f);
        estadoActual = Estado.Persiguiendo;
    }

    // ============================================
    // TRIGGER: Detectar jugador al ENTRAR en zona
    // ============================================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDetectado = other.transform;
            estadoActual = Estado.Persiguiendo;
            tiempoSinVerJugador = 0f;
            Debug.Log("Jugador detectado!");
        }
    }

    // ============================================
    // TRIGGER STAY: El jugador sigue dentro del trigger
    // ============================================
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDetectado = other.transform;

            // Si estamos en Alerta, verificar si ahora podemos verlo
            if (estadoActual == Estado.Alerta)
            {
                Vector2 direccion = (jugadorDetectado.position - transform.position).normalized;
                Vector2 origenRaycast = (Vector2)transform.position + new Vector2(0, -0.3f);
                Vector2 direccionRaycast = new Vector2(direccion.x, 0);

                RaycastHit2D hit = Physics2D.Raycast(origenRaycast, direccionRaycast, distanciaDeteccionObstaculo, capaObstaculos);

                Debug.DrawRay(origenRaycast, direccionRaycast * distanciaDeteccionObstaculo, Color.green);

                if (hit.collider == null)
                {
                    // Jugador visible de nuevo - Volver a perseguir
                    Debug.Log("Jugador visible de nuevo! Volviendo a perseguir");
                    estadoActual = Estado.Persiguiendo;
                    tiempoSinVerJugador = 0f;
                }
            }
        }
    }

    // ============================================
    // TRIGGER: Perder jugador al SALIR de zona
    // ============================================
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            estadoActual = Estado.Buscando;
            ultimaPosicionJugador = jugadorDetectado.position;
            tiempoSinVerJugador = 0f;
            Debug.Log("Jugador perdido... buscando");
        }
    }
}