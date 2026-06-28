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
    public float attackInterval     = 2.5f;
    public float attackVariance     = 0.5f;
    public float warningDuration    = 0.5f;
    public float attackAnimDuration = 0.5f;
 
    [Header("Ataque de Runa")]
    public int   minPatternsBeforeRune = 3;
    public int   maxPatternsBeforeRune = 4;
    public float runeChallengeTime     = 4f;
 
    [Header("Runas reales (arrastra RunaZ RunaC RunaW)")]
    public RuneDefinition runaZ;
    public RuneDefinition runaC;
    public RuneDefinition runaW;

    [Header("Imágenes de Runas (UI en Canvas)")]
    public GameObject runaImageC;
    public GameObject runaImageW;
    public GameObject runaImageZ;
 
    private bool isActive       = false;
    private int  patternCount   = 0;
    private int  patternsUntilRune;
    private bool waitingForRune = false;
    private Animator anim;
 
    private const float WeightSingle = 0.50f;
    private const float WeightDouble = 0.50f;
 
    void Start()
    {
        anim = GetComponent<Animator>();
        runeSystem.OnSuccess += OnRuneResolved;
        runeSystem.OnFail    += OnRuneResolved;
        
        // Asignar las runas REALES al RuneSystem en vez de crear instancias vacías
        AsignarRunasReales();
        ResetRuneCounter();
        OcultarTodasLasRunas();
        
        StartBossFight();
    }
 
    void OnDestroy()
    {
        runeSystem.OnSuccess -= OnRuneResolved;
        runeSystem.OnFail    -= OnRuneResolved;
    }

    void AsignarRunasReales()
    {
        var runas = new List<RuneDefinition>();

        if (runaZ != null) runas.Add(runaZ);
        if (runaC != null) runas.Add(runaC);
        if (runaW != null) runas.Add(runaW);

        if (runas.Count == 0)
        {
            Debug.LogWarning("[BossAttackController] No hay runas asignadas — arrastra RunaZ RunaC RunaW en el Inspector");
            return;
        }

        runeSystem.availableRunes = runas.ToArray();
        Debug.Log($"[BossAttackController] {runas.Count} runas asignadas al RuneSystem");
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
 
        if (anim != null) anim.SetTrigger("IsAttacking");
        yield return new WaitForSeconds(attackAnimDuration);
 
        foreach (int lane in lanes)
            SpawnProjectile(lane);
    }
 
    IEnumerator RuneAttack()
    {
        waitingForRune = true;
        
        runeSystem.StartChallenge(runeChallengeTime);

        // Esperar un frame para que runaForzada se asigne en StartChallenge
        yield return null;

        if (runeSystem.runaForzada != null)
            MostrarRuna(runeSystem.runaForzada.runeName);

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

        string nombre = letraRuna.ToUpper();

        // Comparar por nombre de la runa
        if (runaC != null && nombre == runaC.runeName.ToUpper())
            if (runaImageC != null) runaImageC.SetActive(true);

        if (runaW != null && nombre == runaW.runeName.ToUpper())
            if (runaImageW != null) runaImageW.SetActive(true);

        if (runaZ != null && nombre == runaZ.runeName.ToUpper())
            if (runaImageZ != null) runaImageZ.SetActive(true);
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
 
        if (roll < WeightSingle)
            return new int[] { playerLane };
        else
        {
            int other;
            do { other = Random.Range(0, 3); } while (other == playerLane);
            return new int[] { playerLane, other };
        }
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