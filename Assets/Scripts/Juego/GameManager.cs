using UnityEngine;

// Define los estados posibles de tu juego
public enum GameState { Climbing, BossBattle, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // El juego siempre inicia en la fase de escalada de la torre
    public GameState CurrentState { get; private set; } = GameState.Climbing;

    private void Awake()
    {
        // Implementación de Singleton para acceder al manager desde cualquier script
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

    /// <summary>
    /// Cambia el estado del juego (ej. de Climbing a BossBattle)
    /// </summary>
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameManager] Estado cambiado a: {newState}");
    }
}