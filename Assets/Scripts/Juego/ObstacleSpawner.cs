using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Referencia al Jugador")]
    [SerializeField] private Transform player;

    [Header("Configuración de Columnas")]
    // Ajustado a la posición real de tus paredes
    [SerializeField] private float leftWallX = -3f;
    [SerializeField] private float rightWallX = 3f;

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
    [SerializeField] private GameObject sierraPrefab;
    [SerializeField] private GameObject spikesPrefab; // Espinas

    [Header("Runas para las Cajas")]
    [SerializeField] private RuneDefinition runaLinea;
    [SerializeField] private RuneDefinition runaV;
    [SerializeField] private RuneDefinition runaCirculo;

    [Header("Probabilidades de Spawn")]
    [SerializeField] private float probabilidadCaja    = 0.3f;  // 30% caja
    [SerializeField] private float probabilidadSierra  = 0.2f;  // 20% sierra
    [SerializeField] private float probabilidadPuas    = 0.15f; // 15% púas en pared
                                                                 // 35% obstáculo normal

    [Header("Dificultad")]
    [SerializeField] private float difficultyIncreaseRate = 0.01f;

    [Header("Límite de Obstáculos en Pantalla")]
    [SerializeField] private int maxObstaclesOnScreen = 6;

    private readonly Color colorLinea   = new Color(0.2f, 0.8f, 1f);
    private readonly Color colorV       = new Color(1f, 0.3f, 0.5f);
    private readonly Color colorCirculo = new Color(0.4f, 1f, 0.4f);

    private float lastSpawnY = 0f;
    private float nextSpawnInterval;
    private float columnWidth;
    private float[] columnCenters = new float[3];
    private List<GameObject> activeObstacles = new List<GameObject>();

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        // Calcula el ancho de las 3 columnas basándose en tus paredes de -3 a 3
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

        if (player != null && player.position.y - lastSpawnY > nextSpawnInterval)
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
        // Spawn de púas EXCLUSIVO en las paredes
        if (spikesPrefab != null && Random.value < probabilidadPuas)
        {
            SpawnPuas();
        }

        // Spawn del resto de obstáculos en las columnas centrales
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

            float roll = Random.value;

            if (roll < probabilidadCaja)
                SpawnCaja(spawnPos);
            else if (roll < probabilidadCaja + probabilidadSierra)
                SpawnSierra(spawnPos);
            else
                SpawnObstaculo(spawnPos);
        }
    }

    void SpawnPuas()
    {
        // Elegir pared aleatoria
        bool esIzquierda = Random.value < 0.5f;
        
        // Empujón para que no se entierren en la pared de -3 o 3
        float offsetPared = 0.70f; 
        float wallX = esIzquierda ? (leftWallX + offsetPared) : (rightWallX - offsetPared);
        
        float spawnY = player.position.y + spawnDistanceAhead + Random.Range(-verticalJitter, verticalJitter);
        Vector3 pos = new Vector3(wallX, spawnY, 0f);

        // Generar las espinas
        GameObject spikes = Instantiate(spikesPrefab, pos, Quaternion.identity);

        // Forzar tu escala exacta del prefab
        spikes.transform.localScale = new Vector3(0.3f, 0.25f, 1f);

        // Rotar apuntando al centro
        if (esIzquierda)
        {
            spikes.transform.rotation = Quaternion.Euler(0, 0, -90f); // Apunta a la derecha
        }
        else
        {
            spikes.transform.rotation = Quaternion.Euler(0, 0, 90f);  // Apunta a la izquierda
        }

        // Asegurar que se dibujen por encima de las paredes visualmente
        SpriteRenderer sr = spikes.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 10;
        }

        activeObstacles.Add(spikes);
        AsegurarAutoDestroy(spikes);
    }

    void SpawnObstaculo(Vector3 pos)
    {
        if (obstaclePrefab == null) return;
        GameObject obj = Instantiate(obstaclePrefab, pos, Quaternion.identity);
        activeObstacles.Add(obj);
        AsegurarAutoDestroy(obj);
    }

    void SpawnSierra(Vector3 pos)
    {
        if (sierraPrefab == null) return;
        GameObject obj = Instantiate(sierraPrefab, pos, Quaternion.identity);
        activeObstacles.Add(obj);
        AsegurarAutoDestroy(obj);
    }

    void SpawnCaja(Vector3 pos)
    {
        if (boxPrefab == null) return;
        GameObject obj = Instantiate(boxPrefab, pos, Quaternion.identity);
        activeObstacles.Add(obj);
        AsegurarAutoDestroy(obj);

        BoxWithRune box = obj.GetComponent<BoxWithRune>();
        if (box == null) return;

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

        box.AsignarRuna(runaElegida, colorElegido);
    }

    void AsegurarAutoDestroy(GameObject obj)
    {
        if (obj.GetComponent<ObstacleAutoDestroy>() == null)
            obj.AddComponent<ObstacleAutoDestroy>();
    }

    public float[] GetColumnCenters() => columnCenters;
}