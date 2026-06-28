using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LaneSystem))]
public class SlimeBossController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private LaneSystem laneSystem;

    [Header("Referencias")]
    [SerializeField] private RuneSystem runeSystem;

    [Header("Vida del Slime en Bossfight")]
    [SerializeField] private int vidasBossfight = 3;  // Aguanta 3 golpes
    [SerializeField] private float tiempoInvulnerabilidad = 1.5f;
    [SerializeField] private Color colorDaño = new Color(1f, 0.2f, 0.2f, 1f);

    private int vidasActuales;
    private bool esInvulnerable = false;
    private SpriteRenderer sr;
    private bool estaPresionando = false;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        laneSystem   = GetComponent<LaneSystem>();
        sr           = GetComponent<SpriteRenderer>();

        if (runeSystem == null)
            runeSystem = FindFirstObjectByType<RuneSystem>();
    }

    void Start()
    {
        vidasActuales = vidasBossfight;
    }

    void OnEnable()
    {
        inputActions.BossFight.Enable();
        inputActions.Player.Disable();

        var map = inputActions.BossFight;
        laneSystem.Init(map.Swipe, map.TouchPress);
        runeSystem.Init(map.DrawRune, map.TouchPress);
    }

    void OnDisable()
    {
        inputActions.BossFight.Disable();
    }

    void Update()
    {
        if (runeSystem.IsActive)
        {
            bool presionandoAhora = false;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                presionandoAhora = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                presionandoAhora = true;

            if (presionandoAhora && !estaPresionando)
            {
                estaPresionando = true;
                runeSystem.NotifyPressStarted();
            }
            else if (!presionandoAhora && estaPresionando)
            {
                estaPresionando = false;
                runeSystem.NotifyPressEnded();
            }

            runeSystem.Tick();
        }
        else
        {
            estaPresionando = false;
            laneSystem.Tick();
        }
    }

    // ─────────────────────────────────────────────
    //  DAÑO AL SLIME — proyectiles del jefe
    // ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Lethal")) return;
        RecibirDaño();
    }

    public void RecibirDaño()
    {
        if (esInvulnerable) return;

        // Verificar escudo del GameManager
        if (GameManager.Instance != null && GameManager.Instance.ConsumirEscudo())
        {
            Debug.Log("[SlimeBoss] Escudo absorbió el golpe");
            StartCoroutine(FlashDaño());
            StartCoroutine(PeriodoInvulnerabilidad());
            return;
        }

        vidasActuales--;
        Debug.Log($"[SlimeBoss] Vida restante: {vidasActuales}/{vidasBossfight}");

        if (vidasActuales <= 0)
            Morir();
        else
            StartCoroutine(PeriodoInvulnerabilidad());
    }

    void Morir()
    {
        Debug.Log("[SlimeBoss] El slime murió en la bossfight");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetStats();
            GameManager.Instance.ChangeState(GameState.GameOver);
        }
    }

    System.Collections.IEnumerator FlashDaño()
    {
        if (sr != null) sr.color = colorDaño;
        yield return new WaitForSeconds(0.15f);
        if (sr != null) sr.color = Color.white;
    }

    System.Collections.IEnumerator PeriodoInvulnerabilidad()
    {
        esInvulnerable = true;
        StartCoroutine(FlashDaño());

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
}