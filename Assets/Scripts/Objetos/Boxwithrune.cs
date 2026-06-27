using UnityEngine;

public enum PotionType { Damage, Shield, Speed }

public class BoxWithRune : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private RuneDefinition runaRequerida;

    [Header("Visual")]
    [SerializeField] private Color colorCaja = new Color(1f, 0.2f, 0.2f);

    private SpriteRenderer sr;
    private Collider2D col;
    private bool yaAbierta = false;
    private PotionType potionType;
    private RuneSystem runeSystem;

    void Start()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (col != null) col.isTrigger = true;

        potionType = (PotionType)Random.Range(0, 3);

        if (sr != null && runaRequerida == null)
            sr.color = colorCaja;

        runeSystem = FindFirstObjectByType<RuneSystem>();
        if (runeSystem != null)
            runeSystem.OnSuccessWithRune += OnRunaExitosa;
    }

    void OnDestroy()
    {
        if (runeSystem != null)
            runeSystem.OnSuccessWithRune -= OnRunaExitosa;
    }

    public void AsignarRuna(RuneDefinition runa, Color color, Sprite sprite)
    {
        runaRequerida = runa;
        colorCaja     = color;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.color  = Color.white; // Sin tinte
            }
            else
            {
                spriteRenderer.color = color; // Fallback si no hay sprite
            }
        }
    }

    void OnRunaExitosa(RuneDefinition runaDetectada)
    {
        if (yaAbierta) return;

        if (runaRequerida != null && runaDetectada != runaRequerida)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float distancia = Vector2.Distance(transform.position, player.transform.position);
        if (distancia > detectionRadius) return;

        AbrirCaja();
    }

    void AbrirCaja()
    {
        yaAbierta = true;

        if (GameManager.Instance != null)
        {
            switch (potionType)
            {
                case PotionType.Damage:
                    GameManager.Instance.AplicarPocionDano();
                    Debug.Log("[BoxWithRune] ⚔️ +Daño");
                    break;
                case PotionType.Shield:
                    GameManager.Instance.AplicarPocionEscudo();
                    Debug.Log("[BoxWithRune] 🛡️ Escudo");
                    break;
                case PotionType.Speed:
                    GameManager.Instance.AplicarPocionVelocidad();
                    SlimeController slime = FindFirstObjectByType<SlimeController>();
                    if (slime != null) slime.ActualizarCooldownDash();
                    Debug.Log("[BoxWithRune] ⚡ +Velocidad");
                    break;
            }
        }

        if (sr != null) sr.color = Color.green;
        Invoke(nameof(Destruir), 0.2f);
    }

    void Destruir() => Destroy(gameObject);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}