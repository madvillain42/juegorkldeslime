using UnityEngine;

public enum GameState { Climbing, BossBattle, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Climbing;

    // ─── Stats que viajan de la escalada a la bossfight ───────────────────────
    
    // Daño acumulado por pociones (+15% por caja)
    public float DamageMultiplier { get; private set; } = 1f;
    private const float DAMAGE_PER_POTION = 0.15f;

    // Escudo — único, protege 1 golpe en escalada
    public bool TieneEscudo { get; private set; } = false;

    // Velocidad — reduce cooldown del dash en escalada y runas en bossfight
    public float CooldownReduction { get; private set; } = 0f;
    private const float COOLDOWN_REDUCTION_PER_POTION = 0.25f;
    private const float MIN_COOLDOWN = 2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameManager] Estado cambiado a: {newState}");
    }

    // ─── Métodos para aplicar pociones ───────────────────────────────────────

    public void AplicarPocionDano()
    {
        DamageMultiplier += DAMAGE_PER_POTION;
        Debug.Log($"[GameManager] Poción de daño — Multiplicador: {DamageMultiplier:F2}x");
    }

    public void AplicarPocionEscudo()
    {
        if (TieneEscudo)
        {
            Debug.Log("[GameManager] Ya tienes escudo — poción ignorada");
            return;
        }
        TieneEscudo = true;
        Debug.Log("[GameManager] Poción de escudo aplicada");
    }

    public void AplicarPocionVelocidad()
    {
        CooldownReduction += COOLDOWN_REDUCTION_PER_POTION;
        Debug.Log($"[GameManager] Poción de velocidad — Reducción total: {CooldownReduction:F2}s");
    }

    // Retorna el cooldown del dash con la reducción aplicada
    public float GetDashCooldown(float baseCooldown)
    {
        return Mathf.Max(MIN_COOLDOWN, baseCooldown - CooldownReduction);
    }

    // Retorna el cooldown de runas con la reducción aplicada (para bossfight)
    public float GetRuneCooldown(float baseCooldown)
    {
        return Mathf.Max(MIN_COOLDOWN, baseCooldown - CooldownReduction);
    }

    // Consumir el escudo al recibir un golpe
    public bool ConsumirEscudo()
    {
        if (!TieneEscudo) return false;
        TieneEscudo = false;
        Debug.Log("[GameManager] Escudo consumido");
        return true;
    }

    // Reset para nueva partida
    public void ResetStats()
    {
        DamageMultiplier  = 1f;
        TieneEscudo       = false;
        CooldownReduction = 0f;
        Debug.Log("[GameManager] Stats reseteados");
    }
}