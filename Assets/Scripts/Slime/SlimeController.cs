using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private float holdThreshold = 0.3f;       // Segundos para activar el modo dash
    [SerializeField] private float slowMotionScale = 0.3f;     // Qué tan lento se pone el tiempo
    [SerializeField] private float dashArrowLength = 1.5f;     // Largo visual de la flecha
    [SerializeField] private Color auraColorReady = new Color(1f, 0.9f, 0.2f, 1f);   // Amarillo = dash listo
    [SerializeField] private Color auraColorCooldown = new Color(0.3f, 0.8f, 1f, 1f); // Azul = normal

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
    private bool estaEnModoDash = false;       // Hold activo esperando dirección
    private float holdTimer = 0f;              // Cuánto lleva presionado
    private bool inputPresionado = false;
    private Vector2 dashDireccion = Vector2.right;

    // --- Flecha de dirección ---
    private LineRenderer flechaLine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalGravity = rb.gravityScale;
        transform.localScale = tamañoSlime;

        // Crear LineRenderer para la flecha del dash
        flechaLine = gameObject.AddComponent<LineRenderer>();
        flechaLine.positionCount = 2;
        flechaLine.startWidth = 0.08f;
        flechaLine.endWidth = 0.2f;
        flechaLine.material = new Material(Shader.Find("Sprites/Default"));
        flechaLine.startColor = new Color(1f, 1f, 0.2f, 0.8f);
        flechaLine.endColor = new Color(1f, 0.4f, 0f, 0.9f);
        flechaLine.enabled = false;

        ActualizarAura();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Climbing) return;

        ManejarCooldownDash();
        ManejarInputDash();
        ManejarSaltoPorTap();

        // Deslizamiento en pared
        if (isTouchingWall && !isTouchingGround && rb.linearVelocity.y < 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
    }

    // ─────────────────────────────────────────────
    //  COOLDOWN Y AURA
    // ─────────────────────────────────────────────
    void ManejarCooldownDash()
    {
        if (!dashDisponible)
        {
            dashCooldownTimer -= Time.unscaledDeltaTime; // Usa unscaled para que cuente aunque haya slow motion
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
    //  INPUT SEPARADO: HOLD = DASH / TAP = SALTO
    // ─────────────────────────────────────────────
    void ManejarInputDash()
    {
        // Detectar presión y posición del input
        bool presionandoAhora = false;
        Vector2 posicionInput = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            presionandoAhora = true;
            posicionInput = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            // Click derecho en PC para testear el dash
            presionandoAhora = true;
            posicionInput = Mouse.current.position.ReadValue();
        }

        // Solo el dash funciona en el aire
        bool enElAire = !isTouchingWall && !isTouchingGround;

        if (presionandoAhora && enElAire && dashDisponible)
        {
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdThreshold && !estaEnModoDash)
            {
                // Activar modo dash
                estaEnModoDash = true;
                Time.timeScale = slowMotionScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                flechaLine.enabled = true;
            }

            if (estaEnModoDash)
            {
                // Calcular dirección desde el slime hacia donde apunta el dedo
                Vector2 posSlimeEnPantalla = Camera.main.WorldToScreenPoint(transform.position);
                Vector2 dir = (posicionInput - posSlimeEnPantalla).normalized;

                if (dir.magnitude > 0.1f)
                    dashDireccion = dir;

                // Dibujar flecha
                Vector3 origen = transform.position;
                Vector3 destino = origen + (Vector3)(dashDireccion * dashArrowLength);
                flechaLine.SetPosition(0, origen);
                flechaLine.SetPosition(1, destino);
            }
        }
        else if (!presionandoAhora && estaEnModoDash)
        {
            // Soltó → ejecutar dash
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
        // Restaurar tiempo
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        flechaLine.enabled = false;
        estaEnModoDash = false;
        holdTimer = 0f;

        // Aplicar impulso en la dirección elegida
        rb.linearVelocity = dashDireccion * dashForce;

        // Actualizar direccionActualX para que los saltos posteriores sean consistentes
        if (dashDireccion.x > 0.1f) direccionActualX = 1f;
        else if (dashDireccion.x < -0.1f) direccionActualX = -1f;

        // Iniciar cooldown
        dashDisponible = false;
        dashCooldownTimer = dashCooldown;
        ActualizarAura();

        Debug.Log($"[Dash] Dirección: {dashDireccion} | Fuerza: {dashForce}");
    }

    // ─────────────────────────────────────────────
    //  SALTO POR TAP (solo si NO está en modo dash)
    // ─────────────────────────────────────────────
    void ManejarSaltoPorTap()
    {
        if (estaEnModoDash) return; // El modo dash bloquea el salto

        bool realizoTap = false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) realizoTap = true;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) realizoTap = true;

        if (realizoTap) ProcessSlimeJump();

        // Deslizamiento en pared
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
            Debug.Log($"[Suelo Seguro] Despegue hacia X: {direccionActualX}");
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
            Debug.Log($"[Torre - Salto 1] Rebote hacia X: {direccionActualX}");
        }
        else
        {
            rb.linearVelocity = new Vector2(direccionActualX * baseHorizontalForce, rb.linearVelocity.y + fuerzaImpulsoAire);
            Debug.Log($"[Torre - Salto {jumpCount}] Boost en el aire hacia X: {direccionActualX}");
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

            // Si estaba en modo dash, cancelarlo
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
            Debug.Log($"[Obstáculo] Normal: {contactNormal} → Nueva dirección: {direccionActualX}");
        }
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
}