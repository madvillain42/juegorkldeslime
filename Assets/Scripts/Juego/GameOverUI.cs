using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button reintentoButton;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (reintentoButton != null)
            reintentoButton.onClick.AddListener(Reintentar);
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.GameOver)
        {
            if (gameOverPanel != null && !gameOverPanel.activeSelf)
                gameOverPanel.SetActive(true);
        }
    }

    public void Reintentar()
    {
        // Resetear stats Y cambiar estado ANTES de recargar
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetStats();
            GameManager.Instance.ChangeState(GameState.Climbing); // ← clave
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}