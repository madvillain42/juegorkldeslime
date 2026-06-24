using UnityEngine;

[RequireComponent(typeof(LaneSystem))]
[RequireComponent(typeof(RuneSystem))]
public class SlimeBossController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private LaneSystem laneSystem;
    private RuneSystem runeSystem;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        laneSystem   = GetComponent<LaneSystem>();
        runeSystem   = GetComponent<RuneSystem>();
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
            runeSystem.Tick();
        else
            laneSystem.Tick();
    }
}