using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D m_rigidbody2D;
    private GatherInput m_gatherinput;
    private Transform m_transform;
    private Animator m_animator;

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

    [Header("Pistola")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootCooldown = 0.3f;
    private float lastShootTime;

    void Start()
    {
        m_gatherinput = GetComponent<GatherInput>();
        m_transform = GetComponent<Transform>();
        m_rigidbody2D = GetComponent<Rigidbody2D>();
        m_animator = GetComponent<Animator>();
        idSpeed = Animator.StringToHash("Speed");
        idIsGrounded = Animator.StringToHash("isGrounded");
        lFoot = GameObject.Find("LFoot").GetComponent<Transform>();
        rFoot = GameObject.Find("RFoot").GetComponent<Transform>();
        originalGravity = m_rigidbody2D.gravityScale;
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

    private void Attack()
    {
        if (m_gatherinput.IsAttacking)
        {
            Collider2D[] objetivos = Physics2D.OverlapCircleAll(transform.position, 1.5f);

            foreach (Collider2D objetivo in objetivos)
            {
                PropVida prop = objetivo.GetComponent<PropVida>();
                if (prop != null) prop.RecibirGolpe(1);

                EnemyHealth enemy = objetivo.GetComponent<EnemyHealth>();
                if (enemy != null) enemy.TakeDamage(1);

                MiniBossController miniBoss = objetivo.GetComponent<MiniBossController>();
                if (miniBoss != null) miniBoss.RecibirDanio(1);
            }
            m_gatherinput.IsAttacking = false;
        }
    }

    private void CheckGround()
    {
        RaycastHit2D lFootRay = Physics2D.Raycast(lFoot.position, Vector2.down, rayLength, groundLayer);
        RaycastHit2D rFootRay = Physics2D.Raycast(rFoot.position, Vector2.down, rayLength, groundLayer);
        isGrounded = lFootRay || rFootRay;
    }

    private void HandleShooting()
    {
        if (m_gatherinput.IsShooting && Time.time > lastShootTime + shootCooldown)
        {
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