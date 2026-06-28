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
    [SerializeField] private float horizontalJitter = 0.4f;
    [SerializeField] private float verticalJitter = 0.3f; 

    [Header("Prefabs Originales")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private GameObject sierraPrefab;
    [SerializeField] private GameObject spikesPrefab;

    [Header("NUEVO: Configuración Bola de Fuego (Meteoro)")]
    [SerializeField] private GameObject warningPrefab;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private float probabilidadMeteoro = 0.15f; // 15% de salir
    [SerializeField] private float tiempoAdvertencia = 1.0f; // 1 segundo

    [Header("Runas para las Cajas")]
    [SerializeField] private RuneDefinition runaZ;
    [SerializeField] private RuneDefinition runaC;
    [SerializeField] private RuneDefinition runaW;

    [Header("Sprites de las Cajas")]
    [SerializeField] private Sprite spriteRunaZ;
    [SerializeField] private Sprite spriteRunaC;
    [SerializeField] private Sprite spriteRunaW;

    [Header("Probabilidades de Spawn (Resto)")]
    [SerializeField] private float probabilidadCaja   = 0.10f; 
    [SerializeField] private float probabilidadSierra = 0.35f; 
    [SerializeField] private float probabilidadPuas   = 0.40f; 

    [Header("Dificultad")]
    [SerializeField] private float difficultyIncreaseRate = 0.01f;
    [SerializeField] private int maxObstaclesOnScreen = 5;

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

        // 1. Intentar spawnear Bola de Fuego (15% probabilidad)
        if (warningPrefab != null && fireballPrefab != null && Random.value < probabilidadMeteoro)
        {
            SpawnBolaDeFuego(spawnY);
        }

        // 2. Intentar spawnear Púas
        if (spikesPrefab != null && Random.value < probabilidadPuas && spawnY - lastSpikesY >= minDistanciaEntrePuas)
        {
            SpawnPuas(spawnY);
            lastSpikesY = spawnY;
            spawnoPuas = true;
        }

        // 3. Generar Cajas, Sierras u Obstáculos normales
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

            if (spawnoPuas && roll < probabilidadCaja)
            {
                roll = probabilidadCaja; 
            }

            if (roll < probabilidadCaja) SpawnCaja(spawnPos);
            else if (roll < probabilidadCaja + probabilidadSierra) SpawnSierra(spawnPos);
            else SpawnObstaculo(spawnPos);
        }
    }

    void SpawnBolaDeFuego(float baseSpawnY)
    {
        // Seleccionamos 1 de los 3 carriles de forma aleatoria
        int columnaElegida = Random.Range(0, 3);
        float targetX = columnCenters[columnaElegida];

        // Ya no calculamos la Y aquí, el aviso se pegará a la cámara por sí solo
        // Lo instanciamos temporalmente en 0 en Y, el script MeteorWarning lo corregirá al instante
        GameObject aviso = Instantiate(warningPrefab, new Vector3(targetX, 0f, 0f), Quaternion.identity);
        
        MeteorWarning scriptAviso = aviso.GetComponent<MeteorWarning>();
        if (scriptAviso != null)
        {
            // Le pasamos el prefab del fuego, el tiempo de espera, la posición X y el sonido
            scriptAviso.Inicializar(fireballPrefab, tiempoAdvertencia, targetX, warningSound);
        }
        
        // Lo añadimos a la lista para no exceder el límite en pantalla
        activeObstacles.Add(aviso);
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