using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SlimeController : MonoBehaviour
{
    [Header("Configuración de Tamaño")]
    [SerializeField] private Vector3 tamañoSlime = new Vector3(0.7f, 0.7f, 1f);

    [Header("Configuración de Fuerzas")]
    [SerializeField] private float baseHorizontalForce = 8f;
    [SerializeField] private float baseVerticalForce = 6f;

    [Header("Impulsos Extras en el Aire (Saltos 2 y 3)")]
    [SerializeField] private float fuerzaImpulsoAire = 5f;

    [Header("Física de Deslizamiento")]
    [SerializeField] private float wallSlideSpeed = 2.5f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 14f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float holdThreshold = 0.3f;
    [SerializeField] private float slowMotionScale = 0.3f;
    [SerializeField] private float dashArrowLength = 1.5f;
    [SerializeField] private Color auraColorReady    = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color auraColorCooldown = new Color(0.3f, 0.8f, 1f, 1f);

    [Header("Vida y Daño")]
    [SerializeField] private Color colorDaño  = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float tiempoInvulnerabilidad = 1.5f;

    [Header("Botón de Runa")]
    [SerializeField] private RuneButton runeButton;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private int jumpCount = 0;
    private bool isTouchingWall = false;
    private bool isTouchingGround = false;
    private float originalGravity;
    private float direccionActualX = 1f;

    // --- Estado del Dash ---
    private bool dashDisponible = true;
    private float dashCooldownTimer = 0f;
    private bool estaEnModoDash = false;
    private float holdTimer = 0f;
    private bool inputPresionado = false;
    private Vector2 dashDireccion = Vector2.right;

    // --- Flecha de dirección ---
    private LineRenderer flechaLine;

    // --- Vida ---
    private bool estaVivo = true;
    private bool esInvulnerable = false;

    private Camera camaraPrincipal;

    private bool ModoRunaActivo => runeButton != null && runeButton.ModoRunaActivo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalGravity = rb.gravityScale;
        transform.localScale = tamañoSlime;

        camaraPrincipal = Camera.main;

        flechaLine = gameObject.AddComponent<LineRenderer>();
        flechaLine.positionCount = 2;
        flechaLine.startWidth = 0.08f;
        flechaLine.endWidth = 0.2f;
        flechaLine.material = new Material(Shader.Find("Sprites/Default"));
        flechaLine.startColor = new Color(1f, 1f, 0.2f, 0.8f);
        flechaLine.endColor = new Color(1f, 0.4f, 0f, 0.9f);
        flechaLine.enabled = false;

        if (runeButton == null)
            runeButton = FindFirstObjectByType<RuneButton>();

        ActualizarCooldownDash();
        ActualizarAura();
    }

    void Update()
    {
        if (!estaVivo) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Climbing) return;

        if (ModoRunaActivo)
        {
            ActualizarBotonRuna();
            VerificarLimitePantalla();
            return;
        }

        ManejarCooldownDash();
        ManejarInputDash();
        ManejarSaltoPorTap();
        ActualizarBotonRuna();

        if (isTouchingWall && !isTouchingGround && rb.linearVelocity.y < 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);

        VerificarLimitePantalla();
    }

    // ─────────────────────────────────────────────
    //  SISTEMA DE VIDA Y DAÑO
    // ─────────────────────────────────────────────
    public void RecibirDaño()
    {
        if (!estaVivo || esInvulnerable) return;

        if (GameManager.Instance != null && GameManager.Instance.ConsumirEscudo())
        {
            StartCoroutine(FlashDaño());
            StartCoroutine(PeriodoInvulnerabilidad());
            return;
        }

        Morir();
    }

    void Morir()
    {
        if (!estaVivo) return;
        estaVivo = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        StartCoroutine(SecuenciaMuerte());
    }

    IEnumerator SecuenciaMuerte()
    {
        // Flash rojo
        if (sr != null) sr.color = colorDaño;
        yield return new WaitForSeconds(0.3f);

        // Desaparecer
        if (sr != null) sr.enabled = false;
        yield return new WaitForSeconds(0.5f);

        // Cambiar estado a GameOver — GameOverUI mostrará el panel
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetStats();
            GameManager.Instance.ChangeState(GameState.GameOver);
        }
        // ← Sin reload automático, lo maneja el botón Reintentar
    }

    IEnumerator FlashDaño()
    {
        Color colorOriginal = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = colorDaño;
        yield return new WaitForSeconds(0.15f);
        if (sr != null) sr.color = colorOriginal;
    }

    IEnumerator PeriodoInvulnerabilidad()
    {
        esInvulnerable = true;

        float elapsed = 0f;
        while (elapsed < tiempoInvulnerabilidad)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (sr != null) sr.enabled = true;
        esInvulnerable = false;
    }

    // ─────────────────────────────────────────────
    //  COOLDOWN DEL DASH
    // ─────────────────────────────────────────────
    public void ActualizarCooldownDash()
    {
        if (GameManager.Instance != null)
            dashCooldown = GameManager.Instance.GetDashCooldown(3f);
    }

    // ─────────────────────────────────────────────
    //  BOTÓN DE RUNA
    // ─────────────────────────────────────────────
    void ActualizarBotonRuna()
    {
        if (runeButton == null) return;
        bool enElAire = !isTouchingWall && !isTouchingGround;
        runeButton.SetBotonDisponible(enElAire);
    }

    // ─────────────────────────────────────────────
    //  COOLDOWN Y AURA
    // ─────────────────────────────────────────────
    void ManejarCooldownDash()
    {
        if (!dashDisponible)
        {
            dashCooldownTimer -= Time.unscaledDeltaTime;
            if (dashCooldownTimer <= 0f)
            {
                dashDisponible = true;
                ActualizarAura();
            }
        }
    }

    void ActualizarAura()
    {
        if (sr != null)
            sr.color = dashDisponible ? auraColorReady : auraColorCooldown;
    }

    // ─────────────────────────────────────────────
    //  INPUT: HOLD = DASH / TAP = SALTO
    // ─────────────────────────────────────────────
    void ManejarInputDash()
    {
        bool presionandoAhora = false;
        Vector2 posicionInput = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            presionandoAhora = true;
            posicionInput = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            presionandoAhora = true;
            posicionInput = Mouse.current.position.ReadValue();
        }

        bool enElAire = !isTouchingWall && !isTouchingGround;

        if (presionandoAhora && enElAire && dashDisponible)
        {
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdThreshold && !estaEnModoDash)
            {
                estaEnModoDash = true;
                Time.timeScale = slowMotionScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                flechaLine.enabled = true;
            }

            if (estaEnModoDash)
            {
                Vector2 posSlimeEnPantalla = Camera.main.WorldToScreenPoint(transform.position);
                Vector2 dir = (posicionInput - posSlimeEnPantalla).normalized;

                if (dir.magnitude > 0.1f)
                    dashDireccion = dir;

                Vector3 origen = transform.position;
                Vector3 destino = origen + (Vector3)(dashDireccion * dashArrowLength);
                flechaLine.SetPosition(0, origen);
                flechaLine.SetPosition(1, destino);
            }
        }
        else if (!presionandoAhora && estaEnModoDash)
        {
            EjecutarDash();
        }
        else if (!presionandoAhora)
        {
            holdTimer = 0f;
        }

        inputPresionado = presionandoAhora;
    }

    void EjecutarDash()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        flechaLine.enabled = false;
        estaEnModoDash = false;
        holdTimer = 0f;

        rb.linearVelocity = dashDireccion * dashForce;

        if (dashDireccion.x > 0.1f) direccionActualX = 1f;
        else if (dashDireccion.x < -0.1f) direccionActualX = -1f;

        dashDisponible = false;
        dashCooldownTimer = dashCooldown;
        ActualizarAura();
    }

    // ─────────────────────────────────────────────
    //  SALTO POR TAP
    // ─────────────────────────────────────────────
    void ManejarSaltoPorTap()
    {
        if (estaEnModoDash) return;

        bool realizoTap = false;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) realizoTap = true;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) realizoTap = true;

        if (realizoTap) ProcessSlimeJump();

        if (isTouchingWall && !isTouchingGround && rb.linearVelocity.y < 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
    }

    void ProcessSlimeJump()
    {
        if (jumpCount >= 3 && !isTouchingWall && !isTouchingGround) return;

        jumpCount++;

        if (isTouchingGround)
        {
            direccionActualX = (transform.position.x < 0) ? 1f : -1f;
            transform.position = new Vector3(transform.position.x + (direccionActualX * 0.25f), transform.position.y + 0.1f, transform.position.z);
            rb.linearVelocity = new Vector2(direccionActualX * baseHorizontalForce, baseVerticalForce * 1.2f);
            isTouchingGround = false;
            isTouchingWall = false;
            rb.gravityScale = originalGravity;
        }
        else if (isTouchingWall)
        {
            bool enParedReal = Mathf.Abs(transform.position.x) > 1.5f;
            if (enParedReal)
                direccionActualX = (transform.position.x < 0) ? 1f : -1f;

            transform.position = new Vector3(transform.position.x + (direccionActualX * 0.25f), transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(direccionActualX * baseHorizontalForce, baseVerticalForce);
            isTouchingWall = false;
            rb.gravityScale = originalGravity;
        }
        else
        {
            rb.linearVelocity = new Vector2(direccionActualX * baseHorizontalForce, rb.linearVelocity.y + fuerzaImpulsoAire);
        }
    }

    // ─────────────────────────────────────────────
    //  COLISIONES
    // ─────────────────────────────────────────────
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isTouchingGround = true;
            jumpCount = 0;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (estaEnModoDash)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                flechaLine.enabled = false;
                estaEnModoDash = false;
                holdTimer = 0f;
            }
            return;
        }

        if (collision.gameObject.CompareTag("Wall") && !isTouchingGround)
        {
            isTouchingWall = true;
            jumpCount = 0;
            rb.linearVelocity = Vector2.zero;
        }

        if (collision.gameObject.CompareTag("Obstacle") && !isTouchingGround)
        {
            Vector2 contactNormal = collision.contacts[0].normal;
            if (contactNormal.x > 0.3f) direccionActualX = 1f;
            else if (contactNormal.x < -0.3f) direccionActualX = -1f;

            isTouchingWall = true;
            jumpCount = 0;
            rb.linearVelocity = Vector2.zero;
        }

        if (collision.gameObject.CompareTag("Lethal"))
            RecibirDaño();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isTouchingGround = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall")) isTouchingWall = false;
        if (collision.gameObject.CompareTag("Ground")) isTouchingGround = false;
        if (collision.gameObject.CompareTag("Obstacle")) isTouchingWall = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Lethal"))
            RecibirDaño();
    }

    void VerificarLimitePantalla()
    {
        if (!estaVivo) return;

        float limiteInferior = camaraPrincipal.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).y;

        if (transform.position.y < limiteInferior - 1f)
            Morir();
    }

}