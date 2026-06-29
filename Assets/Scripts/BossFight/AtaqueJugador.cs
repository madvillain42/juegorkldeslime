using UnityEngine;

public class AtaqueJugador : MonoBehaviour
{
    [Header("Referencias")]
    public RuneSystem runeSystem;
    public BossHealth bossHealth;

    [Header("Balanceo")]
    public float danoPorRuna = 20f;

    [Header("Daño Pasivo")]
    public float danoPasivo = 1f;
    public float intervaloDanoPasivo = 1f;

    [Header("Sonidos")]
    public AudioClip sonidoDanoPasivo;
    public AudioClip sonidoGolpeRuna;

    [Header("Animaciones")]
    public Animator animatorPlayer;

    private float timerDanoPasivo = 0f;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (runeSystem != null)
        {
            runeSystem.OnSuccess += RealizarGolpe;
        }
        else
        {
            Debug.LogWarning("[AtaqueJugador] Falta asignar el RuneSystem en el Inspector.");
        }
    }

    void OnDestroy()
    {
        if (runeSystem != null)
        {
            runeSystem.OnSuccess -= RealizarGolpe;
        }
    }

    void Update()
    {
        if (bossHealth == null) return;

        timerDanoPasivo += Time.deltaTime;

        if (timerDanoPasivo >= intervaloDanoPasivo)
        {
            timerDanoPasivo = 0f;
            bossHealth.TakeDamage(danoPasivo);

            if (sonidoDanoPasivo != null)
                audioSource.PlayOneShot(sonidoDanoPasivo);

            Debug.Log($"[AtaqueJugador] Daño pasivo: -{danoPasivo} HP");
        }
    }

    private void RealizarGolpe()
    {
        if (animatorPlayer != null)
        {
            // animatorPlayer.Play("GolpeRuna"); 
        }

        if (bossHealth != null)
        {
            Debug.Log($"[Player] ¡Runa completada con éxito! Golpeando al jefe con {danoPorRuna} de daño.");
            bossHealth.TakeDamage(danoPorRuna);

            if (sonidoGolpeRuna != null)
                audioSource.PlayOneShot(sonidoGolpeRuna);
        }
        else
        {
            Debug.LogWarning("[AtaqueJugador] No se pudo dañar al jefe porque BossHealth no está asignado.");
        }
    }
}