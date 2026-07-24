using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D m_rigidbody2D;
    private GatherInput m_gatherinput;
    private Transform m_transform;
    private Animator m_animator;
    private SpriteRenderer m_spriteRenderer;

    [Header("Move and Jump settings")]
    [SerializeField] private float speed;
    private int direction = 1;
    [SerializeField] private float Jumpforce;
    [SerializeField] private float multiplicadorSaltoBajo = 0.5f;
    [SerializeField] private float tiempoMaximoSalto = 0.25f;
    private float tiempoSaltando = 0f;
    private bool saltando = false;
    private bool botonSaltoLiberado = true;
    private int idSpeed;
    private float multiplicadorVelocidad = 1f;

    private bool enHitstun = false;
    private bool bloqueoExterno = false;
    private bool PuedeControlar => !enHitstun && !bloqueoExterno;

    [Header("Dash settings")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;
    private float originalGravity;

    [Header("Ground settings")]
    [SerializeField] private Transform lFoot;
    [SerializeField] private Transform rFoot;
    [SerializeField] private bool isGrounded;
    [SerializeField] private float rayLength;
    [SerializeField] private LayerMask groundLayer;
    private int idIsGrounded;

    [Header("Melee settings")]
    [SerializeField] private float meleeRange = 1.5f;

    [Header("Melee - Empuje y Combo")]
    [SerializeField] private float empujeNormal = 5f;
    [SerializeField] private float empujeCombo = 12f;
    [SerializeField] private float tiempoLimiteCombo = 0.6f;
    private int comboActual = 0;
    private float tiempoUltimoGolpe = -10f;

    [Header("Golpe Cargado")]
    [SerializeField] private float tiempoAntesDeParpadear = 0.15f;
    [SerializeField] private float tiempoCarga = 1f;
    [SerializeField] private float empujeCargado = 18f;
    [SerializeField] private float duracionAturdimientoCargado = 2f;
    [SerializeField] private Color colorCarga = new Color(0.6f, 0.2f, 1f, 1f); // morado, distinto a los demas estados
    private bool botonAtaqueAnterior = false;
    private bool cargando = false;
    private float tiempoCargando = 0f;
    private bool cargaCompleta = false;
    private bool parpadeoIniciado = false;
    private Color colorOriginalAntesDeCarga;
    private Coroutine parpadeoCargaCoroutine;

    [Header("Ondas de aire (golpe cargado)")]
    [SerializeField] private float duracionOndaAire = 0.25f;
    [SerializeField] private float alcanceOndaAire = 2.5f;
    [SerializeField] private Color colorOndaAire = new Color(0.6f, 0.2f, 1f, 0.9f);

    [Header("Pistola")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootCooldown = 0.3f;
    private float lastShootTime;

    [Header("Recarga")]
    [SerializeField] private int municionMaxima = 8;
    [SerializeField] private float tiempoRecarga = 1.5f;
    [SerializeField] private Color colorRecarga = new Color(1f, 0.55f, 0f, 1f); // naranja
    private int municionActual;
    private bool isReloading = false;

    void Start()
    {
        m_gatherinput = GetComponent<GatherInput>();
        m_transform = GetComponent<Transform>();
        m_rigidbody2D = GetComponent<Rigidbody2D>();
        m_animator = GetComponent<Animator>();
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        idSpeed = Animator.StringToHash("Speed");
        idIsGrounded = Animator.StringToHash("isGrounded");
        lFoot = GameObject.Find("LFoot").GetComponent<Transform>();
        rFoot = GameObject.Find("RFoot").GetComponent<Transform>();
        originalGravity = m_rigidbody2D.gravityScale;

        municionActual = municionMaxima;
    }

    private void Update()
    {
        SetAnimatorValues();
        HandleShooting();
    }

    private void SetAnimatorValues()
    {
        m_animator.SetFloat(idSpeed, Mathf.Abs(m_rigidbody2D.linearVelocityX));
        m_animator.SetBool(idIsGrounded, isGrounded);
    }

    void FixedUpdate()
    {
        if (PuedeControlar)
        {
            if (!isDashing)
            {
                Move();
                Jump();
            }
            Attack();
        }
        CheckGround();
    }

    private void Move()
    {
        Flip();

        float inputX = m_gatherinput.ValueX;

        if (m_gatherinput.IsDashing && canDash && !isDashing && isGrounded)
        {
            StartCoroutine(Dash());
            return;
        }

        m_rigidbody2D.linearVelocity = new Vector2(speed * multiplicadorVelocidad * inputX, m_rigidbody2D.linearVelocityY);
    }

    private System.Collections.IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;

        float dashDirection = direction;

        m_rigidbody2D.gravityScale = 0;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = Color.cyan;
        transform.localScale = new Vector3(dashDirection * 1.5f, 0.7f, 1f);

        m_rigidbody2D.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);

        yield return new WaitForSeconds(dashDuration);

        m_rigidbody2D.gravityScale = originalGravity;
        m_rigidbody2D.linearVelocity = Vector2.zero;
        sr.color = originalColor;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void Flip()
    {
        if (m_gatherinput.ValueX * direction < 0)
        {
            float currentY = m_transform.localScale.y;
            m_transform.localScale = new Vector3(-m_transform.localScale.x, currentY, 1);
            direction *= -1;
        }
    }

    private void Jump()
    {
        if (!m_gatherinput.IsJumping)
            botonSaltoLiberado = true;

        if (m_gatherinput.IsJumping && isGrounded && !saltando && botonSaltoLiberado)
        {
            saltando = true;
            botonSaltoLiberado = false;
            tiempoSaltando = 0f;
            m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocityX, Jumpforce);
        }

        if (m_gatherinput.IsJumping && saltando && m_rigidbody2D.linearVelocityY > 0)
        {
            tiempoSaltando += Time.fixedDeltaTime;
            if (tiempoSaltando >= tiempoMaximoSalto)
                saltando = false;
        }

        if (!m_gatherinput.IsJumping && saltando && m_rigidbody2D.linearVelocityY > 0)
        {
            m_rigidbody2D.linearVelocity = new Vector2(
                m_rigidbody2D.linearVelocityX,
                m_rigidbody2D.linearVelocityY * multiplicadorSaltoBajo
            );
            saltando = false;
        }

        if (m_rigidbody2D.linearVelocityY < 0)
            saltando = false;
    }

    // ============================================
    // ATAQUE CUERPO A CUERPO
    // Detecta tap (golpe normal / combo) vs. mantener
    // presionado (golpe cargado) usando el flanco de
    // subida/bajada de IsAttacking, sin modificar
    // GatherInput.cs.
    // ============================================
    private void Attack()
    {
        bool botonPresionado = m_gatherinput.IsAttacking;

        // Flanco de subida: se acaba de presionar el boton.
        // Aun no arrancamos el parpadeo — solo empieza a "cargar" en silencio.
        if (botonPresionado && !botonAtaqueAnterior)
        {
            cargando = true;
            tiempoCargando = 0f;
            cargaCompleta = false;
            parpadeoIniciado = false;
            colorOriginalAntesDeCarga = m_spriteRenderer.color;
        }

        // Mientras se mantiene presionado: acumula tiempo de carga.
        // El parpadeo solo arranca despues de tiempoAntesDeParpadear,
        // asi un tap rapido (golpe normal) nunca llega a parpadear.
        if (botonPresionado && cargando)
        {
            tiempoCargando += Time.fixedDeltaTime;

            if (!parpadeoIniciado && tiempoCargando >= tiempoAntesDeParpadear)
            {
                parpadeoIniciado = true;
                if (parpadeoCargaCoroutine != null) StopCoroutine(parpadeoCargaCoroutine);
                parpadeoCargaCoroutine = StartCoroutine(ParpadeoCargaCoroutine());
            }

            if (!cargaCompleta && tiempoCargando >= tiempoCarga)
            {
                cargaCompleta = true;
                GenerarOndasDeAire();
            }
        }

        // Flanco de bajada: se solto el boton -> se ejecuta el golpe
        if (!botonPresionado && botonAtaqueAnterior)
        {
            if (parpadeoCargaCoroutine != null)
            {
                StopCoroutine(parpadeoCargaCoroutine);
                parpadeoCargaCoroutine = null;
            }
            m_spriteRenderer.color = colorOriginalAntesDeCarga;

            EjecutarGolpe(cargaCompleta);

            cargando = false;
            tiempoCargando = 0f;
            cargaCompleta = false;
        }

        botonAtaqueAnterior = botonPresionado;
    }

    private void EjecutarGolpe(bool esCargado)
    {
        Collider2D[] objetivos = Physics2D.OverlapCircleAll(transform.position, meleeRange);

        bool esGolpeDeCombo = false;

        if (!esCargado)
        {
            if (Time.time - tiempoUltimoGolpe > tiempoLimiteCombo)
                comboActual = 0;

            comboActual++;
            tiempoUltimoGolpe = Time.time;

            if (comboActual >= 4)
            {
                esGolpeDeCombo = true;
                comboActual = 0;
            }
        }
        else
        {
            // El golpe cargado no se mezcla con el contador de combo
            comboActual = 0;
        }

        foreach (Collider2D objetivo in objetivos)
        {
            PropVida prop = objetivo.GetComponent<PropVida>();
            if (prop != null) prop.RecibirGolpe(1);

            Vector2 direccionEmpuje = ((Vector2)objetivo.transform.position - (Vector2)m_transform.position);
            if (direccionEmpuje == Vector2.zero) direccionEmpuje = new Vector2(direction, 0f);
            direccionEmpuje.Normalize();

            EnemyHealth enemy = objetivo.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                if (esCargado)
                {
                    enemy.TakeDamage(1, direccionEmpuje, empujeCargado);
                    enemy.AplicarAturdimiento(duracionAturdimientoCargado);
                }
                else if (esGolpeDeCombo)
                {
                    Vector2 empujeElevacion = new Vector2(direccionEmpuje.x, 1f).normalized;
                    enemy.TakeDamage(1, empujeElevacion, empujeCombo);
                }
                else
                {
                    enemy.TakeDamage(1, direccionEmpuje, empujeNormal);
                }
            }

            // El boss SIEMPRE recibe solo daño, sin empuje ni aturdimiento,
            // sin importar si el golpe es normal, de combo o cargado.
            MiniBossController miniBoss = objetivo.GetComponent<MiniBossController>();
            if (miniBoss != null) miniBoss.RecibirDanio(1);
        }
    }

    // ============================================
    // PARPADEO DE CARGA
    // El parpadeo va de lento a rapido conforme se
    // acerca a la carga completa; al llegar a carga
    // completa el parpadeo se mantiene muy rapido
    // hasta que se suelta el boton.
    // ============================================
    private System.Collections.IEnumerator ParpadeoCargaCoroutine()
    {
        while (cargando)
        {
            float progreso = cargaCompleta ? 1f : Mathf.Clamp01(tiempoCargando / tiempoCarga);
            float intervalo = Mathf.Lerp(0.22f, 0.04f, progreso);

            m_spriteRenderer.color = colorCarga;
            yield return new WaitForSeconds(intervalo);
            if (!cargando) break;
            m_spriteRenderer.color = colorOriginalAntesDeCarga;
            yield return new WaitForSeconds(intervalo);
        }
    }

    // ============================================
    // ONDAS DE AIRE (feedback visual de carga completa)
    // Distintas a las OndaSuelo del boss: mas rapidas,
    // en forma de arco corto frente al player, y
    // puramente visuales (no hacen daño).
    // ============================================
    private void GenerarOndasDeAire()
    {
        StartCoroutine(OndaAireCoroutine());
    }

    private System.Collections.IEnumerator OndaAireCoroutine()
    {
        GameObject onda = new GameObject("OndaAireCargada");
        onda.transform.position = transform.position;

        LineRenderer lr = onda.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = colorOndaAire;
        lr.endColor = colorOndaAire;

        float tiempo = 0f;
        while (tiempo < duracionOndaAire)
        {
            float progreso = tiempo / duracionOndaAire;
            float distanciaActual = Mathf.Lerp(0.3f, alcanceOndaAire, progreso);

            Vector3 origenArco = transform.position + new Vector3(direction * 0.3f, 0.2f, 0f);
            Vector3 finArco = transform.position + new Vector3(direction * distanciaActual, 0.6f, 0f);

            lr.SetPosition(0, origenArco);
            lr.SetPosition(1, finArco);

            Color c = colorOndaAire;
            c.a = Mathf.Lerp(colorOndaAire.a, 0f, progreso);
            lr.startColor = c;
            lr.endColor = c;

            tiempo += Time.deltaTime;
            yield return null;
        }

        Destroy(onda);
    }

    private void CheckGround()
    {
        RaycastHit2D lFootRay = Physics2D.Raycast(lFoot.position, Vector2.down, rayLength, groundLayer);
        RaycastHit2D rFootRay = Physics2D.Raycast(rFoot.position, Vector2.down, rayLength, groundLayer);
        isGrounded = lFootRay || rFootRay;
    }

    private void HandleShooting()
    {
        if (isReloading) return;

        if (m_gatherinput.IsShooting && Time.time > lastShootTime + shootCooldown)
        {
            if (municionActual <= 0)
            {
                StartCoroutine(RecargarCoroutine());
                return;
            }

            lastShootTime = Time.time;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        Vector2 shootDir = new Vector2(direction, 0);
        bulletScript.Shoot(shootDir);

        municionActual--;

        if (municionActual <= 0)
        {
            StartCoroutine(RecargarCoroutine());
        }
    }

    // ============================================
    // RECARGA
    // Bloquea el disparo durante tiempoRecarga segundos
    // y mantiene un color solido (naranja) durante toda
    // la recarga. Sin parpadeo: el parpadeo se reserva
    // como referencia visual exclusiva de "daño".
    // ============================================
    private System.Collections.IEnumerator RecargarCoroutine()
    {
        isReloading = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color colorOriginal = sr.color;

        sr.color = colorRecarga;

        yield return new WaitForSeconds(tiempoRecarga);

        sr.color = colorOriginal;
        municionActual = municionMaxima;
        isReloading = false;
    }

    // ============================================
    // ATURDIMIENTO — onda de silbato
    // Reduce la velocidad del player visualmente
    // ============================================
    public void AplicarAturdimiento(float duracion, float multiplicador)
    {
        StartCoroutine(AturdimientoCoroutine(duracion, multiplicador));
    }

    private System.Collections.IEnumerator AturdimientoCoroutine(float duracion, float multiplicador)
    {
        multiplicadorVelocidad = multiplicador;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color colorGuard = sr.color;
        float tiempo = duracion;

        // Parpadeo amarillo — distinto al rojo (contacto) y al blanco/gris (golpe normal)
        while (tiempo > 0f)
        {
            sr.color = new Color(1f, 0.85f, 0f, 1f);
            yield return new WaitForSeconds(0.08f);
            sr.color = colorGuard;
            yield return new WaitForSeconds(0.08f);
            tiempo -= 0.16f;
        }

        sr.color = colorGuard;
        multiplicadorVelocidad = 1f;
    }

    // ============================================
    // LENTITUD
    // ============================================
    public void AplicarLentitud(float duracion, float multiplicador)
    {
        StartCoroutine(LentitudCoroutine(duracion, multiplicador));
    }

    private System.Collections.IEnumerator LentitudCoroutine(float duracion, float multiplicador)
    {
        multiplicadorVelocidad = multiplicador;
        yield return new WaitForSeconds(duracion);
        multiplicadorVelocidad = 1f;
    }

    // ============================================
    // HITSTUN
    // ============================================
    public void AplicarHitstun(float duracion)
    {
        StartCoroutine(HitstunCoroutine(duracion));
    }

    private System.Collections.IEnumerator HitstunCoroutine(float duracion)
    {
        enHitstun = true;
        yield return new WaitForSeconds(duracion);
        enHitstun = false;
    }

    // ============================================
    // BLOQUEO EXTERNO
    // ============================================
    public void BloquearControl()
    {
        bloqueoExterno = true;
        m_rigidbody2D.linearVelocity = new Vector2(0f, m_rigidbody2D.linearVelocityY);
    }

    public void DesbloquearControl()
    {
        bloqueoExterno = false;
    }
}