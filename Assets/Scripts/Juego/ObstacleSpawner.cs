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
    [SerializeField] private float horizontalJitter = 0.4f;
    [SerializeField] private float verticalJitter = 0.8f;

    [Header("Prefabs")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject boxPrefab;

    [Header("Runas para las Cajas")]
    [SerializeField] private RuneDefinition runaLinea;    // Celeste
    [SerializeField] private RuneDefinition runaV;        // Rosa
    [SerializeField] private RuneDefinition runaCirculo;  // Verde

    [Header("Dificultad")]
    [SerializeField] private float difficultyIncreaseRate = 0.01f;

    [Header("Límite de Obstáculos en Pantalla")]
    [SerializeField] private int maxObstaclesOnScreen = 6;

    // Colores fijos por runa
    private readonly Color colorLinea   = new Color(0.2f, 0.8f, 1f);   // Celeste
    private readonly Color colorV       = new Color(1f, 0.3f, 0.5f);   // Rosa
    private readonly Color colorCirculo = new Color(0.4f, 1f, 0.4f);   // Verde

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

        activeObstacles.RemoveAll(o => o == null);

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
        int cantidadObstaculos = Random.Range(1, 3);

        for (int i = 0; i < cantidadObstaculos; i++)
        {
            int columna = columnasDisponibles[Random.Range(0, columnasDisponibles.Count)];
            columnasDisponibles.Remove(columna);

            float jitterX = Random.Range(-horizontalJitter, horizontalJitter);
            float jitterY = Random.Range(-verticalJitter, verticalJitter);
            float spawnY  = player.position.y + spawnDistanceAhead + jitterY;
            Vector3 spawnPos = new Vector3(columnCenters[columna] + jitterX, spawnY, 0f);

            bool esCaja = Random.value < 0.3f;

            if (esCaja)
                SpawnCaja(spawnPos);
            else
            {
                GameObject obj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
                activeObstacles.Add(obj);
            }
        }
    }

    void SpawnCaja(Vector3 pos)
    {
        GameObject obj = Instantiate(boxPrefab, pos, Quaternion.identity);
        activeObstacles.Add(obj);

        BoxWithRune box = obj.GetComponent<BoxWithRune>();
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        if (box == null) return;

        // Elegir runa aleatoria de las 3
        int runaIndex = Random.Range(0, 3);

        RuneDefinition runaElegida;
        Color colorElegido;

        switch (runaIndex)
        {
            case 0:
                runaElegida  = runaLinea;
                colorElegido = colorLinea;
                break;
            case 1:
                runaElegida  = runaV;
                colorElegido = colorV;
                break;
            default:
                runaElegida  = runaCirculo;
                colorElegido = colorCirculo;
                break;
        }

        // Asignar runa y color a la caja
        box.AsignarRuna(runaElegida, colorElegido);
    }

    public float[] GetColumnCenters() => columnCenters;
}