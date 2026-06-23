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

    private Rigidbody2D rb;
    private int jumpCount = 0;
    private bool isTouchingWall = false;
    private bool isTouchingGround = false;
    private float originalGravity;

    // -1 = Izquierda, 1 = Derecha. Dirección del viaje actual del Slime
    private float direccionActualX = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
        transform.localScale = tamañoSlime;
    }

    void Update()
    {
        // 1. Validar estado de escalada
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Climbing) return;

        // 2. DETECCIÓN PURA DE TAP
        bool realizoTap = false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) realizoTap = true;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) realizoTap = true;

        // 3. Ejecutar la lógica
        if (realizoTap)
        {
            ProcessSlimeJump();
        }

        // 4. Mecánica de deslizamiento rápido en la pared (SOLO si no está tocando el suelo)
        if (isTouchingWall && !isTouchingGround && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    void ProcessSlimeJump()
    {
        // Bloqueo Anti-Spam en el aire si ya quemó sus 3 saltos
        if (jumpCount >= 3 && !isTouchingWall && !isTouchingGround) return;

        jumpCount++;

        // REGLA DE ORO: Si toca el suelo, ignoramos por completo el estado de la pared.
        if (isTouchingGround)
        {
            direccionActualX = (transform.position.x < 0) ? 1f : -1f;

            transform.position = new Vector3(transform.position.x + (direccionActualX * 0.25f), transform.position.y + 0.1f, transform.position.z);

            rb.linearVelocity = new Vector2(direccionActualX * baseHorizontalForce, baseVerticalForce * 1.2f);

            isTouchingGround = false;
            isTouchingWall = false;
            rb.gravityScale = originalGravity;

            Debug.Log($"[Suelo Seguro] Despegue de esquina hacia X: {direccionActualX}");
        }
        else if (isTouchingWall)
        {
            // Si está tocando un obstáculo, direccionActualX ya fue corregida
            // en OnCollisionEnter2D, así que la respetamos directamente
            // Solo recalculamos por posición si viene de una pared real (X extremo)
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
            // BOOST EN EL AIRE (Saltos 2 y 3)
            rb.linearVelocity = new Vector2(direccionActualX * baseHorizontalForce, rb.linearVelocity.y + fuerzaImpulsoAire);
            Debug.Log($"[Torre - Salto {jumpCount}] Boost en el aire hacia X: {direccionActualX}");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Al caer en el suelo (Tag: Ground) - Manda sobre la pared
        if (collision.gameObject.CompareTag("Ground"))
        {
            isTouchingGround = true;
            jumpCount = 0;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Al impactar una pared (Tag: Wall) - Solo se activa si no está tocando el suelo
        if (collision.gameObject.CompareTag("Wall") && !isTouchingGround)
        {
            isTouchingWall = true;
            jumpCount = 0;
            rb.linearVelocity = Vector2.zero;
        }

        // Al impactar un obstáculo - detecta el lado del choque y corrige la dirección
        if (collision.gameObject.CompareTag("Obstacle") && !isTouchingGround)
        {
            Vector2 contactNormal = collision.contacts[0].normal;

            if (contactNormal.x > 0.3f)
                direccionActualX = 1f;   // Chocó por la izquierda del obstáculo → rebota a la derecha
            else if (contactNormal.x < -0.3f)
                direccionActualX = -1f;  // Chocó por la derecha del obstáculo → rebota a la izquierda
            // Si la normal es vertical (cayó encima) → mantiene la dirección actual

            isTouchingWall = true;
            jumpCount = 0;
            rb.linearVelocity = Vector2.zero;

            Debug.Log($"[Obstáculo] Normal: {contactNormal} → Nueva dirección: {direccionActualX}");
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Asegurar que el suelo mantenga el control si el personaje se queda quieto en la esquina
        if (collision.gameObject.CompareTag("Ground"))
        {
            isTouchingGround = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = false;
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            isTouchingGround = false;
        }
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            isTouchingWall = false;
        }
    }
}