using UnityEngine;

public enum PotionType { Damage, Shield, Speed }

public class BoxWithRune : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private RuneDefinition runaRequerida;

    [Header("Visual")]
    [SerializeField] private Color colorCaja = new Color(1f, 0.2f, 0.2f);

    [Header("Efectos Visuales (Feedback)")]
    [SerializeField] private GameObject prefabEfectoViento;
    [SerializeField] private GameObject prefabIconoFlotante;
    [SerializeField] private Sprite spriteDaño;
    [SerializeField] private Sprite spriteEscudo;
    [SerializeField] private Sprite spriteVelocidad;

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

        // 1. Instanciar el efecto de Viento en la posición de la poción
        if (prefabEfectoViento != null)
        {
            Instantiate(prefabEfectoViento, transform.position, Quaternion.identity);
        }

        Sprite iconoMostrar = null;

        if (GameManager.Instance != null)
        {
            switch (potionType)
            {
                case PotionType.Damage:
                    GameManager.Instance.AplicarPocionDano();
                    iconoMostrar = spriteDaño;
                    Debug.Log("[BoxWithRune] ⚔️ +Daño");
                    break;
                case PotionType.Shield:
                    GameManager.Instance.AplicarPocionEscudo();
                    iconoMostrar = spriteEscudo;
                    Debug.Log("[BoxWithRune] 🛡️ Escudo");
                    break;
                case PotionType.Speed:
                    GameManager.Instance.AplicarPocionVelocidad();
                    SlimeController slime = FindFirstObjectByType<SlimeController>();
                    if (slime != null) slime.ActualizarCooldownDash();
                    iconoMostrar = spriteVelocidad;
                    Debug.Log("[BoxWithRune] ⚡ +Velocidad");
                    break;
            }
        }

        // 2. Instanciar el Icono Flotante sobre el jugador
        if (prefabIconoFlotante != null && iconoMostrar != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 posicionIcono = player.transform.position + new Vector3(0, 1f, 0);
                GameObject nuevoIcono = Instantiate(prefabIconoFlotante, posicionIcono, Quaternion.identity);
                
                EfectoIconoFlotante efecto = nuevoIcono.GetComponent<EfectoIconoFlotante>();
                if (efecto != null) efecto.IniciarEfecto(iconoMostrar);
            }
        }

        // 3. Destruimos la caja de inmediato para que el viento sea el protagonista visual
        Destruir();
    }

    void Destruir() => Destroy(gameObject);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}