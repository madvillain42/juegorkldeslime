using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class SlimeBossController : MonoBehaviour
{
    // ─── Carriles ─────────────────────────────────────────────────────────────
    [Header("Carriles")]
    [Tooltip("Arrastra aquí los 3 GameObjects de carril en orden: Izq, Mid, Der")]
    public Transform carrilIzq;
    public Transform carrilMid;
    public Transform carrilDer;

    [Tooltip("Velocidad de deslizamiento entre carriles")]
    public float laneSlideSpeed = 12f;

    // ─── Swipe ────────────────────────────────────────────────────────────────
    [Header("Swipe")]
    public float swipeThreshold = 60f;

    // ─── Runas ────────────────────────────────────────────────────────────────
    [Header("Sistema de Runas")]
    public GameObject runePanel;
    public UnityEngine.UI.Image runeTargetImage;
    public TMPro.TextMeshProUGUI timerText;
    public LineRenderer traceRenderer;
    public RuneDefinition[] availableRunes;
    [Range(0.4f, 0.95f)]
    public float matchThreshold = 0.65f;
    public int segmentCount = 8;

    // ─── Eventos ──────────────────────────────────────────────────────────────
    public event System.Action OnRuneSuccess;
    public event System.Action OnRuneFail;
    public bool IsRuneActive { get; private set; } = false;

    // ─── Estado interno ───────────────────────────────────────────────────────
    private Transform[] lanes;
    private int currentLane = 1;
    private bool isSliding = false;

    private float swipeAccumX = 0f;
    private bool swipeLocked = false;

    private List<Vector2> drawnPoints = new List<Vector2>();
    private bool isDrawingTrace = false;
    private Coroutine runeTimerCoroutine;
    private RuneDefinition currentRune;

    // ─── Input ────────────────────────────────────────────────────────────────
    private InputSystem_Actions inputActions;
    private InputAction swipeAction;
    private InputAction drawRuneAction;
    private InputAction touchPressAction;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputActions.BossFight.Enable();
        inputActions.Player.Disable();

        swipeAction      = inputActions.BossFight.Swipe;
        drawRuneAction   = inputActions.BossFight.DrawRune;
        touchPressAction = inputActions.BossFight.TouchPress;

        touchPressAction.started  += OnTouchStarted;
        touchPressAction.canceled += OnTouchEnded;
    }

    void OnDisable()
    {
        touchPressAction.started  -= OnTouchStarted;
        touchPressAction.canceled -= OnTouchEnded;
        inputActions.BossFight.Disable();
    }

    void Start()
    {
        // Construir array de carriles desde las referencias
        lanes = new Transform[] { carrilIzq, carrilMid, carrilDer };

        // Validar que estén asignados
        for (int i = 0; i < lanes.Length; i++)
        {
            if (lanes[i] == null)
            {
                Debug.LogError($"SlimeBossController: carril {i} no asignado en el inspector.");
                return;
            }
        }

        // Snap al carril central al inicio
        SnapToLane(1);

        if (runePanel != null) runePanel.SetActive(false);
    }

    void Update()
    {
        if (IsRuneActive)
            HandleRuneDrawing();
        else
            HandleSwipe();
    }

    // ─── Carriles ─────────────────────────────────────────────────────────────

    void HandleSwipe()
    {
        Vector2 delta = swipeAction.ReadValue<Vector2>();
        swipeAccumX += delta.x;

        if (!swipeLocked && Mathf.Abs(swipeAccumX) >= swipeThreshold)
        {
            int dir = swipeAccumX > 0 ? 1 : -1;
            TryMoveToLane(currentLane + dir);
            swipeLocked = true;
        }

        if (!touchPressAction.IsPressed())
        {
            swipeAccumX = 0f;
            swipeLocked = false;
        }
    }

    void TryMoveToLane(int target)
    {
        if (isSliding) return;
        if (target < 0 || target >= lanes.Length) return;
        currentLane = target;
        StartCoroutine(SlideToLane(lanes[target].position.x));
    }

    void SnapToLane(int lane)
    {
        currentLane = lane;
        transform.position = new Vector3(lanes[lane].position.x, transform.position.y, transform.position.z);
    }

    IEnumerator SlideToLane(float targetX)
    {
        isSliding = true;
        while (Mathf.Abs(transform.position.x - targetX) > 0.01f)
        {
            float newX = Mathf.MoveTowards(transform.position.x, targetX, laneSlideSpeed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            yield return null;
        }
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        isSliding = false;
    }

    public int CurrentLane => currentLane;
    public float[] LanePositionsX => new float[]
    {
        carrilIzq.position.x,
        carrilMid.position.x,
        carrilDer.position.x
    };

    // ─── Runas ────────────────────────────────────────────────────────────────

    public void StartRuneChallenge(float timeLimit)
    {
        if (availableRunes == null || availableRunes.Length == 0) return;

        currentRune = availableRunes[Random.Range(0, availableRunes.Length)];
        drawnPoints.Clear();
        IsRuneActive = true;

        if (runePanel != null) runePanel.SetActive(true);
        if (runeTargetImage != null && currentRune.displaySprite != null)
            runeTargetImage.sprite = currentRune.displaySprite;
        if (traceRenderer != null) { traceRenderer.positionCount = 0; traceRenderer.enabled = true; }

        runeTimerCoroutine = StartCoroutine(RuneTimer(timeLimit));
    }

    void OnTouchStarted(InputAction.CallbackContext ctx)
    {
        if (!IsRuneActive) return;
        drawnPoints.Clear();
        isDrawingTrace = true;
        if (traceRenderer != null) traceRenderer.positionCount = 0;
    }

    void OnTouchEnded(InputAction.CallbackContext ctx)
    {
        if (!IsRuneActive || !isDrawingTrace) return;
        isDrawingTrace = false;
        EvaluateRune();
    }

    void HandleRuneDrawing()
    {
        if (!isDrawingTrace) return;
        Vector2 screenPos = drawRuneAction.ReadValue<Vector2>();
        AddDrawPoint(screenPos);
    }

    void AddDrawPoint(Vector2 screenPos)
    {
        if (drawnPoints.Count > 0 &&
            Vector2.Distance(drawnPoints[drawnPoints.Count - 1], screenPos) < 5f) return;

        drawnPoints.Add(screenPos);

        if (traceRenderer != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            traceRenderer.positionCount = drawnPoints.Count;
            traceRenderer.SetPosition(drawnPoints.Count - 1, worldPos);
        }
    }

    void EvaluateRune()
    {
        if (drawnPoints.Count < 5) { ResolveFail(); return; }
        float score = CompareTrace(drawnPoints, currentRune.templatePoints);
        Debug.Log($"[Runa] {currentRune.runeName} | Score: {score:F2} | Umbral: {matchThreshold}");
        if (score >= matchThreshold) ResolveSuccess();
        else ResolveFail();
    }

    float CompareTrace(List<Vector2> drawn, Vector2[] template)
    {
        if (template == null || template.Length < 2) return 0f;
        Vector2[] normDrawn    = ResampleTrace(drawn, segmentCount + 1);
        Vector2[] normTemplate = ResampleTrace(new List<Vector2>(template), segmentCount + 1);
        float[] angDrawn    = GetAngles(normDrawn);
        float[] angTemplate = GetAngles(normTemplate);
        int matches = 0;
        for (int i = 0; i < angDrawn.Length; i++)
            if (Mathf.Abs(Mathf.DeltaAngle(angDrawn[i], angTemplate[i])) <= 45f) matches++;
        return (float)matches / angDrawn.Length;
    }

    Vector2[] ResampleTrace(List<Vector2> pts, int count)
    {
        float totalLen = 0f;
        for (int i = 1; i < pts.Count; i++) totalLen += Vector2.Distance(pts[i - 1], pts[i]);
        float interval = totalLen / (count - 1);
        List<Vector2> result = new List<Vector2> { pts[0] };
        float accumulated = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            float segLen = Vector2.Distance(pts[i - 1], pts[i]);
            while (accumulated + segLen >= interval && result.Count < count)
            {
                float t = (interval - accumulated) / segLen;
                Vector2 np = Vector2.Lerp(pts[i - 1], pts[i], t);
                result.Add(np);
                pts.Insert(i, np);
                segLen = Vector2.Distance(pts[i - 1], pts[i]);
                accumulated = 0f;
            }
            accumulated += segLen;
        }
        while (result.Count < count) result.Add(pts[pts.Count - 1]);
        return result.ToArray();
    }

    float[] GetAngles(Vector2[] pts)
    {
        float[] angles = new float[pts.Length - 1];
        for (int i = 0; i < angles.Length; i++)
        {
            Vector2 d = pts[i + 1] - pts[i];
            angles[i] = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }
        return angles;
    }

    void ResolveSuccess() { EndRune(); OnRuneSuccess?.Invoke(); }
    void ResolveFail()    { EndRune(); OnRuneFail?.Invoke(); }

    void EndRune()
    {
        if (runeTimerCoroutine != null) StopCoroutine(runeTimerCoroutine);
        IsRuneActive = false;
        isDrawingTrace = false;
        drawnPoints.Clear();
        if (runePanel != null) runePanel.SetActive(false);
        if (traceRenderer != null) traceRenderer.enabled = false;
    }

    IEnumerator RuneTimer(float timeLimit)
    {
        float elapsed = 0f;
        while (elapsed < timeLimit)
        {
            elapsed += Time.deltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(timeLimit - elapsed).ToString();
            yield return null;
        }
        if (IsRuneActive) { isDrawingTrace = false; ResolveFail(); }
    }
}