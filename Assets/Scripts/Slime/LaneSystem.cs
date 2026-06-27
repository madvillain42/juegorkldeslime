using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class LaneSystem : MonoBehaviour
{
    [Header("Carriles")]
    public Transform carrilIzq;
    public Transform carrilMid;
    public Transform carrilDer;
    public float slideSpeed = 12f;

    [Header("Swipe")]
    public float swipeThreshold = 60f;

    private Transform[] lanes;
    private int currentLane = 1;
    private bool isSliding = false;
    private float swipeAccumX = 0f;
    private bool swipeLocked = false;

    private InputAction swipeAction;
    private InputAction touchPressAction;

    public int CurrentLane => currentLane;
    public float[] PositionsX => new float[]
    {
        carrilIzq.position.x,
        carrilMid.position.x,
        carrilDer.position.x
    };

    public void Init(InputAction swipe, InputAction press)
    {
        swipeAction      = swipe;
        touchPressAction = press;

        lanes = new Transform[] { carrilIzq, carrilMid, carrilDer };

        for (int i = 0; i < lanes.Length; i++)
            if (lanes[i] == null)
                Debug.LogError($"LaneSystem: carril {i} no asignado.");

        SnapToLane(1);
    }

    public void Tick()
    {
        // Solo acumular delta si el dedo/click está presionado
        if (!touchPressAction.IsPressed())
        {
            swipeAccumX = 0f;
            swipeLocked = false;
            return;
        }

        Vector2 delta = swipeAction.ReadValue<Vector2>();
        swipeAccumX += delta.x;

        if (!swipeLocked && Mathf.Abs(swipeAccumX) >= swipeThreshold)
        {
            int dir = swipeAccumX > 0 ? 1 : -1;
            TryMoveToLane(currentLane + dir);
            swipeLocked = true;
        }
    }

    void TryMoveToLane(int target)
    {
        if (isSliding) return;
        if (target < 0 || target >= lanes.Length) return;
        currentLane = target;
        StartCoroutine(SlideTo(lanes[target].position.x));
    }

    void SnapToLane(int lane)
    {
        currentLane = lane;
        transform.position = new Vector3(lanes[lane].position.x, transform.position.y, transform.position.z);
    }

    IEnumerator SlideTo(float targetX)
    {
        isSliding = true;
        while (Mathf.Abs(transform.position.x - targetX) > 0.01f)
        {
            float newX = Mathf.MoveTowards(transform.position.x, targetX, slideSpeed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            yield return null;
        }
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        isSliding = false;
    }
}