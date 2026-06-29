using UnityEngine;
using UnityEngine.SceneManagement;

public class BossResultManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelVictoria;
    public GameObject panelDerrota;

    [Header("Referencias")]
    public BossHealth bossHealth;

    [Header("Escenas")]
    public string escenaSiguiente = "Nivel2";
    public string escenaMenu      = "Menu";

    private bool resultadoMostrado = false; // ← evita llamarse múltiples veces

    void Start()
    {
        if (bossHealth != null)
            bossHealth.OnDeath += MostrarVictoria;

        // Suscribirse al cambio de estado del GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += MostrarDerrota;
    }

    void OnDestroy()
    {
        if (bossHealth != null)
            bossHealth.OnDeath -= MostrarVictoria;

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver -= MostrarDerrota;
    }

    void MostrarVictoria()
    {
        if (resultadoMostrado) return;
        resultadoMostrado = true;
        Time.timeScale = 0f;
        if (panelVictoria != null) panelVictoria.SetActive(true);
    }

    public void MostrarDerrota()
    {
        if (resultadoMostrado) return;
        resultadoMostrado = true;
        Time.timeScale = 0f;
        if (panelDerrota != null) panelDerrota.SetActive(true);
    }

    // ─── Botones ──────────────────────────────────────────────────────────────

    public void BotonAvanzar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaSiguiente);
    }

    public void BotonReintentar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BotonMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }
}