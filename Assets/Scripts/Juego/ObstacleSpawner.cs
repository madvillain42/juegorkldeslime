using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Referencia al Jugador")]
    [SerializeField] private Transform player;

    [Header("Configuración de Columnas")]
    [SerializeField] private float leftWallX = -2.5f;
    [SerializeField] private float rightWallX = 2.5f;

    [Header("Configuración de Spawn")]
    [SerializeField] private float spawnDistanceAhead = 8f;
    [SerializeField] private float minSpawnInterval = 4f;
    [SerializeField] private float maxSpawnInterval = 7f;

    [Header("Variación de Posición")]
    [SerializeField] private float horizontalJitter = 0.4f; // Desplazamiento aleatorio dentro de la columna
    [SerializeField] private float verticalJitter = 0.8f;   // Desplazamiento aleatorio en Y para romper la grilla

    [Header("Prefabs")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject boxPrefab;

    [Header("Dificultad")]
    [SerializeField] private float difficultyIncreaseRate = 0.01f;

    [Header("Límite de Obstáculos en Pantalla")]
    [SerializeField] private int maxObstaclesOnScreen = 6;

    private float lastSpawnY = 0f;
    private float nextSpawnInterval;
    private float columnWidth;
    private float[] columnCenters = new float[3];
    private List<GameObject> activeObstacles = new List<GameObject>();

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        float totalWidth = rightWallX - leftWallX;
        columnWidth = totalWidth / 3f;

        for (int i = 0; i < 3; i++)
            columnCenters[i] = leftWallX + (columnWidth * i) + (columnWidth / 2f);

        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Climbing) return;

        // Limpiar referencias nulas de obstáculos ya destruidos
        activeObstacles.RemoveAll(o => o == null);

        // No spawnear si hay demasiados en pantalla
        if (activeObstacles.Count >= maxObstaclesOnScreen) return;

        if (player.position.y - lastSpawnY > nextSpawnInterval)
        {
            SpawnRow();
            lastSpawnY = player.position.y;

            float difficulty = player.position.y * difficultyIncreaseRate;
            minSpawnInterval = Mathf.Max(1.5f, 4f - difficulty);
            maxSpawnInterval = Mathf.Max(2.5f, 7f - difficulty);

            nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    void SpawnRow()
    {
        List<int> columnasDisponibles = new List<int> { 0, 1, 2 };

        // Máximo 2 obstáculos por fila, siempre deja 1 columna libre
        int cantidadObstaculos = Random.Range(1, 3);

        for (int i = 0; i < cantidadObstaculos; i++)
        {
            int columna = columnasDisponibles[Random.Range(0, columnasDisponibles.Count)];
            columnasDisponibles.Remove(columna);

            // Jitter horizontal: se mueve un poco dentro de su columna
            float jitterX = Random.Range(-horizontalJitter, horizontalJitter);

            // Jitter vertical: rompe la grilla horizontal perfecta
            float jitterY = Random.Range(-verticalJitter, verticalJitter);

            float spawnY = player.position.y + spawnDistanceAhead + jitterY;
            Vector3 spawnPos = new Vector3(columnCenters[columna] + jitterX, spawnY, 0f);

            bool esCaja = Random.value < 0.3f;
            GameObject obj = Instantiate(esCaja ? boxPrefab : obstaclePrefab, spawnPos, Quaternion.identity);
            activeObstacles.Add(obj);
        }
    }

    public float[] GetColumnCenters() => columnCenters;
}