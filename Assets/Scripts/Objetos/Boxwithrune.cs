using UnityEngine;

public enum PotionType { Damage, Shield, Speed }

public class BoxWithRune : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float detectionRadius = 8f; // Distancia máxima para abrir la caja

    [Header("Visual")]
    [SerializeField] private Color colorCaja = new Color(0.2f, 0.8f, 1f); // Celeste = línea horizontal

    private SpriteRenderer sr;
    private bool yaAbierta = false;
    private PotionType potionType;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Asignar poción aleatoria al spawnear
        potionType = (PotionType)Random.Range(0, 3);

        // Color de la caja indica la runa requerida
        if (sr != null) sr.color = colorCaja;

        // Suscribirse al evento del RuneSystem
        RuneSystem runeSystem = FindFirstObjectByType<RuneSystem>();
        if (runeSystem != null)
            runeSystem.OnSuccess += OnRunaExitosa;
    }

    void OnDestroy()
    {
        RuneSystem runeSystem = FindFirstObjectByType<RuneSystem>();
        if (runeSystem != null)
            runeSystem.OnSuccess -= OnRunaExitosa;
    }

    void OnRunaExitosa()
    {
        if (yaAbierta) return;

        // Verificar distancia al jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float distancia = Vector2.Distance(transform.position, player.transform.position);
        if (distancia > detectionRadius) return;

        AbrirCaja();
    }

    void AbrirCaja()
    {
        yaAbierta = true;

        // Aplicar efecto de la poción al GameManager
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
                    // Notificar al SlimeController para actualizar el cooldown del dash
                    SlimeController slime = FindFirstObjectByType<SlimeController>();
                    if (slime != null) slime.ActualizarCooldownDash();
                    MostrarMensaje("⚡ +Velocidad");
                    break;
            }
        }

        // Flash verde y destruir
        if (sr != null) sr.color = Color.green;
        Invoke(nameof(Destruir), 0.2f);
    }

    void MostrarMensaje(string texto)
    {
        Debug.Log($"[BoxWithRune] Caja abierta — {texto} | Poción: {potionType}");
    }

    void Destruir()
    {
        Destroy(gameObject);
    }

    // Dibujar el radio de detección en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}