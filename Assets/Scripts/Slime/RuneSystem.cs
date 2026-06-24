using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class RuneSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject runePanel;
    public UnityEngine.UI.Image runeTargetImage;
    public TMPro.TextMeshProUGUI timerText;
    public LineRenderer traceRenderer;

    [Header("Runas")]
    public RuneDefinition[] availableRunes;
    [Range(0.4f, 0.95f)]
    public float matchThreshold = 0.65f;
    public int segmentCount = 8;

    public event System.Action OnSuccess;
    public event System.Action OnFail;
    public bool IsActive { get; private set; } = false;

    private RuneDefinition currentRune;
    private List<Vector2> drawnPoints = new List<Vector2>();
    private bool isDrawing = false;
    private Coroutine timerCoroutine;

    private InputAction drawAction;
    private InputAction pressAction;

    public void Init(InputAction draw, InputAction press)
    {
        drawAction  = draw;
        pressAction = press;

        pressAction.started  += OnPressStarted;
        pressAction.canceled += OnPressEnded;

        if (runePanel != null) runePanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (pressAction != null)
        {
            pressAction.started  -= OnPressStarted;
            pressAction.canceled -= OnPressEnded;
        }
    }

    public void StartChallenge(float timeLimit)
    {
        if (availableRunes == null || availableRunes.Length == 0) return;

        currentRune = availableRunes[Random.Range(0, availableRunes.Length)];
        drawnPoints.Clear();
        IsActive = true;

        if (runePanel != null) runePanel.SetActive(true);
        if (runeTargetImage != null && currentRune.displaySprite != null)
            runeTargetImage.sprite = currentRune.displaySprite;
        if (traceRenderer != null) { traceRenderer.positionCount = 0; traceRenderer.enabled = true; }

        timerCoroutine = StartCoroutine(Timer(timeLimit));
    }

    public void Tick()
    {
        if (!isDrawing) return;

        Vector2 screenPos = drawAction.ReadValue<Vector2>();

        if (drawnPoints.Count > 0 &&
            Vector2.Distance(drawnPoints[drawnPoints.Count - 1], screenPos) < 5f) return;

        drawnPoints.Add(screenPos);

        if (traceRenderer != null)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            traceRenderer.positionCount = drawnPoints.Count;
            traceRenderer.SetPosition(drawnPoints.Count - 1, world);
        }
    }

    void OnPressStarted(InputAction.CallbackContext ctx)
    {
        if (!IsActive) return;
        drawnPoints.Clear();
        isDrawing = true;
        if (traceRenderer != null) traceRenderer.positionCount = 0;
    }

    void OnPressEnded(InputAction.CallbackContext ctx)
    {
        if (!IsActive || !isDrawing) return;
        isDrawing = false;
        Evaluate();
    }

    void Evaluate()
    {
        if (drawnPoints.Count < 5) { Fail(); return; }

        float score = Compare(drawnPoints, currentRune.templatePoints);
        Debug.Log($"[Runa] {currentRune.runeName} | Score: {score:F2}");

        if (score >= matchThreshold) Success();
        else Fail();
    }

    void Success() { End(); OnSuccess?.Invoke(); }
    void Fail()    { End(); OnFail?.Invoke(); }

    void End()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        IsActive  = false;
        isDrawing = false;
        drawnPoints.Clear();
        if (runePanel != null) runePanel.SetActive(false);
        if (traceRenderer != null) traceRenderer.enabled = false;
    }

    IEnumerator Timer(float limit)
    {
        float elapsed = 0f;
        while (elapsed < limit)
        {
            elapsed += Time.deltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(limit - elapsed).ToString();
            yield return null;
        }
        if (IsActive) { isDrawing = false; Fail(); }
    }

    // ─── Comparación de trazos ────────────────────────────────────────────────

    float Compare(List<Vector2> drawn, Vector2[] template)
    {
        if (template == null || template.Length < 2) return 0f;
        var a = Resample(drawn, segmentCount + 1);
        var b = Resample(new List<Vector2>(template), segmentCount + 1);
        var angA = Angles(a);
        var angB = Angles(b);
        int matches = 0;
        for (int i = 0; i < angA.Length; i++)
            if (Mathf.Abs(Mathf.DeltaAngle(angA[i], angB[i])) <= 45f) matches++;
        return (float)matches / angA.Length;
    }

    Vector2[] Resample(List<Vector2> pts, int count)
    {
        float total = 0f;
        for (int i = 1; i < pts.Count; i++) total += Vector2.Distance(pts[i-1], pts[i]);
        float interval = total / (count - 1);
        var result = new List<Vector2> { pts[0] };
        float acc = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            float seg = Vector2.Distance(pts[i-1], pts[i]);
            while (acc + seg >= interval && result.Count < count)
            {
                float t = (interval - acc) / seg;
                Vector2 np = Vector2.Lerp(pts[i-1], pts[i], t);
                result.Add(np);
                pts.Insert(i, np);
                seg = Vector2.Distance(pts[i-1], pts[i]);
                acc = 0f;
            }
            acc += seg;
        }
        while (result.Count < count) result.Add(pts[pts.Count - 1]);
        return result.ToArray();
    }

    float[] Angles(Vector2[] pts)
    {
        var angles = new float[pts.Length - 1];
        for (int i = 0; i < angles.Length; i++)
        {
            Vector2 d = pts[i+1] - pts[i];
            angles[i] = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }
        return angles;
    }
}