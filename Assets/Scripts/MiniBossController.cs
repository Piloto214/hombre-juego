using UnityEngine;

public class MiniBossController : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 20;
    private int vidaActual;

    [Header("Estados")]
    [SerializeField] private Estado estadoActual = Estado.TomandoCafe;
    public enum Estado { TomandoCafe, Dialogo, Patrulla, Alerta, AtaqueTaza, AtaqueGuajolota, Salto, Embestida, Transformacion, Muerto }

    [Header("Movimiento - Patrulla")]
    [SerializeField] private Transform puntoA;
    [SerializeField] private Transform puntoB;
    [SerializeField] private float velocidadPatrulla = 1.5f;
    [SerializeField] private float velocidadEmbestida = 4f;
    private Transform objetivoPatrulla;
    private bool mirandoDerecha = true;

    [Header("Deteccion")]
    [SerializeField] private float distanciaDeteccion = 5f;
    [SerializeField] private LayerMask capaJugador;

    [Header("Alerta y distancia de combate")]
    [SerializeField] private float tiempoEsperaAlerta = 0.3f;
    [SerializeField] private float toleranciaDistancia = 0.4f;
    [SerializeField] private float velocidadReposicion = 2f;
    [SerializeField] private float tiempoMaximoReposicion = 1.5f;

    private float RangoEmbestida => velocidadEmbestida * duracionEmbestida;

    [Header("Ataque 1: Taza de Cafe")]
    [SerializeField] private GameObject prefabTazaCafe;
    [SerializeField] private Transform puntoLanzamiento;
    [SerializeField] private int danioTaza = 1;
    [SerializeField] private float cooldownTaza = 2f;
    [SerializeField] private float cooldownTazaFase2 = 1f;
    [SerializeField] private float velocidadHorizontalTaza = 5f;

    [Header("Ataque 2: Guajolota (fase 2)")]
    [SerializeField] private GameObject prefabGuajolota;
    [SerializeField] private int danioGuajolota = 2;
    [SerializeField] private float cooldownGuajolota = 2f;
    [SerializeField] private float cooldownGuajolotaFase2 = 1f;
    [SerializeField] private float velocidadHorizontalGuajolota = 5f;
    [SerializeField] private float duracionLentitud = 3f;
    [SerializeField] private float multiplicadorLentitud = 0.5f;

    [Header("Ataque 3: Embestida")]
    [SerializeField] private float duracionEmbestida = 1f;
    [SerializeField] private float multiplicadorDanioEmbestida = 2f;

    [Header("Ataque 4 (Fase 2): Salto de Impacto")]
    [SerializeField] private float duracionApuntado = 0.4f;
    [SerializeField] private float alturaSalto = 3f;
    [SerializeField] private float duracionRecuperacionSalto = 1f;
    [SerializeField] private Transform puntoTierra;
    [SerializeField] private GameObject prefabOndaSuelo;
    [SerializeField] private float velocidadOnda = 6f;
    [SerializeField] private float alcanceOnda = 4f;
    [SerializeField] private int danioOnda = 1;

    [Header("Contacto de cuerpo")]
    [SerializeField] private int danioContactoCuerpo = 1;
    [SerializeField] private float fuerzaEmpujeCuerpo = 20f;

    [Header("Fase 2")]
    [SerializeField] private bool faseDosActivada = false;
    private bool siguienteAtaqueEsTaza = true;

    [Header("Visual Placeholder")]
    [SerializeField] private Color colorNormal = new Color(0.6f, 0.4f, 0.2f);
    [SerializeField] private Color colorDanio = Color.white;
    [SerializeField] private Color colorPanico = new Color(1f, 0.5f, 0f);
    [SerializeField] private float tiempoFlash = 0.1f;

    [Header("Transicion Fase 2 (grito)")]
    [SerializeField] private float duracionGrito = 1.5f;
    [SerializeField] private int cantidadOndas = 3;
    [SerializeField] private float radioMaximoOnda = 4f;
    [SerializeField] private float duracionOndaGrito = 1f;
    [SerializeField] private Color colorOndaGrito = new Color(1f, 0.4f, 0f, 0.8f);

    [Header("Drop Tarjeta")]
    [SerializeField] private GameObject tarjetaPrefab;

    public delegate void MuerteMiniBoss();
    public static event MuerteMiniBoss OnMuerte;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform jugador;
    private bool dialogoMostrado = false;
    private Coroutine cicloCombateActivo;
    private Vector3 posicionInicialBoss;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        vidaActual = vidaMaxima;
        objetivoPatrulla = puntoA;
        spriteRenderer.color = colorNormal;
        posicionInicialBoss = transform.position;
    }

    private void OnEnable()
    {
        PlayerHealth.OnRespawn += ReiniciarBoss;
    }

    private void OnDisable()
    {
        PlayerHealth.OnRespawn -= ReiniciarBoss;
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
            case Estado.Patrulla:
                EstadoPatrulla();
                break;
            default:
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
        estadoActual = Estado.Patrulla;
    }

    private void EstadoPatrulla()
    {
        Patrullar();

        if (jugador != null)
        {
            float distancia = Vector2.Distance(transform.position, jugador.position);
            if (distancia <= distanciaDeteccion)
            {
                IniciarCicloCombate();
            }
        }
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

    private void IniciarCicloCombate()
    {
        if (cicloCombateActivo != null) return;
        cicloCombateActivo = StartCoroutine(CicloCombate());
    }

    private System.Collections.IEnumerator CicloCombate()
    {
        while (jugador != null && estadoActual != Estado.Muerto)
        {
            yield return StartCoroutine(EstadoAlertaConReposicion());

            if (estadoActual == Estado.Muerto) yield break;

            if (!faseDosActivada)
            {
                yield return StartCoroutine(EjecutarAtaqueTaza());
            }
            else
            {
                yield return StartCoroutine(EjecutarSaltoImpacto());
                if (estadoActual == Estado.Muerto) yield break;

                if (siguienteAtaqueEsTaza)
                    yield return StartCoroutine(EjecutarAtaqueTaza());
                else
                    yield return StartCoroutine(EjecutarAtaqueGuajolota());

                siguienteAtaqueEsTaza = !siguienteAtaqueEsTaza;
            }

            if (estadoActual == Estado.Muerto) yield break;

            yield return StartCoroutine(EjecutarEmbestida());

            if (estadoActual == Estado.Muerto) yield break;

            float distancia = Vector2.Distance(transform.position, jugador.position);
            if (distancia > distanciaDeteccion)
            {
                estadoActual = Estado.Patrulla;
                cicloCombateActivo = null;
                yield break;
            }
        }

        cicloCombateActivo = null;
    }

    private System.Collections.IEnumerator EstadoAlertaConReposicion()
    {
        estadoActual = Estado.Alerta;

        float tiempo = 0f;
        float rango = RangoEmbestida;

        while (tiempo < tiempoMaximoReposicion)
        {
            if (jugador == null) break;

            float distancia = Vector2.Distance(transform.position, jugador.position);
            float diferencia = distancia - rango;

            if (Mathf.Abs(diferencia) <= toleranciaDistancia)
            {
                break;
            }

            Vector2 direccionAlJugador = (jugador.position - transform.position).normalized;
            float sentido = (diferencia < 0f) ? -1f : 1f;

            rb.linearVelocity = new Vector2(direccionAlJugador.x * sentido * velocidadReposicion, rb.linearVelocity.y);

            if (direccionAlJugador.x > 0 && !mirandoDerecha) Voltear();
            else if (direccionAlJugador.x < 0 && mirandoDerecha) Voltear();

            tiempo += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(tiempoEsperaAlerta);
    }

    private System.Collections.IEnumerator EjecutarAtaqueTaza()
    {
        estadoActual = Estado.AtaqueTaza;
        LanzarTaza();
        Debug.Log("GUARDIA lanza la taza de cafe.");

        float espera = faseDosActivada ? cooldownTazaFase2 : cooldownTaza;
        yield return new WaitForSeconds(espera);
    }

    private System.Collections.IEnumerator EjecutarAtaqueGuajolota()
    {
        estadoActual = Estado.AtaqueGuajolota;
        LanzarGuajolota();
        Debug.Log("GUARDIA lanza la guajolota.");

        float espera = faseDosActivada ? cooldownGuajolotaFase2 : cooldownGuajolota;
        yield return new WaitForSeconds(espera);
    }

    private Vector2 CalcularVelocidadParabola(Vector2 origen, Vector2 destino, float velocidadHorizontal, float gravityScaleProyectil)
    {
        float deltaX = destino.x - origen.x;
        float deltaY = destino.y - origen.y;

        float distanciaHorizontal = Mathf.Abs(deltaX);
        if (distanciaHorizontal < 0.1f) distanciaHorizontal = 0.1f;

        float tiempoVuelo = distanciaHorizontal / velocidadHorizontal;

        float gravedad = Physics2D.gravity.y * gravityScaleProyectil;

        float vx = deltaX / tiempoVuelo;
        float vy = (deltaY - 0.5f * gravedad * tiempoVuelo * tiempoVuelo) / tiempoVuelo;

        return new Vector2(vx, vy);
    }

    // Calcula la velocidad inicial del SALTO DEL BOSS usando una altura maxima deseada,
    // en vez de derivarla de la velocidad horizontal (a diferencia de los proyectiles).
    private Vector2 CalcularVelocidadSaltoConAltura(Vector2 origen, Vector2 destino, float alturaMaxima, float gravityScaleBoss)
    {
        float gravedad = Physics2D.gravity.y * gravityScaleBoss;

        float vy0 = Mathf.Sqrt(-2f * gravedad * alturaMaxima);

        float deltaX = destino.x - origen.x;

        float a = 0.5f * gravedad;
        float b = vy0;
        float c = -(destino.y - origen.y);
        float discriminante = Mathf.Max(b * b - 4f * a * c, 0f);
        float raiz = Mathf.Sqrt(discriminante);

        float t1 = (-b + raiz) / (2f * a);
        float t2 = (-b - raiz) / (2f * a);
        float tiempoVuelo = Mathf.Max(t1, t2);
        if (tiempoVuelo <= 0.01f) tiempoVuelo = 0.01f;

        float vx = deltaX / tiempoVuelo;

        return new Vector2(vx, vy0);
    }

    private void LanzarTaza()
    {
        if (prefabTazaCafe == null || puntoLanzamiento == null || jugador == null) return;

        GameObject proyectil = Instantiate(prefabTazaCafe, puntoLanzamiento.position, Quaternion.identity);

        Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
        if (rbProyectil != null)
        {
            Vector2 velocidad = CalcularVelocidadParabola(puntoLanzamiento.position, jugador.position, velocidadHorizontalTaza, rbProyectil.gravityScale);
            rbProyectil.linearVelocity = velocidad;
        }

        TazaCafe scriptTaza = proyectil.GetComponent<TazaCafe>();
        if (scriptTaza != null) scriptTaza.danio = danioTaza;
    }

    private void LanzarGuajolota()
    {
        if (prefabGuajolota == null || puntoLanzamiento == null || jugador == null) return;

        GameObject proyectil = Instantiate(prefabGuajolota, puntoLanzamiento.position, Quaternion.identity);

        Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
        if (rbProyectil != null)
        {
            Vector2 velocidad = CalcularVelocidadParabola(puntoLanzamiento.position, jugador.position, velocidadHorizontalGuajolota, rbProyectil.gravityScale);
            rbProyectil.linearVelocity = velocidad;
        }

        Guajolota scriptGuajolota = proyectil.GetComponent<Guajolota>();
        if (scriptGuajolota != null)
        {
            scriptGuajolota.danio = danioGuajolota;
            scriptGuajolota.duracionLentitud = duracionLentitud;
            scriptGuajolota.multiplicadorLentitud = multiplicadorLentitud;
        }
    }

    private System.Collections.IEnumerator EjecutarSaltoImpacto()
    {
        estadoActual = Estado.Salto;
        spriteRenderer.color = colorPanico;

        Vector3 objetivo = transform.position;

        float tiempoApuntado = 0f;
        while (tiempoApuntado < duracionApuntado)
        {
            if (jugador != null) objetivo = jugador.position;
            tiempoApuntado += Time.deltaTime;
            yield return null;
        }

        Vector2 velocidadSalto = CalcularVelocidadSaltoConAltura(transform.position, objetivo, alturaSalto, rb.gravityScale);
        rb.linearVelocity = velocidadSalto;

        float gravedad = Physics2D.gravity.y * rb.gravityScale;
        float a = 0.5f * gravedad;
        float b = velocidadSalto.y;
        float c = -(objetivo.y - transform.position.y);
        float discriminante = Mathf.Max(b * b - 4f * a * c, 0f);
        float raiz = Mathf.Sqrt(discriminante);
        float t1 = (-b + raiz) / (2f * a);
        float t2 = (-b - raiz) / (2f * a);
        float tiempoVuelo = Mathf.Max(t1, t2);

        yield return new WaitForSeconds(tiempoVuelo);

        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = colorNormal;

        GenerarOndasSuelo();

        // Pausa de recuperacion tras el impacto: el boss se queda quieto un momento,
        // como si el propio golpe contra el suelo lo dejara aturdido brevemente.
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(duracionRecuperacionSalto);
    }

    private void GenerarOndasSuelo()
    {
        if (prefabOndaSuelo == null) return;

        Vector3 posicionSpawn = (puntoTierra != null) ? puntoTierra.position : transform.position;

        GameObject ondaFrente = Instantiate(prefabOndaSuelo, posicionSpawn, Quaternion.identity);
        OndaSuelo scriptFrente = ondaFrente.GetComponent<OndaSuelo>();
        if (scriptFrente != null)
        {
            scriptFrente.danio = danioOnda;
            scriptFrente.velocidad = velocidadOnda;
            scriptFrente.alcance = alcanceOnda;
            scriptFrente.Configurar(mirandoDerecha ? Vector2.right : Vector2.left);
        }

        GameObject ondaAtras = Instantiate(prefabOndaSuelo, posicionSpawn, Quaternion.identity);
        OndaSuelo scriptAtras = ondaAtras.GetComponent<OndaSuelo>();
        if (scriptAtras != null)
        {
            scriptAtras.danio = danioOnda;
            scriptAtras.velocidad = velocidadOnda;
            scriptAtras.alcance = alcanceOnda;
            scriptAtras.Configurar(mirandoDerecha ? Vector2.left : Vector2.right);
        }
    }

    private System.Collections.IEnumerator EjecutarEmbestida()
    {
        estadoActual = Estado.Embestida;
        spriteRenderer.color = colorPanico;

        float tiempo = 0f;
        while (tiempo < duracionEmbestida)
        {
            if (jugador == null) break;

            Vector2 direccion = (jugador.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direccion.x * velocidadEmbestida, rb.linearVelocity.y);

            if (direccion.x > 0 && !mirandoDerecha) Voltear();
            else if (direccion.x < 0 && mirandoDerecha) Voltear();

            tiempo += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = colorNormal;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        PlayerHealth jugadorVida = collision.collider.GetComponent<PlayerHealth>();
        if (jugadorVida == null) return;

        if (estadoActual == Estado.Embestida)
        {
            int danioEmbestida = Mathf.RoundToInt(danioTaza * multiplicadorDanioEmbestida);
            jugadorVida.RecibirGolpe(transform.position, danioEmbestida, fuerzaEmpujeCuerpo);
        }
        else if (estadoActual == Estado.Patrulla || estadoActual == Estado.Alerta ||
                 estadoActual == Estado.AtaqueTaza || estadoActual == Estado.AtaqueGuajolota ||
                 estadoActual == Estado.Salto)
        {
            jugadorVida.RecibirGolpe(transform.position, danioContactoCuerpo, fuerzaEmpujeCuerpo);
        }
    }

    public void RecibirDanio(int cantidad)
    {
        if (estadoActual == Estado.Muerto) return;
        if (estadoActual == Estado.TomandoCafe || estadoActual == Estado.Dialogo) return;
        if (estadoActual == Estado.Transformacion) return;

        vidaActual -= cantidad;
        StartCoroutine(FlashDanio());

        Debug.Log("Guardia recibio dano. Vida: " + vidaActual + "/" + vidaMaxima);

        if (vidaActual <= 0)
        {
            Morir();
            return;
        }

        if (!faseDosActivada && vidaActual <= vidaMaxima / 2)
        {
            IniciarTransicionFase2();
        }
    }

    private System.Collections.IEnumerator FlashDanio()
    {
        spriteRenderer.color = colorDanio;
        yield return new WaitForSeconds(tiempoFlash);
        spriteRenderer.color = colorNormal;
    }

    private void IniciarTransicionFase2()
    {
        if (cicloCombateActivo != null)
        {
            StopCoroutine(cicloCombateActivo);
            cicloCombateActivo = null;
        }

        StartCoroutine(TransicionFase2());
    }

    private System.Collections.IEnumerator TransicionFase2()
    {
        estadoActual = Estado.Transformacion;
        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = colorPanico;

        Debug.Log("GUARDIA entra en FASE 2 (grito de furia).");

        StartCoroutine(GenerarOndasGrito());

        yield return new WaitForSeconds(duracionGrito);

        faseDosActivada = true;
        spriteRenderer.color = colorNormal;

        IniciarCicloCombate();
    }

    private System.Collections.IEnumerator GenerarOndasGrito()
    {
        float intervalo = duracionGrito / cantidadOndas;

        for (int i = 0; i < cantidadOndas; i++)
        {
            StartCoroutine(CrearOndaGrito());
            yield return new WaitForSeconds(intervalo);
        }
    }

    private System.Collections.IEnumerator CrearOndaGrito()
    {
        GameObject onda = new GameObject("OndaFase2");
        onda.transform.position = transform.position;

        LineRenderer lr = onda.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.positionCount = 32;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = colorOndaGrito;
        lr.endColor = colorOndaGrito;

        Destroy(onda, duracionOndaGrito + 0.1f);

        float tiempo = 0f;
        while (tiempo < duracionOndaGrito)
        {
            float progreso = tiempo / duracionOndaGrito;
            float radioActual = Mathf.Lerp(0.2f, radioMaximoOnda, progreso);

            DibujarCirculo(lr, radioActual);

            Color colorActual = colorOndaGrito;
            colorActual.a = Mathf.Lerp(colorOndaGrito.a, 0f, progreso);
            lr.startColor = colorActual;
            lr.endColor = colorActual;

            tiempo += Time.deltaTime;
            yield return null;
        }

        Destroy(onda);
    }

    private void DibujarCirculo(LineRenderer lr, float radio)
    {
        int segmentos = lr.positionCount;
        for (int i = 0; i < segmentos; i++)
        {
            float angulo = (i / (float)segmentos) * Mathf.PI * 2f;
            Vector3 punto = new Vector3(
                transform.position.x + Mathf.Cos(angulo) * radio,
                transform.position.y + Mathf.Sin(angulo) * radio,
                0f
            );
            lr.SetPosition(i, punto);
        }
    }

    private void Morir()
    {
        estadoActual = Estado.Muerto;
        Debug.Log("GUARDIA DERROTADO.");

        StopAllCoroutines();
        cicloCombateActivo = null;

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

    private void ReiniciarBoss()
    {
        if (estadoActual == Estado.Muerto) return;
        if (estadoActual == Estado.TomandoCafe) return;

        StopAllCoroutines();
        cicloCombateActivo = null;

        vidaActual = vidaMaxima;
        faseDosActivada = false;
        siguienteAtaqueEsTaza = true;

        transform.position = posicionInicialBoss;
        objetivoPatrulla = puntoA;
        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = colorNormal;

        estadoActual = Estado.Patrulla;

        Debug.Log("GUARDIA reiniciado: vida completa, de vuelta a patrulla.");
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, RangoEmbestida);
    }
}