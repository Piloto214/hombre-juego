using System;
using System.Runtime.CompilerServices;
using UnityEditor.Tilemaps;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //PLAYER COMPONENTS
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

    void Start()
    {
        m_gatherinput = GetComponent<GatherInput>();
        m_transform = GetComponent <Transform> ();
        m_rigidbody2D = GetComponent<Rigidbody2D>();
        m_animator = GetComponent <Animator> ();
        idSpeed = Animator.StringToHash("Speed");
        idIsGrounded = Animator.StringToHash("isGrounded");
        lFoot = GameObject.Find("LFoot").GetComponent <Transform> ();
        rFoot = GameObject.Find("RFoot").GetComponent <Transform> ();
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
        if (!isDashing)
        {
            Move();
            Jump();
        }
        Attack();
        CheckGround();
    }

    private void Move()
    {
        Flip();

        float inputX = m_gatherinput.ValueX;

        // DASH
        if (m_gatherinput.IsDashing && canDash && !isDashing && isGrounded)
        {
            StartCoroutine(Dash());
            return;
        }

        // Movimiento normal
        m_rigidbody2D.linearVelocity = new Vector2(speed * inputX, m_rigidbody2D.linearVelocityY);
    }

    private System.Collections.IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;

        // Direccion del dash
        float dashDirection = direction;

        // Desactivar gravedad para dash recto
        m_rigidbody2D.gravityScale = 0;

        // Animacion placeholder: estirar y cambiar color
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = Color.cyan;
        transform.localScale = new Vector3(
            dashDirection * 1.5f,
            0.7f,
            1f
        );

        // Aplicar velocidad de dash
        m_rigidbody2D.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);

        // Esperar duracion
        yield return new WaitForSeconds(dashDuration);

        // Restaurar
        m_rigidbody2D.gravityScale = originalGravity;
        m_rigidbody2D.linearVelocity = Vector2.zero;
        sr.color = originalColor;

        isDashing = false;

        // Cooldown
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
        {
            botonSaltoLiberado = true;
        }

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
            {
                saltando = false;
            }
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
        {
            saltando = false;
        }
    }

    private void Attack()
    {
        if (m_gatherinput.IsAttacking)
        {
            Collider2D[] objetivos = Physics2D.OverlapCircleAll(transform.position, 1.5f);

            foreach (Collider2D objetivo in objetivos)
            {
                if (objetivo.gameObject == gameObject) continue;

                PropVida prop = objetivo.GetComponent <PropVida> ();
                if (prop == null) continue;
                {
                    prop.RecibirGolpe(1);
                    Debug.Log("Atacaste: " + objetivo.name);
                }

            }
            m_gatherinput.IsAttacking = false;
        }
    }

    private void CheckGround()
    {
        RaycastHit2D lFootRay = Physics2D.Raycast(lFoot.position, Vector2.down, rayLength, groundLayer);
        RaycastHit2D rFootRay = Physics2D.Raycast(rFoot.position, Vector2.down, rayLength, groundLayer);
        if (lFootRay || rFootRay)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    [Header("Pistola")]
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public Transform firePoint;
    [SerializeField] private float shootCooldown = 0.3f;
    private float lastShootTime;

    private void HandleShooting()
    {
        Debug.Log("HendleShooting - IsShooting: " + m_gatherinput.IsShooting);

        if (m_gatherinput.IsShooting && Time.time > lastShootTime + shootCooldown)
        {
            Debug.Log(" ¡DISPARANDO! ");
            lastShootTime = Time.time;
            Shoot();
        }
    }

    private void Shoot()
    {
        Debug.Log("=== SHOOT() LLAMADO ===");

        if (bulletPrefab == null)
        {
            Debug.LogError("ERROR: bulletPrefab es NULL! Asigna el prefab en el Inspector.");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("ERROR: firePoint es NULL! Asigna el FirePoint en el Inspector.");
            return;
        }

        Debug.Log("Creando bala...");
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Debug.Log("Bala creada: " + bullet.name + " en posicion: " + firePoint.position);

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript == null)
        {
            Debug.LogError("ERROR: La bala no tiene el script Bullet! Agregalo al prefab.");
            return;
        }

        float shootDir = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 shootDirection = new Vector2(shootDir, 0);

        Debug.Log("Disparando hacia: " + shootDirection);
        bulletScript.Shoot(shootDirection);

        Debug.Log("=== DISPARO COMPLETADO ===");
    }
}