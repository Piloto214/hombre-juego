using UnityEngine;

public class MiniBossController : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 5;
    private int vidaActual;

    [Header("Estados")]
    [SerializeField] private Estado estadoActual = Estado.TomandoCafe;
    public enum Estado { TomandoCafe, Dialogo, Batalla, Muerto }

    [Header("Movimiento - Patrulla")]
    [SerializeField] private Transform puntoA;
    [SerializeField] private Transform puntoB;
    [SerializeField] private float velocidadPatrulla = 1.5f;
    [SerializeField] private float velocidadPanico = 4f;
    private Transform objetivoPatrulla;
    private bool mirandoDerecha = true;

    [Header("Deteccion")]
    [SerializeField] private float distanciaDeteccion = 5f;
    [SerializeField] private LayerMask capaJugador;

    [Header("Ataque 1: Cafe Caliente")]
    [SerializeField] private GameObject prefabTazaCafe;
    [SerializeField] private Transform puntoLanzamiento;
    [SerializeField] private float cooldownCafe = 2f;
    [SerializeField] private float fuerzaLanzamiento = 8f;
    private float tiempoUltimoCafe;

    [Header("Ataque 3: Panico / Embestida")]
    [SerializeField] private float duracionEmbestida = 1f;
    private bool enEmbestida = false;

    [Header("Ataque 4: Zona Negacion")]
    [SerializeField] private float radioNegacion = 2f;
    [SerializeField] private float duracionNegacion = 2f;
    [SerializeField] private float cooldownNegacion = 5f;
    private float tiempoUltimaNegacion;
    private bool zonaNegacionActiva = false;

    [Header("Visual Placeholder")]
    [SerializeField] private Color colorNormal = new Color(0.6f, 0.4f, 0.2f);
    [SerializeField] private Color colorDanio = Color.white;
    [SerializeField] private Color colorPanico = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color colorNegacion = new Color(0.8f, 0f, 0f);
    [SerializeField] private float tiempoFlash = 0.1f;

    [Header("Drop Tarjeta")]
    [SerializeField] private GameObject tarjetaPrefab;

    // Evento de muerte
    public delegate void MuerteMiniBoss();
    public static event MuerteMiniBoss OnMuerte;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform jugador;
    private bool dialogoMostrado = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        vidaActual = vidaMaxima;
        objetivoPatrulla = puntoA;
        spriteRenderer.color = colorNormal;
    }

    private void Update()
    {
        switch (estadoActual)
        {
            case Estado.TomandoCafe:
                EstadoTomandoCafe();
                break;
            case Estado.Dialogo:
                EstadoDialogo();
                break;
            case Estado.Batalla:
                EstadoBatalla();
                break;
            case Estado.Muerto:
                break;
        }
    }

    private void EstadoTomandoCafe()
    {
        rb.linearVelocity = Vector2.zero;

        float parpadeo = Mathf.PingPong(Time.time * 0.5f, 0.1f);
        spriteRenderer.color = new Color(
            colorNormal.r + parpadeo,
            colorNormal.g + parpadeo,
            colorNormal.b + parpadeo
        );

        DetectarJugador();
    }

    private void EstadoDialogo()
    {
        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = colorNormal;

        if (!dialogoMostrado)
        {
            Debug.Log("GUARDIA: 'Oye, no puedes pasar sin tarjeta. Y esta es mia, jeje.'");
            Debug.Log("GUARDIA guarda la tarjeta en su bolsillo.");
            dialogoMostrado = true;

            Invoke(nameof(IniciarBatalla), 3f);
        }
    }

    private void IniciarBatalla()
    {
        Debug.Log("GUARDIA: '¡Entonces asi las querias! ¡VENGA!'");
        estadoActual = Estado.Batalla;
    }

    private void EstadoBatalla()
    {
        if (jugador == null) return;

        float distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

        if (Time.time > tiempoUltimaNegacion + cooldownNegacion && !zonaNegacionActiva)
        {
            ActivarZonaNegacion();
            return;
        }

        if (distanciaAlJugador > 3f && Time.time > tiempoUltimoCafe + cooldownCafe)
        {
            LanzarCafe();
            return;
        }

        if (vidaActual <= vidaMaxima / 2 && !enEmbestida)
        {
            spriteRenderer.color = colorPanico;
            Embestir();
            return;
        }

        Patrullar();
    }

    private void LanzarCafe()
    {
        if (prefabTazaCafe == null || puntoLanzamiento == null || jugador == null) return;

        tiempoUltimoCafe = Time.time;

        // OFFSET: Mover el punto de spawn hacia afuera para no quedar dentro del boss
        Vector2 direccion = (jugador.position - puntoLanzamiento.position).normalized;
        Vector2 posicionLanzamiento = (Vector2)puntoLanzamiento.position + direccion * 0.8f;

        GameObject taza = Instantiate(prefabTazaCafe, posicionLanzamiento, Quaternion.identity);

        Rigidbody2D rbTaza = taza.GetComponent<Rigidbody2D>();
        if (rbTaza != null)
        {
            rbTaza.linearVelocity = direccion * fuerzaLanzamiento;
        }

        Debug.Log("GUARDIA lanza su taza de cafe hacia " + direccion);
    }

    private void Embestir()
    {
        enEmbestida = true;
        Debug.Log("GUARDIA entra en PANICO y embiste!");

        StartCoroutine(EmbestidaCoroutine());
    }

    private System.Collections.IEnumerator EmbestidaCoroutine()
    {
        float tiempo = 0f;

        while (tiempo < duracionEmbestida)
        {
            if (jugador == null) break;

            Vector2 direccion = (jugador.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direccion.x * velocidadPanico, rb.linearVelocity.y);

            if (direccion.x > 0 && !mirandoDerecha) Voltear();
            else if (direccion.x < 0 && mirandoDerecha) Voltear();

            tiempo += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        enEmbestida = false;
    }

    private void ActivarZonaNegacion()
    {
        tiempoUltimaNegacion = Time.time;
        zonaNegacionActiva = true;
        spriteRenderer.color = colorNegacion;

        Debug.Log("GUARDIA: '¡NO PASAS SIN TARJETA!'");

        StartCoroutine(ZonaNegacionCoroutine());
    }

    private System.Collections.IEnumerator ZonaNegacionCoroutine()
    {
        float tiempo = 0f;

        while (tiempo < duracionNegacion)
        {
            Collider2D[] colisiones = Physics2D.OverlapCircleAll(transform.position, radioNegacion, capaJugador);

            foreach (Collider2D col in colisiones)
            {
                PlayerHealth jugadorVida = col.GetComponent<PlayerHealth>();
                if (jugadorVida != null)
                {
                    jugadorVida.RecibirGolpe(transform.position);
                }
            }

            tiempo += Time.deltaTime;
            yield return null;
        }

        zonaNegacionActiva = false;
        spriteRenderer.color = colorNormal;
    }

    private void Patrullar()
    {
        if (puntoA == null || puntoB == null) return;

        Vector2 direccion = (objetivoPatrulla.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direccion.x * velocidadPatrulla, rb.linearVelocity.y);

        if (direccion.x > 0 && !mirandoDerecha) Voltear();
        else if (direccion.x < 0 && mirandoDerecha) Voltear();

        if (Vector2.Distance(transform.position, objetivoPatrulla.position) < 1.5f)
        {
            objetivoPatrulla = (objetivoPatrulla == puntoA) ? puntoB : puntoA;
        }
    }

    private void DetectarJugador()
    {
        Collider2D[] colisiones = Physics2D.OverlapCircleAll(transform.position, distanciaDeteccion, capaJugador);

        foreach (Collider2D col in colisiones)
        {
            if (col.CompareTag("Player"))
            {
                jugador = col.transform;
                estadoActual = Estado.Dialogo;
                Debug.Log("GUARDIA detecta al jugador.");
                return;
            }
        }
    }

    public void RecibirDanio(int cantidad)
    {
        if (estadoActual == Estado.Muerto) return;

        vidaActual -= cantidad;
        StartCoroutine(FlashDanio());

        Debug.Log("Guardia recibio daño. Vida: " + vidaActual + "/" + vidaMaxima);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private System.Collections.IEnumerator FlashDanio()
    {
        spriteRenderer.color = colorDanio;
        yield return new WaitForSeconds(tiempoFlash);
        spriteRenderer.color = (estadoActual == Estado.Batalla && vidaActual <= vidaMaxima / 2) ? colorPanico : colorNormal;
    }

    private void Morir()
    {
        estadoActual = Estado.Muerto;
        Debug.Log("GUARDIA DERROTADO.");

        SoltarTarjeta();

        OnMuerte?.Invoke();

        StartCoroutine(MuerteConEfecto());
    }

    private void SoltarTarjeta()
    {
        if (tarjetaPrefab == null) return;

        GameObject tarjeta = Instantiate(tarjetaPrefab, transform.position, Quaternion.identity);

        Rigidbody2D rbTarjeta = tarjeta.GetComponent<Rigidbody2D>();
        if (rbTarjeta != null)
        {
            rbTarjeta.linearVelocity = new Vector2(0, 5f);
        }

        Debug.Log("Guardia solto la tarjeta de acceso!");
    }

    private System.Collections.IEnumerator MuerteConEfecto()
    {
        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            spriteRenderer.color = colorNormal;
            yield return new WaitForSeconds(0.05f);
        }

        Destroy(gameObject);
    }

    private void Voltear()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccion);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioNegacion);
    }
}