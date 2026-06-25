using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RuneButton : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Button boton;
    [SerializeField] private Image imagenBoton;
    [SerializeField] private GameObject drawingPanel;
    [SerializeField] private RuneSystem runeSystem;

    [Header("Colores del Botón")]
    [SerializeField] private Color colorActivo    = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color colorCooldown  = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color colorDibujando = new Color(1f, 0.8f, 0.1f);

    [Header("Configuración")]
    [SerializeField] private float cooldownDuration = 5f;
    [SerializeField] private float slowMotionScale  = 0.3f;
    [SerializeField] private float maxDrawTime      = 3f;
    [SerializeField] private float tiempoLimiteRuna = 3f;
    [SerializeField] private float delayAntesDeCapturar = 0.15f; // Espera antes de activar el RuneSystem

    public bool ModoRunaActivo => modoRunaActivo;

    private bool modoRunaActivo = false;
    private bool enCooldown     = false;
    private float cooldownTimer = 0f;
    private float drawTimer     = 0f;
    private bool runeSystemIniciado = false; // Controla si ya se inició el challenge

    private InputAction drawAction;
    private InputAction pressAction;

    void Start()
    {
        drawAction  = new InputAction("Draw",  binding: "<Touchscreen>/primaryTouch/position");
        pressAction = new InputAction("Press", binding: "<Touchscreen>/primaryTouch/press");

        drawAction.AddBinding("<Mouse>/position");
        pressAction.AddBinding("<Mouse>/rightButton");

        drawAction.Enable();
        pressAction.Enable();

        if (runeSystem != null)
        {
            runeSystem.Init(drawAction, pressAction);
            runeSystem.OnSuccess += OnRunaExitosa;
            runeSystem.OnFail    += OnRunaFallida;
        }

        boton.interactable = false;
        ActualizarColorBoton();

        if (drawingPanel != null)
            drawingPanel.SetActive(false);

        boton.onClick.AddListener(ActivarModoRuna);
    }

    void OnDestroy()
    {
        drawAction?.Disable();
        pressAction?.Disable();

        if (runeSystem != null)
        {
            runeSystem.OnSuccess -= OnRunaExitosa;
            runeSystem.OnFail    -= OnRunaFallida;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (enCooldown)
        {
            cooldownTimer -= Time.unscaledDeltaTime;
            if (cooldownTimer <= 0f)
            {
                enCooldown = false;
                ActualizarColorBoton();
            }
        }

        if (modoRunaActivo)
        {
            drawTimer += Time.unscaledDeltaTime;

            // Esperar el delay antes de iniciar el challenge para evitar
            // que el click del botón dispare el pressAction inmediatamente
            if (!runeSystemIniciado && drawTimer >= delayAntesDeCapturar)
            {
                runeSystemIniciado = true;
                if (runeSystem != null)
                    runeSystem.StartChallenge(tiempoLimiteRuna);

                Debug.Log("[RuneButton] RuneSystem challenge iniciado");
            }

            if (runeSystem != null && runeSystemIniciado)
                runeSystem.Tick();

            if (drawTimer >= maxDrawTime + delayAntesDeCapturar)
                TerminarModoRuna();
        }
    }

    public void SetBotonDisponible(bool disponible)
    {
        if (enCooldown || modoRunaActivo) return;
        boton.interactable = disponible;
        ActualizarColorBoton();
    }

    void ActivarModoRuna()
    {
        if (enCooldown || modoRunaActivo) return;

        modoRunaActivo     = true;
        runeSystemIniciado = false; // Reset — esperará el delay
        drawTimer          = 0f;

        Time.timeScale      = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (drawingPanel != null)
            drawingPanel.SetActive(true);

        imagenBoton.color  = colorDibujando;
        boton.interactable = false;

        Debug.Log("[RuneButton] Modo runa activado — esperando delay...");
    }

    void TerminarModoRuna()
    {
        if (!modoRunaActivo) return;
        modoRunaActivo     = false;
        runeSystemIniciado = false;

        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (drawingPanel != null)
            drawingPanel.SetActive(false);

        IniciarCooldown();
        Debug.Log("[RuneButton] Modo runa terminado");
    }

    void OnRunaExitosa()
    {
        Debug.Log("[RuneButton] ¡Runa exitosa!");
        TerminarModoRuna();
    }

    void OnRunaFallida()
    {
        Debug.Log("[RuneButton] Runa fallida");
        TerminarModoRuna();
    }

    void IniciarCooldown()
    {
        enCooldown         = true;
        cooldownTimer      = cooldownDuration;
        boton.interactable = false;
        ActualizarColorBoton();
    }

    void ActualizarColorBoton()
    {
        if (imagenBoton == null) return;
        imagenBoton.color = enCooldown        ? colorCooldown :
                            boton.interactable ? colorActivo   :
                                                 colorCooldown;
    }
}