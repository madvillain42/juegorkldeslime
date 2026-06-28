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
    // Se eliminó colorActivo para usar el color por defecto del componente Image
    [SerializeField] private Color colorCooldown  = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color colorDibujando = new Color(1f, 0.8f, 0.1f);

    [Header("Configuración")]
    [SerializeField] private float cooldownDuration = 5f;
    [SerializeField] private float slowMotionScale  = 0.3f;
    [SerializeField] private float maxDrawTime      = 3f;
    [SerializeField] private float tiempoLimiteRuna = 3f;

    public bool ModoRunaActivo => modoRunaActivo;

    private bool modoRunaActivo = false;
    private bool enCooldown     = false;
    private float cooldownTimer = 0f;
    private float drawTimer     = 0f;

    private Color colorPorDefecto; // Aquí guardaremos tu color original

    private InputAction drawAction;

    // Estado del press manual
    private bool estaPresionando = false;

    void Start()
    {
        // Guardamos el color original que configuraste en el Inspector de Unity
        if (imagenBoton != null)
        {
            colorPorDefecto = imagenBoton.color;
        }

        // Solo necesitamos la posición del mouse/touch para el trazo
        drawAction = new InputAction("Draw", binding: "<Touchscreen>/primaryTouch/position");
        drawAction.AddBinding("<Mouse>/position");
        drawAction.Enable();

        // Inicializar RuneSystem con solo el drawAction
        if (runeSystem != null)
        {
            runeSystem.Init(drawAction);
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

        if (runeSystem != null)
        {
            runeSystem.OnSuccess -= OnRunaExitosa;
            runeSystem.OnFail    -= OnRunaFallida;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // Cooldown
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

            // Detectar press del click derecho (PC) o touch (móvil)
            bool presionandoAhora = false;
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
                presionandoAhora = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                presionandoAhora = true;

            // Notificar al RuneSystem cuando empieza y termina el press
            if (presionandoAhora && !estaPresionando)
            {
                estaPresionando = true;
                if (runeSystem != null) runeSystem.NotifyPressStarted();
            }
            else if (!presionandoAhora && estaPresionando)
            {
                estaPresionando = false;
                if (runeSystem != null) runeSystem.NotifyPressEnded();
            }

            // Tick para capturar puntos del trazo
            if (runeSystem != null)
                runeSystem.Tick();

            if (drawTimer >= maxDrawTime)
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

        modoRunaActivo  = true;
        estaPresionando = false;
        drawTimer       = 0f;

        Time.timeScale      = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (drawingPanel != null)
            drawingPanel.SetActive(true);

        if (runeSystem != null)
            runeSystem.StartChallenge(tiempoLimiteRuna);

        imagenBoton.color  = colorDibujando;
        boton.interactable = false;

        Debug.Log("[RuneButton] Modo runa activado");
    }

    void TerminarModoRuna()
    {
        if (!modoRunaActivo) return;
        modoRunaActivo  = false;
        estaPresionando = false;

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
        
        // Si está en cooldown o no se puede presionar (ej. tocando el suelo), se vuelve gris.
        // Si está interactable, restaura el color original que le diste en Unity.
        imagenBoton.color = enCooldown         ? colorCooldown :
                            boton.interactable ? colorPorDefecto :
                                                 colorCooldown;
    }
}