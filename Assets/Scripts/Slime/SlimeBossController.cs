using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LaneSystem))]
public class SlimeBossController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private LaneSystem laneSystem;

    [Header("Referencias")]
    [SerializeField] private RuneSystem runeSystem;

    // Estado del press para el dibujo — igual que RuneButton
    private bool estaPresionando = false;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        laneSystem   = GetComponent<LaneSystem>();

        if (runeSystem == null)
            runeSystem = FindFirstObjectByType<RuneSystem>();
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
            // Detectar press — igual que RuneButton
            // PC: click izquierdo | Móvil: touch
            bool presionandoAhora = false;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                presionandoAhora = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                presionandoAhora = true;

            // Notificar inicio y fin del trazo
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

            // Capturar puntos del trazo
            runeSystem.Tick();
        }
        else
        {
            estaPresionando = false;
            laneSystem.Tick();
        }
    }
}