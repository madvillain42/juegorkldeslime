using UnityEngine;

public enum PotionType { Damage, Shield, Speed }

public class BoxWithRune : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private RuneDefinition runaRequerida;

    [Header("Visual")]
    [SerializeField] private Color colorCaja = new Color(0.2f, 0.8f, 1f);

    private SpriteRenderer sr;
    private bool yaAbierta = false;
    private PotionType potionType;
    private RuneSystem runeSystem;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        potionType = (PotionType)Random.Range(0, 3);

        // Aplicar color solo si no fue asignado por el spawner
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

    // Llamado por el ObstacleSpawner al instanciar la caja
    public void AsignarRuna(RuneDefinition runa, Color color)
    {
        runaRequerida = runa;
        colorCaja     = color;

        // Aplicar color inmediatamente
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    void OnRunaExitosa(RuneDefinition runaDetectada)
    {
        if (yaAbierta) return;

        if (runaRequerida != null && runaDetectada != runaRequerida)
        {
            Debug.Log($"[BoxWithRune] Runa incorrecta — Necesitaba: {runaRequerida.runeName} | Recibió: {runaDetectada.runeName}");
            return;
        }

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
                    MostrarMensaje("⚔️ +Daño");
                    break;
                case PotionType.Shield:
                    GameManager.Instance.AplicarPocionEscudo();
                    MostrarMensaje("🛡️ Escudo");
                    break;
                case PotionType.Speed:
                    GameManager.Instance.AplicarPocionVelocidad();
                    SlimeController slime = FindFirstObjectByType<SlimeController>();
                    if (slime != null) slime.ActualizarCooldownDash();
                    MostrarMensaje("⚡ +Velocidad");
                    break;
            }
        }

        if (sr != null) sr.color = Color.green;
        Invoke(nameof(Destruir), 0.2f);
    }

    void MostrarMensaje(string texto)
    {
        Debug.Log($"[BoxWithRune] Caja abierta — {texto} | Poción: {potionType}");
    }

    void Destruir() => Destroy(gameObject);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}