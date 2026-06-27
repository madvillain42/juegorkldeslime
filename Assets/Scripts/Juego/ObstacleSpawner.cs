using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Referencia al Jugador")]
    [SerializeField] private Transform player;

    [Header("Configuración de Columnas")]
    [SerializeField] private float leftWallX = -3f;
    [SerializeField] private float rightWallX = 3f;

    [Header("Configuración de Spawn")]
    [SerializeField] private float spawnDistanceAhead = 8f;
    [SerializeField] private float minSpawnInterval = 2.5f;
    [SerializeField] private float maxSpawnInterval = 4.5f;

    [Header("Variación de Posición")]
    [SerializeField] private float horizontalJitter = 0.4f;
    [SerializeField] private float verticalJitter = 0.3f; // Bajado para evitar amontonamiento

    [Header("Prefabs")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private GameObject sierraPrefab;
    [SerializeField] private GameObject spikesPrefab;

    [Header("Runas para las Cajas")]
    [SerializeField] private RuneDefinition runaZ;
    [SerializeField] private RuneDefinition runaC;
    [SerializeField] private RuneDefinition runaW;

    [Header("Sprites de las Cajas")]
    [SerializeField] private Sprite spriteRunaZ;
    [SerializeField] private Sprite spriteRunaC;
    [SerializeField] private Sprite spriteRunaW;

    [Header("Probabilidades de Spawn")]
    [SerializeField] private float probabilidadCaja   = 0.10f; // Pociones muy raras
    [SerializeField] private float probabilidadSierra = 0.35f; // Sierras frecuentes
    [SerializeField] private float probabilidadPuas   = 0.40f; // Púas muy frecuentes
                                                                // 0.15 obstáculo normal

    [Header("Dificultad")]
    [SerializeField] private float difficultyIncreaseRate = 0.01f;

    [Header("Límite de Obstáculos en Pantalla")]
    [SerializeField] private int maxObstaclesOnScreen = 5;

    // Separación mínima entre espinas para evitar que se amontonen
    private float lastSpikesY = -999f;
    [SerializeField] private float minDistanciaEntrePuas = 4f;

    private readonly Color colorZ = new Color(1f, 0.2f, 0.2f);
    private readonly Color colorC = new Color(1f, 0.6f, 0.1f);
    private readonly Color colorW = new Color(0.2f, 0.5f, 1f);

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
            minSpawnInterval = Mathf.Max(1.5f, 2.5f - difficulty);
            maxSpawnInterval = Mathf.Max(2.5f, 4.5f - difficulty);

            nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    void SpawnRow()
    {
        float spawnY = player.position.y + spawnDistanceAhead;
        bool spawnoPuas = false;

        // Solo spawneamos púas si hay distancia suficiente
        if (spikesPrefab != null && 
            Random.value < probabilidadPuas && 
            spawnY - lastSpikesY >= minDistanciaEntrePuas)
        {
            SpawnPuas(spawnY);
            lastSpikesY = spawnY;
            spawnoPuas = true;
        }

        // Si spawneó púas, reducir probabilidad de caja en esa fila
        List<int> columnasDisponibles = new List<int> { 0, 1, 2 };
        int cantidadObstaculos = Random.Range(1, 3);

        for (int i = 0; i < cantidadObstaculos; i++)
        {
            int columna = columnasDisponibles[Random.Range(0, columnasDisponibles.Count)];
            columnasDisponibles.Remove(columna);

            float jitterX = Random.Range(-horizontalJitter, horizontalJitter);
            float jitterY = Random.Range(-verticalJitter, verticalJitter);
            float posY    = spawnY + jitterY;
            Vector3 spawnPos = new Vector3(columnCenters[columna] + jitterX, posY, 0f);

            float roll = Random.value;

            // Si hay púas en esta fila, no spawnear cajas
            if (spawnoPuas && roll < probabilidadCaja)
            {
                roll = probabilidadCaja; // Forzar a no ser caja
            }

            if (roll < probabilidadCaja)
                SpawnCaja(spawnPos);
            else if (roll < probabilidadCaja + probabilidadSierra)
                SpawnSierra(spawnPos);
            else
                SpawnObstaculo(spawnPos);
        }
    }

    void SpawnPuas(float spawnY)
    {
        bool esIzquierda = Random.value < 0.5f;
        float offsetPared = 0.70f;
        float wallX = esIzquierda ? (leftWallX + offsetPared) : (rightWallX - offsetPared);

        Vector3 pos = new Vector3(wallX, spawnY, 0f);
        GameObject spikes = Instantiate(spikesPrefab, pos, Quaternion.identity);

        spikes.transform.localScale = new Vector3(0.3f, 0.25f, 1f);
        spikes.transform.rotation = Quaternion.Euler(0, 0, esIzquierda ? -90f : 90f);

        SpriteRenderer sr = spikes.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 10;

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
        Sprite spriteElegido;

        switch (runaIndex)
        {
            case 0:
                runaElegida   = runaZ;
                colorElegido  = colorZ;
                spriteElegido = spriteRunaZ;
                break;
            case 1:
                runaElegida   = runaC;
                colorElegido  = colorC;
                spriteElegido = spriteRunaC;
                break;
            default:
                runaElegida   = runaW;
                colorElegido  = colorW;
                spriteElegido = spriteRunaW;
                break;
        }

        box.AsignarRuna(runaElegida, colorElegido, spriteElegido);
    }

    void AsegurarAutoDestroy(GameObject obj)
    {
        if (obj.GetComponent<ObstacleAutoDestroy>() == null)
            obj.AddComponent<ObstacleAutoDestroy>();
    }

    public float[] GetColumnCenters() => columnCenters;
}