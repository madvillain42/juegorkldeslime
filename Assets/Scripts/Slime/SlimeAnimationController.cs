using UnityEngine;

public class SlimeAnimationController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Ground Check")]
    public Transform groundCheck;        // punto vacío bajo el slime
    public LayerMask groundLayer;        // capa del suelo
    public float checkRadius = 0.1f;

    public bool isOnWall = false;
    private bool wasGrounded = false;    // para detectar el momento de aterrizaje

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Detectar suelo
        bool isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, 
            checkRadius, 
            groundLayer
        );

        bool isJumping = rb.linearVelocity.y > 0.5f;

        // Detectar momento exacto de aterrizaje
        bool justLanded = isGrounded && !wasGrounded;
        wasGrounded = isGrounded;

        anim.SetBool("IsJumping", isJumping);
        anim.SetBool("OnWall", isOnWall);
        anim.SetBool("IsGrounded", isGrounded);

        // Trigger de landing solo en el momento de tocar suelo
        if (justLanded)
            anim.SetTrigger("IsLanding");
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Wall"))
            isOnWall = true;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Wall"))
            isOnWall = false;
    }

    public void TriggerDeath()
    {
        anim.SetBool("IsDead", true);
    }
} 