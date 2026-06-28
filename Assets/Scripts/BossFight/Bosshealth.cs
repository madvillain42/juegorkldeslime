using UnityEngine;
using UnityEngine.UI;
using TMPro;
 
public class BossHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;
 
    [Header("Barra de Vida")]
    public Image fillImage;
 
    [Header("Texto de Vida")]
    public TextMeshProUGUI lifeText;
 
    public float CurrentHP { get; private set; }
 
    public event System.Action<float> OnHPChanged;
    public event System.Action OnDeath;
 
    void Start()
    {
        CurrentHP = maxHP;
        UpdateBar();
        UpdateText();
    }
 
    public void TakeDamage(float amount)
    {
        if (CurrentHP <= 0f) return;
 
        CurrentHP = Mathf.Max(CurrentHP - amount, 0f);
        OnHPChanged?.Invoke(CurrentHP);
        UpdateBar();
        UpdateText();
 
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
        UpdateBar();
        UpdateText();
    }
 
    void UpdateBar()
    {
        if (fillImage != null)
            fillImage.fillAmount = CurrentHP / maxHP;
    }
 
    void UpdateText()
    {
        if (lifeText != null)
            lifeText.text = $"{CurrentHP}/{maxHP}";
    }
}