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
            // Decidimos la dirección del salto basándonos en qué mitad de la pantalla está parado.
            // Si está en la izquierda (X < 0), salta obligatoriamente a la derecha (1).
            // Si está en la derecha (X > 0), salta obligatoriamente a la izquierda (-1).
            direccionActualX = (transform.position.x < 0) ? 1f : -1f;

            // Separación manual de la pared cercana para evitar micro-choques en las esquinas inferiores
            transform.position = new Vector3(transform.position.x + (direccionActualX * 0.25f), transform.position.y + 0.1f, transform.position.z);

            rb.linearVelocity = new Vector2(direccionActualX * baseHorizontalForce, baseVerticalForce * 1.2f);
            
            // Apagamos ambos estados para limpiar el despegue
            isTouchingGround = false;
            isTouchingWall = false;
            rb.gravityScale = originalGravity;

            Debug.Log($"[Suelo Seguro] Despegue de esquina hacia X: {direccionActualX}");
        }
        else if (isTouchingWall)
        {
            // ESCALADA PURA EN LA TORRE: Miramos la posición real para decidir el lado contrario
            direccionActualX = (transform.position.x < 0) ? 1f : -1f;

            // Separación física manual para despegarlo del colisionador de la torre
            transform.position = new Vector3(transform.position.x + (direccionActualX * 0.25f), transform.position.y, transform.position.z);

            rb.linearVelocity = new Vector2(direccionActualX * baseHorizontalForce, baseVerticalForce);
            
            isTouchingWall = false;
            rb.gravityScale = originalGravity;
            
            Debug.Log($"[Torre - Salto 1] Rebote absoluto hacia X: {direccionActualX}");
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
            return; // Cortamos aquí para que no ejecute la lógica de pared si toca ambos a la vez
        }

        // Al impactar una pared (Tag: Wall) - Solo se activa si no está tocando el suelo
        if (collision.gameObject.CompareTag("Wall") && !isTouchingGround)
        {
            isTouchingWall = true;
            jumpCount = 0; 
            rb.linearVelocity = Vector2.zero; 
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
    }
}