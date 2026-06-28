using UnityEngine;

public class AtaqueJugador : MonoBehaviour
{
    [Header("Referencias")]
    public RuneSystem runeSystem;
    public BossHealth bossHealth;

    [Header("Balanceo")]
    public float danoPorRuna = 20f;

    [Header("Animaciones")]
    public Animator animatorPlayer; // Dejado listo para tus futuras animaciones

    void Start()
    {
        // El jugador se suscribe al evento de éxito del dibujo
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
        // Limpieza del evento si el jugador se destruye o cambia de escena
        if (runeSystem != null)
        {
            runeSystem.OnSuccess -= RealizarGolpe;
        }
    }

    private void RealizarGolpe()
    {
        // --- AQUÍ IRÁ TU ANIMACIÓN EN EL FUTURO ---
        if (animatorPlayer != null)
        {
            // Cuando tengas la animación, descomenta la línea de abajo y pon el nombre exacto de tu animación:
            // animatorPlayer.Play("GolpeRuna"); 
        }

        // Aplicamos el daño directamente al jefe conectado
        if (bossHealth != null)
        {
            Debug.Log($"[Player] ¡Runa completada con éxito! Golpeando al jefe con {danoPorRuna} de daño.");
            bossHealth.TakeDamage(danoPorRuna);
        }
        else
        {
            Debug.LogWarning("[AtaqueJugador] No se pudo dañar al jefe porque BossHealth no está asignado.");
        }
    }
}