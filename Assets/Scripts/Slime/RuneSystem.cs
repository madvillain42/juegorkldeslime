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
    [Range(0.3f, 0.95f)]
    public float matchThreshold = 0.35f;
    public int segmentCount = 16;

    [Header("Modo")]
    // Escalada → true  (jugador dibuja lo que quiere, abre cajas)
    // Bossfight → false (el jefe pide una runa específica)
    public bool modoLibre = true;

    [Header("Colores de Estela")]
    public Color[] traceColors = new Color[]
    {
        new Color(0.2f, 0.8f, 1f),
        new Color(1f, 0.3f, 0.5f),
        new Color(0.4f, 1f, 0.4f)
    };

    public event System.Action<RuneDefinition> OnSuccessWithRune;
    public event System.Action OnSuccess;
    public event System.Action OnFail;
    public bool IsActive { get; private set; } = false;

    // En modo forzado, esta es la runa que el jefe pidió
    private RuneDefinition runaForzada = null;

    private List<Vector2> drawnPoints = new List<Vector2>();
    private bool isDrawing = false;
    private Coroutine timerCoroutine;
    private InputAction drawAction;

    public void Init(InputAction draw)
    {
        drawAction = draw;
        if (runePanel != null) runePanel.SetActive(false);
    }

    public void Init(InputAction draw, InputAction press) => Init(draw);

    // ─── StartChallenge ───────────────────────────────────────────────────────
    // Modo libre (escalada) → jugador dibuja lo que quiere
    // Modo forzado (bossfight) → elige una runa aleatoria y la muestra
    public void StartChallenge(float timeLimit)
    {
        if (availableRunes == null || availableRunes.Length == 0)
        {
            Debug.LogWarning("[RuneSystem] No hay runas asignadas");
            return;
        }

        drawnPoints.Clear();
        IsActive = true;

        if (!modoLibre)
        {
            // Bossfight: elegir runa aleatoria y mostrarla al jugador
            runaForzada = availableRunes[Random.Range(0, availableRunes.Length)];
            Debug.Log($"[RuneSystem] Modo forzado — Runa pedida: {runaForzada.runeName}");

            if (runeTargetImage != null && runaForzada.displaySprite != null)
                runeTargetImage.sprite = runaForzada.displaySprite;
        }
        else
        {
            runaForzada = null;
            Debug.Log("[RuneSystem] Modo libre — dibuja lo que quieras");
        }

        if (runePanel != null) runePanel.SetActive(true);

        if (traceRenderer != null)
        {
            traceRenderer.positionCount = 0;
            traceRenderer.enabled = true;

            if (traceColors != null && traceColors.Length > 0)
            {
                Color c = traceColors[Random.Range(0, traceColors.Length)];
                traceRenderer.startColor = c;
                traceRenderer.endColor   = new Color(c.r, c.g, c.b, 0f);
            }
        }

        timerCoroutine = StartCoroutine(Timer(timeLimit));
    }

    public void NotifyPressStarted()
    {
        if (!IsActive) return;
        drawnPoints.Clear();
        isDrawing = true;
        if (traceRenderer != null) traceRenderer.positionCount = 0;
    }

    public void NotifyPressEnded()
    {
        if (!IsActive || !isDrawing) return;
        isDrawing = false;
        Evaluate();
    }

    public void Tick()
    {
        if (!isDrawing) return;

        Vector2 screenPos = drawAction.ReadValue<Vector2>();

        if (drawnPoints.Count > 0 &&
            Vector2.Distance(drawnPoints[drawnPoints.Count - 1], screenPos) < 3f) return;

        drawnPoints.Add(screenPos);

        if (traceRenderer != null)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            traceRenderer.positionCount = drawnPoints.Count;
            traceRenderer.SetPosition(drawnPoints.Count - 1, world);
        }
    }

    void Evaluate()
    {
        if (drawnPoints.Count < 5) { Fail(); return; }

        List<Vector2> normalized = NormalizePoints(drawnPoints);

        float bestScore = 0f;
        RuneDefinition bestRune = null;

        foreach (var rune in availableRunes)
        {
            List<Vector2> templateNorm = NormalizePoints(new List<Vector2>(rune.templatePoints));
            float score = Compare(normalized, templateNorm.ToArray());
            Debug.Log($"[Runa] {rune.runeName} | Score: {score:F2}");
            if (score > bestScore)
            {
                bestScore = score;
                bestRune  = rune;
            }
        }

        if (bestScore >= matchThreshold && bestRune != null)
        {
            if (!modoLibre)
            {
                // Bossfight: solo exitoso si dibujó la runa correcta
                if (bestRune == runaForzada)
                {
                    Debug.Log($"[RuneSystem] ✅ Bossfight — Runa correcta: {bestRune.runeName}");
                    End();
                    OnSuccess?.Invoke();
                    OnSuccessWithRune?.Invoke(bestRune);
                }
                else
                {
                    Debug.Log($"[RuneSystem] ❌ Bossfight — Runa incorrecta: {bestRune.runeName} | Pedía: {runaForzada?.runeName}");
                    Fail();
                }
            }
            else
            {
                // Escalada: exitoso con cualquier runa reconocida
                Debug.Log($"[RuneSystem] ✅ Escalada — Runa detectada: {bestRune.runeName}");
                End();
                OnSuccess?.Invoke();
                OnSuccessWithRune?.Invoke(bestRune);
            }
        }
        else
        {
            Debug.Log($"[RuneSystem] ❌ No se reconoció ninguna runa | Mejor: {bestRune?.runeName} ({bestScore:F2})");
            Fail();
        }
    }

    List<Vector2> NormalizePoints(List<Vector2> pts)
    {
        if (pts.Count == 0) return pts;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var p in pts)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        float width  = Mathf.Max(maxX - minX, 1f);
        float height = Mathf.Max(maxY - minY, 1f);

        var result = new List<Vector2>();
        foreach (var p in pts)
            result.Add(new Vector2((p.x - minX) / width, (p.y - minY) / height));

        return result;
    }

    void Fail() { End(); OnFail?.Invoke(); }

    void End()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        IsActive    = false;
        isDrawing   = false;
        runaForzada = null;
        drawnPoints.Clear();
        if (runePanel != null) runePanel.SetActive(false);
        if (traceRenderer != null) traceRenderer.enabled = false;
    }

    IEnumerator Timer(float limit)
    {
        float elapsed = 0f;
        while (elapsed < limit)
        {
            elapsed += Time.unscaledDeltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(limit - elapsed).ToString();
            yield return null;
        }
        if (IsActive) { isDrawing = false; Fail(); }
    }

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