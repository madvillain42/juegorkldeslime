using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackController : MonoBehaviour
{
    [Header("Referencias")]
    public LaneSystem laneSystem;
    public RuneSystem runeSystem;

    [Header("Prefabs")]
    public GameObject projectilePrefab;
    public GameObject warningPrefab;

    [Header("Posiciones")]
    public float warningY = 1f;
    public float spawnY   = 3f;
    public float despawnY = -5f;

    [Header("Timing")]
    public float attackInterval  = 2.5f;
    public float attackVariance  = 0.5f;
    public float warningDuration = 0.5f;

    [Header("Ataque de Runa")]
    public int   minPatternsBeforeRune = 3;
    public int   maxPatternsBeforeRune = 4;
    public float runeChallengeTime     = 4f;

    [Header("Imágenes de Runas (UI en Canvas)")]
    public GameObject runaImageC;
    public GameObject runaImageW;
    public GameObject runaImageZ;

    private bool isActive      = false;
    private int  patternCount  = 0;
    private int  patternsUntilRune;
    private bool waitingForRune = false;

    private const float WeightSingle = 0.50f;
    private const float WeightDouble = 0.50f;

    void Start()
    {
        runeSystem.OnSuccess += OnRuneResolved;
        runeSystem.OnFail    += OnRuneResolved;
        
        ResetRuneCounter();
        OcultarTodasLasRunas(); 
        
        StartBossFight();
    }

    void OnDestroy()
    {
        runeSystem.OnSuccess -= OnRuneResolved;
        runeSystem.OnFail    -= OnRuneResolved;
    }

    public void StartBossFight()
    {
        isActive     = true;
        patternCount = 0;
        ResetRuneCounter();
        StartCoroutine(AttackLoop());
    }

    public void StopBossFight()
    {
        isActive = false;
        StopAllCoroutines();
        OcultarTodasLasRunas();
    }

    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(1.5f);

        while (isActive)
        {
            float wait = attackInterval + Random.Range(-attackVariance, attackVariance);
            yield return new WaitForSeconds(wait);

            if (!isActive) yield break;

            patternCount++;

            if (patternCount >= patternsUntilRune)
            {
                yield return StartCoroutine(RuneAttack());
                patternCount = 0;
                ResetRuneCounter();
            }
            else
            {
                yield return StartCoroutine(NormalAttack());
            }
        }
    }

    IEnumerator NormalAttack()
    {
        int[] lanes = PickLanes();

        var warnings = ShowWarnings(lanes);
        yield return new WaitForSeconds(warningDuration);
        DestroyWarnings(warnings);

        foreach (int lane in lanes)
            SpawnProjectile(lane);
    }

    IEnumerator RuneAttack()
    {
        waitingForRune = true;
        
        // 1. Iniciamos el reto en el sistema.
        runeSystem.StartChallenge(runeChallengeTime);

        // 2. Leemos qué runa real acaba de elegir y prendemos su imagen
        if (runeSystem.runaForzada != null)
        {
            MostrarRuna(runeSystem.runaForzada.runeName);
        }

        yield return new WaitUntil(() => !waitingForRune);
    }

    void OnRuneResolved()
    {
        waitingForRune = false;
        OcultarTodasLasRunas(); 
    }

    public void MostrarRuna(string letraRuna)
    {
        OcultarTodasLasRunas();

        switch (letraRuna.ToUpper())
        {
            case "C": if (runaImageC != null) runaImageC.SetActive(true); break;
            case "W": if (runaImageW != null) runaImageW.SetActive(true); break;
            case "Z": if (runaImageZ != null) runaImageZ.SetActive(true); break;
        }
    }

    public void OcultarTodasLasRunas()
    {
        if (runaImageC != null) runaImageC.SetActive(false);
        if (runaImageW != null) runaImageW.SetActive(false);
        if (runaImageZ != null) runaImageZ.SetActive(false);
    }

    int[] PickLanes()
    {
        int playerLane = laneSystem.CurrentLane;
        float roll = Random.Range(0f, WeightSingle + WeightDouble);

        if (roll < WeightSingle) return new int[] { playerLane };
        
        int other;
        do { other = Random.Range(0, 3); } while (other == playerLane);
        return new int[] { playerLane, other };
    }

    void ResetRuneCounter()
    {
        patternsUntilRune = Random.Range(minPatternsBeforeRune, maxPatternsBeforeRune + 1);
    }

    List<GameObject> ShowWarnings(int[] lanes)
    {
        var list = new List<GameObject>();
        foreach (int lane in lanes)
        {
            if (warningPrefab == null) continue;
            float x = laneSystem.PositionsX[lane];
            list.Add(Instantiate(warningPrefab, new Vector3(x, warningY, 0f), Quaternion.identity));
        }
        return list;
    }

    void DestroyWarnings(List<GameObject> warnings)
    {
        foreach (var w in warnings) Destroy(w);
    }

    void SpawnProjectile(int lane)
    {
        if (projectilePrefab == null) return;
        float x = laneSystem.PositionsX[lane];
        var proj = Instantiate(projectilePrefab, new Vector3(x, spawnY, 0f), Quaternion.identity);
        var bp = proj.GetComponent<BossProjectile>();
        if (bp != null) bp.Init(despawnY);
    }
}