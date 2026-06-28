using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;

    public float CurrentHP { get; private set; }

    public event System.Action<float> OnHPChanged; // pasa el HP actual
    public event System.Action OnDeath;

    void Start()
    {
        CurrentHP = maxHP;
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHP <= 0f) return;

        CurrentHP = Mathf.Max(CurrentHP - amount, 0f);
        OnHPChanged?.Invoke(CurrentHP);

        Debug.Log($"[Boss] HP: {CurrentHP}/{maxHP}");

        if (CurrentHP <= 0f)
        {
            Debug.Log("[Boss] Muerto.");
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (CurrentHP <= 0f) return;
        CurrentHP = Mathf.Min(CurrentHP + amount, maxHP);
        OnHPChanged?.Invoke(CurrentHP);
    }
}